using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using HydeSearch.Services;
using HydeSearch.Configuration;
using HydeSearch.Models;
using HydeSearch.Examples;

namespace HydeSearch.Examples;

/// <summary>
/// Example program demonstrating HyDE search with mock services
/// </summary>
public static class MockExample
{
    public static async Task RunMockExampleAsync()
    {
        // Create host builder with dependency injection
        var builder = Host.CreateApplicationBuilder();        // Configure mock services (no API keys required)
        builder.Services.Configure<HydeConfiguration>(options =>
        {
            options.HydeWeight = 0.7f;
            options.TraditionalWeight = 0.3f;
            options.MaxResults = 5;
            options.SimilarityThreshold = 0.0f; // Lower threshold for mock data
        });

        // Register mock services
        builder.Services.AddSingleton<IDocumentStore, InMemoryDocumentStore>();
        builder.Services.AddSingleton<IVectorSimilarityService, VectorSimilarityService>();
        builder.Services.AddSingleton<IEmbeddingService, MockEmbeddingService>();
        builder.Services.AddSingleton<IHypotheticalDocumentGenerator, MockHypotheticalDocumentGenerator>();
        builder.Services.AddSingleton<IHydeSearchService, HydeSearchService>();

        // Add logging
        builder.Services.AddLogging(configure => configure.AddConsole().SetMinimumLevel(LogLevel.Information));

        var host = builder.Build();

        // Get services
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var hydeSearch = host.Services.GetRequiredService<IHydeSearchService>();

        try
        {
            logger.LogInformation("=== HyDE Search Mock Example ===");
            logger.LogInformation("This example uses mock services and doesn't require OpenAI API keys");
            logger.LogInformation("");

            // Index sample documents
            await IndexSampleDocumentsAsync(hydeSearch, logger);

            // Perform example searches
            await PerformExampleSearchesAsync(hydeSearch, logger);

            logger.LogInformation("=== Mock Example Completed Successfully ===");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during mock example execution");
        }
    }

    private static async Task IndexSampleDocumentsAsync(IHydeSearchService hydeSearch, ILogger logger)
    {
        logger.LogInformation("📚 Indexing sample documents...");

        var documents = new[]
        {
            new Document
            {
                Id = "ai-ml-basics",
                Title = "Artificial Intelligence and Machine Learning Fundamentals",
                Content = "Artificial Intelligence (AI) represents the simulation of human intelligence in machines. Machine Learning, a subset of AI, enables systems to learn and improve from experience without explicit programming. Key techniques include supervised learning, unsupervised learning, and reinforcement learning."
            },
            new Document
            {
                Id = "deep-learning",
                Title = "Deep Learning and Neural Networks",
                Content = "Deep learning utilizes artificial neural networks with multiple layers to model and understand complex patterns in data. These networks excel at tasks like image recognition, natural language processing, and speech synthesis. Popular architectures include CNNs, RNNs, and Transformers."
            },
            new Document
            {
                Id = "nlp-overview",
                Title = "Natural Language Processing Technologies",
                Content = "Natural Language Processing enables computers to understand, interpret, and generate human language. Applications include sentiment analysis, machine translation, chatbots, and text summarization. Modern NLP leverages transformer models and large language models."
            },
            new Document
            {
                Id = "computer-vision",
                Title = "Computer Vision and Image Processing",
                Content = "Computer vision enables machines to interpret visual information from images and videos. Applications span from medical imaging and autonomous vehicles to facial recognition and quality control in manufacturing. Techniques include image classification, object detection, and segmentation."
            },
            new Document
            {
                Id = "data-science",
                Title = "Data Science and Analytics",
                Content = "Data science combines statistics, programming, and domain expertise to extract insights from data. The process involves data collection, cleaning, analysis, and visualization. Key tools include Python, R, SQL, and various machine learning libraries."
            },
            new Document
            {
                Id = "cloud-computing",
                Title = "Cloud Computing Platforms",
                Content = "Cloud computing provides on-demand access to computing resources over the internet. Major platforms include AWS, Azure, and Google Cloud. Services range from infrastructure (IaaS) to platforms (PaaS) and software (SaaS), enabling scalable and flexible computing solutions."
            }
        };

        await hydeSearch.IndexDocumentsAsync(documents);
        var count = await hydeSearch.GetIndexedDocumentCountAsync();
        logger.LogInformation("✅ Successfully indexed {DocumentCount} documents", count);
        logger.LogInformation("");
    }

    private static async Task PerformExampleSearchesAsync(IHydeSearchService hydeSearch, ILogger logger)
    {
        var queries = new[]
        {
            "What is artificial intelligence?",
            "How do neural networks learn?",
            "What are the applications of computer vision?",
            "How does machine learning work?",
            "What is cloud computing?",
            "Text analysis and processing techniques"
        };

        foreach (var query in queries)
        {
            logger.LogInformation("🔍 Searching for: '{Query}'", query);
            
            var results = await hydeSearch.SearchAsync(query);
            var resultList = results.ToList();

            if (resultList.Any())
            {
                logger.LogInformation("📋 Found {ResultCount} results:", resultList.Count);
                
                foreach (var (result, index) in resultList.Take(3).Select((r, i) => (r, i + 1)))
                {
                    logger.LogInformation("  {Index}. {Title}", index, result.Document.Title);
                    logger.LogInformation("     Combined Score: {Score:F3} (Traditional: {Traditional:F3}, HyDE: {Hyde:F3})",
                        result.Similarity,
                        result.TraditionalSimilarity,
                        result.HydeSimilarity);
                }
                
                var topResult = resultList.First();
                if (!string.IsNullOrEmpty(topResult.HypotheticalDocument))
                {
                    logger.LogInformation("💭 Hypothetical Document: {HypotheticalDocument}", 
                        topResult.HypotheticalDocument);
                }
            }
            else
            {
                logger.LogWarning("❌ No results found for query: {Query}", query);
            }
            
            logger.LogInformation("");
        }
    }
}
