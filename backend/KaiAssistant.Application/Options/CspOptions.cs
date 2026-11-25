using System.ComponentModel.DataAnnotations;
namespace KaiAssistant.Application.Options;

public sealed class CspOptions
{
    public const string SectionName = "Csp";

    /// <summary>
    /// When true, allows 'unsafe-eval' in script-src (not recommended for production)
    /// </summary>
    public bool AllowUnsafeEval { get; set; } = false;

    /// <summary>
    /// Additional script sources to allow (e.g., for analytics)
    /// </summary>
    public string[] AdditionalScriptSources { get; set; } = [];

    /// <summary>
    /// Additional style sources to allow
    /// </summary>
    public string[] AdditionalStyleSources { get; set; } = [];

    /// <summary>
    /// Additional connect sources to allow
    /// </summary>
    public string[] AdditionalConnectSources { get; set; } = [];

    /// <summary>
    /// Additional frame sources to allow
    /// </summary>
    public string[] AdditionalFrameSources { get; set; } = [];
}