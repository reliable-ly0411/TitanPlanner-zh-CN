#if !LIB
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MissionPlanner.Controls
{
    /// <summary>Windows touch input for explicitly selected inert backgrounds in one scroll viewport.</summary>
    internal sealed class SidebarTouchScroll
    {
        private const int PointerUpdate = 0x0245, PointerDown = 0x0246, PointerUp = 0x0247,
            PointerCaptureChanged = 0x024C;
        private const int Primary = 0x2000, InContact = 0x0004, Canceled = 0x8000;
        private const uint Touch = 2;

        private readonly ScrollableControl viewport;
        private readonly VerticalPan pan = new VerticalPan();
        private SurfaceWindow owningSurface;
        private uint pointer;

        private SidebarTouchScroll(ScrollableControl viewport)
        {
            this.viewport = viewport;
            // A tab switch stops this gesture. Keep owning its input sequence until up/capture loss,
            // so a drag cannot turn into a promoted mouse click after the switch.
            viewport.VisibleChanged += (sender, args) => { if (!viewport.Visible) pan.Cancel(); };
        }

        public static void Attach(ScrollableControl viewport, params Control[] safeBackgrounds)
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                return;
            var controller = new SidebarTouchScroll(viewport);
            foreach (var background in safeBackgrounds)
                new SurfaceWindow(controller, background);
        }

        private bool HandlePointer(SurfaceWindow source, int message, IntPtr wParam, IntPtr lParam)
        {
            int flags = (int)(wParam.ToInt64() >> 16);
            uint id = (uint)(wParam.ToInt64() & 0xffff);
            long coords = lParam.ToInt64();
            var point = new Point(unchecked((short)coords), unchecked((short)(coords >> 16)));

            if (message == PointerDown)
            {
                if (owningSurface != null)
                {
                    if (pointer == id && source == owningSurface) return true;
                    if (IsTouch(id)) pan.Cancel();
                    return false;
                }

                if ((flags & (Primary | InContact)) != (Primary | InContact)
                    || !CanScroll(viewport)
                    || !source.IsEmptyClientPoint(point)
                    || WindowFromPoint(new NativePoint { X = point.X, Y = point.Y }) != source.Handle
                    || !IsTouch(id))
                    return false;

                owningSurface = source;
                pointer = id;
                pan.Begin(point, Math.Max(6, SystemInformation.DragSize.Height));
                return true;
            }

            if (source != owningSurface || id != pointer) return false;
            if (message == PointerUp || message == PointerCaptureChanged)
            {
                End(source);
                return true;
            }

            if ((flags & (InContact | Canceled)) != InContact || !CanScroll(viewport))
                pan.Cancel();
            int delta = pan.Update(point);
            if (delta != 0) ScrollViewport(viewport, delta);
            return true;
        }

        private void End(SurfaceWindow source)
        {
            if (owningSurface != source) return;
            owningSurface = null;
            pan.Cancel();
        }

        internal static bool CanScroll(ScrollableControl viewport) =>
            viewport.Visible && viewport.Enabled && viewport.AutoScroll && viewport.VerticalScroll.Visible
            && viewport.VerticalScroll.Maximum >= viewport.VerticalScroll.LargeChange;

        internal static void ScrollViewport(ScrollableControl viewport, int delta)
        {
            if (!CanScroll(viewport)) return;
            var position = viewport.AutoScrollPosition;
            var scrollbar = viewport.VerticalScroll;
            int maximum = Math.Max(0, scrollbar.Maximum - scrollbar.LargeChange + 1);
            int y = Math.Max(0, Math.Min(maximum, -position.Y + delta));
            if (y != -position.Y)
                viewport.AutoScrollPosition = new Point(-position.X, y);
        }

        internal static bool IsEmptyClientPoint(Control surface, Point client)
        {
            // Include disabled children when testing the background; they are not safe targets either.
            return surface.ClientRectangle.Contains(client) &&
                   surface.GetChildAtPoint(client, GetChildAtPointSkip.Invisible) == null;
        }

        internal sealed class VerticalPan
        {
            private Point start;
            private int previousY, threshold;
            private bool active, dragging;

            public void Begin(Point point, int dragThreshold)
            {
                start = point;
                previousY = point.Y;
                threshold = Math.Max(1, dragThreshold);
                active = true;
                dragging = false;
            }

            public int Update(Point point)
            {
                if (!active) return 0;

                if (!dragging)
                {
                    int horizontal = Math.Abs(point.X - start.X);
                    int vertical = Math.Abs(point.Y - start.Y);
                    if (horizontal >= threshold && horizontal >= vertical)
                        Cancel();
                    if (!active || vertical < threshold) return 0;
                    dragging = true;
                }

                int delta = previousY - point.Y;
                previousY = point.Y;
                return delta;
            }

            public void Cancel()
            {
                active = false;
            }
        }

        private sealed class SurfaceWindow : NativeWindow
        {
            private readonly SidebarTouchScroll owner;
            private readonly Control surface;

            public SurfaceWindow(SidebarTouchScroll owner, Control surface)
            {
                this.owner = owner;
                this.surface = surface;
                surface.HandleCreated += Attach;
                surface.HandleDestroyed += Detach;
                surface.Disposed += Dispose;
                if (surface.IsHandleCreated) Attach(null, EventArgs.Empty);
            }

            private void Attach(object sender, EventArgs args) => AssignHandle(surface.Handle);

            private void Detach(object sender, EventArgs args)
            {
                owner.End(this);
                if (Handle != IntPtr.Zero) ReleaseHandle();
            }

            private void Dispose(object sender, EventArgs args)
            {
                Detach(sender, args);
                surface.HandleCreated -= Attach;
                surface.HandleDestroyed -= Detach;
                surface.Disposed -= Dispose;
            }

            public bool IsEmptyClientPoint(Point screen) =>
                SidebarTouchScroll.IsEmptyClientPoint(surface, surface.PointToClient(screen));

            protected override void WndProc(ref Message message)
            {
                int id = message.Msg;
                if ((id == PointerDown || id == PointerUpdate || id == PointerUp || id == PointerCaptureChanged)
                    && owner.HandlePointer(this, id, message.WParam, message.LParam))
                {
                    // A touch starting on a safe, empty background has no click action. Consume its
                    // full sequence so default promotion cannot click a control under the release.
                    message.Result = IntPtr.Zero;
                    return;
                }
                base.WndProc(ref message);
            }
        }

        private static bool IsTouch(uint id)
        {
            try { return GetPointerType(id, out uint type) && type == Touch; }
            catch (EntryPointNotFoundException) { return false; } // Windows versions before pointer messages
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint { public int X, Y; }

        [DllImport("user32.dll")]
        private static extern bool GetPointerType(uint pointerId, out uint pointerType);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(NativePoint point);
    }
}
#endif
