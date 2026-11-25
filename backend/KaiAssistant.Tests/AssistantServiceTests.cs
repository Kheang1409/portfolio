using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using KaiAssistant.Application.Services;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Diagnostics;
using KaiAssistant.Application.AI;
using KaiAssistant.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
#nullable enable
namespace KaiAssistant.Tests;

public class AssistantServiceTests
{
    private readonly Mock<IResumeContextProvider> _mockResumeProvider;
    private readonly Mock<IAiPromptBuilder> _mockPromptBuilder;
    private readonly Mock<IAiModelGateway> _mockGateway;
    private readonly Mock<IModelOrchestrator> _mockOrchestrator;
    private readonly Mock<ILogger<AssistantService>> _mockLogger;
    private readonly IOptions<GeminiSettings> _geminiOptions;
    private readonly AssistantService _assistantService;
    public AssistantServiceTests()
    {
        _mockResumeProvider = new Mock<IResumeContextProvider>();
        _mockPromptBuilder = new Mock<IAiPromptBuilder>();
        _mockGateway = new Mock<IAiModelGateway>();
        _mockOrchestrator = new Mock<IModelOrchestrator>();
        _mockLogger = new Mock<ILogger<AssistantService>>();
        var geminiSettings = new GeminiSettings
        {
            ApiKey = "test-key",
            ModelNames = new List<string> { "gemini-1.5-pro" },
            Endpoint = "https://api.test/",
            SystemPrompt = "Test system prompt"
        };
        _geminiOptions = Options.Create(geminiSettings);
        _mockOrchestrator
            .Setup(x => x.BuildDecisionAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiRoutingDecision
            {
                SelectedPrimaryModel = "gemini-1.5-pro",
                CandidateModels = new[] { "gemini-1.5-pro" },
                EstimatedCostUsd = 0.001m
            });
        _mockPromptBuilder.Setup(x => x.NormalizeInput(It.IsAny<string>()))
            .Returns<string>(x => x);
        _mockPromptBuilder.Setup(x => x.NormalizeContextBlock(It.IsAny<AssistantContext?>()))
            .Returns(string.Empty);
        _mockPromptBuilder.Setup(x => x.ComputePromptHash(It.IsAny<string>(), It.IsAny<ConversationMessage[]?>(), It.IsAny<string>(), It.IsAny<AssistantContext?>()))
            .Returns("hash");
        _assistantService = new AssistantService(
            _mockResumeProvider.Object,
            _mockPromptBuilder.Object,
            _mockGateway.Object,
            _mockOrchestrator.Object,
            _geminiOptions,
            _mockLogger.Object
        );
    }
    [Fact]
    public async Task AskQuestionAsync_WithValidQuestion_ShouldReturnValidResponse()
    {
        // Arrange
        const string question = "What is your experience?";
        const string systemPrompt = "You are a helpful AI assistant.";
        var resumeChunks = new[] { new ResumeChunk { Label = "Experience", Content = "10 years in software development" } };
        _mockResumeProvider.Setup(x => x.GetRelevantChunksAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resumeChunks);
        _mockPromptBuilder.Setup(x => x.BuildSystemPrompt())
            .Returns(systemPrompt);
        _mockPromptBuilder.Setup(x => x.BuildGenerationConfig(It.IsAny<string>()))
            .Returns(new { temperature = 0.3, topK = 20, topP = 0.85, maxOutputTokens = 512, candidateCount = 1 });
        var geminiResponse = new
        {
            candidates = new object[]
            {
                new
                {
                    content = new
                    {
                        parts = new object[] { new { text = "I have extensive experience in software development." } }
                    }
                }
            }
        };
        _mockGateway.Setup(x => x.SendGenerationRequestAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((JsonSerializer.Serialize(geminiResponse), "gemini-1.5-pro"));
        // Act
        var result = await _assistantService.AskQuestionAsync(question, cancellationToken: CancellationToken.None);
        // Assert
        result.Text.Should().NotBeNullOrEmpty();
        result.Text.Should().Contain("experience");
        _mockGateway.Verify(x => x.SendGenerationRequestAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
    [Fact]
    public async Task AskQuestionAsync_WithEmptyQuestion_ShouldReturnErrorMessage()
    {
        // Act
        var result = await _assistantService.AskQuestionAsync("", cancellationToken: CancellationToken.None);
        // Assert
        result.Text.Should().Be("Please provide a question.");
        _mockGateway.Verify(x => x.SendGenerationRequestAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact]
    public async Task AskQuestionAsync_WithNoResumeChunks_ShouldIncludeNoteInSystemPrompt()
    {
        // Arrange
        const string question = "Tell me about yourself";
        const string systemPrompt = "You are a helpful AI assistant.";
        _mockResumeProvider.Setup(x => x.GetRelevantChunksAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ResumeChunk>());
        _mockResumeProvider.Setup(x => x.GetResumeChunksAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ResumeChunk>());
        _mockPromptBuilder.Setup(x => x.BuildSystemPrompt())
            .Returns(systemPrompt);
        _mockPromptBuilder.Setup(x => x.BuildGenerationConfig(It.IsAny<string>()))
            .Returns(new { temperature = 0.3, topK = 20, topP = 0.85, maxOutputTokens = 512, candidateCount = 1 });
        var geminiResponse = new
        {
            candidates = new object[]
            {
                new
                {
                    content = new
                    {
                        parts = new object[] { new { text = "I don't have resume information available." } }
                    }
                }
            }
        };
        string? capturedPayload = null;
        _mockGateway.Setup(x => x.SendGenerationRequestAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, IEnumerable<string>, CancellationToken>((payload, _, _) => capturedPayload = payload)
            .ReturnsAsync((JsonSerializer.Serialize(geminiResponse), "gemini-1.5-pro"));
        // Act
        var result = await _assistantService.AskQuestionAsync(question, cancellationToken: CancellationToken.None);
        // Assert
        result.Text.Should().NotBeNullOrEmpty();
        // Verify payload includes note about missing resume
        var payload = JsonDocument.Parse(capturedPayload!);
        var root = payload.RootElement;
        root.TryGetProperty("contents", out var contents).Should().BeTrue();
        var systemContent = contents[0].GetProperty("parts")[0].GetProperty("text").GetString();
        systemContent.Should().Contain("Note: I don't have access to the user's resume");
    }
    [Fact]
    public async Task AskQuestionAsync_WithGatewayFailure_ShouldReturnErrorMessage()
    {
        // Arrange
        const string question = "What is your experience?";
        _mockResumeProvider.Setup(x => x.GetRelevantChunksAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new ResumeChunk { Label = "Experience", Content = "10 years" } });
        _mockPromptBuilder.Setup(x => x.BuildSystemPrompt())
            .Returns("System prompt");
        _mockPromptBuilder.Setup(x => x.BuildGenerationConfig(It.IsAny<string>()))
            .Returns(new { temperature = 0.3, topK = 20, topP = 0.85, maxOutputTokens = 512, candidateCount = 1 });
        _mockGateway.Setup(x => x.SendGenerationRequestAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, null));
        // Act
        var result = await _assistantService.AskQuestionAsync(question, cancellationToken: CancellationToken.None);
        // Assert
        result.Text.Should().Be("I'm temporarily unavailable. Please try again later.");
    }
    [Fact]
    public async Task AskQuestionAsync_RequestPayload_ShouldHaveCorrectContentsOrder()
    {
        // Arrange
        const string question = "What skills do you have?";
        const string systemPrompt = "You are helpful.";
        var resumeChunks = new[] { new ResumeChunk { Label = "Skills", Content = "C#, .NET, SQL" } };
        _mockResumeProvider.Setup(x => x.GetRelevantChunksAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resumeChunks);
        _mockPromptBuilder.Setup(x => x.BuildSystemPrompt())
            .Returns(systemPrompt);
        _mockPromptBuilder.Setup(x => x.BuildGenerationConfig(It.IsAny<string>()))
            .Returns(new { temperature = 0.3, topK = 20, topP = 0.85, maxOutputTokens = 512, candidateCount = 1 });
        var geminiResponse = new { candidates = new object[] { new { content = new { parts = new object[] { new { text = "I know C#, .NET, and SQL" } } } } } };
        string? capturedPayload = null;
        _mockGateway.Setup(x => x.SendGenerationRequestAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, IEnumerable<string>, CancellationToken>((payload, _, _) => capturedPayload = payload)
            .ReturnsAsync((JsonSerializer.Serialize(geminiResponse), "gemini-1.5-pro"));
        // Act
        await _assistantService.AskQuestionAsync(question, cancellationToken: CancellationToken.None);
        // Assert
        capturedPayload.Should().NotBeNullOrEmpty();
        var payload = JsonDocument.Parse(capturedPayload!);
        var root = payload.RootElement;
        root.TryGetProperty("contents", out var contents).Should().BeTrue();
        contents.GetArrayLength().Should().BeGreaterThanOrEqualTo(2);
        // Verify first element (system prompt)
        var firstElement = contents[0];
        firstElement.TryGetProperty("role", out var role).Should().BeTrue();
        role.GetString().Should().Be("model");
        firstElement.TryGetProperty("parts", out var parts).Should().BeTrue();
        parts.GetArrayLength().Should().BeGreaterThan(0);
        parts[0].GetProperty("text").GetString().Should().StartWith("You are helpful.");
        // Verify second element (resume context)
        var secondElement = contents[1];
        secondElement.TryGetProperty("role", out var role2).Should().BeTrue();
        role2.GetString().Should().Be("model");
        secondElement.TryGetProperty("parts", out var parts2).Should().BeTrue();
        parts2[0].GetProperty("text").GetString().Should().Contain("Resume context:");
        // Verify last element is current user question
        var lastElement = contents[contents.GetArrayLength() - 1];
        lastElement.GetProperty("role").GetString().Should().Be("user");
        lastElement.GetProperty("parts")[0].GetProperty("text").GetString().Should().Be(question);
    }
    [Fact]
    public async Task AskQuestionAsync_RequestPayload_ShouldHaveCamelCaseGenerationConfig()
    {
        // Arrange
        const string question = "What projects have you worked on?";
        const string systemPrompt = "System prompt";
        var resumeChunks = new[] { new ResumeChunk { Label = "Projects", Content = "Project A, Project B" } };
        _mockResumeProvider.Setup(x => x.GetRelevantChunksAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resumeChunks);
        _mockPromptBuilder.Setup(x => x.BuildSystemPrompt())
            .Returns(systemPrompt);
        _mockPromptBuilder.Setup(x => x.BuildGenerationConfig(It.IsAny<string>()))
            .Returns(new { temperature = 0.4, topK = 25, topP = 0.9, maxOutputTokens = 768, candidateCount = 1 });
        var geminiResponse = new { candidates = new object[] { new { content = new { parts = new object[] { new { text = "I worked on Project A and Project B" } } } } } };
        string? capturedPayload = null;
        _mockGateway.Setup(x => x.SendGenerationRequestAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, IEnumerable<string>, CancellationToken>((payload, _, _) => capturedPayload = payload)
            .ReturnsAsync((JsonSerializer.Serialize(geminiResponse), "gemini-1.5-pro"));
        // Act
        await _assistantService.AskQuestionAsync(question, cancellationToken: CancellationToken.None);
        // Assert
        capturedPayload.Should().NotBeNullOrEmpty();
        var payload = JsonDocument.Parse(capturedPayload!);
        var root = payload.RootElement;
        root.TryGetProperty("generationConfig", out var genConfig).Should().BeTrue();
        // Verify camelCase fields exist
        genConfig.TryGetProperty("temperature", out var temp).Should().BeTrue();
        temp.GetDouble().Should().Be(0.4);
        genConfig.TryGetProperty("topK", out var topK).Should().BeTrue();
        topK.GetInt32().Should().Be(25);
        genConfig.TryGetProperty("topP", out var topP).Should().BeTrue();
        topP.GetDouble().Should().Be(0.9);
        genConfig.TryGetProperty("maxOutputTokens", out var maxTokens).Should().BeTrue();
        maxTokens.GetInt32().Should().Be(768);
        genConfig.TryGetProperty("candidateCount", out var candCount).Should().BeTrue();
        candCount.GetInt32().Should().Be(1);
        // Verify NO snake_case fields exist
        genConfig.TryGetProperty("top_k", out _).Should().BeFalse();
        genConfig.TryGetProperty("top_p", out _).Should().BeFalse();
        genConfig.TryGetProperty("max_output_tokens", out _).Should().BeFalse();
        genConfig.TryGetProperty("candidate_count", out _).Should().BeFalse();
    }
    [Fact]
    public async Task AskQuestionAsync_RequestPayload_ShouldNotContainLegacyFields()
    {
        // Arrange
        const string question = "Tell me more";
        const string systemPrompt = "System";
        var resumeChunks = new[] { new ResumeChunk { Label = "Summary", Content = "Summary text" } };
        _mockResumeProvider.Setup(x => x.GetRelevantChunksAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resumeChunks);
        _mockPromptBuilder.Setup(x => x.BuildSystemPrompt())
            .Returns(systemPrompt);
        _mockPromptBuilder.Setup(x => x.BuildGenerationConfig(It.IsAny<string>()))
            .Returns(new { temperature = 0.3, topK = 20, topP = 0.85, maxOutputTokens = 512, candidateCount = 1 });
        var geminiResponse = new { candidates = new object[] { new { content = new { parts = new object[] { new { text = "Response text" } } } } } };
        string? capturedPayload = null;
        _mockGateway.Setup(x => x.SendGenerationRequestAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, IEnumerable<string>, CancellationToken>((payload, _, _) => capturedPayload = payload)
            .ReturnsAsync((JsonSerializer.Serialize(geminiResponse), "gemini-1.5-pro"));
        // Act
        await _assistantService.AskQuestionAsync(question, cancellationToken: CancellationToken.None);
        // Assert
        capturedPayload.Should().NotBeNullOrEmpty();
        var payload = JsonDocument.Parse(capturedPayload!);
        var root = payload.RootElement;
        // Verify legacy fields DO NOT exist at top level
        root.TryGetProperty("systemInstruction", out _).Should().BeFalse();
        root.TryGetProperty("prompt", out _).Should().BeFalse();
        root.TryGetProperty("system", out _).Should().BeFalse();
        // Verify correct structure exists
        root.TryGetProperty("contents", out _).Should().BeTrue();
        root.TryGetProperty("generationConfig", out _).Should().BeTrue();
        root.TryGetProperty("safetySettings", out _).Should().BeTrue();
    }
    [Fact]
    public async Task AskQuestionAsync_WithMalformedGeminiResponse_ShouldHandleGracefully()
    {
        // Arrange
        const string question = "What's next?";
        _mockResumeProvider.Setup(x => x.GetRelevantChunksAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new ResumeChunk { Label = "Info", Content = "Some info" } });
        _mockPromptBuilder.Setup(x => x.BuildSystemPrompt())
            .Returns("System");
        _mockPromptBuilder.Setup(x => x.BuildGenerationConfig(It.IsAny<string>()))
            .Returns(new { temperature = 0.3, topK = 20, topP = 0.85, maxOutputTokens = 512, candidateCount = 1 });
        _mockGateway.Setup(x => x.SendGenerationRequestAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((JsonSerializer.Serialize(new { error = "Something went wrong" }), "gemini-1.5-pro"));
        // Act
        var result = await _assistantService.AskQuestionAsync(question, cancellationToken: CancellationToken.None);
        // Assert
        result.Text.Should().Be("I couldn't generate a suitable response right now.");
    }
    [Fact]
    public async Task AskQuestionAsync_WithPersonalDetailsAllowed_ShouldIncludePersonalChunk()
    {
        // Arrange
        const string question = "Who are you?";
        _geminiOptions.Value.IncludePersonalDetails = true;
        var resumeChunks = new[]
        {
            new ResumeChunk { Label = "Personal Details", Content = "Legal name: Hang Kheang Taing\nPhone: 123-456" },
            new ResumeChunk { Label = "Experience", Content = "Built APIs" }
        };
        _mockResumeProvider.Setup(x => x.GetRelevantChunksAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resumeChunks);
        _mockPromptBuilder.Setup(x => x.BuildSystemPrompt())
            .Returns("System");
        _mockPromptBuilder.Setup(x => x.BuildGenerationConfig(It.IsAny<string>()))
            .Returns(new { temperature = 0.3, topK = 20, topP = 0.85, maxOutputTokens = 512, candidateCount = 1 });
        var geminiResponse = new
        {
            candidates = new object[]
            {
                new { content = new { parts = new object[] { new { text = "answer" } } } }
            }
        };
        string? capturedPayload = null;
        _mockGateway.Setup(x => x.SendGenerationRequestAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, IEnumerable<string>, CancellationToken>((payload, _, _) => capturedPayload = payload)
            .ReturnsAsync((JsonSerializer.Serialize(geminiResponse), "gemini-1.5-pro"));
        // Act
        await _assistantService.AskQuestionAsync(question, cancellationToken: CancellationToken.None);
        // Assert
        capturedPayload.Should().NotBeNullOrEmpty();
        var payload = JsonDocument.Parse(capturedPayload!);
        var text = payload.RootElement.GetProperty("contents")[1].GetProperty("parts")[0].GetProperty("text").GetString();
        text.Should().Contain("Legal name");
        text.Should().Contain("Phone");
        text.Should().Contain("Experience");
    }
    [Fact]
    public async Task AskQuestionAsync_WithPersonalDetailsDisabled_ShouldFilterPersonalChunk()
    {
        // Arrange
        const string question = "Who are you?";
        _geminiOptions.Value.IncludePersonalDetails = false;
        var resumeChunks = new[]
        {
            new ResumeChunk { Label = "Personal Details", Content = "Legal name: Hang Kheang Taing\nPhone: 123-456" },
            new ResumeChunk { Label = "Skills", Content = "C#, .NET" }
        };
        _mockResumeProvider.Setup(x => x.GetRelevantChunksAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resumeChunks);
        _mockPromptBuilder.Setup(x => x.BuildSystemPrompt())
            .Returns("System");
        _mockPromptBuilder.Setup(x => x.BuildGenerationConfig(It.IsAny<string>()))
            .Returns(new { temperature = 0.3, topK = 20, topP = 0.85, maxOutputTokens = 512, candidateCount = 1 });
        var geminiResponse = new
        {
            candidates = new object[]
            {
                new { content = new { parts = new object[] { new { text = "answer" } } } }
            }
        };
        string? capturedPayload = null;
        _mockGateway.Setup(x => x.SendGenerationRequestAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, IEnumerable<string>, CancellationToken>((payload, _, _) => capturedPayload = payload)
            .ReturnsAsync((JsonSerializer.Serialize(geminiResponse), "gemini-1.5-pro"));
        // Act
        await _assistantService.AskQuestionAsync(question, cancellationToken: CancellationToken.None);
        // Assert
        capturedPayload.Should().NotBeNullOrEmpty();
        var payload = JsonDocument.Parse(capturedPayload!);
        var text = payload.RootElement.GetProperty("contents")[1].GetProperty("parts")[0].GetProperty("text").GetString();
        text.Should().NotContain("Legal name");
        text.Should().NotContain("Phone");
        text.Should().Contain("Skills");
    }

    [Fact]
    public async Task AskQuestionAsync_IncludesBothResumeAndRetrievedKnowledgeContext()
    {
        _mockResumeProvider.Setup(x => x.GetRelevantChunksAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ResumeChunk { Label = "Experience", Content = "Resume experience evidence" }]);
        _mockPromptBuilder.Setup(x => x.BuildSystemPrompt()).Returns("System");
        _mockPromptBuilder.Setup(x => x.BuildGenerationConfig(It.IsAny<string>()))
            .Returns(new { temperature = 0.3, topK = 20, topP = 0.85, maxOutputTokens = 512, candidateCount = 1 });
        var rag = new Mock<IRagService>();
        rag.Setup(x => x.BuildAugmentedPromptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagContextResult
            {
                Snippets = [new RagSnippet { Source = "projects.md", Content = "Retrieved project evidence" }]
            });
        var geminiResponse = new
        {
            candidates = new object[]
            {
                new { content = new { parts = new object[] { new { text = "answer" } } } }
            }
        };
        string? capturedPayload = null;
        _mockGateway.Setup(x => x.SendGenerationRequestAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, IEnumerable<string>, CancellationToken>((payload, _, _) => capturedPayload = payload)
            .ReturnsAsync((JsonSerializer.Serialize(geminiResponse), "gemini-1.5-pro"));
        var assistantService = new AssistantService(
            _mockResumeProvider.Object,
            _mockPromptBuilder.Object,
            _mockGateway.Object,
            _mockOrchestrator.Object,
            _geminiOptions,
            _mockLogger.Object,
            rag: rag.Object);

        await assistantService.AskQuestionAsync("Question", cancellationToken: CancellationToken.None);

        var payload = JsonDocument.Parse(capturedPayload!);
        var contents = payload.RootElement.GetProperty("contents");
        var contextText = contents[1].GetProperty("parts")[0].GetProperty("text").GetString();
        contextText.Should().Contain("Resume experience evidence");
        contextText.Should().Contain("Retrieved project evidence");
    }
}