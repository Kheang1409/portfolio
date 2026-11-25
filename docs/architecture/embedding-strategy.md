# Embedding Strategy

## Overview

The portfolio assistant uses **Google Gemini `gemini-embedding-001`** for semantic retrieval. This document covers the embedding model, integration, fallback behavior, and operational considerations.

---

## Model Specifications

| Property | Value |
|----------|-------|
| **Model** | `gemini-embedding-001` |
| **Dimensions** | 768 |
| **Max Input Tokens** | 2048 |
| **Similarity Metric** | Cosine similarity |
| **API Endpoint** | `https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-001:embedContent` |
| **Pricing** | Free tier available; pay-as-you-go beyond |

---

## Integration

### Embedding Service Interface

```csharp
public interface IEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default);
    Task<float[][]> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken ct = default);
    Task<float[]> GenerateQueryEmbeddingAsync(string query, CancellationToken ct = default);
    bool IsAvailable { get; }
}
```

### Implementation (Gemini)

```csharp
public class GeminiEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiSettings _settings;
    private readonly ILogger<GeminiEmbeddingService> _logger;

    public bool IsAvailable => !string.IsNullOrEmpty(_settings.ApiKey);

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        if (!IsAvailable) throw new InvalidOperationException("Gemini API key not configured");

        var payload = new
        {
            model = "models/gemini-embedding-001",
            content = new { parts = new[] { new { text = Truncate(text, 2000) } } }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, _settings.EmbeddingEndpoint)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("x-goog-api-key", _settings.ApiKey);

        using var response = await _httpClient.SendAsync(request, ct);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Gemini embedding failed: {Status} - {Error}", response.StatusCode, error);
            throw new HttpRequestException($"Embedding API error: {response.StatusCode}");
        }

        var json = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: ct);
        return json?.Embedding?.Values?.Select(v => (float)v).ToArray() ?? Array.Empty<float>();
    }

    public async Task<float[][]> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken ct = default)
    {
        var results = new List<float[]>();
        foreach (var text in texts)
        {
            try
            {
                results.Add(await GenerateEmbeddingAsync(text, ct));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to embed text, using zero vector");
                results.Add(new float[768]); // Zero vector as placeholder
            }
        }
        return results.ToArray();
    }

    public async Task<float[]> GenerateQueryEmbeddingAsync(string query, CancellationToken ct = default)
    {
        // Queries use same model; prepend instruction for better retrieval
        var prefixedQuery = $"Represent this query for retrieving relevant portfolio information: {query}";
        return await GenerateEmbeddingAsync(prefixedQuery, ct);
    }
}
```

---

## Chunking for Embeddings

**Input**: Knowledge chunks (~500 chars each, ~100 tokens)

**Strategy**:
- One embedding per knowledge chunk
- Chunk size optimized for 768-dim model (well under 2048 token limit)
- Title + Section prepended to content for context

```csharp
string textForEmbedding = $"{chunk.Section}: {chunk.Title}\n{chunk.Content}";
var embedding = await _embeddingService.GenerateEmbeddingAsync(textForEmbedding);
```

---

## Storage

### MongoDB Document

```csharp
public class KnowledgeChunk
{
    [BsonId]
    public ObjectId Id { get; set; }
    public string ResumeHash { get; set; }
    public int ChunkIndex { get; set; }
    public string Section { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public string[] Keywords { get; set; }
    public float[]? Embedding { get; set; }  // 768 floats = ~3KB per chunk
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**Storage Cost**: ~3KB/chunk × ~50 chunks/resume = ~150KB/resume (negligible)

---

## Vector Search (MongoDB Atlas)

### Index Definition

```json
{
  "name": "knowledge_embeddings",
  "type": "vectorSearch",
  "definition": {
    "fields": [
      {
        "type": "vector",
        "path": "Embedding",
        "numDimensions": 768,
        "similarity": "cosine"
      },
      {
        "type": "filter",
        "path": "ResumeHash"
      }
    ]
  }
}
```

### Query Pipeline

```javascript
[
  {
    $vectorSearch: {
      index: "knowledge_embeddings",
      path: "Embedding",
      queryVector: [0.1, -0.3, ...],  // 768-dim query embedding
      numCandidates: 100,
      limit: 10,
      filter: { ResumeHash: "abc123..." }
    }
  },
  {
    $project: {
      Content: 1,
      Section: 1,
      Title: 1,
      Keywords: 1,
      score: { $meta: "vectorSearchScore" }
    }
  }
]
```

---

## Fallback Behavior

### When Embeddings Unavailable

| Scenario | Behavior |
|----------|----------|
| `GEMINI_API_KEY` not set | `IsAvailable = false`; semantic retrieval skipped |
| API quota exceeded | Log warning; keyword-only retrieval |
| Network timeout | Retry with exponential backoff (3x); then keyword-only |
| Invalid response | Log error; keyword-only |

**Code Path**:
```csharp
public async Task<RetrievalResult> RetrieveAsync(string query, int maxResults = 5)
{
    var keywordResults = await _keywordRetriever.RetrieveAsync(query, maxResults);
    
    var semanticResults = new List<KnowledgeChunk>();
    if (_embeddingService.IsAvailable)
    {
        try 
        {
            var queryEmbedding = await _embeddingService.GenerateQueryEmbeddingAsync(query);
            semanticResults = await _semanticRetriever.RetrieveAsync(queryEmbedding, maxResults);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Semantic retrieval failed, falling back to keyword-only");
            // Continue with keyword results only
        }
    }

    return CombineAndRank(keywordResults, semanticResults);
}
```

---

## Operational Considerations

### Latency

| Operation | Typical Latency |
|-----------|-----------------|
| Single embedding (API) | 100-300ms |
| Batch (10 chunks) | 500-1500ms |
| Query embedding | 100-200ms |
| Vector search (Atlas) | 50-150ms |

**Optimization**: Batch embeddings at startup; cache query embeddings for repeated queries.

### Cost Management

- **Free Tier**: 1,500 requests/day (as of 2024)
- **Production**: ~50 chunks × 1 embedding = 50 requests per resume update
- **Query**: 1 embedding per user query

**Monitoring**:
```csharp
// Metrics emitted
_counterEmbeddingRequests.Add(1, new("model", "gemini-embedding-001"));
_counterEmbeddingErrors.Add(1, new("error_type", ex.GetType().Name));
_histogramEmbeddingLatency.Record(elapsedMs);
```

### Versioning

If model changes (dimensions, semantics):
1. New resume hash → full re-embedding at next bootstrap
2. Old embeddings remain but won't match new query embeddings
3. Hybrid retrieval gracefully degrades to keyword-only during transition

---

## Testing

### Unit Tests

```csharp
[Fact]
public async Task GenerateEmbeddingAsync_Returns768Dimensions()
{
    // Arrange
    var handler = new MockHttpMessageHandler();
    handler.When("*/models/gemini-embedding-001:embedContent")
        .RespondJson(new { embedding = new { values = Enumerable.Repeat(0.1, 768) } });
    
    var service = new GeminiEmbeddingService(handler.ToHttpClient(), Settings, logger);

    // Act
    var embedding = await service.GenerateEmbeddingAsync("test text");

    // Assert
    embedding.Should().HaveCount(768);
}
```

### Integration Tests

```csharp
[Fact]
public async Task SemanticRetrieval_ReturnsRelevantChunks()
{
    // Requires real Gemini API key (mark as [Integration])
    // Insert test chunks with known embeddings
    // Query with related text
    // Assert cosine similarity > 0.75
}
```

---

## Migration Guide

### From Previous Model (if applicable)

1. Update `GEMINI_MODEL_NAMES` to include new embedding model
2. Deploy — next resume update triggers re-embedding
3. Monitor `EmbeddingCoverage` metric → should reach 100%
4. Old embeddings automatically ignored (hash mismatch)

---

## Related Documents

- [RAG Architecture](rag-architecture.md) — Retrieval pipeline
- [AI Pipeline](ai-pipeline.md) — Full AI flow
- [Corpus Management](corpus-management.md) — Knowledge base operations
- [Resilience Patterns](../backend/resilience.md) — Retry policies for embedding API