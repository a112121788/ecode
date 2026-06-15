# CLI 参考

`ecode` CLI 可执行本地命令、连接主应用 pipe、发送 `ecode.v2` 请求，并提供 setup、update、doctor、completion 等运维入口。

## 全局参数

```powershell
ecode --json status
ecode --id-format refs status
ecode --id-format uuids status
ecode --id-format both status
```

| 参数 | 说明 |
|---|---|
| `--json` | 输出 JSON。 |
| `--id-format refs|uuids|both` | 控制输出中的短引用与 UUID。human 默认 refs，JSON 默认 both。 |
| `--help` / `-h` | 帮助。 |
| `--version` / `-v` | 版本。 |

## 本地命令

这些命令不需要主应用 pipe：

```powershell
ecode version
ecode help
ecode doctor
ecode setup status
ecode profile import --dry-run
ecode completion powershell
```

## v1 兼容

兼容命令会发送旧式 pipe 命令：

| 命令 | 说明 |
|---|---|
| `ecode status` | 查看主应用状态。 |
| `ecode notify --title T --body B` | 发送通知。 |
| `ecode split right|down` | 分屏。 |
| `ecode reload-config` | 重载 `ecode.json`。 |
| `ecode restore-session` | 刷新恢复绑定并定位可恢复 Pane。 |

## `ecode.v2` 命令组

### Window 命令

```powershell
ecode window list
ecode window current
ecode window focus window:1
ecode window create
ecode window close window:1
```

对应方法：`window.list`、`window.current`、`window.focus`、`window.create`、`window.close`。

### Workspace 命令

```powershell
ecode workspace list
ecode workspace create --name demo
ecode workspace select workspace:1
ecode workspace rename workspace:1 demo-app
ecode workspace reorder workspace:2 0
ecode workspace close workspace:1
```

对应方法：`workspace.list`、`workspace.create`、`workspace.select`、`workspace.close`、`workspace.rename`、`workspace.reorder`。

### Surface 命令

```powershell
ecode surface move surface:1 workspace:2
ecode surface reorder surface:2 0
ecode surface resume set --pane pane:1 --kind tmux --command "tmux attach -t demo"
ecode surface resume show
ecode surface resume clear --pane pane:1
```

对应方法：`surface.move`、`surface.reorder` 与 resume binding 兼容命令。

### Pane 命令

```powershell
ecode pane list
ecode pane focus pane:1
ecode pane write pane:1 "npm test"
ecode pane read pane:1
ecode pane split right
ecode pane resize pane:1 0.05
ecode pane swap pane:1 pane:2
ecode pane zoom pane:1
ecode pane close pane:1
```

对应方法：`pane.list`、`pane.focus`、`pane.write`、`pane.read`、`pane.split`、`pane.close`、`pane.resize`、`pane.swap`、`pane.zoom`。

### Notification 命令

```powershell
ecode notification list
ecode notification read notification:1
ecode notification unread notification:1
ecode notification jump-latest
ecode notification clear
```

对应方法：`notification.list`、`notification.read`、`notification.unread`、`notification.jump-latest`、`notification.clear`。

### Config / Status / Health 命令

```powershell
ecode config reload
ecode config diagnostics
ecode status
ecode health
```

对应方法：`config.reload`、`config.diagnostics`、`status`、`health`。

## Browser 命令

```powershell
ecode browser open https://example.com
ecode browser new https://example.com
ecode browser open-split https://example.com --direction right
ecode browser snapshot --surfaceRef surface:1
ecode browser click --role button --name Submit
ecode browser fill --testid email --value user@example.com
ecode browser hover --text Help
ecode browser press --key Enter
ecode browser eval "document.title"
ecode browser screenshot --path .\browser.png
```

详情见 [Browser API](./browser-api.md)。

## Setup 命令

```powershell
ecode setup status
ecode setup install --write true
ecode setup uninstall --write true
```

setup 可规划并应用 PATH、PowerShell profile、cmd AutoRun 集成，支持 dry-run diff。

## Update 命令

```powershell
ecode update check --feed https://example.com/ecode/
ecode update install --feed https://example.com/ecode/
```

更新命令读取 Velopack `RELEASES` feed。

## Completion 命令

```powershell
ecode completion powershell > $PROFILE.CurrentUserAllHosts
```

补全覆盖顶层命令、子命令、常用参数和短引用前缀。

## 常见错误

如果看到 `Error: Could not connect to ecode. Is it running?`，请先启动 ECode 主应用。只读本地文件的命令（如 `doctor`、`setup status`、`version`、`completion powershell`）不需要主应用 pipe。
