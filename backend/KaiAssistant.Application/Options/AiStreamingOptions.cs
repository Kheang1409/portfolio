using System.ComponentModel.DataAnnotations;
namespace KaiAssistant.Application.Options;

public sealed class AiStreamingOptions
{
    public const string SectionName = "AiStreaming";
    public bool Enabled { get; set; } = true;
    [Range(5, 600)]
    public int MaxStreamDurationSeconds { get; set; } = 45;
    [Range(32, 16384)]
    public int MaxTokensPerStream { get; set; } = 1600;
}