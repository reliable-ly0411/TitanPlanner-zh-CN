using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MissionPlanner.GCSViews;

namespace MissionPlannerTests.Controls
{
    [TestClass]
    public class MapFooterLayoutTests
    {
        [TestMethod]
        public void OptionsAreCenteredAndUnclippedAcrossWidthsFontsAndScaling()
        {
            OnSta(() =>
            {
                foreach (bool chinese in new[] { true, false })
                foreach (string family in new[] { "Microsoft YaHei", "Segoe UI", "Noto Sans CJK SC" })
                foreach (float scale in new[] { 1f, 1.25f, 1.5f, 2f })
                using (var fixture = new FooterFixture(chinese, family))
                {
                    fixture.Form.Scale(new SizeF(scale, scale));
                    foreach (int width in new[] { 1100, 800, 600, 420, 280 })
                    {
                        fixture.Form.ClientSize = new Size((int)(width * scale), (int)(360 * scale));
                        fixture.VerifyLayout();
                    }

                    // Font changes after construction must also invalidate the preferred height.
                    using (var larger = new Font(family, 12))
                    {
                        fixture.Form.Font = larger;
                        fixture.VerifyLayout();
                    }
                    Assert.AreEqual(0, fixture.CheckedChanges, "Layout changed an option's state.");
                    Assert.IsTrue(fixture.Options.Last().Checked, "Auto-pan state was lost.");
                }
            });
        }

        [TestMethod]
        public void WrappedFooterReturnsSpaceToTheMapWhenWidened()
        {
            OnSta(() =>
            {
                using (var fixture = new FooterFixture(true, "Microsoft YaHei"))
                {
                    fixture.Form.ClientSize = new Size(1100, 360);
                    fixture.VerifyLayout();
                    int wideHeight = fixture.Footer.Height;
                    int mapHeight = fixture.Map.Height;
                    for (int i = 0; i < 5; i++)
                    {
                        fixture.Form.ClientSize = new Size(280, 360);
                        fixture.VerifyLayout();
                        Assert.IsTrue(fixture.Footer.Height > wideHeight, "Narrow footer did not wrap.");
                        Assert.IsTrue(fixture.Map.Height < mapHeight, "Wrapped footer overlaps the map.");

                        fixture.Form.ClientSize = new Size(1100, 360);
                        fixture.VerifyLayout();
                        Assert.AreEqual(wideHeight, fixture.Footer.Height, "Footer kept its wrapped height.");
                        Assert.AreEqual(mapHeight, fixture.Map.Height, "Map space was not restored.");
                    }
                }
            });
        }

        [TestMethod]
        public void RealCoordinatesFitThe186PixelPaneOfA760PixelHost()
        {
            OnSta(() =>
            {
                foreach (string family in new[] { "Segoe UI", "Microsoft YaHei", "Noto Sans CJK SC" })
                using (var fixture = new FooterFixture(true, family, true))
                {
                    int naturalCoordinateWidth = fixture.Coordinates.Width;
                    var split = new SplitContainer
                    {
                        Dock = DockStyle.Fill, SplitterWidth = 4, FixedPanel = FixedPanel.Panel1,
                        Panel1MinSize = 0, Panel2MinSize = 0
                    };
                    fixture.Form.Controls.Remove(fixture.Table);
                    split.Panel2.Controls.Add(fixture.Table);
                    fixture.Form.Controls.Add(split);
                    foreach (int width in new[] { 760, 1100, 760, 1360, 760 })
                    {
                        fixture.Form.ClientSize = new Size(width, 360);
                        split.SplitterDistance = 568;
                        fixture.VerifyLayout();
                        if (width == 760)
                        {
                            Assert.AreEqual(186, fixture.Footer.ClientSize.Width, "The production narrow-pane constraint was not reproduced.");
                            Assert.IsTrue(fixture.Options[3].Bottom + fixture.Options[3].Margin.Bottom <= fixture.Footer.ClientSize.Height,
                                "Auto-pan's last row is clipped in the 760-pixel host.");
                        }
                        else
                            Assert.AreEqual(naturalCoordinateWidth, fixture.Coordinates.Width, "Coordinates did not regain their natural width.");
                    }
                    // Scale while constrained, then widen: the natural coordinate width must scale too.
                    fixture.Form.Scale(new SizeF(1.5f, 1.5f));
                    fixture.Form.ClientSize = new Size(1140, 540);
                    split.SplitterDistance = 852;
                    fixture.VerifyLayout();
                    fixture.Form.ClientSize = new Size(2040, 540);
                    split.SplitterDistance = 852;
                    fixture.VerifyLayout();
                    Assert.IsTrue(Math.Abs(naturalCoordinateWidth * 1.5 - fixture.Coordinates.Width) <= 1,
                        "Scaling a constrained coordinate control lost its natural width.");
                    Assert.IsTrue(fixture.Coordinates is MissionPlanner.Controls.Coords, "Use the real coordinate control in this regression.");
                    Assert.AreEqual(0, fixture.CheckedChanges);
                    Assert.IsTrue(fixture.Options[3].Checked);
                }
            });
        }

        [TestMethod]
        public void ConfigurationKeepsControlsAndTheirCheckedChangedHandlers()
        {
            OnSta(() =>
            {
                using (var fixture = new FooterFixture(true, "Segoe UI"))
                {
                    fixture.Options[0].Checked = true;
                    fixture.Configure();
                    fixture.VerifyLayout();
                    Assert.AreEqual(1, fixture.CheckedChanges);
                    Assert.IsTrue(fixture.Options[0].Checked);
                    Assert.IsTrue(fixture.Options[3].Checked);
                    for (int i = 0; i < fixture.Options.Length; i++)
                        Assert.AreSame(fixture.Options[i], fixture.Footer.Controls[i + 1]);
                    fixture.Options[0].Checked = false;
                    Assert.AreEqual(2, fixture.CheckedChanges, "Original event handler was detached.");
                }
            });
        }

        private static void OnSta(Action action)
        {
            Exception failure = null;
            var thread = new Thread(() =>
            {
                try { action(); }
                catch (Exception ex) { failure = ex; }
            }) { IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(45)), "Map footer layout timed out.");
            if (failure != null)
                Assert.Fail(failure.ToString());
        }

        private sealed class FooterFixture : IDisposable
        {
            internal readonly Form Form = new Form { AutoScaleMode = AutoScaleMode.None, ShowInTaskbar = false };
            internal readonly TableLayoutPanel Table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
            };
            internal readonly Panel Map = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
            internal readonly FlowLayoutPanel Footer = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = Padding.Empty };
            internal readonly Control Coordinates;
            private Control coordinates { get { return Coordinates; } }
            private readonly Font font;
            internal readonly CheckBox[] Options;
            internal int CheckedChanges;

            internal FooterFixture(bool chinese, string family, bool realCoordinates = false)
            {
                Coordinates = realCoordinates ? (Control)new MissionPlanner.Controls.Coords() : new Panel();
                Coordinates.Size = new Size(255, 21);
                font = new Font(family, 9);
                Form.Font = font;
                Form.ClientSize = new Size(1100, 360);
                Table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                Table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                Table.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
                Table.Controls.Add(Map, 0, 0);
                Table.Controls.Add(Footer, 0, 1);
                Form.Controls.Add(Table);

                // Reproduce the mixed localized coordinates and runtime 3D option from FlightData.
                string[] labels = chinese ? new[] { "调试", "参数", "3D 地图", "自动平移" }
                    : new[] { "Tuning", "Params", "3D Map", "Auto Pan" };
                Options = labels.Select(text => new CheckBox
                {
                    Text = text, AutoSize = true, Anchor = AnchorStyles.Bottom | AnchorStyles.Left
                }).ToArray();
                Options[0].Location = new Point(260, chinese ? 11 : 5);
                Options[1].Location = new Point(335, 5);
                Options[2].Anchor = AnchorStyles.Top | AnchorStyles.Left;
                Options[2].Location = new Point(Options[1].Right + 10, 5);
                Options[3].Location = new Point(Options[2].Right + 10, chinese ? 12 : 5);
                Options[3].Checked = true;
                foreach (var option in Options)
                    option.CheckedChanged += (s, e) => CheckedChanges++;

                Footer.SuspendLayout();
                Footer.Controls.AddRange(new Control[] { coordinates, Options[3], Options[0], Options[1], Options[2] });
                Configure();
                Footer.ResumeLayout(true);
                Form.Show();
            }

            internal void Configure()
            {
                typeof(FlightData).Assembly.GetType("MissionPlanner.Controls.MapFooterLayout", true)
                    .GetMethod("Configure", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { Table, Footer, coordinates, Options });
            }

            internal void VerifyLayout()
            {
                Form.PerformLayout();
                Table.PerformLayout();
                Footer.PerformLayout();
                Application.DoEvents();
                Assert.IsFalse(Map.Bounds.IntersectsWith(Footer.Bounds), "Footer covers the map.");
                var controls = new Control[] { coordinates }.Concat(Options).ToArray();
                foreach (var control in controls)
                {
                    Assert.IsTrue(Footer.ClientRectangle.Contains(control.Bounds),
                        control.Text + " is clipped: " + control.Bounds + " in " + Footer.ClientRectangle);
                    Assert.IsTrue(control.Height >= control.GetPreferredSize(Size.Empty).Height || control == coordinates,
                        control.Text + " is shorter than its preferred height.");
                }
                for (int i = 0; i < controls.Length; i++)
                for (int j = i + 1; j < controls.Length; j++)
                {
                    Assert.IsFalse(controls[i].Bounds.IntersectsWith(controls[j].Bounds), "Footer controls overlap.");
                    if (controls[i].Top < controls[j].Bottom && controls[j].Top < controls[i].Bottom)
                        Assert.IsTrue(Math.Abs((controls[i].Top + controls[i].Bottom) -
                                              (controls[j].Top + controls[j].Bottom)) <= 2,
                            "Controls in the same row have different vertical centers.");
                }
            }

            public void Dispose()
            {
                Form.Dispose();
                font.Dispose();
            }
        }
    }
}
