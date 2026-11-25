using System.ComponentModel.DataAnnotations;
namespace KaiAssistant.API.Options;

public sealed class OpsOptions
{
    public const string SectionName = "Ops";
    public bool EnabledInProduction { get; set; } = false;
    [Range(50, 60_000)]
    public int SlowRequestThresholdMs { get; set; } = 800;
    [Range(0.01d, 1.0d)]
    public double HighVolumeLogSampleRate { get; set; } = 0.2;
    public string[] HighVolumePathPrefixes { get; set; } = ["/api/assistants/ask"];
}