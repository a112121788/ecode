# CLI Reference

`ecode` is the Windows CLI for controlling a running ECode app, managing local setup, and automating Browser and terminal workflows.

```powershell
ecode [--json] [--id-format refs|uuids|both] <command> [options]
```

## Global Options

| Option | Description |
|---|---|
| `--json` | Print raw JSON. Also defaults `--id-format` to `both`. |
| `--id-format refs` | Return short refs such as `workspace:1`, `surface:2`, and `pane:1`. Default for human output. |
| `--id-format uuids` | Return UUIDs only where an API supports id formatting. |
| `--id-format both` | Return both refs and UUIDs. Default with `--json`. |

Arguments are parsed as:

- `--key value`
- `--key=value` for global options that support it
- `--flag` as `--flag true`
- `-k value`
- positional args as `_arg0`, `_arg1`, and so on

Most commands pretty-print JSON for humans. Use `--json` for scripts.

## Transport Layers

ECode has both legacy/v1 pipe commands and `ecode.v2` methods.

| Layer | Used By | Notes |
|---|---|---|
| v1 compatibility pipe | `notify`, `surface create/next/previous`, `surface resume`, `split`, `browser`, `reload-config`, `restore-session`, `status` | Sends named commands such as `BROWSER.OPEN` or `SURFACE.RESUME.SET` to `\\.\pipe\ecode`. |
| `ecode.v2` | `notification`, `window`, most `workspace`, `surface move/reorder`, `pane`, `config`, `health` | Sends JSON requests with stable method names such as `pane.write`. |
| Local-only | `setup`, `profile`, `update`, `doctor`, `completion`, `version`, `help` | Runs in the CLI process; some subcommands still contact the app or network. |

Compatibility note: when the compatibility setting is enabled, the CLI accepts a legacy top-level `cmux ...` invocation and routes it through `ecode ...`.

## Notifications

### `notify`

Sends a simple app notification through the v1 pipe.

```powershell
ecode notify --title Build --body "Tests are waiting" --subtitle CI
```

| Option | Default | Description |
|---|---|---|
| `--title <text>` | `Terminal` | Notification title. |
| `--body <text>` | Empty | Notification body. |
| `--subtitle <text>` | Empty | Optional subtitle. |

### `notification`

Manages notifications through `ecode.v2`.

| Command | Method | Example |
|---|---|---|
| `ecode notification list` | `notification.list` | `ecode notification list --unread true` |
| `ecode notification read <id>` | `notification.read` | `ecode notification read notification-id` |
| `ecode notification read --all true` | `notification.read` | Mark all as read. |
| `ecode notification unread <id>` | `notification.unread` | Mark one as unread. |
| `ecode notification jump-latest` | `notification.jump-latest` | Focus latest unread notification. |
| `ecode notification clear` | `notification.clear` | Remove all notifications. |

Aliases: `notifications`, `list`/`ls`, `jump-latest`/`jump`, `--notification-id`, `--workspace`, `--surface`, and `--pane`.

## Windows

Window commands use `ecode.v2`.

| Command | Method | Description |
|---|---|---|
| `ecode window list` | `window.list` | List open app windows. |
| `ecode window current` | `window.current` | Show the current window. |
| `ecode window focus <ref/id>` | `window.focus` | Focus a window such as `window:1`. |
| `ecode window create [title]` | `window.create` | Create a new window. |
| `ecode window close <ref/id>` | `window.close` | Close a target window. |

## Workspaces

Workspaces are top-level projects. Most commands use `ecode.v2`; `next` and `previous` use the v1 compatibility pipe.

| Command | Method/Command | Description |
|---|---|---|
| `ecode workspace list` | `workspace.list` | List workspaces. |
| `ecode workspace create --name <name>` | `workspace.create` | Create a workspace. |
| `ecode workspace select <ref/id/name>` | `workspace.select` | Select a workspace. |
| `ecode workspace close [ref/id/name]` | `workspace.close` | Close selected or target workspace. |
| `ecode workspace rename <ref/id> <name>` | `workspace.rename` | Rename a workspace. |
| `ecode workspace reorder <order>` | `workspace.reorder` | Reorder, for example `workspace:2,workspace:1`. |
| `ecode workspace next` | `WORKSPACE.NEXT` | Switch to next workspace. |
| `ecode workspace previous` | `WORKSPACE.PREVIOUS` | Switch to previous workspace. |

Useful aliases:

- `workspace create` also accepts `new`.
- `workspace list` also accepts `ls`.
- `workspace previous` also accepts `prev`.
- `--workspace-ref` maps to the target selector.

## Surfaces

Surfaces are tabs inside a workspace.

| Command | Method/Command | Description |
|---|---|---|
| `ecode surface create` | `SURFACE.CREATE` | Create a new terminal surface. |
| `ecode surface move <ref/id> <index>` | `surface.move` | Move a surface to an index. |
| `ecode surface reorder <order>` | `surface.reorder` | Reorder surfaces, for example `surface:2,surface:1`. |
| `ecode surface next` | `SURFACE.NEXT` | Switch to next surface. |
| `ecode surface previous` | `SURFACE.PREVIOUS` | Switch to previous surface. |

Aliases: `surface create` also accepts `new`, `previous` accepts `prev`, `--surface-ref` maps to the target selector, and `--workspace-ref` selects the owning workspace.

### Resume Bindings

Resume commands use the v1 pipe and write to `%USERPROFILE%\.ecode\resume.json`.

| Command | Description |
|---|---|
| `ecode surface resume show` | Show binding for the focused/selected pane. |
| `ecode surface resume show --all` | Show every binding in the selected surface. |
| `ecode surface resume set --shell <cmd>` | Save or replace the current pane binding. |
| `ecode surface resume clear` | Clear the focused/selected pane binding. |
| `ecode surface resume clear --id <binding-id>` | Clear one binding by id. |

Common options: `--kind tmux/custom`, `--checkpoint <id>`, `--cwd <path>`, `--trusted true`, `--approvedPrefix <prefix>`, `--workspace <name>`, `--surface <name>`, `--paneId <id>`, `--paneName <name>`, and `--paneIndex <n>`.

See [Session Restore](./session-restore.md) for the data model and trust rules.

## Panes

Pane commands use `ecode.v2` and target panes within the selected or specified surface.

| Command | Method | Description |
|---|---|---|
| `ecode pane list` | `pane.list` | List panes in a surface. |
| `ecode pane focus <ref/id>` | `pane.focus` | Focus a pane. |
| `ecode pane write <text>` | `pane.write` | Write text to a pane. |
| `ecode pane read [ref/id]` | `pane.read` | Read recent pane output. |
| `ecode pane split [right/down]` | `pane.split` | Split the focused pane. |
| `ecode pane close [ref/id]` | `pane.close` | Close a pane. |
| `ecode pane resize <ref/id> <delta>` | `pane.resize` | Resize nearest split ratio by delta. |
| `ecode pane swap <a> <b>` | `pane.swap` | Swap two panes. |
| `ecode pane zoom [true|false]` | `pane.zoom` | Set or toggle zoom. |

Common options:

| Option | Description |
|---|---|
| `--submit true` | For `pane.write`, send Enter after writing. |
| `--submitKey enter/linefeed/crlf/none/auto` | Submit sequence for `pane.write`. |
| `--lines <n>` | For `pane.read`, number of tail lines. |
| `--maxChars <n>` | For `pane.read`, maximum returned characters. |
| `--workspace-ref <ref>` | Select workspace. |
| `--surface-ref <ref>` | Select surface. |
| `--pane-ref <ref>` | Select pane. |

Examples:

```powershell
ecode pane list
ecode pane split right
ecode pane write "npm test" --submit true
ecode pane read pane:1 --lines 80
ecode pane resize pane:1 0.05
```

## Split Shortcut

The top-level `split` command is a v1 compatibility shortcut:

```powershell
ecode split right
ecode split down
```

Aliases:

- `right`, `vertical`, `v`
- `down`, `horizontal`, `h`

## Browser

Browser commands use the v1 pipe and live WebView2 surfaces.

| Command | Description |
|---|---|
| `ecode browser open <url>` | Open URL in the selected Browser surface or create one. |
| `ecode browser new <url>` | Always create a new Browser surface. |
| `ecode browser open-split <url>` | Compatibility entry; currently creates a new Browser surface. |
| `ecode browser snapshot` | Print a Browser surface snapshot. |
| `ecode browser click` | Click by `--testid`, `--text`, or `--role --name`. |
| `ecode browser fill` | Fill by locator and `--value <text>`. |
| `ecode browser hover` | Hover by locator. |
| `ecode browser press` | Press a key by locator. |
| `ecode browser eval <script>` | Run JavaScript. |
| `ecode browser screenshot` | Return PNG screenshot payload as base64 JSON. |

Common options: `--surfaceRef <ref>`, `--surface-ref <ref>`, `--workspace <name>`, `--surface <name>`, `--testid <id>`, `--text <text>`, `--role <role>`, `--name <name>`, `--key <key>`, and `--value <text>`.

See [Browser API](./browser-api.md) for response shapes and supported/unsupported automation methods.

## Config

```powershell
ecode reload-config
ecode config reload
ecode config diagnostics
```

| Command | Layer | Description |
|---|---|---|
| `ecode reload-config` | v1 | Reload `ecode.json` commands/actions/layout. |
| `ecode config reload` | `ecode.v2` `config.reload` | Reload and cache diagnostics. |
| `ecode config diagnostics` | `ecode.v2` `config.diagnostics` | Return last reload diagnostics or trigger a fresh reload. |

See [Custom Commands](./custom-commands.md) for `ecode.json`.

## Session Restore

```powershell
ecode restore-session
ecode restore-session --all
ecode restore-session --trusted
```

`restore-session` refreshes resume bindings and focuses the first recoverable pane. Options include `--all`, `--trusted`, `--workspace <name>`, `--surface <name>`, and `--noFocus true`.

## Setup

Setup commands run locally and manage PATH, PowerShell profile, and `cmd.exe` AutoRun integration.

| Command | Description |
|---|---|
| `ecode setup status` | Show current setup status and diff. |
| `ecode setup install` | Print a dry-run install plan. |
| `ecode setup install --write true` | Apply the install plan. |
| `ecode setup uninstall` | Print a dry-run cleanup plan. |
| `ecode setup uninstall --write true` | Apply cleanup. |

Options:

| Option | Description |
|---|---|
| `--install-dir <path>` | CLI directory to add/remove from user PATH. |
| `--profile <path>` | PowerShell profile path override. |
| `--powershell-profile <path>` | Alias for `--profile`. |
| `--write true` / `--apply true` | Apply instead of dry-run. |

## Profile Import

```powershell
ecode profile import --settings "$env:LOCALAPPDATA\Packages\Microsoft.WindowsTerminal_8wekyb3d8bbwe\LocalState\settings.json"
```

Imports an ECode Windows Terminal profile plan.

| Option | Default |
|---|---|
| `--settings <path>` | Windows Terminal default settings path. |
| `--profile-name` / `--name` | `ECode Shell` |
| `--guid <guid>` | `{7f4f7d8d-7a1f-45f3-b0c7-ec0de0000001}` |
| `--commandline` / `--command-line` / `--shell` | `pwsh.exe -NoLogo` |
| `--starting-directory` / `--cwd` | `%USERPROFILE%` |
| `--color-scheme` / `--scheme` | `ECode Dark` |
| `--font-face` / `--font` | `Cascadia Mono` |
| `--font-size <n>` | `11` |
| `--write true` | Write the plan; omitted prints dry-run JSON. |
| `--print-json true` | Print JSON after write/dry-run. |

## Updates

Velopack update commands require a feed URL from `--feed-url` or `ECODE_UPDATE_FEED_URL`.

```powershell
ecode update check --feed-url https://example.com/releases
ecode update install --feed-url https://example.com/releases --download-only true
```

| Command | Description |
|---|---|
| `ecode update check` | Check feed for a newer version. |
| `ecode update install` | Download setup and launch it unless `--download-only true` is set. |

Options: `--feed-url <url>`, `--setup-url <url>`, `--installer-url <url>`, `--pack-id <id>`, `--download-dir <path>`, `--download-only true`, `--wait true`, and `--silent false`.

## Status, Health, Doctor, Completion

| Command | Layer | Description |
|---|---|---|
| `ecode status` | v1 `STATUS` | Show app status summary. |
| `ecode health` | `ecode.v2` `health` | Show health summary with checks. |
| `ecode doctor --timeout-ms 700` | Local + daemon probe | Diagnose ConPTY, WebView2, PATH, daemon, and config directory. |
| `ecode completion powershell` | Local | Print PowerShell completion script. |
| `ecode version` | Local | Print CLI version. |
| `ecode help` | Local | Print built-in help and shortcuts. |

## Exit Codes

| Code | Meaning |
|---:|---|
| `0` | Command completed successfully. |
| `1` | Argument error, unknown command, connection timeout, update failure, or local operation failure. |
| installer exit code | `ecode update install --wait true` returns the setup process exit code when available. |

If you see `Error: Could not connect to ecode. Is it running?`, start or focus the ECode app first. Commands that only read local files, such as `help`, `version`, `setup status`, `profile import`, `doctor`, and `completion powershell`, do not require the app pipe.
