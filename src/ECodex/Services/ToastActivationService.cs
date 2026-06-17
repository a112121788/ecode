using ECodex.Core.Models;
using ECodex.Core.Services;

namespace ECodex.Services;

public sealed record ToastActivationResult(
    bool Handled,
    bool RestoredWindow,
    bool Jumped,
    bool FallbackShown,
    string? NotificationId);

public sealed class ToastActivationService
{
    private readonly NotificationService _notificationService;
    private readonly Func<TerminalNotification?, bool> _jumpToNotification;
    private readonly Action _restoreWindow;
    private readonly Action<TerminalNotification?> _showFallback;
    private readonly Action<Action> _dispatchToUi;

    public ToastActivationService(
        NotificationService notificationService,
        Func<TerminalNotification?, bool> jumpToNotification,
        Action restoreWindow,
        Action<TerminalNotification?> showFallback,
        Action<Action>? dispatchToUi = null)
    {
        _notificationService = notificationService;
        _jumpToNotification = jumpToNotification;
        _restoreWindow = restoreWindow;
        _showFallback = showFallback;
        _dispatchToUi = dispatchToUi ?? (action => action());
    }

    public bool HandleActivated(string? arguments)
    {
        if (!ToastActivationParser.TryParse(arguments, out var request))
            return false;

        _dispatchToUi(() => HandleRequest(request!));
        return true;
    }

    public ToastActivationResult HandleRequest(ToastActivationRequest request)
    {
        var notification = _notificationService.Notifications.FirstOrDefault(item =>
            string.Equals(item.Id, request.NotificationId, StringComparison.Ordinal));

        _restoreWindow();

        if (notification == null)
        {
            _showFallback(null);
            return new ToastActivationResult(true, true, false, true, request.NotificationId);
        }

        if (_jumpToNotification(notification))
            return new ToastActivationResult(true, true, true, false, notification.Id);

        _showFallback(notification);
        return new ToastActivationResult(true, true, false, true, notification.Id);
    }
}
