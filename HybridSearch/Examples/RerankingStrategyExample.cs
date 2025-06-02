using HydeSearch.Configuration;
using HydeSearch.Models;
using HydeSearch.Services;

namespace HydeSearch.Examples;

/// <summary>
/// Example demonstrating different reranking strategies
/// </summary>
public static class RerankingStrategyExample
{
    /// <summary>
    /// Compare search results using different reranking strategies
    /// </summary>
    public static async Task<Dictionary<RerankingStrategy, IEnumerable<HybridSearchResult>>> CompareRerankingStrategiesAsync(
        IHybridSearchService hybridSearchService, 
        string query)
    {
        var results = new Dictionary<RerankingStrategy, IEnumerable<HybridSearchResult>>();
        
        // Test each reranking strategy
        var strategies = Enum.GetValues<RerankingStrategy>();
        
        foreach (var strategy in strategies)
        {
            Console.WriteLine($"\n=== Testing {strategy} Strategy ===");
            
            // Note: In a real implementation, you would need to modify the configuration
            // This is a conceptual example showing how different strategies would work
            var searchResults = await hybridSearchService.SearchAsync(query);
            results[strategy] = searchResults;
            
            Console.WriteLine($"Found {searchResults.Count()} results:");
            foreach (var result in searchResults.Take(5))
            {
                Console.WriteLine($"  - {result.Document.Title} (Score: {result.CombinedScore:F4})");
            }
        }
        
        return results;
    }
    
    /// <summary>
    /// Analyze the differences between reranking strategies
    /// </summary>
    public static void AnalyzeRerankingDifferences(
        Dictionary<RerankingStrategy, IEnumerable<HybridSearchResult>> strategyResults)
    {
        Console.WriteLine("\n=== Reranking Strategy Analysis ===");
        
        foreach (var (strategy, results) in strategyResults)
        {
            var resultList = results.ToList();
            var topResult = resultList.FirstOrDefault();
            
            Console.WriteLine($"\n{strategy}:");
            Console.WriteLine($"  Top result: {topResult?.Document.Title ?? "None"}");
            Console.WriteLine($"  Top score: {topResult?.CombinedScore:F4}");
            Console.WriteLine($"  Score range: {resultList.MinBy(r => r.CombinedScore)?.CombinedScore:F4} - {resultList.MaxBy(r => r.CombinedScore)?.CombinedScore:F4}");
        }
        
        // Find documents that rank differently across strategies
        var allDocuments = strategyResults.Values
            .SelectMany(results => results)
            .Select(r => r.Document.Id)
            .Distinct()
            .ToList();
            
        Console.WriteLine($"\n=== Ranking Variations ===");
        foreach (var docId in allDocuments.Take(5))
        {
            Console.WriteLine($"\nDocument {docId}:");
            foreach (var (strategy, results) in strategyResults)
            {
                var result = results.FirstOrDefault(r => r.Document.Id == docId);
                if (result != null)
                {
                    var rank = results.ToList().IndexOf(result) + 1;
                    Console.WriteLine($"  {strategy}: Rank {rank}, Score {result.CombinedScore:F4}");
                }
            }
        }
    }
    
    /// <summary>
    /// Create sample documents for testing reranking strategies
    /// </summary>
    public static List<Document> CreateSampleDocuments()
    {
        return new List<Document>
        {
            new() { Id = "1", Title = "Machine Learning Fundamentals", Content = "Introduction to machine learning algorithms, supervised and unsupervised learning techniques." },
            new() { Id = "2", Title = "Deep Learning with Neural Networks", Content = "Advanced neural network architectures including CNNs, RNNs, and transformers for deep learning." },
            new() { Id = "3", Title = "Data Science Best Practices", Content = "Best practices for data science projects, including data cleaning, feature engineering, and model evaluation." },
            new() { Id = "4", Title = "Python Programming Guide", Content = "Comprehensive guide to Python programming for data science and machine learning applications." },
            new() { Id = "5", Title = "Statistical Analysis Methods", Content = "Statistical methods for data analysis, hypothesis testing, and experimental design." },
            new() { Id = "6", Title = "AI Ethics and Responsible AI", Content = "Ethical considerations in artificial intelligence development and deployment." },
            new() { Id = "7", Title = "Computer Vision Applications", Content = "Computer vision techniques for image recognition, object detection, and image processing." },
            new() { Id = "8", Title = "Natural Language Processing", Content = "NLP techniques for text analysis, sentiment analysis, and language understanding." },
            new() { Id = "9", Title = "Reinforcement Learning", Content = "Reinforcement learning algorithms and applications in game playing and robotics." },
            new() { Id = "10", Title = "Big Data Technologies", Content = "Technologies for processing and analyzing large-scale data including Hadoop and Spark." }
        };
    }
}
