using System.Text.RegularExpressions;

namespace DBmcp.Services.Common;

public static partial class ConnectionStringNormalizer
{
    private const string LocalhostHostEnvVar = "DBMCP_LOCALHOST_HOST";
    private const string DefaultDockerHost = "host.docker.internal";

    public static string Normalize(string connectionString)
    {
        var replacement = GetLocalhostReplacement();
        if (replacement is null)
            return connectionString;

        var result = LocalhostRegex().Replace(connectionString, replacement);
        return LoopbackRegex().Replace(result, replacement);
    }

    private static string? GetLocalhostReplacement()
    {
        var configured = Environment.GetEnvironmentVariable(LocalhostHostEnvVar);
        if (!string.IsNullOrWhiteSpace(configured))
            return configured.Trim();

        return IsRunningInDocker() ? DefaultDockerHost : null;
    }

    private static bool IsRunningInDocker() => File.Exists("/.dockerenv");

    [GeneratedRegex(@"\blocalhost\b", RegexOptions.IgnoreCase)]
    private static partial Regex LocalhostRegex();

    [GeneratedRegex(@"\b127\.0\.0\.1\b")]
    private static partial Regex LoopbackRegex();
}
