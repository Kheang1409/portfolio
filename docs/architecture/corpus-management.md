# Corpus Management

## Overview

The knowledge corpus is derived from the **latest resume document** in MongoDB. This document covers how to manage, update, and monitor the corpus.

---

## Corpus Lifecycle

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  Resume     │────▶│  Chunking   │────▶│  Indexing   │────▶│  Serving    │
│  Update     │     │  & Extract  │     │  (Keyword + │     │  (Retrieval)│
│  (MongoDB)  │     │  Keywords   │     │   Semantic) │     │             │
└─────────────┘     └─────────────┘     └─────────────┘     └─────────────┘
       │                   │                   │                   │
       ▼                   ▼                   ▼                   ▼
  New document       ~50 chunks          MongoDB              Hybrid
  triggers           per resume          knowledgeChunks      retrieval
  re-index           + embeddings        collection           at query time
```

---

## Updating the Corpus

### Method 1: MongoDB Direct (Recommended)

```javascript
// Connect to Atlas
use KaiAssistant

// Replace latest resume (upsert by sort order)
db.resumes.replaceOne(
  {},  // Match any (single latest)
  {
    profile: {
      fullName: "Kai Assistant",
      headline: "Senior Software Engineer",
      summary: "Updated summary...",
      githubUrl: "https://github.com/kai",
      linkedInUrl: "https://linkedin.com/in/kai"
    },
    experience: [ /* ... */ ],
    projects: [ /* ... */ ],
    education: [ /* ... */ ],
    certifications: [ /* ... */ ],
    skills: [ /* ... */ ],
    updatedAt: new Date()
  },
  { upsert: true }
)
```

**Next startup**: `KnowledgeBootstrapHostedService` detects hash change → re-indexes automatically.

---

### Method 2: Manual Re-index Endpoint

```bash
# Trigger immediate re-index without restart
curl -X POST http://localhost:5000/ops/knowledge/reindex \
  -H "Authorization: Bearer <admin-token>"
```

Response:
```json
{
  "status": "started",
  "message": "Knowledge re-indexing triggered",
  "estimatedDurationSeconds": 30
}
```

---

### Method 3: Resume API (If Implemented)

```bash
curl -X PUT http://localhost:5000/api/resumes/latest \
  -H "Content-Type: application/json" \
  -d '{ "profile": {...}, "experience": [...], ... }'
```

---

## Corpus Structure

### Resume Document Schema (MongoDB: `resumes`)

```json
{
  "_id": ObjectId,
  "profile": {
    "fullName": "string",
    "headline": "string", 
    "summary": "string",
    "githubUrl": "string",
    "linkedInUrl": "string",
    "email": "string",
    "location": "string"
  },
  "experience": [
    {
      "company": "string",
      "role": "string",
      "startDate": "ISODate",
      "endDate": "ISODate|null",
      "description": "string",
      "technologies": ["string"],
      "highlights": ["string"]
    }
  ],
  "projects": [
    {
      "name": "string",
      "description": "string",
      "technologies": ["string"],
      "url": "string",
      "githubUrl": "string",
      "imageUrl": "string"
    }
  ],
  "education": [
    {
      "institution": "string",
      "degree": "string",
      "field": "string",
      "startYear": "int",
      "endYear": "int",
      "honors": ["string"]
    }
  ],
  "certifications": [
    {
      "name": "string",
      "issuer": "string",
      "year": "int",
      "credentialId": "string",
      "url": "string"
    }
  ],
  "skills": [
    {
      "name": "string",
      "category": "string",  // "Language", "Framework", "Cloud", "Tool", "Database"
      "level": "string"      // "Expert", "Advanced", "Intermediate", "Beginner"
    }
  ],
  "updatedAt": "ISODate",
  "createdAt": "ISODate"
}
```

---

## Knowledge Chunks (MongoDB: `knowledgeChunks`)

Auto-generated from resume. **Do not edit directly.**

```json
{
  "_id": ObjectId,
  "resumeHash": "sha256-hex",
  "chunkIndex": 0,
  "section": "experience",
  "title": "Senior Software Engineer",
  "content": "Senior Software Engineer at TechCorp (2022–Present)\nLed migration to .NET 8...",
  "keywords": ["senior", "software", "engineer", "techcorp", "migration", "dotnet", "azure"],
  "embedding": [0.12, -0.34, ...],  // 768 floats (nullable)
  "createdAt": "ISODate",
  "updatedAt": "ISODate"
}
```

---

## Monitoring & Verification

### Health Checks

```bash
# Check corpus status
curl http://localhost:5000/ops/knowledge/status

# Response
{
  "resumeHash": "abc123...",
  "chunkCount": 47,
  "embeddingCoverage": 1.0,  // 1.0 = 100% have embeddings
  "lastIndexed": "2026-11-24T10:30:00Z",
  "isCurrent": true
}
```

### Metrics

| Metric | Target | Alert Threshold |
|--------|--------|-----------------|
| `knowledge.chunk_count` | > 20 | < 10 |
| `knowledge.embedding_coverage` | 1.0 | < 0.9 |
| `knowledge.last_indexed_age_hours` | < 24 | > 48 |
| `knowledge.reindex_duration_seconds` | < 60 | > 120 |

### Manual Verification

```bash
# Count chunks
docker exec -it backend mongosh KaiAssistant --eval 'db.knowledgeChunks.countDocuments()'

# Check embedding coverage
docker exec -it backend mongosh KaiAssistant --eval 'db.knowledgeChunks.countDocuments({Embedding: {$exists: true, $ne: null}})'

# Sample chunks
docker exec -it backend mongosh KaiAssistant --eval 'db.knowledgeChunks.find().limit(3).pretty()'

# Verify hash matches resume
docker exec -it backend mongosh KaiAssistant --eval 'db.resumes.findOne({}, {sort: {updatedAt: -1}})'
```

---

## Troubleshooting

| Symptom | Cause | Fix |
|---------|-------|-----|
| Assistant gives outdated info | Resume updated but hash same | Check `updatedAt` field changed; force re-index via `/ops/knowledge/reindex` |
| Embedding coverage < 100% | Gemini API failed during bootstrap | Check logs; trigger re-index; verify `GEMINI_API_KEY` |
| No chunks found | Resume collection empty | Insert resume document; verify `MONGODB_DATABASE` |
| Keyword search poor | Keywords not extracted | Check chunking logic; verify text content not empty |
| Semantic search poor | Embeddings stale | Force re-index; check Atlas Vector Search index exists |

---

## Backup & Recovery

The corpus is **derivable from resume** — no separate backup needed.

**Recovery Procedure**:
1. Restore `resumes` collection from MongoDB Atlas backup
2. Restart backend (or call `/ops/knowledge/reindex`)
3. Corpus rebuilds automatically

---

## Related Documents

- [RAG Architecture](rag-architecture.md) — Indexing pipeline details
- [Embedding Strategy](embedding-strategy.md) — Vector embeddings
- [AI Pipeline](ai-pipeline.md) — Full retrieval flow
- [Data Protection](../deployment/data-protection.md) — MongoDB backup strategy