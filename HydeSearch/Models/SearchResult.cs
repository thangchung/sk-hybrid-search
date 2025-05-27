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

/// <summary>
/// Represents a hybrid search result combining BM25 and HyDE scores
/// </summary>
public class HybridSearchResult
{
    public required Document Document { get; init; }
    
    /// <summary>Final combined score from BM25 and HyDE</summary>
    public float CombinedScore { get; init; }
    
    /// <summary>BM25 keyword search score</summary>
    public float? BM25Score { get; init; }
    
    /// <summary>HyDE semantic search score</summary>
    public float? HydeScore { get; init; }
    
    /// <summary>Traditional query-document similarity score</summary>
    public float? TraditionalSimilarity { get; init; }
    
    /// <summary>Generated hypothetical document used in HyDE search</summary>
    public string? HypotheticalDocument { get; init; }
    
    /// <summary>Normalized BM25 score used in final combination</summary>
    public float? NormalizedBM25Score { get; init; }
    
    /// <summary>Normalized HyDE score used in final combination</summary>
    public float? NormalizedHydeScore { get; init; }
}
