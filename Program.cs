using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using HydeSearch.Services;
using HydeSearch.Configuration;
using HydeSearch.Models;
using HydeSearch.Examples;

// Check command line arguments
var runMockExample = args.Contains("--mock") || args.Contains("-m");

if (runMockExample)
{
    // Run the mock example that doesn't require OpenAI API keys
    await MockExample.RunMockExampleAsync();
    return 0;
}

// Check if OpenAI API key is configured
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>()
    .Build();

var openAiApiKey = configuration["OpenAI:ApiKey"];
if (string.IsNullOrEmpty(openAiApiKey))
{
    Console.WriteLine("⚠️  OpenAI API key not found!");
    Console.WriteLine();
    Console.WriteLine("To use the full OpenAI-powered HyDE search, please set your API key using one of these methods:");
    Console.WriteLine();
    Console.WriteLine("1. User Secrets (recommended for development):");
    Console.WriteLine("   dotnet user-secrets set \"OpenAI:ApiKey\" \"your-api-key-here\"");
    Console.WriteLine();
    Console.WriteLine("2. Environment Variable:");
    Console.WriteLine("   set OpenAI__ApiKey=your-api-key-here");
    Console.WriteLine();
    Console.WriteLine("3. To run the mock example instead (no API key required):");
    Console.WriteLine("   dotnet run -- --mock");
    Console.WriteLine();
    return 1;
}

// Create host builder with dependency injection for OpenAI-powered search
var builder = Host.CreateApplicationBuilder(args);

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
