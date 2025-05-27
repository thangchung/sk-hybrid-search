using HydeSearch.Configuration;
using HydeSearch.Models;

using Microsoft.Extensions.Options;

using Nest;

namespace HydeSearch.Services;

/// <summary>
/// ElasticSearch implementation of BM25 keyword search
/// </summary>
public class ElasticSearchBM25Service : IBM25Service
{
	private readonly IElasticClient _client;
	private readonly ElasticSearchConfiguration _config;
	private readonly ILogger<ElasticSearchBM25Service> _logger;
	private readonly string _indexName;

	public ElasticSearchBM25Service(
		IElasticClient client,
		IOptions<BM25Configuration> bm25Config,
		ILogger<ElasticSearchBM25Service> logger)
	{
		_client = client;
		_config = bm25Config.Value.ElasticSearch;
		_logger = logger;
		_indexName = _config.IndexName.ToLowerInvariant();
	}

	public async Task IndexDocumentsAsync(IEnumerable<Document> documents, CancellationToken cancellationToken = default)
	{
		var documentList = documents.ToList();
		_logger.LogInformation("Indexing {DocumentCount} documents to ElasticSearch index: {IndexName}",
			documentList.Count, _indexName);

		try
		{
			// Create index if it doesn't exist
			await EnsureIndexExistsAsync(cancellationToken);

			// Clear existing documents
			var deleteResponse = await _client.DeleteByQueryAsync<ElasticSearchDocument>(d => d
				.Index(_indexName)
				.Query(q => q.MatchAll()),
				cancellationToken);

			if (!deleteResponse.IsValid)
			{
				_logger.LogWarning("Failed to clear existing documents: {Error}", deleteResponse.DebugInformation);
			}

			// Prepare documents for indexing
			var elasticDocs = documentList.Select(doc => new ElasticSearchDocument
			{
				Id = doc.Id,
				Title = doc.Title,
				Content = doc.Content,
				Metadata = doc.Metadata,
				SearchableText = $"{doc.Title} {doc.Content}".Trim()
			}).ToList();

			var bulkResponse = await _client.BulkAsync(b =>
			{
				foreach (var doc in documents)
				{
					b.Index<Document>(i => i
						.Id(doc.Id)
						.Document(doc)
					);
				}
				return b;
			});

			// Check for real failures (not the 201 "errors" that aren't actually errors)
			if (bulkResponse.Errors && bulkResponse.ItemsWithErrors.Any(i => i.Status != 201))
			{
				throw new InvalidOperationException($"Failed to index documents: {bulkResponse.DebugInformation}");
			}

			// Refresh index to make documents searchable immediately
			await _client.Indices.RefreshAsync(_indexName, ct: cancellationToken);

			_logger.LogInformation("Successfully indexed {DocumentCount} documents to ElasticSearch", documentList.Count);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error indexing documents to ElasticSearch");
			throw;
		}
	}

	public async Task<IEnumerable<BM25Result>> CalculateScoresAsync(string query, IEnumerable<Document> documents, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(query))
		{
			return [];
		}

		_logger.LogDebug("Executing BM25 search in ElasticSearch for query: '{Query}'", query);

		try
		{
			// Get document IDs to filter results
			var documentIds = documents.Select(d => d.Id).ToHashSet();            // Execute search with BM25 scoring
			var searchResponse = await _client.SearchAsync<ElasticSearchDocument>(s => s
				.Index(_indexName)
				.Query(q => q
					.Bool(b => b
						.Must(m => m
							.MultiMatch(mm => mm
								.Fields(f => f
									.Field(fd => fd.Title, 2.0) // Boost title matches
									.Field(fd => fd.Content)
									.Field(fd => fd.SearchableText))
								.Query(query)
								.Type(TextQueryType.BestFields)
								.Operator(Operator.Or)))
						.Filter(f => f
							.Terms(t => t
								.Field(fd => fd.Id)
								.Terms(documentIds)))))
				.Size(1000) // Get more results to ensure we capture all relevant documents
				.TrackScores(true),
				cancellationToken);

			if (!searchResponse.IsValid)
			{
				_logger.LogWarning("ElasticSearch query failed: {Error}", searchResponse.DebugInformation);
				return [];
			}

			// Convert ElasticSearch results to BM25Results
			var results = searchResponse.Documents
				.Where(doc => documentIds.Contains(doc.Id))
				.Select(doc => new BM25Result
				{
					Document = new Document
					{
						Id = doc.Id,
						Title = doc.Title,
						Content = doc.Content,
						Metadata = doc.Metadata ?? new Dictionary<string, object>()
					},
					Score = (float)(searchResponse.Hits.FirstOrDefault(h => h.Id == doc.Id)?.Score ?? 0.0)
				})
				.Where(r => r.Score > 0)
				.OrderByDescending(r => r.Score)
				.ToList();

			_logger.LogDebug("ElasticSearch BM25 search returned {ResultCount} results for query: '{Query}'",
				results.Count, query);

			return results;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error executing BM25 search in ElasticSearch");
			return [];
		}
	}

	private async Task EnsureIndexExistsAsync(CancellationToken cancellationToken)
	{
		var indexExists = await _client.Indices.ExistsAsync(_indexName, ct: cancellationToken);

		if (!indexExists.Exists)
		{
			_logger.LogInformation("Creating ElasticSearch index: {IndexName}", _indexName);

			var createResponse = await _client.Indices.CreateAsync(_indexName, c => c
				.Settings(s => s
					.Analysis(a => a
						.Analyzers(an => an
							.Standard("standard_analyzer", sa => sa
								.StopWords("_english_")))))
				.Map<ElasticSearchDocument>(m => m
					.Properties(p => p
						.Keyword(k => k.Name(n => n.Id))
						.Text(t => t
							.Name(n => n.Title)
							.Analyzer("standard_analyzer")
							.Fields(f => f
								.Keyword(k => k.Name("keyword"))))
						.Text(t => t
							.Name(n => n.Content)
							.Analyzer("standard_analyzer"))
						.Text(t => t
							.Name(n => n.SearchableText)
							.Analyzer("standard_analyzer"))
						.Object<Dictionary<string, object>>(o => o
							.Name(n => n.Metadata)
							.Enabled(false)))), // Store but don't index metadata
				cancellationToken);

			if (!createResponse.IsValid)
			{
				_logger.LogError("Failed to create ElasticSearch index: {Error}", createResponse.DebugInformation);
				throw new InvalidOperationException($"Failed to create index: {createResponse.DebugInformation}");
			}

			_logger.LogInformation("Successfully created ElasticSearch index: {IndexName}", _indexName);
		}
	}
}

/// <summary>
/// ElasticSearch document model for indexing
/// </summary>
public class ElasticSearchDocument
{
	public required string Id { get; set; }
	public required string Title { get; set; }
	public required string Content { get; set; }
	public required string SearchableText { get; set; }
	public Dictionary<string, object>? Metadata { get; set; }
}
