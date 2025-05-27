using HydeSearch.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;

namespace HydeSearch.Services;

/// <summary>
/// Interface for generating text embeddings
/// </summary>
public interface IEmbeddingService
{
    Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    Task<float[][]> GetEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);
}

/// <summary>
/// Semantic Kernel-based embedding service implementation
/// </summary>
public class SemanticKernelEmbeddingService : IEmbeddingService
{
    private readonly ITextEmbeddingGenerationService _embeddingService;

    public SemanticKernelEmbeddingService(ITextEmbeddingGenerationService embeddingService)
    {
        _embeddingService = embeddingService;
    }

    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var embedding = await _embeddingService.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken);
        return embedding.ToArray();
    }

    public async Task<float[][]> GetEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        var textList = texts.ToList();
        var embeddings = await _embeddingService.GenerateEmbeddingsAsync(textList, cancellationToken: cancellationToken);
        return embeddings.Select(e => e.ToArray()).ToArray();
    }
}

/// <summary>
/// Mock embedding service for testing without AI providers
/// </summary>
public class MockEmbeddingService : IEmbeddingService
{
    private const int EmbeddingDimensions = 1536; // Same as OpenAI text-embedding-3-small

    public Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GenerateMockEmbedding(text));
    }

    public Task<float[][]> GetEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        var embeddings = texts.Select(GenerateMockEmbedding).ToArray();
        return Task.FromResult(embeddings);
    }

    private static float[] GenerateMockEmbedding(string text)
    {
        // Generate a deterministic "embedding" based on text content
        // This won't be as good as real embeddings but allows testing
        var random = new Random(text.GetHashCode());
        var embedding = new float[EmbeddingDimensions];
        
        for (int i = 0; i < EmbeddingDimensions; i++)
        {
            embedding[i] = (float)(random.NextDouble() - 0.5) * 2.0f; // Range [-1, 1]
        }
        
        // Normalize the vector
        var magnitude = Math.Sqrt(embedding.Select(x => x * x).Sum());
        if (magnitude > 0)
        {
            for (int i = 0; i < EmbeddingDimensions; i++)
            {
                embedding[i] /= (float)magnitude;
            }
        }
        
        return embedding;
    }
}
