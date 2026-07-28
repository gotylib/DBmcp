using System.Text.RegularExpressions;

namespace DBmcp.Services.Common;

public static partial class SqlSafetyGuard
{
    private static readonly HashSet<string> AllowedFirstTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "SELECT",
        "WITH",
    };

    private static readonly HashSet<string> ForbiddenKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "INSERT",
        "UPDATE",
        "DELETE",
        "DROP",
        "CREATE",
        "ALTER",
        "TRUNCATE",
        "MERGE",
        "EXEC",
        "EXECUTE",
        "CALL",
        "GRANT",
        "REVOKE",
        "REPLACE",
        "COPY",
        "INTO",
    };

    public static bool IsReadOnly(string sql)
    {
        var stripped = StripLeadingCommentsAndWhitespace(sql);
        if (string.IsNullOrWhiteSpace(stripped))
            return false;

        if (HasMultipleStatements(stripped))
            return false;

        var firstToken = GetFirstToken(stripped);
        if (firstToken is null || !AllowedFirstTokens.Contains(firstToken))
            return false;

        foreach (Match match in WordTokenRegex().Matches(stripped))
        {
            if (ForbiddenKeywords.Contains(match.Value))
                return false;
        }

        return true;
    }

    public static bool IsUnsafeEnabled()
    {
        var value = Environment.GetEnvironmentVariable("DBMCP_UNSAFE");
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.Equals("true", StringComparison.OrdinalIgnoreCase)
            || value.Equals("1", StringComparison.OrdinalIgnoreCase)
            || value.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }

    private static string StripLeadingCommentsAndWhitespace(string sql)
    {
        var span = sql.AsSpan().TrimStart();

        while (!span.IsEmpty)
        {
            if (span.StartsWith("--"))
            {
                var newline = span.IndexOfAny('\r', '\n');
                span = newline < 0 ? ReadOnlySpan<char>.Empty : span[(newline + 1)..].TrimStart();
                continue;
            }

            if (span.StartsWith("/*"))
            {
                var end = span.IndexOf("*/");
                if (end < 0)
                    return string.Empty;

                span = span[(end + 2)..].TrimStart();
                continue;
            }

            break;
        }

        return span.ToString();
    }

    private static bool HasMultipleStatements(string sql)
    {
        var semicolonIndex = sql.IndexOf(';');
        if (semicolonIndex < 0)
            return false;

        var after = sql.AsSpan(semicolonIndex + 1).Trim();
        return !after.IsEmpty;
    }

    private static string? GetFirstToken(string sql)
    {
        var match = WordTokenRegex().Match(sql);
        return match.Success ? match.Value : null;
    }

    [GeneratedRegex(@"\b[A-Za-z_][A-Za-z0-9_]*\b")]
    private static partial Regex WordTokenRegex();
}
