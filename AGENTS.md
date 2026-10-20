# Countdown App 维护说明

## 项目定位

这是 Windows 桌面倒计时应用，使用 C#、WinForms 和 .NET Framework 构建。
翻页数字区域使用 WebView2 承载 MIT 许可的 `pqina/flip`；窗口、操作栏、存档和
托盘保持原生 WinForms。程序不引入 Python、Node.js 或构建时 NuGet 下载。

## 标准命令

在仓库根目录执行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-tests.ps1
```

- `build.ps1` 构建 `build\CountdownApp.exe`。
- `run.cmd` 构建并正常启动桌面程序。
- `run-demo.cmd` 使用内存演示数据启动，不覆盖用户事件。
- `CountdownApp.exe --demo --screenshot=<path>` 可生成主窗口验收截图。
- 修改代码后必须构建并运行完整离线测试。
- 运行环境需要 Microsoft Edge WebView2 Runtime。

## 核心模块

- `Program.cs`：入口、单实例和演示模式。
- `MainForm.cs`：主窗口、WebView2 状态桥接、托盘和用户交互。
- `EventEditForm.cs`：添加和编辑事件对话框。
- `CountdownEngine.cs`：日期解析、剩余时间、排序和状态计算。
- `EventStore.cs`：`data\events.json` 持久化和容错。
- `AppResources.cs`：颜色、主题、高 DPI 尺寸和图标。
- `UiControls.cs`：原生圆角面板、状态标签和图标。
- `web\countdown.template.html`：方案 E 卡片布局和 `pqina/flip` 初始化。
- `web\vendor`：固定版本的 `pqina/flip` 资源，不联网加载。
- `lib\WebView2`：固定版本的 WebView2 SDK 和原生 Loader。

## 兼容约束

- 使用 Windows 自带 `csc.exe` 可编译的 C# 5 语法。
- 不使用字符串插值、表达式成员、记录类型、`nameof` 或其他新语法。
- 不修改 `events.json` 的字段名和含义；变更时必须提供归一化逻辑。
- 不直接编辑或重置 `build\data\events.json` 中的用户事件。
- 保留单实例、系统托盘和关闭窗口后继续驻留的行为。
- 操作按钮使用蓝色 `Accent`；红色只用于临近到期和危险状态。
- 主窗口采用方案 E：左侧主倒计时卡，右侧紧凑倒计时列表，底部统一操作栏。
- 主界面和桌面小组件采用同一窗口、单进程、双模式设计：默认打开主界面，
  主界面底部“组件”按钮切换到小组件模式，小组件顶部 `↗` 返回主界面。
- 桌面小组件采用纵向事件流：最紧迫事件在最上方，下面是最多两张半透明次卡；
  不使用整块不透明底板，组件底部必须保持透明。
- 小组件运行时轮询存档时只比较事件状态；不要在定时器中重建整个动画 DOM。
- 小组件的颜色集中在 `WidgetTheme.cs` 和 `web\widget.template.html` 的
  CSS 变量中，布局调整不得把颜色散落回各个控件。
- 同时至少展示 3 个事件；超过 3 个时右侧紧凑区滚动，不恢复单事件模式。
- 数字翻页交给 `pqina/flip`，不要重新实现简化翻页动画。
- 当前合并版以主 WebView2 作为两种模式的唯一渲染宿主；不要为模式切换再创建
  第二个 WebView2。如果以后增加原生回退，必须单独验收且不能冒充翻页动画。
- 不使用 `data-layout` 的 `fit` 自动缩放；翻片上下两面必须保持精确 `50%` 高度，
  避免小数像素字号造成上下偏移。
- 不修改 `web\vendor` 中的库文件；升级时记录版本、来源和 MIT 许可。
- WebView2 只负责中间卡片区域，底部操作栏和窗口行为保持原生。
- 修改动画后必须检查前后两个阶段的中间帧，不能只检查静止后的最终画面。
- 修改小组件前先确认概念图；页面背景、卡片透明度和纵向排列都属于界面改动。
- 天、时、分、秒单位必须分别居中在对应数字组下方，不能按平均宽三等分。
- 不恢复顶部状态栏、统计条、事件表格、侧边详情、状态栏或快捷键提示。
- 点击卡片切换当前事件；添加、编辑、删除集中底部操作栏，撤销只在删除后临时出现。
- 用户输入必须始终通过 `Text`/`textContent` 等价方式显示，不拼接可执行内容。

## 开发方式

- 日期计算和排序在 `CountdownEngine` 中保持可测试。
- 新行为先补 `tools\CountdownLogicTests.cs`，再做最小实现。
- 在 GitHub 查找到候选方案时，先向用户汇报并等待明确决定，不得直接采用或移植。
- 只有用户明确允许后，才可以直接使用候选项目代码；采用时必须记录仓库、版本、
  许可证、改动范围和来源。
- 任何界面改动前先生成概念图并等待用户确认；确认前不得修改窗口、卡片、颜色、
  字体、间距或交互视觉。
- 网络接口和自动更新不属于当前版本，不要提前增加。
- 未经用户明确要求，不提交 Git commit，不执行推送或重写历史。

## 完成标准

1. 构建成功。
2. 离线测试全部通过。
3. `git diff --check` 没有空白错误。
4. 添加、编辑、删除、撤销、持久化和托盘恢复均能使用。
5. README 和界面说明与行为保持一致。
