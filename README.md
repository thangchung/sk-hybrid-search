# HyDE Search - Hypothetical Document Embeddings

A modern C#/.NET 9 **Web API** implementation of HyDE (Hypothetical Document Embeddings) search, which enhances traditional semantic search by generating hypothetical documents that would answer queries and using them to improve search relevance.

## Features

- **🌐 RESTful Web API**: ASP.NET Core minimal APIs with OpenAPI/Swagger documentation
- **🔍 HyDE Algorithm Implementation**: Combines traditional query-document similarity with hypothetical document generation
- **⚡ High-Performance Vector Operations**: Uses `System.Numerics.Tensors` for efficient similarity calculations
- **🤖 OpenAI Integration**: Leverages OpenAI's embedding and completion APIs with fallback to mock services
- **🏗️ Modern C# Architecture**: Built with dependency injection, async/await, and nullable reference types
- **⚙️ Configurable Search Parameters**: Customizable weights and thresholds
- **📝 Comprehensive Logging**: Structured logging with Microsoft.Extensions.Logging
- **🚀 .NET Aspire Ready**: Configured for cloud-native deployment and orchestration
- **🧪 Mock Services**: Built-in mock services for development and testing without OpenAI API

## 📚 Documentation

- **[Architecture Overview](docs/Architecture-Overview.md)** - Complete system architecture with C4 component model
- **[Workflow Documentation](docs/HyDE-Search-Workflow.md)** - Detailed workflow diagrams and process flow
- **[API Reference](#api-endpoints)** - RESTful endpoint documentation

## How HyDE Works

1. **Traditional Embedding**: Generate an embedding for the original query
2. **Hypothetical Document Generation**: Use an LLM to create a hypothetical document that would answer the query
3. **Hypothetical Embedding**: Generate an embedding for the hypothetical document
4. **Dual Similarity Calculation**: Calculate both query-to-document and hypothetical-to-document similarities
5. **Weighted Combination**: Combine similarities using configurable weights for final ranking

## Getting Started

### Prerequisites

- .NET 9 SDK
- OpenAI API key

### Configuration

1. Set your OpenAI API key in one of the following ways:

   **Option A: User Secrets (Recommended for development)**
   ```bash
   dotnet user-secrets set "OpenAI:ApiKey" "your-openai-api-key-here"
   ```

   **Option B: Environment Variable**
   ```bash
   set OpenAI__ApiKey=your-openai-api-key-here### Quick Start

1. **Clone and build**:
   ```bash
   git clone <repository-url>
   cd sk-hybrid-search
   dotnet build
   ```

2. **Run with mock services** (no OpenAI API key required):
   ```bash
   dotnet run
   ```
   The API will automatically use mock services and start on `http://localhost:5000`

3. **Access the API**:
   - **Swagger UI**: http://localhost:5000/swagger
   - **API Root**: http://localhost:5000
   - **Health Check**: http://localhost:5000/api/health

4. **Optional: Configure OpenAI** (for production use):
   ```bash
   dotnet user-secrets set "OpenAI:ApiKey" "your-openai-api-key-here"
   ```

### Development Configuration

The application automatically detects the environment and configures services accordingly:

- **🧪 Development Mode** (no OpenAI API key): Uses mock services with deterministic responses
- **🚀 Production Mode** (with OpenAI API key): Uses real OpenAI API services

Configure search parameters in `appsettings.json`:
```json
{
  "HyDE": {
    "HydeWeight": 0.7,
    "TraditionalWeight": 0.3,
    "MaxResults": 10,
    "SimilarityThreshold": 0.1
  }
}
```

## API Endpoints

The HyDE Search API provides the following RESTful endpoints:

### Core Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/` | API information and available endpoints |
| `GET` | `/api/health` | Health check with service status |
| `GET` | `/api/documents` | Get count of indexed documents |
| `POST` | `/api/documents` | Index new documents with auto-embedding |
| `POST` | `/api/search` | Perform HyDE search with detailed results |
| `GET` | `/api/search/quick?q={query}` | Quick search with query parameter |

### Example API Usage

**Index Documents:**
```bash
curl -X POST "http://localhost:5000/api/documents" \
  -H "Content-Type: application/json" \
  -d '[
    {
      "id": "doc1",
      "title": "Machine Learning Basics",
      "content": "Machine learning is a subset of artificial intelligence..."
    }
  ]'
```

**Search Documents:**
```bash
curl -X POST "http://localhost:5000/api/search" \
  -H "Content-Type: application/json" \
  -d '{
    "query": "What is machine learning?",
    "maxResults": 5
  }'
```

**Quick Search:**
```bash
curl "http://localhost:5000/api/search/quick?q=machine%20learning&limit=5"
```

## Project Structure

```
├── Models/
│   ├── Document.cs              # Document model with embedding support
│   └── SearchResult.cs          # Search result with similarity scores
├── Services/
│   ├── EmbeddingService.cs      # OpenAI embedding generation
│   ├── HypotheticalDocumentGenerator.cs  # LLM-based document generation
│   ├── VectorSimilarityService.cs        # High-performance similarity calculations
│   ├── DocumentStore.cs         # Document storage and retrieval
│   └── HydeSearchService.cs     # Main HyDE search orchestration
├── Configuration/
│   └── SearchConfiguration.cs   # Configuration models
└── Program.cs                   # Application entry point with demo
```

## Core Services

### IHydeSearchService
Main search service that orchestrates the HyDE algorithm:
- `SearchAsync()`: Perform HyDE-enhanced search
- `IndexDocumentAsync()`: Add documents to the search index
- `IndexDocumentsAsync()`: Batch index multiple documents

### IEmbeddingService
Generates text embeddings using OpenAI's API:
- `GetEmbeddingAsync()`: Generate embedding for a single text
- `GetEmbeddingsAsync()`: Batch generate embeddings

### IHypotheticalDocumentGenerator
Creates hypothetical documents using OpenAI's completion API:
- `GenerateHypotheticalDocumentAsync()`: Generate a document that would answer the query

### IVectorSimilarityService
High-performance vector similarity calculations:
- `CalculateCosineSimilarity()`: Cosine similarity using System.Numerics.Tensors
- `CalculateDotProduct()`: Dot product calculation
- `CalculateEuclideanDistance()`: Euclidean distance calculation

## Configuration Options

### OpenAI Configuration
- `ApiKey`: Your OpenAI API key
- `BaseUrl`: OpenAI API base URL (default: https://api.openai.com/v1)
- `EmbeddingModel`: Model for embeddings (default: text-embedding-3-small)
- `CompletionModel`: Model for text generation (default: gpt-4o-mini)
- `MaxTokens`: Maximum tokens for completion (default: 1000)
- `Temperature`: Creativity level for generation (default: 0.7)

### HyDE Configuration
- `HydeWeight`: Weight for hypothetical document similarity (default: 0.7)
- `TraditionalWeight`: Weight for traditional query similarity (default: 0.3)
- `MaxResults`: Maximum number of results to return (default: 10)
- `SimilarityThreshold`: Minimum similarity score to include (default: 0.1)
- `HydePromptTemplate`: Template for generating hypothetical documents

## Example Usage

```csharp
// Get the search service from DI
var hydeSearch = serviceProvider.GetRequiredService<IHydeSearchService>();

// Index documents
var documents = new[]
{
    new Document
    {
        Id = "1",
        Title = "Machine Learning Basics",
        Content = "Machine learning is a subset of AI..."
    }
};
await hydeSearch.IndexDocumentsAsync(documents);

// Perform search
var results = await hydeSearch.SearchAsync("What is artificial intelligence?");

foreach (var result in results)
{
    Console.WriteLine($"Document: {result.Document.Title}");
    Console.WriteLine($"Combined Similarity: {result.Similarity:F3}");
    Console.WriteLine($"Traditional Similarity: {result.TraditionalSimilarity:F3}");
    Console.WriteLine($"HyDE Similarity: {result.HydeSimilarity:F3}");
    Console.WriteLine($"Hypothetical Document: {result.HypotheticalDocument}");
    Console.WriteLine();
}
```

## Performance Considerations

- Uses `System.Numerics.Tensors` for high-performance vector operations
- Supports batch embedding generation to reduce API calls
- In-memory document store for fast retrieval (can be extended with persistent storage)
- Configurable similarity thresholds to filter irrelevant results

## Dependencies

- **Microsoft.Extensions.Hosting** (8.0.0): Dependency injection and hosting
- **Microsoft.Extensions.Http** (8.0.0): HTTP client integration
- **System.Numerics.Tensors** (9.0.0): High-performance vector operations

## License

This project is licensed under the MIT License.

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

## References

- [HyDE Paper: Precise Zero-Shot Dense Retrieval without Relevance Labels](https://arxiv.org/abs/2212.10496)
- [OpenAI API Documentation](https://platform.openai.com/docs)
- [System.Numerics.Tensors Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.numerics.tensors)
