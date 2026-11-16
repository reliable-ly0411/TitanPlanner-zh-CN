using Microsoft.VisualStudio.TestTools.UnitTesting;
using MissionPlanner.Utilities;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace MissionPlannerTests.Utilities
{
    [TestClass]
    public class BrandLogoPaintTests
    {
        private static readonly Color Poison = Color.Magenta;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

        [DllImport("dwmapi.dll")]
        private static extern int DwmFlush();

        private static void InNativeSta(Action test)
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                Assert.Inconclusive("Real WM_PAINT and desktop pixels are checked on Windows");
            Exception failure = null;
            var thread = new Thread(() =>
            {
                try { test(); }
                catch (Exception error) { failure = error; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(30)), "Native paint test timed out");
            if (failure != null) throw new Exception("Native paint regression", failure);
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void WmPaintReplacesOldPixelsWithTheCompleteOriginalLogo(bool dark)
        {
            InNativeSta(() =>
            {
                using (var fixture = new PaintFixture(dark, false, 58))
                using (var expected = ExpectedLogo(fixture.Brand.Control.Size, fixture.Background))
                using (var actual = new Bitmap(expected.Width, expected.Height))
                {
                    using (var graphics = Graphics.FromImage(actual)) graphics.Clear(Poison);
                    foreach (Control link in fixture.Brand.Control.Controls)
                    {
                        var style = typeof(Control).GetMethod("GetStyle", BindingFlags.Instance | BindingFlags.NonPublic);
                        Console.WriteLine(link.Name + " Opaque=" + style.Invoke(link, new object[] { ControlStyles.Opaque }));
                        using (var half = new Bitmap(link.Width, link.Height))
                        {
                            // Seed a previous frame, then dispatch WM_PAINT without a
                            // background erase. WM_PRINT/DrawToBitmap takes another path.
                            using (var graphics = Graphics.FromImage(half))
                            {
                                graphics.Clear(Poison);
                                var dc = graphics.GetHdc();
                                try { SendMessage(link.Handle, 0x000F, dc, IntPtr.Zero); }
                                finally { graphics.ReleaseHdc(dc); }
                            }
                            using (var graphics = Graphics.FromImage(actual))
                                graphics.DrawImageUnscaled(half, link.Location);
                        }
                    }
                    Save("wm-paint-" + dark, actual);
                    AssertPixels(expected, actual, fixture.Background, "WM_PAINT " + dark);
                }
            });
        }

        [DataTestMethod]
        [DataRow(false, false, 58)]
        [DataRow(true, false, 87)]
        [DataRow(false, true, 116)]
        [DataRow(true, true, 58)]
        public void LiveDesktopKeepsLogoVisibleAcrossPageChangesResizeAndOverlay(bool dark, bool overlay, int height)
        {
            InNativeSta(() =>
            {
                using (var fixture = new PaintFixture(dark, overlay, height))
                {
                    string name = "live-" + dark + "-" + overlay + "-" + height;
                    fixture.RevealToolbarOverPreviousPage();
                    fixture.AssertLivePixels(name + "-revealed");
                    fixture.Form.WindowState = FormWindowState.Maximized;
                    fixture.Pump();
                    fixture.AssertLivePixels(name + "-maximized");
                    fixture.Form.WindowState = FormWindowState.Normal;
                    var area = Screen.PrimaryScreen.WorkingArea;
                    fixture.Form.ClientSize = new Size(Math.Min(1420, area.Width - 80), Math.Min(790, area.Height - 120));
                    fixture.Pump();
                    fixture.AssertLivePixels(name + "-restored");
                    fixture.ChangePageAndBackground();
                    fixture.AssertLivePixels(name + "-page-changed");
                    fixture.Header.Visible = false;
                    fixture.Pump();
                    fixture.Header.Visible = true;
                    fixture.Header.BringToFront();
                    fixture.Pump();
                    fixture.AssertLivePixels(name + "-shown-again");
                }
            });
        }

        private static Bitmap ExpectedLogo(Size size, Color background)
        {
            var expected = new Bitmap(size.Width, size.Height);
            using (var stream = typeof(BrandLogoLinks).Assembly.GetManifestResourceStream("MissionPlanner.Resources.LocalizedBrandLogo.png"))
            using (var source = new Bitmap(stream))
            using (var graphics = Graphics.FromImage(expected))
            {
                graphics.Clear(background);
                int height = (int)Math.Round(size.Width * (double)source.Height / source.Width);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.DrawImage(source, new Rectangle(0, (size.Height - height) / 2, size.Width, height));
            }
            return expected;
        }

        private static void AssertPixels(Bitmap expected, Bitmap actual, Color background, string stage)
        {
            Assert.AreEqual(expected.Size, actual.Size);
            int bad = 0, ghosts = 0;
            long error = 0;
            var foreground = new int[2];
            var matched = new int[2];
            for (int y = 0; y < expected.Height; y++)
            for (int x = 0; x < expected.Width; x++)
            {
                var a = actual.GetPixel(x, y);
                var e = expected.GetPixel(x, y);
                int delta = Math.Max(Math.Abs(a.R - e.R), Math.Max(Math.Abs(a.G - e.G), Math.Abs(a.B - e.B)));
                error += Math.Abs(a.R - e.R) + Math.Abs(a.G - e.G) + Math.Abs(a.B - e.B);
                if (delta > 16) bad++;
                if (a.R > 220 && a.G < 40 && a.B > 220) ghosts++;
                if (Math.Max(Math.Abs(e.R - background.R), Math.Max(Math.Abs(e.G - background.G), Math.Abs(e.B - background.B))) >= 10)
                {
                    int half = x < expected.Width / 2 ? 0 : 1;
                    foreground[half]++;
                    if (delta <= 16) matched[half]++;
                }
            }
            double mean = error / (3.0 * expected.Width * expected.Height);
            string metrics = stage + ": mean RGB error=" + mean + ", wrong pixels=" + bad + ", stale magenta=" + ghosts
                + ", foreground left=" + matched[0] + "/" + foreground[0] + ", right=" + matched[1] + "/" + foreground[1];
            Console.WriteLine(metrics);
            Assert.AreEqual(0, ghosts, metrics);
            for (int half = 0; half < 2; half++)
            {
                Assert.IsTrue(foreground[half] > 20, "Reference must contain artwork in both halves");
                Assert.IsTrue(matched[half] >= foreground[half] * 0.95, metrics);
            }
            Assert.IsTrue(mean <= 2.0 && bad <= expected.Width * expected.Height * 0.01, metrics);
        }

        private static void Save(string name, Bitmap bitmap)
        {
            var directory = Environment.GetEnvironmentVariable("BRAND_LOGO_SCREENSHOTS");
            if (string.IsNullOrEmpty(directory)) return;
            directory = Path.Combine(directory, "actual-paint");
            Directory.CreateDirectory(directory);
            bitmap.Save(Path.Combine(directory, name + ".png"), ImageFormat.Png);
        }

        private sealed class PaintForm : Form
        {
            public PaintForm() { DoubleBuffered = true; }
        }

        private sealed class PaintFixture : IDisposable
        {
            public readonly Form Form;
            public readonly Panel Header;
            public readonly MenuStrip Menu;
            public readonly ToolStripControlHost Brand;
            public Color Background;
            private readonly Panel page;
            private readonly CheckBox parameter;
            private Bitmap tile;

            public PaintFixture(bool dark, bool overlay, int height)
            {
                var area = Screen.PrimaryScreen.WorkingArea;
                Assert.IsTrue(area.Width >= 1000 && area.Height >= 600, "Native visual fixture needs an unobscured 1000x600 desktop");
                Form = new PaintForm { AutoScaleMode = AutoScaleMode.None, TopMost = true,
                    StartPosition = FormStartPosition.Manual, Location = area.Location,
                    ClientSize = new Size(Math.Min(1420, area.Width - 32), Math.Min(790, area.Height - 80)) };
                page = new Panel { Dock = DockStyle.Fill, BackColor = Poison };
                parameter = new CheckBox { Text = "参数", AutoSize = true, Location = new Point(Form.ClientSize.Width - 110, 16) };
                page.Controls.Add(parameter);
                page.Paint += (sender, args) => args.Graphics.DrawEllipse(Pens.Lime, page.Width - 140, 4, 100, 100);
                Form.Controls.Add(page);
                Header = new Panel { Name = "ToolbarPanel", Height = height, Dock = overlay ? DockStyle.None : DockStyle.Top,
                    Location = Point.Empty, Width = Form.ClientSize.Width, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
                Menu = new MenuStrip { Dock = DockStyle.Fill, AutoSize = false, Padding = Padding.Empty };
                Header.Controls.Add(Menu);
                Form.Controls.Add(Header);
                Header.BringToFront();
                for (int index = 0; index < 6; index++)
                    Menu.Items.Add(new ToolStripButton { Name = "Navigation" + index, Text = "Tab " + index,
                        AutoSize = false, Size = new Size(55, height), Margin = Padding.Empty });
                var connect = new ToolStripButton { Name = "Connect", Text = "Connect", AutoSize = false,
                    Size = new Size(79, height), Alignment = ToolStripItemAlignment.Right, Margin = Padding.Empty };
                var connection = new ToolStripControlHost(new Panel { Size = new Size(241, height) })
                    { Name = "SerialBaud", Alignment = ToolStripItemAlignment.Right, Margin = Padding.Empty };
                connection.Control.Controls.Add(new ComboBox { Text = "AUTO", Width = 121 });
                connection.Control.Controls.Add(new ComboBox { Text = "115200", Width = 111, Left = 127 });
                Menu.Items.Add(connect);
                Menu.Items.Add(connection);
                var original = new ToolStripButton { Name = "MenuArduPilot", Alignment = ToolStripItemAlignment.Right };
                Menu.Items.Add(original);
                Brand = BrandLogoLinks.Configure(Menu, original, url => { }, connection, connect);
                Background = dark ? Color.FromArgb(64, 64, 64) : Color.WhiteSmoke;
                SetBackground();
                Form.Show();
                parameter.Focus();
                Pump();
                Assert.AreEqual(ToolStripItemPlacement.Main, Brand.Placement, "Visual test must inspect a visible toolbar logo");
            }

            private void SetBackground()
            {
                var previous = tile;
                tile = new Bitmap(3, 3);
                using (var graphics = Graphics.FromImage(tile)) graphics.Clear(Background);
                Menu.BackColor = Background;
                Menu.BackgroundImage = tile;
                Menu.BackgroundImageLayout = ImageLayout.Tile;
                if (previous != null) previous.Dispose();
                BrandLogoLinks.PreserveSurface(Brand);
            }

            public void Pump()
            {
                Form.PerformLayout();
                Form.Invalidate(true);
                Form.Update();
                Application.DoEvents();
                DwmFlush();
            }

            public void RevealToolbarOverPreviousPage()
            {
                Header.Visible = false;
                Pump();
                var point = Form.PointToScreen(new Point(Form.ClientSize.Width / 2, 2));
                using (var probe = new Bitmap(1, 1))
                {
                    using (var graphics = Graphics.FromImage(probe)) graphics.CopyFromScreen(point, Point.Empty, probe.Size);
                    Assert.AreEqual(Poison.ToArgb(), probe.GetPixel(0, 0).ToArgb(), "Desktop capture must see the seeded previous page");
                }
                Header.Visible = true;
                Header.BringToFront();
                Pump();
            }

            public void ChangePageAndBackground()
            {
                page.BackColor = Color.Lime;
                parameter.Text = "下一页";
                Background = Background.R < 100 ? Color.WhiteSmoke : Color.FromArgb(64, 64, 64);
                SetBackground();
                parameter.Focus();
                Pump();
            }

            public void AssertLivePixels(string name)
            {
                var bounds = Brand.Control.RectangleToScreen(Brand.Control.ClientRectangle);
                Assert.IsTrue(Header.RectangleToScreen(Header.ClientRectangle).Contains(bounds), "Logo must lie inside the actual header");
                Assert.IsTrue(Screen.FromControl(Form).Bounds.Contains(bounds), "The complete logo must be on screen for pixel verification");
                using (var actual = new Bitmap(bounds.Width, bounds.Height))
                using (var expected = ExpectedLogo(bounds.Size, Background))
                {
                    using (var graphics = Graphics.FromImage(actual)) graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                    Save(name, actual);
                    // Retain an actual desktop capture before any assertions fail.
                    var window = Rectangle.Intersect(Form.Bounds, Screen.FromControl(Form).Bounds);
                    using (var frame = new Bitmap(window.Width, window.Height))
                    {
                        using (var graphics = Graphics.FromImage(frame)) graphics.CopyFromScreen(window.Location, Point.Empty, window.Size);
                        Save(name + "-window", frame);
                    }
                    AssertPixels(expected, actual, Background, name);
                }
            }

            public void Dispose()
            {
                Form.Close();
                Form.Dispose();
                tile.Dispose();
            }
        }
    }
}
