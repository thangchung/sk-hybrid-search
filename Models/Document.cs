namespace HydeSearch.Models;

/// <summary>
/// Represents a document in the search index
/// </summary>
public class Document
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Content { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
    public float[]? Embedding { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
