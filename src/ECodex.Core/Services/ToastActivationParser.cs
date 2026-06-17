using ECodex.Core.Models;

namespace ECodex.Core.Services;

public sealed record ToastActivationRequest(
    string NotificationId,
    string WorkspaceId,
    string SurfaceId,
    string? PaneId);

public static class ToastActivationParser
{
    public const string JumpToNotificationAction = "jumpToNotification";

    public static IReadOnlyDictionary<string, string> BuildArguments(TerminalNotification notification)
        => new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["action"] = JumpToNotificationAction,
            ["notificationId"] = notification.Id,
            ["workspaceId"] = notification.WorkspaceId,
            ["surfaceId"] = notification.SurfaceId,
            ["paneId"] = notification.PaneId ?? "",
        };

    public static bool TryParse(string? argumentText, out ToastActivationRequest? request)
        => TryParse(ParseArguments(argumentText), out request);

    public static bool TryParse(IReadOnlyDictionary<string, string> arguments, out ToastActivationRequest? request)
    {
        request = null;

        if (!arguments.TryGetValue("action", out var action) ||
            !string.Equals(action, JumpToNotificationAction, StringComparison.Ordinal))
        {
            return false;
        }

        if (!TryGetRequired(arguments, "notificationId", out var notificationId) ||
            !TryGetRequired(arguments, "workspaceId", out var workspaceId) ||
            !TryGetRequired(arguments, "surfaceId", out var surfaceId))
        {
            return false;
        }

        arguments.TryGetValue("paneId", out var paneId);
        request = new ToastActivationRequest(
            notificationId,
            workspaceId,
            surfaceId,
            string.IsNullOrWhiteSpace(paneId) ? null : paneId);
        return true;
    }

    private static Dictionary<string, string> ParseArguments(string? argumentText)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(argumentText))
            return result;

        foreach (var segment in argumentText.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = segment.IndexOf('=');
            var rawKey = separator >= 0 ? segment[..separator] : segment;
            if (string.IsNullOrWhiteSpace(rawKey))
                continue;

            var rawValue = separator >= 0 ? segment[(separator + 1)..] : "";
            result[Unescape(rawKey)] = Unescape(rawValue);
        }

        return result;
    }

    private static bool TryGetRequired(IReadOnlyDictionary<string, string> arguments, string key, out string value)
    {
        if (arguments.TryGetValue(key, out value!) && !string.IsNullOrWhiteSpace(value))
            return true;

        value = "";
        return false;
    }

    private static string Unescape(string value)
    {
        try
        {
            return Uri.UnescapeDataString(value.Replace('+', ' '));
        }
        catch (UriFormatException)
        {
            return value;
        }
    }
}
