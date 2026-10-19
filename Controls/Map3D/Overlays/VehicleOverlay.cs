using System;
using System.Collections.Generic;
using System.Drawing;
using MissionPlanner.Utilities;
using OpenTK;
using OpenTK.Graphics.ES20;

namespace MissionPlanner.Controls.Scene3D
{
    /// <summary>
    /// The vehicle model and everything drawn from it: heading, navigation bearing, GPS course, predicted
    /// turn, and the flown trail.
    /// </summary>
    public sealed class VehicleOverlay : IDisposable
    {
        const double LineLength = 100;
        const double TurnArcLength = 200;
        const int TurnSegments = 50;

        /// <summary>Frames of non-zero altitude to wait for before recording the trail, so it never starts at 0 m.</summary>
        const int TrailStabilityFrames = 30;

        readonly VertexList _vertices = new VertexList();
        readonly List<double[]> _trail = new List<double[]>();
        readonly ModelRenderer _model;
        int _trailZoneVersion = -1;
        int _trailStableFrames;

        public VehicleOverlay(STLModelLoader loader)
        {
            _model = new ModelRenderer(loader);
        }

        /// <summary>Records the trail in absolute UTM while armed; call once per frame.</summary>
        public void RecordTrail(WorldFrame frame, VehicleTracker vehicle, bool enabled, bool armed)
        {
            if (!enabled || !armed)
            {
                _trailStableFrames = 0;
                return;
            }

            if (vehicle.Z < 0.5)
            {
                _trailStableFrames = 0;
                return;
            }

            if (++_trailStableFrames < TrailStabilityFrames)
                return;

            if (_trailZoneVersion != frame.ZoneVersion)
            {
                _trail.Clear();
                _trailZoneVersion = frame.ZoneVersion;
            }

            int maxPoints = Settings.Instance.GetInt32("NUM_tracklength", 200) * 15;
            if (_trail.Count > maxPoints)
                _trail.RemoveRange(0, _trail.Count - maxPoints);
            _trail.Add(new[] { vehicle.X + frame.OriginEasting, vehicle.Y + frame.OriginNorthing, vehicle.Z });
        }

        public void DrawIndicators(LineRenderer lines, Map3DSettings settings, VehicleTracker vehicle,
            PointLatLngAlt navTarget, double navTargetAltitude, WorldFrame frame, ref Matrix4 projection, ref Matrix4 view)
        {
            var cs = MainV2.comPort?.MAV?.cs;
            double pitch = vehicle.Pitch * Math.PI / 180;
            GL.Enable(EnableCap.DepthTest);

            if (settings.ShowHeadingLine)
            {
                _vertices.Clear();
                AddDirection(vehicle, vehicle.Yaw, pitch, 1, 0, 0);
                lines.Draw(_vertices, PrimitiveType.Lines, 1.5f, ref projection, ref view);
            }

            if (settings.ShowNavBearingLine)
            {
                _vertices.Clear();
                if (navTarget != null && navTarget.Lat != 0 && navTarget.Lng != 0)
                {
                    double x, y;
                    frame.ToLocal(navTarget.Lat, navTarget.Lng, out x, out y);
                    _vertices.AddColored(vehicle.X, vehicle.Y, vehicle.Z, 1, 0.5f, 0, 1);
                    _vertices.AddColored(x, y, navTargetAltitude, 1, 0.5f, 0, 1);
                }
                else
                {
                    AddDirection(vehicle, vehicle.NavBearing, pitch, 1, 0.5f, 0);
                }

                lines.Draw(_vertices, PrimitiveType.Lines, 1.5f, ref projection, ref view);
            }

            if (settings.ShowGpsHeadingLine)
            {
                _vertices.Clear();
                AddDirection(vehicle, vehicle.GpsHeading, 0, 0, 0, 0);
                lines.Draw(_vertices, PrimitiveType.Lines, 1.5f, ref projection, ref view);
            }

            double radius = cs?.radius ?? 0;
            if (settings.ShowTurnRadius && Math.Abs(radius) > 1)
            {
                _vertices.Clear();
                AddTurnArc(vehicle, radius, pitch);
                lines.Draw(_vertices, PrimitiveType.LineStrip, 2f, ref projection, ref view);
            }
        }

        /// <summary>A line from the vehicle along a bearing (degrees), climbing with pitch (radians).</summary>
        void AddDirection(VehicleTracker vehicle, double bearing, double pitch, float r, float g, float b)
        {
            double bearingRad = bearing * Math.PI / 180;
            double horizontal = LineLength * Math.Cos(pitch);
            _vertices.AddColored(vehicle.X, vehicle.Y, vehicle.Z, r, g, b, 1);
            _vertices.AddColored(vehicle.X + Math.Sin(bearingRad) * horizontal, vehicle.Y + Math.Cos(bearingRad) * horizontal,
                vehicle.Z + LineLength * Math.Sin(pitch), r, g, b, 1);
        }

        /// <summary>The path the vehicle will fly at its current turn radius (negative turns left).</summary>
        void AddTurnArc(VehicleTracker vehicle, double radius, double pitch)
        {
            const float r = 1.0f, g = 105f / 255f, b = 180f / 255f;
            double sweep = Math.Min(Math.PI, TurnArcLength / Math.Abs(radius));
            double course = vehicle.GpsHeading * Math.PI / 180;
            double perpendicular = course + (radius > 0 ? Math.PI / 2 : -Math.PI / 2);
            double horizontalRadius = Math.Abs(radius) * Math.Cos(pitch);
            double centerX = vehicle.X + Math.Sin(perpendicular) * horizontalRadius;
            double centerY = vehicle.Y + Math.Cos(perpendicular) * horizontalRadius;
            double start = Math.Atan2(vehicle.X - centerX, vehicle.Y - centerY);
            double step = sweep / TurnSegments;
            double direction = radius > 0 ? 1 : -1;
            double climbPerSegment = Math.Abs(radius) * step * Math.Sin(pitch);

            _vertices.AddColored(vehicle.X, vehicle.Y, vehicle.Z, r, g, b, 1);
            for (int i = 1; i <= TurnSegments; i++)
            {
                double angle = start + direction * step * i;
                _vertices.AddColored(centerX + Math.Sin(angle) * horizontalRadius, centerY + Math.Cos(angle) * horizontalRadius,
                    vehicle.Z + climbPerSegment * i, r, g, b, 1);
            }
        }

        public void DrawTrail(LineRenderer lines, WorldFrame frame, VehicleTracker vehicle, bool armed,
            ref Matrix4 projection, ref Matrix4 view)
        {
            if (!armed || _trail.Count < 2 || _trailZoneVersion != frame.ZoneVersion)
                return;

            // MidnightBlue, as the 2D map's track.
            const float r = 25f / 255f, g = 25f / 255f, b = 112f / 255f, a = 144f / 255f;

            _vertices.Clear();
            SmoothInto(_trail, 20, 60, frame, (x, y, z) => _vertices.AddColored(x, y, z, r, g, b, a));
            _vertices.AddColored(vehicle.X, vehicle.Y, vehicle.Z, r, g, b, a);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            lines.Draw(_vertices, PrimitiveType.LineStrip, 5f, ref projection, ref view);
            GL.Disable(EnableCap.Blend);
        }

        /// <summary>
        /// Centred moving average (wider for altitude, which is noisier), computed with running sums.
        /// </summary>
        static void SmoothInto(List<double[]> points, int windowXY, int windowZ, WorldFrame frame, Action<double, double, double> emit)
        {
            int n = points.Count;
            var sumX = new double[n + 1];
            var sumY = new double[n + 1];
            var sumZ = new double[n + 1];
            for (int i = 0; i < n; i++)
            {
                sumX[i + 1] = sumX[i] + (points[i][0] - frame.OriginEasting);
                sumY[i + 1] = sumY[i] + (points[i][1] - frame.OriginNorthing);
                sumZ[i + 1] = sumZ[i] + points[i][2];
            }

            bool smooth = n >= Math.Max(windowXY, windowZ);
            for (int i = 0; i < n; i++)
            {
                if (!smooth)
                {
                    emit(points[i][0] - frame.OriginEasting, points[i][1] - frame.OriginNorthing, points[i][2]);
                    continue;
                }

                int lo = Math.Max(0, i - windowXY / 2), hi = Math.Min(n - 1, i + windowXY / 2);
                int loZ = Math.Max(0, i - windowZ / 2), hiZ = Math.Min(n - 1, i + windowZ / 2);
                emit((sumX[hi + 1] - sumX[lo]) / (hi - lo + 1),
                    (sumY[hi + 1] - sumY[lo]) / (hi - lo + 1),
                    (sumZ[hiZ + 1] - sumZ[loZ]) / (hiZ - loZ + 1));
            }
        }

        public void DrawModel(VehicleTracker vehicle, float scale, Color color, ref Matrix4 projection, ref Matrix4 view)
        {
            // The STL is in millimetres.
            var model = Matrix4.CreateScale(scale / 1000f);
            model = Matrix4.Mult(model, Matrix4.CreateRotationY(vehicle.Roll * (float)Math.PI / 180));
            model = Matrix4.Mult(model, Matrix4.CreateRotationX(vehicle.Pitch * (float)Math.PI / 180));
            model = Matrix4.Mult(model, Matrix4.CreateRotationZ(-vehicle.Yaw * (float)Math.PI / 180));
            model = Matrix4.Mult(model, Matrix4.CreateTranslation((float)vehicle.X, (float)vehicle.Y, (float)vehicle.Z));
            _model.Draw(ref projection, ref view, model, color);
        }

        public void Dispose()
        {
            _model.Dispose();
        }
    }
}
