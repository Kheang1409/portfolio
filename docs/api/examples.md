# API Examples

## Assistant stream (NDJSON)

The active assistant endpoint is `POST /api/assistant`. Each response line is a JSON event.

```bash
curl -N -X POST http://localhost:5000/api/assistant \
  -H "Content-Type: application/json" \
  -d '{"message":"Summarize the architecture in three bullets."}'
```

Example stream:

```json
{"type":"delta","messageId":"...","text":"The backend uses..."}
{"type":"completed","messageId":"..."}
```

To retain conversation memory, provide a stable `conversationId` on each request:

```bash
curl -N -X POST http://localhost:5000/api/assistant \
  -H "Content-Type: application/json" \
  -d '{"message":"What did I ask earlier?","conversationId":"demo-conversation"}'
```

If `conversationId` is omitted, the API can resolve one from `context.metadata.sessionId`; otherwise it creates a request-specific conversation identifier.

## Other active endpoints

```text
POST /api/contacts
POST /api/visits
GET  /api/resumes/latest
GET  /api/health
GET  /health/ready
GET  /health/live
```
