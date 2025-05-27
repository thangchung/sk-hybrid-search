using HydeSearch.Services;
using HydeSearch.Configuration;
using HydeSearch.Models;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "HyDE Search API",
        Version = "v1",
        Description = "A web API for HyDE (Hypothetical Document Embeddings) search functionality",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "HyDE Search API"
        }
    });
});

// Configure HyDE Search settings
builder.Services.Configure<OpenAiConfiguration>(
    builder.Configuration.GetSection("OpenAI"));
builder.Services.Configure<HydeConfiguration>(
    builder.Configuration.GetSection("HyDE"));

// Check if OpenAI API key is configured
var openAiApiKey = builder.Configuration["OpenAI:ApiKey"];
var hasOpenAiKey = !string.IsNullOrWhiteSpace(openAiApiKey);

if (hasOpenAiKey)
{
    // Register HTTP clients for OpenAI services
    builder.Services.AddHttpClient<IEmbeddingService, OpenAiEmbeddingService>();
    builder.Services.AddHttpClient<IHypotheticalDocumentGenerator, OpenAiHypotheticalDocumentGenerator>();
    
    // Register OpenAI services
    builder.Services.AddTransient<IEmbeddingService, OpenAiEmbeddingService>();
    builder.Services.AddTransient<IHypotheticalDocumentGenerator, OpenAiHypotheticalDocumentGenerator>();
}
else
{
    // Register mock services for testing without OpenAI
    builder.Services.AddTransient<IEmbeddingService, MockEmbeddingService>();
    builder.Services.AddTransient<IHypotheticalDocumentGenerator, MockHypotheticalDocumentGenerator>();
}

// Register core HyDE Search services
builder.Services.AddSingleton<IDocumentStore, InMemoryDocumentStore>();
builder.Services.AddSingleton<IVectorSimilarityService, VectorSimilarityService>();
builder.Services.AddTransient<IHydeSearchService, HydeSearchService>();

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

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "HyDE Search API v1");
    c.RoutePrefix = "swagger";
});
app.UseCors("AllowAll");

app.UseHttpsRedirection();

// Minimal API endpoints
app.MapGet("/", () => new { 
    message = "🚀 HyDE Search Web API", 
    version = "1.0.0",
    endpoints = new[] {
        "GET /api/health - Health check",
        "GET /api/documents - Get all documents",
        "POST /api/documents - Add documents",
        "POST /api/search - Search documents using HyDE",
        "GET /api/search/quick?q={query} - Quick search"
    }
})
.WithName("GetRoot")
.WithOpenApi();

app.MapGet("/api/health", () => new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    service = "HyDE Search API"
})
.WithName("GetHealth")
.WithOpenApi();

app.MapGet("/api/documents", async (IHydeSearchService hydeSearch) =>
{
    try
    {
        var count = await hydeSearch.GetIndexedDocumentCountAsync();
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
    IHydeSearchService hydeSearch,
    ILogger<Program> logger) =>
{
    try
    {
        if (documents == null || documents.Length == 0)
        {
            return Results.BadRequest("No documents provided");
        }

        await hydeSearch.IndexDocumentsAsync(documents);
        var count = await hydeSearch.GetIndexedDocumentCountAsync();
        
        logger.LogInformation("✅ Successfully indexed {DocumentCount} documents", documents.Length);
        
        return Results.Ok(new { 
            message = $"Successfully indexed {documents.Length} documents",
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
        
        var resultList = results.Take(request.MaxResults ?? 10).ToList();        var response = new SearchResponse(
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
    [FromQuery] int limit,
    IHydeSearchService hydeSearch,
    ILogger<Program> logger) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Results.BadRequest("Query parameter 'q' cannot be empty");
        }

        if (limit <= 0) limit = 5;

        logger.LogInformation("⚡ Quick search for: '{Query}'", q);
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var results = await hydeSearch.SearchAsync(q);
        stopwatch.Stop();
        
        var resultList = results.Take(limit).ToList();        var response = new SearchResponse(
            q,
            resultList,
            resultList.Count,
            stopwatch.ElapsedMilliseconds
        );

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error in quick search for query: '{Query}'", q);
        return Results.Problem($"Error processing quick search request: {ex.Message}");
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
    var hydeSearch = scope.ServiceProvider.GetRequiredService<IHydeSearchService>();

    try
    {
        logger.LogInformation("🚀 Initializing HyDE Search Web API");

        // Check if OpenAI API key is configured
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var openAiApiKey = configuration["OpenAI:ApiKey"];
        
        if (string.IsNullOrEmpty(openAiApiKey))
        {
            logger.LogWarning("⚠️  OpenAI API key not configured. API will return mock responses.");
            logger.LogInformation("To configure OpenAI API key:");
            logger.LogInformation("  1. dotnet user-secrets set \"OpenAI:ApiKey\" \"your-api-key-here\"");
            logger.LogInformation("  2. Or set environment variable: OpenAI__ApiKey=your-api-key-here");
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
        };        await hydeSearch.IndexDocumentsAsync(documents);
        var count = await hydeSearch.GetIndexedDocumentCountAsync();
        logger.LogInformation("✅ Successfully indexed {DocumentCount} sample documents", count);
        logger.LogInformation("🌐 HyDE Search Web API is ready!");
        logger.LogInformation("📖 Visit /swagger for API documentation");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error initializing sample data");
        logger.LogWarning("⚠️  API will work with manually added documents or mock responses");
    }
}

// Add configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: true);
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddUserSecrets<Program>();

// Configure services
builder.Services.Configure<OpenAiConfiguration>(
    builder.Configuration.GetSection("OpenAI"));
builder.Services.Configure<HydeConfiguration>(
    builder.Configuration.GetSection("HyDE"));

// Register HTTP client
builder.Services.AddHttpClient<IEmbeddingService, OpenAiEmbeddingService>();
builder.Services.AddHttpClient<IHypotheticalDocumentGenerator, OpenAiHypotheticalDocumentGenerator>();

// Register services
builder.Services.AddSingleton<IDocumentStore, InMemoryDocumentStore>();
builder.Services.AddSingleton<IVectorSimilarityService, VectorSimilarityService>();
builder.Services.AddTransient<IEmbeddingService, OpenAiEmbeddingService>();
builder.Services.AddTransient<IHypotheticalDocumentGenerator, OpenAiHypotheticalDocumentGenerator>();
builder.Services.AddTransient<IHydeSearchService, HydeSearchService>();

// Add logging
builder.Services.AddLogging(configure => configure.AddConsole());

var host = builder.Build();

// Get services
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var hydeSearch = host.Services.GetRequiredService<IHydeSearchService>();

try
{
    logger.LogInformation("🚀 Starting HyDE Search with OpenAI Integration");
    logger.LogInformation("");

    // Demo: Add sample documents
    await IndexSampleDocuments(hydeSearch, logger);

    // Demo: Perform searches
    await PerformDemoSearches(hydeSearch, logger);

    logger.LogInformation("✅ HyDE Search Demo completed successfully");
}
catch (Exception ex)
{
    logger.LogError(ex, "❌ An error occurred during execution");
    return 1;
}

return 0;

static async Task IndexSampleDocuments(IHydeSearchService hydeSearch, ILogger logger)
{
    logger.LogInformation("📚 Indexing sample documents...");

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

    await hydeSearch.IndexDocumentsAsync(documents);
    var count = await hydeSearch.GetIndexedDocumentCountAsync();
    logger.LogInformation("✅ Successfully indexed {DocumentCount} documents", count);
    logger.LogInformation("");
}

static async Task PerformDemoSearches(IHydeSearchService hydeSearch, ILogger logger)
{
    var queries = new[]
    {
        "What is artificial intelligence?",
        "How do neural networks work?",
        "Applications of computer vision",
        "Learning from rewards and actions"
    };

    foreach (var query in queries)
    {
        logger.LogInformation("🔍 Searching for: '{Query}'", query);
        
        var results = await hydeSearch.SearchAsync(query);
        var resultList = results.ToList();

        logger.LogInformation("📋 Found {ResultCount} results:", resultList.Count);
        
        foreach (var (result, index) in resultList.Take(3).Select((r, i) => (r, i + 1))) // Show top 3 results
        {
            logger.LogInformation("  {Index}. {Title} (Score: {Similarity:F3}, Traditional: {Traditional:F3}, HyDE: {Hyde:F3})",
                index,
                result.Document.Title,
                result.Similarity,
                result.TraditionalSimilarity,
                result.HydeSimilarity);
        }
        
        if (resultList.Any())
        {
            logger.LogInformation("💭 Hypothetical Document: {HypotheticalDocument}", 
                resultList.First().HypotheticalDocument);
        }
        
        logger.LogInformation("");
    }
}
