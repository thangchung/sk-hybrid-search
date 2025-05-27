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
