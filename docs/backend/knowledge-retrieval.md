# Backend Knowledge & Retrieval

## Overview

The assistant's knowledge comes from the **latest resume document** in MongoDB, indexed at startup via `KnowledgeBootstrapHostedService`.

---

## Knowledge Bootstrap (Startup)

```csharp
public class KnowledgeBootstrapHostedService : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // 1. Get latest resume
        var resume = await _resumeRepository.GetLatestAsync(cancellationToken);
        if (resume == null) return;

        // 2. Compute content hash
        var contentHash = ComputeResumeHash(resume);
        
        // 3. Check if already indexed
        if (await _indexStateRepository.IsCurrentAsync(contentHash, cancellationToken))
            return;

        // 4. Chunk resume
        var chunks = _chunker.Chunk(resume).ToList();

        // 5. Extract keywords for each chunk
        foreach (var chunk in chunks)
        {
            chunk.Keywords = ExtractKeywords(chunk.Content);
        }

        // 6. Upsert keyword chunks (always)
        await _knowledgeChunkRepository.UpsertManyAsync(chunks, cancellationToken);

        // 7. Generate embeddings (best-effort)
        if (_embeddingService.IsAvailable)
        {
            try
            {
                var embeddings = await _embeddingService.GenerateEmbeddingsAsync(
                    chunks.Select(c => $"{c.Section}: {c.Title}\n{c.Content}"), 
                    cancellationToken);
                
                await _knowledgeChunkRepository.UpdateEmbeddingsAsync(chunks, embeddings, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Embedding generation failed; keyword-only retrieval active");
            }
        }

        // 8. Persist hash
        await _indexStateRepository.SetCurrentAsync(contentHash, cancellationToken);
    }
}
```

**Idempotency**: Hash-based — only re-indexes when resume content changes.

---

## Chunking Strategy

```csharp
public class ResumeChunker
{
    public IEnumerable<KnowledgeChunk> Chunk(Resume resume)
    {
        var chunks = new List<KnowledgeChunk>();

        // Profile
        chunks.AddRange(CreateChunks("profile", "Profile", 
            $"{resume.Profile.FullName} — {resume.Profile.Headline}\n{resume.Profile.Summary}"));

        // Skills (by category)
        foreach (var category in resume.Skills.GroupBy(s => s.Category))
        {
            chunks.AddRange(CreateChunks("skills", $"Skills: {category.Key}", 
                string.Join(", ", category.Select(s => s.Name))));
        }

        // Experience (per role)
        foreach (var exp in resume.Experience)
        {
            var content = $"{exp.Role} at {exp.Company} ({exp.StartDate:yyyy}–{exp.EndDate?.ToString("yyyy") ?? "Present"})\n" +
                          $"{exp.Description}\nTech: {string.Join(", ", exp.Technologies)}";
            chunks.AddRange(CreateChunks("experience", exp.Role, content));
        }

        // Projects (per project)
        foreach (var proj in resume.Projects)
        {
            var content = $"{proj.Name}: {proj.Description}\nTech: {string.Join(", ", proj.Technologies)}";
            chunks.AddRange(CreateChunks("projects", proj.Name, content));
        }

        // Education
        foreach (var edu in resume.Education)
        {
            chunks.AddRange(CreateChunks("education", edu.Degree, 
                $"{edu.Degree} in {edu.Field} from {edu.Institution} ({edu.StartYear}–{edu.EndYear})"));
        }

        // Certifications
        foreach (var cert in resume.Certifications)
        {
            chunks.AddRange(CreateChunks("certifications", cert.Name, 
                $"{cert.Name} from {cert.Issuer} ({cert.Year})"));
        }

        return chunks.Select((c, i) => c with { ChunkIndex = i });
    }

    private IEnumerable<KnowledgeChunk> CreateChunks(string section, string title, string content)
    {
        // Split by ~500 chars at sentence boundaries
        var sentences = SplitSentences(content);
        var current = new StringBuilder();
        
        foreach (var sentence in sentences)
        {
            if (current.Length + sentence.Length > 500 && current.Length > 0)
            {
                yield return new KnowledgeChunk 
                { Section = section, Title = title, Content = current.ToString().Trim() };
                current.Clear();
            }
            current.Append(sentence).Append(' ');
        }
        
        if (current.Length > 0)
            yield return new KnowledgeChunk 
            { Section = section, Title = title, Content = current.ToString().Trim() };
    }
}
```

**Output**: ~40-60 chunks per resume, ~500 chars each.

---

## Keyword Extraction

```csharp
public string[] ExtractKeywords(string text)
{
    var stopWords = new HashSet<string> { "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by", "from", "as", "is", "was", "are", "were", "been", "be", "have", "has", "had", "do", "does", "did", "will", "would", "could", "should", "may", "might", "can", "this", "that", "these", "those" };
    
    var words = Regex.Matches(text.ToLowerInvariant(), @"\b[a-z]{3,}\b")
        .Cast<Match>()
        .Select(m => m.Value)
        .Where(w => !stopWords.Contains(w))
        .GroupBy(w => w)
        .OrderByDescending(g => g.Count())
        .Take(15)
        .Select(g => g.Key)
        .ToArray();

    // Boost known tech terms
    var techTerms = new[] { "c#", ".net", "asp.net", "azure", "aws", "docker", "kubernetes", "mongodb", "redis", "sql", "nosql", "microservices", "api", "rest", "graphql", "react", "next.js", "typescript", "javascript", "python", "java", "go", "rust", "ai", "ml", "llm", "rag", "embedding", "vector", "gemini", "paypal", "stripe", "ci/cd", "github", "gitlab" };
    
    return words.Union(techTerms.Where(t => text.Contains(t, StringComparison.OrdinalIgnoreCase))).ToArray();
}
```

---

## Retrieval Pipeline

### Hybrid Retriever

```csharp
public class HybridRetriever : IRetriever
{
    public async Task<RetrievalResult> RetrieveAsync(string query, int maxResults = 5)
    {
        // 1. Keyword retrieval (always)
        var keywordResults = await _keywordRetriever.RetrieveAsync(query, maxResults);

        // 2. Semantic retrieval (if available)
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
                _logger.LogWarning(ex, "Semantic retrieval failed");
            }
        }

        // 3. Combine & rank
        return CombineAndRank(keywordResults, semanticResults, maxResults);
    }
}
```

### Keyword Retrieval (MongoDB Text Search)

```csharp
public class KeywordRetriever : IKeywordRetriever
{
    public async Task<List<KnowledgeChunk>> RetrieveAsync(string query, int maxResults)
    {
        var filter = Builders<KnowledgeChunk>.Filter.Text(PreprocessQuery(query));
        var projection = Builders<KnowledgeChunk>.Projection
            .MetaTextScore("score")
            .Include(c => c.Content)
            .Include(c => c.Section)
            .Include(c => c.Title)
            .Include(c => c.Keywords);

        var results = await _collection
            .Find(filter)
            .Project<KnowledgeChunkWithScore>(projection)
            .SortByDescendingMetaTextScore("score")
            .Limit(maxResults)
            .ToListAsync();

        return results.Select(r => r.Chunk with { EvidenceStrength = EvidenceStrength.Strong }).ToList();
    }

    private string PreprocessQuery(string query)
    {
        // Expand common abbreviations
        return query
            .ToLowerInvariant()
            .Replace(".net", "dotnet")
            .Replace("js", "javascript")
            .Replace("k8s", "kubernetes")
            .Replace("ci/cd", "cicd");
    }
}
```

### Semantic Retrieval (Vector Search)

```csharp
public class SemanticRetriever : ISemanticRetriever
{
    public async Task<List<KnowledgeChunk>> RetrieveAsync(float[] queryEmbedding, int maxResults)
    {
        var pipeline = new[]
        {
            new BsonDocument("$vectorSearch", new BsonDocument
            {
                { "index", "knowledge_embeddings" },
                { "path", "Embedding" },
                { "queryVector", new BsonArray(queryEmbedding) },
                { "numCandidates", 100 },
                { "limit", maxResults }
            }),
            new BsonDocument("$project", new BsonDocument
            {
                { "Content", 1 },
                { "Section", 1 },
                { "Title", 1 },
                { "Keywords", 1 },
                { "score", new BsonDocument("$meta", "vectorSearchScore") }
            })
        };

        var results = await _collection.Aggregate<KnowledgeChunkWithScore>(pipeline).ToListAsync();
        
        return results
            .Where(r => r.Score >= 0.75f)
            .Select(r => r.Chunk with { EvidenceStrength = EvidenceStrength.Weak })
            .ToList();
    }
}
```

---

## Evidence Classification

| Strength | Source | Weight | Usage |
|----------|--------|--------|-------|
| **Strong** | Keyword match (text search) | 1.0 | Primary evidence, explicit citation |
| **Weak** | Semantic similarity (vector) | 0.5-0.9 | Supplementary, marked as "reference only" |

---

## Context Building

```csharp
public class ContextBuilder
{
    public string BuildContext(RetrievalResult retrieval, AssistantRequest request)
    {
        var context = new StringBuilder();

        // 1. System prompt (highest priority)
        context.AppendLine(SYSTEM_PROMPT);
        context.AppendLine();

        // 2. Strong evidence (keyword)
        if (retrieval.StrongEvidence.Any())
        {
            context.AppendLine("=== STRONG EVIDENCE (KEYWORD MATCHES) ===");
            foreach (var chunk in retrieval.StrongEvidence.Take(3))
            {
                context.AppendLine($"[Source: {chunk.Section}] {chunk.Content}");
            }
            context.AppendLine();
        }

        // 3. Weak evidence (semantic)
        if (retrieval.WeakEvidence.Any())
        {
            context.AppendLine("=== WEAK EVIDENCE (SEMANTIC SIMILARITY) ===");
            context.AppendLine("The following content is semantically related but not explicitly matched. Treat as reference only.");
            foreach (var chunk in retrieval.WeakEvidence.Take(3))
            {
                var truncated = chunk.Content.Length > 400 ? chunk.Content[..400] + "..." : chunk.Content;
                context.AppendLine($"[Source: {chunk.Section}] {truncated}");
            }
            context.AppendLine();
        }

        // 4. Conversation history (last 6 messages)
        if (request.ConversationHistory.Any())
        {
            context.AppendLine("=== CONVERSATION HISTORY ===");
            foreach (var msg in request.ConversationHistory.TakeLast(6))
            {
                context.AppendLine($"{msg.Role}: {msg.Content}");
            }
            context.AppendLine();
        }

        // 5. Current query
        context.AppendLine("=== USER QUERY ===");
        context.AppendLine(request.Query);

        return context.ToString();
    }
}
```

**Budget**: ~7500 chars (leaves ~1000 for model overhead)

---

## Retrieval Configuration

| Parameter | Value | Description |
|-----------|-------|-------------|
| Max keyword results | 5 | Per query |
| Max semantic results | 5 | Per query |
| Max combined results | 5 | After dedup |
| Semantic similarity threshold | 0.75 | Cosine similarity |
| Context budget | 7500 chars | Total prompt budget |
| Max history messages | 6 | Last 3 exchanges |

---

## Change Detection

```csharp
public class KnowledgeIndexStateRepository : IKnowledgeIndexStateRepository
{
    public async Task<bool> IsCurrentAsync(string resumeHash, CancellationToken ct)
    {
        var state = await _collection.Find(x => x.Id == "current").FirstOrDefaultAsync(ct);
        return state?.ResumeHash == resumeHash;
    }

    public async Task SetCurrentAsync(string resumeHash, CancellationToken ct)
    {
        await _collection.ReplaceOneAsync(
            x => x.Id == "current",
            new KnowledgeIndexState { Id = "current", ResumeHash = resumeHash, UpdatedAt = DateTime.UtcNow },
            new ReplaceOptions { IsUpsert = true },
            ct);
    }
}
```

**Manual Re-index**: `POST /ops/knowledge/reindex`

---

## Monitoring

| Metric | Target | Alert |
|--------|--------|-------|
| `knowledge.chunk_count` | > 20 | < 10 |
| `knowledge.embedding_coverage` | 1.0 | < 0.9 |
| `knowledge.last_indexed_age_hours` | < 24 | > 48 |
| `retrieval.latency_ms_p95` | < 200 | > 500 |
| `retrieval.keyword_recall` | > 0.85 | < 0.7 |

---

## Related Documents

- [RAG Architecture](../architecture/rag-architecture.md) — Full pipeline
- [Embedding Strategy](../architecture/embedding-strategy.md) — Vector details
- [AI Pipeline](../architecture/ai-pipeline.md) — Generation flow
- [Corpus Management](../architecture/corpus-management.md) — Resume updates