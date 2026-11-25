using KaiAssistant.Application.Interfaces;
using KaiAssistant.Domain.Interfaces.Repositories;
using KaiAssistant.Infrastructure.Mongo;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
namespace KaiAssistant.Infrastructure.Persistence;

public class MongoRepository<T> : IRepository<T> where T : class
{
    private readonly IMongoCollection<BsonDocument> _readCollection;
    private readonly IMongoCollection<BsonDocument> _writeCollection;
    public MongoRepository(IMongoReadProvider readProvider, IMongoWriteProvider writeProvider)
    {
        var name = typeof(T).Name.ToLowerInvariant() + "s";
        _readCollection = readProvider.Database.GetCollection<BsonDocument>(name);
        _writeCollection = writeProvider.Database.GetCollection<BsonDocument>(name);
    }
    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var docs = await _readCollection.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync(cancellationToken).ConfigureAwait(false);
        return docs.Select(d => BsonSerializer.Deserialize<T>(d));
    }
    public async Task<IEnumerable<T>> GetAllAsync(IUnitOfWorkSession? uowSession, CancellationToken cancellationToken = default)
    {
        var session = uowSession?.NativeSession as IClientSessionHandle;
        var docs = session == null
            ? await _readCollection.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync(cancellationToken).ConfigureAwait(false)
            : await _readCollection.Find(session, Builders<BsonDocument>.Filter.Empty).ToListAsync(cancellationToken).ConfigureAwait(false);
        return docs.Select(d => BsonSerializer.Deserialize<T>(d));
    }
    public async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (ObjectId.TryParse(id, out var oid))
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", oid);
            var doc = await _readCollection.Find(filter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (doc == null) return null;
            return BsonSerializer.Deserialize<T>(doc);
        }
        var f2 = Builders<BsonDocument>.Filter.Eq("_id", id);
        var doc2 = await _readCollection.Find(f2).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (doc2 == null) return null;
        return BsonSerializer.Deserialize<T>(doc2);
    }
    public async Task<T?> GetByIdAsync(string id, IUnitOfWorkSession? uowSession, CancellationToken cancellationToken = default)
    {
        FilterDefinition<BsonDocument> filter = ObjectId.TryParse(id, out var oid)
            ? Builders<BsonDocument>.Filter.Eq("_id", oid)
            : Builders<BsonDocument>.Filter.Eq("_id", id);
        var session = uowSession?.NativeSession as IClientSessionHandle;
        var doc = session == null
            ? await _readCollection.Find(filter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false)
            : await _readCollection.Find(session, filter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (doc == null) return null;
        return BsonSerializer.Deserialize<T>(doc);
    }
    public async Task InsertAsync(T entity, CancellationToken cancellationToken = default)
    {
        var doc = entity switch
        {
            BsonDocument bd => bd,
            _ => entity.ToBsonDocument()
        };
        await _writeCollection.InsertOneAsync(doc, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
    public async Task InsertAsync(T entity, IUnitOfWorkSession? uowSession, CancellationToken cancellationToken = default)
    {
        var doc = entity switch
        {
            BsonDocument bd => bd,
            _ => entity.ToBsonDocument()
        };
        var session = uowSession?.NativeSession as IClientSessionHandle;
        if (session == null)
        {
            await _writeCollection.InsertOneAsync(doc, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _writeCollection.InsertOneAsync(session, doc, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
    public async Task ReplaceAsync(string id, T entity, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out var oid))
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
            var doc = entity.ToBsonDocument();
            await _writeCollection.ReplaceOneAsync(filter, doc, new ReplaceOptions { IsUpsert = false }, cancellationToken).ConfigureAwait(false);
            return;
        }
        var filterOid = Builders<BsonDocument>.Filter.Eq("_id", oid);
        var docOid = entity.ToBsonDocument();
        await _writeCollection.ReplaceOneAsync(filterOid, docOid, new ReplaceOptions { IsUpsert = false }, cancellationToken).ConfigureAwait(false);
    }
    public async Task ReplaceAsync(string id, T entity, IUnitOfWorkSession? uowSession, CancellationToken cancellationToken = default)
    {
        var session = uowSession?.NativeSession as IClientSessionHandle;
        var filter = ObjectId.TryParse(id, out var oid)
            ? Builders<BsonDocument>.Filter.Eq("_id", oid)
            : Builders<BsonDocument>.Filter.Eq("_id", id);
        var doc = entity.ToBsonDocument();
        if (session == null)
            await _writeCollection.ReplaceOneAsync(filter, doc, new ReplaceOptions { IsUpsert = false }, cancellationToken).ConfigureAwait(false);
        else
            await _writeCollection.ReplaceOneAsync(session, filter, doc, new ReplaceOptions { IsUpsert = false }, cancellationToken).ConfigureAwait(false);
    }
    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        FilterDefinition<BsonDocument> filter;
        if (ObjectId.TryParse(id, out var oid)) filter = Builders<BsonDocument>.Filter.Eq("_id", oid);
        else filter = Builders<BsonDocument>.Filter.Eq("_id", id);
        await _writeCollection.DeleteOneAsync(filter, cancellationToken).ConfigureAwait(false);
    }
    public async Task DeleteAsync(string id, IUnitOfWorkSession? uowSession, CancellationToken cancellationToken = default)
    {
        FilterDefinition<BsonDocument> filter;
        if (ObjectId.TryParse(id, out var oid)) filter = Builders<BsonDocument>.Filter.Eq("_id", oid);
        else filter = Builders<BsonDocument>.Filter.Eq("_id", id);
        var session = uowSession?.NativeSession as IClientSessionHandle;
        if (session == null)
            await _writeCollection.DeleteOneAsync(filter, cancellationToken).ConfigureAwait(false);
        else
            await _writeCollection.DeleteOneAsync(session, filter, null, cancellationToken).ConfigureAwait(false);
    }
}