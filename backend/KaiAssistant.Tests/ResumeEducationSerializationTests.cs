using KaiAssistant.Domain.Entities.Resumes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Xunit;

namespace KaiAssistant.Tests;

public sealed class ResumeEducationSerializationTests
{
    [Fact]
    public void DeserializeResume_PreservesEducationStatus()
    {
        var document = new BsonDocument
        {
            ["Educations"] = new BsonArray
            {
                new BsonDocument
                {
                    ["Degree"] = "Bachelor's degree",
                    ["Institution"] = "Example University",
                    ["Status"] = "Completed"
                }
            }
        };

        var resume = BsonSerializer.Deserialize<Resume>(document);

        var education = Assert.Single(resume.Educations);
        Assert.Equal("Completed", education.Status);
    }
}
