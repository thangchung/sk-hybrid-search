# Hybrid Search System

A modern C#/.NET 9 **Web API** that intelligently combines **BM25 keyword search** with **HyDE (Hypothetical Document Embeddings)** semantic search for enhanced document retrieval.

## 🚀 Features

- **🔍 Hybrid Search**: Primary search strategy combining BM25 + HyDE with configurable weights (30% keyword + 70% semantic)
- **📊 BM25 Keyword Search**: Full-featured TF-IDF based keyword matching with configurable parameters
- **🤖 HyDE Semantic Search**: Advanced semantic search using hypothetical document generation and embeddings
- **🌐 RESTful Web API**: ASP.NET Core minimal APIs with comprehensive OpenAPI/Swagger documentation
- **⚡ High-Performance**: Uses `System.Numerics.Tensors` for efficient vector similarity calculations
- **🔧 Flexible AI Providers**: Support for OpenAI and Ollama via Semantic Kernel with automatic fallback to mock services
- **⚙️ Configurable**: Adjustable weights, normalization strategies, and search parameters
- **🏗️ Modern Architecture**: Built with dependency injection, async/await, and nullable reference types
- **📝 Comprehensive Logging**: Structured logging with detailed search metrics
- **🚀 .NET Aspire Ready**: Cloud-native deployment with Elasticsearch orchestration

## 📚 Documentation

- **[C4 Architecture Diagrams](docs/C4-Architecture-Diagrams.md)** - Complete system architecture with Context, Container, and Component models
- **[Implementation Summary](docs/IMPLEMENTATION-SUMMARY.md)** - Technical implementation details
- **[API Reference](#-api-endpoints)** - RESTful endpoint documentation

## 🔍 How Hybrid Search Works

The system provides **three search modes** with the hybrid approach as the primary strategy:

### 🎯 Primary: Hybrid Search (BM25 + HyDE)
1. **Parallel Execution**: BM25 keyword search and HyDE semantic search run simultaneously
2. **Score Normalization**: Apply normalization strategy (MinMax, ZScore, or None)
3. **Weighted Fusion**: Combine scores using configurable weights (default: 30% BM25 + 70% HyDE)
4. **Unified Ranking**: Sort by combined scores for optimal relevance

### 🔤 BM25 Keyword Search Component
- Text preprocessing with tokenization and stop word removal
- Term Frequency (TF) and Inverse Document Frequency (IDF) calculation
- BM25 scoring with configurable k1 and b parameters
- Works independently without AI dependencies

### 🧠 HyDE Semantic Search Component
1. **Query Embedding**: Generate embedding for the original query
2. **Hypothetical Document**: LLM creates a document that would answer the query
3. **Hypothetical Embedding**: Generate embedding for the hypothetical document  
4. **Dual Similarity**: Calculate query-to-document AND hypothetical-to-document similarities
5. **Weighted Combination**: Merge similarities using configurable weights

## ⚡ Quick Start

### Prerequisites
- .NET 9 SDK
- Optional: OpenAI API key (system works with mock services without it)

### 1. Clone and Run
```bash
git clone <repository-url>
cd sk-hybrid-search
dotnet run --project HybridSearch
```

### 2. Access the API
- **🌐 API Root**: http://localhost:5000
- **📖 Swagger UI**: http://localhost:5000/swagger  
- **💚 Health Check**: http://localhost:5000/api/health

### 3. Test with Sample Data
The API automatically loads sample documents on startup. Try these quick searches:

```bash
# Quick hybrid search
curl "http://localhost:5000/api/search/quick?q=machine%20learning&limit=3"

# Full hybrid search with detailed metrics  
curl -X POST "http://localhost:5000/api/search/hybrid" \
  -H "Content-Type: application/json" \
  -d '{"query": "neural networks", "maxResults": 5}'
```

### 4. Optional: Configure OpenAI (for production)
```bash
# Using user secrets (recommended)
dotnet user-secrets set "AI:OpenAI:ApiKey" "your-openai-api-key"

# Or environment variable
export AI__OpenAI__ApiKey="your-openai-api-key"
```

## ⚙️ Configuration

### AI Provider Configuration
The system automatically detects available AI providers and falls back gracefully:

```json
{
  "AI": {
    "DefaultProvider": "OpenAI",
    "OpenAI": {
      "ApiKey": "",
      "EmbeddingModel": "text-embedding-3-small",
      "CompletionModel": "gpt-4o-mini"
    },
    "Ollama": {
      "BaseUrl": "http://localhost:11434",
      "EmbeddingModel": "nomic-embed-text", 
      "CompletionModel": "llama3.2"
    }
  }
}
```

### Search Configuration  
```json
{
  "HybridSearch": {
    "BM25Weight": 0.3,
    "HydeWeight": 0.7,
    "MaxResults": 10,
    "ScoreThreshold": 0.01,
    "EnableBM25": true,
    "EnableHyDE": true,
    "NormalizationStrategy": "MinMax"
  },
  "BM25": {
    "Provider": "InMemory"
  }
}
```

## 🌐 API Endpoints

### Core Search Endpoints

| Method | Endpoint | Description | Response |
|--------|----------|-------------|-----------|
| `POST` | `/api/search/hybrid` | **🎯 Primary hybrid search** (BM25 + HyDE) | `HybridSearchResponse` with metrics |
| `GET` | `/api/search/quick` | Quick hybrid search via query parameter | `HybridSearchResponse` |
| `POST` | `/api/search` | HyDE-only semantic search | `SearchResponse` |

### Document Management

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/documents` | Index documents for search |
| `GET` | `/api/documents` | Get indexed document count |

### System Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/` | API information and endpoints |
| `GET` | `/api/health` | System health and status |

### 📋 Request/Response Examples

**Hybrid Search Request:**
```bash
curl -X POST "http://localhost:5000/api/search/hybrid" \
  -H "Content-Type: application/json" \
  -d '{
    "query": "machine learning algorithms",
    "maxResults": 5
  }'
```

**Hybrid Search Response:**
```json
{
  "query": "machine learning algorithms",
  "results": [
    {
      "document": {
        "id": "1",
        "title": "Introduction to Machine Learning",
        "content": "Machine learning is a subset of artificial intelligence..."
      },
      "combinedScore": 0.8542,
      "bm25Score": 0.7234,
      "hydeScore": 0.9123,
      "normalizedBM25Score": 0.8100,
      "normalizedHydeScore": 0.8734,
      "hypotheticalDocument": "A comprehensive guide covering machine learning algorithms..."
    }
  ],
  "totalResults": 1,
  "processingTimeMs": 245,
  "metrics": {
    "bm25Enabled": true,
    "hydeEnabled": true,
    "bm25ResultCount": 1,
    "hydeResultCount": 1,
    "bm25Weight": 0.3,
    "hydeWeight": 0.7
  }
}
```

**Quick Search:**
```bash
curl "http://localhost:5000/api/search/quick?q=neural%20networks&limit=3"
```

**Document Indexing:**
```bash
curl -X POST "http://localhost:5000/api/documents" \
  -H "Content-Type: application/json" \
  -d '[
    {
      "id": "doc1", 
      "title": "Deep Learning Guide",
      "content": "Deep learning networks use multiple layers..."
    }
  ]'
```

## 🏗️ Architecture Overview

### Project Structure
```
HybridSearch/
├── Program.cs                   # Application entry point with DI configuration
├── Models/
│   ├── Document.cs              # Document model with embedding support
│   ├── SearchResult.cs          # Search result models (HyDE & Hybrid)
│   └── ApiModels.cs             # Request/response models
├── Services/
│   ├── HybridSearchService.cs   # Main hybrid search orchestration
│   ├── HydeSearchService.cs     # HyDE semantic search implementation
│   ├── BM25Service.cs           # In-memory BM25 keyword search
│   ├── ElasticSearchBM25Service.cs # Elasticsearch BM25 provider
│   ├── EmbeddingService.cs      # AI embedding generation via Semantic Kernel
│   ├── HypotheticalDocumentGenerator.cs # LLM document generation
│   ├── VectorSimilarityService.cs # High-performance similarity calculations
│   └── DocumentStore.cs         # Document storage and retrieval
├── Configuration/
│   └── SearchConfiguration.cs   # Configuration models
└── Examples/
    ├── MockServices.cs          # Mock implementations for testing
    └── MockExample.cs           # Usage examples
```

### Core Services

#### `IHybridSearchService`
**Primary service** orchestrating hybrid search combining BM25 and HyDE:
- `SearchAsync()`: Execute hybrid search with weighted score combination
- `IndexDocumentsAsync()`: Index documents for both BM25 and HyDE search
- `GetIndexedDocumentCountAsync()`: Get total indexed document count

#### `IHydeSearchService` 
HyDE semantic search implementation:
- `SearchAsync()`: Perform HyDE semantic search with hypothetical documents
- `IndexDocumentAsync()`: Index single document with embedding generation

#### `IBM25Service`
Keyword search providers (two implementations):
- **`BM25Service`**: In-memory BM25 implementation for development
- **`ElasticSearchBM25Service`**: Production Elasticsearch-based BM25

#### Supporting Services
- **`IEmbeddingService`**: Text embedding generation via Semantic Kernel
- **`IHypotheticalDocumentGenerator`**: LLM-based hypothetical document creation
- **`IVectorSimilarityService`**: High-performance vector similarity using `System.Numerics.Tensors`
- **`IDocumentStore`**: Document persistence with embedding caching

### AI Provider Integration
- **Semantic Kernel**: Abstraction layer for AI services
- **OpenAI Provider**: Production embeddings and chat completion
- **Ollama Provider**: Local LLM alternative  
- **Mock Provider**: Testing and development without AI dependencies
- **Automatic Fallback**: Graceful degradation when AI services unavailable

## 💻 Usage Examples

### Basic Hybrid Search
```csharp
// Get the hybrid search service from DI
var hybridSearch = serviceProvider.GetRequiredService<IHybridSearchService>();

// Index documents
var documents = new[]
{
    new Document
    {
        Id = "1",
        Title = "Machine Learning Fundamentals", 
        Content = "Machine learning is a subset of artificial intelligence that focuses on algorithms..."
    },
    new Document
    {
        Id = "2",
        Title = "Deep Learning Networks",
        Content = "Deep learning uses artificial neural networks with multiple layers..."
    }
};

await hybridSearch.IndexDocumentsAsync(documents);

// Perform hybrid search
var results = await hybridSearch.SearchAsync("What are neural networks?");

foreach (var result in results)
{
    Console.WriteLine($"📄 {result.Document.Title}");
    Console.WriteLine($"🎯 Combined Score: {result.CombinedScore:F3}");
    Console.WriteLine($"🔤 BM25 Score: {result.BM25Score:F3}");
    Console.WriteLine($"🧠 HyDE Score: {result.HydeScore:F3}");
    Console.WriteLine($"📝 Hypothetical: {result.HypotheticalDocument}");
    Console.WriteLine();
}
```

### Configuration-Based Usage
```csharp
// Configure services in Program.cs
builder.Services.Configure<HybridSearchConfiguration>(
    builder.Configuration.GetSection("HybridSearch"));

// Use configured weights and thresholds
var config = serviceProvider.GetRequiredService<IOptions<HybridSearchConfiguration>>();
Console.WriteLine($"BM25 Weight: {config.Value.BM25Weight}");
Console.WriteLine($"HyDE Weight: {config.Value.HydeWeight}");
```

## 🚀 Performance & Scalability

### High-Performance Features
- **`System.Numerics.Tensors`**: Hardware-accelerated vector operations
- **Async/Await Pattern**: Non-blocking operations throughout the pipeline
- **Batch Processing**: Efficient batch embedding generation
- **Connection Pooling**: Optimized HTTP client usage for AI APIs
- **Cancellation Support**: Proper cancellation token handling

### Scalability Considerations
- **In-Memory Storage**: Fast development and testing (extensible to persistent storage)
- **Elasticsearch Integration**: Production-ready BM25 with horizontal scaling
- **Stateless Design**: Each request is independent, supports horizontal scaling
- **Configurable Thresholds**: Filter low-relevance results to improve performance

## 🔧 Dependencies

### Core Dependencies
- **Microsoft.Extensions.Hosting** (9.0.0): Dependency injection and hosting
- **Microsoft.SemanticKernel** (1.0.0): AI service abstraction layer
- **System.Numerics.Tensors** (9.0.0): High-performance vector operations
- **NEST** (7.17.5): Elasticsearch .NET client

### AI Provider Dependencies
- **Microsoft.SemanticKernel.Connectors.OpenAI**: OpenAI integration
- **Microsoft.SemanticKernel.Connectors.Ollama**: Local LLM integration

### Development Dependencies  
- **Swashbuckle.AspNetCore**: OpenAPI/Swagger documentation
- **Microsoft.Extensions.Logging**: Structured logging

## 🤝 Contributing

Contributions are welcome! Please follow these guidelines:

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/amazing-feature`)
3. **Commit** your changes (`git commit -m 'Add amazing feature'`)
4. **Push** to the branch (`git push origin feature/amazing-feature`)
5. **Open** a Pull Request

### Development Setup
```bash
# Clone and setup
git clone <repository-url>
cd sk-hybrid-search

# Run tests
dotnet test

# Run with hot reload
dotnet watch run --project HybridSearch
```

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 📚 References

- **[HyDE Paper](https://arxiv.org/abs/2212.10496)**: Precise Zero-Shot Dense Retrieval without Relevance Labels
- **[OpenAI API Documentation](https://platform.openai.com/docs)**: Embedding and completion API reference
- **[System.Numerics.Tensors](https://learn.microsoft.com/en-us/dotnet/api/system.numerics.tensors)**: High-performance tensor operations
- **[Semantic Kernel](https://learn.microsoft.com/en-us/semantic-kernel/)**: Microsoft's AI orchestration framework
- **[.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/)**: Cloud-native application development
