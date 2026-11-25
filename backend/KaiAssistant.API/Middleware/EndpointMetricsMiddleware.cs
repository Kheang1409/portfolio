using System.Diagnostics;
using System.Diagnostics.Metrics;
using KaiAssistant.Application.Interfaces;
namespace KaiAssistant.API.Middleware;

public sealed class EndpointMetricsMiddleware
{
    private static readonly Meter Meter = new("KaiAssistant.ApiEndpoints", "1.0.0");
    private static readonly Histogram<double> EndpointDurationMs = Meter.CreateHistogram<double>("http_endpoint_duration_ms", "ms");
    private static readonly Counter<long> EndpointRequests = Meter.CreateCounter<long>("http_endpoint_requests_total");
    private readonly RequestDelegate _next;
    private readonly IInstanceIdentity _instanceIdentity;
    public EndpointMetricsMiddleware(RequestDelegate next, IInstanceIdentity instanceIdentity)
    {
        _next = next;
        _instanceIdentity = instanceIdentity;
    }
    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        await _next(context).ConfigureAwait(false);
        sw.Stop();
        var endpoint = context.GetEndpoint()?.DisplayName ?? context.Request.Path.Value ?? "unknown";
        var statusCode = context.Response.StatusCode;
        EndpointRequests.Add(1,
            KeyValuePair.Create<string, object?>("endpoint", endpoint),
            KeyValuePair.Create<string, object?>("status", statusCode),
            KeyValuePair.Create<string, object?>("instance_id", _instanceIdentity.InstanceId));
        EndpointDurationMs.Record(sw.Elapsed.TotalMilliseconds,
            KeyValuePair.Create<string, object?>("endpoint", endpoint),
            KeyValuePair.Create<string, object?>("status", statusCode),
            KeyValuePair.Create<string, object?>("instance_id", _instanceIdentity.InstanceId));
    }
}