# Custom Commands

Use `ecode.json` to teach ECode about project-specific commands, command actions, and the terminal/browser surfaces that should be ready when a workspace opens.

The current M5 runtime supports command-palette entries, command actions, confirmation prompts, two command targets, and startup/reload workspace surfaces.

## File Locations

ECode loads JSONC, so comments and trailing commas are allowed. Missing files are skipped.

| Order | Scope | Path |
|---:|---|---|
| 1 | Global | `%USERPROFILE%\.config\ecode\ecode.json` |
| 2 | Workspace | `<workspace>\.ecode\ecode.json` |
| 3 | Workspace | `<workspace>\ecode.json` |

Later files override earlier files:

- `commands` merge by `name`, case-insensitively.
- `actions` merge by object key.
- `workspace` and `ui` replace the earlier object when present.
- The active workspace path comes from the focused pane working directory first, then the selected workspace root.

## Minimal Command Palette Entry

Create `.ecode/ecode.json` in your repository:

```jsonc
{
  "commands": [
    {
      "name": "Dev: run server",
      "description": "Start the local development server",
      "keywords": ["dev", "server", "vite"],
      "command": "npm run dev",
      "target": "currentTerminal",
      "confirm": true
    }
  ]
}
```

Open the command palette and search for `Dev: run server`. ECode writes the command to the focused terminal and submits it with Enter.

## Full Example

```jsonc
{
  "commands": [
    {
      "name": "Dev: run server",
      "description": "Start the local development server",
      "keywords": ["dev", "server"],
      "command": "npm run dev",
      "target": "currentTerminal",
      "confirm": true
    },
    {
      "name": "Test: watch",
      "description": "Run tests in watch mode",
      "keywords": ["test", "watch"],
      "command": "npm test -- --watch",
      "target": "newTabInCurrentPane"
    }
  ],
  "actions": {
    "devServer": {
      "type": "command",
      "title": "Dev Server",
      "subtitle": "npm run dev",
      "command": "npm run dev",
      "target": "newTabInCurrentPane",
      "palette": true,
      "confirm": true
    }
  },
  "ui": {
    "surfaceTabBar": {
      "buttons": [
        {
          "title": "Dev",
          "icon": "play",
          "action": "devServer"
        }
      ]
    }
  },
  "workspace": {
    "selectedSurfaceIndex": 1,
    "surfaces": [
      {
        "type": "terminal",
        "name": "Shell"
      },
      {
        "type": "browser",
        "name": "Preview",
        "url": "http://localhost:5173"
      }
    ]
  }
}
```

## Schema

### `commands[]`

`commands` create searchable command-palette entries.

| Field | Required | Description |
|---|---:|---|
| `name` | Yes | Palette label. Also used as the merge key. |
| `description` | No | Palette description. Falls back to `command`. |
| `keywords` | No | Extra search terms. Empty values are removed; duplicates are collapsed case-insensitively. |
| `command` | Yes | Text written into the target terminal. |
| `target` | No | `currentTerminal` by default. |
| `confirm` | No | Shows a Yes/No prompt before writing the command. |

ECode trims trailing newlines from `command`, records a command submission, then writes the command plus one newline to the focused terminal.

### `actions`

`actions` are keyed objects for reusable UI actions. The current runtime supports `type: "command"`.

| Field | Required | Description |
|---|---:|---|
| object key | Yes | Stable action id, for example `devServer`. |
| `type` | No | Only `command` is executable today. Other values produce a warning and are ignored by the UI. |
| `title` | No | Palette label. Falls back to the object key when empty. |
| `subtitle` | No | Palette description. Falls back to `command`. |
| `command` | Yes for command actions | Text written into the target terminal. |
| `target` | No | `currentTerminal` by default. |
| `palette` | No | `true` by default. Set `false` to hide the action from the command palette. |
| `confirm` | No | Shows a Yes/No prompt before execution. |

`ui.surfaceTabBar.buttons` is part of the accepted schema and can point at an action id:

```jsonc
{
  "actions": {
    "testWatch": {
      "type": "command",
      "title": "Test Watch",
      "command": "npm test -- --watch",
      "target": "newTabInCurrentPane"
    }
  },
  "ui": {
    "surfaceTabBar": {
      "buttons": [
        { "title": "Tests", "icon": "beaker", "action": "testWatch" }
      ]
    }
  }
}
```

In the M5 UI, command actions are guaranteed through the command palette when `palette` is `true`. Treat custom tab-bar buttons as reserved schema until your build renders them.

### Targets

| Target | Behavior |
|---|---|
| `currentTerminal` | Writes to the focused terminal pane in the selected surface. This is the default. |
| `newTabInCurrentPane` | Creates a new terminal surface in the current workspace, selects it, then writes the command there. |

Unsupported targets produce a diagnostic warning. They currently fall back to the focused terminal behavior, so do not depend on custom target names.

### `workspace`

`workspace` seeds or updates surfaces during app startup and config reload.

```jsonc
{
  "workspace": {
    "selectedSurfaceIndex": 0,
    "surfaces": [
      { "type": "terminal", "name": "Shell" },
      { "type": "browser", "name": "Docs", "url": "https://example.com/docs" }
    ]
  }
}
```

| Field | Description |
|---|---|
| `surfaces[].type` | `terminal` or `browser`. Missing type defaults to `terminal`. |
| `surfaces[].name` | Optional display name. Terminal and browser surfaces can be reused by matching name. |
| `surfaces[].url` | Required for browser surfaces. The browser opens or refreshes this URL. |
| `selectedSurfaceIndex` | Optional zero-based index into `surfaces`. Out-of-range values produce a warning. |

Layout application is additive and safe:

- Existing surfaces are reused by terminal name, browser name, or browser URL.
- Missing named terminal surfaces are created.
- Missing browser surfaces are created and opened to `url`.
- Surfaces not listed in `ecode.json` are not closed.
- Pane splits are not changed by `workspace.surfaces`.

## Reloading

Use any of these after editing `ecode.json`:

```powershell
ecode reload-config
ecode config reload
ecode config diagnostics
```

Inside the app, use `Ctrl+Shift+,` or the `Reload ecode.json` command-palette item. Reloading refreshes palette entries and applies `workspace.surfaces` without restarting the app.

`ecode config diagnostics` returns the last diagnostics through the v2 config API, or triggers a fresh reload if no reload result is available.

## Diagnostics

Diagnostics appear in the command palette and in reload results.

Errors:

- `commands[].name` is empty.
- `commands[].command` is empty.
- A command action omits `command`.
- A surface type is not `terminal` or `browser`.
- A browser surface omits `url`.
- JSON syntax or schema parsing fails.

Warnings:

- An action `type` is not `command`.
- A target is not `currentTerminal` or `newTabInCurrentPane`.
- `workspace.selectedSurfaceIndex` is outside `workspace.surfaces`.

## Safe Practices

- Set `confirm: true` for destructive commands such as clean, reset, deploy, or database migrations.
- Prefer committed scripts (`scripts/dev.ps1`, `npm run test`, `dotnet test`) over long inline shell one-liners.
- Keep secrets out of `ecode.json`; commands run in your normal shell and are visible in project files.
- Quote paths inside commands for the target shell, because ECode writes the command exactly as shell input.
- Use workspace-local `.ecode/ecode.json` for project automation and the global file for personal defaults.
