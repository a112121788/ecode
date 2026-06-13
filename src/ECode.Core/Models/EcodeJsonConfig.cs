namespace ECode.Core.Models;

public sealed class EcodeJsonConfig
{
    public Dictionary<string, EcodeAction> Actions { get; set; } = [];

    public List<EcodeCommand> Commands { get; set; } = [];

    public EcodeUiConfig? Ui { get; set; }
}

public sealed class EcodeCommand
{
    public string Name { get; set; } = "";

    public string? Description { get; set; }

    public List<string> Keywords { get; set; } = [];

    public string Command { get; set; } = "";

    public string Target { get; set; } = EcodeActionTargets.CurrentTerminal;

    public bool Confirm { get; set; }
}

public sealed class EcodeAction
{
    public string Type { get; set; } = "command";

    public string Title { get; set; } = "";

    public string? Subtitle { get; set; }

    public string? Command { get; set; }

    public string Target { get; set; } = EcodeActionTargets.CurrentTerminal;

    public bool Palette { get; set; } = true;

    public bool Confirm { get; set; }
}

public sealed class EcodeUiConfig
{
    public EcodeSurfaceTabBarConfig? SurfaceTabBar { get; set; }
}

public sealed class EcodeSurfaceTabBarConfig
{
    public List<EcodeUiButton> Buttons { get; set; } = [];
}

public sealed class EcodeUiButton
{
    public string Title { get; set; } = "";

    public string? Icon { get; set; }

    public string Action { get; set; } = "";
}

public static class EcodeActionTargets
{
    public const string CurrentTerminal = "currentTerminal";
    public const string NewTabInCurrentPane = "newTabInCurrentPane";
}

public enum EcodeJsonDiagnosticSeverity
{
    Warning,
    Error,
}

public sealed record EcodeJsonDiagnostic(
    EcodeJsonDiagnosticSeverity Severity,
    string Path,
    string Message);

public sealed class EcodeJsonLoadResult
{
    public EcodeJsonConfig Config { get; init; } = new();

    public List<EcodeJsonDiagnostic> Diagnostics { get; init; } = [];

    public List<string> LoadedPaths { get; init; } = [];

    public bool HasErrors => Diagnostics.Any(d => d.Severity == EcodeJsonDiagnosticSeverity.Error);
}
