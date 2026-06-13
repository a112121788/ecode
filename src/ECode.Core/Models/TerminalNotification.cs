using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ECode.Core.Models;

public enum NotificationSource
{
    Osc9,
    Osc99,
    Osc777,
    Cli,
}

public record TerminalNotification : INotifyPropertyChanged
{
    private bool _isRead;

    public string Id { get; init; } = Guid.NewGuid().ToString();
    public required string WorkspaceId { get; init; }
    public required string SurfaceId { get; init; }
    public string? PaneId { get; init; }
    public bool IsRead
    {
        get => _isRead;
        set
        {
            if (_isRead == value)
                return;

            _isRead = value;
            OnPropertyChanged();
        }
    }
    public required string Title { get; init; }
    public string? Subtitle { get; init; }
    public required string Body { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public NotificationSource Source { get; init; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public record AppNotification
{
    public required string WorkspaceName { get; init; }
    public required string SurfaceName { get; init; }
    public required TerminalNotification Notification { get; init; }
}
