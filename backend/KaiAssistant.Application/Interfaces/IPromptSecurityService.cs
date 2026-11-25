namespace KaiAssistant.Application.Interfaces;

public interface IPromptSecurityService
{
    string SanitizeInput(string input);
    string SanitizeOutput(string output);
    bool LooksLikePromptInjection(string input);
    /// <summary>
    /// Sanitizes a streaming output chunk with stateful tracking of partial tags across chunk boundaries.
    /// Call for each chunk in sequence; returns sanitized text safe to emit immediately.
    /// Call with null/empty to reset state and get any buffered safe tail.
    /// </summary>
    string SanitizeOutputStreaming(string chunk);
}