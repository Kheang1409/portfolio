# Tradeoffs

## Key Design Decisions

### Clean Architecture Boundaries

- Decision: strict separation between domain, application abstractions, infrastructure implementations, and API transport.
- Benefit: testability, modularity, and safer refactoring.
- Tradeoff: additional interface and wiring overhead.

### MongoDB over Relational Database

- Decision: document model for resumes, knowledge docs, conversation records, and flexible AI metadata.
- Benefit: schema flexibility and fast iteration for AI-centric payloads.
- Tradeoff: fewer relational guarantees and join semantics versus relational designs.

### Redis as Distributed Control Plane

- Decision: Redis for rate limiting, idempotency coordination, and low-latency cache paths.
- Benefit: high-throughput shared state across stateless nodes.
- Tradeoff: added operational dependency and failure-mode complexity.

### Gemini model selection

- Decision: select and retry among configured Gemini models; the chat path does not currently use cross-provider fallback.
- Benefit: model-level availability and routing without maintaining multiple active provider integrations.
- Tradeoff: all chat availability still depends on Gemini.

### Exact response cache + RAG

- Decision: use exact response caching at prompt construction, keyed by question, conversation, and selected evidence.
- Benefit: repeated equivalent requests can avoid model calls without reusing answers for semantically similar but non-identical questions.
- Tradeoff: retrieval and prompt assembly still run before cache lookup; fewer hits than semantic reuse. The outer orchestrator cache and registered semantic cache are not used by the active chat path.
