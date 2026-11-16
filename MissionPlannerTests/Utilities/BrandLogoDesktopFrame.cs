using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace MissionPlannerTests.Utilities
{
    /// <summary>One immutable physical desktop read, shared by PNGs, crops and assertions.</summary>
    internal sealed class BrandLogoDesktopFrame : IDisposable
    {
        [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr GetDC(IntPtr window);
        [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr window, IntPtr dc);
        [DllImport("gdi32.dll", SetLastError = true)] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll", SetLastError = true)] private static extern IntPtr CreateCompatibleBitmap(IntPtr dc, int width, int height);
        [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc, IntPtr value);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr value);
        [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr dc);
        [DllImport("gdi32.dll", SetLastError = true)] private static extern bool BitBlt(IntPtr target, int x, int y, int width, int height, IntPtr source, int sourceX, int sourceY, uint operation);
        [DllImport("gdi32.dll")] private static extern int GetDeviceCaps(IntPtr dc, int index);
        [DllImport("gdi32.dll")] private static extern int GetMapMode(IntPtr dc);
        [DllImport("gdi32.dll")] private static extern bool GetViewportOrgEx(IntPtr dc, out Point point);
        [DllImport("gdi32.dll")] private static extern bool GetWindowOrgEx(IntPtr dc, out Point point);
        [DllImport("gdi32.dll")] private static extern bool LPtoDP(IntPtr dc, ref Point point, int count);
        [DllImport("gdi32.dll")] private static extern uint GetPixel(IntPtr dc, int x, int y);
        [DllImport("gdi32.dll")] private static extern bool GdiFlush();

        public readonly Rectangle Bounds;
        public readonly Bitmap Pixels;
        public readonly string Digest;
        public readonly string Diagnostics;
        public readonly DateTime CapturedUtc;

        private BrandLogoDesktopFrame(Rectangle bounds, Bitmap pixels, string diagnostics)
        {
            Bounds = bounds;
            Pixels = pixels;
            Diagnostics = diagnostics;
            CapturedUtc = DateTime.UtcNow;
            using (var hash = SHA256.Create())
            {
                var data = pixels.LockBits(new Rectangle(Point.Empty, pixels.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
                try
                {
                    var bytes = new byte[Math.Abs(data.Stride) * pixels.Height];
                    for (int y = 0; y < pixels.Height; y++)
                        Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), bytes, y * Math.Abs(data.Stride), Math.Abs(data.Stride));
                    for (int index = 3; index < bytes.Length; index += 4) bytes[index] = 0; // unused RGB32 alpha
                    Digest = BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "");
                }
                finally { pixels.UnlockBits(data); }
            }
        }

        public static BrandLogoDesktopFrame Capture(Rectangle physicalBounds, Point? probe = null)
        {
            if (physicalBounds.Width <= 0 || physicalBounds.Height <= 0) throw new ArgumentOutOfRangeException("physicalBounds");
            IntPtr screen = GetDC(IntPtr.Zero), memory = IntPtr.Zero, bitmap = IntPtr.Zero, previous = IntPtr.Zero;
            if (screen == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
            try
            {
                var text = new StringBuilder();
                Point viewport, window;
                GetViewportOrgEx(screen, out viewport);
                GetWindowOrgEx(screen, out window);
                var mapped = physicalBounds.Location;
                if (!LPtoDP(screen, ref mapped, 1)) throw new Win32Exception(Marshal.GetLastWin32Error());
                var mappedEnd = new Point(physicalBounds.Right, physicalBounds.Bottom);
                if (!LPtoDP(screen, ref mappedEnd, 1)) throw new Win32Exception(Marshal.GetLastWin32Error());
                text.AppendLine("screenDC mapMode=" + GetMapMode(screen) + ", viewportOrigin=" + viewport + ", windowOrigin=" + window
                    + ", LPtoDP(origin)=" + mapped + ", sourceOrigin=" + physicalBounds.Location + ", LPtoDP(end)=" + mappedEnd);
                text.AppendLine("screenDC HORZRES=" + GetDeviceCaps(screen, 8) + ", VERTRES=" + GetDeviceCaps(screen, 10)
                    + ", DESKTOPHORZRES=" + GetDeviceCaps(screen, 118) + ", DESKTOPVERTRES=" + GetDeviceCaps(screen, 117)
                    + ", LOGPIXELS=" + GetDeviceCaps(screen, 88) + "x" + GetDeviceCaps(screen, 90));
                if (GetMapMode(screen) != 1 || mapped != physicalBounds.Location || mappedEnd != new Point(physicalBounds.Right, physicalBounds.Bottom) || viewport != Point.Empty || window != Point.Empty)
                    throw new InvalidOperationException("Desktop DC does not map physical pixel coordinates directly: " + text);
                if (probe.HasValue) text.AppendLine("native GetPixel before=" + ColorRef(GetPixel(screen, probe.Value.X, probe.Value.Y)));
                memory = CreateCompatibleDC(screen);
                bitmap = CreateCompatibleBitmap(screen, physicalBounds.Width, physicalBounds.Height);
                if (memory == IntPtr.Zero || bitmap == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
                previous = SelectObject(memory, bitmap);
                if (!BitBlt(memory, 0, 0, physicalBounds.Width, physicalBounds.Height, screen,
                    physicalBounds.Left, physicalBounds.Top, 0x00CC0020u | 0x40000000u))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                if (!GdiFlush()) throw new Win32Exception(Marshal.GetLastWin32Error());
                var pixels = Bitmap.FromHbitmap(bitmap);
                if (pixels.Size != physicalBounds.Size)
                {
                    pixels.Dispose();
                    throw new InvalidOperationException("Native desktop bitmap changed capture dimensions");
                }
                if (probe.HasValue) text.AppendLine("native GetPixel after=" + ColorRef(GetPixel(screen, probe.Value.X, probe.Value.Y)));
                text.AppendLine("frame bounds=" + physicalBounds + ", bitmap size=" + pixels.Size + ", bitmapDpi=" + pixels.HorizontalResolution + "x" + pixels.VerticalResolution);
                return new BrandLogoDesktopFrame(physicalBounds, pixels, text.ToString());
            }
            finally
            {
                if (previous != IntPtr.Zero) SelectObject(memory, previous);
                if (bitmap != IntPtr.Zero) DeleteObject(bitmap);
                if (memory != IntPtr.Zero) DeleteDC(memory);
                ReleaseDC(IntPtr.Zero, screen);
            }
        }

        private static string ColorRef(uint value)
        {
            return value == 0xFFFFFFFFu ? "CLR_INVALID" : "#" + (value & 255).ToString("X2")
                + ((value >> 8) & 255).ToString("X2") + ((value >> 16) & 255).ToString("X2");
        }

        public Color ReadPhysical(Point point)
        {
            if (!Bounds.Contains(point)) throw new ArgumentOutOfRangeException("point", "Physical point lies outside this frame");
            return Pixels.GetPixel(point.X - Bounds.Left, point.Y - Bounds.Top);
        }

        public Bitmap CropPhysical(Rectangle rectangle)
        {
            if (!Bounds.Contains(rectangle)) throw new ArgumentOutOfRangeException("rectangle", "Physical crop lies outside this frame");
            rectangle.Offset(-Bounds.Left, -Bounds.Top);
            return Pixels.Clone(rectangle, PixelFormat.Format32bppRgb);
        }

        public void Dispose() { Pixels.Dispose(); }
    }
}
