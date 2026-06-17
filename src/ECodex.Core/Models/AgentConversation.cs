namespace ECodex.Core.Models;

public sealed record AgentConversationThread
{
    public string Id { get; init; } = "";
    public string WorkspaceId { get; init; } = "";
    public string? SurfaceId { get; init; }
    public string? PaneId { get; init; }
    public string Title { get; init; } = "";
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }
    public int MessageCount { get; init; }
    public int TotalInputTokens { get; init; }
    public int TotalOutputTokens { get; init; }
    public int TotalTokens { get; init; }
    public int CompactionCount { get; init; }
    public string? LastMessagePreview { get; init; }
}

public sealed record AgentConversationMessage
{
    public string Id { get; init; } = "";
    public string ThreadId { get; init; } = "";
    public string Role { get; init; } = "";
    public string Content { get; init; } = "";
    public DateTimeOffset CreatedAtUtc { get; init; }
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public int TotalTokens { get; init; }
    public bool IsCompaction { get; init; }
}
