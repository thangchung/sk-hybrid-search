# Hybrid Search - BM25 + HyDE Implementation

A modern C#/.NET 9 **Web API** implementation that combines **BM25 keyword search** with **HyDE (Hypothetical Document Embeddings)** semantic search, providing the best of both traditional and modern search techniques.

## Features

- **🔍 Hybrid Search**: Combines BM25 keyword matching with HyDE semantic similarity
- **📊 BM25 Implementation**: Full-featured TF-IDF based keyword search with configurable parameters
- **🤖 HyDE Algorithm**: Semantic search using hypothetical document generation and embeddings
- **🌐 RESTful Web API**: ASP.NET Core minimal APIs with OpenAPI/Swagger documentation
- **⚡ High-Performance Vector Operations**: Uses `System.Numerics.Tensors` for efficient similarity calculations
- **🤖 OpenAI Integration**: Leverages OpenAI's embedding and completion APIs with fallback to mock services
- **⚙️ Configurable Weights**: Adjustable BM25/HyDE score combination and normalization strategies
- **📈 Score Normalization**: Multiple normalization strategies (MinMax, ZScore, None)
- **🏗️ Modern C# Architecture**: Built with dependency injection, async/await, and nullable reference types
- **📝 Comprehensive Logging**: Structured logging with Microsoft.Extensions.Logging
- **🚀 .NET Aspire Ready**: Configured for cloud-native deployment and orchestration
- **🧪 Mock Services**: Built-in mock services for development and testing without OpenAI API

## 📚 Documentation

- **[Architecture Overview](docs/Architecture-Overview.md)** - Complete system architecture with C4 component model
- **[Workflow Documentation](docs/HyDE-Search-Workflow.md)** - Detailed workflow diagrams and process flow
- **[API Reference](#api-endpoints)** - RESTful endpoint documentation

## How Hybrid Search Works

The hybrid approach combines two complementary search strategies:

### BM25 Keyword Search
1. **Text Preprocessing**: Tokenization, lowercasing, and stop word removal
2. **Term Frequency (TF)**: Calculate how often terms appear in documents
3. **Inverse Document Frequency (IDF)**: Measure term importance across the corpus
4. **BM25 Scoring**: Apply BM25 formula with configurable k1 and b parameters

### HyDE Semantic Search
1. **Traditional Embedding**: Generate an embedding for the original query
2. **Hypothetical Document Generation**: Use an LLM to create a hypothetical document that would answer the query
3. **Hypothetical Embedding**: Generate an embedding for the hypothetical document
4. **Dual Similarity Calculation**: Calculate both query-to-document and hypothetical-to-document similarities
5. **Weighted Combination**: Combine similarities using configurable weights for final ranking

### Hybrid Combination
1. **Independent Scoring**: Both BM25 and HyDE score all documents independently
2. **Score Normalization**: Apply normalization strategy (MinMax, ZScore, or None)
3. **Weighted Fusion**: Combine normalized scores using configurable weights (default: BM25=0.3, HyDE=0.7)
4. **Final Ranking**: Sort by combined scores for optimal relevance

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
   ```   The API will automatically use mock services and start on `http://localhost:5000`

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

- **🧪 Development Mode** (no OpenAI API key): Uses mock services with deterministic responses for HyDE, BM25 works independently
- **🚀 Production Mode** (with OpenAI API key): Uses real OpenAI API services for HyDE component

Configure search parameters in `appsettings.json`:
```json
{
  "HybridSearch": {
    "BM25Weight": 0.3,
    "HydeWeight": 0.7,
    "NormalizationStrategy": "MinMax",
    "EnableBM25": true,
    "EnableHyDE": true
  },
  "BM25": {
    "K1": 1.2,
    "B": 0.75
  },
  "HyDE": {
    "HydeWeight": 0.7,
    "TraditionalWeight": 0.3,
    "MaxResults": 10,
    "SimilarityThreshold": 0.1
  }
}
```

## API Endpoints

The Hybrid Search API provides the following RESTful endpoints:

### Core Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/` | API information and available endpoints |
| `GET` | `/api/health` | Health check with service status |
| `POST` | `/api/search/hybrid` | **Primary hybrid search** (BM25 + HyDE) |
| `GET` | `/api/search/quick` | Quick hybrid search with query parameter |
### Legacy HyDE Endpoints (Still Supported)

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/documents` | Get count of indexed documents |
| `POST` | `/api/documents` | Index new documents with auto-embedding |
| `POST` | `/api/search` | Perform HyDE-only search |
| `GET` | `/api/search/quick?q={query}` | Quick HyDE-only search |

### Example API Usage

**Hybrid Search (Recommended):**
```bash
curl -X POST "http://localhost:5000/api/search/hybrid" \
  -H "Content-Type: application/json" \
  -d '{
    "query": "machine learning algorithms",
    "limit": 5,
    "includeScores": true
  }'
```

**Response:**
```json
{
  "results": [
    {
      "document": {
        "id": "doc1",
        "title": "Machine Learning Basics",
        "content": "Machine learning algorithms..."
      },
      "combinedScore": 0.8542,
      "bm25Score": 0.7234,
      "hydeScore": 0.9123,
      "normalizedBM25Score": 0.8100,
      "normalizedHydeScore": 0.8734
    }
  ],
  "searchMetrics": {
    "bm25Weight": 0.3,
    "hydeWeight": 0.7,
    "normalizationStrategy": "MinMax",
    "totalDocuments": 5,
    "searchDurationMs": 245
  }
}
```

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
