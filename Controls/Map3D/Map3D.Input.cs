using System;
using System.Drawing;
using System.Windows.Forms;
using MissionPlanner.Controls.Scene3D;
using MissionPlanner.Utilities;
using OpenTK;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using Vector3 = OpenTK.Vector3;

namespace MissionPlanner.Controls
{
    // Mouse control of the camera, the settings button, picking and ADS-B tooltips.
    public partial class Map3D
    {
        private ToolTip _adsbToolTip;
        private ToolTip _settingsToolTip;
        private adsb.PointLatLngAltHdg _hoveredAdsb;
        private bool _pressingSettings;
        private bool _isDragging;
        private int _dragStartX;
        private int _dragStartY;
        private int _mouseX;
        private int _mouseY;

        private void InitializeToolTip()
        {
            _adsbToolTip = new ToolTip
            {
                AutoPopDelay = 10000,
                InitialDelay = 0,
                ReshowDelay = 0,
                ShowAlways = true
            };
            _settingsToolTip = new ToolTip { ShowAlways = true };
        }

        /// <summary>The terrain point under the mouse.</summary>
        public PointLatLngAlt mousePosition
        {
            get { return getMousePos(_mouseX, _mouseY); }
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            MarkActive();
            if (e.Button != MouseButtons.Left)
                return;

            // A press on the settings button opens it on release and never drags the camera.
            if (_settingsButton.HitTest(e.X, e.Y))
            {
                _pressingSettings = true;
                return;
            }

            _isDragging = true;
            _dragStartX = e.X;
            _dragStartY = e.Y;
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            _isDragging = false;
            if (_pressingSettings)
            {
                _pressingSettings = false;
                if (_settingsButton.HitTest(e.X, e.Y))
                {
                    SetSettingsHover(false);
                    OpenSettings();
                }
            }
        }

        private void OnMouseLeave(object sender, EventArgs e)
        {
            SetSettingsHover(false);
        }

        private void SetSettingsHover(bool hovered)
        {
            if (!_settingsButton.SetHovered(hovered))
                return;

            Cursor = hovered ? Cursors.Hand : Cursors.Default;
            if (hovered)
                _settingsToolTip.Show("3D map settings", this, 8, 8 + SettingsButton.Size + 4);
            else
                _settingsToolTip.Hide(this);
            MarkActive();
            Invalidate();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            _mouseX = e.X;
            _mouseY = e.Y;

            if (!_isDragging)
                SetSettingsHover(_settingsButton.HitTest(e.X, e.Y));

            if (_isDragging)
            {
                MarkActive();
                int dx = e.X - _dragStartX;
                int dy = e.Y - _dragStartY;

                if (IsVehicleConnected)
                {
                    // Orbit around the vehicle and raise/lower the camera.
                    _settings.CameraAngle += dx * 0.5;
                    _settings.CameraHeight = Math.Max(-5.0, Math.Min(5.0, _settings.CameraHeight + dy * 0.005));
                }
                else
                {
                    // Free-look, reversed for a natural "grab the view" feel.
                    _freeLookYaw -= dx * 0.3;
                    _freeLookPitch = Math.Max(-89.0, Math.Min(89.0, _freeLookPitch + dy * 0.3));
                }

                _dragStartX = e.X;
                _dragStartY = e.Y;
                return;
            }

            UpdateAdsbHover(e.X, e.Y);
        }

        private void OnMouseDoubleClick(object sender, MouseEventArgs e)
        {
            MarkActive();
            if (e.Button == MouseButtons.Left && !_settingsButton.HitTest(e.X, e.Y))
                _settings.CameraAngle = 0.0;
        }

        private void OnMouseWheel(object sender, MouseEventArgs e)
        {
            if (!IsVehicleConnected)
                return;

            // 0.1 per notch; scrolling up moves closer.
            _settings.CameraDistance = Math.Max(0.1, Math.Min(10.0, _settings.CameraDistance - e.Delta / 1200.0));
        }

        private void UpdateAdsbHover(int x, int y)
        {
            AdsbOverlay.ScreenPosition hit;
            if (_adsb.HitTest(x, y, out hit))
            {
                if (!ReferenceEquals(_hoveredAdsb, hit.Plane))
                {
                    _hoveredAdsb = hit.Plane;
                    _adsbToolTip.Show(AdsbOverlay.Tooltip(hit), this, x + 15, y + 15);
                }
            }
            else if (_hoveredAdsb != null)
            {
                _hoveredAdsb = null;
                _adsbToolTip.Hide(this);
            }
        }

        /// <summary>The terrain point under a screen position, from last frame's camera.</summary>
        public PointLatLngAlt getMousePos(int x, int y)
        {
            if (!_frame.IsValid || Width <= 0 || Height <= 0)
                return PointLatLngAlt.Zero;

            var size = new Size(Width, Height);
            var end = UnProject(new Vector3(x, y, 1), _projection, _view, size);
            var target = _frame.ToLatLng(end.X, end.Y, end.Z);
            var camera = _frame.ToLatLng(_camera.X, _camera.Y, _camera.Z);
            return srtm.getIntersectionWithTerrain(camera, target);
        }

        public static Vector3 UnProject(Vector3 mouse, Matrix4 projection, Matrix4 view, Size viewport)
        {
            Vector4 vec;
            vec.X = 2.0f * mouse.X / viewport.Width - 1;
            vec.Y = -(2.0f * mouse.Y / viewport.Height - 1);
            vec.Z = mouse.Z;
            vec.W = 1.0f;
            Matrix4 viewInv = Matrix4.Invert(view);
            Matrix4 projInv = Matrix4.Invert(projection);
            Vector4.Transform(ref vec, ref projInv, out vec);
            Vector4.Transform(ref vec, ref viewInv, out vec);
            if (vec.W > 0.000001f || vec.W < -0.000001f)
            {
                vec.X /= vec.W;
                vec.Y /= vec.W;
                vec.Z /= vec.W;
            }

            return vec.Xyz;
        }
    }
}
