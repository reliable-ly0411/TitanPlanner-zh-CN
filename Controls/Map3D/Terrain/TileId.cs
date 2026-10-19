using System;

namespace MissionPlanner.Controls.Scene3D
{
    /// <summary>
    /// A Web Mercator (Google/XYZ scheme) tile address. Y grows southwards.
    /// </summary>
    public struct TileId : IEquatable<TileId>
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        public TileId(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public TileId Parent
        {
            get { return new TileId(X >> 1, Y >> 1, Z - 1); }
        }

        /// <summary>Child 0..3: bit 0 picks east, bit 1 picks south.</summary>
        public TileId Child(int index)
        {
            return new TileId(X * 2 + (index & 1), Y * 2 + (index >> 1), Z + 1);
        }

        /// <summary>The ancestor at a lower zoom level.</summary>
        public TileId AncestorAt(int zoom)
        {
            int shift = Z - zoom;
            return new TileId(X >> shift, Y >> shift, zoom);
        }

        public bool Equals(TileId other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public override bool Equals(object obj)
        {
            return obj is TileId && Equals((TileId)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((X * 397) ^ Y) * 31 + Z;
            }
        }

        public static bool operator ==(TileId a, TileId b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(TileId a, TileId b)
        {
            return !a.Equals(b);
        }

        public override string ToString()
        {
            return Z + "/" + X + "/" + Y;
        }
    }

    /// <summary>
    /// Spherical Web Mercator maths, matching GMap.NET's MercatorProjection.
    /// </summary>
    public static class TileMath
    {
        public const int TileSize = 256;
        public const double EarthRadius = 6378137.0;
        public const double MaxLatitude = 85.05112878;

        public static int TileCount(int zoom)
        {
            return 1 << zoom;
        }

        /// <summary>Longitude of a (possibly fractional) tile X coordinate.</summary>
        public static double TileXToLng(double x, int zoom)
        {
            return x / TileCount(zoom) * 360.0 - 180.0;
        }

        /// <summary>Latitude of a (possibly fractional) tile Y coordinate.</summary>
        public static double TileYToLat(double y, int zoom)
        {
            double n = Math.PI - 2.0 * Math.PI * y / TileCount(zoom);
            return 180.0 / Math.PI * Math.Atan(Math.Sinh(n));
        }

        /// <summary>Fractional tile coordinates of a position.</summary>
        public static void LatLngToTile(double lat, double lng, int zoom, out double x, out double y)
        {
            lat = Math.Max(-MaxLatitude, Math.Min(MaxLatitude, lat));
            double count = TileCount(zoom);
            x = (lng + 180.0) / 360.0 * count;
            double latRad = lat * Math.PI / 180.0;
            y = (1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI) / 2.0 * count;
        }

        /// <summary>Ground size of a tile edge in metres at the given latitude.</summary>
        public static double TileSizeMeters(double lat, int zoom)
        {
            return 2 * Math.PI * EarthRadius * Math.Cos(lat * Math.PI / 180.0) / TileCount(zoom);
        }

        public static double CenterLat(TileId id)
        {
            return TileYToLat(id.Y + 0.5, id.Z);
        }

        public static double CenterLng(TileId id)
        {
            return TileXToLng(id.X + 0.5, id.Z);
        }

        public static bool IsValid(TileId id)
        {
            int count = TileCount(id.Z);
            return id.Z >= 0 && id.X >= 0 && id.Y >= 0 && id.X < count && id.Y < count;
        }
    }
}
