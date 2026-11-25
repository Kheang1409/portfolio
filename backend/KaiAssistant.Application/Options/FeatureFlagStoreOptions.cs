using System.ComponentModel.DataAnnotations;
namespace KaiAssistant.Application.Options;

public sealed class FeatureFlagStoreOptions
{
    public const string SectionName = "FeatureFlagStore";
    public bool Enabled { get; set; } = true;
    [Range(5, 300)]
    public int RefreshIntervalSeconds { get; set; } = 30;
}