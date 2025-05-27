# C4 Architecture Diagrams for Hybrid Search Project

This document contains C4 model diagrams (Context, Container, and Component) for the Hybrid Search project using Mermaid format.

## Context Diagram (Level 1)

Shows the high-level system context and external dependencies.

```mermaid
C4Context
    title System Context Diagram for Hybrid Search System

    Person(user, "Search User", "Users who need to perform semantic and keyword searches")
    Person(admin, "System Administrator", "Manages the search system and documents")
    
    System(hybridSearch, "Hybrid Search System", "Combines semantic search (HyDE) with keyword search (BM25) for enhanced document retrieval")
    
    System_Ext(openai, "OpenAI API", "Provides text embeddings and chat completion for generating hypothetical documents")
    System_Ext(ollama, "Ollama", "Local LLM service for embeddings and chat completion as alternative to OpenAI")
    System_Ext(elasticsearch, "Elasticsearch", "External search engine for BM25 keyword search and document storage")
    
    Rel(user, hybridSearch, "Searches for documents", "HTTPS/REST API")
    Rel(admin, hybridSearch, "Manages documents and configurations", "HTTPS/REST API")
    Rel(hybridSearch, openai, "Generates embeddings and hypothetical documents", "HTTPS/API")
    Rel(hybridSearch, ollama, "Generates embeddings and hypothetical documents (alternative)", "HTTP/API")
    Rel(hybridSearch, elasticsearch, "Performs keyword search and stores documents", "HTTP/API")
    
    UpdateRelStyle(user, hybridSearch, $textColor="blue", $lineColor="blue", $offsetX="5")
    UpdateRelStyle(admin, hybridSearch, $textColor="green", $lineColor="green", $offsetX="5")
    UpdateRelStyle(hybridSearch, openai, $textColor="orange", $lineColor="orange", $offsetY="-10")
    UpdateRelStyle(hybridSearch, ollama, $textColor="purple", $lineColor="purple", $offsetY="-10")
    UpdateRelStyle(hybridSearch, elasticsearch, $textColor="red", $lineColor="red", $offsetY="10")
```

## Container Diagram (Level 2)

Shows the main containers and their interactions within the Hybrid Search system.

```mermaid
C4Container
    title Container Diagram for Hybrid Search System

    Person(user, "Search User", "Users performing searches")
    Person(admin, "System Administrator", "System management")
    
    Container_Boundary(hybridSystem, "Hybrid Search System") {
        Container(webApi, "Hybrid Search Web API", "ASP.NET Core Web API", "Provides REST API endpoints for hybrid search (BM25 + HyDE), document management, and system health")
        Container(appHost, "App Host", ".NET Aspire AppHost", "Orchestrates application services and dependencies including Elasticsearch")
        Container(serviceDefaults, "Service Defaults", ".NET Library", "Provides common service configurations, health checks, and telemetry")
        Container(documentStore, "In-Memory Document Store", "C# In-Memory Storage", "Stores documents and their embeddings for fast retrieval")
    }
    
    System_Ext(openai, "OpenAI API", "Text embeddings and chat completion")
    System_Ext(ollama, "Ollama Service", "Local LLM service")
    System_Ext(elasticsearch, "Elasticsearch", "Search engine and document storage")
    
    Rel(user, webApi, "Search requests", "HTTPS/REST")
    Rel(admin, webApi, "Document management", "HTTPS/REST")
    Rel(webApi, openai, "Generate embeddings/hypothetical docs", "HTTPS")
    Rel(webApi, ollama, "Generate embeddings/hypothetical docs (alt)", "HTTP")
    Rel(webApi, elasticsearch, "Keyword search (BM25)", "HTTP")
    Rel(webApi, documentStore, "Store/retrieve documents", "In-process")
    Rel(appHost, elasticsearch, "Orchestrates", "Container management")
    Rel(webApi, serviceDefaults, "Uses", "In-process")
    
    UpdateRelStyle(user, webApi, $textColor="blue", $lineColor="blue")
    UpdateRelStyle(admin, webApi, $textColor="green", $lineColor="green")
    UpdateRelStyle(webApi, openai, $textColor="orange", $lineColor="orange")
    UpdateRelStyle(webApi, ollama, $textColor="purple", $lineColor="purple")
    UpdateRelStyle(webApi, elasticsearch, $textColor="red", $lineColor="red")
```

## Component Diagram (Level 3)

Shows the internal components of the Hybrid Search Web API container.

```mermaid
C4Component
    title Component Diagram for Hybrid Search Web API

    Person(user, "Search User")
    
    Container_Boundary(webApi, "Hybrid Search Web API") {
        Component(endpoints, "REST API Endpoints", "ASP.NET Core Minimal APIs", "Handles HTTP requests for hybrid search, HyDE-only search, document management, and health checks")
        Component(hybridService, "Hybrid Search Service", "C# Service Class", "Orchestrates hybrid search combining BM25 keyword search with HyDE semantic search using weighted scoring")
        Component(hydeService, "HyDE Search Service", "C# Service Class", "Implements HyDE algorithm: combines query embedding with hypothetical document similarity")
        Component(embeddingService, "Embedding Service", "C# Service Class", "Generates text embeddings using AI providers (OpenAI/Ollama via Semantic Kernel)")
        Component(hypoDocGen, "Hypothetical Document Generator", "C# Service Class", "Generates hypothetical documents that would answer queries using LLMs")
        Component(vectorSimilarity, "Vector Similarity Service", "C# Service Class", "Calculates cosine similarity between document embeddings using System.Numerics.Tensors")
        Component(bm25Service, "BM25 Service", "C# Service Class", "Implements BM25 keyword search algorithm for in-memory documents")
        Component(elasticBM25, "Elasticsearch BM25 Service", "C# Service Class", "Provides BM25 keyword search using Elasticsearch backend")
        Component(documentStore, "Document Store", "C# Service Class", "Manages document storage and retrieval with embedding caching")
        Component(config, "Configuration Services", "C# Configuration Classes", "Manages AI, HyDE, BM25, and Hybrid search configurations")
        Component(mockServices, "Mock Services", "C# Service Classes", "Provides mock implementations for testing without AI providers")
    }
    
    Container_Ext(docStorage, "In-Memory Document Store", "Document storage")
    System_Ext(openai, "OpenAI API")
    System_Ext(ollama, "Ollama Service") 
    System_Ext(elasticsearch, "Elasticsearch")
    
    Rel(user, endpoints, "HTTP requests", "HTTPS/REST")
    Rel(endpoints, hybridService, "Hybrid search requests")
    Rel(endpoints, hydeService, "HyDE-only search requests")
    Rel(endpoints, documentStore, "Document CRUD operations")
    
    Rel(hybridService, hydeService, "Get HyDE semantic scores")
    Rel(hybridService, bm25Service, "Get BM25 keyword scores (in-memory)")
    Rel(hybridService, elasticBM25, "Get BM25 keyword scores (Elasticsearch)")
    
    Rel(hydeService, embeddingService, "Generate query embedding")
    Rel(hydeService, hypoDocGen, "Generate hypothetical document")
    Rel(hydeService, vectorSimilarity, "Calculate similarities")
    Rel(hydeService, documentStore, "Retrieve documents")
    
    Rel(embeddingService, openai, "Generate embeddings", "HTTPS")
    Rel(embeddingService, ollama, "Generate embeddings (alt)", "HTTP")
    Rel(embeddingService, mockServices, "Mock embeddings (fallback)", "In-process")
    Rel(hypoDocGen, openai, "Generate hypothetical docs", "HTTPS")
    Rel(hypoDocGen, ollama, "Generate hypothetical docs (alt)", "HTTP")
    Rel(hypoDocGen, mockServices, "Mock hypothetical docs (fallback)", "In-process")
    
    Rel(bm25Service, docStorage, "Access documents")
    Rel(elasticBM25, elasticsearch, "BM25 queries", "HTTP")
    Rel(documentStore, docStorage, "Store/retrieve")
    
    Rel(endpoints, config, "Load configurations")
    Rel(hybridService, config, "Hybrid search settings")
    Rel(hydeService, config, "HyDE settings")
    Rel(embeddingService, config, "AI settings")
    
    UpdateRelStyle(user, endpoints, $textColor="blue", $lineColor="blue")
    UpdateRelStyle(embeddingService, openai, $textColor="orange", $lineColor="orange")
    UpdateRelStyle(embeddingService, ollama, $textColor="purple", $lineColor="purple")
    UpdateRelStyle(elasticBM25, elasticsearch, $textColor="red", $lineColor="red")
```

## Key Architectural Patterns

### Hybrid Search Strategy
1. **Parallel Execution**: BM25 keyword search and HyDE semantic search run concurrently
2. **Score Normalization**: Different scoring strategies (MinMax, ZScore, None) for combining results
3. **Weighted Combination**: Configurable weights for balancing keyword vs semantic relevance (default: 30% BM25, 70% HyDE)
4. **Unified Ranking**: Final results ranked by combined weighted scores with configurable thresholds

### HyDE Algorithm Implementation
1. **Query Processing**: User query received via REST API endpoints
2. **Dual Embedding Strategy**: Generate embeddings for both original query and hypothetical document
3. **Hypothetical Document Generation**: LLM creates a document that would answer the query
4. **Vector Similarity Calculation**: Cosine similarity between embeddings and stored documents using System.Numerics.Tensors
5. **Score Combination**: Weighted combination of query-to-document and hypothetical-to-document similarities

### API Endpoints Structure
- **`POST /api/search/hybrid`**: Primary hybrid search endpoint combining BM25 and HyDE
- **`POST /api/search`**: HyDE-only semantic search endpoint
- **`GET /api/search/quick`**: Quick hybrid search with query parameter
- **`POST /api/documents`**: Document indexing for both BM25 and HyDE search
- **`GET /api/documents`**: Document count and management
- **`GET /api/health`**: System health check

### AI Provider Flexibility
- **Multiple Backends**: Support for OpenAI and Ollama as AI providers via Semantic Kernel
- **Fallback Strategy**: Mock services when no AI provider is configured for development/testing
- **Service Abstraction**: Interface-based design (`IEmbeddingService`, `IHypotheticalDocumentGenerator`) allows easy provider switching
- **Configuration-Driven**: Provider selection based on configuration with automatic fallback detection

### Storage and Indexing Options
- **In-Memory Document Store**: Fast document storage with embedding caching for development/testing
- **Elasticsearch Integration**: Production-ready storage with advanced BM25 capabilities via NEST client
- **Hybrid Storage Strategy**: Semantic embeddings in-memory, keyword search via Elasticsearch
- **Automatic Indexing**: Documents automatically indexed for both search types on ingestion

### Configuration Management
- **Layered Configuration**: Support for appsettings.json, environment variables, and user secrets
- **Provider-Specific Settings**: Separate configuration sections for OpenAI, Ollama, BM25, and Hybrid search
- **Runtime Configuration**: Settings can be adjusted without code changes
- **Validation and Defaults**: Comprehensive default values with runtime validation

### Performance and Scalability
- **Async/Await Pattern**: All operations use async programming for better scalability
- **Cancellation Token Support**: Proper cancellation handling throughout the pipeline
- **Efficient Vector Operations**: System.Numerics.Tensors for high-performance similarity calculations
- **Connection Pooling**: Elasticsearch client with connection pooling and configuration optimization
