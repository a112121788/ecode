# Session Restore

ECode restores app-owned state automatically and uses explicit resume bindings for commands that need to be run after a restart. It does not checkpoint arbitrary live processes. A resume command only runs when the user chooses it or when a trusted binding is allowed to auto-run.

## Runtime Files

| File | Purpose |
|---|---|
| `%USERPROFILE%\.ecode\session.json` | Window/workspace/surface layout, terminal pane snapshots, and Browser surface metadata. |
| `%USERPROFILE%\.ecode\resume.json` | Per-pane resume commands and trust metadata. |
| `%USERPROFILE%\.ecode\logs\` | Command and transcript logs captured around close/clear events. |

These files are human-readable JSON and are not removed automatically by installers or uninstallers.

## What Session State Restores

`session.json` is app-owned state. It is saved best-effort and loaded on startup.

Important fields:

| Field | Description |
|---|---|
| `version` | Session file version. Current value is `1`. |
| `selectedWorkspaceIndex` | Last selected workspace. |
| `window` | Window bounds, maximized state, sidebar width/visibility, compact sidebar flag. |
| `workspaces[]` | Workspace id, name, icon, accent color, working directory, selected surface. |
| `workspaces[].surfaces[]` | Surface id, name, kind, split tree, focused pane, and pane metadata. |
| `kind` | `Terminal` or `Browser`. Older files default to `Terminal`. |
| `browserUrl` / `browserTitle` / `browserHistory` | Browser surface restore metadata. |
| `rootNode` | Split tree, direction, split ratio, and pane ids. |
| `paneCustomNames` | User names for panes. |
| `paneSnapshots` | Last captured pane cwd, shell, command history, and buffer snapshot. |

Terminal snapshots are for continuity and diagnostics. They do not keep the original process alive after app exit or machine restart.

## Resume Binding Model

`resume.json` stores commands that can recreate useful terminal state, such as attaching tmux or starting a dev server.

```jsonc
{
  "version": 1,
  "bindings": [
    {
      "id": "binding-1",
      "workspaceId": "workspace-uuid",
      "surfaceId": "surface-uuid",
      "paneId": "pane-uuid",
      "kind": "tmux",
      "checkpoint": "sprint-1",
      "shell": "tmux attach -t work",
      "workingDirectory": "C:\\repo",
      "environment": {
        "SAFE_KEY": "safe"
      },
      "trusted": true,
      "trustReason": "user-approved-prefix",
      "approvedPrefix": "tmux attach",
      "createdAtUtc": "2026-01-01T00:00:00Z",
      "updatedAtUtc": "2026-01-01T00:00:00Z"
    }
  ]
}
```

| Field | Description |
|---|---|
| `id` | Stable binding id. Generated when omitted. |
| `workspaceId` / `surfaceId` / `paneId` | Exact target where the resume command belongs. |
| `kind` | `tmux` or `custom`. Current CLI rejects other kinds. |
| `checkpoint` | Optional label for the saved state. |
| `shell` | Command written to the pane when the binding is restored. |
| `workingDirectory` | Cwd to associate with the binding. Defaults to the target pane session cwd when available. |
| `environment` | Optional safe environment metadata. Sensitive names are removed before save. |
| `trusted` | Whether the binding may run without a manual banner click. |
| `trustReason` | Why the binding was trusted, for example `cli`, `user-approved-binding`, or `user-approved-prefix`. |
| `approvedPrefix` | Optional command prefix that was approved by the user. |
| `createdAtUtc` / `updatedAtUtc` | Audit timestamps. |

Sensitive environment keys are scrubbed before persistence when they include names such as `PASSWORD`, `PASSWD`, `SECRET`, `API_KEY`, `ACCESS_KEY`, `TOKEN`, `_TOKEN_`, or `_TOKEN`.

## Save A Resume Binding

Save a command for the focused pane:

```powershell
ecode surface resume set --kind tmux --shell "tmux attach -t work" --checkpoint sprint-1 --trusted true --approvedPrefix "tmux attach"
```

Save a custom dev command with the positional shorthand:

```powershell
ecode surface resume set "npm run dev" --kind custom --cwd C:\repo
```

Common selectors:

| Option | Description |
|---|---|
| `--workspace <name>` | Select workspace by name. |
| `--surface <name>` | Select surface by name. |
| `--paneId <id>` | Select exact pane id. |
| `--paneName <name>` | Select custom pane name. |
| `--paneIndex <n>` | Select pane by 1-based or 0-based index. |
| `--cwd <path>` | Alias for `workingDirectory`. |
| `--trusted true` | Mark the binding trusted at save time. |
| `--approvedPrefix <prefix>` | Store a reviewed safe command prefix. |

`set` replaces older bindings for the same workspace/surface/pane and keeps the previous `id` when possible.

## Show Or Clear Bindings

```powershell
ecode surface resume show
ecode surface resume show --all
ecode surface resume clear
ecode surface resume clear --id binding-1
```

`show` returns the selected workspace, surface, optional pane info, and matching `bindings`. Without `--all`, it shows only the focused or selected pane. `clear` removes by binding id or by the selected pane.

## Restore Workflow

Use the global restore entry to refresh pending bindings and jump to the first recoverable pane:

```powershell
ecode restore-session
ecode restore-session --all
ecode restore-session --trusted
```

| Option | Behavior |
|---|---|
| `--all` | Scan every workspace and surface. Without it, only the selected surface is scanned. |
| `--trusted` | Run trusted bindings immediately for the scanned surfaces. |
| `--workspace <name>` | Scan a specific workspace. |
| `--surface <name>` | Scan a specific surface. |
| `--noFocus true` | Refresh bindings without focusing the first pending pane. |

The response includes:

| Field | Description |
|---|---|
| `scannedSurfaces` | Number of surfaces refreshed. |
| `pendingBindings` | Number of untrusted bindings now shown as pending banners. |
| `trustedStarted` | Number of trusted bindings that were started. |
| `firstPending` | Workspace/surface/pane for the first pending binding, or `null`. |

Inside the app, use `Ctrl+Shift+O` or the `Restore Session Bindings` command-palette entry. The app focuses and flashes the first recoverable pane.

## Trust And Auto-Run Rules

Untrusted bindings do not run automatically. They appear above the target terminal pane with two choices:

| Button | Effect |
|---|---|
| `Recoverable` | Confirms once, runs the command, and keeps the binding untrusted. |
| `Trust and recover` | Marks the binding trusted, stores the shell as the approved prefix when needed, and runs it. |

Trusted bindings can auto-run only when the global setting `AutoResumeTrustedBindings` is enabled. The default is `false`. `ecode restore-session --trusted` can run trusted bindings immediately for a manual restore pass even when the global auto-run setting is off.

Additional safety rules:

- Browser surfaces never run resume bindings.
- Only active pane ids in the restored split tree are considered.
- The latest binding per pane wins.
- A binding id is auto-run at most once during the current surface lifetime.
- Running a binding writes `shell` plus Enter to the target terminal and records it as a command submission.

## Environment Context

New terminal sessions receive:

```text
ECODE_WORKSPACE_ID=<workspace-id>
```

Use this in scripts that need to discover which workspace launched them. The variable is injected for local ConPTY sessions and daemon-managed sessions.

## Safe Practices

- Prefer `kind: "tmux"` with commands like `tmux attach -t work` for long-running work.
- Keep `trusted` off for commands that modify files, deploy, migrate databases, or start external services.
- Use `approvedPrefix` for broad but reviewable command families such as `tmux attach`.
- Do not store secrets in `resume.json`; sensitive environment keys are scrubbed, but command text remains visible.
- Review `%USERPROFILE%\.ecode\resume.json` before sharing logs or support bundles.
