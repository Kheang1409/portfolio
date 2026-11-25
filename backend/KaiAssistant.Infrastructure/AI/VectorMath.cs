namespace KaiAssistant.Infrastructure.AI;

internal static class VectorMath
{
    public static double CosineSimilarity(ReadOnlySpan<float> first, ReadOnlySpan<float> second)
    {
        if (first.Length == 0 || second.Length == 0)
        {
            return -1;
        }
        var len = Math.Min(first.Length, second.Length);
        double dot = 0;
        double normA = 0;
        double normB = 0;
        for (var i = 0; i < len; i++)
        {
            var a = first[i];
            var b = second[i];
            dot += a * b;
            normA += a * a;
            normB += b * b;
        }
        if (normA <= 0 || normB <= 0)
        {
            return -1;
        }
        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}