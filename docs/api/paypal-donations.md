# PayPal Donations Integration

## Overview

The donation flow uses **PayPal Checkout (Smart Payment Buttons)** with backend order creation/capture and webhook verification.

---

## Architecture

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  Frontend   │────▶│  Backend    │────▶│   PayPal    │────▶│  Webhook    │
│  (Browser)  │     │  (API)      │     │  (Checkout) │     │  (Verify)   │
└─────────────┘     └─────────────┘     └─────────────┘     └─────────────┘
       │                   │                   │                   │
       ▼                   ▼                   ▼                   ▼
  PayPal JS SDK      Create Order          User Approves      Verify Signature
  Renders Button     Returns Order ID      Returns to Site    Update MongoDB
  Calls /orders      Stores Attempt        Calls /capture     Send Receipt
```

---

## Frontend Flow

### 1. Load PayPal SDK

```html
<script 
  src="https://www.paypal.com/sdk/js?client-id=CLIENT_ID&currency=USD&components=buttons"
  data-csp-nonce="NONCE">
</script>
```

> **Note**: `CLIENT_ID` from `/api/donations/config` — safe to expose publicly.

### 2. Render Button

```javascript
paypal.Buttons({
  createOrder: async () => {
    const response = await fetch('/api/donations/orders', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        amount: selectedAmount,
        currency: 'USD',
        donorEmail: email,
        donorName: name,
        remark: remark,
        optInLeaderboard: optIn,
        attemptId: sessionStorage.getItem('donationAttemptId') // Recovery
      })
    });
    const { orderId, attemptId } = await response.json();
    sessionStorage.setItem('donationAttemptId', attemptId);
    return orderId;
  },
  
  onApprove: async (data) => {
    const response = await fetch('/api/donations/capture', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ orderId: data.orderID, attemptId })
    });
    const result = await response.json();
    if (result.status === 'COMPLETED') {
      sessionStorage.removeItem('donationAttemptId');
      showSuccess(result);
    }
  },
  
  onError: (err) => {
    console.error('PayPal error:', err);
    showError('Payment failed. Please try again.');
  }
}).render('#donate-button');
```

---

## Backend Endpoints

### GET `/api/donations/config`

Returns public config for SDK initialization.

**Response**:
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

### POST `/api/donations/orders`

Creates or recovers a PayPal order.

**Request**:
```json
{
  "amount": 25.00,
  "currency": "USD",
  "donorEmail": "donor@example.com",
  "donorName": "John Doe",
  "remark": "Great work!",
  "optInLeaderboard": true,
  "attemptId": "optional-uuid-for-recovery"
}
```

**Validation**:
| Field | Rules |
|-------|-------|
| `amount` | 1-10000, 2 decimal places |
| `currency` | Must be "USD" |
| `donorEmail` | Valid email format |
| `donorName` | Optional, max 100 chars |
| `remark` | Optional, max 500 chars |
| `optInLeaderboard` | Boolean |
| `attemptId` | Valid UUID (if provided) |

**Response** (200):
```json
{
  "orderId": "PAYPAL_ORDER_ID",
  "attemptId": "uuid",
  "amount": 25.00,
  "currency": "USD"
}
```

**Response** (409 - Attempt Recovered):
```json
{
  "orderId": "EXISTING_PAYPAL_ORDER_ID",
  "attemptId": "same-uuid",
  "amount": 25.00,
  "currency": "USD",
  "recovered": true
}
```

---

### POST `/api/donations/capture`

Captures and verifies an approved order.

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
  "donationId": "mongo-object-id",
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
| 404 | `OrderNotFound` | Order expired or invalid |
| 409 | `AlreadyCaptured` | Duplicate capture attempt |
| 422 | `PayPalVerificationFailed` | PayPal response invalid |
| 502 | `PayPalError` | PayPal API communication failure |

---

### POST `/api/donations/webhook`

Receives PayPal webhook events.

**Verified Events**:
| Event Type | Action |
|------------|--------|
| `PAYMENT.CAPTURE.COMPLETED` | Mark donation completed, queue receipt |
| `PAYMENT.CAPTURE.REFUNDED` | Mark donation refunded |
| `PAYMENT.CAPTURE.REVERSED` | Mark donation reversed |

**Verification**:
1. Extract headers: `PayPal-Transmission-Id`, `PayPal-Transmission-Time`, `PayPal-Cert-Url`, `PayPal-Auth-Algo`, `PayPal-Transmission-Sig`
2. Fetch certificate from `PayPal-Cert-Url`
3. Verify signature using `PayPal-Auth-Algo` (SHA256withRSA)
4. Validate `PayPal-Transmission-Id` not replayed (store processed IDs 90 days)
5. Process event

**Idempotency**: Webhook events processed exactly once via transmission ID deduplication.

---

## Data Model (MongoDB)

### Donation

```json
{
  "_id": ObjectId,
  "attemptId": "uuid",
  "orderId": "PAYPAL_ORDER_ID",
  "captureId": "PAYPAL_CAPTURE_ID",
  "status": "PENDING|COMPLETED|REFUNDED|REVERSED|FAILED",
  "amount": 25.00,
  "currency": "USD",
  "donorEmail": "donor@example.com",
  "donorName": "John Doe",
  "remark": "Great work!",
  "optInLeaderboard": true,
  "createdAt": "2026-11-24T10:30:00Z",
  "completedAt": "2026-11-24T10:31:00Z",
  "refundedAt": null,
  "webhookEvents": [
    {
      "transmissionId": "webhook-transmission-id",
      "eventType": "PAYMENT.CAPTURE.COMPLETED",
      "receivedAt": "2026-11-24T10:31:00Z",
      "payload": { ... }
    }
  ]
}
```

### Donation Receipt (Outbox)

```json
{
  "_id": ObjectId,
  "donationId": "mongo-object-id",
  "toEmail": "donor@example.com",
  "subject": "Thank you for your donation!",
  "body": "HTML receipt...",
  "status": "PENDING|SENT|FAILED",
  "attempts": 0,
  "lastAttemptAt": null,
  "createdAt": "2026-11-24T10:31:00Z",
  "sentAt": null
}
```

---

## Email Receipts

- **Trigger**: `PAYMENT.CAPTURE.COMPLETED` webhook
- **Queue**: MongoDB outbox collection
- **Processor**: Background hosted service (retries every 5 min, max 10 attempts)
- **SMTP**: Configured via `SMTP_*` environment variables
- **Failure**: Does not reverse payment; logged for manual retry

---

## Security

| Aspect | Implementation |
|--------|----------------|
| **Client Secret** | Server-only, never exposed to frontend |
| **Webhook Verification** | Signature + certificate validation |
| **Idempotency** | `attemptId` (frontend) + `transmissionId` (webhook) |
| **Amount Validation** | Backend validates amount matches order |
| **Email Retention** | `PAYPAL_EMAIL_RETENTION_DAYS` (default 30) then purged |
| **Leaderboard** | Only `optInLeaderboard=true` donations shown |

---

## Sandbox Testing

1. Create PayPal Developer account
2. Create Sandbox App → get `Client ID` + `Secret`
3. Create Sandbox Webhook → get `Webhook ID`
4. Configure backend env vars with sandbox credentials:
   ```bash
   PAYPAL_ENVIRONMENT=sandbox
   PAYPAL_CLIENT_ID=sandbox-client-id
   PAYPAL_CLIENT_SECRET=sandbox-secret
   PAYPAL_WEBHOOK_ID=wh_sandbox_xxx
   ```
5. Use Sandbox buyer accounts for testing

---

## Production Checklist

- [ ] `PAYPAL_ENVIRONMENT=live`
- [ ] Live `PAYPAL_CLIENT_ID` / `PAYPAL_CLIENT_SECRET`
- [ ] Live webhook registered at `https://api.yourdomain.com/api/donations/webhook`
- [ ] `PAYPAL_WEBHOOK_ID` set to live webhook ID (not URL)
- [ ] `ALLOWED_ORIGINS` includes production domain
- [ ] SMTP configured for receipts
- [ ] Test end-to-end with real payment (small amount)
- [ ] Verify webhook events in PayPal dashboard
- [ ] Monitor `/health/ready` for PayPal connectivity

---

## Related Documents

- [API Endpoints](endpoints.md) — Full reference
- [Frontend Donations](../frontend/assistant-ui.md) — UI implementation
- [Deployment Runbook](../deployment/production-runbook.md) — Production setup