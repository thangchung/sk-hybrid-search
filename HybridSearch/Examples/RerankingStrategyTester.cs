using HydeSearch.Configuration;
using HydeSearch.Examples;
using HydeSearch.Models;
using HydeSearch.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HydeSearch.Testing;

/// <summary>
/// Console application to test different reranking strategies
/// </summary>
public class RerankingStrategyTester
{
    public static async Task TestStrategiesAsync()
    {
        // Create a host with services
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Configure hybrid search with default settings
                services.Configure<HybridSearchConfiguration>(options =>
                {
                    options.BM25Weight = 0.3f;
                    options.HydeWeight = 0.7f;
                    options.MaxResults = 10;
                    options.ScoreThreshold = 0.01f;
                    options.EnableBM25 = true;
                    options.EnableHyDE = true;
                    options.NormalizationStrategy = ScoreNormalizationStrategy.MinMax;
                    options.RerankingStrategy = RerankingStrategy.WeightedSum;
                    options.RrfK = 60.0f;
                });
                
                // Register services with mock implementations for testing
                services.AddSingleton<IDocumentStore, MockDocumentStore>();
                services.AddSingleton<IBM25Service, MockBM25Service>();
                services.AddSingleton<IHydeSearchService, MockHydeSearchService>();
                services.AddSingleton<IHybridSearchService, HybridSearchService>();
            })
            .Build();
            
        var hybridSearchService = host.Services.GetRequiredService<IHybridSearchService>();
        var logger = host.Services.GetRequiredService<ILogger<RerankingStrategyTester>>();
        
        // Index sample documents
        var sampleDocuments = RerankingStrategyExample.CreateSampleDocuments();
        await hybridSearchService.IndexDocumentsAsync(sampleDocuments);
        
        Console.WriteLine("=== Reranking Strategy Comparison Test ===");
        Console.WriteLine($"Indexed {sampleDocuments.Count} documents");
        
        // Test query
        var query = "machine learning algorithms";
        Console.WriteLine($"Query: '{query}'\n");
        
        // Test each strategy manually by updating configuration
        var strategies = new[]
        {
            RerankingStrategy.WeightedSum,
            RerankingStrategy.ReciprocalRankFusion,
            RerankingStrategy.CombSum,
            RerankingStrategy.CombMax,
            RerankingStrategy.BordaCount
        };
        
        var allResults = new Dictionary<RerankingStrategy, List<HybridSearchResult>>();
        
        foreach (var strategy in strategies)
        {
            Console.WriteLine($"\n=== {strategy} Strategy ===");
            
            // Update configuration for this strategy
            var config = host.Services.GetRequiredService<IOptions<HybridSearchConfiguration>>();
            var configValue = config.Value;
            
            // Create a new service instance with updated strategy
            // Note: In practice, you would configure this through appsettings.json
            var testConfig = new HybridSearchConfiguration
            {
                BM25Weight = configValue.BM25Weight,
                HydeWeight = configValue.HydeWeight,
                MaxResults = configValue.MaxResults,
                ScoreThreshold = configValue.ScoreThreshold,
                EnableBM25 = configValue.EnableBM25,
                EnableHyDE = configValue.EnableHyDE,
                NormalizationStrategy = configValue.NormalizationStrategy,
                RerankingStrategy = strategy,
                RrfK = configValue.RrfK
            };
            
            var testService = new HybridSearchService(
                host.Services.GetRequiredService<IBM25Service>(),
                host.Services.GetRequiredService<IHydeSearchService>(),
                host.Services.GetRequiredService<IDocumentStore>(),
                Options.Create(testConfig),
                host.Services.GetRequiredService<ILogger<HybridSearchService>>()
            );
            
            var results = await testService.SearchAsync(query);
            var resultList = results.ToList();
            allResults[strategy] = resultList;
            
            Console.WriteLine($"Found {resultList.Count} results:");
            for (int i = 0; i < Math.Min(5, resultList.Count); i++)
            {
                var result = resultList[i];
                Console.WriteLine($"  {i + 1}. {result.Document.Title}");
                Console.WriteLine($"     Combined: {result.CombinedScore:F4}, BM25: {result.BM25Score:F4}, HyDE: {result.HydeScore:F4}");
            }
        }
        
        // Analyze ranking differences
        Console.WriteLine("\n=== Ranking Comparison ===");
        AnalyzeRankingDifferences(allResults);
    }
    
    private static void AnalyzeRankingDifferences(Dictionary<RerankingStrategy, List<HybridSearchResult>> allResults)
    {
        // Get all unique documents
        var allDocIds = allResults.Values
            .SelectMany(results => results.Select(r => r.Document.Id))
            .Distinct()
            .ToList();
            
        Console.WriteLine($"\nRanking variations for top documents:");
        
        foreach (var docId in allDocIds.Take(5))
        {
            var doc = allResults.Values.First().First(r => r.Document.Id == docId).Document;
            Console.WriteLine($"\n'{doc.Title}' (ID: {docId}):");
            
            foreach (var (strategy, results) in allResults)
            {
                var result = results.FirstOrDefault(r => r.Document.Id == docId);
                if (result != null)
                {
                    var rank = results.IndexOf(result) + 1;
                    Console.WriteLine($"  {strategy,-20}: Rank {rank,2}, Score {result.CombinedScore:F4}");
                }
                else
                {
                    Console.WriteLine($"  {strategy,-20}: Not found");
                }
            }
        }
        
        // Show strategy performance summary
        Console.WriteLine($"\n=== Strategy Performance Summary ===");
        foreach (var (strategy, results) in allResults)
        {
            var avgScore = results.Average(r => r.CombinedScore);
            var maxScore = results.Max(r => r.CombinedScore);
            var minScore = results.Min(r => r.CombinedScore);
            
            Console.WriteLine($"{strategy,-20}: Avg={avgScore:F4}, Max={maxScore:F4}, Min={minScore:F4}, Count={results.Count}");
        }
    }
}

// Simple mock implementations for testing
public class MockDocumentStore : IDocumentStore
{
    private readonly List<Document> _documents = new();
    
    public Task AddDocumentAsync(Document document, CancellationToken cancellationToken = default)
    {
        _documents.Add(document);
        return Task.CompletedTask;
    }
    
    public Task AddDocumentsAsync(IEnumerable<Document> documents, CancellationToken cancellationToken = default)
    {
        _documents.AddRange(documents);
        return Task.CompletedTask;
    }
    
    public Task<IEnumerable<Document>> GetAllDocumentsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Document>>(_documents);
    }
    
    public Task<Document?> GetDocumentAsync(string id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_documents.FirstOrDefault(d => d.Id == id));
    }
    
    public Task<int> GetDocumentCountAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_documents.Count);
    }
    
    public Task<bool> DeleteDocumentAsync(string id, CancellationToken cancellationToken = default)
    {
        var document = _documents.FirstOrDefault(d => d.Id == id);
        if (document != null)
        {
            _documents.Remove(document);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
    
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _documents.Clear();
        return Task.CompletedTask;
    }
}

public class MockBM25Service : IBM25Service
{
    public Task<IEnumerable<BM25Result>> CalculateScoresAsync(string query, IEnumerable<Document> documents, CancellationToken cancellationToken = default)
    {
        // Simple mock BM25 scoring based on keyword matching
        var queryWords = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var results = new List<BM25Result>();
        
        foreach (var doc in documents)
        {
            var content = (doc.Title + " " + doc.Content).ToLowerInvariant();
            var score = queryWords.Sum(word => content.Contains(word) ? 1.0f : 0.0f) / queryWords.Length;
            
            if (score > 0)
            {
                results.Add(new BM25Result { Document = doc, Score = score });
            }
        }
        
        return Task.FromResult<IEnumerable<BM25Result>>(results.OrderByDescending(r => r.Score));
    }
    
    public Task IndexDocumentsAsync(IEnumerable<Document> documents, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
    
    public Task ClearIndexAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

public class MockHydeSearchService : IHydeSearchService
{
    public Task<IEnumerable<SearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        // Simple mock HyDE scoring based on semantic similarity (simulated)
        var queryWords = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var results = new List<SearchResult>();
        
        // Simulate getting documents from a store
        var documents = RerankingStrategyExample.CreateSampleDocuments();
        
        foreach (var doc in documents)
        {
            var content = (doc.Title + " " + doc.Content).ToLowerInvariant();
            
            // Simple semantic similarity simulation
            var semanticScore = queryWords.Sum(word => 
            {
                if (content.Contains(word)) return 1.0f;
                if (content.Contains("machine") && word == "ml") return 0.8f;
                if (content.Contains("learning") && word == "algorithm") return 0.6f;
                return 0.0f;
            }) / queryWords.Length;
            
            // Add some randomness to simulate semantic understanding
            semanticScore += (float)(new Random().NextDouble() * 0.2);
            
            if (semanticScore > 0)
            {
                results.Add(new SearchResult 
                { 
                    Document = doc, 
                    Similarity = semanticScore,
                    TraditionalSimilarity = semanticScore * 0.8f,
                    HypotheticalDocument = $"Generated hypothetical document for: {query}"
                });
            }
        }
        
        return Task.FromResult<IEnumerable<SearchResult>>(results.OrderByDescending(r => r.Similarity));
    }
    
    public Task IndexDocumentAsync(Document document, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
    
    public Task IndexDocumentsAsync(IEnumerable<Document> documents, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
    
    public Task<int> GetIndexedDocumentCountAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(10);
    }
    
    public Task ClearIndexAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
