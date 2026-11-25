using Mongo2Go;
using System;
using System.IO;

namespace KaiAssistant.Tests.Integration;

internal static class MongoTestRunner
{
    public static MongoDbRunner Start()
    {
        var root = Path.Combine(Path.GetTempPath(), "KaiAssistant.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var packageRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages", "mongo2go", "4.0.0", "tools");
        return MongoDbRunner.Start(dataDirectory: root, binariesSearchDirectory: packageRoot);
    }
}
