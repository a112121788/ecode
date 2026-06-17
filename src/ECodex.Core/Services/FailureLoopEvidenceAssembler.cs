using ECodex.Core.Models;

namespace ECodex.Core.Services;

public sealed record FailureLoopEvidenceRequest(
    string WorkspaceId,
    string? SurfaceId = null,
    string? PaneId = null)
{
    public string? PackageId { get; init; }
    public DateTimeOffset? CapturedAtUtc { get; init; }
    public TimeSpan WindowBefore { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan WindowAfter { get; init; } = TimeSpan.FromMinutes(5);
    public int MaxTranscriptSummaryChars { get; init; } = 1_200;
    public int MaxDaemonLogLineChars { get; init; } = 1_200;
    public int MaxAgentMessageSummaryChars { get; init; } = 800;
}

public sealed record FailureLoopTranscriptInput(
    TerminalTranscriptEntry Entry,
    string Summary);

public sealed record FailureLoopDaemonLogInput(
    DateTimeOffset TimestampUtc,
    string Line,
    string? PaneId = null);

public sealed record FailureLoopAgentMessageInput(
    string WorkspaceId,
    string? SurfaceId,
    string? PaneId,
    DateTimeOffset CapturedAtUtc,
    string Role,
    string Summary);

/// <summary>
/// Builds a failure-loop evidence package from already-sanitized, caller-provided inputs.
/// </summary>
public sealed class FailureLoopEvidenceAssembler
{
    public FailureLoopEvidencePackage Assemble(
        FailureLoopEvidenceRequest request,
        IEnumerable<CommandLogEntry> commands,
        IEnumerable<FailureLoopTranscriptInput> transcripts,
        IEnumerable<FailureLoopDaemonLogInput> daemonLogs,
        IEnumerable<FailureLoopAgentMessageInput>? agentMessages = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(transcripts);
        ArgumentNullException.ThrowIfNull(daemonLogs);

        if (string.IsNullOrWhiteSpace(request.WorkspaceId))
            throw new ArgumentException("WorkspaceId is required.", nameof(request));

        var failedCommands = commands
            .Where(command => command.ExitCode is not null and not 0)
            .Where(command => MatchesScope(request, command.WorkspaceId, command.SurfaceId, command.PaneId))
            .OrderBy(command => command.StartedAt)
            .ToArray();

        DateTimeOffset? fromUtc = failedCommands.Length == 0
            ? null
            : failedCommands.Min(command => ToUtc(command.StartedAt)).Subtract(request.WindowBefore);
        DateTimeOffset? toUtc = failedCommands.Length == 0
            ? null
            : failedCommands.Max(command => ToUtc(command.CompletedAt ?? command.StartedAt)).Add(request.WindowAfter);

        var commandEvidence = failedCommands
            .Select(command => new FailureLoopCommandEvidence(
                command.Id,
                command.WorkspaceId,
                NormalizeBlank(command.SurfaceId),
                NormalizeBlank(command.PaneId),
                command.Command,
                command.ExitCode!.Value,
                ToUtc(command.StartedAt),
                command.CompletedAt is null ? null : ToUtc(command.CompletedAt.Value),
                command.WorkingDirectory))
            .ToArray();

        var transcriptEvidence = transcripts
            .Where(input => input.Entry is not null)
            .Where(input => MatchesScope(request, input.Entry.WorkspaceId, input.Entry.SurfaceId, input.Entry.PaneId))
            .Where(input => IsWithinWindow(ToUtc(input.Entry.CapturedAt), fromUtc, toUtc))
            .OrderBy(input => input.Entry.CapturedAt)
            .Select(input =>
            {
                var summary = Truncate(input.Summary, request.MaxTranscriptSummaryChars, out var truncated);
                return new FailureLoopTranscriptEvidence(
                    input.Entry.FileName,
                    input.Entry.FilePath,
                    input.Entry.WorkspaceId,
                    NormalizeBlank(input.Entry.SurfaceId),
                    NormalizeBlank(input.Entry.PaneId),
                    ToUtc(input.Entry.CapturedAt),
                    input.Entry.Reason,
                    input.Entry.SizeBytes,
                    summary,
                    truncated);
            })
            .ToArray();

        var daemonLogEvidence = daemonLogs
            .Where(input => IsWithinWindow(input.TimestampUtc.ToUniversalTime(), fromUtc, toUtc))
            .Where(input => MatchesPane(request.PaneId, input.PaneId))
            .OrderBy(input => input.TimestampUtc)
            .Select(input =>
            {
                var line = Truncate(input.Line, request.MaxDaemonLogLineChars, out var truncated);
                return new FailureLoopDaemonLogEvidence(
                    input.TimestampUtc.ToUniversalTime(),
                    line,
                    NormalizeBlank(input.PaneId),
                    truncated);
            })
            .ToArray();

        var agentEvidence = (agentMessages ?? [])
            .Where(input => MatchesScope(request, input.WorkspaceId, input.SurfaceId, input.PaneId))
            .Where(input => IsWithinWindow(input.CapturedAtUtc.ToUniversalTime(), fromUtc, toUtc))
            .OrderBy(input => input.CapturedAtUtc)
            .Select(input => new FailureLoopAgentEvidence(
                input.CapturedAtUtc.ToUniversalTime(),
                NormalizeRole(input.Role),
                Truncate(input.Summary, request.MaxAgentMessageSummaryChars, out _)))
            .ToArray();
        var sources = new[]
        {
            new FailureLoopEvidenceSource(FailureLoopEvidenceSourceKind.CommandLog, "Command log", commandEvidence.Length),
            new FailureLoopEvidenceSource(FailureLoopEvidenceSourceKind.TerminalTranscript, "Terminal transcript", transcriptEvidence.Length),
            new FailureLoopEvidenceSource(FailureLoopEvidenceSourceKind.AgentConversation, "Agent conversation", agentEvidence.Length),
            new FailureLoopEvidenceSource(FailureLoopEvidenceSourceKind.DaemonLog, "Daemon log", daemonLogEvidence.Length),
        };

        return new FailureLoopEvidencePackage(
            request.PackageId ?? Guid.NewGuid().ToString("N"),
            request.WorkspaceId,
            NormalizeBlank(request.SurfaceId),
            NormalizeBlank(request.PaneId),
            request.CapturedAtUtc?.ToUniversalTime() ?? DateTimeOffset.UtcNow,
            fromUtc,
            toUtc,
            sources,
            commandEvidence,
            transcriptEvidence,
            agentEvidence,
            daemonLogEvidence);
    }

    private static bool MatchesScope(FailureLoopEvidenceRequest request, string workspaceId, string? surfaceId, string? paneId)
        => string.Equals(workspaceId, request.WorkspaceId, StringComparison.Ordinal) &&
           MatchesOptional(request.SurfaceId, surfaceId) &&
           MatchesOptional(request.PaneId, paneId);

    private static bool MatchesOptional(string? expected, string? actual)
        => string.IsNullOrWhiteSpace(expected) ||
           string.Equals(expected, actual, StringComparison.Ordinal);

    private static bool MatchesPane(string? requestedPaneId, string? logPaneId)
        => string.IsNullOrWhiteSpace(requestedPaneId) ||
           string.Equals(requestedPaneId, logPaneId, StringComparison.Ordinal);

    private static bool IsWithinWindow(DateTimeOffset timestampUtc, DateTimeOffset? fromUtc, DateTimeOffset? toUtc)
    {
        if (fromUtc is null || toUtc is null)
            return false;

        return timestampUtc >= fromUtc.Value && timestampUtc <= toUtc.Value;
    }

    private static string Truncate(string? value, int maxChars, out bool truncated)
    {
        var text = value ?? string.Empty;
        var safeMax = Math.Max(0, maxChars);
        truncated = text.Length > safeMax;
        return truncated ? text[..safeMax] : text;
    }

    private static DateTimeOffset ToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => new DateTimeOffset(value, TimeSpan.Zero),
            DateTimeKind.Local => new DateTimeOffset(value.ToUniversalTime(), TimeSpan.Zero),
            _ => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc), TimeSpan.Zero),
        };
    }

    private static string? NormalizeBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;

    private static string NormalizeRole(string? role)
        => string.IsNullOrWhiteSpace(role) ? "unknown" : role.Trim().ToLowerInvariant();
}
