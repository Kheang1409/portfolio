using System.Diagnostics.Metrics;
namespace KaiAssistant.Infrastructure.Cache;

public static class RedisMetrics
{
    private static readonly Meter Meter = new("KaiAssistant.Redis", "1.0.0");
    private static readonly Counter<long> RedisFailuresTotal = Meter.CreateCounter<long>("redis_failures_total");
    private static readonly Counter<long> RedisFallbackUsageCount = Meter.CreateCounter<long>("redis_fallback_usage_count");
    private static readonly Histogram<double> RedisLatencyMs = Meter.CreateHistogram<double>("redis_latency_ms", "ms");
    private static int _connected;
    private static readonly ObservableGauge<int> RedisConnected = Meter.CreateObservableGauge(
        "redis_connected",
        () => new Measurement<int>(Volatile.Read(ref _connected)));
    public static void SetConnected(bool connected)
    {
        Interlocked.Exchange(ref _connected, connected ? 1 : 0);
    }
    public static void RecordFailure(string operation)
    {
        RedisFailuresTotal.Add(1, KeyValuePair.Create<string, object?>("operation", operation));
    }
    public static void RecordLatency(double milliseconds, string operation)
    {
        RedisLatencyMs.Record(Math.Max(0, milliseconds), KeyValuePair.Create<string, object?>("operation", operation));
    }
    public static void RecordFallback(string operation)
    {
        RedisFallbackUsageCount.Add(1, KeyValuePair.Create<string, object?>("operation", operation));
    }
}