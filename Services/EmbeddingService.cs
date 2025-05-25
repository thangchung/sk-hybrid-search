using System.Text.Json;
using HydeSearch.Configuration;
using Microsoft.Extensions.Options;

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
/// OpenAI-based embedding service implementation
/// </summary>
public class OpenAiEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiConfiguration _config;

    public OpenAiEmbeddingService(HttpClient httpClient, IOptions<OpenAiConfiguration> config)
    {
        _httpClient = httpClient;
        _config = config.Value;
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.ApiKey);
    }

    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var embeddings = await GetEmbeddingsAsync([text], cancellationToken);
        return embeddings[0];
    }

    public async Task<float[][]> GetEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        var requestData = new
        {
            model = _config.EmbeddingModel,
            input = texts.ToArray()
        };

        var json = JsonSerializer.Serialize(requestData);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_config.BaseUrl}/embeddings", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var embeddingResponse = JsonSerializer.Deserialize<EmbeddingResponse>(responseJson)!;

        return embeddingResponse.Data
            .OrderBy(d => d.Index)
            .Select(d => d.Embedding)
            .ToArray();
    }

    private class EmbeddingResponse
    {
        public EmbeddingData[] Data { get; set; } = [];
    }

    private class EmbeddingData
    {
        public int Index { get; set; }
        public float[] Embedding { get; set; } = [];
    }
}

/// <summary>
/// Mock embedding service for testing without OpenAI API
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
