using System.Text.RegularExpressions;

namespace ECodex.Core.Services;

public enum AgentAttentionSignalKind
{
    WaitingInput,
    ConfirmationRequired,
    ErrorNeedsDecision,
}

public sealed record AgentAttentionDetectionInput(
    string? TextTail,
    string? RecentCommand = null,
    string? AgentHint = null);

public sealed record AgentAttentionSignal(
    AgentAttentionSignalKind Kind,
    string Title,
    string Summary,
    string MatchedText);

/// <summary>
/// Conservative text-tail detector for Codex states that need user attention.
/// It is pure Core logic: no UI, no notification side effects, and no raw byte scanning.
/// </summary>
public static class AgentAttentionSignalDetector
{
    private const int MaxInputChars = 8000;
    private const int MaxSummaryLength = 180;

    private static readonly Regex CodexContextRegex = new(
        @"\bcodex(?:\.exe)?\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex ConfirmationRegex = new(
        @"\b(do you want to allow|allow this command|approval required|requires approval|confirm (?:this|the)|approve\?)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex ErrorDecisionRegex = new(
        @"\b(cannot continue without (?:a )?decision|choose how to proceed|manual intervention required|needs your decision|requires your decision)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex WaitingInputRegex = new(
        @"\b(waiting for (?:user |your )?input|please respond|press enter to continue|enter your response|awaiting (?:your )?input)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex SecretEnvAssignmentRegex = new(
        @"(^|[\s;])([A-Za-z_][A-Za-z0-9_]*\s*=\s*)(""[^""\r\n]*""|'[^'\r\n]*'|[^\s\r\n]+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex SecretFlagRegex = new(
        @"(\-\-(?:password|passwd|pwd|token|secret|api[-_]?key|access[-_]?key)(?:\s+|=)|\-(?:password|passwd|pwd|token|secret)(?:\s+|=))(""[^""\r\n]*""|'[^'\r\n]*'|[^\s\r\n]+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex UriCredentialsRegex = new(
        @"([a-z][a-z0-9+\-.]*://[^\s/@:]+:)([^@\s]+)(@)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static AgentAttentionSignal? Detect(string? textTail, string? recentCommand = null, string? agentHint = null)
        => Detect(new AgentAttentionDetectionInput(textTail, recentCommand, agentHint));

    public static AgentAttentionSignal? Detect(AgentAttentionDetectionInput input)
    {
        var textTail = NormalizeInput(input.TextTail);
        if (string.IsNullOrWhiteSpace(textTail))
            return null;

        if (!IsCodexContext(input, textTail))
            return null;

        var sanitized = SanitizeSummaryText(textTail);

        return TryCreateSignal(
                sanitized,
                ConfirmationRegex,
                AgentAttentionSignalKind.ConfirmationRequired,
                "Codex 等待确认")
            ?? TryCreateSignal(
                sanitized,
                ErrorDecisionRegex,
                AgentAttentionSignalKind.ErrorNeedsDecision,
                "Codex 需要决策")
            ?? TryCreateSignal(
                sanitized,
                WaitingInputRegex,
                AgentAttentionSignalKind.WaitingInput,
                "Codex 等待输入");
    }

    private static bool IsCodexContext(AgentAttentionDetectionInput input, string textTail)
    {
        return CodexContextRegex.IsMatch(input.AgentHint ?? "") ||
               CodexContextRegex.IsMatch(input.RecentCommand ?? "") ||
               CodexContextRegex.IsMatch(textTail);
    }

    private static AgentAttentionSignal? TryCreateSignal(
        string sanitizedText,
        Regex regex,
        AgentAttentionSignalKind kind,
        string title)
    {
        if (!regex.IsMatch(sanitizedText))
            return null;

        var summary = CreateSummary(sanitizedText, regex);
        if (string.IsNullOrWhiteSpace(summary))
            return null;

        return new AgentAttentionSignal(kind, title, summary, regex.ToString());
    }

    private static string NormalizeInput(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var tail = text.Length > MaxInputChars ? text[^MaxInputChars..] : text;
        return RemoveControlCharacters(tail);
    }

    private static string SanitizeSummaryText(string text)
    {
        var sanitized = SecretEnvAssignmentRegex.Replace(text, match =>
        {
            var assignmentPrefix = match.Groups[2].Value;
            var equalsIndex = assignmentPrefix.IndexOf('=');
            var key = equalsIndex >= 0 ? assignmentPrefix[..equalsIndex].Trim() : assignmentPrefix.Trim();

            return IsSensitiveEnvName(key)
                ? $"{match.Groups[1].Value}{assignmentPrefix}[REDACTED]"
                : match.Value;
        });
        sanitized = SecretFlagRegex.Replace(sanitized, "$1[REDACTED]");
        sanitized = UriCredentialsRegex.Replace(sanitized, "$1[REDACTED]$3");
        return sanitized;
    }

    private static string CreateSummary(string sanitizedText, Regex regex)
    {
        var lines = sanitizedText
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n')
            .Select(CollapseWhitespace)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        var matchedLine = lines.FirstOrDefault(line => regex.IsMatch(line))
            ?? CollapseWhitespace(sanitizedText);
        if (matchedLine.Length <= MaxSummaryLength)
            return matchedLine;

        return matchedLine[..(MaxSummaryLength - 3)].TrimEnd() + "...";
    }

    private static string CollapseWhitespace(string value)
        => Regex.Replace(value.Trim(), @"\s+", " ");

    private static string RemoveControlCharacters(string value)
    {
        return new string(value
            .Where(ch => ch == '\r' || ch == '\n' || ch == '\t' || !char.IsControl(ch))
            .ToArray());
    }

    private static bool IsSensitiveEnvName(string key)
    {
        var upper = key.ToUpperInvariant();
        return upper.Contains("PASSWORD", StringComparison.Ordinal) ||
               upper.Contains("PASSWD", StringComparison.Ordinal) ||
               upper.Contains("SECRET", StringComparison.Ordinal) ||
               upper.Contains("API_KEY", StringComparison.Ordinal) ||
               upper.Contains("ACCESS_KEY", StringComparison.Ordinal) ||
               upper is "TOKEN" ||
               upper.EndsWith("_TOKEN", StringComparison.Ordinal) ||
               upper.Contains("_TOKEN_", StringComparison.Ordinal) ||
               upper.StartsWith("TOKEN_", StringComparison.Ordinal);
    }
}
