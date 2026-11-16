using Microsoft.VisualStudio.TestTools.UnitTesting;
using MissionPlanner.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace MissionPlannerTests.Utilities
{
    [TestClass]
    public class BrandLogoLinksTests
    {
        private static void InSta(Action action)
        {
            Exception failure = null;
            var thread = new Thread(() =>
            {
                try { action(); }
                catch (Exception error) { failure = error; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(30)), "UI test timed out");
            if (failure != null) throw new Exception("STA UI test failed", failure);
        }

        [DataTestMethod]
        [DataRow(1.0, 847, false)]
        [DataRow(1.0, 847, true)]
        [DataRow(1.0, 1231, true)]
        [DataRow(1.25, 847, true)]
        [DataRow(1.5, 847, true)]
        [DataRow(2.0, 847, false)]
        [DataRow(2.0, 847, true)]
        [DataRow(2.0, 1231, true)]
        [DataRow(2.0, 1920, true)]
        public void NarrowAndScaledMenusKeepBrandWholeAndConnectionsReachable(double scale, int width, bool connected)
        {
            InSta(() =>
            {
                using (var fixture = new ToolbarFixture(scale, width, connected))
                {
                    var menu = fixture.Menu;
                    var brand = fixture.Brand;
                    Assert.IsTrue(menu.CanOverflow);
                    Assert.IsFalse(fixture.Original.Available);
                    Assert.AreEqual(ToolStripItemOverflow.Never, fixture.Connection.Overflow);
                    Assert.AreEqual(ToolStripItemOverflow.Never, fixture.Connect.Overflow);
                    Assert.AreEqual(ToolStripItemPlacement.Main, fixture.Connection.Placement);
                    Assert.AreEqual(ToolStripItemPlacement.Main, fixture.Connect.Placement);
                    Assert.IsTrue(menu.ClientRectangle.Contains(fixture.Connection.Bounds));
                    Assert.IsTrue(menu.ClientRectangle.Contains(fixture.Connect.Bounds));
                    Assert.AreEqual(2, brand.Control.Controls.Count);
                    // Native GetChildAtPoint excludes invisible overflow controls.
                    // Check geometry now and exercise hit testing after opening it.
                    AssertPartition(brand.Control, brand.Placement == ToolStripItemPlacement.Main);
                    foreach (ToolStripItem item in menu.Items)
                    {
                        var hosted = item as ToolStripControlHost;
                        if (hosted == null || hosted == fixture.Connection || !item.Available || item.Placement != ToolStripItemPlacement.Main) continue;
                        Assert.IsFalse(hosted.Control.Bounds.IntersectsWith(fixture.Connection.Control.Bounds),
                            item.Name + " covers the serial/baud controls");
                    }
                    if (brand.Placement == ToolStripItemPlacement.Main)
                    {
                        Assert.IsTrue(menu.ClientRectangle.Contains(brand.Bounds));
                        foreach (ToolStripItem item in menu.Items)
                        {
                            if (item == brand || !item.Available || item.Placement != ToolStripItemPlacement.Main) continue;
                            Assert.IsFalse(item.Bounds.IntersectsWith(brand.Bounds), item.Name + " overlaps the logo");
                            var host = item as ToolStripControlHost;
                            if (host != null)
                                Assert.IsFalse(host.Control.Bounds.IntersectsWith(brand.Control.Bounds), item.Name + " control covers the logo");
                        }
                    }
                    else
                    {
                        Assert.AreEqual(ToolStripItemPlacement.Overflow, brand.Placement);
                        menu.OverflowButton.ShowDropDown();
                        Application.DoEvents();
                        Assert.IsTrue(brand.Control.Visible, "Whole logo must remain reachable in overflow");
                        Assert.IsTrue(brand.Control.Controls.Cast<Control>().All(control => control.Visible));
                        AssertPartition(brand.Control);
                        var parent = brand.GetCurrentParent();
                        Assert.IsTrue(parent.ClientRectangle.Contains(brand.Control.Bounds), "Overflow must contain the complete logo");
                        fixture.SaveScreenshot("overflow-" + scale + "-" + width + "-" + connected, parent);
                        menu.OverflowButton.HideDropDown();
                    }
                    fixture.SaveScreenshot("layout-" + scale + "-" + width + "-" + connected);
                }
            });
        }

        private static void AssertPartition(Control links, bool hitTest = true)
        {
            int middle = links.Width / 2;
            var left = links.Controls["MenuArduPilotLink"];
            var right = links.Controls["MenuLocalizedGitHub"];
            Assert.AreEqual(0, left.Left);
            Assert.AreEqual(middle, left.Width);
            Assert.AreEqual(middle, right.Left);
            Assert.AreEqual(links.Width, right.Right);
            Assert.AreEqual(links.Height, left.Height);
            Assert.AreEqual(links.Height, right.Height);
            if (hitTest)
            {
                Assert.AreEqual(left, links.GetChildAtPoint(new Point(middle - 1, links.Height / 2)));
                Assert.AreEqual(right, links.GetChildAtPoint(new Point(middle, links.Height / 2)));
            }
        }

        [TestMethod]
        public void LinksExposeIndependentNamesAndExactClickBoundary()
        {
            InSta(() =>
            {
                using (var fixture = new ToolbarFixture(1, 1231, false))
                {
                    var links = fixture.Brand.Control;
                    var left = (Button)links.Controls["MenuArduPilotLink"];
                    var right = (Button)links.Controls["MenuLocalizedGitHub"];
                    AssertPartition(links);
                    Assert.AreEqual("ArduPilot", left.AccessibleName);
                    Assert.AreEqual("GitHub: reliable-ly0411", right.AccessibleName);
                    Assert.AreEqual(AccessibleRole.Link, left.AccessibleRole);
                    Assert.AreEqual(AccessibleRole.Link, right.AccessibleRole);
                    Assert.AreEqual(BrandLogoLinks.ArduPilotUrl, left.AccessibleDescription);
                    Assert.AreEqual(BrandLogoLinks.GitHubUrl, right.AccessibleDescription);
                    Assert.AreEqual(0, left.TabIndex);
                    Assert.AreEqual(1, right.TabIndex);
                    Assert.IsTrue(left.TabStop && right.TabStop);
                    left.PerformClick();
                    right.PerformClick();
                    CollectionAssert.AreEqual(new[] { BrandLogoLinks.ArduPilotUrl, BrandLogoLinks.GitHubUrl }, fixture.Opened);
                }
            });
        }

        [TestMethod]
        public void NativeAccessibilityDefaultActionsInvokeBothLinks()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                Assert.Inconclusive("Windows accessibility bridge is covered on the Windows runner");
            InSta(() =>
            {
                using (var fixture = new ToolbarFixture(1, 1231, false))
                {
                    var left = fixture.Brand.Control.Controls["MenuArduPilotLink"];
                    var right = fixture.Brand.Control.Controls["MenuLocalizedGitHub"];
                    Assert.AreEqual("ArduPilot", left.AccessibilityObject.Name);
                    Assert.AreEqual("GitHub: reliable-ly0411", right.AccessibilityObject.Name);
                    Assert.AreEqual(AccessibleRole.Link, left.AccessibilityObject.Role);
                    Assert.AreEqual(AccessibleRole.Link, right.AccessibilityObject.Role);
                    left.AccessibilityObject.DoDefaultAction();
                    right.AccessibilityObject.DoDefaultAction();
                    CollectionAssert.AreEqual(new[] { BrandLogoLinks.ArduPilotUrl, BrandLogoLinks.GitHubUrl }, fixture.Opened);
                }
            });
        }

        [TestMethod]
        public void KeyboardArrowsAndEnterReachBothLinks()
        {
            InSta(() =>
            {
                using (var fixture = new ToolbarFixture(1, 1231, false))
                {
                    var left = fixture.Brand.Control.Controls["MenuArduPilotLink"];
                    var right = fixture.Brand.Control.Controls["MenuLocalizedGitHub"];
                    Assert.IsTrue(left.Focus(), "Focus left link");
                    Assert.IsTrue(PreProcess(left, Keys.Tab), "Tab advances within group");
                    Assert.IsTrue(right.Focused, "Right link receives Tab");
                    Assert.IsTrue(PreProcess(right, Keys.Shift | Keys.Tab), "Shift+Tab returns within group");
                    Assert.IsTrue(left.Focused, "Left link receives Shift+Tab");
                    Assert.IsTrue(PreProcess(left, Keys.Right), "Right arrow handled");
                    Assert.IsTrue(right.Focused, "Right arrow focuses GitHub");
                    Assert.IsTrue(PreProcess(right, Keys.Enter), "Enter invokes GitHub");
                    Assert.IsTrue(PreProcess(right, Keys.Left), "Left arrow handled");
                    Assert.IsTrue(left.Focused, "Left arrow focuses ArduPilot");
                    Assert.IsTrue(PreProcess(left, Keys.Enter), "Enter invokes ArduPilot");
                    CollectionAssert.AreEqual(new[] { BrandLogoLinks.GitHubUrl, BrandLogoLinks.ArduPilotUrl }, fixture.Opened);
                }
            });
        }

        private static bool PreProcess(Control control, Keys key)
        {
            var message = Message.Create(control.Handle, 0x100, (IntPtr)key, IntPtr.Zero);
            return control.PreProcessMessage(ref message);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

        [TestMethod]
        public void NativeSpaceKeyInvokesFocusedLink()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                Assert.Inconclusive("Native Windows key dispatch is covered on the Windows runner");
            InSta(() =>
            {
                using (var fixture = new ToolbarFixture(1, 1231, false))
                {
                    foreach (var name in new[] { "MenuArduPilotLink", "MenuLocalizedGitHub" })
                    {
                        var link = fixture.Brand.Control.Controls[name];
                        Assert.IsTrue(link.Focus());
                        SendMessage(link.Handle, 0x100, (IntPtr)Keys.Space, (IntPtr)1);
                        SendMessage(link.Handle, 0x101, (IntPtr)Keys.Space, (IntPtr)1);
                    }
                    CollectionAssert.AreEqual(new[] { BrandLogoLinks.ArduPilotUrl, BrandLogoLinks.GitHubUrl }, fixture.Opened);
                }
            });
        }

        [TestMethod]
        public void NativeMouseLeaveFromEitherLinkReachesToolbarHost()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                Assert.Inconclusive("Native mouse dispatch is covered on the Windows runner");
            InSta(() =>
            {
                using (var fixture = new ToolbarFixture(1, 1231, false))
                {
                    int leaves = 0;
                    fixture.Brand.MouseLeave += (sender, args) => leaves++;
                    foreach (var name in new[] { "MenuArduPilotLink", "MenuLocalizedGitHub" })
                        SendMessage(fixture.Brand.Control.Controls[name].Handle, 0x2A3, IntPtr.Zero, IntPtr.Zero);
                    Assert.IsTrue(leaves >= 2, "Auto-hide must observe leaving either child link");
                }
            });
        }

        [TestMethod]
        public void LiveToolbarResizingKeepsImageProportionsAndRebuildsHitRegions()
        {
            InSta(() =>
            {
                using (var fixture = new ToolbarFixture(1, 1920, false))
                {
                    int previous = 0;
                    foreach (int height in new[] { 58, 73, 87, 116 })
                    {
                        fixture.Menu.Height = height;
                        fixture.Menu.PerformLayout();
                        Application.DoEvents();
                        Assert.IsTrue(fixture.Brand.Width > previous, "Resize must use the live toolbar height");
                        Assert.AreEqual(596.0 / 182.0, fixture.Brand.Width / (double)(height * 2 / 3), 0.03);
                        AssertPartition(fixture.Brand.Control);
                        previous = fixture.Brand.Width;
                    }
                }
            });
        }

        [TestMethod]
        public void PngKeepsSuppliedDimensionsCoreColorsAndTransparentCounters()
        {
            using (var stream = typeof(BrandLogoLinks).Assembly.GetManifestResourceStream("MissionPlanner.Resources.LocalizedBrandLogo.png"))
            using (var image = new Bitmap(stream))
            {
                Assert.AreEqual(new Size(596, 182), image.Size);
                Assert.AreEqual(Color.FromArgb(255, 25, 141, 255).ToArgb(), image.GetPixel(40, 140).ToArgb());
                Assert.AreEqual(Color.FromArgb(255, 15, 129, 244).ToArgb(), image.GetPixel(145, 111).ToArgb());
                Assert.AreEqual(Color.FromArgb(255, 42, 42, 42).ToArgb(), image.GetPixel(567, 125).ToArgb());
                Assert.AreEqual(Color.FromArgb(255, 45, 40, 36).ToArgb(), image.GetPixel(382, 90).ToArgb());
                foreach (var point in new[] { Point.Empty, new Point(595, 181), new Point(380, 125) })
                    Assert.AreEqual(0, (int)image.GetPixel(point.X, point.Y).A);
                Assert.IsTrue(Enumerable.Range(0, image.Width).Any(x => image.GetPixel(x, 90).A > 0 && image.GetPixel(x, 90).A < 255));
            }
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void NavigationRefreshPaintsTheActualToolbarBackground(bool dark)
        {
            InSta(() =>
            {
                using (var fixture = new ToolbarFixture(1, 1231, false))
                using (var background = new Bitmap(2, 2))
                using (var frame = new Bitmap(fixture.Menu.Width, fixture.Menu.Height))
                {
                    var color = dark ? Color.FromArgb(64, 64, 64) : Color.WhiteSmoke;
                    using (var graphics = Graphics.FromImage(background)) graphics.Clear(color);
                    fixture.Menu.BackgroundImage = background;
                    fixture.Menu.BackgroundImageLayout = ImageLayout.Tile;
                    for (int page = 0; page < 4; page++)
                    {
                        fixture.Brand.BackColor = Color.WhiteSmoke;
                        fixture.Brand.BackgroundImage = background;
                        fixture.Brand.Control.BackColor = Color.WhiteSmoke;
                        Assert.IsTrue(BrandLogoLinks.PreserveSurface(fixture.Brand));
                        Assert.AreEqual(Color.Transparent, fixture.Brand.BackColor);
                        Assert.AreEqual(Color.Transparent, fixture.Brand.Control.BackColor);
                        Assert.IsNull(fixture.Brand.BackgroundImage);
                        Assert.IsFalse(BrandLogoLinks.PreserveSurface(fixture.Connect));
                        fixture.Menu.DrawToBitmap(frame, fixture.Menu.ClientRectangle);
                        Assert.AreEqual(color.ToArgb(), frame.GetPixel(fixture.Brand.Bounds.Left + 2, 2).ToArgb(), "No white logo rectangle");
                    }
                    fixture.SaveScreenshot(dark ? "dark" : "light");
                    fixture.Menu.BackgroundImage = null;
                }
            });
        }

        private sealed class ToolbarFixture : IDisposable
        {
            public readonly Form Form;
            public readonly MenuStrip Menu;
            public readonly ToolStripControlHost Brand;
            public readonly ToolStripControlHost Connection;
            public readonly ToolStripButton Connect;
            public readonly ToolStripButton Original;
            public readonly List<string> Opened = new List<string>();

            public ToolbarFixture(double scale, int width, bool connected)
            {
                Form = new Form { AutoScaleMode = AutoScaleMode.None, ClientSize = new Size(width, 250), StartPosition = FormStartPosition.Manual, Location = Point.Empty };
                Menu = new MenuStrip { AutoSize = false, Height = (int)Math.Round(58 * scale), Padding = Padding.Empty, BackColor = Color.FromArgb(64, 64, 64) };
                Form.Controls.Add(Menu);
                for (int index = 0; index < 6; index++)
                    Menu.Items.Add(new ToolStripButton { Name = "Navigation" + index, Text = "Tab " + index,
                        AutoSize = false, Size = new Size((int)(55 * scale), Menu.Height), Margin = Padding.Empty });
                Menu.Items.Add(new ToolStripControlHost(new Panel { Size = new Size((int)(200 * scale), (int)(32 * scale)),
                    MinimumSize = new Size((int)(180 * scale), (int)(32 * scale)) })
                    { Name = "VehicleState", AutoSize = false, Size = new Size((int)(200 * scale), (int)(32 * scale)),
                        Margin = new Padding((int)(8 * scale), 0, 0, 0), Visible = connected });
                Connect = new ToolStripButton { Name = "MenuConnect", Text = "Connect", Alignment = ToolStripItemAlignment.Right,
                    AutoSize = false, Size = new Size((int)(79 * scale), Menu.Height), Margin = Padding.Empty };
                Menu.Items.Add(Connect);
                var control = new UserControl { AutoScaleMode = AutoScaleMode.None, Size = new Size((int)(241 * scale), Menu.Height),
                    MinimumSize = new Size((int)(200 * scale), (int)(47 * scale)) };
                control.Controls.Add(new ComboBox { Name = "Serial", Location = Point.Empty, Width = (int)(121 * scale), Text = "AUTO" });
                control.Controls.Add(new ComboBox { Name = "Baud", Location = new Point((int)(127 * scale), 0), Width = (int)(111 * scale), Text = "115200" });
                Connection = new ToolStripControlHost(control) { Name = "Connection", Alignment = ToolStripItemAlignment.Right, Margin = Padding.Empty };
                Menu.Items.Add(Connection);
                Original = new ToolStripButton { Name = "MenuArduPilot", Alignment = ToolStripItemAlignment.Right,
                    AutoSize = false, Size = new Size(255, 40), Margin = Padding.Empty };
                Menu.Items.Add(Original);
                Brand = BrandLogoLinks.Configure(Menu, Original, Opened.Add, Connection, Connect);
                Form.Show();
                Menu.PerformLayout();
                Application.DoEvents();
                SaveScreenshot("initial-" + scale + "-" + width + "-" + connected);
            }

            public void SaveScreenshot(string name, Control surface = null)
            {
                var directory = Environment.GetEnvironmentVariable("BRAND_LOGO_SCREENSHOTS");
                if (string.IsNullOrEmpty(directory)) return;
                System.IO.Directory.CreateDirectory(directory);
                surface = surface ?? Menu;
                using (var frame = new Bitmap(surface.Width, surface.Height))
                {
                    surface.DrawToBitmap(frame, surface.ClientRectangle);
                    frame.Save(System.IO.Path.Combine(directory, name + ".png"));
                }
            }

            public void Dispose()
            {
                Form.Close();
                Form.Dispose();
            }
        }
    }
}
