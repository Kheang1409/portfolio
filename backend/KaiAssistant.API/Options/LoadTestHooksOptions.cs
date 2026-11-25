namespace KaiAssistant.API.Options;

public sealed class LoadTestHooksOptions
{
    public const string SectionName = "LoadTestHooks";
    public bool Enabled { get; set; } = false;
}