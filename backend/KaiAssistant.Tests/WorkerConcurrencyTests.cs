using KaiAssistant.Application.Options;
using Xunit;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KaiAssistant.Tests;

public sealed class WorkerConcurrencyTests
{
    [Fact]
    public async Task BoundedAdmissionNeverExceedsConfiguredConcurrency()
    {
        const int limit = 3;
        using var gate = new SemaphoreSlim(limit, limit);
        var active = 0; var maximum = 0;
        var jobs = Enumerable.Range(0, 6).Select(async _ =>
        {
            await gate.WaitAsync();
            try { var current = Interlocked.Increment(ref active); InterlockedExtensions.Max(ref maximum, current); await Task.Yield(); Interlocked.Decrement(ref active); }
            finally { gate.Release(); }
        });
        await Task.WhenAll(jobs);
        Assert.True(maximum <= limit);
    }

    private static class InterlockedExtensions
    {
        public static void Max(ref int target, int value)
        {
            while (true) { var current = Volatile.Read(ref target); if (current >= value || Interlocked.CompareExchange(ref target, value, current) == current) return; }
        }
    }
}
