using System;

namespace MissionPlanner.Controls.Scene3D
{
    /// <summary>
    /// WGS84 transverse Mercator (UTM) using the Krueger series. Pure math and thread-safe, so terrain
    /// built on worker threads lines up with overlays built on the render thread. Accurate to well under
    /// a millimetre inside a zone and still usable a few hundred kilometres outside it.
    /// </summary>
    public static class UtmProjection
    {
        const double SemiMajorAxis = 6378137.0;
        const double Flattening = 1 / 298.257223563;
        const double ScaleFactor = 0.9996;
        const double FalseEasting = 500000.0;
        const double FalseNorthingSouth = 10000000.0;
        const double DegToRad = Math.PI / 180.0;

        static readonly double N = Flattening / (2 - Flattening);
        static readonly double RectifyingRadius = SemiMajorAxis / (1 + N) * (1 + N * N / 4 + N * N * N * N / 64);
        static readonly double ConformalFactor = 2 * Math.Sqrt(N) / (1 + N);

        static readonly double[] Alpha =
        {
            N / 2 - 2.0 / 3 * N * N + 5.0 / 16 * N * N * N,
            13.0 / 48 * N * N - 3.0 / 5 * N * N * N,
            61.0 / 240 * N * N * N
        };

        static readonly double[] Beta =
        {
            N / 2 - 2.0 / 3 * N * N + 37.0 / 96 * N * N * N,
            1.0 / 48 * N * N + 1.0 / 15 * N * N * N,
            17.0 / 480 * N * N * N
        };

        static readonly double[] Delta =
        {
            2 * N - 2.0 / 3 * N * N - 2 * N * N * N,
            7.0 / 3 * N * N - 8.0 / 5 * N * N * N,
            56.0 / 15 * N * N * N
        };

        /// <summary>
        /// Zone for a position, using the same convention as PointLatLngAlt.GetUTMZone:
        /// 1..60, negative for the southern hemisphere.
        /// </summary>
        public static int ZoneFor(double lat, double lng)
        {
            int zone = (int)((lng + 186.0) / 6.0);
            if (zone < 1) zone = 1;
            if (zone > 60) zone = 60;
            return lat < 0 ? -zone : zone;
        }

        static double CentralMeridian(int zone)
        {
            return CentralMeridianDegrees(zone) * DegToRad;
        }

        public static double CentralMeridianDegrees(int zone)
        {
            return Math.Abs(zone) * 6 - 183;
        }

        /// <summary>Degrees of longitude between a position and a zone's central meridian, across the antimeridian too.</summary>
        public static double LongitudeFromCentralMeridian(double lng, int zone)
        {
            double delta = (lng - CentralMeridianDegrees(zone)) % 360;
            if (delta > 180) delta -= 360;
            if (delta < -180) delta += 360;
            return Math.Abs(delta);
        }

        static double Atanh(double x)
        {
            return 0.5 * Math.Log((1 + x) / (1 - x));
        }

        /// <summary>
        /// Projects lat/lng (degrees) into the given zone. The hemisphere comes from the zone's sign, so
        /// points just across the equator stay continuous with the rest of the scene.
        /// </summary>
        public static void Forward(double lat, double lng, int zone, out double easting, out double northing)
        {
            double phi = lat * DegToRad;
            double lambda = lng * DegToRad - CentralMeridian(zone);

            double sinPhi = Math.Sin(phi);
            double t = Math.Sinh(Atanh(sinPhi) - ConformalFactor * Atanh(ConformalFactor * sinPhi));
            double xiPrime = Math.Atan2(t, Math.Cos(lambda));
            double etaPrime = Atanh(Math.Sin(lambda) / Math.Sqrt(1 + t * t));

            double xi = xiPrime;
            double eta = etaPrime;
            for (int j = 0; j < 3; j++)
            {
                double k = 2 * (j + 1);
                xi += Alpha[j] * Math.Sin(k * xiPrime) * Math.Cosh(k * etaPrime);
                eta += Alpha[j] * Math.Cos(k * xiPrime) * Math.Sinh(k * etaPrime);
            }

            easting = FalseEasting + ScaleFactor * RectifyingRadius * eta;
            northing = (zone < 0 ? FalseNorthingSouth : 0) + ScaleFactor * RectifyingRadius * xi;
        }

        /// <summary>
        /// Inverse of <see cref="Forward"/>, returning degrees.
        /// </summary>
        public static void Inverse(double easting, double northing, int zone, out double lat, out double lng)
        {
            double xi = (northing - (zone < 0 ? FalseNorthingSouth : 0)) / (ScaleFactor * RectifyingRadius);
            double eta = (easting - FalseEasting) / (ScaleFactor * RectifyingRadius);

            double xiPrime = xi;
            double etaPrime = eta;
            for (int j = 0; j < 3; j++)
            {
                double k = 2 * (j + 1);
                xiPrime -= Beta[j] * Math.Sin(k * xi) * Math.Cosh(k * eta);
                etaPrime -= Beta[j] * Math.Cos(k * xi) * Math.Sinh(k * eta);
            }

            double chi = Math.Asin(Math.Sin(xiPrime) / Math.Cosh(etaPrime));
            double phi = chi;
            for (int j = 0; j < 3; j++)
                phi += Delta[j] * Math.Sin(2 * (j + 1) * chi);

            lat = phi / DegToRad;
            lng = (CentralMeridian(zone) + Math.Atan2(Math.Sinh(etaPrime), Math.Cos(xiPrime))) / DegToRad;
        }
    }
}
