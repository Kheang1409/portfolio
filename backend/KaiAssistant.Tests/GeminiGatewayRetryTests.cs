using FluentAssertions;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Options;
using KaiAssistant.Domain.Entities;
using KaiAssistant.Infrastructure.Gateways;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
namespace KaiAssistant.Tests;

public class GeminiGatewayRetryTests
{
    [Fact]
    public async Task SendGenerationRequestAsync_RetriesOn503AndSucceeds()
    {
        var handler = new QueueMessageHandler(
        [
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("busy") },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"ok\":true}") }
        ]);
        var client = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(x => x.CreateClient("Gemini")).Returns(client);
        var modelHealth = new Mock<IModelHealthService>();
        modelHealth.Setup(x => x.CanAttempt(It.IsAny<string>(), It.IsAny<DateTimeOffset>())).Returns(true);
        var orchestrationOptions = new Mock<IOptionsMonitor<AiModelOrchestrationOptions>>();
        orchestrationOptions.SetupGet(x => x.CurrentValue).Returns(new AiModelOrchestrationOptions { EnableFailureSimulation = false });
        var environment = new Mock<IHostEnvironment>();
        environment.Setup(x => x.EnvironmentName).Returns("Development");
        var gateway = new GeminiGateway(
            factory.Object,
            Options.Create(new GeminiSettings { ApiKey = "k", Endpoint = "https://example/", ModelNames = ["m1"] }),
            orchestrationOptions.Object,
            modelHealth.Object,
            new Mock<IResilienceStatusProvider>().Object,
            environment.Object,
            NullLogger<GeminiGateway>.Instance);
        var result = await gateway.SendGenerationRequestAsync("{}", ["m1"], default);
        result.Body.Should().NotBeNull();
        result.UsedModel.Should().Be("m1");
        handler.RequestCount.Should().BeGreaterThanOrEqualTo(2);
    }
    [Fact]
    public async Task SendGenerationRequestAsync_DoesNotRetryOn404()
    {
        var handler = new QueueMessageHandler(
        [
            new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("not found") }
        ]);
        var client = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(x => x.CreateClient("Gemini")).Returns(client);
        var modelHealth = new Mock<IModelHealthService>();
        modelHealth.Setup(x => x.CanAttempt(It.IsAny<string>(), It.IsAny<DateTimeOffset>())).Returns(true);
        var orchestrationOptions = new Mock<IOptionsMonitor<AiModelOrchestrationOptions>>();
        orchestrationOptions.SetupGet(x => x.CurrentValue).Returns(new AiModelOrchestrationOptions { EnableFailureSimulation = false });
        var environment = new Mock<IHostEnvironment>();
        environment.Setup(x => x.EnvironmentName).Returns("Development");
        var gateway = new GeminiGateway(
            factory.Object,
            Options.Create(new GeminiSettings { ApiKey = "k", Endpoint = "https://example/", ModelNames = ["m1"] }),
            orchestrationOptions.Object,
            modelHealth.Object,
            new Mock<IResilienceStatusProvider>().Object,
            environment.Object,
            NullLogger<GeminiGateway>.Instance);
        var result = await gateway.SendGenerationRequestAsync("{}", ["m1"], default);
        result.Body.Should().BeNull();
        result.UsedModel.Should().BeNull();
        handler.RequestCount.Should().Be(1);
    }
    private sealed class QueueMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;
        public QueueMessageHandler(IEnumerable<HttpResponseMessage> responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }
        public int RequestCount { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            if (_responses.Count == 0)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"ok\":true}")
                });
            }
            return Task.FromResult(_responses.Dequeue());
        }
    }
}