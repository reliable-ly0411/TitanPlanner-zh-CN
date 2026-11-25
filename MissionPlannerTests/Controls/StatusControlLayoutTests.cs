using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MissionPlanner;
using MissionPlanner.Controls;

namespace MissionPlannerTests.Controls
{
    [TestClass]
    [DoNotParallelize]
    public class StatusControlLayoutTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        private static T Field<T>(object target, string name)
        {
            return (T)target.GetType().GetField(name, Private).GetValue(target);
        }

        private static object Call(object target, string name, params object[] args)
        {
            return target.GetType().GetMethod(name, Private).Invoke(target, args);
        }

        private static int Column(object column, string name)
        {
            return (int)column.GetType().GetField(name).GetValue(column);
        }

        private static List<StatusControl.StatusItem> Items(StatusControl control)
        {
            return Field<List<StatusControl.StatusItem>>(control, "_allItems");
        }

        private static void Reflow(StatusControl control)
        {
            Call(control, "SearchBox_TextChanged", control, EventArgs.Empty);
            Application.DoEvents();
        }

        private static void Sta(Action action)
        {
            Exception failure = null;
            var thread = new Thread(() =>
            {
                try { action(); }
                catch (Exception ex) { failure = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(30)), "Status layout test timed out.");
            if (failure != null) Assert.Fail(failure.ToString());
        }

        [DataTestMethod]
        [DataRow(0x94C11F, 0x000000, 0xEEEEEE, 0x000000)] // Built-in HighContrast light theme.
        [DataRow(0x94C11F, 0xFFFFFF, 0x262626, 0x94C11F)] // Preserve readable dark accent.
        [DataRow(0x999999, 0xAAAAAA, 0xEEEEEE, 0x000000)] // Both custom colors too faint.
        [DataRow(0x444444, 0x555555, 0x333333, 0xFFFFFF)]
        [DataRow(0xEEEEEE, 0x777777, 0xFFFFFF, 0x000000)] // A near miss below 4.5:1.
        public void StatusNamesSelectReadableThemeColors(int accent, int foreground, int background, int expected)
        {
            var choose = typeof(StatusControl).GetMethod("ChooseNameColor", BindingFlags.Static | BindingFlags.NonPublic);
            int opaque = unchecked((int)0xff000000);
            var actual = (Color)choose.Invoke(null, new object[]
            {
                Color.FromArgb(accent | opaque), Color.FromArgb(foreground | opaque), Color.FromArgb(background | opaque)
            });
            Assert.AreEqual(expected | opaque, actual.ToArgb());
        }

        [TestMethod]
        public void TranslucentAccentMustBeReadableAgainstTheActualPanel()
        {
            var choose = typeof(StatusControl).GetMethod("ChooseNameColor", BindingFlags.Static | BindingFlags.NonPublic);
            var actual = (Color)choose.Invoke(null, new object[]
            {
                Color.FromArgb(40, Color.Black), Color.Navy, Color.White
            });
            Assert.AreEqual(Color.Navy.ToArgb(), actual.ToArgb());
        }

        [TestMethod]
        public void FilteringMeasuresOnlyVisibleLabelsAndStartsAtTheLeft()
        {
            Sta(() =>
            {
                using (var form = new Form { ClientSize = new Size(320, 240) })
                using (var status = new StatusControl { Dock = DockStyle.Fill })
                {
                    form.Controls.Add(status);
                    form.Show();
                    Items(status).Add(new StatusControl.StatusItem { Name = "alt", DisplayName = "alt", Value = "12.5" });
                    for (int i = 0; i < 80; i++)
                        Items(status).Add(new StatusControl.StatusItem { Name = "other" + i,
                            DisplayName = "很长的中英文名称 / " + new string('W', 100), Value = i == 0 ? new string('8', 300) : "-12345.6789" });
                    Reflow(status);
                    var panel = Field<Panel>(status, "_contentPanel");
                    panel.AutoScrollPosition = new Point(97, 0);
                    Assert.IsTrue(panel.AutoScrollPosition.X < 0, "Exercise a real horizontal offset.");
                    Field<PlaceholderTextBox>(status, "_searchBox").Text = "ALT";
                    Application.DoEvents();
                    var columns = Field<IList>(status, "_columns");
                    Assert.AreEqual(1, columns.Count);
                    Assert.AreEqual(0, panel.AutoScrollPosition.X);
                    using (var graphics = panel.CreateGraphics())
                    using (var font = new Font(status.Font, FontStyle.Bold))
                        Assert.IsTrue(Column(columns[0], "NameWidth") <= Math.Ceiling(graphics.MeasureString("alt", font).Width) + 1,
                            "A hidden long label must not reserve the name/value gap.");
                    Assert.AreEqual("12.5", Items(status)[0].Value);
                }
            });
        }

        [DataTestMethod]
        [DataRow(320, 120, 1f)]
        [DataRow(320, 120, 1.25f)]
        [DataRow(320, 120, 1.5f)]
        [DataRow(320, 120, 2f)]
        [DataRow(640, 320, 1f)]
        [DataRow(640, 320, 1.25f)]
        [DataRow(640, 320, 1.5f)]
        [DataRow(640, 320, 2f)]
        [DataRow(960, 500, 1f)]
        [DataRow(960, 500, 1.25f)]
        [DataRow(960, 500, 1.5f)]
        [DataRow(960, 500, 2f)]
        [DataRow(1280, 800, 1f)]
        [DataRow(1280, 800, 1.25f)]
        [DataRow(1280, 800, 1.5f)]
        [DataRow(1280, 800, 2f)]
        [DataRow(160, 56, 2f)]
        public void GroupsKeepValuesAndRowsReachableAtDifferentSizesAndFontScales(int width, int height, float scale)
        {
            Sta(() =>
            {
                using (var font = new Font(SystemFonts.DefaultFont.FontFamily, SystemFonts.DefaultFont.SizeInPoints * scale))
                using (var form = new Form { ClientSize = new Size(width, height), AutoScaleMode = AutoScaleMode.None })
                using (var status = new StatusControl { Dock = DockStyle.Fill, Font = font })
                {
                    form.Controls.Add(status);
                    form.Show();
                    for (int i = 0; i < 70; i++)
                        Items(status).Add(new StatusControl.StatusItem { Name = "field" + i,
                            DisplayName = i % 3 == 0 ? "field" + i : "field" + i + " (长中英文名称 / " + new string('W', 100) + ")",
                            Value = i % 5 == 0 ? "-1234567890.1234567890123456789" : "12.5" });
                    Reflow(status);
                    AssertLayout(status);
                    var panel = Field<Panel>(status, "_contentPanel");
                    using (var bitmap = new Bitmap(panel.Width, panel.Height))
                        panel.DrawToBitmap(bitmap, panel.ClientRectangle);
                    int before = Field<int>(status, "_rowHeight");
                    using (var larger = new Font(font.FontFamily, font.SizeInPoints * 1.2f))
                    {
                        status.Font = larger;
                        Application.DoEvents();
                        Assert.IsTrue(Field<int>(status, "_rowHeight") > before, "Font-size changes must refresh cached fonts and rows.");
                        AssertLayout(status);
                    }
                }
            });
        }

        // The scrollbar is in the native Windows non-client area. Mono can paint it
        // inside the client rectangle, so retain its explicit in-client reservation.
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left, Top, Right, Bottom;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool GetClientRect(IntPtr handle, out NativeRect rect);

        private static int DrawableViewportHeight(Panel panel)
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                return Math.Max(0, panel.ClientSize.Height -
                    (panel.HorizontalScroll.Visible ? SystemInformation.HorizontalScrollBarHeight : 0));

            Assert.IsTrue(GetClientRect(panel.Handle, out var bounds), "Read the Windows drawable client rectangle.");
            Assert.AreEqual(panel.ClientSize.Height, bounds.Bottom - bounds.Top,
                "The Windows scrollbar must not be subtracted again from the client rectangle.");
            return bounds.Bottom - bounds.Top;
        }

        [TestMethod]
        public void NarrowCatalogUsesHorizontalScrollToReachTrailingGroups()
        {
            Sta(() =>
            {
                using (var form = new Form { ClientSize = new Size(320, 240) })
                using (var status = new StatusControl { Dock = DockStyle.Fill })
                {
                    form.Controls.Add(status);
                    form.Show();
                    for (int i = 0; i < 80; i++)
                        Items(status).Add(new StatusControl.StatusItem { Name = "field" + i,
                            DisplayName = "很长双语名称 / " + new string('W', 80), Value = "-12345.6789" });
                    Reflow(status);
                    var panel = Field<Panel>(status, "_contentPanel");
                    var columns = Field<IList>(status, "_columns");
                    Assert.IsTrue(columns.Count > 1, "Rows fill down then continue to the right.");
                    Assert.IsTrue(panel.HorizontalScroll.Visible, "Access overflow with the native horizontal scrollbar.");
                    Assert.IsFalse(panel.VerticalScroll.Visible, "Ordinary rows must fit above the scrollbar.");
                    var last = columns[columns.Count - 1];
                    panel.AutoScrollPosition = new Point(panel.HorizontalScroll.Maximum, 0);
                    Assert.IsTrue(panel.AutoScrollPosition.X < 0);
                    int lastRow = Column(last, "Count") - 1;
                    var point = new Point(Column(last, "Left") + panel.AutoScrollPosition.X + 1,
                        Field<int>(status, "_padding") + lastRow * Field<int>(status, "_rowHeight") + 1);
                    Assert.AreSame(Items(status).Last(), Call(status, "ItemAt", point));
                    AssertLayout(status);
                }
            });
        }

        [DataTestMethod]
        [DataRow(16, false)]
        [DataRow(80, true)]
        public void WideViewportOnlyScrollsWhenTheGroupsOverflow(int itemCount, bool useHorizontalScroll)
        {
            Sta(() =>
            {
                using (var form = new Form { ClientSize = new Size(640, 245) })
                using (var status = new StatusControl { Dock = DockStyle.Fill })
                {
                    form.Controls.Add(status);
                    form.Show();
                    for (int i = 0; i < itemCount; i++)
                        Items(status).Add(new StatusControl.StatusItem { Name = "field" + i,
                            DisplayName = "Field " + i, Value = "12.5" });
                    Reflow(status);
                    var panel = Field<Panel>(status, "_contentPanel");
                    Assert.IsTrue(Field<IList>(status, "_columns").Count >= 2);
                    Assert.AreEqual(useHorizontalScroll, panel.HorizontalScroll.Visible);
                    Assert.IsFalse(panel.VerticalScroll.Visible);
                    AssertLayout(status);
                }
            });
        }

        [TestMethod]
        public void OrdinaryBilingualCatalogFitsThreeGroupsWithAdjacentValues()
        {
            Sta(() =>
            {
                using (var form = new Form { ClientSize = new Size(900, 140), AutoScaleMode = AutoScaleMode.None })
                using (var status = new StatusControl { Dock = DockStyle.Fill })
                {
                    form.Controls.Add(status);
                    form.Show();
                    for (int i = 0; i < 80; i++)
                        Items(status).Add(new StatusControl.StatusItem { Name = "field" + i,
                            DisplayName = i % 4 == 0 ? "Short " + i : "中英文标签 " + new string('W', 23),
                            Value = i % 4 == 0 ? "-1234567890.123" : "-123.125" });
                    Reflow(status);
                    var panel = Field<Panel>(status, "_contentPanel");
                    var columns = Field<IList>(status, "_columns");
                    Assert.IsTrue(columns.Cast<object>().Count(column => Column(column, "Left") + Column(column, "Width") <= panel.ClientSize.Width) >= 3, "At least three complete groups should fit the representative wide status pane.");
                    Assert.IsTrue(panel.HorizontalScroll.Visible);
                    Assert.IsFalse(panel.VerticalScroll.Visible);
                    int gap = Field<int>(status, "_nameValueGap");
                    int reserve = Field<int>(status, "_steadyValueWidth");
                    bool hasTightlyPlacedShortLabel = false;
                    foreach (var column in columns)
                    {
                        int[] labelWidths = (int[])column.GetType().GetField("LabelWidths").GetValue(column);
                        hasTightlyPlacedShortLabel |= labelWidths.Any(width => width < Column(column, "NameWidth") - 25);
                        int[] valueWidths = (int[])column.GetType().GetField("ValueWidths").GetValue(column);
                        Assert.AreEqual(labelWidths.Zip(valueWidths, (label, value) => label + gap + Math.Max(reserve, value)).Max(), Column(column, "Width"),
                            "Group width is the widest rendered pair, with a small reserve for steady updates.");
                    }
                    Assert.IsTrue(hasTightlyPlacedShortLabel, "Short labels do not reserve the longest label width.");
                    AssertLayout(status);
                }
            });
        }

        private static void AssertLayout(StatusControl status)
        {
            var panel = Field<Panel>(status, "_contentPanel");
            var items = Field<List<StatusControl.StatusItem>>(status, "_filteredItems");
            var columns = Field<IList>(status, "_columns");
            int padding = Field<int>(status, "_padding");
            int rowHeight = Field<int>(status, "_rowHeight");
            int count = 0, previousRight = 0, lastRowBottom = 0;
            using (var graphics = panel.CreateGraphics())
            using (var bold = new Font(status.Font, FontStyle.Bold))
            using (var normal = new Font(status.Font, FontStyle.Regular))
            {
                Assert.IsTrue(rowHeight >= Math.Ceiling(bold.GetHeight(graphics)), "Rows must contain the rendered font.");
                foreach (var column in columns)
                {
                    int left = Column(column, "Left"), groupWidth = Column(column, "Width");
                    Assert.IsTrue(left >= previousRight, "Adjacent groups must not overlap.");
                    Assert.AreEqual(count, Column(column, "Start"), "Retain the original item order.");
                    for (int i = 0; i < Column(column, "Count"); i++, count++)
                    {
                        Assert.IsTrue(Column(column, "ValueWidth") >= Math.Ceiling(graphics.MeasureString(items[count].Value, normal).Width),
                            "Every value must have its full measured width.");
                        int visibleHeight = DrawableViewportHeight(panel);
                        Assert.IsTrue(padding + (i + 1) * rowHeight <= visibleHeight || panel.VerticalScroll.Visible,
                            "Rows must stay above the horizontal bar, or be reachable by vertical scrolling in very short panels. " +
                            $"rowBottom={padding + (i + 1) * rowHeight}, visibleHeight={visibleHeight}, client={panel.ClientSize}, " +
                            $"outer={panel.Size}, hbar={panel.HorizontalScroll.Visible}, vbar={panel.VerticalScroll.Visible}, " +
                            $"scrollMin={panel.AutoScrollMinSize}, font={status.Font.SizeInPoints}, dpi={graphics.DpiY}");
                    }
                    int contentBottom = padding * 2 + Column(column, "Count") * rowHeight;
                    lastRowBottom = Math.Max(lastRowBottom, contentBottom);
                    int contentHeight = DrawableViewportHeight(panel);
                    Assert.IsTrue(contentBottom <= contentHeight || panel.VerticalScroll.Visible,
                        $"The last row's bottom padding must also fit above the scrollbar. bottom={contentBottom}, " +
                        $"available={contentHeight}, client={panel.ClientSize}, outer={panel.Size}");
                    previousRight = left + groupWidth;
                }
            }
            Assert.AreEqual(items.Count, count, "Every filtered item must be laid out exactly once.");
            Assert.IsTrue(panel.AutoScrollMinSize.Width >= previousRight + padding, "The last value must be horizontally reachable.");
            if (panel.VerticalScroll.Visible)
            {
                var position = panel.AutoScrollPosition;
                try
                {
                    panel.AutoScrollPosition = new Point(-position.X, panel.AutoScrollMinSize.Height);
                    int available = DrawableViewportHeight(panel);
                    Assert.IsTrue(lastRowBottom + panel.AutoScrollPosition.Y <= available,
                        $"Scrolling to the bottom must expose the full last-row padding. bottom={lastRowBottom}, " +
                        $"offset={panel.AutoScrollPosition.Y}, available={available}, scrollMin={panel.AutoScrollMinSize}");
                }
                finally { panel.AutoScrollPosition = new Point(-position.X, -position.Y); }
            }
        }

        [DataTestMethod]
        [DataRow(65, 58, 75)]
        [DataRow(70, 58, 75)]
        [DataRow(65, 58, 58)]
        [DataRow(58, 58, 75)]
        public void ShortPanelScrollExtentExposesTheReservedFooter(int contentHeight, int availableHeight, int scrollPageHeight)
        {
            // The first two cases use the reported Windows client/page and row
            // sizes. The other cases cover an in-client bar and an exact fit.
            int extent = (int)typeof(StatusControl).GetMethod("MinimumScrollHeight", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { contentHeight, availableHeight, scrollPageHeight });
            if (contentHeight <= availableHeight)
                Assert.AreEqual(0, extent, "Content that fits needs no vertical scroll range.");
            else
                Assert.IsTrue(extent > scrollPageHeight, "Overflow must enable the native vertical scrollbar.");
            int maximumOffset = Math.Max(0, extent - scrollPageHeight);
            Assert.IsTrue(contentHeight - maximumOffset <= availableHeight,
                $"The full content must be reachable above the reserved footer: content={contentHeight}, " +
                $"extent={extent}, page={scrollPageHeight}, available={availableHeight}");
        }

        [DataTestMethod]
        [DataRow(BorderStyle.FixedSingle)]
        [DataRow(BorderStyle.Fixed3D)]
        public void SmallerClientAreaKeepsEveryRowAboveTheScrollbar(BorderStyle borderStyle)
        {
            Sta(() =>
            {
                using (var form = new Form { ClientSize = new Size(640, 220), AutoScaleMode = AutoScaleMode.None })
                using (var status = new StatusControl { Dock = DockStyle.Fill })
                {
                    form.Controls.Add(status);
                    form.Show();
                    var panel = Field<Panel>(status, "_contentPanel");
                    // Exercise an actual outer/client-size difference on both Mono
                    // and Windows, including non-client changes without resizing.
                    panel.BorderStyle = borderStyle;
                    for (int i = 0; i < 70; i++)
                        Items(status).Add(new StatusControl.StatusItem { Name = "field" + i,
                            DisplayName = "field" + i + " (空速)", Value = "-123456.789" });
                    for (int height = 180; height <= 220; height++)
                    {
                        form.ClientSize = new Size(640, height);
                        Reflow(status);
                        Assert.IsTrue(panel.ClientSize.Height < panel.Height);
                        AssertLayout(status);
                    }
                }
            });
        }

        [TestMethod]
        public void LongLabelsAndValuesHaveFullTooltipsAfterHorizontalScrolling()
        {
            Sta(() =>
            {
                using (var form = new Form { ClientSize = new Size(320, 240) })
                using (var status = new StatusControl { Dock = DockStyle.Fill })
                {
                    form.Controls.Add(status);
                    form.Show();
                    var item = new StatusControl.StatusItem { Name = "custom", DisplayName = "名称 / " + new string('W', 150),
                        Value = "-1234567890.1234567890123456789" };
                    Items(status).Add(item);
                    Reflow(status);
                    var panel = Field<Panel>(status, "_contentPanel");
                    var column = Field<IList>(status, "_columns")[0];
                    using (var graphics = panel.CreateGraphics())
                    using (var bold = new Font(status.Font, FontStyle.Bold))
                    {
                        Assert.IsTrue(Column(column, "NameWidth") < graphics.MeasureString(item.DisplayName, bold).Width,
                            "Long labels must be bounded, with an ellipsis and tooltip.");
                        var label = ((string[])column.GetType().GetField("Labels").GetValue(column))[0];
                        Assert.IsTrue(label.EndsWith("…", StringComparison.Ordinal), "Truncation must have a visible marker.");
                        Assert.IsTrue(graphics.MeasureString(label, bold).Width <= Column(column, "NameWidth"));
                    }
                    panel.AutoScrollPosition = new Point(97, 0);
                    int x = Column(column, "Left") + Column(column, "NameWidth") + Field<int>(status, "_nameValueGap") + panel.AutoScrollPosition.X + 1;
                    int y = Field<int>(status, "_padding") + 1;
                    Assert.AreSame(item, Call(status, "ItemAt", new Point(x, y)), "Hit testing must include the scroll offset.");
                    Call(status, "ContentPanel_MouseMove", panel, new MouseEventArgs(MouseButtons.None, 0, x, y, 0));
                    Assert.AreEqual(item.DisplayName + Environment.NewLine + item.Value, Field<ToolTip>(status, "_toolTip").GetToolTip(panel));
                    Assert.IsNull(Call(status, "ItemAt", new Point(0, 0)));
                }
            });
        }

        [TestMethod]
        public void LiveValueGrowthReflowsWithoutChangingSourceOrScroll()
        {
            Sta(() =>
            {
                var state = MainV2.comPort.MAV.cs;
                var previousMode = state.mode;
                try
                {
                    using (var form = new Form { ClientSize = new Size(320, 240) })
                    using (var status = new StatusControl { Dock = DockStyle.Fill })
                    {
                        form.Controls.Add(status);
                        form.Show();
                        Items(status).Add(new StatusControl.StatusItem { Name = "mode", DisplayName = "mode (模式)", Value = "0" });
                        Reflow(status);
                        var panel = Field<Panel>(status, "_contentPanel");
                        int before = panel.AutoScrollMinSize.Width;
                        state.mode = "Long live value " + new string('9', 100);
                        Call(status, "UpdateTimer_Tick", status, EventArgs.Empty);
                        Assert.IsTrue(panel.AutoScrollMinSize.Width > before);
                        Assert.AreEqual(state.mode, Items(status)[0].Value);
                        panel.AutoScrollPosition = new Point(97, 0);
                        int offset = panel.AutoScrollPosition.X;
                        state.mode += "99999";
                        Call(status, "UpdateTimer_Tick", status, EventArgs.Empty);
                        Assert.AreEqual(offset, panel.AutoScrollPosition.X);
                        Assert.AreEqual(state.mode, Items(status)[0].Value);
                        AssertLayout(status);
                    }
                }
                finally { state.mode = previousMode; }
            });
        }

        [DataTestMethod]
        [DataRow("zh-CN", "空速")]
        [DataRow("en-US", "AIRspeed")]
        [DataRow("zh-Hans", "airspeed")]
        public void RebuildingRetainsQueryOrderAndNumericValues(string cultureName, string query)
        {
            Sta(() =>
            {
                var previous = CultureInfo.CurrentUICulture;
                try
                {
                    CultureInfo.CurrentUICulture = new CultureInfo(cultureName);
                    using (var form = new Form { ClientSize = new Size(320, 240) })
                    using (var status = new StatusControl { Dock = DockStyle.Fill })
                    {
                        form.Controls.Add(status);
                        form.Show();
                        var search = Field<PlaceholderTextBox>(status, "_searchBox");
                        search.Text = query;
                        status.Start();
                        status.Stop();
                        Call(status, "UpdateTimer_Tick", status, EventArgs.Empty);
                        var actual = Field<List<StatusControl.StatusItem>>(status, "_filteredItems");
                        var expected = MainV2.comPort.MAV.cs.GetItemList(true)
                            .Where(key => TelemetryFieldPresentation.Matches(query, key, TelemetryFieldPresentation.FieldLabel(key))).ToArray();
                        Assert.IsTrue(expected.Length > 0);
                        CollectionAssert.AreEqual(expected, actual.Select(x => x.Name).ToArray());
                        foreach (var item in actual)
                            Assert.AreEqual(typeof(CurrentState).GetProperty(item.Name).GetValue(MainV2.comPort.MAV.cs)?.ToString() ?? "", item.Value);
                        status.Start();
                        status.Stop();
                        CollectionAssert.AreEqual(expected,
                            Field<List<StatusControl.StatusItem>>(status, "_filteredItems").Select(x => x.Name).ToArray());
                        Assert.AreEqual(query, search.Text);
                        Assert.AreEqual(0, Field<Panel>(status, "_contentPanel").AutoScrollPosition.X);
                        search.Text = "no matching telemetry field 012345";
                        Assert.AreEqual(0, Field<IList>(status, "_columns").Count);
                        Assert.AreEqual(Size.Empty, Field<Panel>(status, "_contentPanel").AutoScrollMinSize);
                    }
                }
                finally { CultureInfo.CurrentUICulture = previous; }
            });
        }

        [TestMethod]
        public void LiveValuesCanAddAndRemoveTheScrollbarWithoutHidingRows()
        {
            Sta(() =>
            {
                var state = MainV2.comPort.MAV.cs;
                var previous = state.mode;
                try
                {
                    using (var form = new Form { ClientSize = new Size(320, 130), AutoScaleMode = AutoScaleMode.None })
                    using (var status = new StatusControl { Dock = DockStyle.Fill })
                    {
                        form.Controls.Add(status);
                        form.Show();
                        Items(status).Add(new StatusControl.StatusItem { Name = "mode", DisplayName = "mode", Value = "0" });
                        for (int i = 1; i < 8; i++)
                            Items(status).Add(new StatusControl.StatusItem { Name = "f" + i, DisplayName = "f" + i, Value = "0" });
                        Reflow(status);
                        var panel = Field<Panel>(status, "_contentPanel");
                        Assert.IsFalse(panel.HorizontalScroll.Visible);
                        state.mode = new string('9', 100);
                        Call(status, "UpdateTimer_Tick", status, EventArgs.Empty);
                        Assert.IsTrue(panel.HorizontalScroll.Visible);
                        AssertLayout(status);
                        state.mode = "0";
                        Call(status, "UpdateTimer_Tick", status, EventArgs.Empty);
                        Assert.IsFalse(panel.HorizontalScroll.Visible, "Reconsider rows without the bar after a value shrinks.");
                        AssertLayout(status);
                    }
                }
                finally { state.mode = previous; }
            });
        }
    }
}
