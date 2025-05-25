<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

# HyDE Search Project Instructions

This is a C#/.NET 9 project implementing HyDE (Hypothetical Document Embeddings) search functionality.

## General

* Make only high confidence suggestions when reviewing code changes.
* Always use the latest version C#, currently C# 13 features.
* Never change global.json unless explicitly asked to.
* Never change package.json or package-lock.json files unless explicitly asked to.

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

## Formatting

* Prefer file-scoped namespace declarations and single-line using directives.
* Insert a newline before the opening curly brace of any code block (e.g., after `if`, `for`, `while`, `foreach`, `using`, `try`, etc.).
* Ensure that the final return statement of a method is on its own line.
* Use pattern matching and switch expressions wherever possible.
* Use `nameof` instead of string literals when referring to member names.
* Ensure that XML doc comments are created for any public APIs. When applicable, include `<example>` and `<code>` documentation in the comments.

### Nullable Reference Types

* Declare variables non-nullable, and check for `null` at entry points.
* Always use `is null` or `is not null` instead of `== null` or `!= null`.
* Trust the C# null annotations and don't add null checks when the type system says a value cannot be null.

### Testing

* We use xUnit SDK v3 for tests.
* Do not emit "Act", "Arrange" or "Assert" comments.
* Use Moq for mocking in tests.
* Copy existing style in nearby files for test method names and capitalization.

## Running tests

* To build and run tests in the repo, use the `build.sh` script that is located in each subdirectory within the `src` folder. For example, to run the build with tests in the `src/Http` directory, run `./src/Http/build.sh -test`.