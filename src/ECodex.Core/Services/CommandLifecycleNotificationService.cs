using ECodex.Core.Models;

namespace ECodex.Core.Services;

public sealed record CommandLifecycleHookEvent(
    string? Phase,
    string? Command,
    int? ExitCode,
    string? WorkingDirectory,
    string? WorkspaceId,
    string? SurfaceId,
    string? PaneId);

/// <summary>
/// Converts shell hook lifecycle events into low-noise terminal notifications.
/// </summary>
public sealed class CommandLifecycleNotificationService
{
    private readonly NotificationService _notificationService;
    private readonly Dictionary<string, CommandLifecycleHookEvent> _activeCommands = new(StringComparer.Ordinal);
    private readonly object _lock = new();

    public CommandLifecycleNotificationService(NotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public bool HandleHookEvent(CommandLifecycleHookEvent hookEvent, bool isAppForegroundActive)
    {
        if (string.IsNullOrWhiteSpace(hookEvent.WorkspaceId))
            return false;

        var key = CreateKey(hookEvent);
        var phase = hookEvent.Phase?.Trim().ToLowerInvariant();

        if (phase == "start")
        {
            lock (_lock)
                _activeCommands[key] = hookEvent;
            return false;
        }

        if (phase != "end")
            return false;

        CommandLifecycleHookEvent? active = null;
        lock (_lock)
        {
            _activeCommands.Remove(key, out active);
        }

        if (isAppForegroundActive)
            return false;

        var command = FirstNonBlank(hookEvent.Command, active?.Command) ?? "命令";
        var workingDirectory = FirstNonBlank(hookEvent.WorkingDirectory, active?.WorkingDirectory);
        var exitCode = hookEvent.ExitCode ?? 0;
        var failed = exitCode != 0;
        var surfaceId = hookEvent.SurfaceId ?? "";
        var paneId = string.IsNullOrWhiteSpace(hookEvent.PaneId) ? null : hookEvent.PaneId;

        _notificationService.AddNotification(
            hookEvent.WorkspaceId!,
            surfaceId,
            paneId,
            failed ? "命令失败" : "命令已完成",
            workingDirectory,
            failed ? $"{command} (exit {exitCode})" : command,
            NotificationSource.Cli);
        return true;
    }

    private static string CreateKey(CommandLifecycleHookEvent hookEvent)
    {
        if (!string.IsNullOrWhiteSpace(hookEvent.PaneId))
            return $"pane:{hookEvent.PaneId}";

        return string.Join(
            "|",
            hookEvent.WorkspaceId ?? "",
            hookEvent.SurfaceId ?? "",
            hookEvent.WorkingDirectory ?? "");
    }

    private static string? FirstNonBlank(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}
