using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Options;
using KaiAssistant.Domain.Entities;
using KaiAssistant.Domain.Entities.AI;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using ConversationDocument = KaiAssistant.Domain.Entities.AI.Conversation;
namespace KaiAssistant.Infrastructure.AI.Conversation;

public sealed class ConversationService : IConversationService
{
    private readonly IMongoCollection<ConversationDocument> _collection;
    private readonly IOptionsMonitor<ConversationOptions> _options;
    private readonly IFeatureFlagService _featureFlags;
    public ConversationService(
        IMongoDatabase database,
        IOptionsMonitor<ConversationOptions> options,
        IFeatureFlagService featureFlags)
    {
        _collection = database.GetCollection<ConversationDocument>("ai_conversations");
        _options = options;
        _featureFlags = featureFlags;
    }
    public async Task<IReadOnlyList<ConversationMessage>> GetShortTermMemoryAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        if (!_options.CurrentValue.Enabled || !_featureFlags.EnableConversationMemory || string.IsNullOrWhiteSpace(conversationId))
        {
            return Array.Empty<ConversationMessage>();
        }
        var conversation = await _collection
            .Find(x => x.ConversationId == conversationId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (conversation is null)
        {
            return Array.Empty<ConversationMessage>();
        }
        var window = Math.Max(1, _options.CurrentValue.ShortTermWindow);
        var ordered = conversation.Messages.OrderBy(x => x.Timestamp).ToList();
        var startIndex = Math.Max(0, ordered.Count - window);
        var result = new List<ConversationMessage>(ordered.Count - startIndex);
        for (var i = startIndex; i < ordered.Count; i++)
        {
            result.Add(new ConversationMessage { Role = ordered[i].Role, Content = ordered[i].Content });
        }
        return result;
    }
    public async Task<string?> GetSummaryAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        if (!_options.CurrentValue.Enabled || !_featureFlags.EnableConversationMemory || string.IsNullOrWhiteSpace(conversationId))
        {
            return null;
        }
        var conversation = await _collection
            .Find(x => x.ConversationId == conversationId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return conversation?.Summary;
    }
    public async Task AppendAsync(
        string conversationId,
        ConversationMessage userMessage,
        ConversationMessage assistantMessage,
        CancellationToken cancellationToken = default)
    {
        if (!_options.CurrentValue.Enabled || !_featureFlags.EnableConversationMemory || string.IsNullOrWhiteSpace(conversationId))
        {
            return;
        }
        var existing = await _collection
            .Find(x => x.ConversationId == conversationId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        existing ??= new ConversationDocument
        {
            ConversationId = conversationId,
            UpdatedAt = DateTime.UtcNow
        };
        existing.Messages.Add(new ConversationEntry
        {
            Role = userMessage.Role,
            Content = userMessage.Content,
            Timestamp = DateTime.UtcNow
        });
        existing.Messages.Add(new ConversationEntry
        {
            Role = assistantMessage.Role,
            Content = assistantMessage.Content,
            Timestamp = DateTime.UtcNow
        });
        ApplySummarization(existing);
        existing.UpdatedAt = DateTime.UtcNow;
        existing.Id ??= ObjectId.GenerateNewId().ToString();
        await _collection.ReplaceOneAsync(
            x => x.ConversationId == conversationId,
            existing,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken).ConfigureAwait(false);
    }
    private void ApplySummarization(ConversationDocument conversation)
    {
        var maxMessages = Math.Max(2, _options.CurrentValue.MaxMessages);
        if (conversation.Messages.Count <= maxMessages)
        {
            return;
        }
        var overflow = conversation.Messages.Take(conversation.Messages.Count - _options.CurrentValue.ShortTermWindow).ToList();
        var summary = string.Join(" ", overflow.Select(x => $"{x.Role}: {x.Content}"));
        if (summary.Length > _options.CurrentValue.SummaryMaxChars)
        {
            summary = summary[.._options.CurrentValue.SummaryMaxChars];
        }
        if (string.IsNullOrWhiteSpace(conversation.Summary))
        {
            conversation.Summary = summary;
        }
        else
        {
            var merged = conversation.Summary + " " + summary;
            conversation.Summary = merged.Length > _options.CurrentValue.SummaryMaxChars
                ? merged[^_options.CurrentValue.SummaryMaxChars..]
                : merged;
        }
        conversation.Messages = conversation.Messages.TakeLast(_options.CurrentValue.ShortTermWindow).ToList();
    }
}