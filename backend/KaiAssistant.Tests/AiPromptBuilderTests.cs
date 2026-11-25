using System.Text.Json;
using FluentAssertions;
using KaiAssistant.Application.Services;
using KaiAssistant.Domain.Entities;
using Microsoft.Extensions.Options;
using Xunit;
namespace KaiAssistant.Tests
{
    public class AiPromptBuilderTests
    {
        [Fact]
        public void BuildSystemPrompt_UsesOptionsAndIncludesGuardrails()
        {
            var settings = new GeminiSettings { SystemPrompt = "Custom system prompt." };
            var options = Options.Create(settings);
            var builder = new AiPromptBuilder(options);
            var prompt = builder.BuildSystemPrompt();
            prompt.Should().Contain("Custom system prompt.");
            prompt.Should().Contain("STYLE RULES");
        }
        [Fact]
        public void BuildGenerationConfig_ReturnsConfig_WithDifferentSizes()
        {
            var settings = new GeminiSettings();
            var options = Options.Create(settings);
            var builder = new AiPromptBuilder(options);
            var shortConfig = builder.BuildGenerationConfig("short question");
            var longConfig = builder.BuildGenerationConfig(new string('x', 200));
            var sJson = JsonSerializer.Serialize(shortConfig);
            var lJson = JsonSerializer.Serialize(longConfig);
            sJson.Should().Contain("maxOutputTokens");
            lJson.Should().Contain("maxOutputTokens");
            sJson.Should().NotBe(lJson);
        }
    }
}