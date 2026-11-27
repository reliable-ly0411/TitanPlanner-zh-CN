using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using MissionPlanner.ArduPilot;
using MissionPlanner.Utilities;

namespace MissionPlanner.Controls
{
    public partial class DisplayThisForm : Form
    {
        private readonly Func<string, bool> _isChecked;
        private readonly EventHandler _checkChangedHandler;
        private readonly MouseEventHandler _mouseDownHandler;
        private readonly bool _scrollbarsInsideClient = Type.GetType("Mono.Runtime") != null;
        private readonly ToolTip _toolTip = new ToolTip { AutoPopDelay = 15000, ShowAlways = true };
        private readonly List<DisplayRow> _layoutRows = new List<DisplayRow>();
        private PlaceholderTextBox _searchBox;
        private DoubleBufferedPanel _contentPanel;
        private List<DisplayItem> _allItems;
        private List<DisplayItem> _filteredItems;
        private Timer _updateTimer;
        private Font _normalFont;
        private Font _headerFont;
        private int _rowHeight = 18;
        private int _padding = 10;
        private int _checkboxSize = 14;
        private int _checkboxMargin = 4;
        // Intrinsic measurements; each displayed column has its own bounded cells.
        private int _intrinsicNameWidth = 140;
        private int _valueWidth = 80;
        private int _nameValueGap = 8;
        private int _columnGap = 20;
        private bool _updatingLayout;
        private bool _viewportLayoutQueued;
        private bool _resourcesDisposed;
        private bool _mouseInside;
        private DisplayItem _hoveredItem;
        private string _hoveredHeader;

        public DisplayThisForm(Func<string, bool> isChecked, EventHandler checkChangedHandler,
            MouseEventHandler mouseDownHandler = null)
        {
            _isChecked = isChecked;
            _checkChangedHandler = checkChangedHandler;
            _mouseDownHandler = mouseDownHandler;
            _allItems = new List<DisplayItem>();
            _filteredItems = new List<DisplayItem>();
            InitializeComponent();
            SetupControls();
        }

        private bool _includeSelectionClickInChange;
        private bool _honorCanceledHudSelection;

        internal static DisplayThisForm CreateTuningChooser(Func<string, bool> isChecked, EventHandler checkChangedHandler)
        {
            // Tuning needs the axis from this click. Public and HUD callers keep the existing EventArgs.Empty contract.
            return new DisplayThisForm(isChecked, checkChangedHandler) { _includeSelectionClickInChange = true };
        }

        internal static DisplayThisForm CreateHudChooser(Func<string, bool> isChecked, EventHandler checkChangedHandler)
        {
            return new DisplayThisForm(isChecked, checkChangedHandler) { _honorCanceledHudSelection = true };
        }

        private void SetupControls()
        {
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
            _contentPanel.MouseClick += ContentPanel_MouseClick;
            _contentPanel.MouseMove += ContentPanel_MouseMove;
            _contentPanel.MouseLeave += ContentPanel_MouseLeave;
            _contentPanel.ClientSizeChanged += (sender, e) => QueueViewportLayout();
            _contentPanel.Scroll += (sender, e) => ClearHover();
            this.Controls.Add(_contentPanel);
            this.Controls.Add(_searchBox);

            _updateTimer = new Timer { Interval = 200 };
            _updateTimer.Tick += UpdateTimer_Tick;
            this.Disposed += (sender, e) => DisposeResources();
        }

        private void DisplayThisForm_Load(object sender, EventArgs e)
        {
            var screen = Screen.FromControl(this);
            this.Width = (int)(screen.WorkingArea.Width * 0.7);
            this.Height = (int)(screen.WorkingArea.Height * 0.7);
            this.CenterToScreen();
            BuildItemList();
            _updateTimer.Start();
            ThemeManager.ApplyThemeTo(this);
            ApplyTheme();
        }

        private void BuildItemList()
        {
            _allItems.Clear();
            object thisBoxed = MainV2.comPort.MAV.cs;
            Type test = thisBoxed.GetType();
            foreach (var field in test.GetProperties())
            {
                object fieldValue = field.GetValue(thisBoxed, null);
                if (fieldValue == null || !fieldValue.IsNumber())
                    continue;
                string group = CurrentState.GetGroupText(field.Name);
                if (string.IsNullOrEmpty(group)) group = "Other";
                string displayName = TelemetryFieldPresentation.FieldLabel(field.Name);
                if (field.Name.Contains("customfield"))
                {
                    if (CurrentState.custom_field_names.ContainsKey(field.Name))
                        displayName = CurrentState.custom_field_names[field.Name];
                    else
                        continue;
                }
                _allItems.Add(new DisplayItem
                {
                    Name = field.Name, DisplayName = displayName, Group = group,
                    GroupLabel = TelemetryFieldPresentation.GroupLabel(group),
                    Value = fieldValue?.ToString() ?? "", IsChecked = _isChecked(field.Name)
                });
            }
            _allItems = _allItems.OrderBy(f => f.Group).ThenBy(f => f.DisplayName).ToList();
            _searchBox.SetAutoCompleteSource(_allItems.SelectMany(x => new[] { x.Name, x.DisplayName }).Distinct().ToList());
            SearchBox_TextChanged(this, EventArgs.Empty);
        }

        private void EnsureFonts()
        {
            if (_normalFont != null) return;
            _normalFont = new Font(this.Font, FontStyle.Regular);
            _headerFont = new Font(this.Font.FontFamily, this.Font.SizeInPoints + 1, FontStyle.Bold);
        }

        private Size ViewportSize()
        {
            var size = _contentPanel.ClientSize;
            if (_scrollbarsInsideClient)
            {
                if (_contentPanel.HorizontalScroll.Visible)
                    size.Height = Math.Max(0, size.Height - SystemInformation.HorizontalScrollBarHeight);
                if (_contentPanel.VerticalScroll.Visible)
                    size.Width = Math.Max(0, size.Width - SystemInformation.VerticalScrollBarWidth);
            }
            return size;
        }

        private List<DisplayColumn> BuildColumns(int capacity)
        {
            var columns = new List<DisplayColumn>();
            DisplayColumn column = null;
            string group = null;
            foreach (var item in _filteredItems)
            {
                bool newGroup = column == null || item.Group != group;
                if (column == null || column.Rows.Count >= capacity ||
                    (newGroup && column.Rows.Count >= capacity - 1))
                {
                    column = new DisplayColumn();
                    columns.Add(column);
                }
                if (newGroup || column.Rows.Count == 0)
                    column.Rows.Add(new DisplayRow { IsHeader = true, Item = item,
                        FullText = item.GroupLabel ?? item.Group ?? "" });
                column.Rows.Add(new DisplayRow { Item = item, FullText = item.DisplayName ?? item.Name ?? "" });
                group = item.Group;
            }
            return columns;
        }

        private void QueueViewportLayout()
        {
            if (_viewportLayoutQueued || _updatingLayout || _resourcesDisposed || !_contentPanel.IsHandleCreated)
                return;

            // Defer scrollbar calculation until the parent has finished the resize layout.
            _viewportLayoutQueued = true;
            _contentPanel.BeginInvoke((Action)(() =>
            {
                _viewportLayoutQueued = false;
                if (!_resourcesDisposed && !_contentPanel.IsDisposed)
                    UpdateScrollSize();
            }));
        }

        private void UpdateScrollSize()
        {
            if (_contentPanel == null || _filteredItems == null || _updatingLayout || _resourcesDisposed)
                return;
            _updatingLayout = true;
            try
            {
                EnsureFonts();
                using (var graphics = _contentPanel.CreateGraphics())
                {
                    float scale = Math.Max(_normalFont.GetHeight(graphics), _headerFont.GetHeight(graphics)) / 13f;
                    _padding = Math.Max(1, (int)Math.Ceiling(8 * scale));
                    _checkboxSize = Math.Max(1, (int)Math.Ceiling(14 * scale));
                    _checkboxMargin = Math.Max(1, (int)Math.Ceiling(4 * scale));
                    _nameValueGap = Math.Max(1, (int)Math.Ceiling(8 * scale));
                    _columnGap = Math.Max(1, (int)Math.Ceiling(20 * scale));
                    _rowHeight = Math.Max(_checkboxSize + 4, (int)Math.Ceiling(17 * scale));
                    int minimumValue = (int)Math.Ceiling(50 * scale);
                    // Enhanced Chinese labels include both the original key and its translation.
                    // Let the actual viewport, value width and inter-column gap size these labels.
                    bool enhancedChinese = string.Equals(CultureInfo.CurrentUICulture.Name, "zh-CN", StringComparison.Ordinal);
                    int maximumName = enhancedChinese ? int.MaxValue
                        : (int)Math.Ceiling(graphics.MeasureString("MMMMMMMMMMMMMMMM", _normalFont).Width);
                    var names = _filteredItems.ToDictionary(item => item,
                        item => (int)Math.Ceiling(graphics.MeasureString(item.DisplayName ?? item.Name ?? "", _normalFont).Width) + 8);
                    var values = _filteredItems.ToDictionary(item => item,
                        item => Math.Max(minimumValue, (int)Math.Ceiling(graphics.MeasureString(item.Value ?? "", _normalFont).Width) + 2));
                    _intrinsicNameWidth = names.Count == 0 ? 0 : names.Values.Max();
                    _valueWidth = values.Count == 0 ? minimumValue : values.Values.Max();
                    int headers = _filteredItems.Select(item => item.Group).Distinct().Count();
                    var viewport = _contentPanel.Size;
                    for (int pass = 0; pass < 4; pass++)
                    {
                        _layoutRows.Clear();
                        if (_filteredItems.Count == 0)
                        {
                            _contentPanel.AutoScrollMinSize = Size.Empty;
                            break;
                        }
                        int innerWidth = Math.Max(1, viewport.Width - _padding * 2);
                        int nameLimit = NameLimit(innerWidth, _valueWidth, maximumName);
                        int estimatedWidth = _checkboxSize + _checkboxMargin + Math.Min(_intrinsicNameWidth, nameLimit) + _nameValueGap + _valueWidth;
                        int count = Math.Max(1, Math.Min(_filteredItems.Count, (innerWidth + _columnGap) / (estimatedWidth + _columnGap)));
                        // Fit complete columns to the width. Extra fields extend downwards,
                        // with repeated headers when a group continues in another column.
                        int low = 2, high = Math.Max(2, _filteredItems.Count + headers);
                        while (low < high)
                        {
                            int mid = (low + high) / 2;
                            if (BuildColumns(mid).Count <= count) high = mid;
                            else low = mid + 1;
                        }
                        var columns = BuildColumns(maximumName == int.MaxValue && count == 1 ? int.MaxValue : low);
                        int x = _padding, bottom = _padding;
                        foreach (var column in columns)
                        {
                            int valueWidth = column.Rows.Where(row => !row.IsHeader).Max(row => values[row.Item]);
                            int nameWidth = Math.Min(column.Rows.Where(row => !row.IsHeader).Max(row => names[row.Item]),
                                NameLimit(innerWidth, valueWidth, maximumName));
                            int width = _checkboxSize + _checkboxMargin + nameWidth + _nameValueGap + valueWidth;
                            int descriptionWidth = width - _checkboxSize - _checkboxMargin;
                            int y = _padding;
                            foreach (var row in column.Rows)
                            {
                                // On smaller panes put the original field key above its localized description
                                // instead of losing the description to a one-line ellipsis.
                                var description = enhancedChinese && !row.IsHeader && names[row.Item] - 8 > nameWidth
                                    ? BilingualDescription(row.Item)
                                    : null;
                                int descriptionHeight = description == null ? 0 : Math.Max(_rowHeight,
                                    (int)Math.Ceiling(graphics.MeasureString(description, _normalFont, descriptionWidth).Height));
                                int rowHeight = _rowHeight + descriptionHeight;
                                row.Bounds = new Rectangle(x, y, width, rowHeight);
                                if (row.IsHeader)
                                    row.NameBounds = row.Bounds;
                                else
                                {
                                    int checkRowHeight = Math.Min(rowHeight, 2 * _rowHeight);
                                    row.CheckBounds = new Rectangle(x, y + (checkRowHeight - _checkboxSize) / 2, _checkboxSize, _checkboxSize);
                                    row.NameBounds = new Rectangle(x + _checkboxSize + _checkboxMargin, y, nameWidth, _rowHeight);
                                    row.ValueBounds = new Rectangle(row.NameBounds.Right + _nameValueGap,
                                        y, valueWidth, _rowHeight);
                                    row.Item.Bounds = row.Bounds;
                                }
                                row.Label = FitLabel(description == null ? row.FullText : row.Item.Name,
                                    row.IsHeader ? _headerFont : _normalFont, row.NameBounds.Width, graphics);
                                if (description != null)
                                {
                                    // The value occupies only the key line. Reuse that width below it.
                                    row.DescriptionBounds = new Rectangle(row.NameBounds.Left, y + _rowHeight,
                                        descriptionWidth, descriptionHeight);
                                    row.Description = description;
                                }
                                _layoutRows.Add(row);
                                y += rowHeight;
                            }
                            bottom = Math.Max(bottom, y);
                            x += width + _columnGap;
                        }
                        _contentPanel.AutoScrollMinSize = new Size(x - _columnGap + _padding, bottom + _padding);
                        if (ViewportSize() == viewport) break;
                        viewport = ViewportSize();
                    }
                }
            }
            finally { _updatingLayout = false; }
            _contentPanel.Invalidate();
            if (_mouseInside)
                UpdateHover(RowAtPoint(_contentPanel.PointToClient(Cursor.Position)));
        }

        private int NameLimit(int width, int valueWidth, int maximumName)
        {
            return Math.Max(_rowHeight * 3, Math.Min(maximumName,
                width - _checkboxSize - _checkboxMargin - _nameValueGap - valueWidth));
        }

        private static string BilingualDescription(DisplayItem item)
        {
            // TelemetryFieldPresentation.Combine uses "key (description)"; never
            // reinterpret a name authored by the operator as a translated field.
            var key = item.Name;
            var label = item.DisplayName;
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(label) || key.Contains("customfield"))
                return null;
            var prefix = key + " (";
            return label.Length > prefix.Length + 1 && label.StartsWith(prefix, StringComparison.Ordinal) &&
                   label.EndsWith(")", StringComparison.Ordinal)
                ? label.Substring(prefix.Length, label.Length - prefix.Length - 1)
                : null;
        }

        private static string FitLabel(string text, Font font, int width, Graphics graphics)
        {
            int lineEnd = text.IndexOfAny(new[] { '\r', '\n' });
            if (lineEnd < 0 && graphics.MeasureString(text, font).Width <= width) return text;
            if (lineEnd >= 0) text = text.Substring(0, lineEnd);
            var elements = StringInfo.ParseCombiningCharacters(text);
            int low = 0, high = elements.Length;
            while (low < high)
            {
                int mid = (low + high + 1) / 2;
                int end = mid == elements.Length ? text.Length : elements[mid];
                if (graphics.MeasureString(text.Substring(0, end) + "…", font).Width <= width) low = mid;
                else high = mid - 1;
            }
            return text.Substring(0, low == elements.Length ? text.Length : elements[low]) + "…";
        }

        private Color ChooseTextColor(Color preferred)
            => ThemeTextContrast.Choose(preferred, _contentPanel.ForeColor, _contentPanel.BackColor);

        private void ContentPanel_Paint(object sender, PaintEventArgs e)
        {
            if (_resourcesDisposed) return;
            EnsureFonts();
            var g = e.Graphics;
            g.TranslateTransform(_contentPanel.AutoScrollPosition.X, _contentPanel.AutoScrollPosition.Y);
            g.Clear(_contentPanel.BackColor);
            using (var headerBrush = new SolidBrush(ChooseTextColor(ThemeManager.BannerColor2)))
            using (var nameBrush = new SolidBrush(_contentPanel.ForeColor))
            using (var valueBrush = new SolidBrush(ChooseTextColor(Color.Gray)))
            using (var checkPen = new Pen(_contentPanel.ForeColor, 1))
            using (var checkFill = new SolidBrush(Color.Green))
            using (var hoverBrush = new SolidBrush(Color.FromArgb(40, 128, 128, 128)))
            using (var format = new StringFormat { FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter })
            {
                foreach (var row in _layoutRows)
                {
                    if (row.IsHeader)
                    {
                        g.DrawString(row.Label, _headerFont, headerBrush, row.NameBounds, format);
                        continue;
                    }
                    if (row.Item == _hoveredItem) g.FillRectangle(hoverBrush, row.Bounds);
                    g.DrawRectangle(checkPen, row.CheckBounds);
                    if (row.Item.IsChecked)
                    {
                        var inside = row.CheckBounds;
                        inside.Inflate(-2, -2);
                        g.FillRectangle(checkFill, inside);
                    }
                    g.DrawString(row.Label, _normalFont, nameBrush, row.NameBounds, format);
                    if (row.Description != null)
                        g.DrawString(row.Description, _normalFont, nameBrush, row.DescriptionBounds);
                    g.DrawString(row.Item.Value ?? "", _normalFont, valueBrush, row.ValueBounds, format);
                }
            }
        }

        private void ContentPanel_MouseClick(object sender, MouseEventArgs e)
        {
            var item = GetItemAtPoint(e.Location);
            if (item == null) return;
            item.IsChecked = !item.IsChecked;
            var fakeCheckbox = new CheckBox { Name = item.Name, Text = item.DisplayName, Checked = item.IsChecked, Tag = "custom" };
            if (e.Button == MouseButtons.Right && _mouseDownHandler != null)
                _mouseDownHandler(fakeCheckbox, e);
            _checkChangedHandler?.Invoke(fakeCheckbox, _includeSelectionClickInChange ? e : EventArgs.Empty);
            if (_honorCanceledHudSelection)
                item.IsChecked = fakeCheckbox.Checked;
            fakeCheckbox.Dispose();
            _contentPanel.Invalidate();
        }

        private DisplayRow RowAtPoint(Point point)
        {
            point.Offset(-_contentPanel.AutoScrollPosition.X, -_contentPanel.AutoScrollPosition.Y);
            return _layoutRows.FirstOrDefault(row => row.Bounds.Contains(point));
        }

        private DisplayItem GetItemAtPoint(Point point)
        {
            var row = RowAtPoint(point);
            return row == null || row.IsHeader ? null : row.Item;
        }

        private void UpdateHover(DisplayRow row)
        {
            if (_resourcesDisposed) return;
            var item = row == null || row.IsHeader ? null : row.Item;
            var header = row != null && row.IsHeader ? row.FullText : null;
            if (item == _hoveredItem && header == _hoveredHeader) return;
            _hoveredItem = item;
            _hoveredHeader = header;
            _contentPanel.Cursor = item != null ? Cursors.Hand : Cursors.Default;
            _toolTip.SetToolTip(_contentPanel, item == null ? header : (item.DisplayName ?? item.Name) + Environment.NewLine + item.Value);
            _contentPanel.Invalidate();
        }

        private void ClearHover()
        {
            if (_resourcesDisposed || _contentPanel == null) return;
            _hoveredItem = null;
            _hoveredHeader = null;
            _toolTip.SetToolTip(_contentPanel, null);
            _contentPanel.Cursor = Cursors.Default;
            _contentPanel.Invalidate();
        }

        private void ContentPanel_MouseMove(object sender, MouseEventArgs e)
        {
            _mouseInside = true;
            UpdateHover(RowAtPoint(e.Location));
        }

        private void ContentPanel_MouseLeave(object sender, EventArgs e)
        {
            _mouseInside = false;
            ClearHover();
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (!this.Visible) return;
            try
            {
                var cs = MainV2.comPort.MAV.cs;
                bool changed = false;
                foreach (var item in _allItems)
                {
                    var prop = typeof(CurrentState).GetProperty(item.Name);
                    if (prop == null) continue;
                    var val = prop.GetValue(cs)?.ToString() ?? "";
                    if (item.Value != val) { item.Value = val; changed = true; }
                }
                if (changed)
                {
                    // Keep click targets stable for ordinary live samples. A wider
                    // value still triggers measurement and a new reachable extent.
                    bool outgrown;
                    using (var graphics = _contentPanel.CreateGraphics())
                        outgrown = _layoutRows.Any(row => !row.IsHeader &&
                            graphics.MeasureString(row.Item.Value ?? "", _normalFont).Width + 2 > row.ValueBounds.Width);
                    if (outgrown) UpdateScrollSize();
                    else _contentPanel.Invalidate();
                }
            }
            catch { }
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            var text = _searchBox.Text.Trim();
            _filteredItems = string.IsNullOrEmpty(text) ? _allItems : _allItems.Where(item =>
                TelemetryFieldPresentation.Matches(text, item.DisplayName, item.Name, item.Group, item.GroupLabel)).ToList();
            ClearHover();
            UpdateScrollSize();
            _contentPanel.AutoScrollPosition = Point.Empty;
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            _normalFont?.Dispose();
            _headerFont?.Dispose();
            _normalFont = null;
            _headerFont = null;
            UpdateScrollSize();
        }

#if !LIB
        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            UpdateScrollSize();
        }
#endif

        private void ApplyTheme()
        {
            this.BackColor = ThemeManager.BGColor;
            _searchBox?.ApplyTheme();
            if (_contentPanel == null) return;
            _contentPanel.BackColor = ThemeManager.BGColor;
            _contentPanel.ForeColor = ThemeManager.TextColor;
            _contentPanel.Invalidate();
        }

        private void DisposeResources()
        {
            if (_resourcesDisposed) return;
            _resourcesDisposed = true;
            _updateTimer?.Stop();
            _updateTimer?.Dispose();
            _normalFont?.Dispose();
            _headerFont?.Dispose();
            _toolTip.Dispose();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            DisposeResources();
            base.OnFormClosing(e);
        }

        private class DisplayItem
        {
            public string Name { get; set; }
            public string DisplayName { get; set; }
            public string Group { get; set; }
            public string GroupLabel { get; set; }
            public string Value { get; set; }
            public bool IsChecked { get; set; }
            public Rectangle Bounds { get; set; }
        }

        private class DisplayRow
        {
            public DisplayItem Item;
            public bool IsHeader;
            public string FullText, Label, Description;
            public Rectangle DescriptionBounds;
            public Rectangle Bounds, NameBounds, ValueBounds, CheckBounds;
        }

        private class DisplayColumn
        {
            public readonly List<DisplayRow> Rows = new List<DisplayRow>();
        }

        private class DoubleBufferedPanel : Panel
        {
            public DoubleBufferedPanel()
            {
                this.DoubleBuffered = true;
                this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
                this.UpdateStyles();
            }
        }
    }
}
