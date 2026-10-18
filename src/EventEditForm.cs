using System;
using System.Drawing;
using System.Windows.Forms;

namespace CountdownApp
{
    public class EventEditForm : Form
    {
        private readonly bool _isNew;
        private readonly CountdownEvent _source;
        private TextBox _nameTextBox;
        private DateTimePicker _datePicker;
        private Label _hintLabel;

        public CountdownEvent Item { get; private set; }

        public EventEditForm(CountdownEvent existing)
        {
            _isNew = existing == null;
            _source = existing;

            Text = _isNew ? "添加倒计时" : "编辑倒计时";
            Icon = AppResources.CreateAppIcon();
            AutoScaleMode = AutoScaleMode.None;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = AppResources.SS(520, 330);
            BackColor = AppResources.Surface;
            Font = AppResources.Font(9F, FontStyle.Regular);

            BuildInterface();
            LoadItem();
        }

        private void BuildInterface()
        {
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 2;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, AppResources.SF(74F)));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            Controls.Add(root);

            Panel header = new Panel();
            header.Dock = DockStyle.Fill;
            header.BackColor = AppResources.Header;

            Label title = new Label();
            title.Text = _isNew ? "添加倒计时事件" : "编辑倒计时事件";
            title.ForeColor = AppResources.HeaderText;
            title.Font = AppResources.Font(15F, FontStyle.Bold);
            title.AutoSize = true;
            title.Location = AppResources.SP(24, 13);
            header.Controls.Add(title);

            Label subtitle = new Label();
            subtitle.Text = "事件名称和日期保存在本机，不需要联网";
            subtitle.ForeColor = AppResources.HeaderMuted;
            subtitle.Font = AppResources.Font(8.5F, FontStyle.Regular);
            subtitle.AutoSize = true;
            subtitle.Location = AppResources.SP(25, 45);
            header.Controls.Add(subtitle);
            root.Controls.Add(header, 0, 0);

            TableLayoutPanel form = new TableLayoutPanel();
            form.Dock = DockStyle.Fill;
            form.Padding = AppResources.SPad(24, 20, 24, 14);
            form.ColumnCount = 2;
            form.RowCount = 5;
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, AppResources.SF(104F)));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, AppResources.SF(44F)));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, AppResources.SF(44F)));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, AppResources.SF(48F)));
            form.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, AppResources.SF(44F)));

            AddLabel(form, "事件名称", 0);
            _nameTextBox = new TextBox();
            _nameTextBox.Dock = DockStyle.Fill;
            _nameTextBox.Font = AppResources.Font(10F, FontStyle.Regular);
            _nameTextBox.BorderStyle = BorderStyle.FixedSingle;
            _nameTextBox.MaxLength = 80;
            _nameTextBox.TextChanged += delegate { _hintLabel.Text = string.Empty; };
            form.Controls.Add(_nameTextBox, 1, 0);

            AddLabel(form, "目标日期", 1);
            _datePicker = new DateTimePicker();
            _datePicker.Dock = DockStyle.Fill;
            _datePicker.Font = AppResources.Font(10F, FontStyle.Regular);
            _datePicker.Format = DateTimePickerFormat.Custom;
            _datePicker.CustomFormat = "yyyy年M月d日";
            _datePicker.ValueChanged += delegate { _hintLabel.Text = string.Empty; };
            form.Controls.Add(_datePicker, 1, 1);

            _hintLabel = new Label();
            _hintLabel.ForeColor = AppResources.Muted;
            _hintLabel.Font = AppResources.Font(8.5F, FontStyle.Regular);
            _hintLabel.TextAlign = ContentAlignment.MiddleLeft;
            _hintLabel.Dock = DockStyle.Fill;
            _hintLabel.Text = "倒计时按目标日期的本地零点计算。";
            form.Controls.Add(_hintLabel, 0, 2);
            form.SetColumnSpan(_hintLabel, 2);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.WrapContents = false;

            Button save = CreatePrimaryButton("保存");
            save.Click += SaveButton_Click;
            buttons.Controls.Add(save);

            Button cancel = CreateSecondaryButton("取消");
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            buttons.Controls.Add(cancel);

            form.Controls.Add(buttons, 0, 4);
            form.SetColumnSpan(buttons, 2);
            root.Controls.Add(form, 0, 1);

            AcceptButton = save;
            CancelButton = cancel;
        }

        private static void AddLabel(TableLayoutPanel form, string text, int row)
        {
            Label label = new Label();
            label.Text = text;
            label.ForeColor = AppResources.Text;
            label.Font = AppResources.Font(9F, FontStyle.Bold);
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Dock = DockStyle.Fill;
            form.Controls.Add(label, 0, row);
        }

        private static Button CreatePrimaryButton(string text)
        {
            Button button = new Button();
            button.Text = text;
            button.Width = AppResources.S(92);
            button.Height = AppResources.S(38);
            button.Margin = AppResources.SPad(8, 0, 0, 0);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = AppResources.Accent;
            button.ForeColor = AppResources.OnAccent;
            button.Font = AppResources.Font(9F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
            return button;
        }

        private static Button CreateSecondaryButton(string text)
        {
            Button button = new Button();
            button.Text = text;
            button.Width = AppResources.S(82);
            button.Height = AppResources.S(38);
            button.Margin = AppResources.SPad(8, 0, 0, 0);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = AppResources.ButtonBorder;
            button.BackColor = AppResources.Surface;
            button.ForeColor = AppResources.Text;
            button.Font = AppResources.Font(9F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
            return button;
        }

        private void LoadItem()
        {
            if (_isNew)
            {
                _nameTextBox.Text = string.Empty;
                _datePicker.Value = DateTime.Today.AddDays(7);
                _nameTextBox.Focus();
                return;
            }

            _nameTextBox.Text = _source.Name;
            DateTime date;
            _datePicker.Value = CountdownEngine.TryParseDate(_source.Date, out date)
                ? date.Date
                : DateTime.Today.AddDays(7);
            _nameTextBox.SelectAll();
            _nameTextBox.Focus();
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
            item.Date = _datePicker.Value.ToString("yyyy-MM-dd");

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
            _hintLabel.ForeColor = AppResources.Danger;
            _hintLabel.Text = message;
        }
    }
}
