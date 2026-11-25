using FluentAssertions;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;
using KaiAssistant.Application.DTOs;
using KaiAssistant.Application.Diagnostics;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Services;
using KaiAssistant.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KaiAssistant.Tests;

public sealed class AssistantOrchestratorCacheTests
{
    [Fact]
    public async Task Orchestrator_does_not_use_context_blind_outer_response_cache()
    {

        var contextBuilder = new Mock<IAssistantContextBuilder>();
        contextBuilder.Setup(x => x.BuildContextAsync(
                "conversation",
                "question",
                "user-42",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConversationContext
            {
                ContextMessages =
                [
                    new ConversationMessage { Role = "user", Content = "previous fact" }
                ]
            });
        var assistant = new Mock<IAssistantService>();
        assistant.Setup(x => x.StreamAsync(
                "question",
                It.IsAny<ConversationMessage[]>(),
                It.IsAny<AssistantContext>(),
                It.IsAny<CancellationToken>()))
            .Returns(EmptyStream());

        var promptSecurity = new Mock<IPromptSecurityService>();
        promptSecurity.Setup(x => x.SanitizeInput(It.IsAny<string>()))
            .Returns<string>(s => s);
        promptSecurity.Setup(x => x.SanitizeOutput(It.IsAny<string>()))
            .Returns<string>(s => s ?? string.Empty);
        promptSecurity.Setup(x => x.LooksLikePromptInjection(It.IsAny<string>()))
            .Returns(false);

        var orchestrator = new AssistantOrchestrator(
            assistant.Object,
            contextBuilder.Object,
            Mock.Of<IConversationRepository>(),
            promptSecurity.Object,
            Mock.Of<ILogger<AssistantOrchestrator>>());

        await foreach (var _ in orchestrator.OrchestrateStreamAsync("question", "conversation", "user-42"))
        {
        }

        assistant.Verify(x => x.StreamAsync(
            "question",
            It.IsAny<ConversationMessage[]>(),
            It.IsAny<AssistantContext>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static async IAsyncEnumerable<AiStreamChunk> EmptyStream()
    {
        await Task.Yield();
        yield break;
    }
}
