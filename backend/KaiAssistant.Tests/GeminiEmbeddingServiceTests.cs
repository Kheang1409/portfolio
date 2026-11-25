using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Options;
using KaiAssistant.Infrastructure.AI.Embeddings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace KaiAssistant.Tests;

public sealed class GeminiEmbeddingServiceTests
{
    [Fact]
    public async Task Batch_maps_document_mode_title_dimensions_and_normalizes()
    {
        string? body = null;
        var handler = new DelegateHandler(async (request, _) =>
        {
            body = await request.Content!.ReadAsStringAsync();
            return Json(HttpStatusCode.OK, "{\"embeddings\":[{\"values\":[3,4]},{\"values\":[0,2]}]}");
        });
        using var service = Create(handler, new EmbeddingOptions { Dimensions = 2, BatchSize = 10 });
        var result = await service.GenerateEmbeddingsAsync(["first", "second"], EmbeddingPurpose.RetrievalDocument, ["One", "Two"]);
        result.Should().HaveCount(2);
        result[0][0].Should().BeApproximately(.6f, .0001f);
        body.Should().Contain("RETRIEVAL_DOCUMENT").And.Contain("One").And.Contain("outputDimensionality");
    }

    [Fact]
    public async Task Query_mode_and_cancellation_are_propagated()
    {
        var handler = new DelegateHandler(async (_, token) => { await Task.Delay(TimeSpan.FromMinutes(1), token); return Json(HttpStatusCode.OK, "{}"); });
        using var service = Create(handler, new EmbeddingOptions { Dimensions = 2, RequestTimeoutSeconds = 20 });
        using var cancellation = new CancellationTokenSource(20);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GenerateEmbeddingAsync("query", EmbeddingPurpose.RetrievalQuery, cancellationToken: cancellation.Token));
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    public async Task Provider_failure_is_classified(HttpStatusCode status, bool retryable)
    {
        using var service = Create(new DelegateHandler((_, _) => Task.FromResult(Json(status, "{}"))), new EmbeddingOptions { Dimensions = 2, MaxRetries = 0 });
        var error = await Assert.ThrowsAsync<EmbeddingProviderException>(() => service.GenerateEmbeddingAsync("query"));
        error.Retryable.Should().Be(retryable);
    }

    [Fact]
    public async Task Rate_limit_is_retried_then_succeeds()
    {
        var calls = 0;
        using var service = Create(new DelegateHandler((_, _) => Task.FromResult(++calls == 1
            ? Json(HttpStatusCode.TooManyRequests, "{}") : Json(HttpStatusCode.OK, "{\"embeddings\":[{\"values\":[1,0]}]}"))),
            new EmbeddingOptions { Dimensions = 2, MaxRetries = 1 });
        (await service.GenerateEmbeddingAsync("query")).Should().HaveCount(2);
        calls.Should().Be(2);
    }

    [Fact]
    public async Task Configured_timeout_is_reported_as_retryable()
    {
        using var service = Create(new DelegateHandler(async (_, token) => { await Task.Delay(TimeSpan.FromMinutes(1), token); return Json(HttpStatusCode.OK, "{}"); }),
            new EmbeddingOptions { Dimensions = 2, RequestTimeoutSeconds = 1, MaxRetries = 0 });
        var error = await Assert.ThrowsAsync<EmbeddingProviderException>(() => service.GenerateEmbeddingAsync("query"));
        error.Retryable.Should().BeTrue();
    }

    [Fact]
    public void Cache_key_changes_with_model_version_and_hides_content()
    {
        var one = new EmbeddingOptions { Version = "v1" }; var two = new EmbeddingOptions { Version = "v2" };
        var first = GeminiEmbeddingService.CreateCacheKey(one, EmbeddingPurpose.RetrievalQuery, "private question");
        GeminiEmbeddingService.CreateCacheKey(two, EmbeddingPurpose.RetrievalQuery, "private question").Should().NotBe(first);
        first.Should().NotContain("private question");
    }

    [Fact]
    public void Production_configuration_has_real_provider_defaults()
    {
        var options = new EmbeddingOptions();
        options.Provider.Should().Be("gemini"); options.Model.Should().Be("gemini-embedding-001"); options.Dimensions.Should().Be(768);
    }

    private static GeminiEmbeddingService Create(HttpMessageHandler handler, EmbeddingOptions options)
    {
        var monitor = new Mock<IOptionsMonitor<EmbeddingOptions>>(); monitor.SetupGet(x => x.CurrentValue).Returns(options);
        var factory = new Mock<IHttpClientFactory>(); factory.Setup(x => x.CreateClient("GeminiEmbeddings")).Returns(new HttpClient(handler) { BaseAddress = new Uri("https://example.test/v1beta/") });
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["GeminiSettings:ApiKey"] = "test-key" }).Build();
        return new(factory.Object, monitor.Object, new MemoryCache(new MemoryCacheOptions()), configuration);
    }
    private static HttpResponseMessage Json(HttpStatusCode status, string json) => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    private sealed class DelegateHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> callback) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => callback(request, cancellationToken); }
}
