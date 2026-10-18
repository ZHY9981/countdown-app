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
- 同时至少展示 3 个事件；超过 3 个时右侧紧凑区滚动，不恢复单事件模式。
- 数字翻页交给 `pqina/flip`，不要重新实现简化翻页动画。
- 不使用 `data-layout` 的 `fit` 自动缩放；翻片上下两面必须保持精确 `50%` 高度，
  避免小数像素字号造成上下偏移。
- 不修改 `web\vendor` 中的库文件；升级时记录版本、来源和 MIT 许可。
- WebView2 只负责中间卡片区域，底部操作栏和窗口行为保持原生。
- 修改动画后必须检查前后两个阶段的中间帧，不能只检查静止后的最终画面。
- 天、时、分、秒单位必须分别居中在对应数字组下方，不能按平均宽三等分。
- 不恢复顶部状态栏、统计条、事件表格、侧边详情、状态栏或快捷键提示。
- 点击卡片切换当前事件；添加、编辑、删除集中底部操作栏，撤销只在删除后临时出现。
- 用户输入必须始终通过 `Text`/`textContent` 等价方式显示，不拼接可执行内容。

## 开发方式

- 日期计算和排序在 `CountdownEngine` 中保持可测试。
- 新行为先补 `tools\CountdownLogicTests.cs`，再做最小实现。
- 网络接口和自动更新不属于当前版本，不要提前增加。
- 未经用户明确要求，不提交 Git commit，不执行推送或重写历史。

## 完成标准

1. 构建成功。
2. 离线测试全部通过。
3. `git diff --check` 没有空白错误。
4. 添加、编辑、删除、撤销、持久化和托盘恢复均能使用。
5. README 和界面说明与行为保持一致。
