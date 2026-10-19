using System;
using MissionPlanner.Utilities;

namespace MissionPlanner.Controls.Scene3D
{
    /// <summary>
    /// The scene's coordinate system: metres east/north of an origin in one UTM zone, with altitude in
    /// metres AMSL. Terrain tiles keep absolute UTM origins, so moving the origin only shifts them; only a
    /// zone change (hundreds of kilometres of travel) invalidates built terrain.
    /// Owned by the render thread.
    /// </summary>
    public sealed class WorldFrame
    {
        /// <summary>Move the origin once the centre is this far from it, to keep float precision.</summary>
        public const double RebaseDistance = 10000;

        /// <summary>
        /// Switch zones once this many degrees from the central meridian (a zone is +/-3 degrees wide; the extra
        /// degree avoids flip-flopping on the boundary). Keeps grid north within a few degrees of true north,
        /// which the vehicle's heading is drawn against.
        /// </summary>
        public const double ZoneChangeLongitude = 4.0;

        public int Zone { get; private set; }
        public double OriginEasting { get; private set; }
        public double OriginNorthing { get; private set; }

        /// <summary>Incremented whenever the origin moves; local coordinates computed earlier are stale.</summary>
        public int Version { get; private set; }

        /// <summary>Incremented whenever the zone changes; absolute UTM coordinates computed earlier are stale.</summary>
        public int ZoneVersion { get; private set; }

        public bool IsValid
        {
            get { return Zone != 0; }
        }

        /// <summary>
        /// Keeps the origin near the given position. Returns true if the origin or zone changed.
        /// </summary>
        public bool Update(double lat, double lng)
        {
            if (!IsValid)
            {
                SetZoneAndOrigin(lat, lng);
                return true;
            }

            // Measured from the zone itself, not the origin: the origin follows the vehicle every 10 km.
            if (UtmProjection.LongitudeFromCentralMeridian(lng, Zone) > ZoneChangeLongitude &&
                Math.Abs(UtmProjection.ZoneFor(lat, lng)) != Math.Abs(Zone))
            {
                SetZoneAndOrigin(lat, lng);
                return true;
            }

            double e, n;
            UtmProjection.Forward(lat, lng, Zone, out e, out n);
            double dist = Math.Sqrt((e - OriginEasting) * (e - OriginEasting) + (n - OriginNorthing) * (n - OriginNorthing));
            if (dist > RebaseDistance)
            {
                OriginEasting = e;
                OriginNorthing = n;
                Version++;
                return true;
            }

            return false;
        }

        void SetZoneAndOrigin(double lat, double lng)
        {
            Zone = UtmProjection.ZoneFor(lat, lng);
            double e, n;
            UtmProjection.Forward(lat, lng, Zone, out e, out n);
            OriginEasting = e;
            OriginNorthing = n;
            Version++;
            ZoneVersion++;
        }

        public void ToAbsolute(double lat, double lng, out double easting, out double northing)
        {
            UtmProjection.Forward(lat, lng, Zone, out easting, out northing);
        }

        public void ToLocal(double lat, double lng, out double x, out double y)
        {
            double e, n;
            UtmProjection.Forward(lat, lng, Zone, out e, out n);
            x = e - OriginEasting;
            y = n - OriginNorthing;
        }

        /// <summary>Local scene coordinates {x, y, alt} for a position.</summary>
        public double[] ToLocal(PointLatLngAlt point)
        {
            double x, y;
            ToLocal(point.Lat, point.Lng, out x, out y);
            return new[] { x, y, point.Alt };
        }

        public PointLatLngAlt ToLatLng(double x, double y, double alt)
        {
            double lat, lng;
            UtmProjection.Inverse(x + OriginEasting, y + OriginNorthing, Zone, out lat, out lng);
            return new PointLatLngAlt(lat, lng, alt);
        }
    }
}
