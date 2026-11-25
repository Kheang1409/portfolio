# RAG Architecture

## Retrieval-Augmented Generation Pipeline

This document details the internal mechanics of the retrieval and knowledge indexing system.

---

## Data Model

### Knowledge Chunk (MongoDB: `knowledgeChunks`)

```csharp
public class KnowledgeChunk
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string ResumeHash { get; set; }      // SHA256 of resume content
    public int ChunkIndex { get; set; }         // Order in document
    public string Section { get; set; }         // "experience", "skills", "projects", etc.
    public string Title { get; set; }           // Human-readable section title
    public string Content { get; set; }         // Plain text content (~500 chars)
    public string[] Keywords { get; set; }      // Extracted key terms
    public float[]? Embedding { get; set; }     // 768-dim Gemini embedding (nullable)
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**Indexes**:
```javascript
// Compound for hash-based lookup
db.knowledgeChunks.createIndex({ ResumeHash: 1, ChunkIndex: 1 }, { unique: true });

// Text search for keyword retrieval
db.knowledgeChunks.createIndex({ 
    Content: "text", 
    Keywords: "text", 
    Section: "text", 
    Title: "text" 
}, { 
    weights: { Keywords: 10, Title: 5, Section: 3, Content: 1 } 
});

// Vector search (Atlas Vector Search) - optional
db.knowledgeChunks.createIndex({ Embedding: "vector" }, { 
    vectorOptions: { type: "vector", path: "Embedding", numDimensions: 768, similarity: "cosine" } 
});
```

---

## Indexing Pipeline

### 1. Resume Hashing

```csharp
public static string ComputeResumeHash(Resume resume)
{
    var normalized = new
    {
        resume.Profile.FullName,
        resume.Profile.Headline,
        resume.Profile.Summary,
        resume.Experience,
        resume.Projects,
        resume.Education,
        resume.Certifications,
        resume.Skills
    };
    
    var json = JsonSerializer.Serialize(normalized, new JsonSerializerOptions 
    { 
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });
    
    using var sha = SHA256.Create();
    return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(json)));
}
```

### 2. Chunking Strategy

```csharp
public class ResumeChunker
{
    public IEnumerable<KnowledgeChunk> Chunk(Resume resume)
    {
        var chunks = new List<KnowledgeChunk>();

        // Profile section
        chunks.AddRange(ChunkSection("profile", "Profile", 
            $"{resume.Profile.FullName} — {resume.Profile.Headline}\n{resume.Profile.Summary}"));

        // Skills (grouped by category)
        foreach (var category in resume.Skills.GroupBy(s => s.Category))
        {
            chunks.AddRange(ChunkSection("skills", $"Skills: {category.Key}", 
                string.Join(", ", category.Select(s => s.Name))));
        }

        // Experience (one chunk per role)
        foreach (var exp in resume.Experience)
        {
            var content = $"{exp.Role} at {exp.Company} ({exp.StartDate:yyyy}–{exp.EndDate?.ToString("yyyy") ?? "Present"})\n" +
                          $"{exp.Description}\n" +
                          $"Tech: {string.Join(", ", exp.Technologies)}";
            chunks.AddRange(ChunkSection("experience", exp.Role, content));
        }

        // Projects (one chunk per project)
        foreach (var proj in resume.Projects)
        {
            var content = $"{proj.Name}: {proj.Description}\n" +
                          $"Tech: {string.Join(", ", proj.Technologies)}\n" +
                          $"Link: {proj.Url}";
            chunks.AddRange(ChunkSection("projects", proj.Name, content));
        }

        // Education
        foreach (var edu in resume.Education)
        {
            chunks.AddRange(ChunkSection("education", edu.Degree, 
                $"{edu.Degree} in {edu.Field} from {edu.Institution} ({edu.StartYear}–{edu.EndYear})"));
        }

        // Certifications
        foreach (var cert in resume.Certifications)
        {
            chunks.AddRange(ChunkSection("certifications", cert.Name, 
                $"{cert.Name} from {cert.Issuer} ({cert.Year})"));
        }

        return chunks.Select((c, i) => c with { ChunkIndex = i });
    }

    private IEnumerable<KnowledgeChunk> ChunkSection(string section, string title, string content)
    {
        // Split by ~500 chars with sentence boundary awareness
        var sentences = SplitSentences(content);
        var current = new StringBuilder();
        
        foreach (var sentence in sentences)
        {
            if (current.Length + sentence.Length > 500 && current.Length > 0)
            {
                yield return new KnowledgeChunk 
                { 
                    Section = section, 
                    Title = title, 
                    Content = current.ToString().Trim(),
                    Keywords = ExtractKeywords(current.ToString())
                };
                current.Clear();
            }
            current.Append(sentence).Append(' ');
        }
        
        if (current.Length > 0)
        {
            yield return new KnowledgeChunk 
            { 
                Section = section, 
                Title = title, 
                Content = current.ToString().Trim(),
                Keywords = ExtractKeywords(current.ToString())
            };
        }
    }
}
```

### 3. Keyword Extraction

```csharp
public string[] ExtractKeywords(string text)
{
    // Simple TF-IDF style: frequency + domain terms
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

## Retrieval Algorithms

### 1. Keyword Retrieval (MongoDB Text Search)

```csharp
public class KeywordRetriever : IKeywordRetriever
{
    public async Task<List<KnowledgeChunk>> RetrieveAsync(string query, int maxResults)
    {
        var filter = Builders<KnowledgeChunk>.Filter.Text(query);
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
}
```

**Query Preprocessing**:
- Lowercase
- Remove punctuation
- Expand common abbreviations (e.g., "js" → "javascript", "k8s" → "kubernetes")
- Boost: exact phrase matches score higher

---

### 2. Semantic Retrieval (Vector Search)

```csharp
public class SemanticRetriever : ISemanticRetriever
{
    public async Task<List<KnowledgeChunk>> RetrieveAsync(float[] queryEmbedding, int maxResults)
    {
        // Atlas Vector Search pipeline
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
            .Where(r => r.Score >= 0.75f)  // Cosine similarity threshold
            .Select(r => r.Chunk with { EvidenceStrength = EvidenceStrength.Weak })
            .ToList();
    }
}
```

**Embedding Model**: Google Gemini `gemini-embedding-001` (768 dimensions)

---

### 3. Hybrid Combination & Ranking

```csharp
public class HybridRetriever : IRetriever
{
    public async Task<RetrievalResult> RetrieveAsync(string query, int maxResults = 5)
    {
        var keywordTask = _keywordRetriever.RetrieveAsync(query, maxResults);
        var semanticTask = _embeddingService.IsAvailable 
            ? _semanticRetriever.RetrieveAsync(await _embeddingService.GenerateQueryEmbeddingAsync(query), maxResults)
            : Task.FromResult(new List<KnowledgeChunk>());

        await Task.WhenAll(keywordTask, semanticTask);

        var keywordResults = keywordTask.Result;
        var semanticResults = semanticTask.Result;

        // Deduplicate by Content hash (semantic may return same chunks)
        var seen = new HashSet<string>();
        var combined = new List<KnowledgeChunk>();

        // Strong evidence first (keyword)
        foreach (var chunk in keywordResults)
        {
            var hash = ComputeContentHash(chunk.Content);
            if (seen.Add(hash))
                combined.Add(chunk);
        }

        // Weak evidence (semantic) — only if not already present
        foreach (var chunk in semanticResults)
        {
            var hash = ComputeContentHash(chunk.Content);
            if (seen.Add(hash))
                combined.Add(chunk);
        }

        // Rank: Strong > Weak, then by score
        var ranked = combined
            .OrderByDescending(c => c.EvidenceStrength)
            .ThenByDescending(c => c.RetrievalScore)
            .Take(maxResults)
            .ToList();

        return new RetrievalResult
        {
            StrongEvidence = ranked.Where(c => c.EvidenceStrength == EvidenceStrength.Strong).ToList(),
            WeakEvidence = ranked.Where(c => c.EvidenceStrength == EvidenceStrength.Weak).ToList()
        };
    }
}
```

---

## Context Budget Management

| Component | Budget | Strategy |
|-----------|--------|----------|
| System Prompt | ~500 chars | Fixed |
| Strong Evidence | ~3000 chars | Up to 3 chunks, full content |
| Weak Evidence | ~2000 chars | Up to 3 chunks, truncated to 400 chars |
| Conversation History | ~1500 chars | Last 6 messages |
| User Query | ~500 chars | As-is |
| **Total** | **~7500 chars** | Leaves ~1000 for model overhead |

---

## Change Detection & Re-indexing

```csharp
public class KnowledgeIndexStateRepository : IKnowledgeIndexStateRepository
{
    public async Task<bool> IsCurrentAsync(string resumeHash)
    {
        var state = await _collection.Find(x => x.Id == "current").FirstOrDefaultAsync();
        return state?.ResumeHash == resumeHash;
    }

    public async Task SetCurrentAsync(string resumeHash)
    {
        await _collection.ReplaceOneAsync(
            x => x.Id == "current",
            new KnowledgeIndexState { Id = "current", ResumeHash = resumeHash, UpdatedAt = DateTime.UtcNow },
            new ReplaceOptions { IsUpsert = true });
    }
}
```

**Trigger**: `KnowledgeBootstrapHostedService` at startup + manual endpoint `POST /ops/knowledge/reindex`

---

## Evaluation Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Keyword Recall@5 | > 0.85 | Ground truth: known resume facts |
| Semantic Recall@5 | > 0.75 | Ground truth: paraphrased queries |
| Hybrid Recall@5 | > 0.90 | Combined |
| Latency (p95) | < 500ms | End-to-end retrieval |
| Embedding Coverage | 100% | All chunks have embeddings (when available) |

See [RAG Evaluation](../backend/rag-evaluation.md) for automated test harness.

---

## Related Documents

- [AI Pipeline](ai-pipeline.md) — Full pipeline overview
- [Embedding Strategy](embedding-strategy.md) — Vector embeddings details
- [System Diagram](../diagrams/ai-flow-diagram.md) — Visual flow
- [Corpus Management](corpus-management.md) — Knowledge base maintenance