# Roadmap

ECode is a Windows-native SuperTerminal: a desktop terminal workspace with
browser panes, scriptable control, session restore, and installer-ready Windows
integration.

This public roadmap mirrors the stable portions of `spec/06-roadmap.md`. The
implementation backlog remains the source of truth for individual PR-sized
tasks.

## Current Focus

- Finish M7 documentation, community workflow, and release-readiness gates.
- Keep P0 bugs at 0 and P1 bugs within the 1.0 release threshold.
- Prepare `v1.0.0` release notes and stable distribution guidance.

## Version Line

| Version | Focus | Milestone | Channel |
|---|---|---|---|
| `0.1.x` | Engineering baseline, tests, release scripts, crash fixes | M0 | Nightly |
| `0.2.x` | UI/notification alignment and `ecode.json` basics | M1 | Preview |
| `0.3.x` | Session restore, resume bindings, workspace env injection | M2 | Preview |
| `0.4.x` | Browser pane foundation | M3 | Preview |
| `0.5.x` | Browser scripting API | M4 | Beta |
| `0.6.x` | `ecode.v2`, multi-window, and short refs | M5 | Beta |
| `0.7.x` | Shell/CLI integration, auto-update, installers | M6 | Release candidate |
| `1.0.0` | Stable Windows release | M7 + defect convergence | Stable |

## Milestones

### M0 - Engineering Baseline

- CI and local validation entry points.
- Core unit tests for high-risk lower-level behavior.
- Unified version source across app, CLI, and status IPC.
- Release artifact checks and smoke-test workflow.

### M1 - UI/UX And `ecode.json`

- More complete workspace, surface, and pane interactions.
- Notification rings, unread navigation, and notification panel behavior.
- `ecode.json` command/action foundations and reload flow.
- Tab and workspace reordering persistence.

### M2 - Session Restore

- `session.json` and `resume.json` data model hardening.
- Trusted resume bindings with sensitive environment stripping.
- Manual and trusted automatic restore flows.
- `ECODE_WORKSPACE_ID` injection for terminal processes.

### M3 - Browser Pane Foundation

- Browser surface persistence and split-pane rendering.
- WebView2 runtime handling and fallback messaging.
- Browser toolbar controls, URL/title/history state, and CLI open commands.
- `workspace.surfaces` layout support for browser tabs.

### M4 - Browser Scripting API

- `ecode.v2` browser request parsing and stable error codes.
- Snapshot, locator, click, fill, hover, press, eval, and screenshot actions.
- Cookies, storage, console, dialog, download, and highlight commands.
- Explicit `not_supported` matrix for future browser automation work.

### M5 - v2 Protocol, Multi-window, And Short Refs

- Short refs such as `window:N`, `workspace:N`, `surface:N`, and `pane:N`.
- Window, workspace, surface, pane, notification, config, status, and health APIs.
- CLI commands backed by structured `ecode.v2` responses.
- v1 pipe compatibility while negotiating v2 JSON requests.

### M6 - System Integration, Install, And Update

- PATH, PowerShell profile, cmd AutoRun, and completion setup.
- Windows Terminal profile import planning.
- `ecode doctor`, setup status/install/uninstall, and update commands.
- Self-contained, CLI, Velopack, Inno Setup, and MSIX packaging paths.

### M7 - Documentation, Community, And 1.0

- User docs for install, getting started, CLI, Browser API, session restore, and
  troubleshooting.
- Contribution, security, issue/PR templates, and Discord release notifications.
- P0/P1 release-readiness gate and user-facing `1.0.0` release notes.

## 1.0 Gate

The stable release requires:

- P0 bug count = 0.
- P1 bug count <= 3, each with a documented workaround.
- CI, `scripts/ci.ps1`, and release scripts remain green.
- Core terminal/layout/notification/session restore/`ecode.json`/browser/v2 CLI
  flows reach beta or stable quality.
- Installer, uninstall, and update paths preserve `%USERPROFILE%\.ecode` data.

See [Release Readiness](./release-readiness.md) for the current gate snapshot.
