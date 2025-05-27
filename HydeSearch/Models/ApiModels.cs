namespace HydeSearch.Models;

public record SearchRequest(string Query, int? MaxResults = 10);

public record SearchResponse(
    string Query,
    IEnumerable<SearchResult> Results,
    int TotalResults,
    long ProcessingTimeMs
);
