using System.Text.RegularExpressions;
using KaiAssistant.Application.Interfaces;
namespace KaiAssistant.Infrastructure.AI;

public sealed class PromptSecurityService : IPromptSecurityService
{
    private static readonly Regex ControlCharsRegex = new("[\\u0000-\\u0008\\u000B\\u000C\\u000E-\\u001F]", RegexOptions.Compiled);
    private static readonly Regex ScriptTagRegex = new("<\\s*script[^>]*>.*?<\\s*/\\s*script\\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex ScriptTagOpenRegex = new("<\\s*script[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ScriptTagCloseRegex = new("<\\s*/\\s*script\\s*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly string[] InjectionIndicators =
    [
        "ignore previous instructions",
        "disregard all prior",
        "reveal system prompt",
        "bypass safety",
        "developer mode"
    ];
    private string _streamingBuffer = string.Empty;
    private bool _inScriptTag = false;
    public string SanitizeInput(string input)
    {
        var sanitized = ControlCharsRegex.Replace(input ?? string.Empty, string.Empty).Trim();
        return sanitized.Length > 4000 ? sanitized[..4000] : sanitized;
    }
    public string SanitizeOutput(string output)
    {
        var sanitized = ScriptTagRegex.Replace(output ?? string.Empty, string.Empty).Trim();
        return sanitized;
    }
    public bool LooksLikePromptInjection(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }
        return InjectionIndicators.Any(x => input.Contains(x, StringComparison.OrdinalIgnoreCase));
    }
    public string SanitizeOutputStreaming(string chunk)
    {
        if (string.IsNullOrEmpty(chunk))
        {
            var remaining = _streamingBuffer;
            ResetStreamingState();
            return SanitizeOutput(remaining);
        }
        _streamingBuffer += chunk;
        var sanitized = ScriptTagRegex.Replace(_streamingBuffer, string.Empty);
        var lastSafeIndex = FindLastSafeEmissionPoint(sanitized);
        if (lastSafeIndex >= 0)
        {
            var safeToEmit = sanitized[..(lastSafeIndex + 1)];
            _streamingBuffer = sanitized[(lastSafeIndex + 1)..];
            return safeToEmit;
        }
        return string.Empty;
    }
    private int FindLastSafeEmissionPoint(string text)
    {
        int lastClose = -1;
        int searchStart = 0;
        while (true)
        {
            var openMatch = ScriptTagOpenRegex.Match(text, searchStart);
            if (!openMatch.Success)
            {
                break;
            }
            var closeMatch = ScriptTagCloseRegex.Match(text, openMatch.Index);
            if (!closeMatch.Success)
            {
                _inScriptTag = true;
                return openMatch.Index - 1;
            }
            _inScriptTag = false;
            lastClose = closeMatch.Index + closeMatch.Length - 1;
            searchStart = closeMatch.Index + closeMatch.Length;
        }
        if (_inScriptTag)
        {
            return -1;
        }
        return lastClose;
    }
    private void ResetStreamingState()
    {
        _streamingBuffer = string.Empty;
        _inScriptTag = false;
    }
}