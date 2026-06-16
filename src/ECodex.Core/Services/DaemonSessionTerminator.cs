using ECodex.Core.IPC;

namespace ECodex.Core.Services;

public sealed record DaemonSessionTerminationResult(int Requested, int Terminated);

public static class DaemonSessionTerminator
{
    public static async Task<DaemonSessionTerminationResult> TerminateAllAsync(DaemonClient daemon)
    {
        if (!daemon.IsConnected)
            return new DaemonSessionTerminationResult(0, 0);

        return await TerminateAllAsync(
            daemon.ListSessionsAsync,
            daemon.CloseSessionAsync).ConfigureAwait(false);
    }

    public static async Task<DaemonSessionTerminationResult> TerminateAllAsync(
        Func<Task<List<DaemonSessionInfo>>> listSessions,
        Func<string, Task> closeSession)
    {
        var paneIds = (await listSessions().ConfigureAwait(false))
            .Select(session => session.PaneId)
            .Where(paneId => !string.IsNullOrWhiteSpace(paneId))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var terminated = 0;
        foreach (var paneId in paneIds)
        {
            await closeSession(paneId).ConfigureAwait(false);
            terminated++;
        }

        return new DaemonSessionTerminationResult(paneIds.Count, terminated);
    }
}
