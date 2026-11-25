using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Cache;
using KaiAssistant.Domain.Entities;
using KaiAssistant.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace KaiAssistant.Application.Services;

public class ResumeContextProvider : IResumeContextProvider
{
    private readonly IResumeRepository _resumeRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ResumeContextProvider> _logger;
    private ResumeChunk[] _resumeChunks = Array.Empty<ResumeChunk>();
    private const int ChunkSize = 500;
    public ResumeContextProvider(IResumeRepository resumeRepository, ICacheService cacheService, ILogger<ResumeContextProvider> logger)
    {
        _resumeRepository = resumeRepository;
        _cacheService = cacheService;
        _logger = logger;
    }
    public async Task<ResumeChunk[]> GetResumeChunksAsync(CancellationToken cancellationToken = default)
    {
        if (_resumeChunks.Length > 0)
            return _resumeChunks;
        var cached = await _cacheService.GetAsync<ResumeChunk[]>(CacheKeys.ResumeChunks(), cancellationToken).ConfigureAwait(false);
        if (cached is { Length: > 0 })
        {
            _resumeChunks = cached;
            return _resumeChunks;
        }
        var resume = await _resumeRepository.GetLatestAsync(cancellationToken).ConfigureAwait(false);
        if (resume == null)
        {
            _logger.LogInformation("No resume found in database. Starting without resume.");
            _resumeChunks = Array.Empty<ResumeChunk>();
            return _resumeChunks;
        }
        var newList = new List<ResumeChunk>();
        if (resume.Personals != null)
        {
            var p = resume.Personals;
            var personalLines = new List<string>();
            if (!string.IsNullOrWhiteSpace(p.LegalName)) personalLines.Add($"Legal name: {p.LegalName}");
            if (!string.IsNullOrWhiteSpace(p.PreferredName) && !string.Equals(p.PreferredName, p.LegalName, StringComparison.OrdinalIgnoreCase))
                personalLines.Add($"Preferred name: {p.PreferredName}");
            if (!string.IsNullOrWhiteSpace(p.Phone)) personalLines.Add($"Phone: {p.Phone}");
            if (!string.IsNullOrWhiteSpace(p.Linkedin)) personalLines.Add($"LinkedIn: {p.Linkedin}");
            if (!string.IsNullOrWhiteSpace(p.Github)) personalLines.Add($"GitHub: {p.Github}");
            if (!string.IsNullOrWhiteSpace(p.Email)) personalLines.Add($"Email: {p.Email}");
            if (!string.IsNullOrWhiteSpace(p.Portfolio)) personalLines.Add($"Portfolio: {p.Portfolio}");
            AddChunkToList(newList, "Personal Details", string.Join("\n", personalLines));
        }
        AddChunkToList(newList, "Summary", resume.Summary ?? string.Empty);
        if (resume.Skills != null && resume.Skills.Any())
        {
            AddChunkToList(newList, "Skills", string.Join(", ", resume.Skills));
        }
        if (resume.Experiences != null)
        {
            foreach (var job in resume.Experiences)
            {
                if (job == null) continue;
                var lines = new List<string>();
                if (!string.IsNullOrWhiteSpace(job.Role)) lines.Add($"Role: {job.Role}");
                if (!string.IsNullOrWhiteSpace(job.Company)) lines.Add($"Company: {job.Company}");
                if (job.StartDates.HasValue || job.EndDate.HasValue)
                {
                    var start = job.StartDates.HasValue ? job.StartDates.Value.ToString("yyyy-MM") : "";
                    var end = job.EndDate.HasValue ? job.EndDate.Value.ToString("yyyy-MM") : "Present";
                    lines.Add($"Dates: {start} - {end}");
                }
                if (job.BulletPoints != null && job.BulletPoints.Any())
                    lines.Add(string.Join("\n", job.BulletPoints));
                AddChunkToList(newList, $"Experience at {job.Company}", string.Join("\n", lines));
            }
        }
        if (resume.Educations != null)
        {
            foreach (var education in resume.Educations)
            {
                if (education == null) continue;
                var lines = new List<string>();
                if (!string.IsNullOrWhiteSpace(education.Degree)) lines.Add($"Degree: {education.Degree}");
                if (!string.IsNullOrWhiteSpace(education.Institution)) lines.Add($"Institution: {education.Institution}");
                if (!string.IsNullOrWhiteSpace(education.Status)) lines.Add($"Status: {education.Status}");
                if (!string.IsNullOrWhiteSpace(education.Location)) lines.Add($"Location: {education.Location}");
                if (education.StartDates.HasValue || education.EndDate.HasValue)
                {
                    var start = education.StartDates.HasValue ? education.StartDates.Value.ToString("yyyy-MM") : "";
                    var end = education.EndDate.HasValue ? education.EndDate.Value.ToString("yyyy-MM") : "Present";
                    lines.Add($"Dates: {start} - {end}");
                }
                AddChunkToList(newList, $"Education: {education.Institution}", string.Join("\n", lines));
            }
        }
        if (resume.Certifications != null)
        {
            foreach (var cert in resume.Certifications)
            {
                if (cert == null) continue;
                var lines = new List<string>();
                if (!string.IsNullOrWhiteSpace(cert.Title)) lines.Add(cert.Title);
                if (!string.IsNullOrWhiteSpace(cert.Issuer)) lines.Add($"Issuer: {cert.Issuer}");
                if (cert.Date.HasValue) lines.Add($"Date: {cert.Date.Value:yyyy-MM}");
                AddChunkToList(newList, $"Certification: {cert.Title}", string.Join(" | ", lines));
            }
        }
        if (resume.Projects != null)
        {
            foreach (var project in resume.Projects)
            {
                AddChunkToList(newList, $"Project: {project?.Name}", project?.Description ?? string.Empty);
            }
        }
        _resumeChunks = newList.ToArray();
        await _cacheService.SetAsync(CacheKeys.ResumeChunks(), _resumeChunks, TimeSpan.FromMinutes(2), cancellationToken).ConfigureAwait(false);
        return _resumeChunks;
    }
    public async Task<ResumeChunk[]> GetRelevantChunksAsync(string question, CancellationToken cancellationToken = default)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(question.ToLowerInvariant())));
        var cached = await _cacheService.GetAsync<ResumeChunk[]>(CacheKeys.RelevantChunks(hash), cancellationToken).ConfigureAwait(false);
        if (cached is { Length: > 0 })
        {
            return cached;
        }
        var snapshot = await GetResumeChunksAsync(cancellationToken).ConfigureAwait(false);
        if (snapshot == null || snapshot.Length == 0) return Array.Empty<ResumeChunk>();
        var questionLower = question.ToLowerInvariant();
        var keywordMappings = new Dictionary<string, string[]>
        {
            { "experience", new[] { "experience", "work", "job", "role", "company", "worked", "career" } },
            { "skills", new[] { "skill", "technology", "tech", "know", "programming", "language", "framework", "tool" } },
            { "education", new[] { "education", "degree", "university", "school", "study", "studied", "graduate" } },
            { "project", new[] { "project", "built", "created", "developed", "portfolio" } },
            { "summary", new[] { "about", "who", "background", "introduction", "profile" } }
        };
        var scoredChunks = snapshot.Select(chunk =>
        {
            int score = 0;
            var chunkLower = (chunk.Label + " " + chunk.Content).ToLowerInvariant();
            foreach (var mapping in keywordMappings)
            {
                if (chunk.Label != null && chunk.Label.ToLowerInvariant().Contains(mapping.Key))
                {
                    if (mapping.Value.Any(keyword => questionLower.Contains(keyword)))
                        score += 10;
                }
            }
            var questionWords = questionLower
                .Split(new[] { ' ', '?', '!', '.' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3)
                .ToArray();
            foreach (var word in questionWords) if (chunkLower.Contains(word)) score += 2;
            return new { Chunk = chunk, Score = score };
        })
        .OrderByDescending(x => x.Score)
        .ToList();
        var maxChunks = 8;
        var envTop = Environment.GetEnvironmentVariable("GEMINI_TOP_CHUNKS");
        if (!string.IsNullOrWhiteSpace(envTop) && int.TryParse(envTop, out var parsedTop) && parsedTop > 0)
            maxChunks = parsedTop;
        var topChunks = scoredChunks
            .Where(x => x.Score > 0 || (x.Chunk.Label != null && x.Chunk.Label.Contains("Summary")))
            .Take(maxChunks)
            .Select(x => x.Chunk)
            .ToList();
        var result = topChunks.Any() ? topChunks.ToArray() : snapshot.Take(maxChunks).ToArray();
        await _cacheService.SetAsync(CacheKeys.RelevantChunks(hash), result, TimeSpan.FromSeconds(45), cancellationToken).ConfigureAwait(false);
        return result;
    }
    private void AddChunkToList(List<ResumeChunk> list, string label, string content, string source = "")
    {
        if (string.IsNullOrWhiteSpace(content)) return;
        for (int i = 0; i < content.Length; i += ChunkSize)
        {
            list.Add(new ResumeChunk
            {
                Label = label,
                Source = source,
                Content = content.Substring(i, System.Math.Min(ChunkSize, content.Length - i))
            });
        }
    }
}