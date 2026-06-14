# Troubleshooting

Start with `ecode doctor`, then inspect the daemon and command logs when a check points to the app, terminal, or Browser runtime.

```powershell
ecode doctor
ecode --json doctor
ecode health
ecode status
```

## Doctor Checks

`ecode doctor` prints a short health report:

```text
ECode doctor
[ok] conpty: Windows 10.0.22631 supports ConPTY.
[ok] webview2: WebView2 Runtime found: C:\Program Files (x86)\Microsoft\EdgeWebView\Application\...\msedgewebview2.exe
[warn] path: CLI directory is not on PATH: C:\Tools\ECode
[warn] daemon: Main app pipe did not respond; start ecode-app.exe if CLI control is needed.
[ok] config: Runtime/config directory exists: C:\Users\you\.ecode
Overall: attention needed
```

| Check | Status | What To Do |
|---|---|---|
| `conpty` | `ok` or `fail` | ECode needs Windows 10 1809 / build 17763 or newer for ConPTY. Upgrade Windows if this fails. |
| `webview2` | `ok` or `warn` | Browser surfaces need Microsoft Edge WebView2 Runtime. Install or repair it if missing. |
| `path` | `ok` or `warn` | Run `ecode setup status`, then `ecode setup install --write true`, or add the CLI directory to PATH manually. |
| `daemon` | `ok` or `warn` | Start `ecode-app.exe` if CLI control is needed. Local-only commands can still run. |
| `config` | `ok` or `warn` | `%USERPROFILE%\.ecode` is created when ECode first writes runtime data. A warning is normal before first launch. |

Use `--timeout-ms <n>` if the app pipe is slow:

```powershell
ecode doctor --timeout-ms 1500
```

## Log Locations

| File Or Folder | Contents |
|---|---|
| `%USERPROFILE%\.ecode\daemon-debug.log` | App/daemon connection, session create/attach, pipe, fallback, and shutdown events. |
| `%USERPROFILE%\.ecode\logs\` | Daily JSONL command logs. |
| `%USERPROFILE%\.ecode\logs\terminal\` | Captured terminal transcripts grouped by date. |
| `%USERPROFILE%\.ecode\session.json` | Last app-owned layout/session state. |
| `%USERPROFILE%\.ecode\resume.json` | Resume bindings and trust metadata. |
| `%USERPROFILE%\.ecode\settings.json` | User settings, including retention and compatibility flags. |

Open the in-app logs window with `Ctrl+Shift+L`.

## `daemon-debug.log` Field Guide

The daemon log is a line-oriented key/value file. Both the WPF app and `ecode-daemon` can append to it concurrently.

Example:

```text
ts=2026-06-14T05:30:00.0000000+08:00 component=daemon-client event=request.send paneId=pane-1 message="Sending daemon request" path="C:\\Users\\mac\\my repo" requestType=SESSION_CREATE
```

Stable fields:

| Field | Meaning |
|---|---|
| `ts` | Local timestamp in round-trip format. |
| `component` | Source such as `ecode-daemon`, `daemon-pipe-server`, `daemon-session-manager`, `daemon-client`, or `daemon`. |
| `event` | Machine-readable event name. |
| `paneId` | Target pane id, or `-` when the event is not pane-specific. |
| `message` | Human-readable message when available. |
| extra fields | Sorted extra details such as `requestType`, `pipe`, `processId`, `cwd`, `connectedClients`, or `path`. |

Values containing spaces, quotes, or newlines are quoted and escaped. Empty values are written as `-`.

Useful commands:

```powershell
$log = "$env:USERPROFILE\.ecode\daemon-debug.log"
Get-Content $log -Tail 120
Select-String -Path $log -Pattern "paneId=pane-1"
Select-String -Path $log -Pattern "SESSION_CREATE|session.created|session.exited"
Select-String -Path $log -Pattern "fallback|not available|Exception|error"
```

Common event names:

| Event | Meaning |
|---|---|
| `startup.begin` | `ecode-daemon` started. |
| `startup.mutex-exists` | Another daemon instance already owns the mutex. |
| `pipe-server.start` / `pipe-server.started` | Daemon pipe server startup. |
| `accept.wait` / `accept.connected` | Waiting for or accepting daemon pipe clients. |
| `client.connected` / `client.disconnected` | App/daemon client lifecycle. |
| `request.received` | Daemon received a session request. |
| `session.create` / `session.created` | Terminal session creation path. |
| `session.attach` | Reconnecting to an existing daemon session. |
| `session.exited` | Terminal process exited. |
| `shutdown.idle-timeout` | Daemon exited after 24h with no clients and no sessions. |

The WPF app also writes messages such as `[StartSession:<paneId>]`, `[DaemonSession:<paneId>]`, and `[DaemonDisconnected]` through `component=daemon event=log`.

## CLI Cannot Connect

Symptom:

```text
Error: Could not connect to ecode. Is it running?
```

Checklist:

1. Start or focus `ecode-app.exe`.
2. Run `ecode health` to verify the main app pipe.
3. Check named pipes:

```powershell
Get-ChildItem \\.\pipe\ | Where-Object Name -Match "ecode"
```

4. If only local commands are needed, use `ecode setup status`, `ecode doctor`, `ecode completion powershell`, or `ecode version`; these do not need the app pipe.
5. Inspect `%USERPROFILE%\.ecode\daemon-debug.log` for app startup or daemon connection errors.

## Terminal Or ConPTY Issues

If terminal panes do not start:

- Confirm `ecode doctor` reports `conpty: ok`.
- Check `daemon-debug.log` for `SESSION_CREATE`, `session.create`, `session.created`, and fallback messages.
- If the daemon disconnects, ECode falls back to local ConPTY for affected sessions.
- Verify the configured shell exists. `pwsh.exe` must be on PATH if you selected PowerShell 7.
- Run `ecode pane read --lines 80` on an active pane to capture recent output.

For developer smoke checks:

```powershell
.\scripts\ci.ps1 -IncludeSmoke
```

## Browser Or WebView2 Issues

If Browser surfaces are blank or browser scripting returns `not_found` / `not_supported`:

- Run `ecode doctor` and check `webview2`.
- Install or repair Microsoft Edge WebView2 Runtime.
- Try `ecode browser new https://example.com` to isolate project app issues.
- Use the Browser toolbar reload/stop buttons and DevTools button when the page loads but automation fails.
- Run `ecode browser snapshot --surfaceRef <ref>` before `click` or `fill` to confirm locators.
- See [Browser API](./browser-api.md) for the supported locator set and `not_supported` matrix.

## PATH Or Shell Setup Drift

Use setup status first:

```powershell
ecode setup status
ecode setup status --install-dir C:\Tools\ECode
```

If the diff shows drift:

```powershell
ecode setup install --write true
```

If you use a custom PowerShell profile:

```powershell
ecode setup install --profile $PROFILE --write true
```

Restart the shell after changing PATH or profile integration.

## `ecode.json` Problems

Reload and inspect diagnostics:

```powershell
ecode reload-config
ecode config reload
ecode config diagnostics
```

Common causes:

- The active workspace is not the directory containing `.ecode\ecode.json`.
- `commands[].name` or `commands[].command` is empty.
- A Browser `workspace.surfaces[]` entry is missing `url`.
- A target is not `currentTerminal` or `newTabInCurrentPane`.
- JSON comments/trailing commas are allowed, but invalid JSON syntax still fails.

See [Custom Commands](./custom-commands.md) for schema details.

## Resume Binding Does Not Run

Check the binding first:

```powershell
ecode surface resume show --all
ecode restore-session
```

If it still does not run:

- Untrusted bindings show a banner and require `Recoverable` or `Trust and recover`.
- Trusted auto-run is off by default; enable `AutoResumeTrustedBindings` or run `ecode restore-session --trusted`.
- Browser surfaces never run resume bindings.
- The binding `paneId` must exist in the restored split tree.
- The latest binding for a pane wins.
- A trusted binding id auto-runs at most once per surface lifetime.

See [Session Restore](./session-restore.md) for the trust model.

## Installer Or Update Problems

For PATH/profile integration:

```powershell
ecode setup status
ecode setup install --write true
ecode setup uninstall --write true
```

For Velopack updates:

```powershell
ecode update check --feed-url https://example.com/releases
ecode update install --feed-url https://example.com/releases --download-only true
```

If update commands fail, verify:

- `--feed-url` is set or `ECODE_UPDATE_FEED_URL` exists.
- The feed contains a valid Velopack `RELEASES` file.
- Network/proxy rules allow downloading the setup package.
- `--setup-url` points directly to an installer when overriding the feed package URL.

## Local Build Or Test Diagnostics

For maintainers:

```powershell
npm run docs:build
.\.dotnet\dotnet.exe test tests\ECode.Tests\ECode.Tests.csproj -p:NuGetAudit=false
.\.dotnet\dotnet.exe build ECode.sln -c Debug -p:NuGetAudit=false
```

Use `-p:NuGetAudit=false` if NuGet advisory lookups fail because of local certificate or network policy. If a Roslyn `VBCSCompiler` file lock appears, rerun the build/test command sequentially.

Browser integration and smoke tests are opt-in:

```powershell
.\scripts\ci.ps1 -IncludeBrowserIntegration
.\scripts\ci.ps1 -IncludeSmoke
```

## Support Bundle Checklist

Before filing an issue, collect:

```powershell
ecode --json doctor > doctor.json
ecode --json health > health.json
ecode --json config diagnostics > config-diagnostics.json
Get-Content "$env:USERPROFILE\.ecode\daemon-debug.log" -Tail 200 > daemon-debug-tail.log
```

Also include:

- ECode version from `ecode version`.
- Windows version and whether WebView2 Runtime is installed.
- Relevant `ecode.json` or `resume.json` snippets with secrets removed.
- Recent command log entries or terminal transcript excerpts from `%USERPROFILE%\.ecode\logs\`.
