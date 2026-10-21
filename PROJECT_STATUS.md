# 倒计时项目状态

最后更新：2026-10-21

## 当前形态

Windows 桌面应用，采用方案 E 多倒计时布局：

- 左侧为主倒计时卡片
- 右侧为紧凑倒计时卡片列表，可滚动
- 至少同屏显示 3 个倒计时
- 底部操作栏负责添加、编辑、删除和撤销
- 关闭主窗口后继续驻留系统托盘

## 接管基线

- 正式仓库：`D:\codex work\countdown-app`
- 当前分支与远端：`main`；最近提交为 `1d43581`，已同步到 `origin/main`。
- 当前工作区包含 2026-10-21 的未提交小组件视觉改进：顶部栏、石墨灰/暖琥珀
  配色、620×680 尺寸、主卡比例和横向次事件布局。
- 2026-10-21 已重新执行 `build.ps1` 和离线测试，均通过。
- 用户数据位于 `build\data\events.json`，其中当前包含测试事件。开发、构建和
  自动测试不得重置或覆盖这份文件；视觉验收优先使用 `--demo`。
- 项目记忆以本文件、`AGENTS.md`、`README.md` 和 Git 为准，不依赖单一聊天会话。

## 工作流沉淀

- `APP_WORKFLOW.md` 已加入“桌面组件与多入口”阶段，记录同窗口/多入口取舍、关闭语义、
  WebView2 生命周期、原生回退、状态签名轮询和主题变量等经验。
- `AGENTS.md` 已把同窗口双模式、关闭语义、防闪烁和集中配色列为项目硬规则。

## 技术结构

- 原生外壳：C# WinForms + .NET Framework
- 翻页数字：`pqina/flip 1.8.4`
- 渲染宿主：Microsoft WebView2
- WebView2 SDK：`1.0.4258.31`
- 数据存档：`build\data\events.json`
- 构建入口：`build.ps1`
- 用户入口：`run.cmd`
- 桌面快捷方式：`C:\Users\ZHY\OneDrive\Desktop\倒计时.lnk`

窗口、存档、托盘、底部操作栏和事件管理保持原生。WebView2 只负责中间卡片区域。

## 桌面小组件

目标是在现有程序中增加常驻桌面的小组件，不另起项目，共用同一份存档、托盘和
主窗口。

- 2026-10-20 已确认概念方向：事件从上到下纵向排列，主事件在最上方，组件
  不再使用整块不透明底板，底部逐渐透出桌面。
- 已确认合并切换概念图：`concepts\countdown-merged-modes-concept-final.png`。
- 布局：最多显示 3 个事件，第一张为磨砂主卡；下面两个事件取消独立卡片、
  圆角和卡间距，用分隔线连成半透明列表；每行左侧显示事件信息，右侧显示
  单行倒计时，最后一项逐渐淡出。
- 排序：最紧迫的事件进入主卡，主卡面积最大。
- 默认位置：屏幕右下角，避开桌面图标；允许拖动并记住位置。
- 交互：双击打开主窗口；托盘菜单可以显示、隐藏组件和退出程序。
- 图层：小组件模式使用无边框顶层窗口形态和
  `WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE`，不出现在任务栏或 Alt+Tab。
- Wallpaper Engine 运行时，组件锚定在壁纸窗口之前、普通应用窗口之后；
  未运行时回退到 `Progman` / `WorkerW`。需要定时检查并在 Explorer 或
  Wallpaper Engine 重启后重新锚定。
- 在这台机器上，把组件做成 `Progman` / `WorkerW` 子窗口会被
  Wallpaper Engine 覆盖，不能继续采用子窗口方案。

用户已明确允许引入相关代码。`DesktopMusicWidget`
(`https://github.com/DuanLingLan/DesktopMusicWidget`, MIT) 的 Z-order 思路
已记录在本地未提交的 `THIRD_PARTY_NOTICES.md`。本项目没有复制其 Rust 实现，
而是在 `src\DesktopLayerHost.cs` 中重新实现等价的 C# 逻辑。

当前正式实现文件：`src\MainForm.cs`、`src\DesktopLayerHost.cs`、
`src\WidgetSettings.cs`、`src\WidgetTheme.cs`、`web\widget.template.html`。

主程序和小组件已在同一个窗口中合并：

- 唯一入口：`build\CountdownApp.exe`。
- 默认显示主倒计时界面，底部操作栏“组件”按钮切换到小组件模式。
- 小组件顶部胶囊的 `↗` 按钮返回主界面。
- 两种模式共用同一个 MainForm、WebView2、事件存档和进程。
- 旧的 `CountdownWidget.exe`、`run-widget.cmd` 和桌面“倒计时小组件”快捷方式
  已删除。

小组件页面在原生消息更新 DOM 后调用 `Tick.DOM.parse(document)`，让
`pqina/flip` 重新扫描动态创建的事件卡，确保收到事件后能显示时间。

小组件只在模式切换、事件数据刷新或页面重新初始化时重建 DOM。倒计时秒数由
页面自身更新，避免反复初始化 `pqina/flip` 造成闪烁。

小组件当前使用石墨灰背景、冷白文字和暖琥珀强调色。网页颜色集中在
`web\widget.template.html` 的 CSS 变量，小组件模式的原生标题栏颜色集中在
`src\WidgetTheme.cs`。后续调整颜色时只改这两处，不改布局。

临时层级验证文件位于：

```text
C:\Users\ZHY\.codex\visualizations\2026\10\18\01a14e87-f47d-7e00-94e0-9aab702c9356
```

这些文件只作为实验证据，不属于正式项目。

## 依赖约定

- `web\vendor` 保存固定版本的 `pqina/flip` 资源及 MIT 许可。
- `lib\WebView2` 保存固定版本的 WebView2 SDK、Loader 和许可文件。
- 运行环境需要 Microsoft Edge WebView2 Runtime。
- 不通过 CDN 加载翻页资源，不在构建时临时下载 NuGet 包。
- 不重新实现简化翻页动画，`pqina/flip` 是当前翻页效果的唯一实现。

## 已验证

- `build.ps1` 构建 `CountdownApp.exe` 成功。
- 默认启动验收显示 1080×680 主界面；`--widget` 模式验收切换到
  620×680 小组件形态。
- 小组件关闭后进程退出，单实例锁释放；随后可以立即重新启动。
- 离线日期与存档测试全部通过。
- 主窗口历史版本曾在正常桌面会话中验证过 WebView2 渲染；当前受限会话下
  新的 WebView2 导航出现 `ConnectionAborted`，不能据此宣称小组件已视觉验收。
- `pqina/flip` 上下翻片强制为精确 `50%`，不使用自动 `fit` 缩放。
- 天、时、分、秒标签与对应数字组对齐。
- 小组件顶部为独立拖动胶囊和两个图标按钮；真实窗口截图确认三个事件均完整显示，
  主卡数字不压住边框。
- 下面两个事件采用左侧名称/日期/状态、右侧单行倒计时的横向布局；真实窗口截图
  确认倒计时没有换行或堆叠。
- 当前验收截图：`concepts\widget-compact-horizontal-final.png`。
- 640 以上物理宽度不等于对应数量的 CSS 像素；调整小组件横向密度前先读取
  WebView2 的 `innerWidth` 和实际元素宽度，不能只按概念图画布尺寸推算。
- 桌面启动器指向最新构建 EXE。

## 已知问题

- `--screenshot` 偶尔会在 WebView2 内容加载完成前触发，产生
  `WebView2 is not ready`。本轮已在正常桌面会话成功生成真实窗口截图；后续自动
  验收失败时应等待或重试，不要只凭空白截图判断界面损坏。
- Windows 亚克力与 WebView2 透明合成实测会让整个窗口在桌面上不可见，相关实验
  已撤回。当前稳定方案是不透明深色底板加半透明卡片，不代表能透出真实壁纸。
- README 提供 `default`、`ink`、`graphite` 主题切换，但网页卡片样式目前是
  固定浅色配色。切换主题后原生外壳与中央 WebView2 区域可能不一致，需要把
  主题参数明确传给网页层。
- 根目录 `D:\codex work\APP_WORKFLOW.md` 目前只有本地副本，没有独立 Git
  远程备份。

## 后续注意

- 升级 `pqina/flip` 或 WebView2 SDK 时同步更新版本记录和许可证。
- 修改翻页 CSS 后检查动画中间帧，不能只检查静止状态。
- 如果继续调整卡片布局，保持主卡与紧凑卡的事件点击、选中状态和底部操作同步。
- 旧的 WinForms 自定义动画控件已不再承担正式渲染，后续清理时不要误删
  `web\countdown.template.html` 和 `web\vendor`。

## 2026-10-21 收尾快照

- 已完成：方案 A 主卡层级、独立顶部拖动胶囊和按钮、暖琥珀配色、620×680
  小组件、下方两项横向排布、第三项淡出。
- 已验证：真实 WebView2 窗口截图确认三个事件完整显示，横向倒计时保持单行；
  构建和全部离线测试通过；`git diff --check` 没有格式错误。
- 未提交：本轮源码、文档、概念图和第三方说明均保留在工作区，尚未创建提交或
  推送。
- 已否决：Windows 亚克力透明方案，原因是会造成整个小组件在桌面上不可见。
- 下次继续：先用 `run.cmd` 人工检查顶部拖动、返回主界面、关闭到托盘、托盘恢复
  和三个以上事件的显示；再决定是否提交本轮改动。
