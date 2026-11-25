using System.Diagnostics;
using KaiAssistant.Application.Interfaces;
namespace KaiAssistant.API.Middleware;

public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    public const string TraceHeaderName = "X-Trace-Id";
    private readonly RequestDelegate _next;
    private readonly IFeatureFlagService _flags;
    private readonly IInstanceIdentity _instanceIdentity;
    public CorrelationIdMiddleware(RequestDelegate next, IFeatureFlagService flags, IInstanceIdentity instanceIdentity)
    {
        _next = next;
        _flags = flags;
        _instanceIdentity = instanceIdentity;
    }
    public async Task InvokeAsync(HttpContext context, ILogger<CorrelationIdMiddleware> logger)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var incoming)
            ? incoming.ToString().Trim()
            : context.TraceIdentifier;
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = context.TraceIdentifier;
        }
        if (correlationId.Length > 128)
        {
            correlationId = correlationId[..128];
        }
        context.TraceIdentifier = correlationId;
        var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
        context.Response.Headers[HeaderName] = correlationId;
        context.Response.Headers[TraceHeaderName] = traceId;
        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["TraceId"] = traceId,
            ["SpanId"] = Activity.Current?.SpanId.ToString(),
            ["InstanceId"] = _instanceIdentity.InstanceId,
            ["ClientIp"] = context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            ["RequestPath"] = context.Request.Path.Value,
            ["RequestMethod"] = context.Request.Method,
            ["EnableCache"] = _flags.EnableCache,
            ["EnableRateLimiting"] = _flags.EnableRateLimiting
        }))
        {
            await _next(context).ConfigureAwait(false);
        }
    }
}