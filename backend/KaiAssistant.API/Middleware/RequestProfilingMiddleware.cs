using System.Diagnostics;
using KaiAssistant.API.Options;
using Microsoft.Extensions.Options;
namespace KaiAssistant.API.Middleware;

public sealed class RequestProfilingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly OpsOptions _options;
    private readonly ILogger<RequestProfilingMiddleware> _logger;
    public RequestProfilingMiddleware(
        RequestDelegate next,
        IOptions<OpsOptions> options,
        ILogger<RequestProfilingMiddleware> logger)
    {
        _next = next;
        _options = options.Value;
        _logger = logger;
    }
    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        await _next(context).ConfigureAwait(false);
        sw.Stop();
        var thresholdMs = Math.Max(1, _options.SlowRequestThresholdMs);
        var path = context.Request.Path.Value ?? string.Empty;
        var isHighVolume = _options.HighVolumePathPrefixes.Any(prefix =>
            path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        if (isHighVolume)
        {
            var sampleRate = Math.Clamp(_options.HighVolumeLogSampleRate, 0.01d, 1d);
            var sampled = Random.Shared.NextDouble() <= sampleRate;
            if (!sampled)
            {
                return;
            }
        }
        if (sw.ElapsedMilliseconds >= thresholdMs)
        {
            _logger.LogWarning(
                "Slow request detected: {Method} {Path} {StatusCode} took {ElapsedMs}ms payload={PayloadSize}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds,
                context.Request.ContentLength ?? 0);
        }
    }
}