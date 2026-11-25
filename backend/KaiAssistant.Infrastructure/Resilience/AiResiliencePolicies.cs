namespace KaiAssistant.Infrastructure.Resilience;

using Polly;
using System;
using System.Net.Http;
using System.Threading.Tasks;
public static class AiResiliencePolicies
{
    public static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy()
    {
        return Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(retryCount: 3, sleepDurationProvider: _ => TimeSpan.FromSeconds(1));
    }
    public static IAsyncPolicy<HttpResponseMessage> CreateCircuitBreakerPolicy(
        int failureThreshold = 3,
        TimeSpan? timeout = null)
    {
        return Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .CircuitBreakerAsync(failureThreshold, timeout ?? TimeSpan.FromSeconds(30));
    }
    public static IAsyncPolicy<HttpResponseMessage> CreateTimeoutPolicy(TimeSpan duration)
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(duration);
    }
    public static IAsyncPolicy<HttpResponseMessage> CreateBulkheadPolicy(
        int maxParallelRequests = 10,
        int maxQueueSize = 50)
    {
        return Policy.BulkheadAsync<HttpResponseMessage>(maxParallelRequests, maxQueueSize);
    }
    public static IAsyncPolicy<HttpResponseMessage> CreateFallbackPolicy()
    {
        return Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .FallbackAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
            });
    }
    public static IAsyncPolicy<HttpResponseMessage> CreateComprehensivePolicy()
    {
        var timeout = CreateTimeoutPolicy(TimeSpan.FromSeconds(15));
        var circuitBreaker = CreateCircuitBreakerPolicy(3, TimeSpan.FromSeconds(30));
        var retry = CreateRetryPolicy();
        return Policy.WrapAsync(retry, circuitBreaker, timeout);
    }
}