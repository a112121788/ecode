# Getting Started

This guide walks through the first 15 minutes with ECode. It is split into English and Chinese sections so new users can follow the same flow in either language.

## English

### 1. Launch ECode

Start the app from your install folder or shortcut:

```powershell
ecode-app.exe
```

If you installed shell integration, confirm the CLI is available:

```powershell
ecode version
ecode doctor
```

`doctor` should report ConPTY support, WebView2 availability, PATH status, daemon status, and the runtime config directory.

### 2. Understand the workspace model

ECode organizes work into four layers:

| Layer | What it means | Typical action |
|---|---|---|
| Window | Top-level app window | Create or focus via `ecode window ...` |
| Workspace | A project or task area | Use `Ctrl+N` or `ecode workspace create` |
| Surface | A tab inside a workspace | Use `Ctrl+T` or `ecode surface create` |
| Pane | A terminal leaf inside a surface | Split with `Ctrl+D` or `Ctrl+Shift+D` |

A common layout is one workspace per repository, one surface per task, and multiple panes for build/test/server shells.

### 3. Create your first workspace

From the UI, press `Ctrl+N` to create a workspace. From the CLI:

```powershell
ecode workspace create "demo"
ecode workspace list
```

Switch between workspaces with `Ctrl+1` through `Ctrl+9`, or:

```powershell
ecode workspace select demo
```

### 4. Split panes

Use these shortcuts in a terminal surface:

- `Ctrl+D`: split right.
- `Ctrl+Shift+D`: split down.
- `Ctrl+Alt+Arrow`: focus another pane.

CLI equivalents:

```powershell
ecode pane split right
ecode pane split down
ecode pane list
```

### 5. Open a browser surface

Browser surfaces use WebView2. Open one from the CLI:

```powershell
ecode browser new https://example.com
```

Use browser surfaces for previewing local apps, reading docs beside terminals, or running browser automation commands.

### 6. Try command and notification flows

Send a notification to the app:

```powershell
ecode notify --title "Build" --body "Tests finished"
ecode notification list
```

Unread notifications appear on workspace/surface/pane UI states, and `Ctrl+Shift+U` jumps to the latest unread notification.

### 7. Save a resume binding

Resume bindings let a pane remember how to restart a long-lived command:

```powershell
ecode surface resume set --shell "npm run dev" --kind custom --trusted true
ecode surface resume show
```

Later, run:

```powershell
ecode restore-session --trusted true
```

### 8. Next steps

- Read [Installation](./installation.md) for installer and update options.
- Read [CLI Reference](./cli.md) for command groups and global flags.
- Read [Custom Commands](./custom-commands.md) to automate project-specific workflows.
- Read [Troubleshooting](./troubleshooting.md) if startup, WebView2, PATH, or daemon checks fail.

## 中文

### 1. 启动 ECode

从安装目录或快捷方式启动：

```powershell
ecode-app.exe
```

如果已经安装 shell 集成，先确认 CLI 可用：

```powershell
ecode version
ecode doctor
```

`doctor` 会检查 ConPTY、WebView2、PATH、daemon 以及运行时配置目录。

### 2. 理解工作区模型

ECode 的层级如下：

| 层级 | 含义 | 常见操作 |
|---|---|---|
| Window | 应用窗口 | 通过 `ecode window ...` 创建或聚焦 |
| Workspace | 一个项目或任务域 | 使用 `Ctrl+N` 或 `ecode workspace create` |
| Surface | 工作区内的标签页 | 使用 `Ctrl+T` 或 `ecode surface create` |
| Pane | Surface 内的终端叶子节点 | 使用 `Ctrl+D` 或 `Ctrl+Shift+D` 分屏 |

推荐方式：一个仓库一个 Workspace，一个任务一个 Surface，再用多个 Pane 跑构建、测试和服务。

### 3. 创建第一个 Workspace

在 UI 中按 `Ctrl+N`。也可以使用 CLI：

```powershell
ecode workspace create "demo"
ecode workspace list
```

使用 `Ctrl+1` 到 `Ctrl+9` 切换 Workspace，或执行：

```powershell
ecode workspace select demo
```

### 4. 分屏终端

常用快捷键：

- `Ctrl+D`：向右分屏。
- `Ctrl+Shift+D`：向下分屏。
- `Ctrl+Alt+Arrow`：切换焦点 Pane。

CLI 等价命令：

```powershell
ecode pane split right
ecode pane split down
ecode pane list
```

### 5. 打开 Browser Surface

Browser Surface 基于 WebView2：

```powershell
ecode browser new https://example.com
```

它适合预览本地 Web 应用、把文档放在终端旁边，或使用 browser automation 命令做脚本化操作。

### 6. 尝试通知流程

发送一条通知：

```powershell
ecode notify --title "Build" --body "Tests finished"
ecode notification list
```

未读通知会显示在 Workspace、Surface 和 Pane 状态上；`Ctrl+Shift+U` 会跳转到最新未读通知。

### 7. 保存恢复绑定

Resume binding 可以记录某个 Pane 该如何重启长期运行命令：

```powershell
ecode surface resume set --shell "npm run dev" --kind custom --trusted true
ecode surface resume show
```

之后可以运行：

```powershell
ecode restore-session --trusted true
```

### 8. 下一步

- 阅读 [Installation](./installation.md) 了解安装、更新和卸载。
- 阅读 [CLI Reference](./cli.md) 了解命令组和全局参数。
- 阅读 [Custom Commands](./custom-commands.md) 配置项目级自动化命令。
- 阅读 [Troubleshooting](./troubleshooting.md) 排查启动、WebView2、PATH 或 daemon 问题。
