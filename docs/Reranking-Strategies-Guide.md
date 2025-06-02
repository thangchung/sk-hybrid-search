# Reranking Strategies Guide

The `HybridSearchService` supports multiple reranking strategies to combine BM25 keyword search and HyDE semantic search results. Each strategy has different characteristics and use cases.

## Available Strategies

### 1. WeightedSum (Default)
**Configuration**: `"RerankingStrategy": "WeightedSum"`

Simple weighted combination of normalized scores:
```
CombinedScore = (NormalizedBM25Score * BM25Weight) + (NormalizedHydeScore * HydeWeight)
```

**Best for**: General purpose, when you trust both search methods equally and want predictable scoring.

### 2. Reciprocal Rank Fusion (RRF)
**Configuration**: 
```json
"RerankingStrategy": "ReciprocalRankFusion",
"RrfK": 60.0
```

Rank-based fusion using the formula:
```
RRF(document) = Σ(Weight / (k + rank(document)))
```

**Best for**: When you care more about rank positions than absolute scores. Reduces the impact of score magnitude differences between systems.

### 3. CombSum
**Configuration**: `"RerankingStrategy": "CombSum"`

Sum of normalized scores from both systems:
```
CombinedScore = NormalizedBM25Score + NormalizedHydeScore
```

**Best for**: Equal weighting of both search methods, treating them as equally important.

### 4. CombMax
**Configuration**: `"RerankingStrategy": "CombMax"`

Takes the maximum of normalized scores:
```
CombinedScore = Max(NormalizedBM25Score, NormalizedHydeScore)
```

**Best for**: When you want to find documents that perform well in at least one search method.

### 5. BordaCount
**Configuration**: `"RerankingStrategy": "BordaCount"`

Rank-based voting method where each system votes for documents:
```
BordaScore = (WeightedRankFromBM25) + (WeightedRankFromHyDE)
```

**Best for**: Democratic ranking where position matters more than score differences.

## Configuration Examples

### Using RRF (Recommended for most cases)
```json
{
  "HybridSearch": {
    "BM25Weight": 0.3,
    "HydeWeight": 0.7,
    "RerankingStrategy": "ReciprocalRankFusion",
    "RrfK": 60.0,
    "NormalizationStrategy": "MinMax"
  }
}
```

### Using Borda Count for Democratic Ranking
```json
{
  "HybridSearch": {
    "BM25Weight": 0.5,
    "HydeWeight": 0.5,
    "RerankingStrategy": "BordaCount",
    "NormalizationStrategy": "MinMax"
  }
}
```

### Using CombMax for Best-of-Both
```json
{
  "HybridSearch": {
    "RerankingStrategy": "CombMax",
    "NormalizationStrategy": "MinMax"
  }
}
```

## Testing Different Strategies

You can test different strategies by modifying the `appsettings.json` configuration and observing the results:

1. **Index some test documents**
2. **Perform the same search with different strategies**
3. **Compare the ranking and scores**

The logs will show which strategy is being used:
```
Hybrid search found X results for query: 'your query' using ReciprocalRankFusion reranking
```

## Performance Considerations

- **WeightedSum**: Fastest, simple computation
- **RRF**: Fast, requires sorting but efficient rank calculation  
- **CombSum/CombMax**: Fast, simple arithmetic operations
- **BordaCount**: Moderate, requires rank calculation for both systems

All strategies are optimized for performance and can handle large result sets efficiently.
