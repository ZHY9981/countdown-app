using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        private readonly UiSettingsStore _uiSettingsStore;
        private readonly UiSettings _uiSettings;

        private WebView2 _webView;
        private StatusPill _countLabel;
        private Label _selectedNameLabel;
        private Button _editButton;
        private Button _deleteButton;
        private Button _undoButton;
        private Button _addButton;
        private Button _themeDarkButton;
        private Button _themeLightButton;
        private Panel _themeTogglePanel;
        private Panel _actionDivider1;
        private Panel _actionDivider2;
        private Label _selectedCaptionLabel;
        private Panel _summaryPanel;
        private FlowLayoutPanel _actionPanel;
        private Panel _actionBar;
        private Panel _widgetHeader;
        private RoundedPanel _widgetDragPill;
        private RoundedPanel _widgetBackButton;
        private RoundedPanel _widgetCloseButton;
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
        private bool _darkTheme;
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
            _uiSettingsStore = new UiSettingsStore();
            _uiSettings = _uiSettingsStore.Load();
            _darkTheme = _uiSettings.DarkTheme;
            LogWebView("Main theme: " + (_darkTheme ? "dark" : "light"));
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
            BackColor = CurrentTheme.Background;
            Font = AppResources.Font(9F, FontStyle.Regular);
            KeyPreview = true;

            BuildInterface();
            BuildTray();

            Load += MainForm_Load;
            Shown += delegate
            {
                MainWindowTheme.Apply(Handle, _darkTheme);
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

        private MainThemePalette CurrentTheme
        {
            get { return _darkTheme ? MainTheme.Dark : MainTheme.Light; }
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
            _workspace.BackColor = CurrentTheme.Background;
            _workspace.Padding = AppResources.SPad(30, 28, 34, 22);
            _rootLayout.Controls.Add(_workspace, 0, 1);

            _webView = new WebView2();
            _webView.Dock = DockStyle.Fill;
            _webView.DefaultBackgroundColor = CurrentTheme.Background;
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

            _widgetDragPill = new RoundedPanel();
            _widgetDragPill.CornerRadius = AppResources.S(20);
            _widgetDragPill.BorderColor = WidgetTheme.Border;
            _widgetDragPill.DrawBorder = true;
            _widgetDragPill.BackColor = WidgetTheme.HeaderPill;
            _widgetDragPill.Cursor = Cursors.SizeAll;
            _widgetDragPill.MouseDown += delegate(object sender, MouseEventArgs e)
            {
                BeginWindowDrag(e);
            };
            header.Controls.Add(_widgetDragPill);

            Label dragDots = new Label();
            dragDots.Text = "···";
            dragDots.Dock = DockStyle.Left;
            dragDots.Width = AppResources.S(32);
            dragDots.TextAlign = ContentAlignment.MiddleCenter;
            dragDots.ForeColor = WidgetTheme.Muted;
            dragDots.BackColor = Color.Transparent;
            dragDots.Font = AppResources.Font(9F, FontStyle.Bold);
            dragDots.Cursor = Cursors.SizeAll;
            dragDots.MouseDown += delegate(object sender, MouseEventArgs e)
            {
                BeginWindowDrag(e);
            };
            _widgetDragPill.Controls.Add(dragDots);

            Label title = new Label();
            title.Text = "倒计时";
            title.Dock = DockStyle.Fill;
            title.TextAlign = ContentAlignment.MiddleLeft;
            title.Padding = AppResources.SPad(34, 0, 0, 0);
            title.ForeColor = WidgetTheme.Text;
            title.BackColor = Color.Transparent;
            title.Font = AppResources.Font(9F, FontStyle.Bold);
            title.MouseDown += delegate(object sender, MouseEventArgs e)
            {
                BeginWindowDrag(e);
            };
            _widgetDragPill.Controls.Add(title);

            _widgetBackButton = CreateWidgetHeaderButton(
                "↗",
                "返回倒计时主界面",
                WidgetTheme.Accent,
                delegate { SwitchToMainMode(); });
            header.Controls.Add(_widgetBackButton);

            _widgetCloseButton = CreateWidgetHeaderButton(
                "×",
                "关闭",
                WidgetTheme.Muted,
                delegate { Close(); });
            header.Controls.Add(_widgetCloseButton);

            header.Resize += delegate { PositionWidgetHeaderControls(); };

            return header;
        }

        private RoundedPanel CreateWidgetHeaderButton(
            string glyph,
            string accessibleName,
            Color foreground,
            EventHandler click)
        {
            RoundedPanel button = new RoundedPanel();
            button.CornerRadius = AppResources.S(8);
            button.BorderColor = WidgetTheme.BorderSoft;
            button.DrawBorder = true;
            button.BackColor = WidgetTheme.HeaderButton;
            button.Cursor = Cursors.Hand;
            button.AccessibleName = accessibleName;

            Label label = new Label();
            label.Text = glyph;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.ForeColor = foreground;
            label.BackColor = Color.Transparent;
            label.Font = AppResources.Font(12F, FontStyle.Bold);
            label.Cursor = Cursors.Hand;
            button.Controls.Add(label);

            button.Click += click;
            label.Click += click;
            button.MouseEnter += delegate { button.BackColor = WidgetTheme.HeaderButtonHover; };
            button.MouseLeave += delegate { button.BackColor = WidgetTheme.HeaderButton; };
            label.MouseEnter += delegate { button.BackColor = WidgetTheme.HeaderButtonHover; };
            label.MouseLeave += delegate { button.BackColor = WidgetTheme.HeaderButton; };
            return button;
        }

        private void PositionWidgetHeaderControls()
        {
            if (_widgetHeader == null
                || _widgetDragPill == null
                || _widgetBackButton == null
                || _widgetCloseButton == null) return;

            int padding = AppResources.S(10);
            int gap = AppResources.S(8);
            int buttonSize = AppResources.S(40);
            int buttonTop = Math.Max(0, (_widgetHeader.ClientSize.Height - buttonSize) / 2);
            int closeX = Math.Max(padding, _widgetHeader.ClientSize.Width - padding - buttonSize);
            int backX = Math.Max(padding, closeX - gap - buttonSize);
            int pillWidth = Math.Max(AppResources.S(180), backX - padding - gap);

            _widgetDragPill.SetBounds(
                padding,
                buttonTop,
                pillWidth,
                buttonSize);
            _widgetBackButton.SetBounds(backX, buttonTop, buttonSize, buttonSize);
            _widgetCloseButton.SetBounds(closeX, buttonTop, buttonSize, buttonSize);
        }

        private Panel BuildActionBar()
        {
            Panel bar = new Panel();
            bar.Dock = DockStyle.Fill;
            bar.BackColor = CurrentTheme.ActionBar;
            bar.Paint += delegate(object sender, PaintEventArgs e)
            {
                using (Pen pen = new Pen(CurrentTheme.Border))
                {
                    e.Graphics.DrawLine(pen, 0, 0, bar.Width, 0);
                }
            };

            _summaryPanel = new Panel();
            _summaryPanel.AutoSize = false;
            _summaryPanel.BackColor = CurrentTheme.ActionBar;
            bar.Controls.Add(_summaryPanel);

            _countLabel = new StatusPill();
            _countLabel.Text = "0 个倒计时";
            _countLabel.PillColor = CurrentTheme.ActionButton;
            _countLabel.ForeColor = CurrentTheme.Muted;
            _countLabel.Margin = Padding.Empty;
            _summaryPanel.Controls.Add(_countLabel);

            _selectedCaptionLabel = new Label();
            _selectedCaptionLabel.Text = "当前选择";
            _selectedCaptionLabel.AutoSize = true;
            _selectedCaptionLabel.ForeColor = CurrentTheme.Muted;
            _selectedCaptionLabel.Font = AppResources.Font(8.5F, FontStyle.Regular);
            _selectedCaptionLabel.Margin = Padding.Empty;
            _summaryPanel.Controls.Add(_selectedCaptionLabel);

            _selectedNameLabel = new Label();
            _selectedNameLabel.Text = "--";
            _selectedNameLabel.AutoSize = true;
            _selectedNameLabel.ForeColor = CurrentTheme.Text;
            _selectedNameLabel.Font = AppResources.Font(10F, FontStyle.Bold);
            _selectedNameLabel.Margin = Padding.Empty;
            _summaryPanel.Controls.Add(_selectedNameLabel);

            _actionPanel = new FlowLayoutPanel();
            _actionPanel.AutoSize = true;
            _actionPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _actionPanel.WrapContents = false;
            _actionPanel.FlowDirection = FlowDirection.LeftToRight;
            _actionPanel.BackColor = CurrentTheme.ActionBar;
            bar.Controls.Add(_actionPanel);

            _themeTogglePanel = new Panel();
            _themeTogglePanel.Width = AppResources.S(124);
            _themeTogglePanel.Height = AppResources.S(34);
            _themeTogglePanel.Margin = AppResources.SPad(0, 0, 8, 0);
            _themeTogglePanel.BackColor = CurrentTheme.ActionButton;
            _themeTogglePanel.Paint += delegate(object sender, PaintEventArgs e)
            {
                using (Pen pen = new Pen(CurrentTheme.Border))
                using (GraphicsPath path = RoundedPanel.CreateRoundedPath(
                    new Rectangle(0, 0, _themeTogglePanel.Width - 1, _themeTogglePanel.Height - 1),
                    AppResources.S(7)))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.DrawPath(pen, path);
                }
            };
            _actionPanel.Controls.Add(_themeTogglePanel);

            _themeDarkButton = CreateThemeButton("☾ 深色", 58);
            _themeDarkButton.Click += delegate { SetDarkTheme(true); };
            _themeTogglePanel.Controls.Add(_themeDarkButton);

            _themeLightButton = CreateThemeButton("☀ 浅色", 58);
            _themeLightButton.Location = AppResources.SP(60, 3);
            _themeLightButton.Click += delegate { SetDarkTheme(false); };
            _themeTogglePanel.Controls.Add(_themeLightButton);
            ApplyThemeButtonStyles();

            _actionDivider1 = CreateActionDivider();
            _actionPanel.Controls.Add(_actionDivider1);

            _editButton = CreateButton("编辑", 68, 34, false, AppGlyph.Edit);
            _editButton.AccessibleName = "编辑";
            _toolTip.SetToolTip(_editButton, "编辑当前倒计时");
            _editButton.Click += delegate { EditSelected(); };
            _actionPanel.Controls.Add(_editButton);

            _deleteButton = CreateButton("删除", 68, 34, false, AppGlyph.Delete);
            _deleteButton.AccessibleName = "删除";
            _toolTip.SetToolTip(_deleteButton, "删除当前倒计时");
            _deleteButton.Click += delegate { DeleteSelected(); };
            _actionPanel.Controls.Add(_deleteButton);

            _undoButton = CreateButton("撤销", 68, 34, false, AppGlyph.Undo);
            _undoButton.Visible = false;
            _toolTip.SetToolTip(_undoButton, "恢复刚刚删除的倒计时");
            _undoButton.Click += delegate { UndoDelete(); };
            _actionPanel.Controls.Add(_undoButton);

            _actionDivider2 = CreateActionDivider();
            _actionPanel.Controls.Add(_actionDivider2);

            _addButton = CreateButton("添加", 76, 34, true, AppGlyph.Add);
            _addButton.Click += delegate { AddEvent(); };
            _actionPanel.Controls.Add(_addButton);

            bar.Resize += delegate { PositionActionBar(bar); };
            return bar;
        }

        private Panel CreateActionDivider()
        {
            Panel divider = new Panel();
            divider.Width = AppResources.S(1);
            divider.Height = AppResources.S(24);
            divider.Margin = AppResources.SPad(5, 5, 9, 0);
            divider.BackColor = CurrentTheme.Border;
            divider.Tag = "action-divider";
            return divider;
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
            button.FlatAppearance.BorderColor = CurrentTheme.Border;
            button.BackColor = primary ? CurrentTheme.Accent : CurrentTheme.ActionButton;
            button.ForeColor = primary ? CurrentTheme.OnAccent : CurrentTheme.Text;
            button.Font = AppResources.Font(9F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
            button.TextImageRelation = TextImageRelation.ImageBeforeText;
            button.ImageAlign = ContentAlignment.MiddleLeft;
            button.Padding = primary ? AppResources.SPad(8, 0, 8, 0) : Padding.Empty;
            button.Image = UiIconFactory.Create(
                glyph,
                primary ? CurrentTheme.OnAccent : CurrentTheme.Muted);
            button.MouseEnter += delegate
            {
                button.BackColor = primary
                    ? CurrentTheme.AccentHover
                    : CurrentTheme.ActionButtonHover;
            };
            button.MouseLeave += delegate
            {
                button.BackColor = primary
                    ? CurrentTheme.Accent
                    : CurrentTheme.ActionButton;
            };
            return button;
        }

        private Button CreateThemeButton(string text, int width)
        {
            Button button = new Button();
            button.Text = text;
            button.Width = AppResources.S(width);
            button.Height = AppResources.S(28);
            button.Location = AppResources.SP(3, 3);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Font = AppResources.Font(8.5F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
            button.MouseEnter += delegate { ApplyThemeButtonStyles(); };
            button.MouseLeave += delegate { ApplyThemeButtonStyles(); };
            return button;
        }

        private void SetDarkTheme(bool dark)
        {
            if (_darkTheme == dark) return;

            _darkTheme = dark;
            _uiSettings.DarkTheme = dark;
            try
            {
                _uiSettingsStore.Save(_uiSettings);
            }
            catch
            {
                // Theme persistence must not block switching.
            }
            ApplyMainTheme();
        }

        private void ApplyMainTheme()
        {
            MainThemePalette theme = CurrentTheme;
            BackColor = theme.Background;
            _workspace.BackColor = theme.Background;
            _webView.DefaultBackgroundColor = theme.Background;
            _actionBar.BackColor = theme.ActionBar;
            _summaryPanel.BackColor = theme.ActionBar;
            _actionPanel.BackColor = theme.ActionBar;
            _countLabel.PillColor = theme.ActionButton;
            _countLabel.ForeColor = theme.Muted;
            _selectedCaptionLabel.ForeColor = theme.Muted;
            _selectedNameLabel.ForeColor = theme.Text;

            SetActionButtonTheme(_addButton, true, AppGlyph.Add);
            SetActionButtonTheme(_editButton, false, AppGlyph.Edit);
            SetActionButtonTheme(_deleteButton, false, AppGlyph.Delete);
            SetActionButtonTheme(_undoButton, false, AppGlyph.Undo);
            _themeTogglePanel.BackColor = theme.ActionButton;
            _actionDivider1.BackColor = theme.Border;
            _actionDivider2.BackColor = theme.Border;
            ApplyThemeButtonStyles();

            MainWindowTheme.Apply(Handle, _darkTheme);
            _actionBar.Invalidate();
            _themeTogglePanel.Invalidate();
            PositionActionBar();
            RefreshWebContent();
        }

        private void ApplyThemeButtonStyles()
        {
            if (_themeDarkButton == null || _themeLightButton == null) return;

            MainThemePalette theme = CurrentTheme;
            StyleThemeButton(_themeDarkButton, _darkTheme, theme);
            StyleThemeButton(_themeLightButton, !_darkTheme, theme);
        }

        private static void StyleThemeButton(
            Button button,
            bool selected,
            MainThemePalette theme)
        {
            button.BackColor = selected ? theme.Accent : theme.ActionButton;
            button.ForeColor = selected ? theme.OnAccent : theme.Muted;
            button.FlatAppearance.MouseOverBackColor = selected
                ? theme.AccentHover
                : theme.ActionButtonHover;
        }

        private void SetActionButtonTheme(
            Button button,
            bool primary,
            AppGlyph glyph)
        {
            if (button == null) return;

            MainThemePalette theme = CurrentTheme;
            button.BackColor = primary ? theme.Accent : theme.ActionButton;
            button.ForeColor = primary ? theme.OnAccent : theme.Text;
            button.FlatAppearance.BorderColor = theme.Border;
            button.FlatAppearance.MouseOverBackColor = primary
                ? theme.AccentHover
                : theme.ActionButtonHover;
            if (button.Image != null)
            {
                Image old = button.Image;
                button.Image = UiIconFactory.Create(
                    glyph,
                    primary ? theme.OnAccent : theme.Muted);
                old.Dispose();
            }
        }

        private void PositionActionBar()
        {
            if (_actionPanel == null || _actionPanel.Parent == null) return;
            PositionActionBar(_actionPanel.Parent);
        }

        private void PositionActionBar(Control bar)
        {
            if (_actionPanel == null || _summaryPanel == null) return;
            _actionPanel.PerformLayout();
            PositionSummaryPanel();
            int summaryTop = Math.Max(0, (bar.ClientSize.Height - _summaryPanel.Height) / 2);
            int actionTop = Math.Max(0, (bar.ClientSize.Height - _actionPanel.Height) / 2);
            _summaryPanel.Location = new Point(AppResources.S(38), summaryTop);
            _actionPanel.Location = new Point(
                Math.Max(AppResources.S(38), bar.ClientSize.Width - _actionPanel.Width - AppResources.S(38)),
                actionTop);
        }

        private void PositionSummaryPanel()
        {
            if (_summaryPanel == null
                || _countLabel == null
                || _selectedCaptionLabel == null
                || _selectedNameLabel == null) return;

            int gap = AppResources.S(10);
            int groupGap = AppResources.S(8);
            int height = Math.Max(
                _countLabel.PreferredHeight,
                Math.Max(
                    _selectedCaptionLabel.PreferredHeight,
                    _selectedNameLabel.PreferredHeight));
            height = Math.Max(AppResources.S(30), height);
            _summaryPanel.Height = height;

            int x = 0;
            _countLabel.Location = new Point(
                x,
                Math.Max(0, (height - _countLabel.PreferredHeight) / 2));
            _countLabel.Width = _countLabel.PreferredWidth;
            _countLabel.Height = _countLabel.PreferredHeight;
            x = _countLabel.Right + gap;

            _selectedCaptionLabel.Location = new Point(
                x,
                Math.Max(0, (height - _selectedCaptionLabel.PreferredHeight) / 2));
            _selectedCaptionLabel.Width = _selectedCaptionLabel.PreferredWidth;
            _selectedCaptionLabel.Height = _selectedCaptionLabel.PreferredHeight;
            x = _selectedCaptionLabel.Right + groupGap;

            _selectedNameLabel.Location = new Point(
                x,
                Math.Max(0, (height - _selectedNameLabel.PreferredHeight) / 2));
            _selectedNameLabel.Width = _selectedNameLabel.PreferredWidth;
            _selectedNameLabel.Height = _selectedNameLabel.PreferredHeight;
            _summaryPanel.Width = _selectedNameLabel.Right;
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
            UpdateSelectedName(selected == null ? "--" : selected.Name);
            _editButton.Enabled = selected != null;
            _deleteButton.Enabled = selected != null;
            UpdateTrayText(now);
            RefreshWebContent();
        }

        private void UpdateSelectedName(string name)
        {
            _selectedNameLabel.Text = name;
            _toolTip.SetToolTip(_selectedNameLabel, name);
            PositionActionBar();
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
                root["darkTheme"] = _darkTheme;

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
                    value["targetTimeText"] =
                        snapshot.TargetDate.ToString("yyyy-MM-dd HH:mm");
                    value["startTimeText"] = item.CreatedAt == DateTime.MinValue
                        ? "起始时间未记录"
                        : item.CreatedAt.ToString("yyyy-MM-dd HH:mm");
                    value["currentTimeText"] = now.ToString("yyyy-MM-dd HH:mm");
                    int progressPercent;
                    value["progressPercent"] =
                        CountdownEngine.TryCalculateProgress(item, now, out progressPercent)
                            ? progressPercent
                            : -1;
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
                LogWebView("Web message: " + type);
                if (type == "select")
                {
                    string id = Convert.ToString(message.ContainsKey("id") ? message["id"] : string.Empty);
                    CountdownEvent selected = FindEvent(id);
                    if (selected != null)
                    {
                        _selectedId = selected.Id;
                        UpdateSelectedName(selected.Name);
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
                else if (type == "widget")
                {
                    SwitchToWidgetMode();
                }
            }
            catch
            {
                // A malformed web message must not stop the countdown window.
            }
        }

        private void AddEvent()
        {
            using (EventEditForm form = new EventEditForm(null, _darkTheme))
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

            using (EventEditForm form = new EventEditForm(selected, _darkTheme))
            {
                DialogResult result = form.ShowDialog(this);
                if (form.DeleteRequested)
                {
                    _selectedId = selected.Id;
                    DeleteSelected();
                    return;
                }
                if (result != DialogResult.OK || form.Item == null) return;
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

            _rootLayout.RowStyles[0].Height = AppResources.SF(52F);
            _rootLayout.RowStyles[2].Height = 0F;
            _widgetHeader.Visible = true;
            _actionBar.Visible = false;
            _workspace.Padding = AppResources.SPad(0, 0, 0, 0);
            _workspace.BackColor = WidgetTheme.Background;
            _widgetHeader.BackColor = WidgetTheme.Background;
            BackColor = WidgetTheme.Background;
            _webView.DefaultBackgroundColor = WidgetTheme.Background;
            FormBorderStyle = FormBorderStyle.None;
            MinimumSize = Size.Empty;
            MaximumSize = Size.Empty;
            Size = new Size(620, 680);

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
            PositionWidgetHeaderControls();
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
            _widgetHeader.BackColor = WidgetTheme.Background;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = _mainMinimumSize;
            MaximumSize = Size.Empty;
            Bounds = _mainBounds;
            Activate();
            ApplyMainTheme();
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
                @"JSON.stringify({
                    ready:document.readyState,
                    ticks:document.querySelectorAll('.tick').length,
                    cards:document.querySelectorAll('.card').length,
                    theme:document.documentElement.getAttribute('data-theme'),
                    viewport:{width:innerWidth,height:innerHeight},
                    hero:(function(){
                        var card=document.querySelector('.card.hero');
                        if(!card)return null;
                        var rect=card.getBoundingClientRect();
                        return {top:rect.top,height:rect.height};
                    })(),
                    compactRows:Array.prototype.map.call(document.querySelectorAll('.compact'),function(card){
                        var head=card.querySelector('.card-head');
                        var clock=card.querySelector('.clock');
                        var tick=card.querySelector('.countdown-tick');
                        var layout=card.querySelector('[data-layout]');
                        var groups=card.querySelectorAll('.tick-group');
                        var rect=card.getBoundingClientRect();
                        return {
                            top:rect.top,
                            height:rect.height,
                            card:card.getBoundingClientRect().width,
                            head:head?head.getBoundingClientRect().width:0,
                            clock:clock?clock.getBoundingClientRect().width:0,
                            tick:tick?tick.getBoundingClientRect().width:0,
                            layout:layout?layout.getBoundingClientRect().width:0,
                            groups:groups.length,
                            groupWidths:Array.prototype.map.call(groups,function(group){return group.getBoundingClientRect().width;})
                        };
                    }),
                    text:document.body.innerText.slice(0,500)
                })");
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
