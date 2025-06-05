# Azure OpenAI Configuration Guide

This guide shows how to configure Azure OpenAI as an AI provider for the Hybrid Search application.

## Prerequisites

1. **Azure OpenAI Resource**: You need an Azure OpenAI resource deployed in Azure
2. **Model Deployments**: Deploy the required models in your Azure OpenAI resource
3. **API Access**: Ensure you have the API key and endpoint URL

## Required Model Deployments

Deploy these models in your Azure OpenAI resource:

### Embedding Model
- **Model**: `text-embedding-3-small` (recommended) or `text-embedding-ada-002`
- **Deployment Name**: `text-embedding-3-small` (or customize in config)

### Chat Completion Model  
- **Model**: `gpt-4o-mini` (recommended) or `gpt-3.5-turbo`
- **Deployment Name**: `gpt-4o-mini` (or customize in config)

## Configuration

### Option 1: Update appsettings.json

```json
{
  "AI": {
    "DefaultProvider": "AzureOpenAI",
    "AzureOpenAI": {
      "ApiKey": "your-azure-openai-api-key",
      "Endpoint": "https://your-resource-name.openai.azure.com/",
      "EmbeddingDeploymentName": "text-embedding-3-small",
      "CompletionDeploymentName": "gpt-4o-mini",
      "ApiVersion": "2024-02-01",
      "MaxTokens": 1000,
      "Temperature": 0.7
    }
  }
}
```

### Option 2: User Secrets (Recommended for Development)

```bash
# Set the default provider
dotnet user-secrets set "AI:DefaultProvider" "AzureOpenAI"

# Set Azure OpenAI credentials
dotnet user-secrets set "AI:AzureOpenAI:ApiKey" "your-azure-openai-api-key"
dotnet user-secrets set "AI:AzureOpenAI:Endpoint" "https://your-resource-name.openai.azure.com/"

# Optional: Customize deployment names if different
dotnet user-secrets set "AI:AzureOpenAI:EmbeddingDeploymentName" "your-embedding-deployment"
dotnet user-secrets set "AI:AzureOpenAI:CompletionDeploymentName" "your-completion-deployment"
```

### Option 3: Environment Variables

```bash
# Windows
set AI__DefaultProvider=AzureOpenAI
set AI__AzureOpenAI__ApiKey=your-azure-openai-api-key
set AI__AzureOpenAI__Endpoint=https://your-resource-name.openai.azure.com/

# Linux/macOS
export AI__DefaultProvider=AzureOpenAI
export AI__AzureOpenAI__ApiKey=your-azure-openai-api-key
export AI__AzureOpenAI__Endpoint=https://your-resource-name.openai.azure.com/
```

## Finding Your Azure OpenAI Settings

### API Key
1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to your Azure OpenAI resource
3. Go to **Keys and Endpoint** section
4. Copy **KEY 1** or **KEY 2**

### Endpoint URL
1. In the same **Keys and Endpoint** section
2. Copy the **Endpoint** URL
3. It should look like: `https://your-resource-name.openai.azure.com/`

### Deployment Names
1. Go to **Model deployments** section in your Azure OpenAI resource
2. Note the **Deployment name** for each model you've deployed
3. Use these exact names in your configuration

## Configuration Properties

| Property | Description | Default | Required |
|----------|-------------|---------|----------|
| `ApiKey` | Azure OpenAI API key | - | ✅ |
| `Endpoint` | Azure OpenAI endpoint URL | - | ✅ |
| `EmbeddingDeploymentName` | Name of embedding model deployment | `text-embedding-3-small` | ✅ |
| `CompletionDeploymentName` | Name of chat completion model deployment | `gpt-4o-mini` | ✅ |
| `ApiVersion` | Azure OpenAI API version | `2024-02-01` | ❌ |
| `MaxTokens` | Maximum tokens for completion | `1000` | ❌ |
| `Temperature` | Randomness in responses (0-2) | `0.7` | ❌ |

## Testing the Configuration

1. **Start the application**: `dotnet run`
2. **Check logs**: Look for `✅ Azure OpenAI configured` message
3. **Test the API**: Make a search request to verify Azure OpenAI integration

### Example Test Request

```bash
curl -X POST "http://localhost:5000/api/search/hybrid" \
  -H "Content-Type: application/json" \
  -d '{"query": "machine learning algorithms", "maxResults": 5}'
```

## Troubleshooting

### Common Issues

1. **Invalid Endpoint**: Ensure the endpoint includes the full URL with protocol
   - ✅ Correct: `https://your-resource-name.openai.azure.com/`
   - ❌ Wrong: `your-resource-name.openai.azure.com`

2. **Wrong Deployment Name**: Use exact deployment names from Azure Portal
   - Check **Model deployments** section for exact names

3. **API Version**: Use a supported API version
   - Recommended: `2024-02-01`
   - Check Azure OpenAI documentation for latest versions

4. **Rate Limits**: Azure OpenAI has rate limits per deployment
   - Monitor usage in Azure Portal
   - Consider scaling up your deployment if needed

### Log Messages

- `✅ Azure OpenAI configured` - Configuration successful
- `⚠️ No AI providers configured` - Check your configuration
- `🤖 AI Provider: AzureOpenAI` - Shows active provider

## Switching Between Providers

You can easily switch between OpenAI, Azure OpenAI, and Ollama by changing the `DefaultProvider`:

```json
{
  "AI": {
    "DefaultProvider": "AzureOpenAI"  // or "OpenAI" or "Ollama"
  }
}
```

The application will automatically use the appropriate provider based on this setting and available configuration.
