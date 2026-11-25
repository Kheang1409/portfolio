using System.ComponentModel.DataAnnotations;
namespace KaiAssistant.Application.Options;

public sealed class AiOrchestrationOptions
{
    public const string SectionName = "AiOrchestration";
    [Required]
    public string PrimaryProvider { get; set; } = "gemini";
    [Required]
    public string SecondaryProvider { get; set; } = "openai";
    [Range(100, 120000)]
    public int ProviderTimeoutMs { get; set; } = 15000;
    [Range(128, 16384)]
    public int DefaultMaxOutputTokens { get; set; } = 1024;
}