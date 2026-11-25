using Microsoft.AspNetCore.Mvc;
using KaiAssistant.Application.Interfaces;
namespace KaiAssistant.API.Controllers;

[ApiController]
[Route("/ops")]
public class ObservabilityController : ControllerBase
{
    private readonly IAssistantOrchestrator _orchestrator;
    private readonly IObservabilityService _observability;
    private readonly IVisitTrackingService _visitTracking;
    private readonly ILogger<ObservabilityController> _logger;
    public ObservabilityController(
        IAssistantOrchestrator orchestrator,
        IObservabilityService observability,
        IVisitTrackingService visitTracking,
        ILogger<ObservabilityController> logger)
    {
        _orchestrator = orchestrator;
        _observability = observability;
        _visitTracking = visitTracking;
        _logger = logger;
    }
    [HttpGet("diagnostics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDiagnostics(CancellationToken cancellationToken)
    {
        var orchestratorDiags = await _orchestrator.GetDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
        return Ok(new
        {
            timestamp = DateTimeOffset.UtcNow,
            ai = new
            {
                lastLatencyMs = orchestratorDiags.LastLatencyMs,
                lastCallAt = orchestratorDiags.LastAiCallAt,
                activeStreamCount = orchestratorDiags.ActiveStreamCount
            },
        });
    }
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Health(CancellationToken cancellationToken)
    {
        var diags = await _orchestrator.GetDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
        var healthy = diags.LastAiCallAt > DateTimeOffset.UtcNow.AddHours(-1)
            && diags.ActiveStreamCount >= 0;
        return healthy ? Ok(new { status = "healthy" }) : StatusCode(503, new { status = "degraded" });
    }
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(SystemObservabilityData), StatusCodes.Status200OK)]
    public async Task<SystemObservabilityData> GetDashboard(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Pro dashboard requested");
        return await _observability.GetDashboardAsync(cancellationToken).ConfigureAwait(false);
    }
    [HttpGet("cache-metrics")]
    [ProducesResponseType(typeof(CacheMetrics), StatusCodes.Status200OK)]
    public async Task<CacheMetrics> GetCacheMetrics(CancellationToken cancellationToken)
    {
        var dashboard = await _observability.GetDashboardAsync(cancellationToken)
            .ConfigureAwait(false);
        return dashboard.Cache;
    }
    [HttpGet("ai-latency")]
    [ProducesResponseType(typeof(AiPerformanceMetrics), StatusCodes.Status200OK)]
    public async Task<AiPerformanceMetrics> GetAiLatency(CancellationToken cancellationToken)
    {
        var dashboard = await _observability.GetDashboardAsync(cancellationToken)
            .ConfigureAwait(false);
        return dashboard.AiPerformance;
    }
    [HttpGet("conversations")]
    [ProducesResponseType(typeof(ConversationMetrics), StatusCodes.Status200OK)]
    public async Task<ConversationMetrics> GetConversationMetrics(CancellationToken cancellationToken)
    {
        var dashboard = await _observability.GetDashboardAsync(cancellationToken)
            .ConfigureAwait(false);
        return dashboard.Conversations;
    }
    [HttpGet("rate-limits")]
    [ProducesResponseType(typeof(RateLimitingMetrics), StatusCodes.Status200OK)]
    public async Task<RateLimitingMetrics> GetRateLimitingMetrics(CancellationToken cancellationToken)
    {
        var dashboard = await _observability.GetDashboardAsync(cancellationToken)
            .ConfigureAwait(false);
        return dashboard.RateLimiting;
    }
    [HttpGet("visits")]
    [ProducesResponseType(typeof(VisitMetrics), StatusCodes.Status200OK)]
    public async Task<VisitMetrics> GetVisitMetrics(CancellationToken cancellationToken)
    {
        var dashboard = await _observability.GetDashboardAsync(cancellationToken)
            .ConfigureAwait(false);
        return dashboard.Visits;
    }
    [HttpGet("visits/statistics")]
    [ProducesResponseType(typeof(VisitStatistics), StatusCodes.Status200OK)]
    public async Task<VisitStatistics> GetVisitStatistics(CancellationToken cancellationToken)
    {
        return await _visitTracking.GetStatisticsAsync(cancellationToken)
            .ConfigureAwait(false);
    }
    [HttpGet("latency-history")]
    [ProducesResponseType(typeof(IEnumerable<long>), StatusCodes.Status200OK)]
    public IActionResult GetLatencyHistory([FromQuery] int count = 100)
    {
        if (count < 1 || count > 1000)
            count = 100;
        var latencies = _observability.GetRecentLatencies(count);
        return Ok(latencies);
    }
}