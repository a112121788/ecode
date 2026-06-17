namespace ECodex.Core.Models;

public sealed record AgentConversationThreadRecordingInput
{
    public string WorkspaceId { get; init; } = "";
    public string? SurfaceId { get; init; }
    public string? PaneId { get; init; }
    public string? Title { get; init; }
    public string? ThreadId { get; init; }
    public DateTimeOffset? CreatedAtUtc { get; init; }
}

public sealed record AgentConversationMessageRecordingInput
{
    public string ThreadId { get; init; } = "";
    public string Role { get; init; } = "";
    public string Content { get; init; } = "";
    public string? MessageId { get; init; }
    public DateTimeOffset? CreatedAtUtc { get; init; }
    public int? InputTokens { get; init; }
    public int? OutputTokens { get; init; }
    public int? TotalTokens { get; init; }
    public bool IsCompaction { get; init; }
}

public sealed record AgentConversationRecordingResult
{
    public bool Succeeded { get; init; }
    public AgentConversationThread? Thread { get; init; }
    public AgentConversationMessage? Message { get; init; }
    public string? ErrorMessage { get; init; }

    public static AgentConversationRecordingResult ForThread(AgentConversationThread thread)
        => new()
        {
            Succeeded = true,
            Thread = thread,
        };

    public static AgentConversationRecordingResult ForMessage(AgentConversationMessage message)
        => new()
        {
            Succeeded = true,
            Message = message,
        };

    public static AgentConversationRecordingResult Failure(string message)
        => new()
        {
            Succeeded = false,
            ErrorMessage = string.IsNullOrWhiteSpace(message) ? "Agent conversation recording failed." : message,
        };
}
