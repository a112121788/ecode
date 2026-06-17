using ECodex.Core.Models;

namespace ECodex.Core.Services;

public interface IFailureLoopEvidenceSourceProvider
{
    IReadOnlyList<CommandLogEntry> GetCommands(DateOnly date);
    IReadOnlyList<TerminalTranscriptEntry> GetTerminalTranscripts(int maxEntries);
    string LoadTerminalTranscriptContent(string filePath);
    IReadOnlyList<FailureLoopDaemonLogInput> GetDaemonLogs(DateTimeOffset? fromUtc, DateTimeOffset? toUtc, string? paneId);
}

public interface IFailureLoopAgentMessageProvider
{
    IReadOnlyList<FailureLoopAgentMessageInput> GetAgentMessages(
        FailureLoopEvidenceRequest request,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc);
}

public sealed record FailureLoopEvidenceCollectionRequest(
    FailureLoopEvidenceRequest EvidenceRequest,
    DateOnly CommandDate)
{
    public int MaxTranscriptEntries { get; init; } = 2_000;
}

public sealed class FailureLoopEvidenceCollector
{
    private readonly FailureLoopEvidenceAssembler _assembler;

    public FailureLoopEvidenceCollector(FailureLoopEvidenceAssembler? assembler = null)
    {
        _assembler = assembler ?? new FailureLoopEvidenceAssembler();
    }

    public FailureLoopEvidencePackage Collect(
        FailureLoopEvidenceCollectionRequest request,
        IFailureLoopEvidenceSourceProvider provider,
        IFailureLoopAgentMessageProvider? agentMessageProvider = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(request.EvidenceRequest);

        var evidenceRequest = request.EvidenceRequest;
        var commands = provider.GetCommands(request.CommandDate);
        var (fromUtc, toUtc) = GetFailureWindow(evidenceRequest, commands);

        var transcriptInputs = provider.GetTerminalTranscripts(request.MaxTranscriptEntries)
            .Where(entry => MatchesScope(evidenceRequest, entry.WorkspaceId, entry.SurfaceId, entry.PaneId))
            .Where(entry => IsWithinWindow(ToUtc(entry.CapturedAt), fromUtc, toUtc))
            .Select(entry => new FailureLoopTranscriptInput(
                entry,
                provider.LoadTerminalTranscriptContent(entry.FilePath)))
            .ToArray();

        var daemonLogs = provider.GetDaemonLogs(fromUtc, toUtc, evidenceRequest.PaneId);
        var agentMessages = agentMessageProvider?.GetAgentMessages(evidenceRequest, fromUtc, toUtc) ?? [];

        return _assembler.Assemble(evidenceRequest, commands, transcriptInputs, daemonLogs, agentMessages);
    }

    private static (DateTimeOffset? FromUtc, DateTimeOffset? ToUtc) GetFailureWindow(
        FailureLoopEvidenceRequest request,
        IReadOnlyList<CommandLogEntry> commands)
    {
        var failedCommands = commands
            .Where(command => command.ExitCode is not null and not 0)
            .Where(command => MatchesScope(request, command.WorkspaceId, command.SurfaceId, command.PaneId))
            .ToArray();

        if (failedCommands.Length == 0)
            return (null, null);

        var fromUtc = failedCommands.Min(command => ToUtc(command.StartedAt)).Subtract(request.WindowBefore);
        var toUtc = failedCommands.Max(command => ToUtc(command.CompletedAt ?? command.StartedAt)).Add(request.WindowAfter);
        return (fromUtc, toUtc);
    }

    private static bool MatchesScope(FailureLoopEvidenceRequest request, string workspaceId, string? surfaceId, string? paneId)
        => string.Equals(workspaceId, request.WorkspaceId, StringComparison.Ordinal) &&
           MatchesOptional(request.SurfaceId, surfaceId) &&
           MatchesOptional(request.PaneId, paneId);

    private static bool MatchesOptional(string? expected, string? actual)
        => string.IsNullOrWhiteSpace(expected) ||
           string.Equals(expected, actual, StringComparison.Ordinal);

    private static bool IsWithinWindow(DateTimeOffset timestampUtc, DateTimeOffset? fromUtc, DateTimeOffset? toUtc)
    {
        if (fromUtc is null || toUtc is null)
            return false;

        return timestampUtc >= fromUtc.Value && timestampUtc <= toUtc.Value;
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
}

public sealed class CommandLogFailureLoopEvidenceSourceProvider : IFailureLoopEvidenceSourceProvider
{
    private readonly CommandLogService _commandLogService;
    private readonly Func<DateTimeOffset?, DateTimeOffset?, string?, IReadOnlyList<FailureLoopDaemonLogInput>> _daemonLogProvider;

    public CommandLogFailureLoopEvidenceSourceProvider(
        CommandLogService commandLogService,
        Func<DateTimeOffset?, DateTimeOffset?, string?, IReadOnlyList<FailureLoopDaemonLogInput>>? daemonLogProvider = null)
    {
        _commandLogService = commandLogService ?? throw new ArgumentNullException(nameof(commandLogService));
        _daemonLogProvider = daemonLogProvider ?? ((_, _, _) => []);
    }

    public IReadOnlyList<CommandLogEntry> GetCommands(DateOnly date)
        => _commandLogService.GetForDate(date);

    public IReadOnlyList<TerminalTranscriptEntry> GetTerminalTranscripts(int maxEntries)
        => _commandLogService.GetTerminalTranscripts(maxEntries);

    public string LoadTerminalTranscriptContent(string filePath)
        => _commandLogService.LoadTerminalTranscriptContent(filePath);

    public IReadOnlyList<FailureLoopDaemonLogInput> GetDaemonLogs(DateTimeOffset? fromUtc, DateTimeOffset? toUtc, string? paneId)
        => _daemonLogProvider(fromUtc, toUtc, paneId);
}

public sealed class AgentConversationFailureLoopEvidenceProvider : IFailureLoopAgentMessageProvider
{
    private readonly AgentConversationStoreService _store;

    public AgentConversationFailureLoopEvidenceProvider(AgentConversationStoreService store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public IReadOnlyList<FailureLoopAgentMessageInput> GetAgentMessages(
        FailureLoopEvidenceRequest request,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (fromUtc is null || toUtc is null)
            return [];

        return _store.GetThreads()
            .Where(thread => MatchesScope(request, thread.WorkspaceId, thread.SurfaceId, thread.PaneId))
            .SelectMany(thread => _store.GetMessages(thread.Id).Select(message => new { thread, message }))
            .Where(item => IsWithinWindow(item.message.CreatedAtUtc.ToUniversalTime(), fromUtc, toUtc))
            .OrderBy(item => item.message.CreatedAtUtc)
            .Select(item => new FailureLoopAgentMessageInput(
                item.thread.WorkspaceId,
                item.thread.SurfaceId,
                item.thread.PaneId,
                item.message.CreatedAtUtc.ToUniversalTime(),
                item.message.Role,
                item.message.Content))
            .ToList();
    }

    private static bool MatchesScope(FailureLoopEvidenceRequest request, string workspaceId, string? surfaceId, string? paneId)
        => string.Equals(workspaceId, request.WorkspaceId, StringComparison.Ordinal) &&
           MatchesOptional(request.SurfaceId, surfaceId) &&
           MatchesOptional(request.PaneId, paneId);

    private static bool MatchesOptional(string? expected, string? actual)
        => string.IsNullOrWhiteSpace(expected) ||
           string.Equals(expected, actual, StringComparison.Ordinal);

    private static bool IsWithinWindow(DateTimeOffset timestampUtc, DateTimeOffset? fromUtc, DateTimeOffset? toUtc)
    {
        if (fromUtc is null || toUtc is null)
            return false;

        return timestampUtc >= fromUtc.Value && timestampUtc <= toUtc.Value;
    }
}
