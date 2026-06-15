---
layout: home

hero:
  name: ECode
  text: Windows 原生 SuperTerminal
  tagline: 面向多项目终端工作流，集成 Workspace、Browser Surface、自动化 API、会话恢复与 Windows 安装更新。
  image:
    src: /app-icon.png
    alt: ECode 图标
  actions:
    - theme: brand
      text: 快速上手
      link: /getting-started
    - theme: alt
      text: 安装
      link: /installation
    - theme: alt
      text: CLI 参考
      link: /cli

features:
  - title: 终端优先
    details: 多 Workspace、多 Surface、分屏 Pane、通知与会话持久化，适合高强度终端协作。
  - title: 浏览器协同
    details: 内置 WebView2 Browser Surface，并提供 snapshot、click、fill、eval、screenshot 等自动化能力。
  - title: Windows 集成
    details: 支持 PATH/profile setup、Windows Terminal profile、doctor 检查、Velopack、Inno Setup、MSIX 与发布自动化。
---

## 文档地图

- [安装](./installation.md)：zip/self-contained、Velopack、Inno Setup、MSIX 与卸载策略。
- [快速上手](./getting-started.md)：首次启动、Workspace、Surface、Pane、Browser Surface、通知与恢复绑定。
- [CLI 参考](./cli.md)：v1/v2 命令、全局参数、setup/update/doctor/completion。
- [故障排查](./troubleshooting.md)：`ecode doctor`、`daemon-debug.log`、WebView2、PATH、恢复与更新问题。
- [发布就绪](./release-readiness.md)：1.0 的 P0/P1 门槛与发布前验证命令。
- [1.0.0 发布说明](./release-notes/1.0.0.md)：可复制到 GitHub Release 的用户说明。
