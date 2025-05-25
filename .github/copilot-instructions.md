<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

# HyDE Search Project Instructions

This is a C#/.NET 9 project implementing HyDE (Hypothetical Document Embeddings) search functionality.

## Architecture Guidelines

- Use modern C# patterns including nullable reference types, record types, and async/await
- Follow dependency injection patterns with Microsoft.Extensions.DependencyInjection
- Implement interfaces for all services to enable testing and flexibility
- Use configuration options pattern for settings
- Leverage System.Numerics.Tensors for high-performance vector operations

## Key Components

- **Models**: Document, SearchResult, and configuration classes
- **Services**: 
  - IEmbeddingService: Generate text embeddings using OpenAI API
  - IHypotheticalDocumentGenerator: Create hypothetical documents from queries
  - IVectorSimilarityService: Calculate vector similarities efficiently
  - IDocumentStore: Store and retrieve documents
  - IHydeSearchService: Main search orchestration service

## HyDE Algorithm

The HyDE search combines traditional query-document similarity with hypothetical document similarity:
1. Generate embedding for the original query
2. Generate a hypothetical document that would answer the query
3. Generate embedding for the hypothetical document
4. Calculate similarities: query-to-document and hypothetical-to-document
5. Combine using weighted average for final ranking

## Code Style

- Use minimal APIs and modern C# features
- Prefer readonly and immutable data structures
- Include comprehensive logging with structured logging
- Handle cancellation tokens for async operations
- Use proper error handling and validation
