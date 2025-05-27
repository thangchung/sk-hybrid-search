using HydeSearch.Models;
using System.Collections.Concurrent;

namespace HydeSearch.Services;

/// <summary>
/// Interface for document storage and retrieval
/// </summary>
public interface IDocumentStore
{
    Task AddDocumentAsync(Document document, CancellationToken cancellationToken = default);
    Task AddDocumentsAsync(IEnumerable<Document> documents, CancellationToken cancellationToken = default);
    Task<Document?> GetDocumentAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Document>> GetAllDocumentsAsync(CancellationToken cancellationToken = default);
    Task<bool> DeleteDocumentAsync(string id, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
    Task<int> GetDocumentCountAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory document store implementation
/// </summary>
public class InMemoryDocumentStore : IDocumentStore
{
    private readonly ConcurrentDictionary<string, Document> _documents = new();

    public Task AddDocumentAsync(Document document, CancellationToken cancellationToken = default)
    {
        _documents.AddOrUpdate(document.Id, document, (_, _) => document);
        return Task.CompletedTask;
    }

    public async Task AddDocumentsAsync(IEnumerable<Document> documents, CancellationToken cancellationToken = default)
    {
        foreach (var document in documents)
        {
            await AddDocumentAsync(document, cancellationToken);
        }
    }

    public Task<Document?> GetDocumentAsync(string id, CancellationToken cancellationToken = default)
    {
        _documents.TryGetValue(id, out var document);
        return Task.FromResult(document);
    }

    public Task<IEnumerable<Document>> GetAllDocumentsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Document>>(_documents.Values.ToList());
    }

    public Task<bool> DeleteDocumentAsync(string id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_documents.TryRemove(id, out _));
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _documents.Clear();
        return Task.CompletedTask;
    }

    public Task<int> GetDocumentCountAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_documents.Count);
    }
}
