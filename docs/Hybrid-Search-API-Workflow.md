# Hybrid Search API Workflow

This document shows the complete workflow of the Hybrid Search API implemented in this project, combining BM25 keyword search with HyDE (Hypothetical Document Embeddings) semantic search.

## Architecture Overview

```mermaid
graph TB
    %% API Layer
    Client[Client Application] --> API[Program.cs<br/>Minimal API Endpoints]
    
    %% Main Service Layer
    API --> HS[HybridSearchService.cs<br/>IHybridSearchService]
    
    %% Search Components
    HS --> BM25[BM25Service.cs<br/>IBM25Service]
    HS --> HYDE[HydeSearchService.cs<br/>IHydeSearchService]
    
    %% HyDE Components
    HYDE --> EMB[EmbeddingService.cs<br/>IEmbeddingService]
    HYDE --> HDG[HypotheticalDocumentGenerator.cs<br/>IHypotheticalDocumentGenerator]
    HYDE --> VS[VectorSimilarityService.cs<br/>IVectorSimilarityService]
    HYDE --> DS[DocumentStore.cs<br/>IDocumentStore]
    
    %% AI Services
    EMB --> SK[Semantic Kernel<br/>ITextEmbeddingGenerationService]
    HDG --> SKCHAT[Semantic Kernel<br/>IChatCompletionService]
    
    %% External Services
    SK --> OPENAI[OpenAI API<br/>text-embedding-3-small]
    SK --> OLLAMA[Ollama<br/>nomic-embed-text]
    SKCHAT --> OPENAI2[OpenAI API<br/>gpt-4o-mini]
    SKCHAT --> OLLAMA2[Ollama<br/>llama3.2]
    
    %% Alternative BM25
    BM25 --> ES[ElasticSearchBM25Service.cs<br/>IBM25Service]
    ES --> ELASTIC[Elasticsearch<br/>BM25 Engine]
    
    %% Configuration
    CONFIG[SearchConfiguration.cs<br/>Configuration Classes] --> HS
    CONFIG --> HYDE
    CONFIG --> BM25
    
    %% Models
    MODELS[Models/<br/>Document.cs<br/>SearchResult.cs<br/>ApiModels.cs] --> HS
    
    classDef apiClass fill:#e1f5fe
    classDef serviceClass fill:#f3e5f5
    classDef aiClass fill:#e8f5e8
    classDef externalClass fill:#fff3e0
    classDef configClass fill:#fce4ec
    classDef modelClass fill:#f1f8e9
    
    class API apiClass
    class HS,HYDE,BM25,EMB,HDG,VS,DS,ES serviceClass
    class SK,SKCHAT aiClass
    class OPENAI,OPENAI2,OLLAMA,OLLAMA2,ELASTIC externalClass
    class CONFIG configClass
    class MODELS modelClass
```

## Hybrid Search Request Flow

```mermaid
sequenceDiagram
    participant C as Client
    participant API as Program.cs
    participant HS as HybridSearchService.cs
    participant BM25 as BM25Service.cs
    participant HYDE as HydeSearchService.cs
    participant EMB as EmbeddingService.cs
    participant HDG as HypotheticalDocumentGenerator.cs
    participant VS as VectorSimilarityService.cs
    participant DS as DocumentStore.cs
    participant AI as AI Provider (OpenAI/Ollama)
    
    C->>API: POST /api/search/hybrid<br/>{query, maxResults}
    API->>HS: SearchAsync(query)
    
    Note over HS: Check configuration:<br/>EnableBM25, EnableHyDE
    
    HS->>DS: GetAllDocumentsAsync()
    DS-->>HS: List<Document>
    
    par BM25 Search (if enabled)
        HS->>BM25: CalculateScoresAsync(query, documents)
        BM25-->>HS: List<BM25Result>
    and HyDE Search (if enabled)
        HS->>HYDE: SearchAsync(query)
        
        HYDE->>EMB: GetEmbeddingAsync(query)
        EMB->>AI: Generate query embedding
        AI-->>EMB: float[] queryEmbedding
        EMB-->>HYDE: queryEmbedding
        
        HYDE->>HDG: GenerateHypotheticalDocumentAsync(query)
        HDG->>AI: Generate hypothetical document
        AI-->>HDG: string hypotheticalDoc
        HDG-->>HYDE: hypotheticalDoc
        
        HYDE->>EMB: GetEmbeddingAsync(hypotheticalDoc)
        EMB->>AI: Generate hypothetical doc embedding
        AI-->>EMB: float[] hydeEmbedding
        EMB-->>HYDE: hydeEmbedding
        
        loop For each document
            HYDE->>VS: CalculateCosineSimilarity(queryEmbedding, docEmbedding)
            VS-->>HYDE: traditionalSimilarity
            HYDE->>VS: CalculateCosineSimilarity(hydeEmbedding, docEmbedding)
            VS-->>HYDE: hydeSimilarity
            Note over HYDE: Combine: (traditional * weight) +<br/>(hyde * weight)
        end
        
        HYDE-->>HS: List<SearchResult>
    end
    
    Note over HS: Normalize Scores:<br/>MinMax or ZScore
    
    Note over HS: Combine Results:<br/>BM25Weight + HydeWeight
    
    Note over HS: Filter by ScoreThreshold<br/>Sort by CombinedScore<br/>Take MaxResults
    
    HS-->>API: List<HybridSearchResult>
    API-->>C: HybridSearchResponse<br/>{results, metrics, processingTime}
```

## Document Indexing Flow

```mermaid
sequenceDiagram
    participant C as Client
    participant API as Program.cs
    participant HS as HybridSearchService.cs
    participant HYDE as HydeSearchService.cs
    participant BM25 as BM25Service.cs
    participant EMB as EmbeddingService.cs
    participant DS as DocumentStore.cs
    participant AI as AI Provider
    
    C->>API: POST /api/documents<br/>Document[]
    API->>HS: IndexDocumentsAsync(documents)
    
    HS->>HYDE: IndexDocumentsAsync(documents)
    
    loop For each document
        HYDE->>EMB: GetEmbeddingAsync(document.Content)
        EMB->>AI: Generate embedding
        AI-->>EMB: float[] embedding
        EMB-->>HYDE: embedding
        
        Note over HYDE: document.Embedding = embedding
        
        HYDE->>DS: AddDocumentAsync(document)
        DS-->>HYDE: success
    end
    
    HYDE-->>HS: success
    
    Note over HS: If BM25 enabled
    HS->>BM25: IndexDocumentsAsync(documents)
    Note over BM25: Calculate term frequencies<br/>Document statistics
    BM25-->>HS: success
    
    HS-->>API: success
    API-->>C: {message, totalDocuments}
```

## Component Responsibilities

### Core Services (.cs files)

| Component | File | Responsibility |
|-----------|------|----------------|
| **API Layer** | `Program.cs` | Minimal API endpoints, DI configuration, middleware setup |
| **Hybrid Search** | `HybridSearchService.cs` | Orchestrates BM25 + HyDE search, score normalization and combination |
| **HyDE Search** | `HydeSearchService.cs` | Implements HyDE algorithm, manages semantic search workflow |
| **BM25 Search** | `BM25Service.cs` | In-memory BM25 keyword search implementation |
| **Elasticsearch BM25** | `ElasticSearchBM25Service.cs` | Elasticsearch-based BM25 search implementation |
| **Embeddings** | `EmbeddingService.cs` | Text-to-vector conversion using AI models |
| **Hypothetical Docs** | `HypotheticalDocumentGenerator.cs` | Generates hypothetical documents from queries |
| **Vector Similarity** | `VectorSimilarityService.cs` | Cosine similarity calculations using System.Numerics.Tensors |
| **Document Store** | `DocumentStore.cs` | In-memory document storage with embeddings |

### Configuration

| Component | File | Purpose |
|-----------|------|---------|
| **Search Config** | `SearchConfiguration.cs` | AI providers, HyDE parameters, BM25 settings, hybrid weights |

### Models

| Component | File | Purpose |
|-----------|------|---------|
| **Core Models** | `Document.cs` | Document structure with embeddings |
| **Search Results** | `SearchResult.cs` | HyDE and Hybrid search result structures |
| **API Models** | `ApiModels.cs` | Request/response DTOs for API endpoints |

## Search Algorithms

### HyDE (Hypothetical Document Embeddings)
1. **Query Processing**: Generate embedding for original query
2. **Hypothetical Generation**: Create a hypothetical document that would answer the query
3. **Hypothetical Embedding**: Generate embedding for hypothetical document
4. **Similarity Calculation**: 
   - Traditional: query ↔ document similarity
   - HyDE: hypothetical document ↔ document similarity
5. **Score Combination**: Weighted average of traditional + HyDE similarities

### BM25 (Best Matching 25)
1. **Term Extraction**: Tokenize query and documents
2. **TF-IDF Calculation**: Term frequency with document frequency normalization
3. **BM25 Scoring**: Advanced TF-IDF with length normalization and saturation
4. **Ranking**: Sort documents by BM25 relevance score

### Hybrid Search
1. **Parallel Execution**: Run BM25 and HyDE searches simultaneously
2. **Score Normalization**: MinMax or Z-Score normalization of individual scores
3. **Weighted Combination**: Combine normalized scores using configurable weights
4. **Final Ranking**: Sort by combined score and apply threshold filtering

## Configuration Options

### Hybrid Search Settings
- **EnableBM25**: Toggle BM25 keyword search
- **EnableHyDE**: Toggle HyDE semantic search  
- **BM25Weight**: Weight for BM25 scores in combination (default: 0.4)
- **HydeWeight**: Weight for HyDE scores in combination (default: 0.6)
- **NormalizationStrategy**: MinMax, ZScore, or None
- **ScoreThreshold**: Minimum combined score for results
- **MaxResults**: Maximum number of results to return

### AI Provider Options
- **OpenAI**: GPT-4o-mini for text generation, text-embedding-3-small for embeddings
- **Ollama**: llama3.2 for text generation, nomic-embed-text for embeddings
- **Mock Services**: For testing without AI providers

### BM25 Provider Options
- **In-Memory**: Built-in BM25 implementation
- **Elasticsearch**: External Elasticsearch cluster with BM25 ranking
