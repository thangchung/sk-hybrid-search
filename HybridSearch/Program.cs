using HydeSearch.Configuration;
using HydeSearch.Models;
using HydeSearch.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;

using ConnectionSettings = Nest.ConnectionSettings;
using ElasticClient = Nest.ElasticClient;
using IElasticClient = Nest.IElasticClient;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
	{
		Title = "Hybrid Search API",
		Version = "v1",
		Description = "A web API for hybrid search functionality",
		Contact = new Microsoft.OpenApi.Models.OpenApiContact
		{
			Name = "Hybrid Search API"
		}
	});
});

// Configure AI, HyDE Search, Hybrid Search, and BM25 settings
builder.Services.Configure<AiConfiguration>(
	builder.Configuration.GetSection("AI"));
builder.Services.Configure<HydeConfiguration>(
	builder.Configuration.GetSection("HyDE"));
builder.Services.Configure<HybridSearchConfiguration>(
	builder.Configuration.GetSection("HybridSearch"));
builder.Services.Configure<BM25Configuration>(
	builder.Configuration.GetSection("BM25"));

// Get AI configuration
var aiConfig = builder.Configuration.GetSection("AI").Get<AiConfiguration>() ?? new AiConfiguration();
var defaultProvider = aiConfig.DefaultProvider;

// Check available providers
var hasOpenAiKey = !string.IsNullOrWhiteSpace(aiConfig.OpenAI.ApiKey);
var hasAzureOpenAiKey = !string.IsNullOrWhiteSpace(aiConfig.AzureOpenAI.ApiKey) && !string.IsNullOrWhiteSpace(aiConfig.AzureOpenAI.Endpoint);
var hasOllamaConfig = !string.IsNullOrWhiteSpace(aiConfig.Ollama.BaseUrl);

if (hasOpenAiKey && defaultProvider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
{
	// Register Semantic Kernel with OpenAI services
	builder.Services.AddTransient<Kernel>(provider =>
	{
		var kernelBuilder = Kernel.CreateBuilder();
		kernelBuilder.AddOpenAIChatCompletion(
			aiConfig.OpenAI.CompletionModel,
			aiConfig.OpenAI.ApiKey);
		kernelBuilder.AddOpenAITextEmbeddingGeneration(
			aiConfig.OpenAI.EmbeddingModel,
			aiConfig.OpenAI.ApiKey);
		return kernelBuilder.Build();
	});

	// Register Semantic Kernel services for dependency injection
	builder.Services.AddTransient<IChatCompletionService>(provider =>
		provider.GetRequiredService<Kernel>().GetRequiredService<IChatCompletionService>());
	builder.Services.AddTransient<ITextEmbeddingGenerationService>(provider =>
		provider.GetRequiredService<Kernel>().GetRequiredService<ITextEmbeddingGenerationService>());
	// Register HyDE services
	builder.Services.AddTransient<IEmbeddingService, SemanticKernelEmbeddingService>();
	builder.Services.AddTransient<IHypotheticalDocumentGenerator, SemanticKernelHypotheticalDocumentGenerator>();
}
else if (hasAzureOpenAiKey && defaultProvider.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase))
{
	// Register Semantic Kernel with Azure OpenAI services
	builder.Services.AddTransient<Kernel>(provider =>
	{
		var kernelBuilder = Kernel.CreateBuilder();
		kernelBuilder.AddAzureOpenAIChatCompletion(
			aiConfig.AzureOpenAI.CompletionDeploymentName,
			aiConfig.AzureOpenAI.Endpoint,
			aiConfig.AzureOpenAI.ApiKey);
		kernelBuilder.AddAzureOpenAITextEmbeddingGeneration(
			aiConfig.AzureOpenAI.EmbeddingDeploymentName,
			aiConfig.AzureOpenAI.Endpoint,
			aiConfig.AzureOpenAI.ApiKey);
		return kernelBuilder.Build();
	});

	// Register Semantic Kernel services for dependency injection
	builder.Services.AddTransient<IChatCompletionService>(provider =>
		provider.GetRequiredService<Kernel>().GetRequiredService<IChatCompletionService>());
	builder.Services.AddTransient<ITextEmbeddingGenerationService>(provider =>
		provider.GetRequiredService<Kernel>().GetRequiredService<ITextEmbeddingGenerationService>());

	// Register HyDE services
	builder.Services.AddTransient<IEmbeddingService, SemanticKernelEmbeddingService>();
	builder.Services.AddTransient<IHypotheticalDocumentGenerator, SemanticKernelHypotheticalDocumentGenerator>();
}
else if (hasOllamaConfig && defaultProvider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
{
	// Register Semantic Kernel with Ollama services
	builder.Services.AddTransient<Kernel>(provider =>
	{
		var kernelBuilder = Kernel.CreateBuilder();
		kernelBuilder.AddOllamaChatCompletion(
			aiConfig.Ollama.CompletionModel,
			new Uri(aiConfig.Ollama.BaseUrl));
		kernelBuilder.AddOllamaTextEmbeddingGeneration(
			aiConfig.Ollama.EmbeddingModel,
			new Uri(aiConfig.Ollama.BaseUrl));
		return kernelBuilder.Build();
	});

	// Register Semantic Kernel services for dependency injection
	builder.Services.AddTransient<IChatCompletionService>(provider =>
		provider.GetRequiredService<Kernel>().GetRequiredService<IChatCompletionService>());
	builder.Services.AddTransient<ITextEmbeddingGenerationService>(provider =>
		provider.GetRequiredService<Kernel>().GetRequiredService<ITextEmbeddingGenerationService>());

	// Register HyDE services
	builder.Services.AddTransient<IEmbeddingService, SemanticKernelEmbeddingService>();
	builder.Services.AddTransient<IHypotheticalDocumentGenerator, SemanticKernelHypotheticalDocumentGenerator>();
}
else
{
	// Register mock services for testing without AI providers
	builder.Services.AddTransient<IEmbeddingService, MockEmbeddingService>();
	builder.Services.AddTransient<IHypotheticalDocumentGenerator, MockHypotheticalDocumentGenerator>();
}

// Register core HyDE Search services
builder.Services.AddSingleton<IDocumentStore, InMemoryDocumentStore>();
builder.Services.AddSingleton<IVectorSimilarityService, VectorSimilarityService>();
builder.Services.AddTransient<IHydeSearchService, HydeSearchService>();

// Register BM25 and Hybrid Search services
var bm25Config = builder.Configuration.GetSection("BM25").Get<BM25Configuration>() ?? new BM25Configuration();

if (bm25Config.Provider.Equals("ElasticSearch", StringComparison.OrdinalIgnoreCase))
{
	// Register ElasticSearch client
	builder.Services.AddSingleton<IElasticClient>(provider =>
	{
		var config = bm25Config.ElasticSearch;
		var connectionSettings = new ConnectionSettings(new Uri(builder.Configuration.GetConnectionString("elasticsearch")))
			.DefaultIndex(config.IndexName.ToLowerInvariant())
			.EnableDebugMode()
			.PrettyJson();

		//if (!string.IsNullOrWhiteSpace(config.Username) && !string.IsNullOrWhiteSpace(config.Password))
		//{
		//    connectionSettings = connectionSettings.BasicAuthentication(config.Username, config.Password);
		//}

		//if (!config.VerifySSL)
		//{
		//    connectionSettings = connectionSettings.ServerCertificateValidationCallback((o, certificate, chain, errors) => true);
		//}

		return new ElasticClient(connectionSettings);
	});

	builder.Services.AddSingleton<IBM25Service, ElasticSearchBM25Service>();
}
else
{
	// Default to in-memory BM25 service
	builder.Services.AddSingleton<IBM25Service, BM25Service>();
}

builder.Services.AddTransient<IHybridSearchService, HybridSearchService>();

// Add CORS for development
builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowAll",
		policy =>
		{
			policy.AllowAnyOrigin()
				  .AllowAnyMethod()
				  .AllowAnyHeader();
		});
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
	c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hybrid Search API v1 (BM25 + HyDE)");
	c.RoutePrefix = "swagger";
});
app.UseCors("AllowAll");

app.UseHttpsRedirection();

// Minimal API endpoints
app.MapGet("/", () => new
{
	message = "🚀 Hybrid Search API (BM25 + HyDE)",
	version = "1.0.0",
	searchTypes = new[] {
		"Hybrid Search: BM25 + HyDE semantic search",
		"HyDE Search: Pure semantic search",
		"BM25 Search: Keyword-based search"
	},
	endpoints = new[] {
		"GET /api/health - Health check",
		"GET /api/documents - Get all documents",
		"POST /api/documents - Add documents",
		"POST /api/search/hybrid - Hybrid search (BM25 + HyDE)",
		"POST /api/search - HyDE semantic search only",
		"GET /api/search/quick?q={query} - Quick hybrid search"
	}
})
.WithName("GetRoot")
.WithOpenApi();

app.MapGet("/api/health", () => new
{
	status = "healthy",
	timestamp = DateTime.UtcNow,
	service = "Hybrid Search API (BM25 + HyDE)"
})
.WithName("GetHealth")
.WithOpenApi();

app.MapGet("/api/documents", async (IHybridSearchService hybridSearch) =>
{
	try
	{
		var count = await hybridSearch.GetIndexedDocumentCountAsync();
		return Results.Ok(new { documentCount = count, message = $"Total indexed documents: {count}" });
	}
	catch (Exception ex)
	{
		return Results.Problem($"Error retrieving document count: {ex.Message}");
	}
})
.WithName("GetDocuments")
.WithOpenApi();

app.MapPost("/api/documents", async (
	[FromBody] Document[] documents,
	IHybridSearchService hybridSearch,
	ILogger<Program> logger) =>
{
	try
	{
		if (documents == null || documents.Length == 0)
		{
			return Results.BadRequest("No documents provided");
		}

		await hybridSearch.IndexDocumentsAsync(documents);
		var count = await hybridSearch.GetIndexedDocumentCountAsync();

		logger.LogInformation("✅ Successfully indexed {DocumentCount} documents for hybrid search", documents.Length);

		return Results.Ok(new
		{
			message = $"Successfully indexed {documents.Length} documents for hybrid search (BM25 + HyDE)",
			totalDocuments = count
		});
	}
	catch (Exception ex)
	{
		logger.LogError(ex, "❌ Error indexing documents");
		return Results.Problem($"Error indexing documents: {ex.Message}");
	}
})
.WithName("PostDocuments")
.WithOpenApi();

app.MapPost("/api/search/hybrid", async (
	[FromBody] HybridSearchRequest request,
	IHybridSearchService hybridSearch,
	IOptions<HybridSearchConfiguration> config,
	ILogger<Program> logger) =>
{
	try
	{
		if (string.IsNullOrWhiteSpace(request.Query))
		{
			return Results.BadRequest("Query cannot be empty");
		}

		logger.LogInformation("🔍 Hybrid search for: '{Query}'", request.Query);

		var stopwatch = System.Diagnostics.Stopwatch.StartNew();
		var results = await hybridSearch.SearchAsync(request.Query);
		stopwatch.Stop();

		var resultList = results.Take(request.MaxResults ?? 10).ToList();

		// Count results by type for metrics
		var bm25Count = resultList.Count(r => r.BM25Score.HasValue);
		var hydeCount = resultList.Count(r => r.HydeScore.HasValue);

		var response = new HybridSearchResponse(
			request.Query,
			resultList,
			resultList.Count,
			stopwatch.ElapsedMilliseconds,
			new SearchMetrics(
				config.Value.EnableBM25,
				config.Value.EnableHyDE,
				bm25Count,
				hydeCount,
				config.Value.BM25Weight,
				config.Value.HydeWeight
			)
		);

		logger.LogInformation("📋 Hybrid search found {ResultCount} results for query: '{Query}' in {ElapsedMs}ms",
			resultList.Count, request.Query, stopwatch.ElapsedMilliseconds);

		return Results.Ok(response);
	}
	catch (Exception ex)
	{
		logger.LogError(ex, "❌ Error in hybrid search for query: '{Query}'", request.Query);
		return Results.Problem($"Error processing hybrid search request: {ex.Message}");
	}
})
.WithName("PostHybridSearch")
.WithOpenApi();

app.MapPost("/api/search", async (
	[FromBody] SearchRequest request,
	IHydeSearchService hydeSearch,
	ILogger<Program> logger) =>
{
	try
	{
		if (string.IsNullOrWhiteSpace(request.Query))
		{
			return Results.BadRequest("Query cannot be empty");
		}

		logger.LogInformation("🔍 Searching for: '{Query}'", request.Query);

		var stopwatch = System.Diagnostics.Stopwatch.StartNew();
		var results = await hydeSearch.SearchAsync(request.Query);
		stopwatch.Stop();

		var resultList = results.Take(request.MaxResults ?? 10).ToList(); var response = new SearchResponse(
			request.Query,
			resultList,
			resultList.Count,
			stopwatch.ElapsedMilliseconds
		);

		logger.LogInformation("📋 Found {ResultCount} results for query: '{Query}' in {ElapsedMs}ms",
			resultList.Count, request.Query, stopwatch.ElapsedMilliseconds);

		return Results.Ok(response);
	}
	catch (Exception ex)
	{
		logger.LogError(ex, "❌ Error searching for query: '{Query}'", request.Query);
		return Results.Problem($"Error processing search request: {ex.Message}");
	}
})
.WithName("PostSearch")
.WithOpenApi();

app.MapGet("/api/search/quick", async (
	[FromQuery] string q,
	[FromQuery] int? limit,
	IHybridSearchService hybridSearch,
	IOptions<HybridSearchConfiguration> config,
	ILogger<Program> logger) =>
{
	try
	{
		if (string.IsNullOrWhiteSpace(q))
		{
			return Results.BadRequest("Query parameter 'q' cannot be empty");
		}

		var searchLimit = limit ?? 5;
		if (searchLimit <= 0) searchLimit = 5;

		logger.LogInformation("⚡ Quick hybrid search for: '{Query}'", q);

		var stopwatch = System.Diagnostics.Stopwatch.StartNew();
		var results = await hybridSearch.SearchAsync(q);
		stopwatch.Stop();

		var resultList = results.Take(searchLimit).ToList();

		// Count results by type for metrics
		var bm25Count = resultList.Count(r => r.BM25Score.HasValue);
		var hydeCount = resultList.Count(r => r.HydeScore.HasValue);

		var response = new HybridSearchResponse(
			q,
			resultList,
			resultList.Count,
			stopwatch.ElapsedMilliseconds,
			new SearchMetrics(
				config.Value.EnableBM25,
				config.Value.EnableHyDE,
				bm25Count,
				hydeCount,
				config.Value.BM25Weight,
				config.Value.HydeWeight
			)
		);

		return Results.Ok(response);
	}
	catch (Exception ex)
	{
		logger.LogError(ex, "❌ Error in quick hybrid search for query: '{Query}'", q);
		return Results.Problem($"Error processing quick hybrid search request: {ex.Message}");
	}
})
.WithName("GetQuickSearch")
.WithOpenApi();

// Initialize with sample data on startup
await InitializeSampleData(app.Services);

app.Run();

static async Task InitializeSampleData(IServiceProvider services)
{
	using var scope = services.CreateScope();
	var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
	var hybridSearch = scope.ServiceProvider.GetRequiredService<IHybridSearchService>();

	try
	{
		logger.LogInformation("🚀 Initializing Hybrid Search API (BM25 + HyDE)");
		// Check if AI providers are configured
		var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
		var aiConfig = configuration.GetSection("AI").Get<AiConfiguration>() ?? new AiConfiguration();
		
		var hasOpenAi = !string.IsNullOrWhiteSpace(aiConfig.OpenAI.ApiKey);
		var hasAzureOpenAi = !string.IsNullOrWhiteSpace(aiConfig.AzureOpenAI.ApiKey) && !string.IsNullOrWhiteSpace(aiConfig.AzureOpenAI.Endpoint);
		var hasOllama = !string.IsNullOrWhiteSpace(aiConfig.Ollama.BaseUrl);

		if (!hasOpenAi && !hasAzureOpenAi && !hasOllama)
		{
			logger.LogWarning("⚠️  No AI providers configured. API will return mock responses for HyDE search.");
			logger.LogInformation("To configure AI providers:");
			logger.LogInformation("  OpenAI: dotnet user-secrets set \"AI:OpenAI:ApiKey\" \"your-api-key-here\"");
			logger.LogInformation("  Azure OpenAI: Configure ApiKey and Endpoint in AI:AzureOpenAI section");
			logger.LogInformation("  Ollama: Ensure Ollama is running at http://localhost:11434");
			logger.LogInformation("📝 BM25 keyword search will work without API keys");
		}
		else
		{
			logger.LogInformation($"🤖 AI Provider: {aiConfig.DefaultProvider}");
			if (hasOpenAi) logger.LogInformation("✅ OpenAI configured");
			if (hasAzureOpenAi) logger.LogInformation("✅ Azure OpenAI configured");
			if (hasOllama) logger.LogInformation("✅ Ollama configured");
		}

		// Add sample documents
		var documents = new[]
		{
			new Document
			{
				Id = "1",
				Title = "Introduction to Machine Learning",
				Content = "Machine learning is a subset of artificial intelligence that focuses on algorithms and statistical models that computer systems use to perform tasks without explicit instructions. It relies on patterns and inference instead of traditional programming approaches."
			},
			new Document
			{
				Id = "2",
				Title = "Deep Learning Fundamentals",
				Content = "Deep learning is a machine learning technique based on artificial neural networks with multiple layers. These networks can learn complex patterns in data and have been particularly successful in areas like computer vision, natural language processing, and speech recognition."
			},
			new Document
			{
				Id = "3",
				Title = "Natural Language Processing Overview",
				Content = "Natural Language Processing (NLP) is a field of artificial intelligence that focuses on the interaction between computers and human language. It involves developing algorithms and models that can understand, interpret, and generate human language in a meaningful way."
			},
			new Document
			{
				Id = "4",
				Title = "Computer Vision Applications",
				Content = "Computer vision is a field of artificial intelligence that trains computers to interpret and understand visual information from the world. Applications include image recognition, object detection, facial recognition, and autonomous vehicles."
			},
			new Document
			{
				Id = "5",
				Title = "Reinforcement Learning Concepts",
				Content = "Reinforcement learning is a type of machine learning where an agent learns to make decisions by taking actions in an environment to maximize cumulative reward. It's used in robotics, game playing, and autonomous systems."
			}
		};

		await hybridSearch.IndexDocumentsAsync(documents);
		var count = await hybridSearch.GetIndexedDocumentCountAsync();
		logger.LogInformation("✅ Successfully indexed {DocumentCount} sample documents for hybrid search", count);
		logger.LogInformation("🌐 Hybrid Search API is ready! (BM25 + HyDE)");
		logger.LogInformation("📖 Visit /swagger for API documentation");
	}
	catch (Exception ex)
	{
		logger.LogError(ex, "❌ Error initializing sample data");
		logger.LogWarning("⚠️  API will work with manually added documents");
	}
}

// Add configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: true);
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddUserSecrets<Program>();


