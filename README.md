# 倒计时

Windows 桌面倒计时工具。窗口、存档、托盘和操作栏使用 WinForms；翻页数字区域
通过 WebView2 运行 MIT 许可的 `pqina/flip`。

## 当前功能

- 深色议程主界面：左侧主倒计时，右侧连续事件列表同屏展示
- 超过 3 个事件时，右侧议程列表可滚动；点击事件切换当前选择
- 主界面、Windows 标题栏和底部操作栏统一使用石墨灰与暖琥珀配色
- 底部操作栏可直接切换深色/浅色主题，并记住上次选择
- 深色主题使用暖金事件线；浅色主题使用深青事件线，两套主题不会共用强调线颜色
- 主事件使用圆环展示创建至目标日期的已过比例，后续事件使用紧凑进度条
- 添加/编辑使用双栏弹窗：左侧填写，右侧实时预览；按钮统一为图标加文字
- 目标日期支持输入 `YYYY-MM-DD`，也可通过“选择”按钮打开日历
- 桌面小组件：纵向显示最多 3 个事件，最紧迫的事件使用磨砂主卡
- 后续事件取消独立卡片，每行左侧显示名称和日期、右侧显示单行倒计时，
  最后一项在底部逐渐淡出
- 主界面右上角“组件”按钮切换到桌面小组件，组件顶部 `↗` 返回
- 底部极简操作栏集中处理当前事件的添加、编辑和删除
- 使用 `pqina/flip` 原生翻页卡状态机和 3D 动画
- `天、时、分、秒`标签由翻页组件自动对齐对应数字组
- 添加、编辑和删除倒计时事件
- 按日期自动排序，已到期事件放在末尾
- 所有卡片按秒更新天、时、分、秒
- 删除后五秒内可撤销
- 事件自动保存到本地 JSON
- 关闭主窗口后驻留系统托盘
- 支持高 DPI 显示

## 启动

双击项目根目录的 `run.cmd`。脚本会构建并启动：

```text
build\CountdownApp.exe
```

打开 `build\CountdownApp.exe` 后默认进入主倒计时界面。点击右上角的
“组件”按钮切换到小组件模式；小组件模式顶部点击 `↗` 返回主界面。两种模式
共用同一份事件存档和同一个进程。

运行环境需要 Microsoft Edge WebView2 Runtime。Windows 10/11 通常已随 Edge 安装。

也可以单独构建：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
.\build\CountdownApp.exe
```

使用演示数据预览：

```bat
run-demo.cmd
```

## 数据位置

事件保存在 `build\data\events.json`。若程序目录不可写，会自动回退到：

```text
%LocalAppData%\CountdownApp\data\events.json
```

桌面小组件的位置和显示状态保存在 `build\data\widget.json`。
主界面的深浅主题选择保存在 `build\data\ui.json`。

## 维护

项目使用 Windows 自带的 .NET Framework 编译器，不安装 .NET SDK。WebView2 SDK
位于 `lib\WebView2`，`pqina/flip` 位于 `web\vendor`，两者均随构建复制到 `build`。
修改前请阅读 [AGENTS.md](AGENTS.md)，代码改动后执行：

当前依赖版本、已完成项和后续注意事项见 [PROJECT_STATUS.md](PROJECT_STATUS.md)。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-tests.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```
