namespace ECodex.Core.Services;

/// <summary>
/// Shared rules for project folders: normalized paths are the identity of a project.
/// </summary>
public static class WorkspaceDirectoryService
{
    public static string? Normalize(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
            return null;

        var trimmed = directory.Trim();
        try
        {
            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(trimmed));
        }
        catch
        {
            return Path.TrimEndingDirectorySeparator(trimmed);
        }
    }

    public static bool AreSame(string? left, string? right)
    {
        var normalizedLeft = Normalize(left);
        var normalizedRight = Normalize(right);
        if (normalizedLeft == null || normalizedRight == null)
            return false;

        return string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsDuplicate(IEnumerable<string?> existingDirectories, string directory)
    {
        return existingDirectories.Any(existing => AreSame(existing, directory));
    }

    public static string GetDefaultWorkspaceName(string directory, string fallback)
    {
        var normalized = Normalize(directory) ?? directory;
        var name = Path.GetFileName(normalized);
        return string.IsNullOrWhiteSpace(name) ? fallback : name;
    }
}
