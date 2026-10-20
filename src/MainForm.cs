using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace CountdownApp
{
    public class MainForm : Form
    {
        private readonly AppState _state;
        private readonly EventStore _store;
        private readonly bool _demoMode;
        private readonly bool _startInWidgetMode;
        private readonly Icon _appIcon;
        private readonly Icon _trayIcon;
        private readonly JavaScriptSerializer _serializer;
        private readonly WidgetSettingsStore _widgetSettingsStore;
        private readonly WidgetSettings _widgetSettings;

        private WebView2 _webView;
        private StatusPill _countLabel;
        private Label _selectedNameLabel;
        private Button _editButton;
        private Button _deleteButton;
        private Button _undoButton;
        private FlowLayoutPanel _summaryPanel;
        private FlowLayoutPanel _actionPanel;
        private Panel _actionBar;
        private Panel _widgetHeader;
        private Panel _workspace;
        private TableLayoutPanel _rootLayout;
        private NotifyIcon _tray;
        private ContextMenuStrip _trayMenu;
        private Timer _uiTimer;
        private Timer _undoTimer;
        private Timer _widgetLayerTimer;
        private ToolTip _toolTip;
        private bool _allowExit;
        private bool _trayHintShown;
        private bool _webReady;
        private bool _widgetMode;
        private CountdownEvent _deletedEvent;
        private int _deletedIndex;
        private string _selectedId;
        private DateTime _renderedDate;
        private Rectangle _mainBounds;
        private Size _mainMinimumSize;

        public MainForm(
            AppState state,
            EventStore store,
            bool demoMode,
            bool startInWidgetMode)
        {
            _state = state;
            _store = store;
            _demoMode = demoMode;
            _startInWidgetMode = startInWidgetMode;
            _serializer = new JavaScriptSerializer();
            _serializer.MaxJsonLength = int.MaxValue;
            _widgetSettingsStore = new WidgetSettingsStore();
            _widgetSettings = _widgetSettingsStore.Load();
            _appIcon = AppResources.CreateAppIcon();
            _trayIcon = AppResources.CreateTrayIcon();
            _toolTip = new ToolTip();
            _toolTip.AutoPopDelay = 5000;
            _toolTip.InitialDelay = 350;
            _toolTip.ReshowDelay = 100;

            Text = "倒计时";
            Icon = _appIcon;
            AutoScaleMode = AutoScaleMode.None;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = AppResources.SS(900, 560);
            Size = AppResources.SS(1080, 680);
            _mainMinimumSize = MinimumSize;
            BackColor = AppResources.Background;
            Font = AppResources.Font(9F, FontStyle.Regular);
            KeyPreview = true;

            BuildInterface();
            BuildTray();

            Load += MainForm_Load;
            Shown += delegate
            {
                PositionActionBar();
                if (_startInWidgetMode)
                {
                    BeginInvoke(new Action(SwitchToWidgetMode));
                }
            };
            FormClosing += MainForm_FormClosing;
            FormClosed += MainForm_FormClosed;
            KeyDown += MainForm_KeyDown;
            LocationChanged += delegate
            {
                if (_widgetMode) SaveWidgetPosition();
            };

            _uiTimer = new Timer();
            _uiTimer.Interval = 1000;
            _uiTimer.Tick += UiTimer_Tick;
            _uiTimer.Start();

            _undoTimer = new Timer();
            _undoTimer.Interval = 5000;
            _undoTimer.Tick += UndoTimer_Tick;

            _widgetLayerTimer = new Timer();
            _widgetLayerTimer.Interval = 1200;
            _widgetLayerTimer.Tick += delegate
            {
                if (_widgetMode && IsHandleCreated)
                {
                    DesktopLayerHost.PlaceBehindAppsAndAboveWallpaper(Handle);
                }
            };
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            _renderedDate = DateTime.Today;
            RefreshView(true);
            PositionActionBar();
            await InitializeWebViewAsync();
            if (_webReady) RefreshWebContent();
        }

        private void BuildInterface()
        {
            _rootLayout = new TableLayoutPanel();
            _rootLayout.Dock = DockStyle.Fill;
            _rootLayout.ColumnCount = 1;
            _rootLayout.RowCount = 3;
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, AppResources.SF(76F)));
            Controls.Add(_rootLayout);

            _widgetHeader = BuildWidgetHeader();
            _widgetHeader.Visible = false;
            _rootLayout.Controls.Add(_widgetHeader, 0, 0);

            _workspace = new Panel();
            _workspace.Dock = DockStyle.Fill;
            _workspace.BackColor = AppResources.Background;
            _workspace.Padding = AppResources.SPad(30, 28, 34, 22);
            _rootLayout.Controls.Add(_workspace, 0, 1);

            _webView = new WebView2();
            _webView.Dock = DockStyle.Fill;
            _webView.DefaultBackgroundColor = AppResources.Background;
            _workspace.Controls.Add(_webView);

            _actionBar = BuildActionBar();
            _rootLayout.Controls.Add(_actionBar, 0, 2);
        }

        private async System.Threading.Tasks.Task InitializeWebViewAsync()
        {
            try
            {
                LogWebView("Initialize start");
                string userDataFolder = ResolveWebViewDataPath();
                LogWebView("User data folder: " + userDataFolder);
                CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(
                    null,
                    userDataFolder,
                    null);
                LogWebView("Environment ready");
                await _webView.EnsureCoreWebView2Async(environment);
                LogWebView("CoreWebView2 ready");

                CoreWebView2 core = _webView.CoreWebView2;
                core.Settings.AreDefaultContextMenusEnabled = false;
                core.Settings.AreDevToolsEnabled = false;
                core.Settings.AreBrowserAcceleratorKeysEnabled = false;
                core.Settings.IsStatusBarEnabled = false;
                core.Settings.IsZoomControlEnabled = false;
                core.WebMessageReceived += WebMessageReceived;
                core.NewWindowRequested += delegate(object sender, CoreWebView2NewWindowRequestedEventArgs args)
                {
                    args.Handled = true;
                };

                string webDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web");
                if (!Directory.Exists(webDirectory))
                {
                    throw new DirectoryNotFoundException("Web assets were not found: " + webDirectory);
                }
                core.SetVirtualHostNameToFolderMapping(
                    "countdown.local",
                    webDirectory,
                    CoreWebView2HostResourceAccessKind.Allow);
                _webReady = true;
                LogWebView("WebView ready");
            }
            catch (Exception ex)
            {
                _webReady = false;
                try
                {
                    File.AppendAllText(
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "webview-error.log"),
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine
                            + ex + Environment.NewLine + Environment.NewLine);
                }
                catch
                {
                    // Logging failure must not hide the original startup error.
                }
                if (!IsAutomationRun())
                {
                    MessageBox.Show(
                        this,
                        "翻页引擎启动失败。请确认已安装 Microsoft Edge WebView2 Runtime。\n\n" + ex.Message,
                        "倒计时",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private static void LogWebView(string message)
        {
            try
            {
                File.AppendAllText(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "webview-error.log"),
                    DateTime.Now.ToString("HH:mm:ss.fff") + " " + message + Environment.NewLine);
            }
            catch
            {
                // Diagnostics must not affect the application.
            }
        }

        private static string ResolveWebViewDataPath()
        {
            string preferred = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "webview-data");
            try
            {
                Directory.CreateDirectory(preferred);
                return preferred;
            }
            catch
            {
                string fallback = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CountdownApp",
                    "webview-data");
                Directory.CreateDirectory(fallback);
                return fallback;
            }
        }

        private Panel BuildWidgetHeader()
        {
            Panel header = new Panel();
            header.Dock = DockStyle.Fill;
            header.BackColor = WidgetTheme.Background;
            header.Cursor = Cursors.SizeAll;
            header.MouseDown += delegate(object sender, MouseEventArgs e)
            {
                BeginWindowDrag(e);
            };

            RoundedPanel pill = new RoundedPanel();
            pill.Location = AppResources.SP(12, 4);
            pill.Size = AppResources.SS(190, 23);
            pill.CornerRadius = AppResources.S(11);
            pill.BorderColor = WidgetTheme.Border;
            pill.DrawBorder = false;
            pill.BackColor = WidgetTheme.HeaderPill;
            pill.Cursor = Cursors.SizeAll;
            pill.MouseDown += delegate(object sender, MouseEventArgs e)
            {
                BeginWindowDrag(e);
            };
            header.Controls.Add(pill);

            Label title = new Label();
            title.Text = "倒计时";
            title.AutoSize = true;
            title.ForeColor = WidgetTheme.Text;
            title.BackColor = Color.Transparent;
            title.Font = AppResources.Font(8F, FontStyle.Bold);
            title.Location = AppResources.SP(12, 5);
            title.MouseDown += delegate(object sender, MouseEventArgs e)
            {
                BeginWindowDrag(e);
            };
            pill.Controls.Add(title);

            Label back = new Label();
            back.Text = "↗";
            back.AccessibleName = "返回倒计时主界面";
            back.AutoSize = true;
            back.ForeColor = WidgetTheme.Accent;
            back.BackColor = Color.Transparent;
            back.Font = AppResources.Font(11F, FontStyle.Bold);
            back.Cursor = Cursors.Hand;
            back.Location = AppResources.SP(148, 1);
            back.Click += delegate { SwitchToMainMode(); };
            pill.Controls.Add(back);

            Label close = new Label();
            close.Text = "×";
            close.AccessibleName = "关闭";
            close.AutoSize = true;
            close.ForeColor = WidgetTheme.Muted;
            close.BackColor = Color.Transparent;
            close.Font = AppResources.Font(12F, FontStyle.Regular);
            close.Cursor = Cursors.Hand;
            close.Location = AppResources.SP(169, 1);
            close.Click += delegate { Close(); };
            pill.Controls.Add(close);

            return header;
        }

        private Panel BuildActionBar()
        {
            Panel bar = new Panel();
            bar.Dock = DockStyle.Fill;
            bar.BackColor = AppResources.Surface;
            bar.Paint += delegate(object sender, PaintEventArgs e)
            {
                using (Pen pen = new Pen(AppResources.Border))
                {
                    e.Graphics.DrawLine(pen, 0, 0, bar.Width, 0);
                }
            };

            _summaryPanel = new FlowLayoutPanel();
            _summaryPanel.AutoSize = true;
            _summaryPanel.WrapContents = false;
            _summaryPanel.FlowDirection = FlowDirection.LeftToRight;
            _summaryPanel.BackColor = AppResources.Surface;
            bar.Controls.Add(_summaryPanel);

            _countLabel = new StatusPill();
            _countLabel.Text = "0 个倒计时";
            _countLabel.PillColor = AppResources.FieldBackground;
            _countLabel.ForeColor = AppResources.Secondary;
            _countLabel.Margin = AppResources.SPad(0, 4, 12, 0);
            _summaryPanel.Controls.Add(_countLabel);

            Label selectedCaption = new Label();
            selectedCaption.Text = "当前选择";
            selectedCaption.AutoSize = true;
            selectedCaption.ForeColor = AppResources.Muted;
            selectedCaption.Font = AppResources.Font(8.5F, FontStyle.Regular);
            selectedCaption.Margin = AppResources.SPad(0, 9, 8, 0);
            _summaryPanel.Controls.Add(selectedCaption);

            _selectedNameLabel = new Label();
            _selectedNameLabel.Text = "--";
            _selectedNameLabel.AutoSize = true;
            _selectedNameLabel.ForeColor = AppResources.Text;
            _selectedNameLabel.Font = AppResources.Font(10F, FontStyle.Bold);
            _selectedNameLabel.Margin = AppResources.SPad(0, 7, 26, 0);
            _summaryPanel.Controls.Add(_selectedNameLabel);

            _actionPanel = new FlowLayoutPanel();
            _actionPanel.AutoSize = true;
            _actionPanel.WrapContents = false;
            _actionPanel.FlowDirection = FlowDirection.LeftToRight;
            _actionPanel.BackColor = AppResources.Surface;
            bar.Controls.Add(_actionPanel);

            Button widgetToggle = CreateButton(
                "组件",
                76,
                34,
                false,
                AppGlyph.Widget);
            _toolTip.SetToolTip(widgetToggle, "切换到桌面小组件");
            widgetToggle.Click += delegate { SwitchToWidgetMode(); };
            _actionPanel.Controls.Add(widgetToggle);

            Button add = CreateButton("添加", 76, 34, true, AppGlyph.Add);
            add.Click += delegate { AddEvent(); };
            _actionPanel.Controls.Add(add);

            _editButton = CreateButton(string.Empty, 34, 34, false, AppGlyph.Edit);
            _editButton.AccessibleName = "编辑";
            _toolTip.SetToolTip(_editButton, "编辑当前倒计时");
            _editButton.Click += delegate { EditSelected(); };
            _actionPanel.Controls.Add(_editButton);

            _deleteButton = CreateButton(string.Empty, 34, 34, false, AppGlyph.Delete);
            _deleteButton.AccessibleName = "删除";
            _toolTip.SetToolTip(_deleteButton, "删除当前倒计时");
            _deleteButton.Click += delegate { DeleteSelected(); };
            _actionPanel.Controls.Add(_deleteButton);

            _undoButton = CreateButton("撤销", 68, 34, false, AppGlyph.Undo);
            _undoButton.Visible = false;
            _toolTip.SetToolTip(_undoButton, "恢复刚刚删除的倒计时");
            _undoButton.Click += delegate { UndoDelete(); };
            _actionPanel.Controls.Add(_undoButton);

            bar.Resize += delegate { PositionActionBar(bar); };
            return bar;
        }

        private Button CreateButton(string text, int width, int height, bool primary, AppGlyph glyph)
        {
            Button button = new Button();
            button.Text = text;
            button.Width = AppResources.S(width);
            button.Height = AppResources.S(height);
            button.Margin = AppResources.SPad(0, 0, 8, 0);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.BorderColor = AppResources.ButtonBorder;
            button.BackColor = primary ? AppResources.Accent : AppResources.Surface;
            button.ForeColor = primary ? AppResources.OnAccent : AppResources.Text;
            button.Font = AppResources.Font(9F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
            button.TextImageRelation = TextImageRelation.ImageBeforeText;
            button.ImageAlign = ContentAlignment.MiddleLeft;
            button.Padding = primary ? AppResources.SPad(8, 0, 8, 0) : Padding.Empty;
            button.Image = UiIconFactory.Create(glyph, primary ? AppResources.OnAccent : AppResources.Secondary);
            button.MouseEnter += delegate
            {
                button.BackColor = primary ? AppResources.AccentHover : AppResources.FieldBackground;
            };
            button.MouseLeave += delegate
            {
                button.BackColor = primary ? AppResources.Accent : AppResources.Surface;
            };
            return button;
        }

        private void PositionActionBar()
        {
            if (_actionPanel == null || _actionPanel.Parent == null) return;
            PositionActionBar(_actionPanel.Parent);
        }

        private void PositionActionBar(Control bar)
        {
            if (_actionPanel == null || _summaryPanel == null) return;
            _summaryPanel.PerformLayout();
            _actionPanel.PerformLayout();
            int summaryTop = Math.Max(0, (bar.ClientSize.Height - _summaryPanel.Height) / 2);
            int actionTop = Math.Max(0, (bar.ClientSize.Height - _actionPanel.Height) / 2);
            _summaryPanel.Location = new Point(AppResources.S(38), summaryTop);
            _actionPanel.Location = new Point(
                Math.Max(AppResources.S(38), bar.ClientSize.Width - _actionPanel.Width - AppResources.S(38)),
                actionTop);
        }

        private void RefreshView(bool selectNearest)
        {
            DateTime now = DateTime.Now;
            _renderedDate = now.Date;
            List<CountdownEvent> sorted = CountdownEngine.SortForDisplay(_state.Events, now);
            if (selectNearest || FindEvent(_selectedId) == null)
            {
                _selectedId = sorted.Count > 0 ? sorted[0].Id : null;
            }

            _countLabel.Text = _state.Events.Count.ToString() + " 个倒计时";
            CountdownEvent selected = FindEvent(_selectedId);
            _selectedNameLabel.Text = selected == null ? "--" : selected.Name;
            _editButton.Enabled = selected != null;
            _deleteButton.Enabled = selected != null;
            UpdateTrayText(now);
            RefreshWebContent();
        }

        private void RefreshWebContent()
        {
            if (!_webReady) return;
            try
            {
                DateTime now = DateTime.Now;
                List<CountdownEvent> sorted = CountdownEngine.SortForDisplay(_state.Events, now);
                Dictionary<string, object> root = new Dictionary<string, object>();
                root["selectedId"] = _selectedId;

                List<Dictionary<string, object>> events = new List<Dictionary<string, object>>();
                for (int i = 0; i < sorted.Count; i++)
                {
                    CountdownEvent item = sorted[i];
                    CountdownSnapshot snapshot = CountdownEngine.Calculate(item, now);
                    int remainingDays = Math.Max(0, (int)snapshot.Remaining.TotalDays);

                    Dictionary<string, object> value = new Dictionary<string, object>();
                    value["id"] = item.Id;
                    value["name"] = item.Name;
                    value["dateText"] = AppResources.FormatDate(snapshot.TargetDate);
                    value["targetIso"] = snapshot.TargetDate.ToString("yyyy-MM-dd'T'HH:mm:ss");
                    value["ended"] = snapshot.Status == CountdownStatus.Ended;
                    if (snapshot.Status == CountdownStatus.Ended)
                    {
                        value["statusText"] = "已到期";
                        value["statusClass"] = "ended";
                    }
                    else if (snapshot.Status == CountdownStatus.Today)
                    {
                        value["statusText"] = "今天";
                        value["statusClass"] = "today";
                    }
                    else
                    {
                        value["statusText"] = "还有 " + remainingDays.ToString() + " 天";
                        value["statusClass"] = remainingDays <= 7 ? "urgent" : "normal";
                    }
                    events.Add(value);
                }
                root["events"] = events;

                string json = _serializer.Serialize(root).Replace("</", "<\\/");
                string templatePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "web",
                    _widgetMode
                        ? "widget.template.html"
                        : "countdown.template.html");
                string template = File.ReadAllText(templatePath);
                string html = template.Replace("__STATE_JSON__", json);
                _webView.NavigateToString(html);
            }
            catch (Exception ex)
            {
                if (!IsAutomationRun())
                {
                    MessageBox.Show(
                        this,
                        "刷新倒计时界面失败：" + ex.Message,
                        "倒计时",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private void WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                Dictionary<string, object> message =
                    _serializer.DeserializeObject(e.WebMessageAsJson) as Dictionary<string, object>;
                if (message == null) return;
                string type = Convert.ToString(message.ContainsKey("type") ? message["type"] : string.Empty);
                if (type == "select")
                {
                    string id = Convert.ToString(message.ContainsKey("id") ? message["id"] : string.Empty);
                    CountdownEvent selected = FindEvent(id);
                    if (selected != null)
                    {
                        _selectedId = selected.Id;
                        _selectedNameLabel.Text = selected.Name;
                        _editButton.Enabled = true;
                        _deleteButton.Enabled = true;
                        UpdateTrayText(DateTime.Now);
                    }
                }
                else if (type == "add")
                {
                    AddEvent();
                }
                else if (type == "openMain")
                {
                    SwitchToMainMode();
                }
            }
            catch
            {
                // A malformed web message must not stop the countdown window.
            }
        }

        private void AddEvent()
        {
            using (EventEditForm form = new EventEditForm(null))
            {
                if (form.ShowDialog(this) != DialogResult.OK || form.Item == null) return;
                _state.Events.Add(form.Item);
                _selectedId = form.Item.Id;
                SaveState();
                RefreshView(false);
            }
        }

        private void EditSelected()
        {
            CountdownEvent selected = FindEvent(_selectedId);
            if (selected == null) return;

            using (EventEditForm form = new EventEditForm(selected))
            {
                if (form.ShowDialog(this) != DialogResult.OK || form.Item == null) return;
                selected.Name = form.Item.Name;
                selected.Date = form.Item.Date;
                SaveState();
                RefreshView(false);
            }
        }

        private void DeleteSelected()
        {
            CountdownEvent selected = FindEvent(_selectedId);
            if (selected == null) return;
            int index = _state.Events.IndexOf(selected);
            if (index < 0) return;

            _deletedEvent = selected;
            _deletedIndex = index;
            _state.Events.RemoveAt(index);
            _selectedId = null;
            SaveState();
            RefreshView(true);
            _undoTimer.Stop();
            _undoTimer.Start();
            _undoButton.Visible = true;
            PositionActionBar();
        }

        private void UndoDelete()
        {
            if (_deletedEvent == null) return;
            int index = Math.Max(0, Math.Min(_deletedIndex, _state.Events.Count));
            _state.Events.Insert(index, _deletedEvent);
            _selectedId = _deletedEvent.Id;
            _deletedEvent = null;
            _undoTimer.Stop();
            _undoButton.Visible = false;
            PositionActionBar();
            SaveState();
            RefreshView(false);
        }

        private void UndoTimer_Tick(object sender, EventArgs e)
        {
            _undoTimer.Stop();
            _deletedEvent = null;
            _undoButton.Visible = false;
            PositionActionBar();
        }

        private void SaveState()
        {
            _state.Initialized = true;
            _state.UpdatedAt = DateTime.Now;
            if (_demoMode) return;

            try
            {
                _store.Save(_state);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    "保存事件失败：" + ex.Message,
                    "倒计时",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private CountdownEvent FindEvent(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < _state.Events.Count; i++)
            {
                CountdownEvent item = _state.Events[i];
                if (item != null && string.Equals(item.Id, id, StringComparison.Ordinal)) return item;
            }
            return null;
        }

        private void UiTimer_Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            if (now.Date != _renderedDate)
            {
                RefreshView(false);
                return;
            }
            UpdateTrayText(now);
        }

        private void UpdateTrayText(DateTime now)
        {
            CountdownEvent selected = FindEvent(_selectedId);
            if (selected == null)
            {
                _tray.Text = "倒计时";
                return;
            }

            CountdownSnapshot snapshot = CountdownEngine.Calculate(selected, now);
            string text = "倒计时 · " + selected.Name;
            if (snapshot.Status == CountdownStatus.Ended) text += " · 已到期";
            else if (snapshot.Status == CountdownStatus.Today) text += " · 今天";
            else text += " · " + Math.Max(0, (int)snapshot.Remaining.TotalDays).ToString() + " 天";
            _tray.Text = text.Length > 63 ? text.Substring(0, 63) : text;
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.N)
            {
                AddEvent();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.F2)
            {
                EditSelected();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Delete)
            {
                DeleteSelected();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void BuildTray()
        {
            _trayMenu = new ContextMenuStrip();
            ToolStripMenuItem show = new ToolStripMenuItem("显示倒计时");
            show.Click += delegate { ShowFromTray(); };
            _trayMenu.Items.Add(show);

            ToolStripMenuItem desktopWidget =
                new ToolStripMenuItem("切换到小组件");
            desktopWidget.Click += delegate { SwitchToWidgetMode(); };
            _trayMenu.Items.Add(desktopWidget);
            _trayMenu.Items.Add(new ToolStripSeparator());
            ToolStripMenuItem exit = new ToolStripMenuItem("退出");
            exit.Click += delegate { ExitApplication(); };
            _trayMenu.Items.Add(exit);

            _tray = new NotifyIcon();
            _tray.Icon = _trayIcon;
            _tray.Text = "倒计时";
            _tray.ContextMenuStrip = _trayMenu;
            _tray.Visible = true;
            _tray.DoubleClick += delegate { ShowFromTray(); };
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_allowExit) return;
            if (e.CloseReason == CloseReason.WindowsShutDown
                || e.CloseReason == CloseReason.TaskManagerClosing)
            {
                _allowExit = true;
                return;
            }

            SaveWidgetPosition();
            e.Cancel = true;
            Hide();
            if (!_trayHintShown)
            {
                _trayHintShown = true;
                _tray.BalloonTipTitle = "倒计时仍在运行";
                _tray.BalloonTipText = "程序已缩小到任务栏托盘，双击图标可重新打开。";
                _tray.BalloonTipIcon = ToolTipIcon.Info;
                _tray.ShowBalloonTip(3000);
            }
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (_uiTimer != null) _uiTimer.Stop();
            if (_undoTimer != null) _undoTimer.Stop();
            if (_widgetLayerTimer != null) _widgetLayerTimer.Stop();
            if (_webView != null) _webView.Dispose();
            if (_tray != null)
            {
                _tray.Visible = false;
                _tray.Dispose();
            }
            if (_trayMenu != null) _trayMenu.Dispose();
            if (_appIcon != null) _appIcon.Dispose();
            if (_trayIcon != null) _trayIcon.Dispose();
        }

        private void ShowFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
            BringToFront();
        }

        private void ExitApplication()
        {
            _allowExit = true;
            if (_tray != null) _tray.Visible = false;
            Close();
        }

        private void SwitchToWidgetMode()
        {
            if (_widgetMode) return;

            _mainBounds = Bounds;
            _widgetMode = true;
            _widgetLayerTimer.Start();

            _rootLayout.RowStyles[0].Height = AppResources.SF(30F);
            _rootLayout.RowStyles[2].Height = 0F;
            _widgetHeader.Visible = true;
            _actionBar.Visible = false;
            _workspace.Padding = AppResources.SPad(8, 4, 8, 8);
            FormBorderStyle = FormBorderStyle.None;
            MinimumSize = Size.Empty;
            MaximumSize = Size.Empty;
            Size = new Size(620, 640);

            Rectangle work = Screen.PrimaryScreen.WorkingArea;
            int x = _widgetSettings.HasPosition
                ? _widgetSettings.X
                : work.Right - Width - AppResources.S(34);
            int y = _widgetSettings.HasPosition
                ? _widgetSettings.Y
                : work.Bottom - Height - AppResources.S(34);
            if (x < work.Left || y < work.Top
                || x + Width > work.Right || y + Height > work.Bottom)
            {
                x = work.Right - Width - AppResources.S(34);
                y = work.Bottom - Height - AppResources.S(34);
            }
            Location = new Point(x, y);
            RefreshWebContent();
            DesktopLayerHost.PlaceBehindAppsAndAboveWallpaper(Handle);
        }

        private void SwitchToMainMode()
        {
            if (!_widgetMode) return;

            SaveWidgetPosition();
            _widgetMode = false;
            _widgetLayerTimer.Stop();
            _rootLayout.RowStyles[0].Height = 0F;
            _rootLayout.RowStyles[2].Height = AppResources.SF(76F);
            _widgetHeader.Visible = false;
            _actionBar.Visible = true;
            _workspace.Padding = AppResources.SPad(30, 28, 34, 22);
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = _mainMinimumSize;
            MaximumSize = Size.Empty;
            Bounds = _mainBounds;
            Activate();
            RefreshWebContent();
        }

        private void SaveWidgetPosition()
        {
            if (!_widgetMode || !IsHandleCreated) return;
            _widgetSettings.X = Left;
            _widgetSettings.Y = Top;
            _widgetSettings.HasPosition = true;
            try
            {
                _widgetSettingsStore.Save(_widgetSettings);
            }
            catch
            {
                // Position persistence must not affect the mode switch.
            }
        }

        private void BeginWindowDrag(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            ReleaseCapture();
            SendMessage(Handle, 0x00A1, new IntPtr(2), IntPtr.Zero);
        }


        public void CaptureToFile(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("path");
            Activate();
            BringToFront();
            Application.DoEvents();
            using (Bitmap bitmap = new Bitmap(Width, Height))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    if (!PrintWindow(Handle, hdc, 2))
                    {
                        throw new InvalidOperationException("PrintWindow failed.");
                    }
                }
                finally
                {
                    graphics.ReleaseHdc(hdc);
                }
                bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        public void CloseForAutomation()
        {
            _allowExit = true;
            Close();
        }

        public async Task CaptureWebViewToFileAsync(string path)
        {
            if (!_webReady || _webView == null || _webView.CoreWebView2 == null)
            {
                throw new InvalidOperationException("WebView2 is not ready.");
            }
            using (FileStream stream = File.Create(path))
            {
                await _webView.CoreWebView2.CapturePreviewAsync(
                    CoreWebView2CapturePreviewImageFormat.Png,
                    stream);
            }
        }

        public bool HasWebView
        {
            get { return _webView != null; }
        }

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(
            IntPtr hWnd,
            int message,
            IntPtr wParam,
            IntPtr lParam);

        public async Task<string> GetWebDiagnosticsAsync()
        {
            if (!_webReady || _webView == null || _webView.CoreWebView2 == null)
            {
                return "WebView2 is not ready.";
            }
            return await _webView.CoreWebView2.ExecuteScriptAsync(
                "JSON.stringify({ready:document.readyState, ticks:document.querySelectorAll('.tick').length, cards:document.querySelectorAll('.card').length, text:document.body.innerText.slice(0,500)})");
        }

        private static bool IsAutomationRun()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] != null && args[i].StartsWith("--screenshot=", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        [DllImport("user32.dll")]
        private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint flags);
    }
}
