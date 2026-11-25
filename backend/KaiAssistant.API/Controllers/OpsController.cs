using System.Security.Cryptography;
using System.Text;
using KaiAssistant.API.Options;
using KaiAssistant.API.Services;
using KaiAssistant.Application.Diagnostics;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Options;
using KaiAssistant.Infrastructure.Cache;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
namespace KaiAssistant.API.Controllers;

[ApiController]
[Route("ops")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class OpsController : ControllerBase
{
    private readonly IHostEnvironment _environment;
    private readonly OpsOptions _opsOptions;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IEventIdempotencyStore _idempotencyStore;
    private readonly IOutboxProcessorState _outboxState;
    private readonly IInstanceIdentity _instanceIdentity;
    private readonly IResilienceStatusProvider _resilience;
    private readonly IRateLimitTelemetry _rateLimitTelemetry;
    private readonly IModelHealthService _modelHealth;
    private readonly ICacheDiagnosticsService _cacheDiagnostics;
    private readonly IFeatureFlagService _flags;
    private readonly IMongoDatabase _mongoDatabase;
    private readonly IRedisConnectionFactory _redisFactory;
    private readonly OutboxRecoveryOptions _outboxRecoveryOptions;
    private readonly OpsSecurityOptions _opsSecurityOptions;
    private readonly LoadTestHooksOptions _loadTestHooksOptions;
    private readonly IOperationalSimulationState _simulationState;
    private readonly IAiTrafficSimulationService _trafficSimulation;
    private readonly IAiDecisionAuditStore _decisionAuditStore;
    private readonly ILogger<OpsController> _logger;
    public OpsController(
        IHostEnvironment environment,
        IOptions<OpsOptions> opsOptions,
        IOutboxRepository outboxRepository,
        IEventIdempotencyStore idempotencyStore,
        IOutboxProcessorState outboxState,
        IInstanceIdentity instanceIdentity,
        IResilienceStatusProvider resilience,
        IRateLimitTelemetry rateLimitTelemetry,
        IModelHealthService modelHealth,
        ICacheDiagnosticsService cacheDiagnostics,
        IFeatureFlagService flags,
        IMongoDatabase mongoDatabase,
        IRedisConnectionFactory redisFactory,
        IOptions<OutboxRecoveryOptions> outboxRecoveryOptions,
        IOptions<OpsSecurityOptions> opsSecurityOptions,
        IOptions<LoadTestHooksOptions> loadTestHooksOptions,
        IOperationalSimulationState simulationState,
        IAiTrafficSimulationService trafficSimulation,
        IAiDecisionAuditStore decisionAuditStore,
        ILogger<OpsController> logger)
    {
        _environment = environment;
        _opsOptions = opsOptions.Value;
        _outboxRepository = outboxRepository;
        _idempotencyStore = idempotencyStore;
        _outboxState = outboxState;
        _instanceIdentity = instanceIdentity;
        _resilience = resilience;
        _rateLimitTelemetry = rateLimitTelemetry;
        _modelHealth = modelHealth;
        _cacheDiagnostics = cacheDiagnostics;
        _flags = flags;
        _mongoDatabase = mongoDatabase;
        _redisFactory = redisFactory;
        _outboxRecoveryOptions = outboxRecoveryOptions.Value;
        _opsSecurityOptions = opsSecurityOptions.Value;
        _loadTestHooksOptions = loadTestHooksOptions.Value;
        _simulationState = simulationState;
        _trafficSimulation = trafficSimulation;
        _decisionAuditStore = decisionAuditStore;
        _logger = logger;
    }
    [HttpGet("health/detailed")]
    public async Task<IActionResult> DetailedHealth(CancellationToken cancellationToken)
    {
        if (!IsOpsAllowed())
        {
            return NotFound();
        }
        var mongoOk = await CheckMongoAsync(cancellationToken).ConfigureAwait(false);
        var redisOk = await CheckRedisAsync(cancellationToken).ConfigureAwait(false);
        var state = _outboxState.Snapshot;
        var payload = new
        {
            instanceId = _instanceIdentity.InstanceId,
            mongo = new { healthy = mongoOk },
            redis = new { healthy = redisOk, enabled = _flags.EnableCache },
            outboxProcessor = state,
            cache = new
            {
                enabled = _flags.EnableCache,
                redisConnected = _cacheDiagnostics.IsRedisConnected
            }
        };
        var isHealthy = mongoOk && (!_flags.EnableCache || redisOk);
        return isHealthy ? Ok(payload) : StatusCode(StatusCodes.Status503ServiceUnavailable, payload);
    }
    [HttpGet("outbox")]
    public async Task<IActionResult> Outbox(CancellationToken cancellationToken)
    {
        if (!IsOpsAllowed())
        {
            return NotFound();
        }
        var stats = await _outboxRepository.GetStatsAsync(DateTimeOffset.UtcNow, cancellationToken).ConfigureAwait(false);
        return Ok(new
        {
            instanceId = _instanceIdentity.InstanceId,
            stats
        });
    }
    [HttpGet("cache")]
    public async Task<IActionResult> Cache(CancellationToken cancellationToken)
    {
        if (!IsOpsAllowed())
        {
            return NotFound();
        }
        var keyCount = await _cacheDiagnostics.GetKeyCountAsync(cancellationToken).ConfigureAwait(false);
        return Ok(new
        {
            instanceId = _instanceIdentity.InstanceId,
            hitCount = _cacheDiagnostics.HitCount,
            missCount = _cacheDiagnostics.MissCount,
            hitRatio = _cacheDiagnostics.HitRatio,
            rebuildCount = _cacheDiagnostics.RebuildCount,
            averageLockWaitMs = _cacheDiagnostics.AverageLockWaitMs,
            keyCount,
            redisConnected = _cacheDiagnostics.IsRedisConnected,
            cacheEnabled = _flags.EnableCache
        });
    }
    [HttpGet("debug/config")]
    public IActionResult DebugConfig()
    {
        if (!IsOpsAllowed())
        {
            return NotFound();
        }
        return Ok(new
        {
            instanceId = _instanceIdentity.InstanceId,
            environment = _environment.EnvironmentName,
            featureFlags = new
            {
                _flags.EnableOutboxProcessing,
                _flags.EnableOutboxRecovery,
                _flags.EnableAiResponseCache,
                _flags.EnableAssistantBatching,
                _flags.EnableCache,
                _flags.EnableRateLimiting,
                _flags.EnableStreaming,
                _flags.EnableSemanticCaching,
                _flags.EnableRag,
                _flags.EnableConversationMemory,
                _flags.PreferredAiProvider
            },
            cache = new
            {
                redisConnected = _cacheDiagnostics.IsRedisConnected
            }
        });
    }
    [HttpGet("resilience")]
    public IActionResult Resilience()
    {
        if (!IsOpsAllowed())
        {
            return NotFound();
        }
        return Ok(new
        {
            instanceId = _instanceIdentity.InstanceId,
            snapshot = _resilience.GetSnapshot()
        });
    }
    [HttpGet("rate-limit")]
    public async Task<IActionResult> RateLimit([FromQuery] int minutes = 10, [FromQuery] int top = 5, CancellationToken cancellationToken = default)
    {
        if (!IsOpsAllowed())
        {
            return NotFound();
        }
        var snapshot = await _rateLimitTelemetry.SnapshotAsync(minutes, top, cancellationToken).ConfigureAwait(false);
        return Ok(new
        {
            instanceId = _instanceIdentity.InstanceId,
            snapshot
        });
    }
    [HttpGet("ai-models")]
    public IActionResult AiModelsLegacy()
    {
        return AiModels();
    }
    [HttpGet("ai/models")]
    public IActionResult AiModels()
    {
        if (!IsOpsAllowed())
        {
            return NotFound();
        }
        var snapshot = _modelHealth.GetSnapshot(DateTimeOffset.UtcNow);
        return Ok(new
        {
            instanceId = _instanceIdentity.InstanceId,
            activeModel = snapshot.ActiveModel,
            snapshot
        });
    }
    [HttpGet("ai/metrics")]
    public IActionResult AiMetrics()
    {
        if (!IsOpsAllowed())
        {
            return NotFound();
        }
        var snapshot = _modelHealth.GetSnapshot(DateTimeOffset.UtcNow);
        var models = snapshot.Models.ToList();
        var totalInputTokens = models.Sum(x => x.TotalInputTokens);
        var totalOutputTokens = models.Sum(x => x.TotalOutputTokens);
        var totalCost = models.Sum(x => x.TotalEstimatedCostUsd);
        var fallbackTotal = models.Sum(x => x.FallbackUsageCount);
        var topScores = models
            .OrderByDescending(x => x.DynamicScore)
            .Take(5)
            .Select(x => new
            {
                x.ModelName,
                x.DynamicScore,
                x.WeightedSuccessRate,
                x.AverageLatencyMs,
                x.RecentFailureRate,
                x.CooldownFrequency
            })
            .ToList();
        return Ok(new
        {
            instanceId = _instanceIdentity.InstanceId,
            snapshotAtUtc = snapshot.CapturedAtUtc,
            stateVersion = snapshot.StateVersion,
            activeModel = snapshot.ActiveModel,
            totals = new
            {
                modelCount = models.Count,
                totalInputTokens,
                totalOutputTokens,
                totalEstimatedCostUsd = totalCost,
                totalFallbackCount = fallbackTotal
            },
            topScores,
            models = models.Select(x => new
            {
                x.ModelName,
                x.IsHealthy,
                x.DynamicScore,
                x.WeightedSuccessRate,
                x.AverageLatencyMs,
                x.RecentFailureRate,
                x.CooldownFrequency,
                x.SuccessCount,
                x.FailureCount,
                x.FallbackUsageCount,
                x.TotalInputTokens,
                x.TotalOutputTokens,
                x.TotalEstimatedCostUsd,
                x.CircuitOpenUntilUtc,
                x.LastFailureReason
            })
        });
    }
    [HttpPost("ai/evaluate")]
    public async Task<IActionResult> EvaluateAi([FromBody] AiEvaluateRequest request, CancellationToken cancellationToken)
    {
        if (!IsOpsAllowed())
        {
            return NotFound();
        }
        if (request?.Prompts is null || request.Prompts.Count == 0)
        {
            return BadRequest(new { message = "At least one prompt is required." });
        }
        var result = await HttpContext.RequestServices
            .GetRequiredService<IAiEvaluationService>()
            .EvaluateAsync(request.Prompts, cancellationToken)
            .ConfigureAwait(false);
        return Ok(new
        {
            instanceId = _instanceIdentity.InstanceId,
            result.CreatedAtUtc,
            result.AverageLatencyMs,
            result.AverageQualityScore,
            count = result.Entries.Count,
            entries = result.Entries
        });
    }
    [HttpGet("feature-flags")]
    public async Task<IActionResult> GetFeatureFlags(CancellationToken cancellationToken)
    {
        if (!IsOpsAllowed())
        {
            return NotFound();
        }
        var flags = await _flags.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return Ok(flags);
    }
    [HttpPost("feature-flags/{name}")]
    public async Task<IActionResult> SetFeatureFlag(string name, [FromBody] SetFeatureFlagRequest request, CancellationToken cancellationToken)
    {
        if (!IsOpsAllowed())
        {
            return NotFound();
        }
        if (request is null || string.IsNullOrWhiteSpace(request.Value))
        {
            return BadRequest(new { message = "Flag value is required." });
        }
        var updated = await _flags.SetFlagAsync(name, request.Value, cancellationToken).ConfigureAwait(false);
        return updated ? Ok(new { updated = true, name, value = request.Value }) : BadRequest(new { updated = false, name });
    }
    [HttpPost("ai/simulation/run")]
    public async Task<IActionResult> RunAiSimulation([FromQuery] int? count, CancellationToken cancellationToken)
    {
        if (!IsOpsAllowed())
        {
            return NotFound();
        }
        if (!IsLoadTestHooksEnabled())
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Load test hooks are disabled." });
        }
        var report = await _trafficSimulation.RunOnceAsync(count, cancellationToken).ConfigureAwait(false);
        return Ok(new
        {
            instanceId = _instanceIdentity.InstanceId,
            report
        });
    }
    [HttpGet("ai/simulation/snapshots")]
    public IActionResult GetAiSimulationSnapshots([FromQuery] int max = 120)
    {
        if (!IsOpsAllowed())
        {
            return NotFound();
        }
        var snapshots = _trafficSimulation.GetSnapshots(max);
        return Ok(new
        {
            instanceId = _instanceIdentity.InstanceId,
            count = snapshots.Count,
            snapshots
        });
    }
    [HttpGet("ai/simulation/report")]
    public IActionResult GetAiSimulationReport()
    {
        if (!IsOpsAllowed())
        {
            return NotFound();
        }
        return Ok(new
        {
            instanceId = _instanceIdentity.InstanceId,
            report = _trafficSimulation.GetLastReport()
        });
    }
    [HttpGet("ai/decisions")]
    public IActionResult GetAiDecisionAudit([FromQuery] int max = 100)
    {
        if (!IsOpsAllowed())
        {
            return NotFound();
        }
        var entries = _decisionAuditStore.GetRecent(max);
        return Ok(new
        {
            instanceId = _instanceIdentity.InstanceId,
            count = entries.Count,
            entries
        });
    }
    [HttpGet("simulate")]
    public async Task<IActionResult> SimulationState(CancellationToken cancellationToken)
    {
        if (!IsOpsAllowed())
        {
            return NotFound();
        }
        if (!IsLoadTestHooksEnabled())
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Load test hooks are disabled." });
        }
        var aiThrottleUntilUtc = await _simulationState.GetForceAiThrottleUntilUtcAsync(cancellationToken).ConfigureAwait(false);
        var outboxArtificialDelayMs = await _simulationState.GetOutboxArtificialDelayMsAsync(cancellationToken).ConfigureAwait(false);
        return Ok(new
        {
            instanceId = _instanceIdentity.InstanceId,
            aiThrottleUntilUtc,
            outboxArtificialDelayMs
        });
    }
    [HttpPost("simulate/ai-throttle")]
    public async Task<IActionResult> SimulateAiThrottle([FromQuery] int seconds = 30, CancellationToken cancellationToken = default)
    {
        if (!IsOpsAllowed())
        {
            return NotFound();
        }
        if (!IsLoadTestHooksEnabled())
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Load test hooks are disabled." });
        }
        var boundedSeconds = Math.Clamp(seconds, 1, 600);
        await _simulationState.ForceAiThrottleForAsync(TimeSpan.FromSeconds(boundedSeconds), cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("AuditSimulateAiThrottle: seconds={Seconds}", boundedSeconds);
        var untilUtc = await _simulationState.GetForceAiThrottleUntilUtcAsync(cancellationToken).ConfigureAwait(false);
        return Ok(new
        {
            applied = true,
            seconds = boundedSeconds,
            untilUtc
        });
    }
    [HttpPost("simulate/outbox-delay")]
    public async Task<IActionResult> SimulateOutboxDelay([FromQuery] int milliseconds = 0, CancellationToken cancellationToken = default)
    {
        if (!IsOpsAllowed())
        {
            return NotFound();
        }
        if (!IsLoadTestHooksEnabled())
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Load test hooks are disabled." });
        }
        var bounded = Math.Clamp(milliseconds, 0, 15_000);
        await _simulationState.SetOutboxArtificialDelayAsync(bounded, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("AuditSimulateOutboxDelay: milliseconds={Milliseconds}", bounded);
        var outboxArtificialDelayMs = await _simulationState.GetOutboxArtificialDelayMsAsync(cancellationToken).ConfigureAwait(false);
        return Ok(new
        {
            applied = true,
            outboxArtificialDelayMs
        });
    }
    [HttpPost("outbox/replay/{id}")]
    public async Task<IActionResult> ReplayOutboxMessage(string id, CancellationToken cancellationToken)
    {
        if (!IsOpsAllowed())
        {
            return NotFound();
        }
        if (!IsRecoveryEnabled())
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Outbox recovery endpoints are disabled." });
        }
        var message = await _outboxRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (message is null)
        {
            return NotFound(new { message = "Outbox message not found." });
        }
        if (message.ProcessedAtUtc is not null)
        {
            return BadRequest(new { message = "Outbox message is already processed and cannot be replayed." });
        }
        if (string.IsNullOrWhiteSpace(message.LastError))
        {
            return BadRequest(new { message = "Only failed outbox messages can be replayed." });
        }
        if (!string.IsNullOrWhiteSpace(message.IdempotencyKey) &&
            await _idempotencyStore.IsProcessedAsync(message.IdempotencyKey, cancellationToken).ConfigureAwait(false))
        {
            return BadRequest(new { message = "Outbox side effects were already processed for this idempotency key." });
        }
        var replayed = await _outboxRepository
            .ReplayFailedAsync(id, DateTimeOffset.UtcNow, cancellationToken)
            .ConfigureAwait(false);
        if (!replayed)
        {
            return BadRequest(new { message = "Failed to replay outbox message." });
        }
        _logger.LogInformation("AuditOutboxReplaySingle: messageId={MessageId}", id);
        return Ok(new { instanceId = _instanceIdentity.InstanceId, messageId = id, replayed = true });
    }
    [HttpPost("outbox/replay-failed")]
    public async Task<IActionResult> ReplayFailedOutbox([FromQuery] int? batchSize, CancellationToken cancellationToken)
    {
        if (!IsOpsAllowed())
        {
            return NotFound();
        }
        if (!IsRecoveryEnabled())
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Outbox recovery endpoints are disabled." });
        }
        var requested = batchSize.GetValueOrDefault(25);
        var effectiveBatch = Math.Clamp(requested, 1, Math.Max(1, _outboxRecoveryOptions.MaxReplayBatchSize));
        var replayed = await _outboxRepository
            .ReplayFailedBatchAsync(effectiveBatch, DateTimeOffset.UtcNow, cancellationToken)
            .ConfigureAwait(false);
        _logger.LogInformation(
            "AuditOutboxReplayBatch: requestedBatch={RequestedBatch} effectiveBatch={EffectiveBatch} replayedCount={ReplayedCount}",
            requested,
            effectiveBatch,
            replayed);
        return Ok(new
        {
            instanceId = _instanceIdentity.InstanceId,
            requestedBatch = requested,
            effectiveBatch,
            replayedCount = replayed
        });
    }
    [HttpPost("outbox/dead-letter/{id}")]
    public async Task<IActionResult> DeadLetterOutboxMessage(string id, CancellationToken cancellationToken)
    {
        if (!IsOpsAllowed())
        {
            return NotFound();
        }
        if (!IsRecoveryEnabled())
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Outbox recovery endpoints are disabled." });
        }
        var message = await _outboxRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (message is null)
        {
            return NotFound(new { message = "Outbox message not found." });
        }
        if (message.ProcessedAtUtc is not null)
        {
            return BadRequest(new { message = "Outbox message is already processed." });
        }
        var deadLettered = await _outboxRepository
            .DeadLetterAsync(id, "manual", DateTimeOffset.UtcNow, cancellationToken)
            .ConfigureAwait(false);
        if (!deadLettered)
        {
            return BadRequest(new { message = "Failed to dead-letter outbox message." });
        }
        _logger.LogInformation("AuditOutboxDeadLetter: messageId={MessageId}", id);
        return Ok(new { instanceId = _instanceIdentity.InstanceId, messageId = id, deadLettered = true });
    }
    private bool IsOpsAllowed()
    {
        if (!(_environment.IsDevelopment() || _opsOptions.EnabledInProduction))
        {
            return false;
        }
        if (!_opsSecurityOptions.Enabled)
        {
            return true;
        }
        if (!IsRequestFromAllowedIp())
        {
            return false;
        }
        if (_opsSecurityOptions.RequireJwt)
        {
            return User?.Identity?.IsAuthenticated == true;
        }
        if (string.IsNullOrWhiteSpace(_opsSecurityOptions.ApiKey))
        {
            return false;
        }
        if (!Request.Headers.TryGetValue(_opsSecurityOptions.HeaderName, out var provided))
        {
            return false;
        }
        var providedBytes = Encoding.UTF8.GetBytes(provided.ToString());
        var expectedBytes = Encoding.UTF8.GetBytes(_opsSecurityOptions.ApiKey);
        if (providedBytes.Length != expectedBytes.Length)
        {
            return false;
        }
        return CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
    private bool IsRequestFromAllowedIp()
    {
        if (_opsSecurityOptions.AllowedIpRanges is null || _opsSecurityOptions.AllowedIpRanges.Length == 0)
        {
            return true;
        }
        var remoteIp = HttpContext.Connection.RemoteIpAddress;
        if (remoteIp is null)
        {
            return false;
        }
        foreach (var range in _opsSecurityOptions.AllowedIpRanges)
        {
            if (string.IsNullOrWhiteSpace(range))
            {
                continue;
            }
            if (TryIsIpInRange(remoteIp, range))
            {
                return true;
            }
        }
        return false;
    }
    private static bool TryIsIpInRange(IPAddress remoteIp, string range)
    {
        if (IPAddress.TryParse(range, out var exact))
        {
            return exact.Equals(remoteIp);
        }
        var slashIndex = range.IndexOf('/');
        if (slashIndex <= 0)
        {
            return false;
        }
        var baseIpText = range[..slashIndex];
        var prefixText = range[(slashIndex + 1)..];
        if (!IPAddress.TryParse(baseIpText, out var baseIp) || !int.TryParse(prefixText, out var prefixLength))
        {
            return false;
        }
        var remoteBytes = remoteIp.GetAddressBytes();
        var baseBytes = baseIp.GetAddressBytes();
        if (remoteBytes.Length != baseBytes.Length)
        {
            return false;
        }
        var fullBytes = prefixLength / 8;
        var remainderBits = prefixLength % 8;
        for (var i = 0; i < fullBytes; i++)
        {
            if (remoteBytes[i] != baseBytes[i])
            {
                return false;
            }
        }
        if (remainderBits == 0)
        {
            return true;
        }
        var mask = (byte)~(255 >> remainderBits);
        return (remoteBytes[fullBytes] & mask) == (baseBytes[fullBytes] & mask);
    }
    private bool IsRecoveryEnabled()
    {
        return _outboxRecoveryOptions.EnableReplayEndpoints && _flags.EnableOutboxRecovery;
    }
    private bool IsLoadTestHooksEnabled()
    {
        return _loadTestHooksOptions.Enabled;
    }
    private async Task<bool> CheckMongoAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _mongoDatabase.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }
    private async Task<bool> CheckRedisAsync(CancellationToken cancellationToken)
    {
        var redis = await _redisFactory.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        if (redis is null)
        {
            return false;
        }
        try
        {
            var db = redis.GetDatabase();
            await db.PingAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            return redis.IsConnected;
        }
        catch
        {
            return false;
        }
    }
    public sealed class AiEvaluateRequest
    {
        public List<string> Prompts { get; set; } = [];
    }
    public sealed class SetFeatureFlagRequest
    {
        public string Value { get; set; } = string.Empty;
    }
}