# Browser API

ECode Browser Surface 基于 WebView2，可通过 CLI 和 `ecode.v2` 协议进行脚本化控制。它适合本地开发 smoke、表单检查、页面状态读取和截图。

## 打开 Browser Surface

```powershell
ecode browser open https://example.com
ecode browser new https://example.com
ecode browser open-split https://example.com --direction right
```

`open` 会复用当前 Browser Surface；`new` 会创建新 Surface；`open-split` 会在当前布局旁边创建 Browser Pane。

## Surface 引用

Browser 命令通过 `surfaceRef` 定位目标。可使用短引用或 UUID：

```powershell
ecode browser snapshot --surfaceRef surface:1
ecode browser eval "document.title" --surfaceRef surface:1
```

human 输出默认展示 refs，JSON 输出默认同时包含 refs 与 UUID。

## Snapshot 快照

```powershell
ecode browser snapshot --surfaceRef surface:1
```

对应 v2 方法：`browser.snapshot`。

返回内容包含可访问树、refs、URL、标题和诊断信息。常见用途：先 snapshot，确认按钮或输入框的 ref，再执行 click/fill。

## Locator 定位器

核心契约支持：

- `find.role`
- `find.text`
- `find.testid`
- `find.first`
- `find.last`
- `find.nth`

CLI 动作通常通过参数表达 locator：

```powershell
ecode browser click --role button --name Submit
ecode browser fill --testid email --value user@example.com
ecode browser click --text "登录"
```

## 动作命令

| CLI | v2 方法 | 说明 |
|---|---|---|
| `ecode browser click` | `browser.click` | 点击元素。 |
| `ecode browser fill` | `browser.fill` | 输入文本；空字符串会清空 input。 |
| `ecode browser hover` | `browser.hover` | 悬停元素。 |
| `ecode browser press` | `browser.press` | 发送键盘按键。 |
| `ecode browser eval` | `browser.eval` | 执行 JavaScript 并返回结果。 |
| `ecode browser screenshot` | `browser.screenshot` | 保存截图。 |

示例：

```powershell
ecode browser fill --testid search --value "ECode"
ecode browser press --key Enter
ecode browser screenshot --path .\artifacts\browser.png
```

## 状态与控制

| v2 方法 | 说明 |
|---|---|
| `browser.cookies.get` / `browser.cookies.set` / `browser.cookies.clear` | 读取、写入、清理 cookie。 |
| `browser.storage.get` / `browser.storage.set` / `browser.storage.clear` | 操作 local/session storage。 |
| `browser.console.list` | 读取 console 事件。 |
| `browser.dialog.accept` / `browser.dialog.dismiss` | 处理 dialog。 |
| `browser.download.list` | 查看下载状态。 |
| `browser.highlight` | 高亮目标元素，辅助调试。 |
| `browser.addinitscript` / `browser.addscript` / `browser.addstyle` | 注入脚本或样式。 |

## not_supported 矩阵

M4 阶段明确返回 `not_supported` 的能力包括：

- `browser.viewport.*`
- `browser.geolocation.*`
- `browser.offline.*`
- `browser.trace.*`
- `browser.network.route`
- `browser.screencast.*`
- `browser.input_*`

稳定错误码：`invalid_ref`、`not_found`、`stale_ref`、`not_supported`、`timeout`、`internal_error`。

## 排查建议

1. 先运行 `ecode browser snapshot --surfaceRef <ref>`。
2. 如果 locator 找不到，检查文本、role、test id 是否在 snapshot 中出现。
3. 严格 CSP 页面可能限制脚本注入；优先使用 WebView2 原生能力。
4. 错误响应中的 `hint`、`snapshotExcerpt` 可帮助定位问题。
