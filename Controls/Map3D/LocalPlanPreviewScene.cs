using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenTK;

namespace MissionPlanner.Controls.Scene3D
{
    internal sealed class PlanPreviewScene
    {
        internal IReadOnlyList<Vector3> Route { get; }
        internal IReadOnlyList<Vector3> TerrainLines { get; }

        internal PlanPreviewScene(Vector3[] route, Vector3[] terrainLines)
        {
            Route = Array.AsReadOnly((Vector3[])route.Clone());
            TerrainLines = Array.AsReadOnly((Vector3[])terrainLines.Clone());
        }
    }

    internal sealed class PlanPreviewDataException : Exception
    {
        internal int Row { get; }

        internal PlanPreviewDataException(string message, int row = 0) : base(message)
        {
            Row = row;
        }
    }

    internal sealed class PlanPreviewSnapshot
    {
        internal sealed class Waypoint
        {
            internal ushort Command { get; }
            internal byte Frame { get; }
            // Param1–4, latitude, longitude, altitude, in their original order.
            internal IReadOnlyList<double> Fields { get; }
            internal double Latitude => Fields[4];
            internal double Longitude => Fields[5];
            internal double Altitude => Fields[6];

            internal Waypoint(ushort command, byte frame, double[] fields)
            {
                Command = command;
                Frame = frame;
                Fields = Array.AsReadOnly((double[])fields.Clone());
            }

            internal bool Matches(Waypoint other)
            {
                return Command == other.Command && Frame == other.Frame && Fields.SequenceEqual(other.Fields);
            }
        }

        internal double HomeLatitude { get; }
        internal double HomeLongitude { get; }
        internal double HomeAltitude { get; }
        internal double HomeDisplayUnitsPerMetre { get; }
        internal IReadOnlyList<Waypoint> Waypoints { get; }

        internal PlanPreviewSnapshot(double homeLatitude, double homeLongitude, double homeAltitude,
            double homeDisplayUnitsPerMetre, Waypoint[] waypoints)
        {
            HomeLatitude = homeLatitude;
            HomeLongitude = homeLongitude;
            HomeAltitude = homeAltitude;
            HomeDisplayUnitsPerMetre = homeDisplayUnitsPerMetre;
            Waypoints = Array.AsReadOnly((Waypoint[])waypoints.Clone());
        }

        internal bool Matches(PlanPreviewSnapshot other)
        {
            return other != null && HomeLatitude == other.HomeLatitude && HomeLongitude == other.HomeLongitude &&
                HomeAltitude == other.HomeAltitude && HomeDisplayUnitsPerMetre == other.HomeDisplayUnitsPerMetre &&
                Waypoints.Count == other.Waypoints.Count && Waypoints.Zip(other.Waypoints, (a, b) => a.Matches(b)).All(same => same);
        }

        internal static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        internal static bool IsGeographic(double latitude, double longitude) =>
            IsFinite(latitude) && IsFinite(longitude) && latitude >= -90 && latitude <= 90 && longitude >= -180 && longitude <= 180;
    }

    // This reader intentionally has no dependency on the shared DEM cache, terrain engine, or vehicle.
    // The selected manifest is the provenance boundary. Its hashes bind the exact terrain bytes used.
    internal sealed class LocalPlanElevationSource
    {
        private const string Schema = "TitanPlanner.PlanPreview.EGM96.v1";
        private const string UsgsSrtmProduct = "USGS:10.5066/F7PR7TFT";
        private const string EsaSnapSrtmProduct = "ESA-SNAP:SRTMGL1";
        private const int PostsPerDegree = 3600;
        private const int TileWidth = PostsPerDegree + 1;
        private const int TileBytes = TileWidth * TileWidth * 2;
        private const int MaxManifestBytes = 4 * 1024 * 1024;
        private const int MaxTilesPerPreview = 4;
        private static readonly Regex TileName = new Regex(@"\A[NS][0-9]{2}[EW][0-9]{3}\.hgt\z", RegexOptions.CultureInvariant);
        private static readonly Regex Digest = new Regex(@"\A[0-9a-fA-F]{64}\z", RegexOptions.CultureInvariant);
        private readonly string _folder;
        private readonly Height _home;
        private readonly HashSet<(double, double, double)> _absolute;
        private readonly Dictionary<string, string> _tiles;

        private sealed class Manifest
        {
            [JsonProperty("schema", Required = Required.Always)]
            public string Schema { get; set; }
            [JsonProperty("planSource", Required = Required.AllowNull)]
            public string PlanSource { get; set; }
            [JsonProperty("home", Required = Required.AllowNull)]
            public Height Home { get; set; }
            [JsonProperty("absolute", Required = Required.Always)]
            public Height[] Absolute { get; set; }
            [JsonProperty("terrainProduct", Required = Required.AllowNull)]
            public string TerrainProduct { get; set; }
            [JsonProperty("tiles", Required = Required.Always)]
            public Dictionary<string, string> Tiles { get; set; }
        }

        private sealed class Height
        {
            [JsonProperty("latitude", Required = Required.Always)]
            public double Latitude { get; set; }
            [JsonProperty("longitude", Required = Required.Always)]
            public double Longitude { get; set; }
            [JsonProperty("metres", Required = Required.Always)]
            public double Metres { get; set; }

            internal bool IsValid => PlanPreviewSnapshot.IsGeographic(Latitude, Longitude) && PlanPreviewSnapshot.IsFinite(Metres);
            internal (double, double, double) Key => (Latitude, Longitude, Metres);
        }

        private LocalPlanElevationSource(string folder, Manifest manifest)
        {
            _folder = folder;
            _home = manifest.Home;
            _absolute = new HashSet<(double, double, double)>(manifest.Absolute.Select(height => height.Key));
            _tiles = new Dictionary<string, string>(manifest.Tiles, StringComparer.Ordinal);
        }

        internal static LocalPlanElevationSource Open(string path)
        {
            path = RequireLocalFile(path);
            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (stream.Length == 0 || stream.Length > MaxManifestBytes)
                        throw new PlanPreviewDataException("The offline elevation manifest has an invalid size.");

                    using (var reader = new JsonTextReader(new StreamReader(stream, new UTF8Encoding(false, true))))
                    {
                        reader.MaxDepth = 8;
                        reader.DateParseHandling = DateParseHandling.None;
                        var json = JObject.Load(reader, new JsonLoadSettings
                        {
                            DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
                        });
                        if (reader.Read())
                            throw new PlanPreviewDataException("The offline elevation manifest is invalid.");
                        var manifest = json.ToObject<Manifest>(JsonSerializer.Create(new JsonSerializerSettings
                        {
                            MissingMemberHandling = MissingMemberHandling.Error
                        }));
                        if (manifest.Schema != Schema ||
                            ((manifest.Home != null || manifest.Absolute.Length > 0) && string.IsNullOrWhiteSpace(manifest.PlanSource)) ||
                            (manifest.Home != null && !manifest.Home.IsValid) ||
                            manifest.Absolute.Any(height => height == null || !height.IsValid) ||
                            (manifest.Tiles.Count > 0 && manifest.TerrainProduct != UsgsSrtmProduct &&
                                manifest.TerrainProduct != EsaSnapSrtmProduct) ||
                            manifest.Tiles.Any(tile => !TileName.IsMatch(tile.Key) || tile.Value == null || !Digest.IsMatch(tile.Value)))
                            throw new PlanPreviewDataException("The offline elevation manifest is invalid.");

                        return new LocalPlanElevationSource(Path.GetDirectoryName(path), manifest);
                    }
                }
            }
            catch (JsonException)
            {
                throw new PlanPreviewDataException("The offline elevation manifest is invalid.");
            }
        }

        internal PlanPreviewScene BuildScene(PlanPreviewSnapshot plan)
        {
            var frame = new WorldFrame();
            var first = plan.Waypoints[0];
            if (first.Latitude < -80 || first.Latitude > 84)
                throw new PlanPreviewDataException("The waypoint is outside the preview coordinate range.", 1);
            frame.Update(first.Latitude, first.Longitude);
            var route = new Vector3[plan.Waypoints.Count];
            var terrain = new List<Vector3>();
            var loadedTiles = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            for (var index = 0; index < route.Length; index++)
            {
                var item = plan.Waypoints[index];
                int row = index + 1;
                if (item.Latitude < -80 || item.Latitude > 84 ||
                    frame.Zone != UtmProjection.ZoneFor(item.Latitude, item.Longitude))
                    throw new PlanPreviewDataException("The waypoint is outside the preview coordinate range.", row);

                frame.ToLocal(item.Latitude, item.Longitude, out var east, out var north);
                double altitude;
                switch (item.Frame)
                {
                    case 0:
                    case 5:
                        if (!_absolute.Contains((item.Latitude, item.Longitude, item.Altitude)))
                            throw new PlanPreviewDataException("The waypoint has no matching EGM96 altitude evidence.", row);
                        altitude = item.Altitude;
                        break;
                    case 3:
                    case 6:
                        if (_home == null || _home.Key != (plan.HomeLatitude, plan.HomeLongitude, plan.HomeAltitude))
                            throw new PlanPreviewDataException("The plan Home has no matching EGM96 altitude evidence.", row);
                        altitude = _home.Metres + item.Altitude;
                        break;
                    case 10:
                    case 11:
                        double ground = ReadTerrain(item.Latitude, item.Longitude, row, loadedTiles);
                        altitude = ground + item.Altitude;
                        // Draw a small cross at each covered DEM sample. There is no surface
                        // or clearance claim between known samples.
                        terrain.Add(new Vector3((float)east - 3, (float)north, (float)ground));
                        terrain.Add(new Vector3((float)east + 3, (float)north, (float)ground));
                        terrain.Add(new Vector3((float)east, (float)north - 3, (float)ground));
                        terrain.Add(new Vector3((float)east, (float)north + 3, (float)ground));
                        break;
                    default:
                        throw new PlanPreviewDataException("The waypoint uses an unsupported altitude reference.", row);
                }

                if (!PlanPreviewSnapshot.IsFinite(altitude) || Math.Abs(altitude) > 10_000_000)
                    throw new PlanPreviewDataException("The waypoint altitude is outside the preview range.", row);
                route[index] = new Vector3((float)east, (float)north, (float)altitude);
            }
            return new PlanPreviewScene(route, terrain.ToArray());
        }

        private double ReadTerrain(double latitude, double longitude, int row, Dictionary<string, byte[]> loaded)
        {
            int south = (int)Math.Floor(latitude);
            int west = (int)Math.Floor(longitude);
            // The USGS high-latitude variant has a different longitudinal spacing.
            if (south < -50 || south >= 50)
                throw new PlanPreviewDataException("The waypoint is outside the supported SRTM grid.", row);

            string name = string.Format(CultureInfo.InvariantCulture, "{0}{1:00}{2}{3:000}.hgt",
                south >= 0 ? "N" : "S", Math.Abs(south), west >= 0 ? "E" : "W", Math.Abs(west));
            if (!_tiles.TryGetValue(name, out var expected))
                throw new PlanPreviewDataException("No offline terrain tile covers the waypoint.", row);
            if (!loaded.TryGetValue(name, out var tile))
            {
                if (loaded.Count == MaxTilesPerPreview)
                    throw new PlanPreviewDataException("The plan spans too many terrain tiles for preview.", row);
                string path = RequireLocalFile(Path.Combine(_folder, name));
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var reader = new BinaryReader(stream))
                {
                    if (stream.Length != TileBytes)
                        throw new PlanPreviewDataException("The terrain tile has an unsupported grid size.", row);
                    tile = reader.ReadBytes(TileBytes);
                }
                using (var sha = SHA256.Create())
                {
                    var actual = BitConverter.ToString(sha.ComputeHash(tile)).Replace("-", "");
                    if (tile.Length != TileBytes || !actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
                        throw new PlanPreviewDataException("The terrain tile does not match the manifest checksum.", row);
                }
                loaded.Add(name, tile);
            }

            // Raster is north-first with overlapping posts. At exact edges only use
            // posts with positive weight, so an unused adjacent void cannot mask zero.
            double x = (longitude - west) * PostsPerDegree;
            double y = (south + 1 - latitude) * PostsPerDegree;
            int x0 = (int)Math.Floor(x);
            int y0 = (int)Math.Floor(y);
            double dx = x - x0;
            double dy = y - y0;
            return ReadPost(tile, x0, y0, (1 - dx) * (1 - dy), row) +
                ReadPost(tile, x0 + 1, y0, dx * (1 - dy), row) +
                ReadPost(tile, x0, y0 + 1, (1 - dx) * dy, row) +
                ReadPost(tile, x0 + 1, y0 + 1, dx * dy, row);
        }

        private static double ReadPost(byte[] tile, int x, int y, double weight, int row)
        {
            if (weight == 0) return 0;
            int offset = (y * TileWidth + x) * 2;
            short value = unchecked((short)((tile[offset] << 8) | tile[offset + 1]));
            if (value == short.MinValue)
                throw new PlanPreviewDataException("The terrain tile has no elevation at the waypoint.", row);
            return value * weight;
        }

        private static string RequireLocalFile(string path)
        {
            string full = Path.GetFullPath(path);
            string root = Path.GetPathRoot(full);
            if (root.StartsWith(@"\\", StringComparison.Ordinal) || new DriveInfo(root).DriveType != DriveType.Fixed)
                throw new PlanPreviewDataException("Choose elevation files on a local fixed drive.");
            for (string part = full; part != null && part.Length > root.Length; part = Path.GetDirectoryName(part))
            {
                if ((File.GetAttributes(part) & FileAttributes.ReparsePoint) != 0)
                    throw new PlanPreviewDataException("Choose elevation files without filesystem redirects.");
            }
            return full;
        }
    }
}
