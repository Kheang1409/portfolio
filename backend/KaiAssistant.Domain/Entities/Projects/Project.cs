namespace KaiAssistant.Domain.Entities.Projects;

public class Project
{
    public string Name { get; internal set; } = string.Empty;
    public string Description { get; internal set; } = string.Empty;
    public List<string> Skills { get; internal set; } = new();
    public Project() { }
    public static Project Create(string name, string description, IEnumerable<string>? skills = null)
    {
        return new Project
        {
            Name = name,
            Description = description,
            Skills = skills != null ? new List<string>(skills) : new()
        };
    }
    public void AddSkill(string skill)
    {
        if (!string.IsNullOrWhiteSpace(skill))
            Skills.Add(skill);
    }
}