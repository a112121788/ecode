using Microsoft.Toolkit.Uwp.Notifications;
using ECodex.Core.Models;
using ECodex.Core.Services;

namespace ECodex.Services;

/// <summary>
/// 当 AI 编码代理需要关注时发送 Windows Toast 通知。
/// 通过 Microsoft.Toolkit.Uwp.Notifications 使用 Windows 10/11 通知系统。
/// </summary>
public static class ToastNotificationHelper
{
    public static void RegisterActivationHandler(Action<string?> handleActivation)
    {
        try
        {
            ToastNotificationManagerCompat.OnActivated += args => handleActivation(args.Argument);
        }
        catch
        {
            // 非打包 WPF / 系统策略可能不支持 Toast activation；通知中心仍可用。
        }
    }

    /// <summary>
    /// 为终端通知显示 Windows Toast 通知。
    /// </summary>
    public static void ShowToast(TerminalNotification notification, string workspaceName)
    {
        try
        {
            var builder = new ToastContentBuilder()
                .AddText(notification.Title)
                .AddText(notification.Body)
                .AddAttributionText($"项目：{workspaceName}");

            foreach (var argument in ToastActivationParser.BuildArguments(notification))
                builder.AddArgument(argument.Key, argument.Value);

            builder.Show();
        }
        catch
        {
            // Toast 通知在某些环境下可能失败
            // （无 UWP 支持、沙盒等）。不关键。
        }
    }

    /// <summary>
    /// 从通知中心清除所有 ecodex Toast 通知。
    /// </summary>
    public static void ClearAll()
    {
        try
        {
            ToastNotificationManagerCompat.History.Clear();
        }
        catch
        {
            // 尽力而为
        }
    }
}
