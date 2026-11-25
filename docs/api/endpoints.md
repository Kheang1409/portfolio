# API Endpoints Reference

## Base URL

| Environment | URL |
|-------------|-----|
| Local Dev | `http://localhost:5000` |
| Docker Dev | `http://localhost:5000` |
| Production | `https://api.yourdomain.com` |

All endpoints under `/api/` unless noted.

---

## Assistant

### Stream Assistant Response

**POST** `/api/assistant`

Streaming NDJSON response grounded in resume knowledge.

**Request**:
```json
{
  "query": "What is your experience with .NET?",
  "history": [
    {"role": "user", "content": "Hi"},
    {"role": "assistant", "content": "Hello! How can I help?"}
  ]
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `query` | string | Yes | User question (max 10000 chars) |
| `history` | array | No | Previous messages (max 6, for context) |

**Response**: `application/x-ndjson` stream

```ndjson
{"type":"content","text":"I have","index":0}
{"type":"content","text":" 8 years","index":1}
{"type":"content","text":" of experience","index":2}
{"type":"done","finishReason":"STOP","index":3}
```

| Chunk Type | Fields | Description |
|------------|--------|-------------|
| `content` | `text`, `index` | Partial response text |
| `done` | `finishReason`, `index` | Stream complete (`STOP`, `LENGTH`, `SAFETY`) |
| `error` | `message`, `code` | Error during streaming |
| `fallback` | `text` | Fallback response (AI unavailable) |

**Errors**:
| Status | Code | Description |
|--------|------|-------------|
| 400 | `ValidationError` | Query empty or too long |
| 429 | `RateLimited` | Too many requests |
| 503 | `ServiceUnavailable` | AI service down (fallback returned) |

---

## Resumes

### Get Latest Resume

**GET** `/api/resumes/latest`

Returns the most recent resume document.

**Response** (200):
```json
{
  "profile": {
    "fullName": "Kai Assistant",
    "headline": "Senior Software Engineer",
    "summary": "Experienced...",
    "githubUrl": "https://github.com/kai",
    "linkedInUrl": "https://linkedin.com/in/kai",
    "email": "kai@example.com",
    "location": "San Francisco, CA"
  },
  "experience": [
    {
      "company": "TechCorp",
      "role": "Senior Software Engineer",
      "startDate": "2022-01-01T00:00:00Z",
      "endDate": null,
      "description": "Lead backend development...",
      "technologies": [".NET 8", "Azure", "MongoDB"],
      "highlights": ["Reduced latency 40%", "Migrated to .NET 8"]
    }
  ],
  "projects": [
    {
      "name": "E-Commerce Platform",
      "description": "Microservices-based...",
      "technologies": [".NET 8", "React", "Kubernetes"],
      "url": "https://example.com",
      "githubUrl": "https://github.com/kai/ecommerce",
      "imageUrl": "/images/project-ecommerce.jpg"
    }
  ],
  "education": [
    {
      "institution": "Stanford University",
      "degree": "Master of Science",
      "field": "Computer Science",
      "startYear": 2015,
      "endYear": 2017,
      "honors": ["Dean's List"]
    }
  ],
  "certifications": [
    {
      "name": "AWS Solutions Architect",
      "issuer": "Amazon Web Services",
      "year": 2023,
      "credentialId": "ABC123",
      "url": "https://aws.amazon.com/verification"
    }
  ],
  "skills": [
    {"name": "C#", "category": "Language", "level": "Expert"},
    {"name": "Azure", "category": "Cloud", "level": "Advanced"}
  ],
  "updatedAt": "2026-11-24T10:30:00Z"
}
```

**Errors**:
| Status | Code | Description |
|--------|------|-------------|
| 404 | `Resume.NotFound` | No resume in database |

---

## Donations

### Get Donation Config

**GET** `/api/donations/config`

Public PayPal configuration for frontend checkout.

**Response** (200):
```json
{
  "clientId": "PAYPAL_CLIENT_ID",
  "environment": "live",
  "currency": "USD",
  "minAmount": 1,
  "maxAmount": 10000
}
```

---

### Get Donation Leaderboard

**GET** `/api/donations/leaderboard`

Public opted-in completed donations.

**Query Parameters**:
| Param | Type | Default | Description |
|-------|------|---------|-------------|
| `limit` | int | 50 | Max results (1-100) |

**Response** (200):
```json
[
  {
    "donorName": "John D.",
    "amount": 25.00,
    "currency": "USD",
    "remark": "Great work!",
    "completedAt": "2026-11-20T15:30:00Z"
  }
]
```

---

### Create Donation Order

**POST** `/api/donations/orders`

Creates or recovers a PayPal order.

**Request**:
```json
{
  "amount": 25.00,
  "currency": "USD",
  "donorEmail": "donor@example.com",
  "donorName": "John Doe",
  "remark": "Keep up the good work!",
  "optInLeaderboard": true,
  "attemptId": "uuid-from-session-storage"  // Optional, for recovery
}
```

**Response** (200):
```json
{
  "orderId": "PAYPAL_ORDER_ID",
  "attemptId": "uuid",
  "amount": 25.00,
  "currency": "USD"
}
```

**Errors**:
| Status | Code | Description |
|--------|------|-------------|
| 400 | `ValidationError` | Amount out of range, invalid email |
| 409 | `AttemptRecovered` | Existing attempt found (returns existing order) |
| 502 | `PayPalError` | PayPal API failure |

---

### Capture Donation Order

**POST** `/api/donations/capture`

Captures and verifies an approved PayPal order.

**Request**:
```json
{
  "orderId": "PAYPAL_ORDER_ID",
  "attemptId": "uuid"
}
```

**Response** (200):
```json
{
  "donationId": "mongo-id",
  "status": "COMPLETED",
  "captureId": "PAYPAL_CAPTURE_ID",
  "amount": 25.00,
  "currency": "USD",
  "completedAt": "2026-11-24T10:30:00Z"
}
```

**Errors**:
| Status | Code | Description |
|--------|------|-------------|
| 400 | `ValidationError` | Missing orderId |
| 404 | `OrderNotFound` | Order not found or expired |
| 409 | `AlreadyCaptured` | Order already captured |
| 422 | `PayPalVerificationFailed` | Capture verification failed |
| 502 | `PayPalError` | PayPal API failure |

---

### PayPal Webhook

**POST** `/api/donations/webhook`

Receives PayPal webhook events (capture, refund, reversal).

**Headers**:
| Header | Description |
|--------|-------------|
| `PayPal-Transmission-Id` | Unique transmission ID |
| `PayPal-Transmission-Time` | ISO 8601 timestamp |
| `PayPal-Cert-Url` | Certificate URL for verification |
| `PayPal-Auth-Algo` | Algorithm (SHA256withRSA) |
| `PayPal-Transmission-Sig` | Signature |

**Body**: PayPal webhook event JSON

**Response**: 200 (acknowledged) or 400/500 (retry later)

---

## Contacts

### Submit Contact Form

**POST** `/api/contacts`

**Request**:
```json
{
  "name": "John Doe",
  "email": "john@example.com",
  "subject": "Inquiry",
  "message": "Hello, I'd like to..."
}
```

**Response** (200):
```json
{
  "contactId": "mongo-id",
  "submittedAt": "2026-11-24T10:30:00Z"
}
```

**Errors**:
| Status | Code | Description |
|--------|------|-------------|
| 400 | `ValidationError` | Invalid email, missing fields |
| 429 | `RateLimited` | Too many submissions |

---

## Visits

### Record Visit

**POST** `/api/visits`

**Request**:
```json
{
  "path": "/",
  "referrer": "https://google.com",
  "userAgent": "Mozilla/5.0...",
  "screenResolution": "1920x1080"
}
```

**Response** (200):
```json
{
  "visitId": "mongo-id",
  "recordedAt": "2026-11-24T10:30:00Z"
}
```

---

## Health Checks

### Basic Health

**GET** `/api/health`

Simple process health.

**Response** (200):
```json
{ "status": "Healthy" }
```

---

### Liveness Probe

**GET** `/health/live`

Kubernetes/Docker liveness — process is running.

**Response** (200):
```json
{ "status": "Alive" }
```

---

### Readiness Probe

**GET** `/health/ready`

Kubernetes/Docker readiness — dependencies healthy.

**Response** (200):
```json
{
  "status": "Ready",
  "checks": {
    "mongodb": "Healthy",
    "redis": "Healthy"
  }
}
```

**Response** (503):
```json
{
  "status": "NotReady",
  "checks": {
    "mongodb": "Unhealthy",
    "redis": "Healthy"
  }
}
```

---

## Operational Endpoints (`/ops/*`)

> **Note**: Availability depends on backend configuration. Disabled by default in production.

### Knowledge Re-index

**POST** `/ops/knowledge/reindex`

Triggers immediate knowledge base rebuild.

**Response** (202):
```json
{ "status": "started", "message": "Re-indexing triggered" }
```

### Knowledge Status

**GET** `/ops/knowledge/status`

**Response** (200):
```json
{
  "resumeHash": "abc123...",
  "chunkCount": 47,
  "embeddingCoverage": 1.0,
  "lastIndexed": "2026-11-24T10:30:00Z",
  "isCurrent": true
}
```

---

## Rate Limits

| Endpoint | Limit | Window | Key |
|----------|-------|--------|-----|
| `/api/assistant` | 20 | 1 min | IP + UserAgent |
| `/api/contacts` | 5 | 1 hour | IP |
| `/api/donations/orders` | 10 | 1 hour | IP |
| `/api/donations/capture` | 10 | 1 hour | IP |
| `/api/visits` | 100 | 1 min | IP |

**Headers** (on limited responses):
```
Retry-After: 45
X-RateLimit-Limit: 20
X-RateLimit-Remaining: 0
X-RateLimit-Reset: 1700000000
```

---

## CORS

Configured via `ALLOWED_ORIGINS` environment variable.

**Allowed Methods**: GET, POST, OPTIONS  
**Allowed Headers**: Content-Type, Authorization, X-Requested-With  
**Credentials**: Not allowed (no cookies cross-origin)

---

## Error Format

All error responses follow this structure:

```json
{
  "error": {
    "code": "ErrorCode",
    "message": "Human-readable description",
    "details": {}  // Optional, validation errors etc.
  }
}
```

**Common Codes**:
| Code | HTTP Status | Description |
|------|-------------|-------------|
| `ValidationError` | 400 | Request validation failed |
| `NotFound` | 404 | Resource not found |
| `Conflict` | 409 | Resource conflict |
| `RateLimited` | 429 | Too many requests |
| `ServiceUnavailable` | 503 | Dependency down |
| `InternalError` | 500 | Unexpected server error |