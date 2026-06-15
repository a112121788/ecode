namespace ECodex.Core.Models;

/// <summary>
/// ecodex.json 配置文件根对象 - 定义命令、动作和 UI 配置
/// </summary>
public sealed class ECodexJsonConfig
{
    public Dictionary<string, ECodexAction> Actions { get; set; } = []; // 可绑定的动作（快捷键/按钮触发）
    public List<ECodexCommand> Commands { get; set; } = []; // 命令面板中的命令列表
    public ECodexWorkspaceConfig? Workspace { get; set; } // 工作区布局配置
    public ECodexUiConfig? Ui { get; set; }
}

/// <summary>
/// 命令定义 - 在命令面板中可搜索和执行的命令
/// </summary>
public sealed class ECodexCommand
{
    public string Name { get; set; } = "";

    public string? Description { get; set; }

    public List<string> Keywords { get; set; } = [];

    public string Command { get; set; } = "";

    public string Target { get; set; } = ECodexActionTargets.CurrentTerminal;

    public bool Confirm { get; set; }
}

public sealed class ECodexAction
{
    public string Type { get; set; } = "command";

    public string Title { get; set; } = "";

    public string? Subtitle { get; set; }

    public string? Command { get; set; }

    public string Target { get; set; } = ECodexActionTargets.CurrentTerminal;

    public bool Palette { get; set; } = true;

    public bool Confirm { get; set; }
}

public sealed class ECodexUiConfig
{
    public ECodexSurfaceTabBarConfig? SurfaceTabBar { get; set; }
}

public sealed class ECodexWorkspaceConfig
{
    public List<ECodexSurfaceConfig> Surfaces { get; set; } = [];

    public int? SelectedSurfaceIndex { get; set; }
}

public sealed class ECodexSurfaceConfig
{
    public string Type { get; set; } = ECodexSurfaceTypes.Terminal;

    public string? Name { get; set; }

    public string? Url { get; set; }
}

public sealed class ECodexSurfaceTabBarConfig
{
    public List<ECodexUiButton> Buttons { get; set; } = [];
}

public sealed class ECodexUiButton
{
    public string Title { get; set; } = "";

    public string? Icon { get; set; }

    public string Action { get; set; } = "";
}

public static class ECodexActionTargets
{
    public const string CurrentTerminal = "currentTerminal";
    public const string NewTabInCurrentPane = "newTabInCurrentPane";
}

public static class ECodexSurfaceTypes
{
    public const string Terminal = "terminal";
    public const string Browser = "browser";
}

public enum ECodexJsonDiagnosticSeverity
{
    Warning,
    Error,
}

public sealed record ECodexJsonDiagnostic(
    ECodexJsonDiagnosticSeverity Severity,
    string Path,
    string Message);

public sealed class ECodexJsonLoadResult
{
    public ECodexJsonConfig Config { get; init; } = new();

    public List<ECodexJsonDiagnostic> Diagnostics { get; init; } = [];

    public List<string> LoadedPaths { get; init; } = [];

    public bool HasErrors => Diagnostics.Any(d => d.Severity == ECodexJsonDiagnosticSeverity.Error);
}
