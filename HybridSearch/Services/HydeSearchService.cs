using HydeSearch.Configuration;
using HydeSearch.Models;
using Microsoft.Extensions.Options;

namespace HydeSearch.Services;

/// <summary>
/// Interface for HyDE (Hypothetical Document Embeddings) search
/// </summary>
public interface IHydeSearchService
{
    Task<IEnumerable<SearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default);
    Task IndexDocumentAsync(Document document, CancellationToken cancellationToken = default);
    Task IndexDocumentsAsync(IEnumerable<Document> documents, CancellationToken cancellationToken = default);
    Task<int> GetIndexedDocumentCountAsync(CancellationToken cancellationToken = default);
    Task ClearIndexAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// HyDE search service implementation that combines traditional and hypothetical document embeddings
/// </summary>
public class HydeSearchService : IHydeSearchService
{
    private readonly IDocumentStore _documentStore;
    private readonly IEmbeddingService _embeddingService;
    private readonly IHypotheticalDocumentGenerator _hydeGenerator;
    private readonly IVectorSimilarityService _similarityService;
    private readonly HydeConfiguration _config;
    private readonly ILogger<HydeSearchService> _logger;

    public HydeSearchService(
        IDocumentStore documentStore,
        IEmbeddingService embeddingService,
        IHypotheticalDocumentGenerator hydeGenerator,
        IVectorSimilarityService similarityService,
        IOptions<HydeConfiguration> config,
        ILogger<HydeSearchService> logger)
    {
        _documentStore = documentStore;
        _embeddingService = embeddingService;
        _hydeGenerator = hydeGenerator;
        _similarityService = similarityService;
        _config = config.Value;
        _logger = logger;
    }

    public async Task<IEnumerable<SearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting HyDE search for query: {Query}", query);

        var documents = await _documentStore.GetAllDocumentsAsync(cancellationToken);
        var documentList = documents.ToList();

        if (!documentList.Any())
        {
            _logger.LogWarning("No documents found in the index");
            return [];
        }

        // Generate traditional query embedding
        var queryEmbedding = await _embeddingService.GetEmbeddingAsync(query, cancellationToken);
        _logger.LogDebug("Generated query embedding with dimension: {Dimension}", queryEmbedding.Length);

        // Generate hypothetical document and its embedding
        var hypotheticalDocument = await _hydeGenerator.GenerateHypotheticalDocumentAsync(query, cancellationToken);
        var hydeEmbedding = await _embeddingService.GetEmbeddingAsync(hypotheticalDocument, cancellationToken);
        _logger.LogDebug("Generated hypothetical document: {HypotheticalDocument}", hypotheticalDocument);

        // Calculate similarities for all documents
        var results = new List<SearchResult>();
        
        foreach (var document in documentList)
        {
            if (document.Embedding == null)
            {
                _logger.LogWarning("Document {DocumentId} has no embedding, skipping", document.Id);
                continue;
            }

            // Calculate traditional similarity (query vs document)
            var traditionalSimilarity = _similarityService.CalculateCosineSimilarity(
                queryEmbedding.AsSpan(), 
                document.Embedding.AsSpan());

            // Calculate HyDE similarity (hypothetical document vs document)
            var hydeSimilarity = _similarityService.CalculateCosineSimilarity(
                hydeEmbedding.AsSpan(), 
                document.Embedding.AsSpan());

            // Combine similarities using weighted average
            var combinedSimilarity = (traditionalSimilarity * _config.TraditionalWeight) + 
                                   (hydeSimilarity * _config.HydeWeight);

            if (combinedSimilarity >= _config.SimilarityThreshold)
            {
                results.Add(new SearchResult
                {
                    Document = document,
                    Similarity = combinedSimilarity,
                    TraditionalSimilarity = traditionalSimilarity,
                    HydeSimilarity = hydeSimilarity,
                    HypotheticalDocument = hypotheticalDocument
                });
            }
        }

        // Sort by similarity and take top results
        var sortedResults = results
            .OrderByDescending(r => r.Similarity)
            .Take(_config.MaxResults)
            .ToList();

        _logger.LogInformation("Found {ResultCount} results for query: {Query}", sortedResults.Count, query);
        return sortedResults;
    }

    public async Task IndexDocumentAsync(Document document, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Indexing document: {DocumentId}", document.Id);

        // Generate embedding if not already present
        if (document.Embedding == null)
        {
            document.Embedding = await _embeddingService.GetEmbeddingAsync(document.Content, cancellationToken);
            _logger.LogDebug("Generated embedding for document {DocumentId}", document.Id);
        }

        await _documentStore.AddDocumentAsync(document, cancellationToken);
        _logger.LogInformation("Successfully indexed document: {DocumentId}", document.Id);
    }

    public async Task IndexDocumentsAsync(IEnumerable<Document> documents, CancellationToken cancellationToken = default)
    {
        var documentList = documents.ToList();
        _logger.LogInformation("Indexing {DocumentCount} documents", documentList.Count);

        // Generate embeddings for documents that don't have them
        var documentsNeedingEmbeddings = documentList.Where(d => d.Embedding == null).ToList();
        
        if (documentsNeedingEmbeddings.Any())
        {
            var texts = documentsNeedingEmbeddings.Select(d => d.Content).ToArray();
            var embeddings = await _embeddingService.GetEmbeddingsAsync(texts, cancellationToken);
            
            for (int i = 0; i < documentsNeedingEmbeddings.Count; i++)
            {
                documentsNeedingEmbeddings[i].Embedding = embeddings[i];
            }
            
            _logger.LogDebug("Generated {EmbeddingCount} embeddings", embeddings.Length);
        }

        await _documentStore.AddDocumentsAsync(documentList, cancellationToken);
        _logger.LogInformation("Successfully indexed {DocumentCount} documents", documentList.Count);
    }

    public Task<int> GetIndexedDocumentCountAsync(CancellationToken cancellationToken = default)
    {
        return _documentStore.GetDocumentCountAsync(cancellationToken);
    }

    public Task ClearIndexAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Clearing document index");
        return _documentStore.ClearAsync(cancellationToken);
    }
}
