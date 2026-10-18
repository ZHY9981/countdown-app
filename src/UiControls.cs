using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CountdownApp
{
    public enum AppGlyph
    {
        Add,
        Edit,
        Delete,
        Undo,
        Clock,
        More
    }

    public static class UiIconFactory
    {
        public static Bitmap Create(AppGlyph glyph, Color color)
        {
            int size = AppResources.S(16);
            Bitmap bitmap = new Bitmap(size, size);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.Clear(Color.Transparent);

                float scale = size / 16f;
                using (Pen pen = new Pen(color, Math.Max(1.25f, 1.45f * scale)))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    pen.LineJoin = LineJoin.Round;
                    DrawGlyph(graphics, pen, glyph, scale);
                }
            }
            return bitmap;
        }

        private static void DrawGlyph(Graphics graphics, Pen pen, AppGlyph glyph, float scale)
        {
            Func<float, float> S = delegate(float value) { return value * scale; };

            if (glyph == AppGlyph.Add)
            {
                graphics.DrawLine(pen, S(8), S(3), S(8), S(13));
                graphics.DrawLine(pen, S(3), S(8), S(13), S(8));
            }
            else if (glyph == AppGlyph.Edit)
            {
                graphics.DrawLine(pen, S(3), S(13), S(5.8f), S(12.3f));
                graphics.DrawLine(pen, S(5.8f), S(12.3f), S(13.1f), S(5));
                graphics.DrawLine(pen, S(11.1f), S(3), S(13.1f), S(5));
                graphics.DrawLine(pen, S(3.2f), S(13.2f), S(3.2f), S(15));
            }
            else if (glyph == AppGlyph.Delete)
            {
                graphics.DrawLine(pen, S(3), S(4.5f), S(13), S(4.5f));
                graphics.DrawRectangle(pen, S(5), S(2.5f), S(6), S(3));
                graphics.DrawLine(pen, S(5.5f), S(4.5f), S(6.3f), S(14));
                graphics.DrawLine(pen, S(10.5f), S(4.5f), S(9.7f), S(14));
                graphics.DrawLine(pen, S(6.3f), S(14), S(9.7f), S(14));
            }
            else if (glyph == AppGlyph.Undo)
            {
                graphics.DrawArc(pen, S(2.5f), S(4), S(11), S(9), 20, 250);
                graphics.DrawLine(pen, S(3), S(4), S(6.2f), S(4.2f));
                graphics.DrawLine(pen, S(3), S(4), S(3.1f), S(7.2f));
            }
            else if (glyph == AppGlyph.Clock)
            {
                graphics.DrawEllipse(pen, S(2.5f), S(2.5f), S(11), S(11));
                graphics.DrawLine(pen, S(8), S(8), S(8), S(4.6f));
                graphics.DrawLine(pen, S(8), S(8), S(10.8f), S(9.6f));
            }
            else if (glyph == AppGlyph.More)
            {
                using (SolidBrush brush = new SolidBrush(pen.Color))
                {
                    graphics.FillEllipse(brush, S(3), S(7), S(2), S(2));
                    graphics.FillEllipse(brush, S(7), S(7), S(2), S(2));
                    graphics.FillEllipse(brush, S(11), S(7), S(2), S(2));
                }
            }
        }
    }

    public class RoundedPanel : Panel
    {
        public int CornerRadius { get; set; }
        public Color BorderColor { get; set; }
        public bool DrawBorder { get; set; }

        public RoundedPanel()
        {
            CornerRadius = AppResources.S(6);
            BorderColor = AppResources.Border;
            DrawBorder = true;
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            UpdateRegion();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (!DrawBorder || Width <= 1 || Height <= 1) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = CreateRoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), CornerRadius))
            using (Pen pen = new Pen(BorderColor))
            {
                e.Graphics.DrawPath(pen, path);
            }
        }

        private void UpdateRegion()
        {
            if (Width <= 0 || Height <= 0) return;
            using (GraphicsPath path = CreateRoundedPath(new Rectangle(0, 0, Width, Height), CornerRadius))
            {
                Region old = Region;
                Region = new Region(path);
                if (old != null) old.Dispose();
            }
        }

        public static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = Math.Max(1, Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height)));
            Rectangle arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public class StatusPill : Label
    {
        public Color PillColor { get; set; }
        public Color PillBorderColor { get; set; }
        public bool DrawPillBorder { get; set; }

        public StatusPill()
        {
            AutoSize = true;
            PillColor = AppResources.FieldBackground;
            PillBorderColor = AppResources.Border;
            DrawPillBorder = false;
            Padding = AppResources.SPad(9, 3, 9, 3);
            Font = AppResources.Font(8.5F, FontStyle.Bold);
            TextAlign = ContentAlignment.MiddleCenter;
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            int radius = Math.Min(Height, AppResources.S(12));
            using (GraphicsPath path = RoundedPanel.CreateRoundedPath(bounds, radius))
            using (SolidBrush fill = new SolidBrush(PillColor))
            {
                e.Graphics.FillPath(fill, path);
                if (DrawPillBorder)
                {
                    using (Pen pen = new Pen(PillBorderColor))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            }

            TextRenderer.DrawText(
                e.Graphics,
                Text,
                Font,
                ClientRectangle,
                ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }

    public class FlipClockControl : Control
    {
        private CountdownSnapshot _snapshot;

        public FlipClockControl()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = AppResources.Surface;
            _snapshot = new CountdownSnapshot();
        }

        public void SetSnapshot(CountdownSnapshot snapshot)
        {
            _snapshot = snapshot ?? new CountdownSnapshot();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = ClientRectangle;
            if (bounds.Width < AppResources.S(80) || bounds.Height < AppResources.S(50)) return;

            if (_snapshot == null || _snapshot.Status == CountdownStatus.Invalid)
            {
                DrawCenteredText(e.Graphics, "--", bounds, AppResources.Muted, 18F, FontStyle.Bold);
                return;
            }

            if (_snapshot.Status == CountdownStatus.Ended)
            {
                DrawCenteredText(e.Graphics, "已到期", bounds, AppResources.Muted, 24F, FontStyle.Bold);
                return;
            }

            if (_snapshot.Status == CountdownStatus.Today)
            {
                DrawCenteredText(e.Graphics, "今天", bounds, AppResources.Today, 30F, FontStyle.Bold);
                return;
            }

            int remainingDays = Math.Max(0, (int)_snapshot.Remaining.TotalDays);
            string days = Math.Min(remainingDays, 999).ToString("000");
            string hours = _snapshot.Remaining.Hours.ToString("00");
            string minutes = _snapshot.Remaining.Minutes.ToString("00");
            string seconds = _snapshot.Remaining.Seconds.ToString("00");

            int outerPadding = AppResources.S(6);
            int available = Math.Max(1, bounds.Width - outerPadding * 2);
            int labelAllowance = AppResources.S(58);
            int digitWidth = Math.Max(AppResources.S(20), Math.Min(AppResources.S(46), (available - labelAllowance) / 10));
            int digitHeight = Math.Max(AppResources.S(34), (int)(digitWidth * 1.42f));
            int groupGap = Math.Max(1, digitWidth / 22);
            int labelWidth = Math.Max(AppResources.S(12), digitWidth / 2);
            int totalWidth = digitWidth * 9 + groupGap * 8 + labelWidth * 3;
            int x = bounds.Left + Math.Max(0, (bounds.Width - totalWidth) / 2);
            int y = bounds.Top + Math.Max(0, (bounds.Height - digitHeight) / 2);

            x = DrawGroup(e.Graphics, days, x, y, digitWidth, digitHeight, groupGap);
            x = DrawLabel(e.Graphics, "天", x, y, digitHeight, labelWidth);
            x = DrawGroup(e.Graphics, hours, x, y, digitWidth, digitHeight, groupGap);
            x = DrawLabel(e.Graphics, ":", x, y, digitHeight, labelWidth);
            x = DrawGroup(e.Graphics, minutes, x, y, digitWidth, digitHeight, groupGap);
            x = DrawLabel(e.Graphics, ":", x, y, digitHeight, labelWidth);
            DrawGroup(e.Graphics, seconds, x, y, digitWidth, digitHeight, groupGap);
        }

        private int DrawGroup(
            Graphics graphics,
            string value,
            int x,
            int y,
            int digitWidth,
            int digitHeight,
            int gap)
        {
            for (int i = 0; i < value.Length; i++)
            {
                DrawDigit(graphics, value[i], new Rectangle(x, y, digitWidth, digitHeight));
                x += digitWidth + gap;
            }
            return x;
        }

        private int DrawLabel(Graphics graphics, string text, int x, int y, int height, int width)
        {
            Rectangle bounds = new Rectangle(x, y, width, height);
            TextRenderer.DrawText(
                graphics,
                text,
                AppResources.Font(9F, FontStyle.Bold),
                bounds,
                AppResources.Muted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            return x + width;
        }

        private static void DrawDigit(Graphics graphics, char value, Rectangle bounds)
        {
            int radius = Math.Max(AppResources.S(4), bounds.Width / 8);
            using (GraphicsPath path = RoundedPanel.CreateRoundedPath(bounds, radius))
            using (SolidBrush fill = new SolidBrush(AppResources.Header))
            {
                graphics.FillPath(fill, path);
            }

            int split = bounds.Top + bounds.Height / 2;
            using (Pen splitPen = new Pen(Color.FromArgb(70, 255, 255, 255)))
            {
                graphics.DrawLine(splitPen, bounds.Left + AppResources.S(3), split, bounds.Right - AppResources.S(3), split);
            }

            float emSize = Math.Max(12F, bounds.Height * 0.92F);
            using (FontFamily family = new FontFamily("Consolas"))
            using (StringFormat format = StringFormat.GenericTypographic)
            using (GraphicsPath glyph = new GraphicsPath())
            {
                glyph.AddString(
                    value.ToString(),
                    family,
                    (int)FontStyle.Bold,
                    emSize,
                    PointF.Empty,
                    format);

                // Center the visible glyph ink, not the font's asymmetric line box.
                RectangleF inkBounds = glyph.GetBounds();
                float offsetX = bounds.Left + (bounds.Width - inkBounds.Width) / 2F - inkBounds.Left;
                float offsetY = bounds.Top + (bounds.Height - inkBounds.Height) / 2F - inkBounds.Top;
                using (Matrix transform = new Matrix())
                {
                    transform.Translate(offsetX, offsetY);
                    glyph.Transform(transform);
                }

                using (SolidBrush brush = new SolidBrush(Color.White))
                {
                    graphics.FillPath(brush, glyph);
                }
            }
        }

        private static void DrawCenteredText(
            Graphics graphics,
            string text,
            Rectangle bounds,
            Color color,
            float size,
            FontStyle style)
        {
            using (Font font = AppResources.Font(size, style))
            {
                TextRenderer.DrawText(
                    graphics,
                    text,
                    font,
                    bounds,
                    color,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }
    }

    public class CountdownCard : Control
    {
        private CountdownEvent _item;
        private CountdownSnapshot _snapshot;
        private bool _featured;
        private bool _selected;
        private readonly Timer _flipTimer;
        private string _currentDigits;
        private string _previousDigits;
        private DateTime _flipStartedUtc;
        private bool _flipping;
        private const int FlipDurationMs = 800;

        public event EventHandler CardClicked;

        public CountdownEvent Item
        {
            get { return _item; }
            set
            {
                _item = value;
                AccessibleName = value == null ? "添加倒计时" : value.Name;
                Invalidate();
            }
        }

        public CountdownSnapshot Snapshot
        {
            get { return _snapshot; }
            set
            {
                _snapshot = value ?? new CountdownSnapshot();
                SetSnapshotDigits(_snapshot);
                Invalidate();
            }
        }

        public bool Featured
        {
            get { return _featured; }
            set
            {
                _featured = value;
                Invalidate();
            }
        }

        public bool Selected
        {
            get { return _selected; }
            set
            {
                _selected = value;
                Invalidate();
            }
        }

        public CountdownCard()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = AppResources.Background;
            Cursor = Cursors.Hand;
            _snapshot = new CountdownSnapshot();
            _flipTimer = new Timer();
            _flipTimer.Interval = 15;
            _flipTimer.Tick += FlipTimer_Tick;
        }

        private void SetSnapshotDigits(CountdownSnapshot snapshot)
        {
            string next = DigitsFor(snapshot);
            if (_currentDigits == null)
            {
                _currentDigits = next;
                _previousDigits = next;
                return;
            }
            if (string.Equals(next, _currentDigits, StringComparison.Ordinal)) return;

            _previousDigits = _currentDigits;
            _currentDigits = next;
            _flipStartedUtc = DateTime.UtcNow;
            _flipping = true;
            _flipTimer.Stop();
            _flipTimer.Start();
        }

        private void FlipTimer_Tick(object sender, EventArgs e)
        {
            if (!_flipping || FlipProgress() >= 1F)
            {
                _flipping = false;
                _flipTimer.Stop();
            }
            Invalidate();
        }

        private float FlipProgress()
        {
            if (!_flipping) return 1F;
            double elapsed = (DateTime.UtcNow - _flipStartedUtc).TotalMilliseconds;
            return Math.Max(0F, Math.Min(1F, (float)(elapsed / FlipDurationMs)));
        }

        private static string DigitsFor(CountdownSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Status == CountdownStatus.Invalid) return "---------";

            int days = Math.Max(0, (int)snapshot.Remaining.TotalDays);
            return Math.Min(days, 999).ToString("000")
                + snapshot.Remaining.Hours.ToString("00")
                + snapshot.Remaining.Minutes.ToString("00")
                + snapshot.Remaining.Seconds.ToString("00");
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (CardClicked != null) CardClicked(this, EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            int radius = AppResources.S(_featured ? 14 : 12);
            using (GraphicsPath path = RoundedPanel.CreateRoundedPath(bounds, radius))
            using (SolidBrush fill = new SolidBrush(AppResources.Surface))
            {
                e.Graphics.FillPath(fill, path);
            }

            Color borderColor = _selected ? AppResources.Accent : AppResources.Border;
            float borderWidth = _selected ? Math.Max(2f, AppResources.SF(2f)) : 1f;
            using (GraphicsPath path = RoundedPanel.CreateRoundedPath(bounds, radius))
            using (Pen border = new Pen(borderColor, borderWidth))
            {
                e.Graphics.DrawPath(border, path);
            }

            if (_item == null)
            {
                DrawEmptyState(e.Graphics);
                return;
            }

            DrawHeader(e.Graphics);
            DrawClockPanel(e.Graphics);
        }

        private void DrawHeader(Graphics graphics)
        {
            int left = AppResources.S(_featured ? 24 : 18);
            int right = Width - AppResources.S(_featured ? 24 : 18);
            int top = AppResources.S(_featured ? 18 : 15);

            if (_featured)
            {
                DrawText(
                    graphics,
                    "最近到期",
                    AppResources.Font(8.5F, FontStyle.Bold),
                    AppResources.Accent,
                    new Rectangle(left, top, AppResources.S(120), AppResources.S(18)),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                top += AppResources.S(24);
            }

            string pillText = StatusText(_snapshot);
            Size pillSize = MeasurePill(graphics, pillText, _featured);
            Rectangle pillBounds = new Rectangle(
                right - pillSize.Width,
                top,
                pillSize.Width,
                pillSize.Height);
            DrawPill(graphics, pillBounds, pillText, _snapshot);

            int nameRight = pillBounds.Left - AppResources.S(12);
            Font nameFont = AppResources.Font(_featured ? 20F : 14F, FontStyle.Bold);
            Rectangle nameBounds = new Rectangle(
                left,
                _featured ? top + AppResources.S(20) : top,
                Math.Max(0, nameRight - left),
                AppResources.S(_featured ? 48 : 30));
            DrawText(
                graphics,
                _item.Name,
                nameFont,
                AppResources.Text,
                nameBounds,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

            Rectangle dateBounds = new Rectangle(
                left,
                _featured ? nameBounds.Bottom - AppResources.S(2) : nameBounds.Bottom + AppResources.S(1),
                Math.Max(0, right - left),
                AppResources.S(_featured ? 25 : 20));
            DrawText(
                graphics,
                AppResources.FormatDate(_snapshot.TargetDate),
                AppResources.Font(_featured ? 9F : 8F, FontStyle.Regular),
                AppResources.Muted,
                dateBounds,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }

        private void DrawClockPanel(Graphics graphics)
        {
            int margin = AppResources.S(_featured ? 24 : 18);
            int availableWidth = Math.Max(0, Width - margin * 2);
            int tileGap = AppResources.S(_featured ? 4 : 2);
            int groupGap = AppResources.S(_featured ? 14 : 8);
            int colonWidth = AppResources.S(_featured ? 17 : 9);
            int unitHeight = AppResources.S(_featured ? 24 : 18);
            int fixedWidth = tileGap * 5 + groupGap + colonWidth * 2;
            int maxTileWidth = AppResources.S(_featured ? 46 : 28);
            int tileWidth = Math.Max(
                AppResources.S(_featured ? 28 : 16),
                Math.Min(maxTileWidth, (availableWidth - fixedWidth) / 9));
            int tileHeight = Math.Max(
                AppResources.S(_featured ? 42 : 24),
                (int)Math.Round(tileWidth * 1.42F));
            int totalWidth = tileWidth * 9 + fixedWidth;
            int clockTop = Height - margin - tileHeight - unitHeight;
            if (totalWidth <= 0 || clockTop < 0) return;

            string current = string.IsNullOrEmpty(_currentDigits)
                ? DigitsFor(_snapshot)
                : _currentDigits;
            string previous = string.IsNullOrEmpty(_previousDigits)
                ? current
                : _previousDigits;
            float progress = FlipProgress();

            int x = (Width - totalWidth) / 2;
            x = DrawDigitGroup(graphics, current, previous, 0, "天", x, clockTop, tileWidth, tileHeight, tileGap, unitHeight, progress);
            x += groupGap;
            x = DrawDigitGroup(graphics, current, previous, 3, "时", x, clockTop, tileWidth, tileHeight, tileGap, unitHeight, progress);
            DrawColon(graphics, new Rectangle(x, clockTop, colonWidth, tileHeight));
            x += colonWidth;
            x = DrawDigitGroup(graphics, current, previous, 5, "分", x, clockTop, tileWidth, tileHeight, tileGap, unitHeight, progress);
            DrawColon(graphics, new Rectangle(x, clockTop, colonWidth, tileHeight));
            x += colonWidth;
            DrawDigitGroup(graphics, current, previous, 7, "秒", x, clockTop, tileWidth, tileHeight, tileGap, unitHeight, progress);
        }

        private int DrawDigitGroup(
            Graphics graphics,
            string current,
            string previous,
            int startIndex,
            string unit,
            int x,
            int y,
            int tileWidth,
            int tileHeight,
            int tileGap,
            int unitHeight,
            float progress)
        {
            int groupLeft = x;
            int count = unit == "天" ? 3 : 2;
            for (int i = 0; i < count; i++)
            {
                Rectangle tile = new Rectangle(x, y, tileWidth, tileHeight);
                char currentDigit = startIndex + i < current.Length ? current[startIndex + i] : '-';
                char previousDigit = startIndex + i < previous.Length ? previous[startIndex + i] : currentDigit;
                DrawFlipDigit(graphics, currentDigit, previousDigit, tile, progress);
                x += tileWidth + tileGap;
            }

            int groupWidth = count * tileWidth + Math.Max(0, count - 1) * tileGap;
            Rectangle labelBounds = new Rectangle(
                groupLeft,
                y + tileHeight,
                groupWidth,
                unitHeight);
            DrawText(
                graphics,
                unit,
                AppResources.Font(_featured ? 8.5F : 7F, FontStyle.Bold),
                Color.FromArgb(116, 126, 140),
                labelBounds,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            return groupLeft + groupWidth;
        }

        private void DrawColon(Graphics graphics, Rectangle bounds)
        {
            DrawText(
                graphics,
                ":",
                AppResources.Font(_featured ? 24F : 13F, FontStyle.Bold),
                Color.FromArgb(132, 142, 156),
                bounds,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        private void DrawFlipDigit(Graphics graphics, char current, char previous, Rectangle bounds, float progress)
        {
            int radius = Math.Max(AppResources.S(4), bounds.Width / 8);
            using (GraphicsPath tilePath = RoundedPanel.CreateRoundedPath(bounds, radius))
            {
                using (SolidBrush fill = new SolidBrush(Color.FromArgb(20, 24, 32)))
                {
                    graphics.FillPath(fill, tilePath);
                }

                GraphicsState state = graphics.Save();
                graphics.SetClip(tilePath);
                int split = bounds.Top + bounds.Height / 2;
                using (SolidBrush top = new SolidBrush(Color.FromArgb(24, 29, 38)))
                using (SolidBrush bottom = new SolidBrush(Color.FromArgb(14, 18, 24)))
                {
                    graphics.FillRectangle(top, bounds.Left, bounds.Top, bounds.Width, split - bounds.Top);
                    graphics.FillRectangle(bottom, bounds.Left, split, bounds.Width, bounds.Bottom - split);
                }
                graphics.Restore(state);
            }

            if (current == previous || progress >= 1F)
            {
                DrawDigitGlyph(graphics, current, bounds);
            }
            else
            {
                DrawDigitGlyph(graphics, current, bounds);
                DrawFlapOverlay(graphics, bounds, progress);
            }

            using (Pen seam = new Pen(Color.FromArgb(64, 73, 88)))
            {
                int middle = bounds.Top + bounds.Height / 2;
                graphics.DrawLine(
                    seam,
                    bounds.Left + AppResources.S(3),
                    middle,
                    bounds.Right - AppResources.S(3),
                    middle);
            }
        }

        private static void DrawFlapOverlay(
            Graphics graphics,
            Rectangle bounds,
            float progress)
        {
            int middle = bounds.Top + bounds.Height / 2;
            int halfHeight = Math.Max(1, bounds.Height / 2);
            PointF[] flapShape;
            PointF edgeStart;
            PointF edgeEnd;
            Color shadeStart;
            Color shadeEnd;

            if (progress < 0.5F)
            {
                float phase = progress / 0.5F;
                float eased = phase * phase;
                int height = Math.Max(1, (int)(halfHeight * (1F - eased)));
                int top = middle - height;
                int inset = (int)(bounds.Width * 0.045F * eased);
                flapShape = new PointF[]
                {
                    new PointF(bounds.Left + inset, top),
                    new PointF(bounds.Right - inset, top),
                    new PointF(bounds.Right, middle),
                    new PointF(bounds.Left, middle)
                };
                edgeStart = new PointF(bounds.Left + inset, top);
                edgeEnd = new PointF(bounds.Right - inset, top);
                shadeStart = Color.FromArgb(178, 0, 0, 0);
                shadeEnd = Color.FromArgb(42, 0, 0, 0);
            }
            else
            {
                float phase = (progress - 0.5F) / 0.5F;
                float eased = 1F - (1F - phase) * (1F - phase);
                int height = Math.Max(1, (int)(halfHeight * eased));
                int bottom = middle + height;
                int inset = (int)(bounds.Width * 0.04F * (1F - phase));
                flapShape = new PointF[]
                {
                    new PointF(bounds.Left, middle),
                    new PointF(bounds.Right, middle),
                    new PointF(bounds.Right - inset, bottom),
                    new PointF(bounds.Left + inset, bottom)
                };
                edgeStart = new PointF(bounds.Left + inset, bottom);
                edgeEnd = new PointF(bounds.Right - inset, bottom);
                shadeStart = Color.FromArgb(28, 0, 0, 0);
                shadeEnd = Color.FromArgb(Math.Max(0, (int)(132F * (1F - phase))), 0, 0, 0);
            }

            GraphicsState state = graphics.Save();
            using (GraphicsPath clipPath = RoundedPanel.CreateRoundedPath(
                bounds,
                Math.Max(1, bounds.Width / 8)))
            {
                graphics.SetClip(clipPath);
            }
            using (LinearGradientBrush flapBrush = new LinearGradientBrush(
                bounds,
                shadeStart,
                shadeEnd,
                LinearGradientMode.Vertical))
            {
                graphics.FillPolygon(flapBrush, flapShape);
            }
            using (Pen edge = new Pen(Color.FromArgb(210, 218, 229, 244), Math.Max(2F, AppResources.SF(2F))))
            {
                graphics.DrawLine(edge, edgeStart, edgeEnd);
            }
            graphics.Restore(state);
        }

        private static void DrawDigitGlyph(Graphics graphics, char value, Rectangle bounds)
        {
            float emSize = Math.Max(10F, bounds.Height * 0.86F);
            using (FontFamily family = new FontFamily("Consolas"))
            using (StringFormat format = StringFormat.GenericTypographic)
            using (GraphicsPath glyph = new GraphicsPath())
            {
                glyph.AddString(
                    value.ToString(),
                    family,
                    (int)FontStyle.Bold,
                    emSize,
                    PointF.Empty,
                    format);
                RectangleF ink = glyph.GetBounds();
                float offsetX = bounds.Left + (bounds.Width - ink.Width) / 2F - ink.Left;
                float offsetY = bounds.Top + (bounds.Height - ink.Height) / 2F - ink.Top;
                using (Matrix transform = new Matrix())
                {
                    transform.Translate(offsetX, offsetY);
                    glyph.Transform(transform);
                }
                using (SolidBrush brush = new SolidBrush(Color.White))
                {
                    graphics.FillPath(brush, glyph);
                }
            }
        }

        private void DrawEmptyState(Graphics graphics)
        {
            Rectangle bounds = new Rectangle(
                AppResources.S(28),
                AppResources.S(28),
                Math.Max(0, Width - AppResources.S(56)),
                Math.Max(0, Height - AppResources.S(56)));
            DrawText(
                graphics,
                "添加一个倒计时",
                AppResources.Font(_featured ? 16F : 12F, FontStyle.Bold),
                AppResources.Muted,
                bounds,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        private string StatusText(CountdownSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Status == CountdownStatus.Invalid) return "日期异常";
            if (snapshot.Status == CountdownStatus.Ended) return "已到期";
            if (snapshot.Status == CountdownStatus.Today) return "今天";
            int remainingDays = Math.Max(0, (int)snapshot.Remaining.TotalDays);
            return "还有 " + remainingDays.ToString() + " 天";
        }

        private Size MeasurePill(Graphics graphics, string text, bool featured)
        {
            Size textSize = TextRenderer.MeasureText(
                graphics,
                text,
                AppResources.Font(featured ? 8.5F : 7.5F, FontStyle.Bold),
                new Size(int.MaxValue, AppResources.S(24)),
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            return new Size(
                textSize.Width + AppResources.S(featured ? 18 : 14),
                AppResources.S(featured ? 24 : 21));
        }

        private void DrawPill(Graphics graphics, Rectangle bounds, string text, CountdownSnapshot snapshot)
        {
            Color foreground = AppResources.Muted;
            Color background = AppResources.FieldBackground;
            if (snapshot != null && snapshot.Status == CountdownStatus.Today)
            {
                foreground = AppResources.Today;
                background = AppResources.TodayPale;
            }
            else if (snapshot != null
                && snapshot.Status == CountdownStatus.Upcoming
                && Math.Max(0, (int)snapshot.Remaining.TotalDays) <= 7)
            {
                foreground = AppResources.Danger;
                background = AppResources.DangerPale;
            }

            using (GraphicsPath path = RoundedPanel.CreateRoundedPath(bounds, bounds.Height / 2))
            using (SolidBrush fill = new SolidBrush(background))
            {
                graphics.FillPath(fill, path);
            }
            DrawText(
                graphics,
                text,
                AppResources.Font(_featured ? 8.5F : 7.5F, FontStyle.Bold),
                foreground,
                bounds,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        private static void DrawText(
            Graphics graphics,
            string text,
            Font font,
            Color color,
            Rectangle bounds,
            TextFormatFlags flags)
        {
            TextRenderer.DrawText(graphics, text, font, bounds, color, flags);
        }
    }

    public class BufferedDataGridView : DataGridView
    {
        public BufferedDataGridView()
        {
            DoubleBuffered = true;
        }
    }
}
