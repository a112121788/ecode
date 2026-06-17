namespace ECodex.Core.Terminal;

/// <summary>
/// Builds the environment passed to shell processes started by ecodex.
/// </summary>
public static class TerminalEnvironmentVariables
{
    public const string WorkspaceId = "ECODEX_WORKSPACE_ID";
    public const string SurfaceId = "ECODEX_SURFACE_ID";
    public const string PaneId = "ECODEX_PANE_ID";

    public static Dictionary<string, string> ForWorkspace(string? workspaceId)
        => ForPane(workspaceId, surfaceId: null, paneId: null);

    public static Dictionary<string, string> ForPane(string? workspaceId, string? surfaceId, string? paneId)
    {
        var environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(workspaceId))
            environment[WorkspaceId] = workspaceId;
        if (!string.IsNullOrWhiteSpace(surfaceId))
            environment[SurfaceId] = surfaceId;
        if (!string.IsNullOrWhiteSpace(paneId))
            environment[PaneId] = paneId;

        if (environment.Count == 0)
            return [];

        return environment;
    }

    public static SortedDictionary<string, string> MergeWithCurrent(IReadOnlyDictionary<string, string>? overrides)
    {
        var merged = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            var key = entry.Key?.ToString();
            if (IsValidName(key))
                merged[key!] = entry.Value?.ToString() ?? "";
        }

        if (overrides != null)
        {
            foreach (var (key, value) in overrides)
            {
                if (IsValidName(key))
                    merged[key] = value;
            }
        }

        return merged;
    }

    private static bool IsValidName(string? name)
    {
        return !string.IsNullOrWhiteSpace(name) &&
               !name.Contains('=', StringComparison.Ordinal);
    }
}
