using HydeSearch.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace HydeSearch.Services;

/// <summary>
/// Interface for generating hypothetical documents from queries
/// </summary>
public interface IHypotheticalDocumentGenerator
{
    Task<string> GenerateHypotheticalDocumentAsync(string query, CancellationToken cancellationToken = default);
}

/// <summary>
/// Semantic Kernel-based hypothetical document generator
/// </summary>
public class SemanticKernelHypotheticalDocumentGenerator : IHypotheticalDocumentGenerator
{
    private readonly IChatCompletionService _chatService;
    private readonly HydeConfiguration _hydeConfig;

    public SemanticKernelHypotheticalDocumentGenerator(
        IChatCompletionService chatService,
        IOptions<HydeConfiguration> hydeConfig)
    {
        _chatService = chatService;
        _hydeConfig = hydeConfig.Value;
    }

    public async Task<string> GenerateHypotheticalDocumentAsync(string query, CancellationToken cancellationToken = default)
    {
        var prompt = _hydeConfig.HydePromptTemplate.Replace("{query}", query);
        
        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(prompt);

        var executionSettings = new PromptExecutionSettings
        {
            ExtensionData = new Dictionary<string, object>
            {
                ["max_tokens"] = 1000,
                ["temperature"] = 0.7
            }
        };

        var response = await _chatService.GetChatMessageContentAsync(
            chatHistory, 
            executionSettings, 
            cancellationToken: cancellationToken);

        return response.Content ?? string.Empty;
    }
}

/// <summary>
/// Mock hypothetical document generator for testing without AI providers
/// </summary>
public class MockHypotheticalDocumentGenerator : IHypotheticalDocumentGenerator
{
    public Task<string> GenerateHypotheticalDocumentAsync(string query, CancellationToken cancellationToken = default)
    {
        // Generate a simple hypothetical document based on the query
        // This is a basic mock - in real scenarios, you'd want more sophisticated logic
        var hypotheticalDocument = $"This document discusses {query.ToLowerInvariant()}. " +
                                   $"It covers the main concepts related to {query.ToLowerInvariant()} " +
                                   $"and provides detailed information about how {query.ToLowerInvariant()} " +
                                   $"works in practice. The document explains the principles behind " +
                                   $"{query.ToLowerInvariant()} and its applications in modern technology.";
        
        return Task.FromResult(hypotheticalDocument);
    }
}
