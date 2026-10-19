using OpenTK;

namespace MissionPlanner.Controls.Scene3D
{
    /// <summary>
    /// View frustum planes extracted from an OpenTK (row-vector) view * projection matrix.
    /// </summary>
    public sealed class Frustum
    {
        // a, b, c, d per plane; a point is inside when a*x + b*y + c*z + d >= 0 for all six.
        readonly double[] _planes = new double[24];

        public static Frustum FromViewProjection(Matrix4 m)
        {
            var f = new Frustum();
            // clip = v * m, so clip.x is v dot column 0, clip.w is v dot column 3.
            f.Set(0, m.M14 + m.M11, m.M24 + m.M21, m.M34 + m.M31, m.M44 + m.M41); // left
            f.Set(1, m.M14 - m.M11, m.M24 - m.M21, m.M34 - m.M31, m.M44 - m.M41); // right
            f.Set(2, m.M14 + m.M12, m.M24 + m.M22, m.M34 + m.M32, m.M44 + m.M42); // bottom
            f.Set(3, m.M14 - m.M12, m.M24 - m.M22, m.M34 - m.M32, m.M44 - m.M42); // top
            f.Set(4, m.M14 + m.M13, m.M24 + m.M23, m.M34 + m.M33, m.M44 + m.M43); // near
            f.Set(5, m.M14 - m.M13, m.M24 - m.M23, m.M34 - m.M33, m.M44 - m.M43); // far
            return f;
        }

        void Set(int plane, double a, double b, double c, double d)
        {
            int o = plane * 4;
            _planes[o] = a;
            _planes[o + 1] = b;
            _planes[o + 2] = c;
            _planes[o + 3] = d;
        }

        /// <summary>True unless the box is entirely outside one of the planes.</summary>
        public bool IntersectsBox(double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
        {
            for (int p = 0; p < 6; p++)
            {
                int o = p * 4;
                double a = _planes[o], b = _planes[o + 1], c = _planes[o + 2], d = _planes[o + 3];
                // The box corner furthest along the plane normal.
                double x = a >= 0 ? maxX : minX;
                double y = b >= 0 ? maxY : minY;
                double z = c >= 0 ? maxZ : minZ;
                if (a * x + b * y + c * z + d < 0)
                    return false;
            }

            return true;
        }
    }
}
