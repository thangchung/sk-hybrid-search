namespace HydeSearch.Configuration;

/// <summary>
/// Configuration for AI model providers
/// </summary>
public class AiConfiguration
{
    public OpenAiConfiguration OpenAI { get; set; } = new();
    public OllamaConfiguration Ollama { get; set; } = new();
    public string DefaultProvider { get; set; } = "OpenAI";
}

/// <summary>
/// Configuration for OpenAI API integration
/// </summary>
public class OpenAiConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
    public string CompletionModel { get; set; } = "gpt-4o-mini";
    public int MaxTokens { get; set; } = 1000;
    public float Temperature { get; set; } = 0.7f;
}

/// <summary>
/// Configuration for Ollama integration
/// </summary>
public class OllamaConfiguration
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string EmbeddingModel { get; set; } = "nomic-embed-text";
    public string CompletionModel { get; set; } = "llama3.2";
    public int MaxTokens { get; set; } = 1000;
    public float Temperature { get; set; } = 0.7f;
}

/// <summary>
/// Configuration for HyDE search parameters
/// </summary>
public class HydeConfiguration
{
    public float HydeWeight { get; set; } = 0.7f;
    public float TraditionalWeight { get; set; } = 0.3f;
    public int MaxResults { get; set; } = 10;
    public float SimilarityThreshold { get; set; } = 0.1f;
    public string HydePromptTemplate { get; set; } = 
        "Write a passage that would answer this question: {query}\n\nPassage:";
}

/// <summary>
/// Configuration for hybrid search combining BM25 and HyDE
/// </summary>
public class HybridSearchConfiguration
{
    /// <summary>Weight for BM25 keyword search results (0.0 to 1.0)</summary>
    public float BM25Weight { get; set; } = 0.3f;
    
    /// <summary>Weight for HyDE semantic search results (0.0 to 1.0)</summary>
    public float HydeWeight { get; set; } = 0.7f;
    
    /// <summary>Maximum number of results to return</summary>
    public int MaxResults { get; set; } = 10;
    
    /// <summary>Minimum combined score threshold for results</summary>
    public float ScoreThreshold { get; set; } = 0.01f;
    
    /// <summary>Enable or disable BM25 keyword search component</summary>
    public bool EnableBM25 { get; set; } = true;
    
    /// <summary>Enable or disable HyDE semantic search component</summary>
    public bool EnableHyDE { get; set; } = true;
    
    /// <summary>Normalization strategy for combining scores</summary>
    public ScoreNormalizationStrategy NormalizationStrategy { get; set; } = ScoreNormalizationStrategy.MinMax;
}

/// <summary>
/// Strategies for normalizing and combining different search scores
/// </summary>
public enum ScoreNormalizationStrategy
{
    /// <summary>Min-max normalization to [0,1] range</summary>
    MinMax,
    /// <summary>Z-score normalization</summary>
    ZScore,
    /// <summary>No normalization, use raw scores</summary>
    None
}

/// <summary>
/// Configuration for ElasticSearch integration
/// </summary>
public class ElasticSearchConfiguration
{
    public string IndexName { get; set; } = "hyde-search";
}

/// <summary>
/// Configuration for BM25 search provider selection
/// </summary>
public class BM25Configuration
{
    public string Provider { get; set; } = "InMemory"; // InMemory or ElasticSearch
    public ElasticSearchConfiguration ElasticSearch { get; set; } = new();
}
