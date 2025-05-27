# ElasticSearch BM25 Provider

This document describes the ElasticSearch BM25 provider implementation for the HyDE Search project.

## Overview

The ElasticSearch BM25 provider offers a distributed, scalable alternative to the in-memory BM25 implementation. It leverages ElasticSearch's native BM25 scoring algorithm and provides the following benefits:

- **Scalability**: Handle large document collections efficiently
- **Persistence**: Documents are stored and indexed in ElasticSearch
- **Performance**: Optimized search performance with ElasticSearch's distributed architecture
- **Advanced Features**: Benefit from ElasticSearch's rich query capabilities

## Configuration

### BM25 Provider Selection

Configure the BM25 provider in `appsettings.json`:

```json
{
  "BM25": {
    "Provider": "ElasticSearch",  // or "InMemory"
    "ElasticSearch": {
      "ConnectionString": "http://localhost:9200",
      "IndexName": "hyde-search",
      "Username": "",
      "Password": "",
      "EnableSSL": false,
      "VerifySSL": true
    }
  }
}
```

### Configuration Options

| Property | Description | Default |
|----------|-------------|---------|
| `Provider` | BM25 provider: "InMemory" or "ElasticSearch" | "InMemory" |
| `ConnectionString` | ElasticSearch connection URL | "http://localhost:9200" |
| `IndexName` | ElasticSearch index name for documents | "hyde-search" |
| `Username` | ElasticSearch username (if authentication enabled) | "" |
| `Password` | ElasticSearch password (if authentication enabled) | "" |
| `EnableSSL` | Enable SSL/TLS connection | false |
| `VerifySSL` | Verify SSL certificates | true |

## Running with ElasticSearch

### Using .NET Aspire (Recommended)

The project includes .NET Aspire integration for easy development:

```bash
# Start the application with Aspire
dotnet run --project AppHost
```

This automatically starts:
- ElasticSearch container on port 9200
- HyDE Search API with ElasticSearch integration

### Manual ElasticSearch Setup

If not using Aspire, start ElasticSearch manually:

```bash
# Start ElasticSearch container
docker run -d --name elasticsearch \
  -p 9200:9200 \
  -e "discovery.type=single-node" \
  -e "xpack.security.enabled=false" \
  -e "ES_JAVA_OPTS=-Xms512m -Xmx512m" \
  docker.elastic.co/elasticsearch/elasticsearch:8.11.0

# Start the HyDE Search API
dotnet run --project HydeSearch
```

### Configuration for Production

For production environments, consider these additional configurations:

```json
{
  "BM25": {
    "Provider": "ElasticSearch",
    "ElasticSearch": {
      "ConnectionString": "https://your-elasticsearch-cluster:9200",
      "IndexName": "production-hyde-search",
      "Username": "your-username",
      "Password": "your-password",
      "EnableSSL": true,
      "VerifySSL": true
    }
  }
}
```

## Implementation Details

### ElasticSearchBM25Service

The `ElasticSearchBM25Service` implements the `IBM25Service` interface and provides:

- **Document Indexing**: Automatically creates and manages ElasticSearch indices
- **BM25 Scoring**: Uses ElasticSearch's native BM25 implementation
- **Field Boosting**: Title fields receive 2x boost for better relevance
- **Multi-field Search**: Searches across title, content, and combined text fields
- **Error Handling**: Comprehensive logging and error handling

### Index Structure

The ElasticSearch index uses the following mapping:

```json
{
  "mappings": {
    "properties": {
      "id": { "type": "keyword" },
      "title": { 
        "type": "text", 
        "analyzer": "standard_analyzer",
        "fields": { "keyword": { "type": "keyword" } }
      },
      "content": { 
        "type": "text", 
        "analyzer": "standard_analyzer" 
      },
      "searchableText": { 
        "type": "text", 
        "analyzer": "standard_analyzer" 
      },
      "metadata": { 
        "type": "object", 
        "enabled": false 
      }
    }
  }
}
```

### Search Strategy

The ElasticSearch provider uses a multi-match query with the following strategy:

1. **Multi-Match Query**: Searches across title, content, and searchableText fields
2. **Field Boosting**: Title field receives 2.0 boost
3. **Best Fields**: Uses `best_fields` type for optimal scoring
4. **Document Filtering**: Results are filtered to match the requested document set
5. **Score Tracking**: ElasticSearch scores are preserved and returned

## Testing

Use the provided test script to verify the ElasticSearch integration:

```bash
# Run the ElasticSearch BM25 provider test
.\test-elasticsearch-bm25.ps1
```

The test script:
1. Builds the application
2. Starts ElasticSearch if needed
3. Tests both in-memory and ElasticSearch providers
4. Verifies document indexing and search functionality

## Benefits vs In-Memory

| Feature | In-Memory | ElasticSearch |
|---------|-----------|---------------|
| Setup Complexity | Simple | Moderate |
| Scalability | Limited by RAM | Highly scalable |
| Persistence | None | Full persistence |
| Performance | Fast for small datasets | Optimized for large datasets |
| Advanced Features | Basic BM25 | Rich query capabilities |
| Resource Usage | High memory | Distributed processing |

## Troubleshooting

### Common Issues

1. **Connection Failed**: Ensure ElasticSearch is running and accessible
2. **Index Creation Failed**: Check ElasticSearch permissions and cluster health
3. **Search Timeout**: Verify cluster performance and index size
4. **Authentication Error**: Verify username/password if security is enabled

### Logs

Enable debug logging to troubleshoot issues:

```json
{
  "Logging": {
    "LogLevel": {
      "HydeSearch.Services.ElasticSearchBM25Service": "Debug"
    }
  }
}
```

### Health Checks

Check ElasticSearch health:

```bash
# Cluster health
curl http://localhost:9200/_cluster/health

# Index information
curl http://localhost:9200/hyde-search/_stats
```
