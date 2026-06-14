# Browser API

ECode Browser surfaces are WebView2 tabs that can be opened from the CLI and automated for local development smoke tests. The M4 browser scripting contract covers surface refs, accessibility snapshots, locator-based actions, JavaScript eval, screenshots, state/control operations, and a stable `not_supported` matrix.

## Requirements

- ECode must be running; the CLI talks to the app through the named pipe daemon.
- WebView2 Runtime must be installed for live browser surfaces.
- A Browser surface must exist. Create one with `ecode browser open`, `ecode browser new`, or `workspace.surfaces` in `ecode.json`.

## Open A Browser Surface

```powershell
ecode browser open http://localhost:5173
ecode browser new http://localhost:5173 --name Preview
ecode browser open-split http://localhost:5173 --direction right
```

| Command | Behavior |
|---|---|
| `ecode browser open <url>` | Reuses the selected Browser surface when possible, otherwise creates one. |
| `ecode browser new <url>` | Always creates a new Browser surface. |
| `ecode browser open-split <url>` | Compatibility entry for mixed-pane workflows. Today it creates a new Browser surface and reports `fallbackMode: "new-surface"`. |

The response includes fields such as `workspaceId`, `workspaceName`, `surfaceId`, `surfaceRef`, `surfaceName`, `kind`, `url`, and `title`.

Use `--json` when a script needs raw JSON:

```powershell
ecode --json browser open http://localhost:5173 --name Preview
```

## Surface Refs

Most browser automation commands need a `surfaceRef`:

```text
surface:<surfaceId>
```

`surfaceRef` is returned by `browser open`, `browser new`, and `browser open-split`.

```json
{
  "ok": true,
  "surfaceId": "9c42...",
  "surfaceRef": "surface:9c42...",
  "url": "http://localhost:5173/"
}
```

If `--surfaceRef` is omitted, ECode picks the selected Browser surface in the selected workspace, then the first Browser surface in that workspace.

Selectors and aliases:

| Option | Description |
|---|---|
| `--surfaceRef <ref>` | Direct browser ref, for example `surface:9c42...`. |
| `--surface-ref <ref>` | Alias for `--surfaceRef`. |
| `--surface-id <id>` | Alias accepted by the CLI argument normalizer. |
| `--workspace <name>` | Resolve within a workspace by name. |
| `--surface <name>` | Resolve a surface by name before scripting. |

Ref errors use stable codes:

| Code | Meaning |
|---|---|
| `invalid_ref` | The ref is missing or does not use `surface:<id>`. |
| `not_found` | The surface or target node does not exist. |
| `stale_ref` | The ref or DOM node was tracked before but is no longer live. |
| `not_supported` | The target is not a Browser surface, or the feature is intentionally unavailable. |
| `timeout` | The browser operation timed out. |
| `internal_error` | The browser bridge raised an unexpected error. |

## Snapshot

```powershell
ecode browser snapshot --surfaceRef surface:9c42...
```

The result contains a visible accessibility-like tree:

```jsonc
{
  "ok": true,
  "result": {
    "surfaceRef": "surface:9c42...",
    "surfaceId": "9c42...",
    "snapshot": {
      "root": {
        "nodeId": "root",
        "role": "document",
        "name": "Example",
        "text": "Welcome",
        "testId": null,
        "visible": true,
        "children": []
      }
    }
  }
}
```

Each node has:

| Field | Description |
|---|---|
| `nodeId` | Runtime DOM node id used by the action bridge. |
| `role` | Derived role such as `button`, `link`, `textbox`, `heading`, `img`, `form`, or `document`. |
| `name` | Accessible-ish name from `aria-label`, `alt`, `title`, `placeholder`, value, or text. |
| `text` | Trimmed visible text. |
| `testId` | `data-testid` or `data-test-id`. |
| `visible` | Hidden nodes are filtered out by locators. |
| `children` | Nested nodes. |

## Locators

Live CLI actions accept one locator:

| Locator | Match Rule |
|---|---|
| `--testid <id>` | Exact `data-testid` or `data-test-id` match. |
| `--text <text>` | Case-insensitive contains match against node text or name. |
| `--role <role> --name <name>` | Case-insensitive role match, with optional exact accessible-name match. |

For action commands, the first matching visible node is used.

Positional shorthand:

```powershell
ecode browser click save-button
ecode browser fill email-input codex@example.com
ecode browser press email-input --key Enter
```

These map to `--testid save-button`, `--testid email-input --value ...`, and `--testid email-input`.

The core M4 locator contract also includes `find.role`, `find.text`, `find.testid`, `find.first`, `find.last`, and `find.nth`. The public live CLI currently exposes the locator result indirectly through `snapshot` plus action commands.

## Live CLI Actions

| Command | Required Params | Result |
|---|---|---|
| `ecode browser click` | A locator | Clicks the first matching node. |
| `ecode browser fill` | A locator and `--value <text>` | Focuses the node, sets value, then dispatches `input` and `change`. Empty string values are supported. |
| `ecode browser hover` | A locator | Dispatches mouseover/mouseenter events. |
| `ecode browser press` | A locator and optional `--key <key>` | Dispatches keydown/keyup. Defaults to `Enter`. |
| `ecode browser eval <script>` | JavaScript text | Runs script in WebView2 and returns the JSON value from `ExecuteScriptAsync`. |
| `ecode browser screenshot` | `surfaceRef` or browser selector | Captures a PNG preview as base64. |

Examples:

```powershell
ecode browser click --surfaceRef surface:9c42... --testid save-button
ecode browser fill --surfaceRef surface:9c42... --testid email-input --value codex@example.com
ecode browser fill --surfaceRef surface:9c42... --testid email-input --value ""
ecode browser hover --surfaceRef surface:9c42... --role button --name Save
ecode browser press --surfaceRef surface:9c42... --testid email-input --key Enter
ecode browser eval --surfaceRef surface:9c42... "document.title"
ecode --json browser screenshot --surfaceRef surface:9c42... > screenshot.json
```

Screenshot action result:

```jsonc
{
  "ok": true,
  "result": {
    "value": {
      "contentType": "image/png",
      "encoding": "base64",
      "data": "iVBORw0KGgo..."
    },
    "diagnostics": {
      "liveSurfaceCount": 2,
      "liveBrowserSurfaceCount": 1,
      "registeredRefCount": 1,
      "surfaceRef": "surface:9c42...",
      "surfaceId": "9c42..."
    }
  }
}
```

## M4 Contract Methods

The browser scripting service keeps these method families aligned with the M4 protocol and tests. Public CLI routing is available for the live subset above; state/control families are service-level contracts until an external router exposes them.

| Family | Methods | Status |
|---|---|---|
| Surface | `browser.snapshot` | Live CLI available as `ecode browser snapshot`. |
| Find | `browser.find.role`, `browser.find.text`, `browser.find.testid`, `browser.find.first`, `browser.find.last`, `browser.find.nth` | Service contract; CLI actions use these locator rules internally. |
| Actions | `browser.click`, `browser.fill`, `browser.hover`, `browser.press`, `browser.eval`, `browser.screenshot` | Live CLI available as `ecode browser ...`. |
| Cookies | `browser.cookies.get`, `browser.cookies.set`, `browser.cookies.clear` | Service contract with state dispatch tests. |
| Storage | `browser.storage.get`, `browser.storage.set`, `browser.storage.clear` | Service contract for local/session storage. |
| Console | `browser.console.list`, `browser.console.clear` | Service contract with control dispatch tests. |
| Dialogs | `browser.dialog.accept`, `browser.dialog.dismiss` | Service contract with control dispatch tests. |
| Downloads | `browser.download.wait` | Service contract with control dispatch tests. |
| Highlight | `browser.highlight` | Service contract with locator dispatch tests. |
| Injection | `browser.addinitscript`, `browser.addscript`, `browser.addstyle` | Service contract with control dispatch tests. |

When a service-level family has no live executor wired in the current app build, it returns `not_supported` rather than pretending the method succeeded.

## Not Supported Matrix

These high-cost or platform-constrained operations intentionally return `not_supported`:

| Feature | Contract Name |
|---|---|
| Viewport emulation | `browser.viewport` |
| Geolocation emulation | `browser.geolocation` |
| Offline mode | `browser.offline` |
| Tracing | `browser.trace` |
| Network routing | `browser.network.route` |
| Screencast | `browser.screencast` |
| Low-level mouse input | `browser.input_mouse` |
| Low-level keyboard input | `browser.input_keyboard` |
| Low-level touch input | `browser.input_touch` |

Use the supported high-level locator commands (`click`, `fill`, `hover`, `press`) for smoke tests.

## Error Shape

Browser scripting errors use this shape:

```jsonc
{
  "ok": false,
  "error": {
    "code": "not_found",
    "message": "Locator did not match any node."
  },
  "diagnostics": {
    "liveSurfaceCount": 2,
    "liveBrowserSurfaceCount": 1,
    "registeredRefCount": 1,
    "surfaceRef": "surface:9c42...",
    "surfaceId": "9c42..."
  }
}
```

`diagnostics` is useful when a script targets the wrong workspace or stale browser tab.

## Script Pattern

```powershell
$open = ecode --json browser open http://localhost:5173 --name Preview | ConvertFrom-Json
$ref = $open.surfaceRef

ecode browser snapshot --surfaceRef $ref
ecode browser fill --surfaceRef $ref --testid email-input --value codex@example.com
ecode browser click --surfaceRef $ref --role button --name Save
ecode browser eval --surfaceRef $ref "document.body.dataset.saved"
```

For repeatable project setup, add the Browser surface to `workspace.surfaces` in `ecode.json`, then run browser commands against the returned or selected `surfaceRef`.
