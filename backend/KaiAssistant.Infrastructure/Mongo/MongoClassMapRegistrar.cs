using System.Reflection;
using MongoDB.Bson.Serialization;
using KaiAssistant.Domain.Entities.Experiences;
namespace KaiAssistant.Infrastructure.Mongo;

public static class MongoClassMapRegistrar
{
    private static readonly object Sync = new();
    public static void RegisterClassMaps()
    {
        lock (Sync)
        {
            if (BsonClassMap.IsClassMapRegistered(typeof(Experience)))
            {
                return;
            }
            try
            {
                BsonClassMap.RegisterClassMap<Experience>(cm =>
                {
                    cm.AutoMap();
                    cm.MapMember(x => x.BulletPoints).SetElementName("BulletPoints");
                    var field = typeof(Experience).GetField("_bulletPoints", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field != null)
                    {
                        cm.MapField(field.Name).SetElementName("BulletPoints");
                    }
                    cm.SetIgnoreExtraElements(true);
                });
            }
            catch (ArgumentException)
            {
                // Class map already registered by another thread.
            }
        }
    }
}