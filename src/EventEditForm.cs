using System;
using System.Drawing;
using System.Windows.Forms;

namespace CountdownApp
{
    public class EventEditForm : Form
    {
        private readonly bool _isNew;
        private readonly CountdownEvent _source;
        private readonly bool _darkTheme;
        private readonly MainThemePalette _theme;
        private TextBox _nameTextBox;
        private TextBox _dateTextBox;
        private Label _hintLabel;
        private Label _previewName;
        private Label _previewDate;
        private Label _previewStatus;

        public CountdownEvent Item { get; private set; }
        public bool DeleteRequested { get; private set; }

        public EventEditForm(CountdownEvent existing, bool darkTheme)
        {
            _isNew = existing == null;
            _source = existing;
            _darkTheme = darkTheme;
            _theme = darkTheme ? MainTheme.Dark : MainTheme.Light;

            Text = _isNew ? "添加倒计时" : "编辑倒计时";
            Icon = AppResources.CreateAppIcon();
            AutoScaleMode = AutoScaleMode.None;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = AppResources.SS(700, 470);
            BackColor = _theme.Background;
            Font = AppResources.Font(9F, FontStyle.Regular);
            Load += delegate { MainWindowTheme.Apply(Handle, _darkTheme); };

            BuildInterface();
            LoadItem();
        }

        private void BuildInterface()
        {
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, AppResources.SF(68F)));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, AppResources.SF(72F)));
            Controls.Add(root);

            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildContent(), 0, 1);
            root.Controls.Add(BuildFooter(), 0, 2);
        }

        private Panel BuildHeader()
        {
            Panel header = new Panel();
            header.Dock = DockStyle.Fill;
            header.BackColor = _theme.ActionBar;

            Label title = new Label();
            title.Text = _isNew ? "添加倒计时事件" : "编辑倒计时事件";
            title.ForeColor = _theme.Text;
            title.Font = AppResources.Font(16F, FontStyle.Bold);
            title.AutoSize = true;
            title.Location = AppResources.SP(24, 13);
            header.Controls.Add(title);

            Label subtitle = new Label();
            subtitle.Text = "事件名称和日期保存在本机，保存后立即更新主界面和小组件";
            subtitle.ForeColor = _theme.Muted;
            subtitle.Font = AppResources.Font(8.5F, FontStyle.Regular);
            subtitle.AutoSize = true;
            subtitle.Location = AppResources.SP(25, 43);
            header.Controls.Add(subtitle);
            return header;
        }

        private TableLayoutPanel BuildContent()
        {
            TableLayoutPanel content = new TableLayoutPanel();
            content.Dock = DockStyle.Fill;
            content.Padding = AppResources.SPad(24, 18, 24, 10);
            content.ColumnCount = 2;
            content.RowCount = 1;
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            content.Controls.Add(BuildFields(), 0, 0);
            content.Controls.Add(BuildPreview(), 1, 0);
            return content;
        }

        private TableLayoutPanel BuildFields()
        {
            TableLayoutPanel form = new TableLayoutPanel();
            form.Dock = DockStyle.Fill;
            form.Margin = AppResources.SPad(0, 0, 18, 0);
            form.ColumnCount = 1;
            form.RowCount = 6;
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, AppResources.SF(20F)));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, AppResources.SF(44F)));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, AppResources.SF(28F)));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, AppResources.SF(20F)));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, AppResources.SF(44F)));
            form.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            form.Controls.Add(CreateFieldLabel("事件名称"), 0, 0);
            _nameTextBox = new TextBox();
            _nameTextBox.Dock = DockStyle.Fill;
            _nameTextBox.Font = AppResources.Font(10F, FontStyle.Regular);
            _nameTextBox.BorderStyle = BorderStyle.FixedSingle;
            _nameTextBox.BackColor = _theme.Field;
            _nameTextBox.ForeColor = _theme.Text;
            _nameTextBox.MaxLength = 80;
            _nameTextBox.TextChanged += delegate
            {
                _hintLabel.Text = string.Empty;
                UpdatePreview();
            };
            form.Controls.Add(_nameTextBox, 0, 1);
            form.Controls.Add(CreateHelperLabel("最多 80 个字符"), 0, 2);

            form.Controls.Add(CreateFieldLabel("目标日期"), 0, 3);
            TableLayoutPanel dateRow = new TableLayoutPanel();
            dateRow.Dock = DockStyle.Fill;
            dateRow.ColumnCount = 2;
            dateRow.RowCount = 1;
            dateRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            dateRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, AppResources.SF(84F)));

            _dateTextBox = new TextBox();
            _dateTextBox.Dock = DockStyle.Fill;
            _dateTextBox.Font = AppResources.Font(10F, FontStyle.Regular);
            _dateTextBox.BorderStyle = BorderStyle.FixedSingle;
            _dateTextBox.BackColor = _theme.Field;
            _dateTextBox.ForeColor = _theme.Text;
            _dateTextBox.MaxLength = 10;
            _dateTextBox.TextChanged += delegate
            {
                _hintLabel.Text = string.Empty;
                UpdatePreview();
            };
            dateRow.Controls.Add(_dateTextBox, 0, 0);

            Button dateButton = CreateCommandButton(
                "选择",
                76,
                _theme.ActionButton,
                _theme.Text,
                true);
            dateButton.Image = UiIconFactory.Create(AppGlyph.Clock, _theme.Muted);
            dateButton.Margin = AppResources.SPad(8, 0, 0, 0);
            dateButton.Click += delegate { OpenCalendar(); };
            dateRow.Controls.Add(dateButton, 1, 0);
            form.Controls.Add(dateRow, 0, 4);

            _hintLabel = CreateHelperLabel("倒计时按目标日期的本地零点计算。");
            form.Controls.Add(_hintLabel, 0, 5);
            return form;
        }

        private RoundedPanel BuildPreview()
        {
            RoundedPanel preview = new RoundedPanel();
            preview.Dock = DockStyle.Fill;
            preview.Margin = Padding.Empty;
            preview.CornerRadius = AppResources.S(8);
            preview.BorderColor = _theme.Border;
            preview.DrawBorder = true;
            preview.BackColor = _theme.ActionButton;

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = AppResources.SPad(16, 16, 16, 16);
            layout.ColumnCount = 1;
            layout.RowCount = 5;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, AppResources.SF(22F)));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, AppResources.SF(38F)));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, AppResources.SF(24F)));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, AppResources.SF(54F)));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            preview.Controls.Add(layout);

            Label kicker = new Label();
            kicker.Text = "实时预览";
            kicker.Dock = DockStyle.Fill;
            kicker.TextAlign = ContentAlignment.MiddleLeft;
            kicker.ForeColor = _theme.Accent;
            kicker.Font = AppResources.Font(8.5F, FontStyle.Bold);
            layout.Controls.Add(kicker, 0, 0);

            _previewName = new Label();
            _previewName.Dock = DockStyle.Fill;
            _previewName.TextAlign = ContentAlignment.MiddleLeft;
            _previewName.ForeColor = _theme.Text;
            _previewName.Font = AppResources.Font(18F, FontStyle.Bold);
            _previewName.AutoEllipsis = true;
            layout.Controls.Add(_previewName, 0, 1);

            _previewDate = new Label();
            _previewDate.Dock = DockStyle.Fill;
            _previewDate.TextAlign = ContentAlignment.TopLeft;
            _previewDate.ForeColor = _theme.Muted;
            _previewDate.Font = AppResources.Font(8.5F, FontStyle.Regular);
            layout.Controls.Add(_previewDate, 0, 2);

            _previewStatus = new Label();
            _previewStatus.Dock = DockStyle.Fill;
            _previewStatus.TextAlign = ContentAlignment.MiddleLeft;
            _previewStatus.ForeColor = _theme.Accent;
            _previewStatus.Font = AppResources.Font(24F, FontStyle.Bold);
            layout.Controls.Add(_previewStatus, 0, 3);

            Label previewHint = new Label();
            previewHint.Dock = DockStyle.Fill;
            previewHint.TextAlign = ContentAlignment.BottomLeft;
            previewHint.ForeColor = _theme.Muted;
            previewHint.Font = AppResources.Font(8F, FontStyle.Regular);
            previewHint.Text = "保存后会重新排序并同步到小组件";
            layout.Controls.Add(previewHint, 0, 4);
            return preview;
        }

        private Panel BuildFooter()
        {
            Panel footer = new Panel();
            footer.Dock = DockStyle.Fill;
            footer.BackColor = _theme.ActionBar;
            footer.Paint += delegate(object sender, PaintEventArgs e)
            {
                using (Pen pen = new Pen(_theme.Border))
                {
                    e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
                }
            };

            if (!_isNew)
            {
                Button delete = CreateDangerButton("删除事件", AppGlyph.Delete);
                delete.Location = AppResources.SP(24, 17);
                delete.Click += delegate
                {
                    DeleteRequested = true;
                    DialogResult = DialogResult.Cancel;
                    Close();
                };
                footer.Controls.Add(delete);
            }

            Label keyboardHint = new Label();
            keyboardHint.Text = "Esc 取消 · Enter 保存";
            keyboardHint.AutoSize = true;
            keyboardHint.ForeColor = _theme.Muted;
            keyboardHint.Font = AppResources.Font(8F, FontStyle.Regular);
            keyboardHint.Location = AppResources.SP(_isNew ? 24 : 142, 29);
            footer.Controls.Add(keyboardHint);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.AutoSize = true;
            actions.WrapContents = false;
            actions.FlowDirection = FlowDirection.LeftToRight;
            actions.BackColor = _theme.ActionBar;
            footer.Controls.Add(actions);

            Button cancel = CreateSecondaryButton("取消", AppGlyph.Close);
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            actions.Controls.Add(cancel);

            Button save = CreatePrimaryButton(
                _isNew ? "保存事件" : "保存修改",
                AppGlyph.Check);
            save.Click += SaveButton_Click;
            actions.Controls.Add(save);

            footer.Resize += delegate
            {
                actions.PerformLayout();
                actions.Location = new Point(
                    footer.ClientSize.Width - actions.Width - AppResources.S(24),
                    Math.Max(0, (footer.ClientSize.Height - actions.Height) / 2));
            };

            AcceptButton = save;
            CancelButton = cancel;
            return footer;
        }

        private Label CreateFieldLabel(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.ForeColor = _theme.Text;
            label.Font = AppResources.Font(9F, FontStyle.Bold);
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Dock = DockStyle.Fill;
            return label;
        }

        private Label CreateHelperLabel(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.ForeColor = _theme.Muted;
            label.Font = AppResources.Font(8F, FontStyle.Regular);
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Dock = DockStyle.Fill;
            return label;
        }

        private Button CreatePrimaryButton(string text, AppGlyph glyph)
        {
            Button button = CreateCommandButton(
                text,
                112,
                _theme.Accent,
                _theme.OnAccent,
                false);
            button.Image = UiIconFactory.Create(glyph, _theme.OnAccent);
            return button;
        }

        private Button CreateSecondaryButton(string text, AppGlyph glyph)
        {
            Button button = CreateCommandButton(
                text,
                88,
                _theme.ActionButton,
                _theme.Text,
                true);
            button.Image = UiIconFactory.Create(glyph, _theme.Muted);
            return button;
        }

        private Button CreateDangerButton(string text, AppGlyph glyph)
        {
            Button button = CreateCommandButton(
                text,
                104,
                _theme.ActionButton,
                _theme.Danger,
                true);
            button.Image = UiIconFactory.Create(glyph, _theme.Danger);
            return button;
        }

        private Button CreateCommandButton(
            string text,
            int width,
            Color background,
            Color foreground,
            bool bordered)
        {
            Button button = new Button();
            button.Text = text;
            button.Width = AppResources.S(width);
            button.Height = AppResources.S(38);
            button.Margin = AppResources.SPad(8, 0, 0, 0);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = bordered ? 1 : 0;
            button.FlatAppearance.BorderColor = bordered ? _theme.Border : background;
            button.BackColor = background;
            button.ForeColor = foreground;
            button.Font = AppResources.Font(9F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
            button.TextImageRelation = TextImageRelation.ImageBeforeText;
            button.ImageAlign = ContentAlignment.MiddleLeft;
            button.Padding = AppResources.SPad(7, 0, 7, 0);
            return button;
        }

        private void LoadItem()
        {
            if (_isNew)
            {
                _nameTextBox.Text = string.Empty;
                _dateTextBox.Text = DateTime.Today.AddDays(7).ToString("yyyy-MM-dd");
                _nameTextBox.Focus();
                UpdatePreview();
                return;
            }

            _nameTextBox.Text = _source.Name;
            DateTime date;
            DateTime selected = CountdownEngine.TryParseDate(_source.Date, out date)
                ? date.Date
                : DateTime.Today.AddDays(7);
            _dateTextBox.Text = selected.ToString("yyyy-MM-dd");
            _nameTextBox.SelectAll();
            _nameTextBox.Focus();
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (_previewName == null || _previewDate == null || _previewStatus == null) return;

            string name = _nameTextBox.Text.Trim();
            _previewName.Text = name.Length == 0 ? "事件名称" : name;
            DateTime targetDate;
            bool validDate = CountdownEngine.TryParseDate(
                _dateTextBox.Text.Trim(),
                out targetDate);
            _previewDate.Text = validDate
                ? AppResources.FormatDate(targetDate.Date)
                : "请输入 YYYY-MM-DD";

            int days = validDate ? (targetDate.Date - DateTime.Today).Days : -1;
            if (days < 0)
            {
                _previewStatus.Text = "已到期";
                _previewStatus.ForeColor = _theme.Muted;
            }
            else if (days == 0)
            {
                _previewStatus.Text = "今天";
                _previewStatus.ForeColor = _theme.Accent;
            }
            else
            {
                _previewStatus.Text = "还有 " + days.ToString() + " 天";
                _previewStatus.ForeColor = _theme.Accent;
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            string name = _nameTextBox.Text.Trim();
            if (name.Length == 0)
            {
                ShowError("请填写事件名称。");
                _nameTextBox.Focus();
                return;
            }

            CountdownEvent item = new CountdownEvent();
            if (!_isNew)
            {
                item.Id = _source.Id;
                item.CreatedAt = _source.CreatedAt;
            }
            item.Name = name;
            DateTime targetDate;
            if (!CountdownEngine.TryParseDate(_dateTextBox.Text.Trim(), out targetDate))
            {
                ShowError("请选择有效日期，格式为 YYYY-MM-DD。");
                _dateTextBox.Focus();
                return;
            }
            item.Date = targetDate.ToString("yyyy-MM-dd");

            string error;
            if (!CountdownEngine.ValidateEvent(item, out error))
            {
                ShowError(error);
                return;
            }

            Item = item;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ShowError(string message)
        {
            _hintLabel.ForeColor = _theme.Danger;
            _hintLabel.Text = message;
        }

        private void OpenCalendar()
        {
            DateTime selectedDate;
            if (!CountdownEngine.TryParseDate(_dateTextBox.Text.Trim(), out selectedDate))
            {
                selectedDate = DateTime.Today.AddDays(7);
            }

            using (Form popup = new Form())
            {
                popup.FormBorderStyle = FormBorderStyle.FixedToolWindow;
                popup.StartPosition = FormStartPosition.Manual;
                popup.ShowInTaskbar = false;
                popup.BackColor = _theme.ActionBar;
                popup.ClientSize = AppResources.SS(236, 190);

                MonthCalendar calendar = new MonthCalendar();
                calendar.MaxSelectionCount = 1;
                calendar.SetDate(selectedDate.Date);
                calendar.Location = AppResources.SP(8, 7);
                calendar.DateSelected += delegate(object sender, DateRangeEventArgs e)
                {
                    _dateTextBox.Text = e.Start.ToString("yyyy-MM-dd");
                    popup.Close();
                };
                popup.Controls.Add(calendar);

                Point location = _dateTextBox.PointToScreen(
                    new Point(0, _dateTextBox.Height + AppResources.S(4)));
                popup.Location = location;
                popup.ShowDialog(this);
            }
        }
    }
}
