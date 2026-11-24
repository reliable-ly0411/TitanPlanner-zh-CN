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
    public class DisplayThisFormLayoutTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static T Field<T>(DisplayThisForm form, string name) =>
            (T)typeof(DisplayThisForm).GetField(name, Private).GetValue(form);
        private static object Call(DisplayThisForm form, string name, params object[] args) =>
            typeof(DisplayThisForm).GetMethod(name, Private).Invoke(form, args);
        private static T Row<T>(object row, string name) => (T)row.GetType().GetField(name).GetValue(row);
        private static T Item<T>(object item, string name) => (T)item.GetType().GetProperty(name).GetValue(item);
        private static void Set(object item, string name, object value) => item.GetType().GetProperty(name).SetValue(item, value);
        private static IList Items(DisplayThisForm form) => Field<IList>(form, "_allItems");
        private static object Add(DisplayThisForm form, string name, string label, string value, string group = "Air")
        {
            var type = Items(form).GetType().GetGenericArguments()[0];
            var item = Activator.CreateInstance(type);
            Set(item, "Name", name); Set(item, "DisplayName", label); Set(item, "Value", value);
            Set(item, "Group", group); Set(item, "GroupLabel", group + " (分组)");
            Items(form).Add(item);
            return item;
        }
        private static void Reflow(DisplayThisForm form)
        {
            Call(form, "SearchBox_TextChanged", form, EventArgs.Empty);
            Application.DoEvents();
        }
        private static void Sta(Action action)
        {
            Exception failure = null;
            var thread = new Thread(() => { try { action(); } catch (Exception ex) { failure = ex; } });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(30)), "Popup layout test timed out.");
            if (failure != null) Assert.Fail(failure.ToString());
        }
        private sealed class Probe : DisplayThisForm
        {
            public Probe(EventHandler changed = null, MouseEventHandler mouseDown = null)
                : base(_ => false, changed, mouseDown)
            {
                MinimumSize = Size.Empty;
                AutoScaleMode = AutoScaleMode.None;
            }
            protected override void OnLoad(EventArgs e) { }
        }
        private static Size Viewport(Panel panel)
        {
            var size = panel.ClientSize;
            if (Type.GetType("Mono.Runtime") != null)
            {
                if (panel.HorizontalScroll.Visible) size.Height -= SystemInformation.HorizontalScrollBarHeight;
                if (panel.VerticalScroll.Visible) size.Width -= SystemInformation.VerticalScrollBarWidth;
            }
            return size;
        }
        private static IList Rows(DisplayThisForm form) => Field<IList>(form, "_layoutRows");
        private static object[] BodyRows(DisplayThisForm form) => Rows(form).Cast<object>().Where(row => !Row<bool>(row, "IsHeader")).ToArray();
        private static Point VisiblePoint(DisplayThisForm form, object row)
        {
            var panel = Field<Panel>(form, "_contentPanel");
            var bounds = Row<Rectangle>(row, "Bounds");
            panel.AutoScrollPosition = new Point(bounds.Left, Math.Max(0, bounds.Top - 5));
            return new Point(bounds.Left + panel.AutoScrollPosition.X + 2, bounds.Top + panel.AutoScrollPosition.Y + 2);
        }

        [DataTestMethod]
        [DataRow(320, 120, 1f)] [DataRow(320, 120, 1.25f)] [DataRow(320, 120, 1.5f)] [DataRow(320, 120, 2f)]
        [DataRow(640, 320, 1f)] [DataRow(640, 320, 1.25f)] [DataRow(640, 320, 1.5f)] [DataRow(640, 320, 2f)]
        [DataRow(960, 500, 1f)] [DataRow(960, 500, 1.25f)] [DataRow(960, 500, 1.5f)] [DataRow(960, 500, 2f)]
        [DataRow(1280, 800, 1f)] [DataRow(1280, 800, 1.25f)] [DataRow(1280, 800, 1.5f)] [DataRow(1280, 800, 2f)]
        public void ResponsiveColumnsKeepEveryFieldAndValueReachable(int width, int height, float scale)
        {
            Sta(() =>
            {
                using (var font = new Font(SystemFonts.DefaultFont.FontFamily, SystemFonts.DefaultFont.SizeInPoints * scale))
                using (var form = new Probe { Font = font, ClientSize = new Size(width, height) })
                {
                    form.Show();
                    for (int i = 0; i < 84; i++)
                    {
                        var item = Add(form, "field" + i, i % 3 == 0 ? "field" + i : "field" + i + " (中英文名称 / " + new string('W', 80) + ")",
                            i % 5 == 0 ? "-1234.56789" : "12.5", "G" + i / 12);
                        Set(item, "IsChecked", i % 7 == 0);
                    }
                    Reflow(form);
                    AssertLayout(form);
                    var panel = Field<Panel>(form, "_contentPanel");
                    using (var bitmap = new Bitmap(panel.Width, panel.Height)) panel.DrawToBitmap(bitmap, panel.ClientRectangle);
                    using (var larger = new Font(font.FontFamily, font.SizeInPoints * 1.2f))
                    {
                        int rowHeight = Field<int>(form, "_rowHeight");
                        form.Font = larger;
                        Application.DoEvents();
                        Assert.IsTrue(Field<int>(form, "_rowHeight") > rowHeight);
                        AssertLayout(form);
                    }
                    for (int i = 0; i < Items(form).Count; i++) Assert.AreEqual(i % 7 == 0, Item<bool>(Items(form)[i], "IsChecked"));
                }
            });
        }

        private static void AssertLayout(DisplayThisForm form)
        {
            var panel = Field<Panel>(form, "_contentPanel");
            var viewport = Viewport(panel);
            var body = BodyRows(form);
            var filtered = Field<IList>(form, "_filteredItems");
            CollectionAssert.AreEqual(filtered.Cast<object>().Select(item => Item<string>(item, "Name")).ToArray(),
                body.Select(row => Item<string>(Row<object>(row, "Item"), "Name")).ToArray(), "Keep the filtered order, without missing or duplicate fields.");
            int padding = Field<int>(form, "_padding");
            int right = 0, bottom = 0;
            using (var graphics = panel.CreateGraphics())
            {
                foreach (var column in Rows(form).Cast<object>().GroupBy(row => Row<Rectangle>(row, "Bounds").Left))
                {
                    var entries = column.ToArray();
                    Assert.IsTrue(Row<bool>(entries[0], "IsHeader"), "Each continuation column needs group context.");
                    for (int i = 0; i < entries.Length; i++)
                    {
                        var row = entries[i];
                        var bounds = Row<Rectangle>(row, "Bounds");
                        var name = Row<Rectangle>(row, "NameBounds");
                        right = Math.Max(right, bounds.Right); bottom = Math.Max(bottom, bounds.Bottom);
                        var rowFont = Field<Font>(form, Row<bool>(row, "IsHeader") ? "_headerFont" : "_normalFont");
                        Assert.IsTrue(graphics.MeasureString(Row<string>(row, "Label"), rowFont).Width <= name.Width,
                            "A displayed label and its explicit ellipsis must fit in its own cell.");
                        if (Row<bool>(row, "IsHeader"))
                            Assert.IsTrue(i + 1 < entries.Length && !Row<bool>(entries[i + 1], "IsHeader"), "Never orphan a group header.");
                        else
                        {
                            var value = Row<Rectangle>(row, "ValueBounds");
                            var check = Row<Rectangle>(row, "CheckBounds");
                            Assert.IsTrue(check.Right <= name.Left && name.Right < value.Left);
                            Assert.IsTrue(graphics.MeasureString(Item<string>(Row<object>(row, "Item"), "Value"), Field<Font>(form, "_normalFont")).Width <= value.Width,
                                "Retain complete value width.");
                            Assert.AreEqual(bounds.Right, value.Right);
                        }
                    }
                }
            }
            int columns = Rows(form).Cast<object>().Select(row => Row<Rectangle>(row, "Bounds").Left).Distinct().Count();
            if (columns > 1) Assert.IsTrue(right + padding <= viewport.Width, "All responsive columns must fit; no partly cut third column.");
            Assert.IsTrue(panel.AutoScrollMinSize.Width >= right + padding);
            Assert.IsTrue(panel.AutoScrollMinSize.Height >= bottom + padding);
            var previous = panel.AutoScrollPosition;
            try
            {
                panel.AutoScrollPosition = new Point(panel.AutoScrollMinSize.Width, panel.AutoScrollMinSize.Height);
                Assert.IsTrue(right + padding + panel.AutoScrollPosition.X <= viewport.Width, "The last value must be horizontally reachable.");
                Assert.IsTrue(bottom + padding + panel.AutoScrollPosition.Y <= viewport.Height, "The last row must be vertically reachable.");
            }
            finally { panel.AutoScrollPosition = new Point(-previous.X, -previous.Y); }
        }

        [TestMethod]
        public void EnhancedChineseNarrowRowUsesSeparateKeyAndDescriptionWithoutChangingTheField()
        {
            Sta(() =>
            {
                var language = CultureInfo.CurrentUICulture;
                try
                {
                    CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("zh-CN");
                    using (var form = new Probe { ClientSize = new Size(260, 110) })
                    {
                        form.Show();
                        const string key = "generator_current";
                        const string description = "发电机蓄电池电流和控制状态";
                        const string label = key + " (" + description + ")";
                        const string value = "-12.500";
                        var item = Add(form, key, label, value);
                        Set(item, "IsChecked", true);
                        Add(form, "customfield0", "customfield0 (操作员自行设置名称)", "0");
                        Add(form, "not_translated", "not_translated", "0");
                        Reflow(form);
                        var rows = BodyRows(form);
                        var row = rows[0];
                        Assert.AreEqual(key, Row<string>(row, "Label"));
                        Assert.AreEqual(description, Row<string>(row, "Description"));
                        var descriptionBounds = Row<Rectangle>(row, "DescriptionBounds");
                        var valueBounds = Row<Rectangle>(row, "ValueBounds");
                        Assert.AreEqual(valueBounds.Right, descriptionBounds.Right);
                        Assert.AreEqual(valueBounds.Bottom, descriptionBounds.Top);
                        Assert.IsFalse(valueBounds.IntersectsWith(descriptionBounds));
                        Assert.AreEqual(2 * Field<int>(form, "_rowHeight"), Row<Rectangle>(row, "Bounds").Height);
                        Assert.IsNull(Row<string>(rows[1], "Description"), "Do not reinterpret an operator label.");
                        Assert.IsNull(Row<string>(rows[2], "Description"), "Unknown fields remain unchanged.");
                        AssertLayout(form);
                        var bounds = Row<Rectangle>(row, "Bounds");
                        var secondLinePoint = new Point(bounds.Left + 2, bounds.Bottom - 2);
                        Assert.AreSame(item, Call(form, "GetItemAtPoint", secondLinePoint));
                        Field<PlaceholderTextBox>(form, "_searchBox").Text = description;
                        Assert.AreEqual(1, BodyRows(form).Length);
                        Assert.AreSame(item, Row<object>(BodyRows(form).Single(), "Item"));
                        Assert.AreEqual(value, Item<string>(item, "Value"));
                        Assert.IsTrue(Item<bool>(item, "IsChecked"));

                        form.ClientSize = new Size(960, 480);
                        Application.DoEvents();
                        Reflow(form);
                        Assert.AreEqual(label, Row<string>(BodyRows(form).Single(), "Label"));
                        Assert.IsNull(Row<string>(BodyRows(form).Single(), "Description"));
                        form.ClientSize = new Size(260, 110);
                        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("zh-Hans");
                        Reflow(form);
                        Assert.IsNull(Row<string>(BodyRows(form).Single(), "Description"), "Legacy Chinese retains single-line layout.");
                    }
                }
                finally { CultureInfo.CurrentUICulture = language; }
            });
        }

        [TestMethod]
        public void SearchUsesOnlyVisibleLabelsResetsScrollingAndRetainsSelections()
        {
            Sta(() =>
            {
                using (var form = new Probe { ClientSize = new Size(640, 320) })
                {
                    form.Show();
                    var selected = Add(form, "airspeed", "airspeed (空速)", "12.5");
                    Set(selected, "IsChecked", true);
                    for (int i = 0; i < 100; i++) Add(form, "other" + i, new string('W', 100), "15", "Other");
                    Reflow(form);
                    var panel = Field<Panel>(form, "_contentPanel");
                    panel.AutoScrollPosition = new Point(0, 100);
                    Field<PlaceholderTextBox>(form, "_searchBox").Text = "空速";
                    Application.DoEvents();
                    Assert.AreEqual(1, BodyRows(form).Length);
                    Assert.AreEqual(Point.Empty, panel.AutoScrollPosition);
                    var row = BodyRows(form)[0];
                    using (var graphics = panel.CreateGraphics())
                        Assert.IsTrue(Row<Rectangle>(row, "NameBounds").Width <= Math.Ceiling(graphics.MeasureString("airspeed (空速)", Field<Font>(form, "_normalFont")).Width) + 8);
                    Assert.IsTrue(Item<bool>(selected, "IsChecked"));
                    Field<PlaceholderTextBox>(form, "_searchBox").Text = "AIRspeed";
                    Assert.AreSame(selected, Row<object>(BodyRows(form)[0], "Item"));
                    Field<PlaceholderTextBox>(form, "_searchBox").Text = "no such field 000";
                    Assert.AreEqual(0, Rows(form).Count);
                    Assert.AreEqual(Size.Empty, panel.AutoScrollMinSize);
                    Field<PlaceholderTextBox>(form, "_searchBox").Text = "";
                    Assert.IsTrue(Item<bool>(selected, "IsChecked"));
                }
            });
        }

        [TestMethod]
        public void ClickingAfterReflowAndScrollingPreservesCallbackContract()
        {
            Sta(() =>
            {
                var events = new List<string>();
                using (var form = new Probe((sender, e) =>
                {
                    var check = (CheckBox)sender;
                    events.Add("change:" + check.Name + ":" + check.Checked + ":" + check.Tag + ":" + check.Text);
                }, (sender, e) =>
                {
                    var check = (CheckBox)sender;
                    events.Add("right:" + check.Name);
                    check.Checked = !check.Checked;
                }) { ClientSize = new Size(640, 320) })
                {
                    form.Show();
                    for (int i = 0; i < 80; i++) Add(form, "field" + i,
                        i == 79 ? "字段 79（完整标签）" + new string('W', 80) : "字段 " + i, "12.5", "G" + i / 10);
                    Reflow(form);
                    var panel = Field<Panel>(form, "_contentPanel");
                    var row = BodyRows(form).Last();
                    var item = Row<object>(row, "Item");
                    var fullLabel = Item<string>(item, "DisplayName");
                    Assert.IsTrue(Row<string>(row, "Label").EndsWith("…", StringComparison.Ordinal));
                    var point = VisiblePoint(form, row);
                    Assert.AreSame(item, Call(form, "GetItemAtPoint", point), "Hit geometry must be available before painting.");
                    Call(form, "ContentPanel_MouseClick", panel, new MouseEventArgs(MouseButtons.Left, 1, point.X, point.Y, 0));
                    Assert.AreEqual("change:field79:True:custom:" + fullLabel, events.Single());
                    Call(form, "ContentPanel_MouseClick", panel, new MouseEventArgs(MouseButtons.Right, 1, point.X, point.Y, 0));
                    CollectionAssert.AreEqual(new[] { "change:field79:True:custom:" + fullLabel, "right:field79", "change:field79:True:custom:" + fullLabel }, events);
                    Assert.IsFalse(Item<bool>(item, "IsChecked"), "Retain the original model toggle and right-handler ordering.");
                    var header = Rows(form).Cast<object>().First(entry => Row<bool>(entry, "IsHeader"));
                    point = VisiblePoint(form, header);
                    Call(form, "ContentPanel_MouseClick", panel, new MouseEventArgs(MouseButtons.Left, 1, point.X, point.Y, 0));
                    Assert.AreEqual(3, events.Count, "Group headers are not selectable fields.");
                }
            });
        }

        [TestMethod]
        public void LongLabelsAndValuesHaveFullTipsAndScrollExtent()
        {
            Sta(() =>
            {
                using (var form = new Probe { ClientSize = new Size(320, 180) })
                {
                    form.Show();
                    var item = Add(form, "long_key", "长名称 " + new string('W', 100), "-" + new string('9', 100));
                    Reflow(form);
                    AssertLayout(form);
                    var row = BodyRows(form).Single();
                    Assert.IsTrue(Row<string>(row, "Label").EndsWith("…", StringComparison.Ordinal));
                    var point = VisiblePoint(form, row);
                    var panel = Field<Panel>(form, "_contentPanel");
                    Call(form, "ContentPanel_MouseMove", panel, new MouseEventArgs(MouseButtons.None, 0, point.X, point.Y, 0));
                    Assert.AreEqual(Item<string>(item, "DisplayName") + Environment.NewLine + Item<string>(item, "Value"), Field<ToolTip>(form, "_toolTip").GetToolTip(panel));
                }
            });
        }

        [TestMethod]
        public void LiveValueGrowthRetainsFieldKeySelectionAndQuery()
        {
            Sta(() =>
            {
                var state = MainV2.comPort.MAV.cs;
                var previous = state.airspeed;
                try
                {
                    using (var form = new Probe { ClientSize = new Size(320, 180) })
                    {
                        form.Show();
                        var item = Add(form, "airspeed", "airspeed (空速)", "0");
                        Set(item, "IsChecked", true);
                        Reflow(form);
                        Field<PlaceholderTextBox>(form, "_searchBox").Text = "空速";
                        state.airspeed = float.MaxValue;
                        Call(form, "UpdateTimer_Tick", form, EventArgs.Empty);
                        Assert.AreEqual(state.airspeed.ToString(), Item<string>(item, "Value"));
                        Assert.IsTrue(Item<bool>(item, "IsChecked"));
                        Assert.AreEqual("空速", Field<PlaceholderTextBox>(form, "_searchBox").Text);
                        AssertLayout(form);
                    }
                }
                finally { state.airspeed = previous; }
            });
        }
    }
}
