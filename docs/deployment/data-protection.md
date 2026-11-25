# Data Protection Strategy

## Overview

This document defines the data protection strategy for production deployment, covering MongoDB backup procedures, Redis persistence configuration, and disaster recovery procedures.

---

## MongoDB Backup Strategy

### Current Architecture
- **Database:** MongoDB Atlas (Managed Service)
- **Cluster:** M10+ (production)
- **Database Name:** `KaiAssistant`
- **Collections:** `donations`, `donationWebhookEvents`, `donationReceipts`, `visitors`, `conversations`, `rateLimitKeys`, `knowledgeChunks`, `resumes`, `indexState`

### Automated Backups (Atlas Built-in)

| Setting | Value | Notes |
|---------|-------|-------|
| **Backup Frequency** | Continuous (Oplog-based) | Point-in-time recovery |
| **Retention** | 30+ days (M10+) | Configure per tier |
| **Snapshot Schedule** | Daily at 03:00 UTC | Configurable |
| **PITR Window** | 7 days (M10+) | Point-in-time recovery |

### Manual Backup Triggers
```bash
# Trigger manual snapshot via Atlas CLI
atlas backups snapshots create --clusterName <CLUSTER_NAME> --description "Pre-deployment backup"

# Or via Atlas UI: Clusters → Backup → Take Snapshot
```

### Collections Requiring Backup Priority

| Priority | Collections | Reason |
|----------|-------------|--------|
| **Critical (P0)** | `donations`, `donationWebhookEvents`, `donationReceipts` | Financial records, regulatory compliance, idempotency keys |
| **High (P1)** | `visitors` | Analytics, unique visitor tracking |
| **Medium (P2)** | `conversations`, `knowledgeChunks` | AI conversation history, rebuildable from resume |
| **Low (P3)** | `rateLimitKeys`, `indexState` | Ephemeral, auto-expires (TTL index) |

### Restore Procedures

#### Point-in-Time Recovery (PITR)
1. Go to Atlas UI → Clusters → Backup → Restore
2. Select "Point in Time"
3. Choose timestamp (within retention window)
4. Restore to **new cluster** (recommended) or existing
5. Update `MONGODB_CONNECTIONSTRING` to restored cluster
6. Verify application health checks

#### Snapshot Restore
1. Atlas UI → Backup → Snapshots
2. Select snapshot → Restore
3. Choose target cluster (new or existing)
4. Wait for restore completion
5. Update connection string
6. Run data integrity verification

#### Collection-Level Restore (mongorestore)
```bash
# Export specific collections
mongodump --uri="mongodb+srv://..." --db=KaiAssistant --collection=donations --out=/backup

# Restore to target
mongorestore --uri="mongodb+srv://..." --db=KaiAssistant --collection=donations /backup/KaiAssistant/donations.bson
```

### Backup Verification

| Check | Frequency | Method |
|-------|-----------|--------|
| Backup job success | Daily | Atlas Alerts → Backup Status |
| Restore test | Monthly | Restore to test cluster, verify document counts |
| PITR validation | Quarterly | Restore to 1 hour ago, verify data consistency |
| Cross-region replica | Per tier | Enable Atlas Cross-Region Replication (M10+) |

### Retention Policy

| Data Type | Retention | Legal Basis |
|-----------|-----------|-------------|
| Donation records | 7 years | Financial regulations |
| Webhook events | 90 days | Debugging, audit trail |
| Visitor analytics | 2 years | Business analytics |
| AI conversations | 1 year | Product improvement |
| Rate limit keys | TTL (24h) | Auto-expire |
| Knowledge chunks | Until resume update | Rebuildable |

---

## Redis Persistence Requirements

### Current Architecture
- **Redis Version:** 7.0
- **Deployment:** Docker container (single instance)
- **TLS:** Required in Production (`rediss://` on port 6380)
- **Use Cases:** Distributed rate limiting, AI circuit breaker state, session caching

### Persistence Configuration

#### RDB (Snapshotting) - Recommended for Production
```redis
# In redis.conf or via command
save 900 1      # Save after 900 sec if at least 1 key changed
save 300 10     # Save after 300 sec if at least 10 keys changed
save 60 10000   # Save after 60 sec if at least 10000 keys changed

# RDB file settings
dbfilename dump.rdb
dir /data
rdbcompression yes
rdbchecksum yes
```

#### AOF (Append-Only File) - Optional for Durability
```redis
# Enable AOF for better durability (slower writes)
appendonly yes
appendfilename "appendonly.aof"
appendfsync everysec  # Options: always, everysec, no
auto-aof-rewrite-percentage 100
auto-aof-rewrite-min-size 64mb
```

#### Recommended Production Config (Balanced)
```redis
# RDB for fast recovery + AOF for durability
save 300 10
appendonly yes
appendfsync everysec

# Memory management
maxmemory 256mb
maxmemory-policy allkeys-lru
```

### Docker Compose Production Redis Config
```yaml
# docker-compose.prod.yml
services:
  redis:
    image: redis:7.0
    command: >
      redis-server
      --save 300 10
      --appendonly yes
      --appendfsync everysec
      --maxmemory 256mb
      --maxmemory-policy allkeys-lru
      --tls-port 6380
      --port 0
      --tls-cert-file /etc/redis/tls/redis.crt
      --tls-key-file /etc/redis/tls/redis.key
      --tls-ca-cert-file /etc/redis/tls/ca.crt
      --tls-auth-clients no
    volumes:
      - redis-data:/data
      - redis-certs:/etc/redis/tls:ro
    deploy:
      resources:
        limits:
          memory: 256M
          cpus: '0.25'
    restart: unless-stopped

volumes:
  redis-data:  # Named volume for persistence
  redis-certs:
    external: true
```

### Data Classification in Redis

| Data Type | TTL | Persistence Priority | Recovery Impact |
|-----------|-----|---------------------|-----------------|
| Rate limit counters | 60-3600s | Low (ephemeral) | Brief rate limit reset |
| AI circuit breaker state | 30s-5min | Medium | Circuit breaker reset (brief degraded mode) |
| Session cache | 15-30min | Medium | User re-authentication |
| Visitor tracking | 24h | Low | Analytics gap |

### Redis Backup Strategy

#### Automated RDB Backup (via Cron in Container)
```bash
# Add to redis container or sidecar
# Backup every 6 hours
0 */6 * * * redis-cli -u rediss://redis:6380 --tls --cert /etc/redis/tls/redis.crt --key /etc/redis/tls/redis.key --cacert /etc/redis/tls/ca.crt BGSAVE
```

#### Manual Backup
```bash
# Trigger background save
docker exec redis redis-cli -u rediss://redis:6380 --tls BGSAVE

# Copy RDB file
docker cp redis:/data/dump.rdb ./backup/redis-dump-$(date +%Y%m%d).rdb
```

#### Restore Redis Data
```bash
# Stop Redis
docker compose -f docker-compose.yml -f docker-compose.prod.yml stop redis

# Replace RDB file
docker cp ./backup/redis-dump-20261123.rdb redis:/data/dump.rdb

# Start Redis
docker compose -f docker-compose.yml -f docker-compose.prod.yml start redis
```

---

## Disaster Recovery Procedures

### Scenario 1: MongoDB Atlas Regional Outage

**RTO:** 15 minutes (Atlas auto-failover)  
**RPO:** < 1 second (multi-region cluster)

**Procedure:**
1. Atlas automatically fails over to secondary region (if configured)
2. Verify application health checks
3. If manual failover needed: Atlas UI → Clusters → Failover
4. Update DNS/connection string if not using Atlas SRV

### Scenario 2: MongoDB Data Corruption / Accidental Deletion

**RTO:** 30-60 minutes  
**RPO:** 0 (PITR) to 24 hours (snapshot)

**Procedure:**
1. Identify corruption scope (collection vs database)
2. Use PITR to restore to timestamp before corruption
3. Restore to new cluster
4. Verify data integrity (document counts, referential integrity)
5. Switch application connection string
6. Monitor for 30 minutes

### Scenario 3: Redis Complete Failure

**RTO:** 5 minutes  
**RPO:** Up to 6 hours (RDB snapshot interval)

**Procedure:**
1. Deploy new Redis container with restored RDB
2. Verify TLS configuration
3. Application reconnects automatically (circuit breaker handles transient failure)
4. Rate limits temporarily reset (acceptable)

### Scenario 4: Complete Environment Loss

**RTO:** 2-4 hours  
**RPO:** MongoDB: < 1s (Atlas) / Redis: 6h

**Procedure:**
1. Provision new infrastructure (VMs, Kubernetes, etc.)
2. Deploy docker-compose stack
3. Restore MongoDB from Atlas PITR/snapshot
4. Restore Redis from latest RDB backup
5. Configure secrets from secret store
6. Update DNS
7. Run full smoke test suite
8. Monitor for 1 hour

---

## Monitoring & Alerting for Data Protection

### MongoDB Atlas Alerts (Configure in Atlas UI)

| Alert | Condition | Severity | Action |
|-------|-----------|----------|--------|
| Backup Failed | Backup job fails | Critical | Page on-call |
| Backup Behind | No backup in 25 hours | Warning | Investigate |
| Oplog Window | < 1 hour oplog window | Warning | Check replication lag |
| Disk Usage | > 80% storage used | Warning | Scale cluster |

### Redis Alerts (via Application Metrics)

| Metric | Threshold | Severity | Action |
|--------|-----------|----------|--------|
| Redis connectivity | Health check fails 3x | Critical | Page on-call |
| Redis memory | > 90% maxmemory | Warning | Investigate key explosion |
| Redis persistence | BGSAVE fails | Warning | Check disk space |
| Rate limit fallback | In-memory mode active | Info | Redis connectivity issue |

### Application-Level Data Integrity Checks

```csharp
// Scheduled job (daily)
public async Task<DataIntegrityReport> RunIntegrityCheckAsync()
{
    var report = new DataIntegrityReport();
    
    // Donation referential integrity
    report.DonationWebhookEventsWithoutDonation = await _db.DonationWebhookEvents
        .CountDocumentsAsync(e => !_db.Donations.Find(d => d.Id == e.DonationId).Any());
    
    // Receipt completeness
    report.DonationsWithoutReceipt = await _db.Donations
        .CountDocumentsAsync(d => d.Status == "COMPLETED" && 
            !_db.DonationReceipts.Find(r => r.DonationId == d.Id).Any());
    
    // Idempotency key uniqueness
    report.DuplicateIdempotencyKeys = await _db.Donations
        .Aggregate()
        .Group(new BsonDocument { { "_id", "$IdempotencyKey" }, { "count", new BsonDocument("$sum", 1) } })
        .Match(new BsonDocument("count", new BsonDocument("$gt", 1)))
        .CountDocumentsAsync();
    
    return report;
}
```

---

## Compliance & Audit

### Data Handling Summary

| Regulation | Applicable | Controls |
|------------|------------|----------|
| GDPR | Yes (if EU visitors) | Right to deletion (visitor data), data minimization, consent |
| CCPA | Yes (if CA visitors) | Opt-out, data access, deletion |
| PCI DSS | No (PayPal handles card data) | PayPal SAQ-A compliance |
| SOX | No | N/A |

### Audit Trail Requirements

- All donation mutations logged with correlation ID
- Webhook events stored with full payload (90 days)
- Admin actions (if any) logged with user identity
- Backup/restore operations logged with operator identity

---

## Related Documents

- `PRODUCTION_ENVIRONMENT_VARIABLES.md` - Environment configuration
- `PRODUCTION_DEPLOYMENT_RUNBOOK.md` - Deployment procedures
- `docker-compose.prod.yml` - Production infrastructure config
- [Architecture: Data Protection](../architecture/scalability.md) - Redis persistence details

---

## Appendix: Quick Reference Commands

```bash
# MongoDB Atlas CLI (install: https://www.mongodb.com/docs/atlas/cli/stable/)
atlas backups snapshots list --clusterName <CLUSTER>
atlas backups restoreJobs create --clusterName <CLUSTER> --snapshotId <SNAPSHOT_ID>

# Redis backup
docker exec redis redis-cli -u rediss://redis:6380 --tls BGSAVE
docker cp redis:/data/dump.rdb ./redis-backup-$(date +%Y%m%d).rdb

# Verify Redis data
docker exec redis redis-cli -u rediss://redis:6380 --tls INFO memory
docker exec redis redis-cli -u rediss://redis:6380 --tls DBSIZE

# Application health
curl http://localhost:5000/health/ready
curl http://localhost:3000/api/health/live
```