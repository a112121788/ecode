namespace Cmux.Core.Config;

/// <summary>
/// 应用级设置，提供合理的默认值。
/// 通过 <see cref="SettingsService"/> 与 settings.json 互相序列化。
/// </summary>
public class CmuxSettings
{
    // ── 外观 ──────────────────────────────────────────────

    public string FontFamily { get; set; } = "Cascadia Code";
    public int FontSize { get; set; } = 14;
    public string ThemeName { get; set; } = "Default Dark";
    public bool UseCustomTerminalColors { get; set; } = false;
    public string CustomTerminalBackground { get; set; } = "";
    public string CustomTerminalForeground { get; set; } = "";
    public string CustomTerminalCursor { get; set; } = "";
    public string CustomTerminalSelection { get; set; } = "";
    public double Opacity { get; set; } = 1.0;
    public string CursorStyle { get; set; } = "bar"; // bar | block | underline
    public bool CursorBlink { get; set; } = true;
    public int CursorBlinkMs { get; set; } = 530;
    public double LineHeight { get; set; } = 1.0;
    public int Padding { get; set; } = 0;

    // ── 终端 ────────────────────────────────────────────────

    public string DefaultShell { get; set; } = "";
    public string DefaultShellArgs { get; set; } = "";
    public int ScrollbackLines { get; set; } = 10_000;
    public bool BellSound { get; set; } = false;
    public bool VisualBell { get; set; } = true;
    public bool BracketedPaste { get; set; } = true;
    public string WordSeparators { get; set; } = " \t\n{}[]()\"'`,:;<>";

    // ── 行为 ────────────────────────────────────────────────

    public bool RestoreSessionOnStartup { get; set; } = true;
    public bool ConfirmOnClose { get; set; } = true;
    public bool AutoCopyOnSelect { get; set; } = false;
    public bool CtrlClickOpensUrls { get; set; } = true;
    public int AutoSaveIntervalSeconds { get; set; } = 30;
    public bool CaptureTranscriptsOnClose { get; set; } = true;
    public bool CaptureTranscriptsOnClear { get; set; } = true;
    // 0 = 永久保留日志（不清理）
    public int CommandLogRetentionDays { get; set; } = 90;
    // 0 = 永久保留捕获内容（不清理）
    public int TranscriptRetentionDays { get; set; } = 90;

    // ── 集合 ─────────────────────────────────────────────

    public List<ShellProfile> ShellProfiles { get; set; } = [];
    public Dictionary<string, string> KeyBindings { get; set; } = [];
    public List<string> RecentDirectories { get; set; } = [];
    public AgentSettings Agent { get; set; } = new();
}

/// <summary>
/// 用于启动终端会话的命名 Shell 配置。
/// </summary>
public class ShellProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Default";
    public string Command { get; set; } = "";
    public string Arguments { get; set; } = "";
    public string WorkingDirectory { get; set; } = "";
    public Dictionary<string, string> Environment { get; set; } = [];
    public string? ThemeOverride { get; set; }
    public bool IsDefault { get; set; }
}
