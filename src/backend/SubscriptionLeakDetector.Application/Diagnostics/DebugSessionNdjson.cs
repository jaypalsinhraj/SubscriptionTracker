using System.Text.Json;

namespace SubscriptionLeakDetector.Application.Diagnostics;

#region agent log
/// <summary>Debug-mode NDJSON sink (session 16d4ea). Do not log secrets or PII.</summary>
internal static class DebugSessionNdjson
{
    private const string FileName = "debug-16d4ea.log";
    private const string SessionId = "16d4ea";
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    public static void Write(string hypothesisId, string location, string message, object? data = null)
    {
        try
        {
            var path = ResolveLogPath();
            var line = JsonSerializer.Serialize(new
            {
                sessionId = SessionId,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                hypothesisId,
                location,
                message,
                data
            }, JsonOpts);
            File.AppendAllText(path, line + "\n");
        }
        catch
        {
            // intentionally silent — debug must not break import/detection
        }
    }

    private static string ResolveLogPath()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var dir = start;
            for (var i = 0; i < 18 && !string.IsNullOrEmpty(dir); i++)
            {
                var repoSln = Path.Combine(dir!, "src", "backend", "SubscriptionLeakDetector.sln");
                if (File.Exists(repoSln))
                    return Path.Combine(dir!, FileName);

                var backendSln = Path.Combine(dir!, "SubscriptionLeakDetector.sln");
                if (File.Exists(backendSln))
                {
                    var repoRoot = Path.GetFullPath(Path.Combine(dir!, "..", ".."));
                    return Path.Combine(repoRoot, FileName);
                }

                dir = Directory.GetParent(dir)?.FullName;
            }
        }

        return Path.Combine(Directory.GetCurrentDirectory(), FileName);
    }
}
#endregion
