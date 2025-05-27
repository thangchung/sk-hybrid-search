using HydeSearch.Models;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace HydeSearch.Services;

/// <summary>
/// Interface for BM25 keyword search functionality
/// </summary>
public interface IBM25Service
{
    /// <summary>
    /// Calculate BM25 scores for documents given a query
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="documents">Documents to score</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of BM25 search results</returns>
    Task<IEnumerable<BM25Result>> CalculateScoresAsync(string query, IEnumerable<Document> documents, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Preprocess and index documents for BM25 search
    /// </summary>
    /// <param name="documents">Documents to index</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task IndexDocumentsAsync(IEnumerable<Document> documents, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a BM25 search result with score
/// </summary>
public class BM25Result
{
    public required Document Document { get; init; }
    public float Score { get; init; }
}

/// <summary>
/// In-memory BM25 implementation for keyword search
/// </summary>
public class BM25Service : IBM25Service
{
    private readonly ILogger<BM25Service> _logger;
    private readonly Dictionary<string, DocumentStats> _documentStats = new();
    private readonly Dictionary<string, float> _termDocumentFrequencies = new();
    private readonly object _lock = new();
    
    // BM25 parameters
    private const float K1 = 1.2f;  // Term frequency saturation parameter
    private const float B = 0.75f;  // Length normalization parameter
    
    private float _averageDocumentLength;
    private int _totalDocuments;

    public BM25Service(ILogger<BM25Service> logger)
    {
        _logger = logger;
    }

    public async Task IndexDocumentsAsync(IEnumerable<Document> documents, CancellationToken cancellationToken = default)
    {
        var documentList = documents.ToList();
        _logger.LogInformation("Indexing {DocumentCount} documents for BM25 search", documentList.Count);

        await Task.Run(() =>
        {
            lock (_lock)
            {
                // Clear existing stats
                _documentStats.Clear();
                _termDocumentFrequencies.Clear();
                
                var allTermCounts = new Dictionary<string, int>();
                var totalLength = 0;

                // First pass: collect document statistics
                foreach (var document in documentList)
                {
                    var text = $"{document.Title} {document.Content}";
                    var terms = TokenizeAndNormalize(text);
                    var termCounts = CountTerms(terms);
                    
                    var docLength = terms.Count;
                    totalLength += docLength;
                    
                    _documentStats[document.Id] = new DocumentStats
                    {
                        DocumentLength = docLength,
                        TermCounts = termCounts
                    };

                    // Count documents containing each term
                    foreach (var term in termCounts.Keys)
                    {
                        allTermCounts[term] = allTermCounts.GetValueOrDefault(term, 0) + 1;
                    }
                }

                _totalDocuments = documentList.Count;
                _averageDocumentLength = _totalDocuments > 0 ? (float)totalLength / _totalDocuments : 0;

                // Calculate IDF values
                foreach (var (term, documentCount) in allTermCounts)
                {
                    _termDocumentFrequencies[term] = CalculateIDF(documentCount, _totalDocuments);
                }
            }
        }, cancellationToken);

        _logger.LogInformation("Successfully indexed {DocumentCount} documents for BM25. Average document length: {AvgLength:F1}", 
            documentList.Count, _averageDocumentLength);
    }

    public async Task<IEnumerable<BM25Result>> CalculateScoresAsync(string query, IEnumerable<Document> documents, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var documentList = documents.ToList();
        if (!documentList.Any())
        {
            return [];
        }

        _logger.LogDebug("Calculating BM25 scores for query: '{Query}' across {DocumentCount} documents", query, documentList.Count);

        return await Task.Run(() =>
        {
            var queryTerms = TokenizeAndNormalize(query);
            var results = new List<BM25Result>();

            lock (_lock)
            {
                foreach (var document in documentList)
                {
                    if (!_documentStats.TryGetValue(document.Id, out var docStats))
                    {
                        // Document not indexed, skip
                        continue;
                    }

                    var score = CalculateBM25Score(queryTerms, docStats);
                    
                    if (score > 0)
                    {
                        results.Add(new BM25Result
                        {
                            Document = document,
                            Score = score
                        });
                    }
                }
            }

            var sortedResults = results.OrderByDescending(r => r.Score).ToList();
            _logger.LogDebug("BM25 search returned {ResultCount} results for query: '{Query}'", sortedResults.Count, query);
            
            return sortedResults;
        }, cancellationToken);
    }

    private static List<string> TokenizeAndNormalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        // Simple tokenization: split on non-alphanumeric, convert to lowercase, filter short terms
        var tokens = Regex.Split(text.ToLowerInvariant(), @"[^\w\d]+")
            .Where(token => token.Length >= 2)  // Filter out single character terms
            .Where(token => !IsStopWord(token))  // Filter out common stop words
            .ToList();

        return tokens;
    }

    private static Dictionary<string, int> CountTerms(IEnumerable<string> terms)
    {
        var counts = new Dictionary<string, int>();
        foreach (var term in terms)
        {
            counts[term] = counts.GetValueOrDefault(term, 0) + 1;
        }
        return counts;
    }

    private static float CalculateIDF(int documentFrequency, int totalDocuments)
    {
        if (documentFrequency == 0 || totalDocuments == 0)
        {
            return 0f;
        }

        // IDF = log((N - df + 0.5) / (df + 0.5))
        return MathF.Log((totalDocuments - documentFrequency + 0.5f) / (documentFrequency + 0.5f));
    }

    private float CalculateBM25Score(IList<string> queryTerms, DocumentStats docStats)
    {
        var score = 0f;

        foreach (var term in queryTerms.Distinct())
        {
            if (!_termDocumentFrequencies.TryGetValue(term, out var idf))
            {
                continue; // Term not in corpus
            }

            var termFrequency = docStats.TermCounts.GetValueOrDefault(term, 0);
            if (termFrequency == 0)
            {
                continue; // Term not in document
            }

            // BM25 formula
            var tfComponent = (termFrequency * (K1 + 1)) / 
                             (termFrequency + K1 * (1 - B + B * (docStats.DocumentLength / _averageDocumentLength)));
            
            score += idf * tfComponent;
        }

        return score;
    }

    private static bool IsStopWord(string term)
    {
        // Simple English stop words list
        var stopWords = new HashSet<string>
        {
            "a", "an", "and", "are", "as", "at", "be", "by", "for", "from",
            "has", "he", "in", "is", "it", "its", "of", "on", "that", "the",
            "to", "was", "will", "with", "the", "this", "but", "they", "have",
            "had", "what", "said", "each", "which", "she", "do", "how", "their",
            "if", "will", "up", "other", "about", "out", "many", "then", "them"
        };
        
        return stopWords.Contains(term);
    }

    private class DocumentStats
    {
        public required int DocumentLength { get; init; }
        public required Dictionary<string, int> TermCounts { get; init; }
    }
}
