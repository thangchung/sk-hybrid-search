using HydeSearch.Models;
using HydeSearch.Services;

namespace HydeSearch.Examples;

/// <summary>
/// Mock embedding service for testing without OpenAI API
/// </summary>
public class MockEmbeddingService : IEmbeddingService
{
    private readonly Random _random = new(42); // Deterministic for testing

    public Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        // Generate a mock embedding that has some relation to text content
        var words = text.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2) // Filter out short words
            .Take(20) // Take first 20 meaningful words
            .ToArray();
        
        var embedding = new float[384]; // Smaller dimension for faster processing
        
        // Initialize with small random values
        var baseRandom = new Random(42);
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = (float)(baseRandom.NextDouble() * 0.1 - 0.05); // Small base noise
        }
        
        // Add patterns based on words in the text
        foreach (var word in words)
        {
            var wordHash = word.GetHashCode();
            var wordRandom = new Random(Math.Abs(wordHash));
            
            // Add word-specific patterns to the embedding
            for (int i = 0; i < Math.Min(embedding.Length, 50); i++)
            {
                var index = (Math.Abs(wordHash) + i) % embedding.Length;
                embedding[index] += (float)(wordRandom.NextDouble() * 0.3);
            }
        }
        
        // Normalize the vector to unit length
        var magnitude = Math.Sqrt(embedding.Sum(x => x * x));
        if (magnitude > 0)
        {
            for (int i = 0; i < embedding.Length; i++)
            {
                embedding[i] /= (float)magnitude;
            }
        }
        
        return Task.FromResult(embedding);
    }

    public async Task<float[][]> GetEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        var results = new List<float[]>();
        foreach (var text in texts)
        {
            results.Add(await GetEmbeddingAsync(text, cancellationToken));
        }
        return results.ToArray();
    }
}

/// <summary>
/// Mock hypothetical document generator for testing
/// </summary>
public class MockHypotheticalDocumentGenerator : IHypotheticalDocumentGenerator
{
    public Task<string> GenerateHypotheticalDocumentAsync(string query, CancellationToken cancellationToken = default)
    {
        // Generate a simple hypothetical document based on the query
        var hypothetical = $"This is a comprehensive explanation about {query.ToLower()}. " +
                          $"The topic involves various aspects and considerations related to {query.ToLower()}. " +
                          $"Key concepts include the fundamental principles and practical applications of {query.ToLower()}.";
        
        return Task.FromResult(hypothetical);
    }
}
