using ECodex.Core.Models;

namespace ECodex.Core.Services;

public sealed class AgentConversationRecorder
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.Ordinal)
    {
        "user",
        "assistant",
        "tool",
        "system",
    };

    private readonly AgentConversationStoreService _store;

    public AgentConversationRecorder(AgentConversationStoreService store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public AgentConversationRecordingResult StartThread(AgentConversationThreadRecordingInput? input)
    {
        if (input is null)
            return AgentConversationRecordingResult.Failure("Agent conversation thread input is required.");

        try
        {
            var thread = _store.CreateThread(
                input.WorkspaceId,
                input.SurfaceId,
                input.PaneId,
                input.Title,
                input.ThreadId,
                input.CreatedAtUtc);
            return AgentConversationRecordingResult.ForThread(thread);
        }
        catch (Exception ex)
        {
            return AgentConversationRecordingResult.Failure(ex.Message);
        }
    }

    public AgentConversationRecordingResult AppendMessage(AgentConversationMessageRecordingInput? input)
    {
        if (input is null)
            return AgentConversationRecordingResult.Failure("Agent conversation message input is required.");

        var role = NormalizeRole(input.Role);
        if (!AllowedRoles.Contains(role))
            return AgentConversationRecordingResult.Failure("Agent conversation role must be one of: user, assistant, tool, system.");

        if (input.IsCompaction && role is not ("system" or "assistant"))
            return AgentConversationRecordingResult.Failure("Agent conversation compaction messages must use system or assistant role.");

        try
        {
            var message = _store.AppendMessage(
                input.ThreadId,
                new AgentConversationMessage
                {
                    Id = input.MessageId ?? "",
                    Role = role,
                    Content = input.Content ?? "",
                    CreatedAtUtc = input.CreatedAtUtc ?? default,
                    InputTokens = Math.Max(0, input.InputTokens ?? 0),
                    OutputTokens = Math.Max(0, input.OutputTokens ?? 0),
                    TotalTokens = Math.Max(0, input.TotalTokens ?? 0),
                    IsCompaction = input.IsCompaction,
                });
            return AgentConversationRecordingResult.ForMessage(message);
        }
        catch (Exception ex)
        {
            return AgentConversationRecordingResult.Failure(ex.Message);
        }
    }

    private static string NormalizeRole(string? role)
        => string.IsNullOrWhiteSpace(role) ? "" : role.Trim().ToLowerInvariant();
}
