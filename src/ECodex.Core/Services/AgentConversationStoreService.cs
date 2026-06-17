using System.Text;
using System.Text.Json;
using ECodex.Core.Models;

namespace ECodex.Core.Services;

public sealed class AgentConversationStoreService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
    };

    private readonly object _lock = new();
    private readonly Dictionary<string, AgentConversationThread> _threads;

    public AgentConversationStoreService(string agentDirectory)
    {
        if (string.IsNullOrWhiteSpace(agentDirectory))
            throw new ArgumentException("Agent conversation directory is required.", nameof(agentDirectory));

        AgentDirectory = Path.GetFullPath(agentDirectory);
        Directory.CreateDirectory(AgentDirectory);
        Directory.CreateDirectory(MessagesDirectory);
        _threads = LoadThreadIndex()
            .Where(thread => !string.IsNullOrWhiteSpace(thread.Id))
            .ToDictionary(thread => thread.Id, thread => thread, StringComparer.Ordinal);
    }

    public string AgentDirectory { get; }

    private string ThreadsIndexPath => Path.Combine(AgentDirectory, "threads.json");
    private string MessagesDirectory => Path.Combine(AgentDirectory, "messages");

    public AgentConversationThread CreateThread(
        string workspaceId,
        string? surfaceId = null,
        string? paneId = null,
        string? title = null,
        string? threadId = null,
        DateTimeOffset? createdAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(workspaceId))
            throw new ArgumentException("Workspace id is required.", nameof(workspaceId));

        lock (_lock)
        {
            var id = string.IsNullOrWhiteSpace(threadId) ? Guid.NewGuid().ToString("N") : threadId.Trim();
            if (_threads.ContainsKey(id))
                throw new InvalidOperationException($"Agent conversation thread already exists: {id}");

            var now = createdAtUtc ?? DateTimeOffset.UtcNow;
            var thread = new AgentConversationThread
            {
                Id = id,
                WorkspaceId = workspaceId,
                SurfaceId = NormalizeOptional(surfaceId),
                PaneId = NormalizeOptional(paneId),
                Title = string.IsNullOrWhiteSpace(title) ? "Untitled conversation" : title.Trim(),
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };

            _threads[id] = thread;
            PersistThreadsIndex();
            return Clone(thread);
        }
    }

    public AgentConversationThread? GetThread(string threadId)
    {
        if (string.IsNullOrWhiteSpace(threadId))
            return null;

        lock (_lock)
        {
            return _threads.TryGetValue(threadId, out var thread) ? Clone(thread) : null;
        }
    }

    public IReadOnlyList<AgentConversationThread> GetThreads()
    {
        lock (_lock)
        {
            return _threads.Values
                .OrderByDescending(thread => thread.UpdatedAtUtc)
                .ThenBy(thread => thread.Title, StringComparer.OrdinalIgnoreCase)
                .Select(Clone)
                .ToList();
        }
    }

    public IReadOnlyList<AgentConversationThread> SearchThreads(string query)
    {
        lock (_lock)
        {
            var threads = _threads.Values.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.Trim();
                threads = threads.Where(thread =>
                    Contains(thread.Id, q) ||
                    Contains(thread.WorkspaceId, q) ||
                    Contains(thread.SurfaceId, q) ||
                    Contains(thread.PaneId, q) ||
                    Contains(thread.Title, q) ||
                    Contains(thread.LastMessagePreview, q));
            }

            return threads
                .OrderByDescending(thread => thread.UpdatedAtUtc)
                .ThenBy(thread => thread.Title, StringComparer.OrdinalIgnoreCase)
                .Select(Clone)
                .ToList();
        }
    }

    public AgentConversationMessage AppendMessage(string threadId, AgentConversationMessage message)
    {
        if (string.IsNullOrWhiteSpace(threadId))
            throw new ArgumentException("Thread id is required.", nameof(threadId));

        lock (_lock)
        {
            if (!_threads.TryGetValue(threadId, out var thread))
                throw new InvalidOperationException($"Agent conversation thread was not found: {threadId}");

            var normalized = NormalizeMessage(message, threadId);
            var messagePath = GetMessageFilePath(threadId);
            Directory.CreateDirectory(MessagesDirectory);
            File.AppendAllText(messagePath, JsonSerializer.Serialize(normalized, JsonOptions) + Environment.NewLine, Encoding.UTF8);

            var messages = ReadMessagesFromFile(messagePath);
            _threads[threadId] = RecalculateThread(thread, messages);
            PersistThreadsIndex();
            return normalized;
        }
    }

    public IReadOnlyList<AgentConversationMessage> GetMessages(string threadId)
    {
        if (string.IsNullOrWhiteSpace(threadId))
            return [];

        lock (_lock)
        {
            return ReadMessagesFromFile(GetMessageFilePath(threadId))
                .Select(message => message with { })
                .ToList();
        }
    }

    public IReadOnlyList<AgentConversationMessage> ReadMessagesFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return [];

        var bytes = File.ReadAllBytes(filePath);
        if (bytes.Length == 0)
            return [];

        var start = HasUtf8Bom(bytes) ? 3 : 0;
        var reader = new Utf8JsonReader(
            new ReadOnlySpan<byte>(bytes, start, bytes.Length - start),
            new JsonReaderOptions
            {
                AllowMultipleValues = true,
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
            });

        var messages = new List<AgentConversationMessage>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                var message = JsonSerializer.Deserialize<AgentConversationMessage>(ref reader, JsonOptions);
                if (message is not null)
                    messages.Add(NormalizeMessage(message, NormalizeOptional(message.ThreadId)));
            }
            else if (reader.TokenType == JsonTokenType.StartArray)
            {
                var loaded = JsonSerializer.Deserialize<List<AgentConversationMessage>>(ref reader, JsonOptions);
                if (loaded is not null)
                    messages.AddRange(loaded.Select(message => NormalizeMessage(message, NormalizeOptional(message.ThreadId))));
            }
        }

        return messages;
    }

    private IReadOnlyList<AgentConversationThread> LoadThreadIndex()
    {
        if (!File.Exists(ThreadsIndexPath))
            return [];

        try
        {
            var json = File.ReadAllText(ThreadsIndexPath, Encoding.UTF8);
            return JsonSerializer.Deserialize<List<AgentConversationThread>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private void PersistThreadsIndex()
    {
        Directory.CreateDirectory(AgentDirectory);
        var ordered = _threads.Values
            .OrderByDescending(thread => thread.UpdatedAtUtc)
            .ThenBy(thread => thread.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var json = JsonSerializer.Serialize(ordered, JsonOptions);
        var tempPath = ThreadsIndexPath + ".tmp";
        File.WriteAllText(tempPath, json, Encoding.UTF8);
        File.Move(tempPath, ThreadsIndexPath, overwrite: true);
    }

    private string GetMessageFilePath(string threadId)
    {
        return Path.Combine(MessagesDirectory, SanitizeFileNameSegment(threadId) + ".jsonl");
    }

    private static AgentConversationThread RecalculateThread(
        AgentConversationThread thread,
        IReadOnlyList<AgentConversationMessage> messages)
    {
        var lastMessage = messages
            .OrderBy(message => message.CreatedAtUtc)
            .LastOrDefault();
        var updatedAtUtc = lastMessage?.CreatedAtUtc ?? thread.UpdatedAtUtc;

        return thread with
        {
            UpdatedAtUtc = updatedAtUtc,
            MessageCount = messages.Count,
            TotalInputTokens = messages.Sum(message => message.InputTokens),
            TotalOutputTokens = messages.Sum(message => message.OutputTokens),
            TotalTokens = messages.Sum(message => message.TotalTokens),
            CompactionCount = messages.Count(message => message.IsCompaction),
            LastMessagePreview = lastMessage is null ? null : BuildPreview(lastMessage.Content),
        };
    }

    private static AgentConversationMessage NormalizeMessage(AgentConversationMessage message, string? threadId)
    {
        var inputTokens = Math.Max(0, message.InputTokens);
        var outputTokens = Math.Max(0, message.OutputTokens);
        var totalTokens = message.TotalTokens > 0 ? message.TotalTokens : inputTokens + outputTokens;

        return message with
        {
            Id = string.IsNullOrWhiteSpace(message.Id) ? Guid.NewGuid().ToString("N") : message.Id.Trim(),
            ThreadId = string.IsNullOrWhiteSpace(message.ThreadId) ? threadId ?? "" : message.ThreadId.Trim(),
            Role = NormalizeRole(message.Role),
            Content = message.Content ?? "",
            CreatedAtUtc = message.CreatedAtUtc == default ? DateTimeOffset.UtcNow : message.CreatedAtUtc,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            TotalTokens = totalTokens,
        };
    }

    private static AgentConversationThread Clone(AgentConversationThread thread) => thread with { };

    private static string NormalizeRole(string? role)
    {
        return string.IsNullOrWhiteSpace(role) ? "unknown" : role.Trim().ToLowerInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool Contains(string? value, string query)
    {
        return value?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;
    }

    private static string BuildPreview(string content)
    {
        var collapsed = string.Join(' ', content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return collapsed.Length <= 160 ? collapsed : collapsed[..160];
    }

    private static bool HasUtf8Bom(byte[] bytes)
    {
        return bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
    }

    private static string SanitizeFileNameSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
            builder.Append(invalid.Contains(ch) ? '_' : ch);
        return builder.Length == 0 ? Guid.NewGuid().ToString("N") : builder.ToString();
    }
}
