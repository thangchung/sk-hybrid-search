namespace HydeSearch.Models;

public record SearchRequest(string Query, int? MaxResults = 10);

public record SearchResponse(
    string Query,
    IEnumerable<SearchResult> Results,
    int TotalResults,
    long ProcessingTimeMs
);

public record HybridSearchRequest(string Query, int? MaxResults = 10);

public record HybridSearchResponse(
    string Query,
    IEnumerable<HybridSearchResult> Results,
    int TotalResults,
    long ProcessingTimeMs,
    SearchMetrics Metrics
);

public record SearchMetrics(
    bool BM25Enabled,
    bool HydeEnabled,
    int BM25ResultCount,
    int HydeResultCount,
    float BM25Weight,
    float HydeWeight
);
