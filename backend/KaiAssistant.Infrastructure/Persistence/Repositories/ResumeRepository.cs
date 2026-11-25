using KaiAssistant.Application.Cache;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Domain.Entities.Resumes;
using KaiAssistant.Domain.Interfaces.Repositories;
using KaiAssistant.Infrastructure.Mongo;
using System.Diagnostics.Metrics;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
namespace KaiAssistant.Infrastructure.Persistence.Repositories;

public class ResumeRepository : IResumeRepository
{
    private static readonly Meter Meter = new("KaiAssistant.Resume", "1.0.0");
    private static readonly Counter<long> ResumeReads = Meter.CreateCounter<long>("resume_reads_total");
    private readonly IMongoCollection<Resume> _collection;
    private readonly IMongoCollection<BsonDocument> _bsonCollection;
    private readonly ICacheService _cacheService;
    private readonly IMongoWriteProvider _writeProvider;
    public ResumeRepository(IMongoReadProvider readProvider, IMongoWriteProvider writeProvider, ICacheService cacheService)
    {
        _writeProvider = writeProvider;
        _collection = writeProvider.Database.GetCollection<Resume>("resumes");
        _bsonCollection = readProvider.Database.GetCollection<BsonDocument>("resumes");
        _cacheService = cacheService;
    }
    public async Task<Resume?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var cached = await _cacheService.GetAsync<Resume>(CacheKeys.ResumeById(id), cancellationToken).ConfigureAwait(false);
        if (cached is not null)
        {
            ResumeReads.Add(1,
                KeyValuePair.Create<string, object?>("method", "by_id"),
                KeyValuePair.Create<string, object?>("source", "cache"),
                KeyValuePair.Create<string, object?>("success", true));
            return cached;
        }
        if (!ObjectId.TryParse(id, out var objectId))
        {
            return null;
        }
        var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
        var doc = await _bsonCollection
            .Find(filter)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (doc == null) return null;
        NormalizeExperienceBulletPoints(doc);
        var result = BsonSerializer.Deserialize<Resume>(doc);
        await _cacheService.SetAsync(CacheKeys.ResumeById(id), result, TimeSpan.FromMinutes(2), cancellationToken).ConfigureAwait(false);
        ResumeReads.Add(1,
            KeyValuePair.Create<string, object?>("method", "by_id"),
            KeyValuePair.Create<string, object?>("source", "mongo"),
            KeyValuePair.Create<string, object?>("success", result is not null));
        return result;
    }
    public async Task<Resume?> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        var cached = await _cacheService.GetAsync<Resume>(CacheKeys.LatestResume(), cancellationToken).ConfigureAwait(false);
        if (cached is not null)
        {
            ResumeReads.Add(1,
                KeyValuePair.Create<string, object?>("method", "latest"),
                KeyValuePair.Create<string, object?>("source", "cache"),
                KeyValuePair.Create<string, object?>("success", true));
            return cached;
        }
        var doc = await _bsonCollection
            .Find(Builders<BsonDocument>.Filter.Empty)
            .SortByDescending(d => d["CreatedAtUtc"])
            .ThenByDescending(d => d["_id"])
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (doc == null) return null;
        NormalizeExperienceBulletPoints(doc);
        var latest = BsonSerializer.Deserialize<Resume>(doc);
        await _cacheService.SetAsync(CacheKeys.LatestResume(), latest, TimeSpan.FromMinutes(2), cancellationToken).ConfigureAwait(false);
        ResumeReads.Add(1,
            KeyValuePair.Create<string, object?>("method", "latest"),
            KeyValuePair.Create<string, object?>("source", "mongo"),
            KeyValuePair.Create<string, object?>("success", latest is not null));
        return latest;
    }
    private void NormalizeExperienceBulletPoints(BsonDocument doc)
    {
        if (!doc.Contains("experiences")) return;
        var experiences = doc.GetValue("experiences").AsBsonArray;
        for (int i = 0; i < experiences.Count; i++)
        {
            if (!experiences[i].IsBsonDocument) continue;
            var expDoc = experiences[i].AsBsonDocument;
            if (expDoc.Contains("BulletPoint") && !expDoc.Contains("BulletPoints"))
            {
                expDoc["BulletPoints"] = expDoc.GetValue("BulletPoint");
                expDoc.Remove("BulletPoint");
            }
            if (expDoc.Contains("bulletPoint") && !expDoc.Contains("BulletPoints"))
            {
                expDoc["BulletPoints"] = expDoc.GetValue("bulletPoint");
                expDoc.Remove("bulletPoint");
            }
            if (expDoc.Contains("bulletPoints") && !expDoc.Contains("BulletPoints"))
            {
                expDoc["BulletPoints"] = expDoc.GetValue("bulletPoints");
                expDoc.Remove("bulletPoints");
            }
            experiences[i] = expDoc;
        }
        doc["experiences"] = experiences;
    }
    public async Task InsertAsync(Resume resume, CancellationToken cancellationToken = default)
    {
        if (resume == null) throw new ArgumentNullException(nameof(resume));
        await _writeProvider.ExecuteWriteAsync(
            db => db.GetCollection<Resume>("resumes").InsertOneAsync(resume, cancellationToken: cancellationToken),
            cancellationToken).ConfigureAwait(false);
        await _cacheService.RemoveAsync(CacheKeys.LatestResume(), cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(resume.Id))
        {
            await _cacheService.RemoveAsync(CacheKeys.ResumeById(resume.Id), cancellationToken).ConfigureAwait(false);
        }
    }
}