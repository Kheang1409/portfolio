using KaiAssistant.Domain.Entities.Certifications;
using KaiAssistant.Domain.Entities.Educations;
using KaiAssistant.Domain.Entities.Experiences;
using KaiAssistant.Domain.Entities.Projects;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace KaiAssistant.Domain.Entities.Resumes;

[BsonIgnoreExtraElements]
public class Resume
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public Personals? Personals { get; set; }
    public string Summary { get; set; } = string.Empty;
    public List<string> Skills { get; internal set; } = new();
    public void AddSkill(string skill)
    {
        if (string.IsNullOrWhiteSpace(skill))
            return;
        if (!Skills.Contains(skill))
            Skills.Add(skill);
    }
    public bool RemoveSkill(string skill) => Skills.Remove(skill);
    public List<Experience> Experiences { get; internal set; } = new();
    public void AddExperience(Experience experience)
    {
        if (experience != null)
            Experiences.Add(experience);
    }
    public List<Project> Projects { get; internal set; } = new();
    public void AddProject(Project project)
    {
        if (project != null)
            Projects.Add(project);
    }
    public List<Education> Educations { get; internal set; } = new();
    public void AddEducation(Education education)
    {
        if (education != null)
            Educations.Add(education);
    }
    public List<Certification> Certifications { get; internal set; } = new();
    public void AddCertification(Certification certification)
    {
        if (certification != null)
            Certifications.Add(certification);
    }
    public Resume() { }
}