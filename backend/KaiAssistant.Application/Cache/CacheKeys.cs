namespace KaiAssistant.Application.Cache;

public static class CacheKeys
{
    public const string Version = "v2";
    public static string LatestResume() => $"{Version}:resume:latest";
    public static string ResumeById(string id) => $"{Version}:resume:id:{id}";
    public static string ResumeChunks() => $"{Version}:resume:chunks";
    public static string RelevantChunks(string hash) => $"{Version}:resume:relevant:{hash}";
}