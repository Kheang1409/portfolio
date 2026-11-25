using System.Diagnostics.Metrics;
using System.Text.RegularExpressions;
using KaiAssistant.Application.Diagnostics;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Options;
using KaiAssistant.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace KaiAssistant.Infrastructure.Governance;

public sealed class AiUsageGuard : IAiUsageGuard
{
    private static readonly Meter Meter = new("KaiAssistant.AiGovernance", "1.0.0");
    private static readonly Counter<long> AiTokensUsed = Meter.CreateCounter<long>("ai_tokens_used_total");
    private static readonly Counter<long> AiRequestsBlocked = Meter.CreateCounter<long>("ai_requests_blocked_total");
    private readonly IMemoryCache _memoryCache;
    private readonly IClientContextAccessor _clientContextAccessor;
    private readonly IOptionsMonitor<AiGovernanceOptions> _options;
    private readonly IOperationalSimulationState? _simulation;
    private readonly ILogger<AiUsageGuard> _logger;
    public AiUsageGuard(
        IMemoryCache memoryCache,
        IClientContextAccessor clientContextAccessor,
        IOptionsMonitor<AiGovernanceOptions> options,
        ILogger<AiUsageGuard> logger,
        IOperationalSimulationState? simulation = null)
    {
        _memoryCache = memoryCache;
        _clientContextAccessor = clientContextAccessor;
        _options = options;
        _logger = logger;
        _simulation = simulation;
    }
    public async Task<AiUsageDecision> EvaluateAsync(
        string question,
        ConversationMessage[]? history,
        AssistantContext? context,
        CancellationToken cancellationToken = default)
    {
        var opts = _options.CurrentValue;
        var throttleUntil = _simulation is null
            ? null
            : await _simulation.GetForceAiThrottleUntilUtcAsync(cancellationToken).ConfigureAwait(false);
        if (throttleUntil is DateTimeOffset simulatedUntil && simulatedUntil > DateTimeOffset.UtcNow)
        {
            return Block("simulated_ai_throttle", "AI service is temporarily throttled for load testing. Please retry later.");
        }
        if (!opts.Enabled)
        {
            return new AiUsageDecision
            {
                Allowed = true,
                Question = Sanitize(question),
                History = SanitizeHistory(history),
                Context = context,
                EstimatedInputTokens = EstimateTokens(question, history, opts),
                EstimatedCostUsd = EstimateCostUsd(EstimateTokens(question, history, opts))
            };
        }
        var clientIp = _clientContextAccessor.GetClientIp();
        if (!IsRequestRateAllowed(clientIp, opts.MaxRequestsPerMinutePerIp))
        {
            return Block("rate_limit", "Too many AI requests from your IP. Please wait a minute and retry.");
        }
        var sanitizedQuestion = Sanitize(question);
        var sanitizedHistory = SanitizeHistory(history, opts.MaxHistoryMessages, opts.MaxHistoryMessageChars);
        var totalChars = sanitizedQuestion.Length + (sanitizedHistory?.Sum(x => x.Content.Length + x.Role.Length) ?? 0);
        if (totalChars > opts.MaxInputChars)
        {
            if (!opts.TruncateOversizedInput)
            {
                return Block("input_too_large", "Your input is too large. Please shorten your message and try again.");
            }
            sanitizedQuestion = sanitizedQuestion[..Math.Min(sanitizedQuestion.Length, opts.MaxInputChars)];
        }
        var estimatedTokens = EstimateTokens(sanitizedQuestion, sanitizedHistory, opts);
        if (estimatedTokens > opts.MaxTokensPerRequest)
        {
            if (!opts.TruncateOversizedInput)
            {
                return Block("tokens_exceeded", "Your request is too large for AI processing. Please shorten it and retry.");
            }
            var maxCharsFromTokens = opts.MaxTokensPerRequest * Math.Max(1, opts.EstimatedCharsPerToken);
            sanitizedQuestion = sanitizedQuestion[..Math.Min(sanitizedQuestion.Length, maxCharsFromTokens)];
            sanitizedHistory = null;
            estimatedTokens = EstimateTokens(sanitizedQuestion, sanitizedHistory, opts);
        }
        var wasTruncated = !string.Equals(sanitizedQuestion, question, StringComparison.Ordinal);
        var estimatedCost = EstimateCostUsd(estimatedTokens);
        return new AiUsageDecision
        {
            Allowed = true,
            Question = sanitizedQuestion,
            History = sanitizedHistory,
            Context = context,
            EstimatedInputTokens = estimatedTokens,
            EstimatedCostUsd = estimatedCost,
            Truncated = wasTruncated
        };
    }
    public void RecordTokensUsed(int inputTokens, int outputTokens)
    {
        var total = Math.Max(0, inputTokens) + Math.Max(0, outputTokens);
        if (total > 0)
        {
            AiTokensUsed.Add(total);
        }
    }
    public bool TryChargeBudget(decimal requestCostUsd, AssistantContext? context, out string? rejectionMessage)
    {
        rejectionMessage = null;
        var opts = _options.CurrentValue;
        var cost = Math.Max(0m, requestCostUsd);
        if (opts.MaxCostPerRequestUsd > 0 && cost > opts.MaxCostPerRequestUsd)
        {
            rejectionMessage = $"Request exceeds maximum AI cost budget (${opts.MaxCostPerRequestUsd:0.####}).";
            return false;
        }
        var clientIp = _clientContextAccessor.GetClientIp();
        var minuteKey = $"ai-cost:ip:{clientIp}:{DateTimeOffset.UtcNow:yyyyMMddHHmm}";
        var minuteTotal = (_memoryCache.Get<decimal?>(minuteKey) ?? 0m) + cost;
        _memoryCache.Set(minuteKey, minuteTotal, TimeSpan.FromMinutes(2));
        if (opts.SoftBudgetPerMinuteUsdPerIp > 0 && minuteTotal > opts.SoftBudgetPerMinuteUsdPerIp)
        {
            rejectionMessage = "AI usage budget for your IP is temporarily exceeded. Please retry later.";
            return false;
        }
        var sessionId = ExtractSessionId(context);
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            var sessionKey = $"ai-cost:session:{sessionId}:{DateTimeOffset.UtcNow:yyyyMMddHH}";
            var sessionTotal = (_memoryCache.Get<decimal?>(sessionKey) ?? 0m) + cost;
            _memoryCache.Set(sessionKey, sessionTotal, TimeSpan.FromHours(2));
            if (opts.SoftBudgetPerHourUsdPerSession > 0 && sessionTotal > opts.SoftBudgetPerHourUsdPerSession)
            {
                rejectionMessage = "AI session budget exceeded. Please start a new session or try later.";
                return false;
            }
        }
        return true;
    }
    public AiResponseDecision EvaluateResponse(string response)
    {
        var opts = _options.CurrentValue;
        if (string.IsNullOrWhiteSpace(response))
        {
            return new AiResponseDecision
            {
                Allowed = false,
                BlockReason = "empty_response",
                UserFacingMessage = "I couldn't generate a response right now. Please try again."
            };
        }
        var normalized = Regex.Replace(response, "\\s+", " ").Trim();
        if (normalized.Length < Math.Max(1, opts.MinimumResponseChars))
        {
            return new AiResponseDecision
            {
                Allowed = false,
                BlockReason = "response_too_short",
                UserFacingMessage = "The AI response was too short to be useful. Please retry your question."
            };
        }
        if (opts.EnableUnsafeOutputFiltering && opts.UnsafeTerms.Length > 0)
        {
            foreach (var term in opts.UnsafeTerms.Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                if (normalized.Contains(term, StringComparison.OrdinalIgnoreCase))
                {
                    return new AiResponseDecision
                    {
                        Allowed = false,
                        BlockReason = "unsafe_term_detected",
                        UserFacingMessage = "I can't provide that response safely right now. Please rephrase your request."
                    };
                }
            }
        }
        return new AiResponseDecision
        {
            Allowed = true,
            NormalizedResponse = normalized
        };
    }
    private bool IsRequestRateAllowed(string ip, int maxPerMinute)
    {
        var limit = Math.Max(1, maxPerMinute);
        var minuteWindow = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmm");
        var key = $"ai-rate:{ip}:{minuteWindow}";
        var count = _memoryCache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            return 0;
        });
        var next = (int)count + 1;
        _memoryCache.Set(key, next, TimeSpan.FromMinutes(1));
        return next <= limit;
    }
    private AiUsageDecision Block(string reason, string userMessage)
    {
        AiRequestsBlocked.Add(1, KeyValuePair.Create<string, object?>("reason", reason));
        _logger.LogWarning("AI request blocked: reason={Reason}", reason);
        return new AiUsageDecision
        {
            Allowed = false,
            BlockReason = reason,
            UserFacingMessage = userMessage
        };
    }
    private static int EstimateTokens(string question, ConversationMessage[]? history, AiGovernanceOptions options)
    {
        var charsPerToken = Math.Max(1, options.EstimatedCharsPerToken);
        var charCount = question.Length + (history?.Sum(x => x.Content.Length + x.Role.Length) ?? 0);
        return (int)Math.Ceiling((double)charCount / charsPerToken);
    }
    private static decimal EstimateCostUsd(int estimatedTokens)
    {
        // Low-fidelity approximation for soft budgeting only.
        return Math.Round((estimatedTokens / 1000m) * 0.0025m, 6);
    }
    private static string? ExtractSessionId(AssistantContext? context)
    {
        if (context?.Metadata is null)
        {
            return null;
        }
        if (context.Metadata.TryGetValue("sessionId", out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }
        if (context.Metadata.TryGetValue("session_id", out value) && !string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }
        return null;
    }
    private static ConversationMessage[]? SanitizeHistory(ConversationMessage[]? history, int maxMessages = 8, int maxCharsPerMessage = 1200)
    {
        if (history is null || history.Length == 0)
        {
            return null;
        }
        return history
            .TakeLast(Math.Max(1, maxMessages))
            .Select(x => new ConversationMessage
            {
                Role = Sanitize(x.Role),
                Content = SanitizeWithLimit(x.Content, maxCharsPerMessage)
            })
            .ToArray();
    }
    private static string Sanitize(string input)
    {
        return SanitizeWithLimit(input, int.MaxValue);
    }
    private static string SanitizeWithLimit(string input, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }
        var withoutControls = Regex.Replace(input, "[\\u0000-\\u0008\\u000B\\u000C\\u000E-\\u001F]", string.Empty);
        var normalized = Regex.Replace(withoutControls, "\\s+", " ").Trim();
        var bounded = Math.Max(1, maxChars);
        return normalized.Length <= bounded ? normalized : normalized[..bounded];
    }
}