using KaiAssistant.Application.Interfaces;
using KaiAssistant.Domain.Entities.Visitors;
using KaiAssistant.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
namespace KaiAssistant.API.Controllers;

[ApiController]
[Route("api/visits")]
public sealed class VisitsController : ControllerBase
{
    private readonly IRepository<VisitorEvent> _repository;
    private readonly IVisitDeduplicationService _deduplication;
    private readonly ILogger<VisitsController> _logger;
    public VisitsController(
        IRepository<VisitorEvent> repository,
        IVisitDeduplicationService deduplication,
        ILogger<VisitsController> logger)
    {
        _repository = repository;
        _deduplication = deduplication;
        _logger = logger;
    }
    [HttpPost]
    public async Task<IActionResult> TrackVisit([FromBody] VisitorEventDto? dto, CancellationToken cancellationToken)
    {
        if (dto is null)
        {
            return BadRequest(new { message = "Invalid request payload." });
        }
        var visitedAtUtc = DateTimeOffset.UtcNow;
        var userAgent = FirstNonEmpty(dto.UserAgent, Request.Headers.UserAgent.ToString());
        var ipAddress = GetClientIp(Request) ?? "unknown";
        // Check deduplication: skip if refresh spam detected
        var shouldCount = await _deduplication.ShouldCountVisitAsync(
            ipAddress,
            userAgent ?? "unknown",
            TimeSpan.FromSeconds(30),
            cancellationToken)
            .ConfigureAwait(false);
        if (!shouldCount)
        {
            _logger.LogDebug("Visit deduplicated: IP={IP}", ipAddress);
            return Accepted(new { message = "Visit ignored (duplicate within debounce window)." });
        }
        // Persist full visitor event (regardless of dedup)
        var visitorEvent = new VisitorEvent
        {
            VisitedAtUtc = visitedAtUtc,
            SessionId = string.IsNullOrWhiteSpace(dto.SessionId) ? Guid.NewGuid().ToString("N") : dto.SessionId.Trim(),
            Path = NormalizePath(dto.Path),
            Referrer = TrimOrNull(dto.Referrer, 1024),
            UserAgent = TrimOrNull(userAgent, 2048),
            DeviceType = TrimOrNull(FirstNonEmpty(dto.DeviceType, InferDeviceType(userAgent)), 64),
            Browser = TrimOrNull(FirstNonEmpty(dto.Browser, InferBrowser(userAgent)), 128),
            OperatingSystem = TrimOrNull(FirstNonEmpty(dto.OperatingSystem, InferOs(userAgent)), 128),
            Timezone = TrimOrNull(dto.Timezone, 128),
            Language = TrimOrNull(dto.Language, 128),
            ScreenWidth = dto.ScreenWidth,
            ScreenHeight = dto.ScreenHeight,
            ViewportWidth = dto.ViewportWidth,
            ViewportHeight = dto.ViewportHeight,
            Platform = TrimOrNull(dto.Platform, 128),
            NetworkType = TrimOrNull(dto.NetworkType, 64),
            IpAddress = ipAddress,
            IsUniqueVisit = true  // Flagged by dedup service
        };
        await _repository.InsertAsync(visitorEvent, cancellationToken).ConfigureAwait(false);
        return Accepted(new { message = "Visit tracked." });
    }
    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }
        var normalized = path.Trim();
        if (!normalized.StartsWith('/'))
        {
            normalized = "/" + normalized;
        }
        return normalized.Length > 512 ? normalized[..512] : normalized;
    }
    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }
        return null;
    }
    private static string? TrimOrNull(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
    private static string? GetClientIp(HttpRequest request)
    {
        var forwarded = request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            return forwarded.Split(',')[0].Trim();
        }
        return request.HttpContext.Connection.RemoteIpAddress?.ToString();
    }
    private static string InferDeviceType(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return "unknown";
        }
        var ua = userAgent.ToLowerInvariant();
        if (ua.Contains("tablet") || ua.Contains("ipad")) return "tablet";
        if (ua.Contains("mobi") || ua.Contains("android")) return "mobile";
        return "desktop";
    }
    private static string InferBrowser(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return "unknown";
        }
        var ua = userAgent.ToLowerInvariant();
        if (ua.Contains("edg/")) return "Edge";
        if (ua.Contains("chrome/")) return "Chrome";
        if (ua.Contains("safari/") && !ua.Contains("chrome/")) return "Safari";
        if (ua.Contains("firefox/")) return "Firefox";
        if (ua.Contains("opr/") || ua.Contains("opera")) return "Opera";
        return "unknown";
    }
    private static string InferOs(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return "unknown";
        }
        var ua = userAgent.ToLowerInvariant();
        if (ua.Contains("windows")) return "Windows";
        if (ua.Contains("mac os") || ua.Contains("macintosh")) return "macOS";
        if (ua.Contains("android")) return "Android";
        if (ua.Contains("iphone") || ua.Contains("ipad") || ua.Contains("ios")) return "iOS";
        if (ua.Contains("linux")) return "Linux";
        return "unknown";
    }
    public sealed class VisitorEventDto
    {
        public string? SessionId { get; set; }
        public string? Path { get; set; }
        public string? Referrer { get; set; }
        public string? UserAgent { get; set; }
        public string? DeviceType { get; set; }
        public string? Browser { get; set; }
        public string? OperatingSystem { get; set; }
        public string? Timezone { get; set; }
        public string? Language { get; set; }
        public int? ScreenWidth { get; set; }
        public int? ScreenHeight { get; set; }
        public int? ViewportWidth { get; set; }
        public int? ViewportHeight { get; set; }
        public string? Platform { get; set; }
        public string? NetworkType { get; set; }
    }
}
