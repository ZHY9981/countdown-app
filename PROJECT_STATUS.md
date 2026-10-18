# 倒计时项目状态

最后更新：2026-10-18

## 当前形态

Windows 桌面应用，采用方案 E 多倒计时布局：

- 左侧为主倒计时卡片
- 右侧为紧凑倒计时卡片列表，可滚动
- 至少同屏显示 3 个倒计时
- 底部操作栏负责添加、编辑、删除和撤销
- 关闭主窗口后继续驻留系统托盘

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

## 依赖约定

- `web\vendor` 保存固定版本的 `pqina/flip` 资源及 MIT 许可。
- `lib\WebView2` 保存固定版本的 WebView2 SDK、Loader 和许可文件。
- 运行环境需要 Microsoft Edge WebView2 Runtime。
- 不通过 CDN 加载翻页资源，不在构建时临时下载 NuGet 包。
- 不重新实现简化翻页动画，`pqina/flip` 是当前翻页效果的唯一实现。

## 已验证

- `build.ps1` 构建成功。
- 离线日期与存档测试全部通过。
- WebView2 真实渲染成功，3 张倒计时卡片正常初始化。
- `pqina/flip` 上下翻片强制为精确 `50%`，不使用自动 `fit` 缩放。
- 天、时、分、秒标签与对应数字组对齐。
- 桌面启动器指向最新构建 EXE。

## 后续注意

- 升级 `pqina/flip` 或 WebView2 SDK 时同步更新版本记录和许可证。
- 修改翻页 CSS 后检查动画中间帧，不能只检查静止状态。
- 如果继续调整卡片布局，保持主卡与紧凑卡的事件点击、选中状态和底部操作同步。
- 旧的 WinForms 自定义动画控件已不再承担正式渲染，后续清理时不要误删
  `web\countdown.template.html` 和 `web\vendor`。
