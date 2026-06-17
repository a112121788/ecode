namespace ECodex.Core.Models;

public enum FailureLoopEvidenceSourceKind
{
    CommandLog,
    TerminalTranscript,
    AgentConversation,
    DaemonLog,
}

public sealed record FailureLoopEvidencePackage(
    string PackageId,
    string WorkspaceId,
    string? SurfaceId,
    string? PaneId,
    DateTimeOffset CapturedAtUtc,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    IReadOnlyList<FailureLoopEvidenceSource> Sources,
    IReadOnlyList<FailureLoopCommandEvidence> Commands,
    IReadOnlyList<FailureLoopTranscriptEvidence> Transcripts,
    IReadOnlyList<FailureLoopAgentEvidence> AgentMessages,
    IReadOnlyList<FailureLoopDaemonLogEvidence> DaemonLogs);

public sealed record FailureLoopEvidenceSource(
    FailureLoopEvidenceSourceKind Kind,
    string DisplayName,
    int Count);

public sealed record FailureLoopCommandEvidence(
    string Id,
    string WorkspaceId,
    string? SurfaceId,
    string? PaneId,
    string? Command,
    int ExitCode,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string? WorkingDirectory);

public sealed record FailureLoopTranscriptEvidence(
    string FileName,
    string FilePath,
    string WorkspaceId,
    string? SurfaceId,
    string? PaneId,
    DateTimeOffset CapturedAtUtc,
    string Reason,
    long SizeBytes,
    string Summary,
    bool SummaryWasTruncated);

public sealed record FailureLoopAgentEvidence(
    DateTimeOffset CapturedAtUtc,
    string Role,
    string Summary);

public sealed record FailureLoopDaemonLogEvidence(
    DateTimeOffset TimestampUtc,
    string Line,
    string? PaneId,
    bool LineWasTruncated);
