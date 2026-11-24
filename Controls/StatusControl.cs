using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using MissionPlanner.Utilities;

namespace MissionPlanner.Controls
{
    public class StatusControl : UserControl
    {
        private PlaceholderTextBox _searchBox;
        private Panel _contentPanel;
        private List<StatusItem> _allItems;
        private List<StatusItem> _filteredItems;
        private Timer _updateTimer;
        private Font _boldFont;
        private Font _normalFont;
        private int _rowHeight = 18;
        private int _padding = 10;
        private int _nameValueGap = 8;
        private int _steadyValueWidth;
        private int _columnGap = 20;
        private int _minimumValueWidth = 50;
        private readonly List<StatusColumn> _columns = new List<StatusColumn>();
        private readonly ToolTip _toolTip = new ToolTip { AutoPopDelay = 15000, ShowAlways = true };
        private readonly bool _scrollbarsInsideClient = Type.GetType("Mono.Runtime") != null;
        private bool _updatingLayout;

        public StatusControl()
        {
            InitializeComponent();
            _allItems = new List<StatusItem>();
            _filteredItems = new List<StatusItem>();

            _updateTimer = new Timer();
            _updateTimer.Interval = 100;
            _updateTimer.Tick += UpdateTimer_Tick;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            _searchBox = new PlaceholderTextBox();
            _searchBox.Dock = DockStyle.Top;
            _searchBox.Font = new Font(this.Font.FontFamily, 10);
            _searchBox.PlaceholderText = UiText.Translate("Search");
            _searchBox.PlaceholderColor = Color.Gray;
            _searchBox.TextChanged += SearchBox_TextChanged;
            _searchBox.MinimumSize = new Size(0, 28);
            _searchBox.Height = 28;

            _contentPanel = new DoubleBufferedPanel();
            _contentPanel.Dock = DockStyle.Fill;
            _contentPanel.AutoScroll = true;
            _contentPanel.Paint += ContentPanel_Paint;
            _contentPanel.ClientSizeChanged += (sender, e) => UpdateScrollSize();
            _contentPanel.MouseMove += ContentPanel_MouseMove;
            _contentPanel.MouseLeave += (sender, e) => ClearToolTip();
            _contentPanel.Scroll += (sender, e) => ClearToolTip();

            this.Controls.Add(_contentPanel);
            this.Controls.Add(_searchBox);
            this.ResumeLayout(false);
        }

        public void Start()
        {
            BuildItemList();
            _updateTimer.Start();
        }

        public void Stop()
        {
            _updateTimer.Stop();
        }

        private void BuildItemList()
        {
            _allItems.Clear();
            var list = MainV2.comPort.MAV.cs.GetItemList(true);
            foreach (var field in list)
            {
                _allItems.Add(new StatusItem { Name = field, DisplayName = TelemetryFieldPresentation.FieldLabel(field), Value = "" });
            }

            _searchBox.SetAutoCompleteSource(_allItems.SelectMany(x => new[] { x.Name, x.DisplayName }).Distinct().ToList());
            // Reopening the tab or reconnecting must retain the visible search query.
            SearchBox_TextChanged(this, EventArgs.Empty);
        }

        private void EnsureFonts()
        {
            if (_boldFont == null)
            {
                _boldFont = new Font(this.Font, FontStyle.Bold);
                _normalFont = new Font(this.Font, FontStyle.Regular);
            }
        }

        private void UpdateScrollSize()
        {
            if (_filteredItems == null || _contentPanel == null || _updatingLayout)
                return;
            _updatingLayout = true;
            try
            {
                EnsureFonts();
                using (var graphics = _contentPanel.CreateGraphics())
                {
                    // Rendered font metrics include both DPI and user font changes.
                    float scale = Math.Max(_boldFont.GetHeight(graphics), _normalFont.GetHeight(graphics)) / 13f;
                    _padding = Math.Max(1, (int)Math.Ceiling(8 * scale));
                    _nameValueGap = Math.Max(1, (int)Math.Ceiling(6 * scale));
                    _columnGap = Math.Max(1, (int)Math.Ceiling(27 * scale));
                    _rowHeight = Math.Max(1, (int)Math.Ceiling(17 * scale));
                    _minimumValueWidth = (int)Math.Ceiling(50 * scale);
                    _steadyValueWidth = (int)Math.Ceiling(28 * scale);
                    var nameWidths = _filteredItems.Select(item =>
                        (int)Math.Ceiling(graphics.MeasureString(item.DisplayName ?? item.Name ?? "", _boldFont).Width)).ToArray();
                    var valueWidths = _filteredItems.Select(item =>
                        (int)Math.Ceiling(graphics.MeasureString(item.Value ?? "", _normalFont).Width)).ToArray();

                    // Scrollbars change the client area and hence the rows per group.
                    // Settle against that area so the last row stays above the bar.
                    var viewport = _contentPanel.Size;
                    for (int pass = 0; pass < 4; pass++)
                    {
                        int rows = Math.Max(1, (viewport.Height - _padding * 2) / _rowHeight);
                        int nameLimit = Math.Max(_rowHeight * 3,
                            Math.Min((int)Math.Ceiling(240 * scale), (int)(viewport.Width * 0.55)));
                        // Fill each group top to bottom, then continue right as in the upstream
                        // Status tab. The native horizontal scrollbar reaches the remaining groups.
                        int groupNameLimit = _filteredItems.Count > rows
                            ? Math.Max(_rowHeight * 3, Math.Min(nameLimit, viewport.Width * 29 / 100))
                            : nameLimit;
                        int width = BuildColumns(rows, groupNameLimit, viewport.Width, nameWidths, valueWidths,
                            graphics, nameLimit);
                        int height = _padding * 2 + Math.Min(rows, _filteredItems.Count) * _rowHeight;
                        int scrollPageHeight = _contentPanel.ClientSize.Height;
                        if (_scrollbarsInsideClient && _contentPanel.HorizontalScroll.Visible)
                            scrollPageHeight = Math.Max(0, scrollPageHeight - SystemInformation.HorizontalScrollBarHeight);
                        _contentPanel.AutoScrollMinSize = _filteredItems.Count == 0 ? Size.Empty :
                            new Size(width,
                                MinimumScrollHeight(height, viewport.Height, scrollPageHeight));
                        if (ViewportSize() == viewport)
                            break;
                        viewport = ViewportSize();
                    }
                }
                _contentPanel.Invalidate();
            }
            finally { _updatingLayout = false; }
        }

        private int BuildColumns(int rows, int nameLimit, int availableWidth, int[] nameWidths,
            int[] valueWidths, Graphics graphics, int trailingNameLimit)
        {
            _columns.Clear();
            int left = _padding;
            int width = 0;
            for (int start = 0; start < _filteredItems.Count; start += rows)
            {
                int count = Math.Min(rows, _filteredItems.Count - start);
                // The last group has no following group to push away.
                int labelLimit = start + count == _filteredItems.Count
                    ? trailingNameLimit : nameLimit;
                var column = new StatusColumn { Start = start, Count = count, Left = left,
                    ValueWidth = _minimumValueWidth };
                for (int i = start; i < start + count; i++)
                {
                    column.NameWidth = Math.Max(column.NameWidth, Math.Min(labelLimit, nameWidths[i]));
                    column.ValueWidth = Math.Max(column.ValueWidth, valueWidths[i]);
                }
                if (start == 0 && count == _filteredItems.Count)
                {
                    int availableNameWidth = availableWidth - _padding * 2 - _nameValueGap - column.ValueWidth;
                    int minimumLabelWidth = Math.Min(column.NameWidth,
                        (int)Math.Ceiling(graphics.MeasureString("…", _boldFont).Width));
                    if (availableNameWidth >= minimumLabelWidth)
                        column.NameWidth = Math.Min(column.NameWidth, availableNameWidth);
                }

                column.Labels = new string[count];
                column.LabelWidths = new int[count];
                column.ValueWidths = new int[count];
                for (int row = 0; row < count; row++)
                {
                    var item = _filteredItems[start + row];
                    column.Labels[row] = FitName(item.DisplayName ?? item.Name ?? "", nameWidths[start + row],
                        column.NameWidth, graphics);
                    column.LabelWidths[row] = Math.Min(column.NameWidth, Math.Max(1,
                        (int)Math.Ceiling(graphics.MeasureString(column.Labels[row], _boldFont).Width)));
                    column.ValueWidths[row] = Math.Max(1, valueWidths[start + row]);
                    column.Width = Math.Max(column.Width,
                        column.LabelWidths[row] + _nameValueGap + Math.Max(_steadyValueWidth, column.ValueWidths[row]));
                }
                width = left + column.Width + _padding;
                _columns.Add(column);
                left += column.Width + _columnGap;
            }
            return width;
        }

        private static int MinimumScrollHeight(int contentHeight, int viewportHeight, int scrollPageHeight)
        {
            if (contentHeight <= viewportHeight)
                return 0;
            // The scroll range is compared with the runtime's physical page,
            // whereas rows also reserve a footer. Include that difference so a
            // mandatory single row can scroll fully above the reserved gutter.
            return contentHeight + Math.Max(0, scrollPageHeight - viewportHeight);
        }

        private Size ViewportSize()
        {
            // Native scrollbars and borders can shrink the drawable client area
            // without changing the panel's outer Size. Keep the row budget in
            // client coordinates; only Mono draws its scrollbars inside the client.
            var size = _contentPanel.ClientSize;
            if (_scrollbarsInsideClient && _contentPanel.HorizontalScroll.Visible)
                size.Height = Math.Max(0, size.Height - SystemInformation.HorizontalScrollBarHeight);
            if (_scrollbarsInsideClient && _contentPanel.VerticalScroll.Visible)
                size.Width = Math.Max(0, size.Width - SystemInformation.VerticalScrollBarWidth);
            return size;
        }

        private static Color ChooseNameColor(Color accent, Color foreground, Color background)
        {
            const double minimum = 4.5; // Normal-size text, including bold UI labels.
            double bg = RelativeLuminance(background.R, background.G, background.B);
            if (ContrastRatio(accent, background, bg) >= minimum)
                return accent;
            if (ContrastRatio(foreground, background, bg) >= minimum)
                return foreground;

            // A custom theme may provide neither a readable accent nor foreground.
            return ContrastRatio(Color.Black, background, bg) >= ContrastRatio(Color.White, background, bg)
                ? Color.Black : Color.White;
        }

        private static double ContrastRatio(Color foreground, Color background, double backgroundLuminance)
        {
            double opacity = foreground.A / 255.0;
            double luminance = RelativeLuminance(
                background.R + (foreground.R - background.R) * opacity,
                background.G + (foreground.G - background.G) * opacity,
                background.B + (foreground.B - background.B) * opacity);
            return (Math.Max(luminance, backgroundLuminance) + 0.05) /
                   (Math.Min(luminance, backgroundLuminance) + 0.05);
        }

        private static double RelativeLuminance(double red, double green, double blue)
        {
            return 0.2126 * LinearChannel(red) + 0.7152 * LinearChannel(green) + 0.0722 * LinearChannel(blue);
        }

        private static double LinearChannel(double channel)
        {
            channel /= 255.0;
            return channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
        }

        private void ContentPanel_Paint(object sender, PaintEventArgs e)
        {
            if (_filteredItems.Count == 0)
                return;
            EnsureFonts();
            var g = e.Graphics;
            int leftEdge = -_contentPanel.AutoScrollPosition.X;
            int rightEdge = leftEdge + _contentPanel.ClientSize.Width;
            g.TranslateTransform(_contentPanel.AutoScrollPosition.X, _contentPanel.AutoScrollPosition.Y);
            g.Clear(_contentPanel.BackColor);

            using (var nameBrush = new SolidBrush(ChooseNameColor(
                       ThemeManager.BannerColor2, _contentPanel.ForeColor, _contentPanel.BackColor)))
            using (var valueBrush = new SolidBrush(_contentPanel.ForeColor))
            using (var format = new StringFormat { FormatFlags = StringFormatFlags.NoWrap,
                Trimming = StringTrimming.EllipsisCharacter })
            {
                foreach (var column in _columns)
                {
                    if (column.Left >= rightEdge) break;
                    if (column.Left + column.Width <= leftEdge) continue;
                    for (int row = 0; row < column.Count; row++)
                    {
                        var item = _filteredItems[column.Start + row];
                        int y = _padding + row * _rowHeight;
                        // Long names show an ellipsis and a full row tooltip; each
                        // group measures only its own filtered labels and values.
                        g.DrawString(column.Labels[row], _boldFont, nameBrush,
                            new Rectangle(column.Left, y, column.LabelWidths[row], _rowHeight), format);
                        g.DrawString(item.Value ?? "", _normalFont, valueBrush,
                            new Rectangle(column.Left + column.LabelWidths[row] + _nameValueGap, y,
                                column.ValueWidths[row], _rowHeight), format);
                    }
                }
            }
        }

        private string FitName(string name, int measuredWidth, int width, Graphics graphics)
        {
            int lineEnd = name.IndexOfAny(new[] { '\r', '\n' });
            if (lineEnd < 0 && measuredWidth <= width)
                return name;
            if (lineEnd >= 0)
                name = name.Substring(0, lineEnd);
            // Some GDI+ implementations clip without rendering StringTrimming's
            // ellipsis. Fit an explicit marker, preserving Unicode text elements.
            var elements = StringInfo.ParseCombiningCharacters(name);
            int low = 0, high = elements.Length;
            while (low < high)
            {
                int mid = (low + high + 1) / 2;
                int end = mid == elements.Length ? name.Length : elements[mid];
                if (graphics.MeasureString(name.Substring(0, end) + "…", _boldFont).Width <= width)
                    low = mid;
                else
                    high = mid - 1;
            }
            return name.Substring(0, low == elements.Length ? name.Length : elements[low]) + "…";
        }

        private void UpdateValueWidths()
        {
            if (_columns.Count == 0)
                return;
            var viewport = ViewportSize();
            using (var graphics = _contentPanel.CreateGraphics())
            {
                int left = _padding;
                foreach (var column in _columns)
                {
                    column.ValueWidth = _minimumValueWidth;
                    column.Width = 0;
                    for (int row = 0; row < column.Count; row++)
                    {
                        int valueWidth = Math.Max(1, (int)Math.Ceiling(graphics.MeasureString(
                            _filteredItems[column.Start + row].Value ?? "", _normalFont).Width));
                        column.ValueWidth = Math.Max(column.ValueWidth, valueWidth);
                        column.ValueWidths[row] = valueWidth;
                        column.Width = Math.Max(column.Width, column.LabelWidths[row] + _nameValueGap + Math.Max(_steadyValueWidth, valueWidth));
                    }
                    column.Left = left;
                    left += column.Width + _columnGap;
                }
                _contentPanel.AutoScrollMinSize = new Size(left - _columnGap + _padding,
                    _contentPanel.AutoScrollMinSize.Height);
            }
            // Mono can change bar visibility without raising ClientSizeChanged.
            // Ordinary samples only need new value widths, not name measurements.
            if (ViewportSize() != viewport)
                UpdateScrollSize();
            _contentPanel.Invalidate();
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (!this.Visible)
                return;
            try
            {
                var cs = MainV2.comPort.MAV.cs;
                bool changed = false;
                foreach (var item in _allItems)
                {
                    var prop = typeof(CurrentState).GetProperty(item.Name);
                    if (prop != null)
                    {
                        var val = prop.GetValue(cs)?.ToString() ?? "";
                        if (item.Value != val)
                        {
                            item.Value = val;
                            changed = true;
                        }
                    }
                }
                // Later values can be longer than the first sample. Recalculate
                // without changing the query, item order or current scroll position.
                if (changed)
                    UpdateValueWidths();
            }
            catch
            {
                // Ignore errors during update
            }
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            var searchText = _searchBox.Text.Trim();
            if (string.IsNullOrEmpty(searchText))
                _filteredItems = _allItems;
            else
                _filteredItems = _allItems.Where(x => TelemetryFieldPresentation.Matches(searchText, x.Name, x.DisplayName)).ToList();

            ClearToolTip();
            UpdateScrollSize();
            // Apply after reflow: WinForms can clamp the old offset as the scroll
            // extent changes. Every new search starts with the field name visible.
            _contentPanel.AutoScrollPosition = Point.Empty;
            _contentPanel.Invalidate();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            _boldFont?.Dispose();
            _normalFont?.Dispose();
            _boldFont = null;
            _normalFont = null;
            UpdateScrollSize();
        }

#if !LIB
        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            UpdateScrollSize();
        }
#endif

        private StatusItem ItemAt(Point location)
        {
            location.Offset(-_contentPanel.AutoScrollPosition.X, -_contentPanel.AutoScrollPosition.Y);
            if (location.Y < _padding)
                return null;
            int row = (location.Y - _padding) / _rowHeight;
            foreach (var column in _columns)
                if (row < column.Count && location.X >= column.Left && location.X < column.Left + column.Width)
                    return _filteredItems[column.Start + row];
            return null;
        }

        private void ContentPanel_MouseMove(object sender, MouseEventArgs e)
        {
            var item = ItemAt(e.Location);
            var text = item == null ? null : item.DisplayName + Environment.NewLine + item.Value;
            if (_toolTip.GetToolTip(_contentPanel) != text)
                _toolTip.SetToolTip(_contentPanel, text);
        }

        private void ClearToolTip()
        {
            _toolTip.SetToolTip(_contentPanel, null);
        }

        public void ApplyTheme()
        {
            this.BackColor = ThemeManager.BGColor;
            _searchBox.ApplyTheme();
            _contentPanel.BackColor = ThemeManager.BGColor;
            _contentPanel.ForeColor = ThemeManager.TextColor;
            _contentPanel.Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _updateTimer?.Stop();
                _updateTimer?.Dispose();
                _boldFont?.Dispose();
                _normalFont?.Dispose();
                _toolTip.Dispose();
            }
            base.Dispose(disposing);
        }

        public class StatusItem
        {
            public string Name { get; set; }
            public string DisplayName { get; set; }
            public string Value { get; set; }
        }

        private class StatusColumn
        {
            public int Start, Count, Left, Width, NameWidth, ValueWidth;
            public string[] Labels;
            public int[] LabelWidths, ValueWidths;
        }

        private class DoubleBufferedPanel : Panel
        {
            public DoubleBufferedPanel()
            {
                this.DoubleBuffered = true;
                this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                              ControlStyles.UserPaint |
                              ControlStyles.OptimizedDoubleBuffer, true);
                this.UpdateStyles();
            }
        }
    }
}
