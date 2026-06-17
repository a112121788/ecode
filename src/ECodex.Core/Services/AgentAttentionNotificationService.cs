using ECodex.Core.Models;

namespace ECodex.Core.Services;

/// <summary>
/// Turns conservative agent-attention signals into low-noise notifications.
/// </summary>
public sealed class AgentAttentionNotificationService
{
    private static readonly TimeSpan DefaultDuplicateCooldown = TimeSpan.FromSeconds(30);

    private readonly NotificationService _notificationService;
    private readonly Dictionary<string, DateTimeOffset> _recentNotifications = new(StringComparer.Ordinal);
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly TimeSpan _duplicateCooldown;
    private readonly object _lock = new();

    public AgentAttentionNotificationService(
        NotificationService notificationService,
        Func<DateTimeOffset>? utcNow = null,
        TimeSpan? duplicateCooldown = null)
    {
        _notificationService = notificationService;
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
        _duplicateCooldown = duplicateCooldown ?? DefaultDuplicateCooldown;
    }

    public bool TryNotify(
        string? workspaceId,
        string? surfaceId,
        string? paneId,
        AgentAttentionSignal? signal,
        bool isAppForegroundActive)
    {
        if (signal == null || isAppForegroundActive)
            return false;

        if (string.IsNullOrWhiteSpace(workspaceId) ||
            string.IsNullOrWhiteSpace(surfaceId) ||
            string.IsNullOrWhiteSpace(paneId))
        {
            return false;
        }

        var throttleKey = CreateThrottleKey(workspaceId, surfaceId, paneId, signal);
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
            workspaceId,
            surfaceId,
            paneId,
            signal.Title,
            "Codex",
            signal.Summary,
            NotificationSource.AgentAttention);
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

    private static string CreateThrottleKey(
        string workspaceId,
        string surfaceId,
        string paneId,
        AgentAttentionSignal signal)
        => string.Join(
            "|",
            workspaceId,
            surfaceId,
            paneId,
            signal.Kind.ToString(),
            signal.Summary);
}
