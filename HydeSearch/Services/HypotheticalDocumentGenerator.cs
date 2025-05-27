using System.Text.Json;
using HydeSearch.Configuration;
using Microsoft.Extensions.Options;

namespace HydeSearch.Services;

/// <summary>
/// Interface for generating hypothetical documents from queries
/// </summary>
public interface IHypotheticalDocumentGenerator
{
    Task<string> GenerateHypotheticalDocumentAsync(string query, CancellationToken cancellationToken = default);
}

/// <summary>
/// OpenAI-based hypothetical document generator
/// </summary>
public class OpenAiHypotheticalDocumentGenerator : IHypotheticalDocumentGenerator
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiConfiguration _openAiConfig;
    private readonly HydeConfiguration _hydeConfig;

    public OpenAiHypotheticalDocumentGenerator(
        HttpClient httpClient, 
        IOptions<OpenAiConfiguration> openAiConfig,
        IOptions<HydeConfiguration> hydeConfig)
    {
        _httpClient = httpClient;
        _openAiConfig = openAiConfig.Value;
        _hydeConfig = hydeConfig.Value;
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _openAiConfig.ApiKey);
    }

    public async Task<string> GenerateHypotheticalDocumentAsync(string query, CancellationToken cancellationToken = default)
    {
        var prompt = _hydeConfig.HydePromptTemplate.Replace("{query}", query);
        
        var requestData = new
        {
            model = _openAiConfig.CompletionModel,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            max_tokens = _openAiConfig.MaxTokens,
            temperature = _openAiConfig.Temperature
        };

        var json = JsonSerializer.Serialize(requestData);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_openAiConfig.BaseUrl}/chat/completions", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var completionResponse = JsonSerializer.Deserialize<CompletionResponse>(responseJson)!;

        return completionResponse.Choices[0].Message.Content.Trim();
    }

    private class CompletionResponse
    {
        public Choice[] Choices { get; set; } = [];
    }

    private class Choice
    {
        public Message Message { get; set; } = new();
    }

    private class Message
    {
        public string Content { get; set; } = string.Empty;
    }
}

/// <summary>
/// Mock hypothetical document generator for testing without OpenAI API
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
