using HydeSearch.Configuration;
using HydeSearch.Models;
using Microsoft.Extensions.Options;

namespace HydeSearch.Services;

/// <summary>
/// Interface for hybrid search combining BM25 keyword search and HyDE semantic search
/// </summary>
public interface IHybridSearchService
{
    /// <summary>
    /// Perform hybrid search combining BM25 and HyDE algorithms
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Combined search results</returns>
    Task<IEnumerable<HybridSearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Index documents for both BM25 and HyDE search
    /// </summary>
    /// <param name="documents">Documents to index</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task IndexDocumentsAsync(IEnumerable<Document> documents, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get the number of indexed documents
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document count</returns>
    Task<int> GetIndexedDocumentCountAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clear all indexed documents
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ClearIndexAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Hybrid search service that combines BM25 keyword search with HyDE semantic search
/// </summary>
public class HybridSearchService : IHybridSearchService
{
    private readonly IBM25Service _bm25Service;
    private readonly IHydeSearchService _hydeSearchService;
    private readonly IDocumentStore _documentStore;
    private readonly HybridSearchConfiguration _config;
    private readonly ILogger<HybridSearchService> _logger;

    public HybridSearchService(
        IBM25Service bm25Service,
        IHydeSearchService hydeSearchService,
        IDocumentStore documentStore,
        IOptions<HybridSearchConfiguration> config,
        ILogger<HybridSearchService> logger)
    {
        _bm25Service = bm25Service;
        _hydeSearchService = hydeSearchService;
        _documentStore = documentStore;
        _config = config.Value;
        _logger = logger;
    }

    public async Task<IEnumerable<HybridSearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogWarning("Empty query provided to hybrid search");
            return [];
        }

        _logger.LogInformation("Starting hybrid search for query: '{Query}' (BM25: {BM25Enabled}, HyDE: {HydeEnabled})", 
            query, _config.EnableBM25, _config.EnableHyDE);

        var documents = await _documentStore.GetAllDocumentsAsync(cancellationToken);
        var documentList = documents.ToList();

        if (!documentList.Any())
        {
            _logger.LogWarning("No documents found in the index");
            return [];
        }

        var results = new Dictionary<string, HybridSearchResult>();

        // Perform BM25 search if enabled
        List<BM25Result> bm25Results = [];
        if (_config.EnableBM25)
        {
            var bm25ResultsEnum = await _bm25Service.CalculateScoresAsync(query, documentList, cancellationToken);
            bm25Results = bm25ResultsEnum.ToList();
            _logger.LogDebug("BM25 search returned {Count} results", bm25Results.Count);
        }

        // Perform HyDE search if enabled
        List<SearchResult> hydeResults = [];
        if (_config.EnableHyDE)
        {
            var hydeResultsEnum = await _hydeSearchService.SearchAsync(query, cancellationToken);
            hydeResults = hydeResultsEnum.ToList();
            _logger.LogDebug("HyDE search returned {Count} results", hydeResults.Count);
        }

        // Apply reranking strategy
        var finalResults = ApplyRerankingStrategy(bm25Results, hydeResults)
            .Where(r => r.CombinedScore >= _config.ScoreThreshold)
            .OrderByDescending(r => r.CombinedScore)
            .Take(_config.MaxResults)
            .ToList();

        _logger.LogInformation("Hybrid search found {ResultCount} results for query: '{Query}' using {Strategy} reranking", 
            finalResults.Count, query, _config.RerankingStrategy);
        
        return finalResults;
    }

    public async Task IndexDocumentsAsync(IEnumerable<Document> documents, CancellationToken cancellationToken = default)
    {
        var documentList = documents.ToList();
        _logger.LogInformation("Indexing {DocumentCount} documents for hybrid search", documentList.Count);

        // Index for HyDE search first (this also updates the document store)
        await _hydeSearchService.IndexDocumentsAsync(documentList, cancellationToken);

        // Index for BM25 search
        if (_config.EnableBM25)
        {
            await _bm25Service.IndexDocumentsAsync(documentList, cancellationToken);
        }

        _logger.LogInformation("Successfully indexed {DocumentCount} documents for hybrid search", documentList.Count);
    }

    public Task<int> GetIndexedDocumentCountAsync(CancellationToken cancellationToken = default)
    {
        return _documentStore.GetDocumentCountAsync(cancellationToken);
    }

    public Task ClearIndexAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Clearing hybrid search index");
        return _hydeSearchService.ClearIndexAsync(cancellationToken);
    }

    /// <summary>
    /// Apply the configured reranking strategy to combine BM25 and HyDE results
    /// </summary>
    private IEnumerable<HybridSearchResult> ApplyRerankingStrategy(
        List<BM25Result> bm25Results, 
        List<SearchResult> hydeResults)
    {
        return _config.RerankingStrategy switch
        {
            RerankingStrategy.WeightedSum => ApplyWeightedSum(bm25Results, hydeResults),
            RerankingStrategy.ReciprocalRankFusion => ApplyReciprocalRankFusion(bm25Results, hydeResults),
            RerankingStrategy.CombSum => ApplyCombSum(bm25Results, hydeResults),
            RerankingStrategy.CombMax => ApplyCombMax(bm25Results, hydeResults),
            RerankingStrategy.BordaCount => ApplyBordaCount(bm25Results, hydeResults),
            _ => ApplyWeightedSum(bm25Results, hydeResults)
        };
    }

    /// <summary>
    /// Traditional weighted sum approach (current implementation)
    /// </summary>
    private IEnumerable<HybridSearchResult> ApplyWeightedSum(
        List<BM25Result> bm25Results, 
        List<SearchResult> hydeResults)
    {
        var results = new Dictionary<string, HybridSearchResult>();

        // Normalize scores
        var normalizedBM25Scores = NormalizeScores(bm25Results.Select(r => r.Score).ToList());
        var normalizedHydeScores = NormalizeScores(hydeResults.Select(r => r.Similarity).ToList());

        // Combine BM25 results
        for (int i = 0; i < bm25Results.Count; i++)
        {
            var result = bm25Results[i];
            var normalizedScore = normalizedBM25Scores.Count > i ? normalizedBM25Scores[i] : 0f;
            
            results[result.Document.Id] = new HybridSearchResult
            {
                Document = result.Document,
                BM25Score = result.Score,
                NormalizedBM25Score = normalizedScore,
                CombinedScore = normalizedScore * _config.BM25Weight
            };
        }

        // Combine HyDE results
        for (int i = 0; i < hydeResults.Count; i++)
        {
            var result = hydeResults[i];
            var normalizedScore = normalizedHydeScores.Count > i ? normalizedHydeScores[i] : 0f;
            
            if (results.TryGetValue(result.Document.Id, out var existingResult))
            {
                // Update existing result
                results[result.Document.Id] = new HybridSearchResult
                {
                    Document = existingResult.Document,
                    BM25Score = existingResult.BM25Score,
                    NormalizedBM25Score = existingResult.NormalizedBM25Score,
                    HydeScore = result.Similarity,
                    TraditionalSimilarity = result.TraditionalSimilarity,
                    HypotheticalDocument = result.HypotheticalDocument,
                    NormalizedHydeScore = normalizedScore,
                    CombinedScore = existingResult.CombinedScore + (normalizedScore * _config.HydeWeight)
                };
            }
            else
            {
                // Create new result
                results[result.Document.Id] = new HybridSearchResult
                {
                    Document = result.Document,
                    HydeScore = result.Similarity,
                    TraditionalSimilarity = result.TraditionalSimilarity,
                    HypotheticalDocument = result.HypotheticalDocument,
                    NormalizedHydeScore = normalizedScore,
                    CombinedScore = normalizedScore * _config.HydeWeight
                };
            }
        }

        return results.Values;
    }

    /// <summary>
    /// Reciprocal Rank Fusion (RRF) - considers rank positions rather than just scores
    /// Formula: RRF(d) = Σ(1 / (k + rank(d))) where k is a parameter (typically 60)
    /// </summary>
    private IEnumerable<HybridSearchResult> ApplyReciprocalRankFusion(
        List<BM25Result> bm25Results, 
        List<SearchResult> hydeResults)
    {
        var results = new Dictionary<string, HybridSearchResult>();

        // Create rank dictionaries
        var bm25Ranks = bm25Results
            .OrderByDescending(r => r.Score)
            .Select((r, index) => new { r.Document.Id, Rank = index + 1 })
            .ToDictionary(x => x.Id, x => x.Rank);

        var hydeRanks = hydeResults
            .OrderByDescending(r => r.Similarity)
            .Select((r, index) => new { r.Document.Id, Rank = index + 1 })
            .ToDictionary(x => x.Id, x => x.Rank);

        // Get all unique document IDs
        var allDocumentIds = bm25Results.Select(r => r.Document.Id)
            .Union(hydeResults.Select(r => r.Document.Id))
            .Distinct();

        foreach (var docId in allDocumentIds)
        {
            float rrfScore = 0f;
            
            // Add BM25 RRF contribution
            if (bm25Ranks.TryGetValue(docId, out var bm25Rank))
            {
                rrfScore += _config.BM25Weight / (_config.RrfK + bm25Rank);
            }
            
            // Add HyDE RRF contribution
            if (hydeRanks.TryGetValue(docId, out var hydeRank))
            {
                rrfScore += _config.HydeWeight / (_config.RrfK + hydeRank);
            }

            // Find the document and create result
            var bm25Result = bm25Results.FirstOrDefault(r => r.Document.Id == docId);
            var hydeResult = hydeResults.FirstOrDefault(r => r.Document.Id == docId);
            var document = bm25Result?.Document ?? hydeResult?.Document;

            if (document != null)
            {
                results[docId] = new HybridSearchResult
                {
                    Document = document,
                    BM25Score = bm25Result?.Score,
                    HydeScore = hydeResult?.Similarity,
                    TraditionalSimilarity = hydeResult?.TraditionalSimilarity,
                    HypotheticalDocument = hydeResult?.HypotheticalDocument,
                    CombinedScore = rrfScore
                };
            }
        }

        return results.Values;
    }

    /// <summary>
    /// CombSum - Sum of normalized scores from different systems
    /// </summary>
    private IEnumerable<HybridSearchResult> ApplyCombSum(
        List<BM25Result> bm25Results, 
        List<SearchResult> hydeResults)
    {
        var results = new Dictionary<string, HybridSearchResult>();

        // Normalize scores
        var normalizedBM25Scores = NormalizeScores(bm25Results.Select(r => r.Score).ToList());
        var normalizedHydeScores = NormalizeScores(hydeResults.Select(r => r.Similarity).ToList());

        // Create lookup dictionaries
        var bm25Lookup = bm25Results
            .Select((r, index) => new { r.Document, Score = normalizedBM25Scores.Count > index ? normalizedBM25Scores[index] : 0f, OriginalScore = r.Score })
            .ToDictionary(x => x.Document.Id, x => x);

        var hydeLookup = hydeResults
            .Select((r, index) => new { r.Document, Score = normalizedHydeScores.Count > index ? normalizedHydeScores[index] : 0f, Result = r })
            .ToDictionary(x => x.Document.Id, x => x);

        // Get all unique document IDs
        var allDocumentIds = bm25Results.Select(r => r.Document.Id)
            .Union(hydeResults.Select(r => r.Document.Id))
            .Distinct();

        foreach (var docId in allDocumentIds)
        {
            var bm25Score = bm25Lookup.TryGetValue(docId, out var bm25) ? bm25.Score : 0f;
            var hydeScore = hydeLookup.TryGetValue(docId, out var hyde) ? hyde.Score : 0f;
            
            var document = bm25?.Document ?? hyde?.Document;
            if (document != null)
            {
                results[docId] = new HybridSearchResult
                {
                    Document = document,
                    BM25Score = bm25?.OriginalScore,
                    HydeScore = hyde?.Result.Similarity,
                    TraditionalSimilarity = hyde?.Result.TraditionalSimilarity,
                    HypotheticalDocument = hyde?.Result.HypotheticalDocument,
                    NormalizedBM25Score = bm25Score,
                    NormalizedHydeScore = hydeScore,
                    CombinedScore = bm25Score + hydeScore // Simple sum
                };
            }
        }

        return results.Values;
    }

    /// <summary>
    /// CombMax - Maximum of normalized scores from different systems
    /// </summary>
    private IEnumerable<HybridSearchResult> ApplyCombMax(
        List<BM25Result> bm25Results, 
        List<SearchResult> hydeResults)
    {
        var results = new Dictionary<string, HybridSearchResult>();

        // Normalize scores
        var normalizedBM25Scores = NormalizeScores(bm25Results.Select(r => r.Score).ToList());
        var normalizedHydeScores = NormalizeScores(hydeResults.Select(r => r.Similarity).ToList());

        // Create lookup dictionaries
        var bm25Lookup = bm25Results
            .Select((r, index) => new { r.Document, Score = normalizedBM25Scores.Count > index ? normalizedBM25Scores[index] : 0f, OriginalScore = r.Score })
            .ToDictionary(x => x.Document.Id, x => x);

        var hydeLookup = hydeResults
            .Select((r, index) => new { r.Document, Score = normalizedHydeScores.Count > index ? normalizedHydeScores[index] : 0f, Result = r })
            .ToDictionary(x => x.Document.Id, x => x);

        // Get all unique document IDs
        var allDocumentIds = bm25Results.Select(r => r.Document.Id)
            .Union(hydeResults.Select(r => r.Document.Id))
            .Distinct();

        foreach (var docId in allDocumentIds)
        {
            var bm25Score = bm25Lookup.TryGetValue(docId, out var bm25) ? bm25.Score : 0f;
            var hydeScore = hydeLookup.TryGetValue(docId, out var hyde) ? hyde.Score : 0f;
            
            var document = bm25?.Document ?? hyde?.Document;
            if (document != null)
            {
                results[docId] = new HybridSearchResult
                {
                    Document = document,
                    BM25Score = bm25?.OriginalScore,
                    HydeScore = hyde?.Result.Similarity,
                    TraditionalSimilarity = hyde?.Result.TraditionalSimilarity,
                    HypotheticalDocument = hyde?.Result.HypotheticalDocument,
                    NormalizedBM25Score = bm25Score,
                    NormalizedHydeScore = hydeScore,
                    CombinedScore = Math.Max(bm25Score, hydeScore) // Maximum score
                };
            }
        }

        return results.Values;
    }

    /// <summary>
    /// Borda Count - Rank-based voting method
    /// Each system votes for documents based on their rank position
    /// </summary>
    private IEnumerable<HybridSearchResult> ApplyBordaCount(
        List<BM25Result> bm25Results, 
        List<SearchResult> hydeResults)
    {
        var results = new Dictionary<string, HybridSearchResult>();

        // Create rank dictionaries (higher rank = better position)
        var totalDocs = Math.Max(bm25Results.Count, hydeResults.Count);
        
        var bm25Ranks = bm25Results
            .OrderByDescending(r => r.Score)
            .Select((r, index) => new { r.Document.Id, Score = totalDocs - index, OriginalScore = r.Score, Document = r.Document })
            .ToDictionary(x => x.Id, x => x);

        var hydeRanks = hydeResults
            .OrderByDescending(r => r.Similarity)
            .Select((r, index) => new { r.Document.Id, Score = totalDocs - index, Result = r })
            .ToDictionary(x => x.Id, x => x);

        // Get all unique document IDs
        var allDocumentIds = bm25Results.Select(r => r.Document.Id)
            .Union(hydeResults.Select(r => r.Document.Id))
            .Distinct();

        foreach (var docId in allDocumentIds)
        {
            var bm25Score = bm25Ranks.TryGetValue(docId, out var bm25) ? bm25.Score * _config.BM25Weight : 0f;
            var hydeScore = hydeRanks.TryGetValue(docId, out var hyde) ? hyde.Score * _config.HydeWeight : 0f;
            
            var document = bm25?.Document ?? hyde?.Result.Document;
            if (document != null)
            {
                results[docId] = new HybridSearchResult
                {
                    Document = document,
                    BM25Score = bm25?.OriginalScore,
                    HydeScore = hyde?.Result.Similarity,
                    TraditionalSimilarity = hyde?.Result.TraditionalSimilarity,
                    HypotheticalDocument = hyde?.Result.HypotheticalDocument,
                    CombinedScore = bm25Score + hydeScore // Weighted sum of rank scores
                };
            }
        }

        return results.Values;
    }

    private List<float> NormalizeScores(IList<float> scores)
    {
        if (!scores.Any())
        {
            return [];
        }

        return _config.NormalizationStrategy switch
        {
            ScoreNormalizationStrategy.MinMax => NormalizeMinMax(scores),
            ScoreNormalizationStrategy.ZScore => NormalizeZScore(scores),
            ScoreNormalizationStrategy.None => scores.ToList(),
            _ => scores.ToList()
        };
    }

    private static List<float> NormalizeMinMax(IList<float> scores)
    {
        if (!scores.Any())
        {
            return [];
        }

        var min = scores.Min();
        var max = scores.Max();
        
        if (Math.Abs(max - min) < float.Epsilon)
        {
            // All scores are the same, normalize to 1.0
            return scores.Select(_ => 1.0f).ToList();
        }

        return scores.Select(score => (score - min) / (max - min)).ToList();
    }

    private static List<float> NormalizeZScore(IList<float> scores)
    {
        if (!scores.Any())
        {
            return [];
        }

        var mean = scores.Average();
        var variance = scores.Sum(score => MathF.Pow(score - mean, 2)) / scores.Count;
        var stdDev = MathF.Sqrt(variance);

        if (stdDev < float.Epsilon)
        {
            // All scores are the same, normalize to 0.0
            return scores.Select(_ => 0.0f).ToList();
        }

        return scores.Select(score => (score - mean) / stdDev).ToList();
    }
}
