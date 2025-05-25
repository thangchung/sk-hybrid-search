namespace HydeSearch.Models;

/// <summary>
/// Represents a search result from HyDE search
/// </summary>
public class SearchResult
{
    public required Document Document { get; init; }
    public float Similarity { get; init; }
    public float? HydeSimilarity { get; init; }
    public float? TraditionalSimilarity { get; init; }
    public string? HypotheticalDocument { get; init; }
}
