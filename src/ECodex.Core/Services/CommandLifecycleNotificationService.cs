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
    private static readonly TimeSpan DefaultDuplicateCooldown = TimeSpan.FromSeconds(30);

    private readonly NotificationService _notificationService;
    private readonly Dictionary<string, CommandLifecycleHookEvent> _activeCommands = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DateTimeOffset> _recentNotifications = new(StringComparer.Ordinal);
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly TimeSpan _duplicateCooldown;
    private readonly object _lock = new();

    public CommandLifecycleNotificationService(
        NotificationService notificationService,
        Func<DateTimeOffset>? utcNow = null,
        TimeSpan? duplicateCooldown = null)
    {
        _notificationService = notificationService;
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
        _duplicateCooldown = duplicateCooldown ?? DefaultDuplicateCooldown;
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
        var throttleKey = CreateThrottleKey(hookEvent.WorkspaceId!, surfaceId, paneId, command, exitCode);
        var now = _utcNow();

        lock (_lock)
        {
            PruneRecentNotifications(now);
            if (_recentNotifications.TryGetValue(throttleKey, out var lastSeen) &&
                now - lastSeen < _duplicateCooldown)
            {
                return false;
            }

            _recentNotifications[throttleKey] = now;
        }

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

    private void PruneRecentNotifications(DateTimeOffset now)
    {
        foreach (var key in _recentNotifications
                     .Where(entry => now - entry.Value >= _duplicateCooldown)
                     .Select(entry => entry.Key)
                     .ToList())
        {
            _recentNotifications.Remove(key);
        }
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

    private static string CreateThrottleKey(string workspaceId, string surfaceId, string? paneId, string command, int exitCode)
        => string.Join(
            "|",
            workspaceId,
            surfaceId,
            paneId ?? "",
            command,
            exitCode.ToString(System.Globalization.CultureInfo.InvariantCulture));

    private static string? FirstNonBlank(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}
