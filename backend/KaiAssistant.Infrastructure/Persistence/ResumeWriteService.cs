using KaiAssistant.Application.Cache;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Domain.Entities.Outbox;
using KaiAssistant.Domain.Entities.Resumes;
using KaiAssistant.Infrastructure.Mongo;
using MongoDB.Driver;
namespace KaiAssistant.Infrastructure.Persistence;

public sealed class ResumeWriteService : IResumeWriteService
{
    private readonly IMongoClient _mongoClient;
    private readonly IMongoCollection<Resume> _resumeCollection;
    private readonly IMongoCollection<OutboxMessage> _outboxCollection;
    private readonly ICacheService _cacheService;
    public ResumeWriteService(IMongoClient mongoClient, IMongoWriteProvider writeProvider, ICacheService cacheService)
    {
        _mongoClient = mongoClient;
        _resumeCollection = writeProvider.Database.GetCollection<Resume>("resumes");
        _outboxCollection = writeProvider.Database.GetCollection<OutboxMessage>("outbox_messages");
        _cacheService = cacheService;
    }
    public async Task<Resume> CreateWithOutboxAsync(
        Resume resume,
        OutboxMessage outboxMessage,
        CancellationToken cancellationToken = default)
    {
        using var session = await _mongoClient.StartSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        session.StartTransaction();
        try
        {
            await _resumeCollection.InsertOneAsync(session, resume, cancellationToken: cancellationToken).ConfigureAwait(false);
            await _outboxCollection.InsertOneAsync(session, outboxMessage, cancellationToken: cancellationToken).ConfigureAwait(false);
            await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
            await _cacheService.SetAsync(CacheKeys.LatestResume(), resume, TimeSpan.FromMinutes(2), cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(resume.Id))
            {
                await _cacheService.SetAsync(CacheKeys.ResumeById(resume.Id), resume, TimeSpan.FromMinutes(2), cancellationToken).ConfigureAwait(false);
            }
            await _cacheService.RemoveAsync(CacheKeys.ResumeChunks(), cancellationToken).ConfigureAwait(false);
            return resume;
        }
        catch
        {
            await session.AbortTransactionAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }
}