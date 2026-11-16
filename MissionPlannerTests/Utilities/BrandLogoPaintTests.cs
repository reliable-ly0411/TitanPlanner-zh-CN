using Microsoft.VisualStudio.TestTools.UnitTesting;
using MissionPlanner.Utilities;
using System;
using System.Drawing;
using System.Diagnostics;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;
using System.Windows.Forms;

namespace MissionPlannerTests.Utilities
{
    [TestClass]
    public class BrandLogoPaintTests
    {
        private static readonly Color Poison = Color.Magenta;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(Point point);

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr window, uint flags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RedrawWindow(IntPtr window, IntPtr rectangle, IntPtr region, uint flags);
        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool GdiFlush();
        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDpiAwarenessContext(IntPtr window);
        [DllImport("user32.dll")]
        private static extern bool AreDpiAwarenessContextsEqual(IntPtr first, IntPtr second);

        [DllImport("dwmapi.dll")]
        private static extern int DwmFlush();

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left, Top, Right, Bottom;
            public Rectangle Rectangle { get { return Rectangle.FromLTRB(Left, Top, Right, Bottom); } }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MonitorInfo
        {
            public int Size;
            public NativeRect Monitor, Work;
            public uint Flags;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
        [DllImport("user32.dll")]
        private static extern IntPtr GetThreadDpiAwarenessContext();
        [DllImport("user32.dll")]
        private static extern int GetAwarenessFromDpiAwarenessContext(IntPtr context);
        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr window);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetClientRect(IntPtr window, out NativeRect rectangle);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr window, out NativeRect rectangle);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ClientToScreen(IntPtr window, ref Point point);
        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr window, StringBuilder name, int capacity);

        private sealed class NativeDpiScope : IDisposable
        {
            private readonly IntPtr previous;
            public NativeDpiScope()
            {
                previous = SetThreadDpiAwarenessContext(new IntPtr(-4)); // per-monitor v2
                if (previous == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
                Console.WriteLine("DPI awareness previous=" + GetAwarenessFromDpiAwarenessContext(previous)
                    + ", capture=" + GetAwarenessFromDpiAwarenessContext(GetThreadDpiAwarenessContext()));
                Assert.AreEqual(2, GetAwarenessFromDpiAwarenessContext(GetThreadDpiAwarenessContext()),
                    "Pixel fixture must use one physical per-monitor coordinate space");
            }
            public void Dispose()
            {
                if (SetThreadDpiAwarenessContext(previous) == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        private static Rectangle PhysicalClientBounds(Control control)
        {
            NativeRect client;
            var origin = Point.Empty;
            if (!GetClientRect(control.Handle, out client) || !ClientToScreen(control.Handle, ref origin))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            return new Rectangle(origin, client.Rectangle.Size);
        }

        private static Rectangle PhysicalWindowBounds(Control control)
        {
            NativeRect bounds;
            if (!GetWindowRect(control.Handle, out bounds)) throw new Win32Exception(Marshal.GetLastWin32Error());
            return bounds.Rectangle;
        }

        private static Rectangle PhysicalMonitorBounds(Control control, bool workingArea)
        {
            var info = new MonitorInfo { Size = Marshal.SizeOf(typeof(MonitorInfo)) };
            var monitor = MonitorFromWindow(control == null ? IntPtr.Zero : control.Handle, control == null ? 1u : 2u);
            if (!GetMonitorInfo(monitor, ref info)) throw new Win32Exception(Marshal.GetLastWin32Error());
            return workingArea ? info.Work.Rectangle : info.Monitor.Rectangle;
        }

        private static void InNativeSta(Action test)
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                Assert.Inconclusive("Real WM_PAINT and desktop pixels are checked on Windows");
            Exception failure = null;
            var thread = new Thread(() =>
            {
                try { using (var dpi = new NativeDpiScope()) test(); }
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
                    fixture.RevealToolbarOverPreviousPage(name);
                    fixture.AssertLivePixels(name + "-revealed");
                    fixture.Form.WindowState = FormWindowState.Maximized;
                    fixture.Pump();
                    fixture.AssertLivePixels(name + "-maximized");
                    fixture.Form.WindowState = FormWindowState.Normal;
                    var area = PhysicalMonitorBounds(null, true);
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

        [TestMethod]
        public void CapturedFrameKeepsTheSamePhysicalPixelAfterTheDesktopChanges()
        {
            InNativeSta(() =>
            {
                using (var fixture = new PaintFixture(false, false, 58))
                {
                    fixture.Header.Visible = false;
                    var patch = fixture.AddCalibrationPatch(new Point(80, 80), Color.Red);
                    fixture.Pump();
                    var bounds = PhysicalClientBounds(patch);
                    var point = new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
                    using (var red = fixture.CaptureClientFrame("calibration-red", point))
                    {
                        Assert.AreEqual(Color.Red.ToArgb(), red.ReadPhysical(point).ToArgb());
                        patch.BackColor = Color.Blue;
                        fixture.Pump();
                        using (var blue = fixture.CaptureClientFrame("calibration-blue", point))
                        using (var png = new MemoryStream())
                        {
                            Assert.AreEqual(Color.Blue.ToArgb(), blue.ReadPhysical(point).ToArgb());
                            Assert.AreEqual(Color.Red.ToArgb(), red.ReadPhysical(point).ToArgb(), "Earlier frame must stay unchanged");
                            red.Pixels.Save(png, ImageFormat.Png);
                            png.Position = 0;
                            using (var reopened = new Bitmap(png))
                                Assert.AreEqual(red.ReadPhysical(point).ToArgb(), reopened.GetPixel(point.X - red.Bounds.Left, point.Y - red.Bounds.Top).ToArgb(),
                                    "The saved PNG and assertion must identify the same pixel in the same frame");
                        }
                    }
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
                var destination = new Rectangle(0, (size.Height - height) / 2, size.Width, height);
                if (Contrast(background, Color.FromArgb(42, 42, 42)) < 4.5)
                {
                    // Golden composition: a thin neutral underlay follows only dark
                    // source ink; the supplied PNG is still the final foreground.
                    using (var ink = new Bitmap(source.Width, source.Height))
                    {
                        for (int y = 0; y < source.Height; y++)
                        for (int x = 0; x < source.Width; x++)
                        {
                            var pixel = source.GetPixel(x, y);
                            if (Math.Max(pixel.R, Math.Max(pixel.G, pixel.B)) <= 110)
                                ink.SetPixel(x, y, Color.FromArgb((pixel.A * 217 + 127) / 255, 190, 190, 190));
                        }
                        const int radius = 1;
                        for (int dy = -radius; dy <= radius; dy++)
                        for (int dx = -radius; dx <= radius; dx++)
                        {
                            if (dx * dx + dy * dy == 0 || dx * dx + dy * dy > radius * (radius + 1)) continue;
                            graphics.DrawImage(ink, new Rectangle(destination.X + dx, destination.Y + dy, destination.Width, destination.Height));
                        }
                    }
                }
                graphics.DrawImage(source, destination);
            }
            return expected;
        }

        private static double Contrast(Color a, Color b)
        {
            Func<Color, double> luminance = color =>
            {
                Func<byte, double> linear = channel => channel / 255.0 <= 0.04045 ? channel / 255.0 / 12.92
                    : Math.Pow((channel / 255.0 + 0.055) / 1.055, 2.4);
                return 0.2126 * linear(color.R) + 0.7152 * linear(color.G) + 0.0722 * linear(color.B);
            };
            double first = luminance(a), second = luminance(b);
            return (Math.Max(first, second) + 0.05) / (Math.Min(first, second) + 0.05);
        }

        [DataTestMethod]
        [DataRow(58)]
        [DataRow(87)]
        [DataRow(116)]
        public void DarkToolbarKeepsOriginalInkAndAddsReadableContourWithoutAWhiteRectangle(int toolbarHeight)
        {
            InNativeSta(() =>
            {
                using (var fixture = new PaintFixture(true, false, toolbarHeight))
                {
                    fixture.RevealToolbarOverPreviousPage("contrast-" + toolbarHeight);
                    fixture.AssertLivePixels("contrast-" + toolbarHeight);
                    var bounds = PhysicalClientBounds(fixture.Brand.Control);
                    using (var frame = fixture.CaptureClientFrame("contrast-" + toolbarHeight + "-core"))
                    using (var actual = frame.CropPhysical(bounds))
                    using (var sourceStream = typeof(BrandLogoLinks).Assembly.GetManifestResourceStream("MissionPlanner.Resources.LocalizedBrandLogo.png"))
                    using (var source = new Bitmap(sourceStream))
                    {
                        int imageHeight = (int)Math.Round(bounds.Width * (double)source.Height / source.Width);
                        int top = (bounds.Height - imageHeight) / 2;
                        using (var scaled = new Bitmap(bounds.Width, imageHeight))
                        {
                            using (var graphics = Graphics.FromImage(scaled))
                            {
                                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                                graphics.DrawImage(source, new Rectangle(Point.Empty, scaled.Size));
                            }
                            int core = 0, contour = 0;
                            for (int y = 0; y < scaled.Height; y++)
                            for (int x = 0; x < scaled.Width; x++)
                            {
                                var original = scaled.GetPixel(x, y);
                                var pixel = actual.GetPixel(x, top + y);
                                if (original.A == 255 && Math.Max(original.R, Math.Max(original.G, original.B)) <= 110)
                                {
                                    core++;
                                    Assert.IsTrue(Math.Abs(original.R - pixel.R) <= 2 && Math.Abs(original.G - pixel.G) <= 2
                                        && Math.Abs(original.B - pixel.B) <= 2, "Original black ink was recolored");
                                }
                                if (x >= scaled.Width / 2 && original.A == 0 && pixel.R >= 170
                                    && Math.Abs(pixel.R - pixel.G) <= 3 && Math.Abs(pixel.R - pixel.B) <= 3
                                    && Contrast(pixel, Color.FromArgb(42, 42, 42)) >= 4.5) contour++;
                            }
                            Assert.IsTrue(core >= 50, "Check actual original ink, not merely an accessible name");
                            Assert.IsTrue(contour >= 20, "Dark text needs a visibly contrasting contour");
                            Console.WriteLine("contrast height=" + toolbarHeight + ", original core=" + core + ", readable contour=" + contour);
                            for (int x = 0; x < actual.Width; x++)
                            {
                                Assert.AreEqual(fixture.Background.ToArgb(), actual.GetPixel(x, 1).ToArgb(), "No rectangular light backing");
                                Assert.AreEqual(fixture.Background.ToArgb(), actual.GetPixel(x, actual.Height - 2).ToArgb(), "No rectangular light backing");
                            }
                        }
                    }
                }
            });
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
            private int pagePaints;
            private string pageGraphics = "not painted";

            public PaintFixture(bool dark, bool overlay, int height)
            {
                var area = PhysicalMonitorBounds(null, true);
                Assert.IsTrue(area.Width >= 1000 && area.Height >= 600, "Native visual fixture needs an unobscured 1000x600 desktop");
                Form = new PaintForm { AutoScaleMode = AutoScaleMode.None, TopMost = true,
                    StartPosition = FormStartPosition.Manual, Location = area.Location,
                    ClientSize = new Size(Math.Min(1420, area.Width - 32), Math.Min(790, area.Height - 80)) };
                page = new Panel { Dock = DockStyle.Fill, BackColor = Poison };
                parameter = new CheckBox { Text = "参数", AutoSize = true, Location = new Point(Form.ClientSize.Width - 110, 16) };
                page.Controls.Add(parameter);
                page.Paint += (sender, args) =>
                {
                    pagePaints++;
                    using (var transform = args.Graphics.Transform)
                        pageGraphics = "dpi=" + args.Graphics.DpiX + "x" + args.Graphics.DpiY + ", pageUnit=" + args.Graphics.PageUnit
                            + ", pageScale=" + args.Graphics.PageScale + ", transform=" + string.Join(",", transform.Elements);
                    args.Graphics.DrawEllipse(Pens.Lime, page.Width - 140, 4, 100, 100);
                };
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
                // Hosted runners can open their own topmost console after our form.
                // Raise only this fixture, never hide or capture unrelated windows.
                Assert.IsTrue(SetWindowPos(Form.Handle, new IntPtr(-1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0040),
                    "Could not raise the native paint fixture");
                Form.Activate();
                Form.PerformLayout();
                Form.Invalidate(true);
                Application.DoEvents();
                // UpdateWindow(Form) alone does not synchronously paint every child.
                // Include the visible descendants and erase their old backing pixels.
                if (!RedrawWindow(Form.Handle, IntPtr.Zero, IntPtr.Zero, 0x0001 | 0x0004 | 0x0080 | 0x0100))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                if (!GdiFlush()) throw new Win32Exception(Marshal.GetLastWin32Error());
                int result = DwmFlush();
                if (result < 0) Marshal.ThrowExceptionForHR(result);
            }

            public void RevealToolbarOverPreviousPage(string name)
            {
                int paintsBefore = pagePaints;
                Header.Visible = false;
                Pump();
                var client = PhysicalClientBounds(Form);
                var body = PhysicalClientBounds(page);
                var point = new Point(body.Left + body.Width / 2, body.Top + 2);
                var legacy = Point.Empty;
                var context = SetThreadDpiAwarenessContext(new IntPtr(-1));
                if (context == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
                try { legacy = Form.PointToScreen(new Point(Form.ClientSize.Width / 2, 2)); }
                finally
                {
                    if (SetThreadDpiAwarenessContext(context) == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
                }
                var text = new StringBuilder();
                text.AppendLine("probe physical=" + point + ", legacy-unaware=" + legacy);
                foreach (var control in new Control[] { Form, page, Header, Menu, Brand.Control })
                {
                    var deviceDpi = typeof(Control).GetProperty("DeviceDpi");
                    text.AppendLine(control.GetType().Name + " hwnd=" + control.Handle + " nativeClient=" + PhysicalClientBounds(control)
                        + ", nativeWindow=" + PhysicalWindowBounds(control) + ", managedClient=" + control.ClientSize
                        + ", managedOrigin=" + control.PointToScreen(Point.Empty) + ", windowDpi=" + GetDpiForWindow(control.Handle)
                        + ", deviceDpi=" + (deviceDpi == null ? null : deviceDpi.GetValue(control, null))
                        + ", hwndAwareness=" + GetAwarenessFromDpiAwarenessContext(GetWindowDpiAwarenessContext(control.Handle))
                        + ", hwndPMv2=" + AreDpiAwarenessContextsEqual(GetWindowDpiAwarenessContext(control.Handle), new IntPtr(-4)));
                }
                text.AppendLine("monitor=" + PhysicalMonitorBounds(Form, false) + ", work=" + PhysicalMonitorBounds(Form, true)
                    + ", managedScreen=" + Screen.FromControl(Form).Bounds + ", threadAwareness="
                    + GetAwarenessFromDpiAwarenessContext(GetThreadDpiAwarenessContext()));
                text.AppendLine("page paints=" + paintsBefore + "->" + pagePaints + ", pageColor=" + page.BackColor + ", graphics=" + pageGraphics);
                var packedPoint = new IntPtr((point.X & 0xFFFF) | (point.Y << 16));
                text.AppendLine("form WM_NCHITTEST=" + SendMessage(Form.Handle, 0x0084, IntPtr.Zero, packedPoint) + " (client=1, caption=2)");
                using (var frame = CaptureClientFrame(name + "-probe", point))
                {
                    // PNG, crop and assertion all use this exact immutable bitmap.
                    var actual = frame.ReadPhysical(point);
                    var relative = new Point(point.X - frame.Bounds.Left, point.Y - frame.Bounds.Top);
                    text.AppendLine("asserted frame=" + frame.Digest + ", utc=" + frame.CapturedUtc.ToString("O")
                        + ", bounds=" + frame.Bounds + ", probe-relative=" + relative);
                    text.AppendLine(frame.Diagnostics);
                    var crop = Rectangle.Intersect(new Rectangle(point.X - 20, point.Y - 20, 41, 41), frame.Bounds);
                    using (var neighbourhood = frame.CropPhysical(crop)) Save(name + "-probe-neighbourhood", neighbourhood);
                    var window = WindowFromPoint(point);
                    var className = new StringBuilder(256);
                    GetClassName(window, className, className.Capacity);
                    text.AppendLine("probe expected=#FF00FF actual=#" + actual.R.ToString("X2") + actual.G.ToString("X2") + actual.B.ToString("X2")
                        + ", sampledHwnd=" + window + ", sampledRoot=" + GetAncestor(window, 2) + ", class=" + className);
                    if (frame.Bounds.Contains(legacy))
                    {
                        var oldColor = frame.ReadPhysical(legacy);
                        text.AppendLine("legacy-unaware point in SAME frame=#" + oldColor.R.ToString("X2")
                            + oldColor.G.ToString("X2") + oldColor.B.ToString("X2") + ", owner=" + GetAncestor(WindowFromPoint(legacy), 2));
                    }
                    Console.WriteLine(text.ToString());
                    SaveDiagnostics(name + "-probe", text.ToString());
                    Save(name + "-probe-window", frame.Pixels);
                    Assert.IsTrue(pagePaints > paintsBefore, "Hidden page must have painted after the toolbar was removed; " + text);
                    Assert.AreEqual(client.Top, body.Top, "Hidden toolbar must release the top client area; " + text);
                    Assert.AreEqual(client.Width, body.Width, "Previous page must fill the physical client width; " + text);
                    Assert.AreEqual(Form.Handle, GetAncestor(window, 2), "Probe is occluded; " + text);
                    Assert.AreEqual(page.Handle, window, "Probe must hit the exposed page, not non-client chrome or the hidden header; " + text);
                    Assert.AreEqual(Poison.ToArgb(), actual.ToArgb(), "Physical y=2 must see the previous page; " + text);
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
                Pump();
                var bounds = PhysicalClientBounds(Brand.Control);
                Assert.AreEqual(Brand.Control.ClientSize, bounds.Size, "Managed/native logo dimensions disagree; capture cannot be compared");
                Assert.IsTrue(PhysicalClientBounds(Header).Contains(bounds), "Logo must lie inside the actual header");
                Assert.IsTrue(PhysicalMonitorBounds(Form, false).Contains(bounds), "The complete logo must be on screen for pixel verification");
                AssertUnobscured(bounds);
                using (var frame = CaptureClientFrame(name))
                using (var actual = frame.CropPhysical(bounds))
                using (var expected = ExpectedLogo(bounds.Size, Background))
                {
                    AssertUnobscured(bounds);
                    Save(name, actual);
                    Save(name + "-window", frame.Pixels);
                    AssertPixels(expected, actual, Background, name);
                }
            }

            public BrandLogoDesktopFrame CaptureClientFrame(string name, Point? probe = null)
            {
                BrandLogoDesktopFrame previous = null;
                var timeline = new StringBuilder();
                var timer = Stopwatch.StartNew();
                try
                {
                    for (int index = 0; index < 60 && timer.Elapsed < TimeSpan.FromSeconds(2); index++)
                    {
                        Application.DoEvents();
                        if (!GdiFlush()) throw new Win32Exception(Marshal.GetLastWin32Error());
                        int result = DwmFlush();
                        if (result < 0) Marshal.ThrowExceptionForHR(result);
                        var current = BrandLogoDesktopFrame.Capture(PhysicalClientBounds(Form), probe);
                        timeline.AppendLine("frame=" + index + ", elapsedMs=" + timer.ElapsedMilliseconds + ", utc=" + current.CapturedUtc.ToString("O")
                            + ", bounds=" + current.Bounds + ", hash=" + current.Digest
                            + (probe.HasValue ? ", probe=" + current.ReadPhysical(probe.Value) : ""));
                        if (index == 0) Save(name + "-first-client", current.Pixels);
                        // Stability depends on equal image bytes, never the wanted color.
                        if (previous != null && previous.Bounds == current.Bounds && previous.Digest == current.Digest)
                        {
                            previous.Dispose();
                            previous = null;
                            Save(name + "-client", current.Pixels);
                            timeline.AppendLine(current.Diagnostics);
                            return current;
                        }
                        if (previous != null) previous.Dispose();
                        previous = current;
                        Thread.Sleep(20); // permit another desktop presentation, bounded to two seconds
                    }
                    throw new InvalidOperationException("Desktop image did not settle to two equal frames; " + timeline);
                }
                finally
                {
                    if (previous != null) previous.Dispose();
                    SaveDiagnostics(name + "-frames", timeline.ToString());
                }
            }

            public Control AddCalibrationPatch(Point location, Color color)
            {
                var patch = new Panel { Location = location, Size = new Size(24, 24), BackColor = color };
                page.Controls.Add(patch);
                patch.BringToFront();
                return patch;
            }

            private static void SaveDiagnostics(string name, string text)
            {
                var directory = Environment.GetEnvironmentVariable("BRAND_LOGO_SCREENSHOTS");
                if (string.IsNullOrEmpty(directory)) return;
                directory = Path.Combine(directory, "actual-paint");
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, name + ".txt"), text);
            }

            private void AssertUnobscured(Rectangle bounds)
            {
                foreach (var point in new[] { new Point(bounds.Left + 1, bounds.Top + 1),
                    new Point(bounds.Right - 2, bounds.Top + 1), new Point(bounds.Left + 1, bounds.Bottom - 2),
                    new Point(bounds.Right - 2, bounds.Bottom - 2), new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2) })
                    Assert.AreEqual(Form.Handle, GetAncestor(WindowFromPoint(point), 2),
                        "Another desktop window occludes the logo at " + point + "; not an application pixel result");
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
