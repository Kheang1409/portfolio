using FluentAssertions;
using KaiAssistant.API.Controllers;
using KaiAssistant.Application.Diagnostics;
using KaiAssistant.Application.DTOs;
using KaiAssistant.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
namespace KaiAssistant.Tests;
#nullable enable
public class AssistantStreamingControllerTests
{
    [Fact]
    public async Task Stream_WhenEnabled_WritesNdjsonChunks()
    {
        var orchestrator = new Mock<IAssistantOrchestrator>();
        var conversationRepo = new Mock<IConversationRepository>();
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<AssistantController>>();
        orchestrator
            .Setup(x => x.OrchestrateStreamAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateStreamChunks());
        conversationRepo
            .Setup(x => x.GetByUserIdAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KaiAssistant.Domain.Entities.Conversation>());
        conversationRepo
            .Setup(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KaiAssistant.Domain.Entities.Conversation { UserId = "test-user" });
        var controller = new AssistantController(orchestrator.Object, logger.Object, conversationRepo.Object);
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        await controller.Stream(new AssistantRequestDto("hello"), CancellationToken.None);
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        httpContext.Response.ContentType.Should().Be("application/x-ndjson");
        httpContext.Response.Body.Position = 0;
        var body = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();
        body.Should().Contain("\"type\":\"delta\"");
        body.Should().Contain("\"type\":\"completed\"");
    }
    [Fact]
    public async Task Stream_WhenCancelled_DoesNotThrow()
    {
        var orchestrator = new Mock<IAssistantOrchestrator>();
        var conversationRepo = new Mock<IConversationRepository>();
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<AssistantController>>();
        orchestrator
            .Setup(x => x.OrchestrateStreamAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(CancelledStream());
        conversationRepo
            .Setup(x => x.GetByUserIdAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KaiAssistant.Domain.Entities.Conversation>());
        conversationRepo
            .Setup(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KaiAssistant.Domain.Entities.Conversation { UserId = "test-user" });
        var controller = new AssistantController(orchestrator.Object, logger.Object, conversationRepo.Object);
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var act = async () => await controller.Stream(new AssistantRequestDto("cancel"), cts.Token);
        await act.Should().NotThrowAsync();
    }
    private static async IAsyncEnumerable<AssistantStreamEvent> CreateStreamChunks()
    {
        yield return new AssistantStreamEvent
        {
            Type = StreamEventType.Token,
            Content = "Hello"
        };
        await Task.Yield();
        yield return new AssistantStreamEvent
        {
            Type = StreamEventType.End
        };
    }
    private static async IAsyncEnumerable<AssistantStreamEvent> CancelledStream()
    {
        await Task.Delay(1);
        throw new OperationCanceledException();
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }
}
