using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using MissionPlanner.Utilities;
using OpenTK;
using OpenTK.Graphics.ES20;
using Vector3 = OpenTK.Vector3;

namespace MissionPlanner.Controls.Scene3D
{
    // This window owns no vehicle, map, terrain provider or persistent state. All positions
    // are already admitted by the caller, in local east/north and EGM96 metres.
    internal sealed class LocalPlanPreviewWindow : Form
    {
        private sealed class Frame
        {
            internal readonly Vector3[] Route;
            internal readonly Vector3[] Terrain;
            internal readonly SceneBounds SceneBounds;
            internal Frame(PlanPreviewScene scene)
            {
                if (scene == null || scene.Route == null)
                    throw new ArgumentException("Missing preview geometry");
                Route = scene.Route.ToArray();
                Terrain = scene.TerrainLines == null ? new Vector3[0] : scene.TerrainLines.ToArray();
                if ((Terrain.Length & 1) != 0 || Route.Any(Invalid) || Terrain.Any(Invalid))
                    throw new ArgumentException("Invalid preview geometry");
                SceneBounds = BoundsFor(this);
            }
            private static bool Invalid(Vector3 p)
            {
                return float.IsNaN(p.X) || float.IsInfinity(p.X) || float.IsNaN(p.Y) ||
                    float.IsInfinity(p.Y) || float.IsNaN(p.Z) || float.IsInfinity(p.Z);
            }
        }

        private readonly Label _status;
        private readonly Plan2D _plan;
        private readonly PreviewGL _gl;
        private readonly Label _cover;
        private Frame _visible;
        private Frame _pending;
        private long _revision;
        private bool _closed;
        protected override bool ShowWithoutActivation => true;

        internal event EventHandler ChooseElevationSource;
        internal Func<bool> CanPublishSource { get; set; }

        internal LocalPlanPreviewWindow()
        {
            Text = UiText.Translate("Local plan · Read-only");
            Width = 1050; Height = 650; MinimumSize = new Size(560, 360);
            var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 42, WrapContents = false };
            var choose = new Button { AutoSize = true, Text = UiText.Translate("Choose elevation source") };
            choose.Click += (s, e) => { var handler = ChooseElevationSource; if (handler != null) handler(this, EventArgs.Empty); };
            _status = new Label { AutoSize = true, Padding = new Padding(8, 8, 0, 0), Text = UiText.Translate("No local plan preview") };
            toolbar.Controls.Add(choose); toolbar.Controls.Add(_status);
            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, Width = 1000, SplitterDistance = 500 };
            _plan = new Plan2D(() => _visible) { Dock = DockStyle.Fill };
            _gl = new PreviewGL(() => _visible, () => _pending, Ready, Failed) { Dock = DockStyle.Fill };
            split.Panel1.Controls.Add(_plan);
            var glHost = new Panel { Dock = DockStyle.Fill };
            _cover = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(20, 26, 32), ForeColor = Color.White,
                Text = UiText.Translate("No local plan preview") };
            glHost.Controls.Add(_gl); glHost.Controls.Add(_cover); _cover.BringToFront();
            split.Panel2.Controls.Add(glHost);
            split.Panel1.Controls.Add(PaneLabel("2D · Local plan · Read-only"));
            split.Panel2.Controls.Add(PaneLabel("3D · Local plan · Read-only"));
            Controls.Add(split); Controls.Add(toolbar);
            _plan.VisibleChanged += PaneChanged; _plan.SizeChanged += PaneChanged;
            _gl.VisibleChanged += PaneChanged; _gl.SizeChanged += PaneChanged;
            VisibleChanged += PaneChanged; Resize += PaneChanged;
        }

        private bool PanesReady()
        {
            return !_closed && !IsDisposed && Visible && WindowState != FormWindowState.Minimized &&
                _plan.Visible && _plan.IsHandleCreated && _plan.ClientSize.Width > 0 && _plan.ClientSize.Height > 0 &&
                _gl.Visible && _gl.IsHandleCreated && _gl.ClientSize.Width > 0 && _gl.ClientSize.Height > 0;
        }

        private void PaneChanged(object sender, EventArgs e)
        {
            if (!_closed && (_visible != null || _pending != null) && !PanesReady())
                Clear(UiText.Translate("Local preview unavailable"));
        }

        private void Cover(string reason)
        {
            _cover.Text = reason;
            _cover.Visible = true;
            _cover.BringToFront();
        }

        private static Label PaneLabel(string text)
        {
            return new Label { Text = UiText.Translate(text), Dock = DockStyle.Top, Height = 26,
                TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.FromArgb(36, 43, 52), ForeColor = Color.White };
        }

        // Must be called on the form's UI thread. Both views consume only _visible.
        internal void Replace(PlanPreviewScene scene)
        {
            RequireUI();
            if (_closed) return;
            if (!PanesReady()) { Clear(UiText.Translate("Local preview unavailable")); return; }
            Frame next;
            try { next = new Frame(scene); }
            catch (Exception) { Clear(UiText.Translate("Invalid local preview geometry")); return; }
            _pending = next;
            long revision = ++_revision;
            _status.Text = UiText.Translate("Preparing local preview");
            _gl.RequestReady(revision);
        }

        internal void Clear(string reason)
        {
            RequireUI();
            if (_closed) return;
            ++_revision; _pending = null; _visible = null;
            _gl.CancelReady();
            _status.Text = String.IsNullOrWhiteSpace(reason) ? UiText.Translate("No local plan preview") : reason;
            Cover(_status.Text);
            // Clear the currently presented GL buffer even if the opaque cover
            // suppresses its next WM_PAINT. The cover stays if the context fails.
            if (_gl.IsHandleCreated && _gl.Visible)
                _gl.PresentCurrent();
            _plan.Invalidate(); _gl.Invalidate();
        }

        private void Ready(long revision)
        {
            if (_closed || IsDisposed || !IsHandleCreated) return;
            // Never publish from the GL paint callback. A later request, handle loss or
            // close makes this queued acknowledgement harmless.
            try
            {
                BeginInvoke((Action)(() =>
                {
                    if (_closed || IsDisposed || revision != _revision || _pending == null) return;
                    if (!PanesReady() || !_gl.CanPublish(revision))
                    {
                        Clear(UiText.Translate("Local preview unavailable"));
                        return;
                    }
                    bool sourceValid;
                    try { sourceValid = CanPublishSource != null && CanPublishSource(); }
                    catch (Exception ex) { System.Diagnostics.Trace.WriteLine("Local preview source check: " + ex); sourceValid = false; }
                    if (!sourceValid) { Clear(UiText.Translate("The plan changed. Press Ctrl+M to refresh.")); return; }
                    _visible = _pending; _pending = null;
                    // While covered, render the committed frame before uncovering. A GL
                    // failure clears the shared state and keeps the cover in place.
                    if (!_gl.PresentCurrent()) return;
                    _status.Text = UiText.Translate("Local plan · Read-only");
                    _cover.Visible = false;
                    _plan.Invalidate(); _gl.Invalidate();
                }));
            }
            catch (InvalidOperationException) { }
        }

        private void Failed()
        {
            if (_closed || IsDisposed) return;
            Clear(UiText.Translate("Local preview unavailable"));
        }

        private void RequireUI()
        {
            if (InvokeRequired) throw new InvalidOperationException("Preview must be updated on the UI thread");
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _closed = true; ++_revision; _pending = null; _visible = null;
            _gl.Shutdown();
            base.OnFormClosed(e);
        }

        private sealed class Plan2D : Control
        {
            private readonly Func<Frame> _get;
            internal Plan2D(Func<Frame> get) { _get = get; DoubleBuffered = true; BackColor = Color.FromArgb(20, 26, 32); }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                Frame f = _get(); if (f == null || f.Route.Length == 0) return;
                var b = f.SceneBounds; float sx = Math.Max(1, Width - 40) / b.SpanX, sy = Math.Max(1, Height - 40) / b.SpanY;
                float scale = Math.Min(sx, sy);
                Func<Vector3, PointF> point = p => new PointF(Width / 2f + (p.X - b.X) * scale, Height / 2f - (p.Y - b.Y) * scale);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var terrain = new Pen(Color.FromArgb(90, 145, 135, 100), 1))
                    for (int i = 0; i + 1 < f.Terrain.Length; i += 2) e.Graphics.DrawLine(terrain, point(f.Terrain[i]), point(f.Terrain[i + 1]));
                using (var route = new Pen(Color.DeepSkyBlue, 2))
                {
                    for (int i = 1; i < f.Route.Length; i++) e.Graphics.DrawLine(route, point(f.Route[i - 1]), point(f.Route[i]));
                    foreach (var p in f.Route) { var q = point(p); e.Graphics.FillEllipse(Brushes.DeepSkyBlue, q.X - 3, q.Y - 3, 6, 6); }
                }
            }
        }

        private struct SceneBounds
        {
            internal float X, Y, Z, SpanX, SpanY, SpanZ;
        }
        private static SceneBounds BoundsFor(Frame f)
        {
            var points = f.Route.Concat(f.Terrain).ToArray();
            if (points.Length == 0) return new SceneBounds { SpanX = 1, SpanY = 1, SpanZ = 1 };
            float minX = points.Min(p => p.X), maxX = points.Max(p => p.X), minY = points.Min(p => p.Y), maxY = points.Max(p => p.Y);
            float minZ = points.Min(p => p.Z), maxZ = points.Max(p => p.Z);
            return new SceneBounds { X = minX / 2 + maxX / 2, Y = minY / 2 + maxY / 2, Z = minZ / 2 + maxZ / 2,
                SpanX = Math.Max(1, maxX - minX), SpanY = Math.Max(1, maxY - minY), SpanZ = Math.Max(1, maxZ - minZ) };
        }

        private sealed class PreviewGL : GLControl
        {
            private readonly Func<Frame> _get;
            private readonly Func<Frame> _getPending;
            private readonly Action<long> _ready;
            private readonly Action _failed;
            private LineRenderer _lines;
            private readonly VertexList _terrainVertices = new VertexList();
            private readonly VertexList _routeVertices = new VertexList();
            private readonly VertexList _markerVertices = new VertexList();
            private long _wanted, _swapped;
            private bool _dead, _failedState;
            internal PreviewGL(Func<Frame> get, Func<Frame> getPending, Action<long> ready, Action failed) { _get = get; _getPending = getPending; _ready = ready; _failed = failed; }
            internal void RequestReady(long revision)
            {
                if (_dead) return;
                _failedState = false; _wanted = revision; _swapped = 0;
                // An opaque failure cover may suppress WM_PAINT. Stage directly on the
                // UI queue so a fresh explicit request can recover behind that cover.
                try { BeginInvoke((Action)(() => { if (!_dead && _wanted == revision) PaintFrame(); })); }
                catch (InvalidOperationException ex) { Fail(ex); }
            }
            internal void CancelReady() { _wanted = 0; _swapped = 0; }
            internal bool CanPublish(long revision) { return !_dead && !_failedState && IsHandleCreated && _wanted == revision && _swapped == revision; }
            internal void Shutdown() { _dead = true; CancelReady(); Release(); }
            protected override void OnPaint(PaintEventArgs e)
            {
                if (_dead || _failedState) return;
                try { base.OnPaint(e); }
                catch (Exception ex) { Fail(ex); return; }
                PaintFrame();
            }
            internal bool PresentCurrent()
            {
                PaintFrame();
                return !_dead && !_failedState;
            }
            private void PaintFrame()
            {
                if (_dead || _failedState || !IsHandleCreated || Width <= 0 || Height <= 0) return;
                try
                {
                    if (Context == null) throw new InvalidOperationException("No OpenGL preview context");
                    MakeCurrent();
                    if (String.IsNullOrEmpty(GL.GetString(StringName.Version)))
                        throw new InvalidOperationException("No valid OpenGL preview context");
                    if (_lines == null) _lines = new LineRenderer();
                    GL.Viewport(0, 0, Width, Height);
                    GL.ClearColor(0.08f, 0.10f, 0.13f, 1f);
                    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                    // Exercise lazy shader creation and the entire pending geometry while it is
                    // still hidden in the backbuffer. Clear it before showing the previous
                    // committed frame, so staging can never leak into only the GL pane.
                    if (_wanted != 0 && _swapped != _wanted)
                    {
                        Draw(_getPending());
                        var error = GL.GetError();
                        if (error != ErrorCode.NoError) throw new InvalidOperationException("Preview staging GL error: " + error);
                        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                    }
                    Draw(_get());
                    var committedError = GL.GetError();
                    if (committedError != ErrorCode.NoError) throw new InvalidOperationException("Preview draw GL error: " + committedError);
                    SwapBuffers();
                    if (_wanted != 0 && _swapped != _wanted) { _swapped = _wanted; _ready(_wanted); }
                }
                catch (Exception ex) { Fail(ex); }
            }
            private void Draw(Frame f)
            {
                if (f == null || f.Route.Length == 0) return;
                var b = f.SceneBounds;
                float radius = Math.Max(1, Math.Max(b.SpanX, Math.Max(b.SpanY, b.SpanZ)));
                var projection = Matrix4.CreatePerspectiveFieldOfView((float)(Math.PI / 4), Math.Max(0.01f, Width / (float)Math.Max(1, Height)), radius / 1000f, radius * 20f);
                // Recenter before projection to retain precision with large absolute elevations.
                var view = Matrix4.LookAt(radius * 1.5f, -radius * 1.8f, radius * 1.4f, 0, 0, 0, 0, 0, 1);
                var terrain = _terrainVertices; terrain.Clear();
                foreach (var p in f.Terrain) terrain.AddColored(p.X - b.X, p.Y - b.Y, p.Z - b.Z, .48f, .44f, .30f, 1);
                _lines.Draw(terrain, PrimitiveType.Lines, 1, ref projection, ref view);
                var route = _routeVertices; route.Clear();
                foreach (var p in f.Route) route.AddColored(p.X - b.X, p.Y - b.Y, p.Z - b.Z, 0, .75f, 1, 1);
                _lines.Draw(route, PrimitiveType.LineStrip, 2, ref projection, ref view);
                // One-point plans remain visible without inventing a route segment.
                var markers = _markerVertices; markers.Clear(); float tick = radius * .012f;
                foreach (var p in f.Route)
                {
                    markers.AddColored(p.X - b.X - tick, p.Y - b.Y, p.Z - b.Z, 0, .75f, 1, 1);
                    markers.AddColored(p.X - b.X + tick, p.Y - b.Y, p.Z - b.Z, 0, .75f, 1, 1);
                    markers.AddColored(p.X - b.X, p.Y - b.Y - tick, p.Z - b.Z, 0, .75f, 1, 1);
                    markers.AddColored(p.X - b.X, p.Y - b.Y + tick, p.Z - b.Z, 0, .75f, 1, 1);
                }
                _lines.Draw(markers, PrimitiveType.Lines, 2, ref projection, ref view);
            }
            private void Fail(Exception error)
            {
                if (_dead || _failedState) return;
                System.Diagnostics.Trace.WriteLine("Local preview OpenGL failure: " + error);
                _failedState = true; CancelReady(); Release(); _failed();
            }
            protected override void OnHandleCreated(EventArgs e)
            {
                try { base.OnHandleCreated(e); }
                catch (Exception ex) { Fail(ex); }
            }
            private void Release()
            {
                var lines = _lines; _lines = null;
                if (lines == null) return;
                try { if (IsHandleCreated && Context != null) { MakeCurrent(); lines.Dispose(); } } catch (Exception) { }
            }
            protected override void OnHandleDestroyed(EventArgs e)
            {
                Release();
                if (!_dead) Fail(new InvalidOperationException("OpenGL preview handle was destroyed"));
                base.OnHandleDestroyed(e);
            }
            protected override void Dispose(bool disposing)
            {
                if (disposing) Shutdown();
                base.Dispose(disposing);
            }
        }
    }
}
