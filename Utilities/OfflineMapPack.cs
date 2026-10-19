using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using GMap.NET;
using GMap.NET.MapProviders;
using log4net;
using MissionPlanner.Maps;
using Newtonsoft.Json;

namespace MissionPlanner.Utilities
{
    /// <summary>
    /// Works on the tiles behind an <see cref="OfflineMapRegion"/>: coverage statistics, deletion, and the
    /// .mappack export/import format used to move map areas between computers.
    ///
    /// A .mappack is a plain zip file containing
    ///   manifest.json                       the region entries (see <see cref="Manifest"/>)
    ///   tiles/{provider}/{zoom}/{y}/{x}.jpg the tiles, in the same layout MyImageCache keeps them on disk
    /// so importing is a straight copy into the cache folder, and the file can be unpacked by hand into
    /// gmapcache\TileDBv3\en\ if the app is not available.
    /// </summary>
    public static class OfflineMapPack
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public const string FileExtension = "mappack";
        public const string FileFilter = "Offline map pack (*.mappack)|*.mappack|All files (*.*)|*.*";

        private const string ManifestName = "manifest.json";
        private const string FormatTag = "TitanPlanner.OfflineMapPack";
        private const int FormatVersion = 1;

        // tiles/{provider}/{zoom}/{y}/{x}.{ext}; the provider segment may not contain path separators
        private static readonly Regex TileEntry = new Regex(@"^tiles/([^/\\]+)/(\d{1,2})/(\d+)/(\d+)\.(jpg|jpeg|png)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public class Manifest
        {
            public string Format = FormatTag;
            public int Version = FormatVersion;
            public string Application;
            public DateTime Created;
            public long TileCount;
            public List<OfflineMapRegion> Regions = new List<OfflineMapRegion>();
        }

        public class RegionStats
        {
            public long TilesOnDisk;
            public long BytesOnDisk;
            public long TilesExpected;

            public double Coverage
            {
                get { return TilesExpected == 0 ? 0 : Math.Min(1.0, (double)TilesOnDisk / TilesExpected); }
            }
        }

        public class PackInfo
        {
            public Manifest Manifest;
            public long TileEntries;
            public List<string> Providers = new List<string>();
        }

        public class ImportResult
        {
            public long Added;
            public long Skipped;
            public bool Cancelled;
            public List<OfflineMapRegion> Regions = new List<OfflineMapRegion>();
            public List<string> UnknownProviders = new List<string>();
        }

        public struct TileRange
        {
            public long X1, Y1, X2, Y2;

            public long Count
            {
                get { return (X2 < X1 || Y2 < Y1) ? 0 : (X2 - X1 + 1) * (Y2 - Y1 + 1); }
            }
        }

        /// <summary>The folder tiles live in: gmapcache\TileDBv3\{language}, without a trailing separator.</summary>
        public static string CacheRoot
        {
            get
            {
                var root = MyImageCache.Instance?.CacheLocation;
                if (string.IsNullOrEmpty(root))
                    root = Path.Combine(Settings.GetDataDirectory(), "gmapcache", "TileDBv3", GMapProvider.LanguageStr);
                return root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
        }

        public static GMapProvider FindProvider(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            return GMapProviders.List.FirstOrDefault(p => p.Name == name) ??
                   GMapProviders.List.FirstOrDefault(p => p.Overlays != null && p.Overlays.Any(o => o.Name == name));
        }

        /// <summary>
        /// Folder names under the cache root that hold a provider's tiles. MyImageCache files tiles by the
        /// overlay they were fetched from, so a composite provider (e.g. a hybrid map) uses several folders.
        /// </summary>
        public static string[] TileDirNames(string providerName)
        {
            var provider = GMapProviders.List.FirstOrDefault(p => p.Name == providerName);
            if (provider == null || provider.Overlays == null || provider.Overlays.Length == 0)
                return new[] { providerName };
            return provider.Overlays.Select(o => o.Name).Distinct().ToArray();
        }

        /// <summary>Tile indices covering area at zoom, the same way TilePrefetcher walks them.</summary>
        public static TileRange GetTileRange(GMapProvider provider, RectLatLng area, int zoom)
        {
            var proj = provider.Projection;
            var topLeft = proj.FromPixelToTileXY(proj.FromLatLngToPixel(area.LocationTopLeft, zoom));
            var rightBottom = proj.FromPixelToTileXY(proj.FromLatLngToPixel(area.LocationRightBottom, zoom));
            var min = proj.GetTileMatrixMinXY(zoom);
            var max = proj.GetTileMatrixMaxXY(zoom);
            return new TileRange
            {
                X1 = Math.Max(topLeft.X, min.Width),
                Y1 = Math.Max(topLeft.Y, min.Height),
                X2 = Math.Min(rightBottom.X, max.Width),
                Y2 = Math.Min(rightBottom.Y, max.Height)
            };
        }

        public static long ExpectedTiles(OfflineMapRegion region)
        {
            var provider = FindProvider(region.Provider);
            if (provider == null)
                return 0;

            long total = 0;
            int dirs = TileDirNames(region.Provider).Length;
            for (int zoom = region.MinZoom; zoom <= region.MaxZoom; zoom++)
                total += GetTileRange(provider, region.Bounds, zoom).Count * dirs;
            return total;
        }

        /// <summary>Width and height of the area in km, measured through its centre.</summary>
        public static void GetSizeKm(RectLatLng area, out double widthKm, out double heightKm)
        {
            var mid = area.LocationMiddle;
            widthKm = DistanceKm(new PointLatLng(mid.Lat, area.Lng), new PointLatLng(mid.Lat, area.Right));
            heightKm = DistanceKm(new PointLatLng(area.Lat, mid.Lng), new PointLatLng(area.Bottom, mid.Lng));
        }

        private static double DistanceKm(PointLatLng a, PointLatLng b)
        {
            const double earthRadiusKm = 6371.0088;
            double dLat = ToRadians(b.Lat - a.Lat);
            double dLng = ToRadians(b.Lng - a.Lng);
            double h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(a.Lat)) * Math.Cos(ToRadians(b.Lat)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            return 2 * earthRadiusKm * Math.Asin(Math.Min(1, Math.Sqrt(h)));
        }

        private static double ToRadians(double deg)
        {
            return deg * Math.PI / 180.0;
        }

        public static string FormatSize(long bytes)
        {
            if (bytes >= 1L << 30) return (bytes / (double)(1L << 30)).ToString("0.00") + " GB";
            if (bytes >= 1L << 20) return (bytes / (double)(1L << 20)).ToString("0.0") + " MB";
            if (bytes >= 1L << 10) return (bytes / (double)(1L << 10)).ToString("0") + " KB";
            return bytes + " B";
        }

        /// <summary>Every cached tile file inside the region, as (path relative to the cache root, full path).</summary>
        private static IEnumerable<KeyValuePair<string, string>> EnumerateTiles(OfflineMapRegion region, CancellationToken ct)
        {
            var provider = FindProvider(region.Provider);
            if (provider == null)
                yield break;

            string root = CacheRoot;
            var bounds = region.Bounds;

            foreach (var dirName in TileDirNames(region.Provider))
            {
                for (int zoom = region.MinZoom; zoom <= region.MaxZoom; zoom++)
                {
                    var range = GetTileRange(provider, bounds, zoom);
                    for (long y = range.Y1; y <= range.Y2; y++)
                    {
                        ct.ThrowIfCancellationRequested();

                        string yDir = Path.Combine(root, dirName, zoom.ToString(CultureInfo.InvariantCulture),
                            y.ToString(CultureInfo.InvariantCulture));
                        if (!Directory.Exists(yDir))
                            continue;

                        // GetFiles (not Enumerate) so callers may delete while iterating
                        foreach (var file in Directory.GetFiles(yDir))
                        {
                            long x;
                            if (!long.TryParse(Path.GetFileNameWithoutExtension(file), NumberStyles.None,
                                    CultureInfo.InvariantCulture, out x))
                                continue;
                            if (x < range.X1 || x > range.X2)
                                continue;

                            yield return new KeyValuePair<string, string>(
                                dirName + "/" + zoom + "/" + y + "/" + Path.GetFileName(file), file);
                        }
                    }
                }
            }
        }

        private static string TilePath(string root, string dirName, int zoom, long x, long y)
        {
            return Path.Combine(root, dirName, zoom.ToString(CultureInfo.InvariantCulture),
                y.ToString(CultureInfo.InvariantCulture), x.ToString(CultureInfo.InvariantCulture) + ".jpg");
        }

        /// <summary>
        /// Composites cached tiles into a picture of the area, using the highest zoom in the area's range
        /// whose tiles fit within maxTilesPerSide², cropped to the area's bounds. Tiles that are not on disk
        /// show as hatching so gaps in coverage are visible. Returns null when the map source is not
        /// installed. partial is set when even the lowest zoom has too many tiles; only the centre is drawn then.
        /// </summary>
        public static Bitmap RenderPreview(OfflineMapRegion region, int maxTilesPerSide, CancellationToken ct,
            out int zoomUsed, out int tilesDrawn, out bool partial)
        {
            zoomUsed = 0;
            tilesDrawn = 0;
            partial = false;

            var provider = FindProvider(region.Provider);
            if (provider == null)
                return null;

            var proj = provider.Projection;
            var bounds = region.Bounds;

            int zoom = region.MinZoom;
            for (int z = region.MaxZoom; z >= region.MinZoom; z--)
            {
                var r = GetTileRange(provider, bounds, z);
                if (r.X2 - r.X1 + 1 <= maxTilesPerSide && r.Y2 - r.Y1 + 1 <= maxTilesPerSide)
                {
                    zoom = z;
                    break;
                }
            }
            zoomUsed = zoom;

            var range = GetTileRange(provider, bounds, zoom);
            if (range.X2 - range.X1 + 1 > maxTilesPerSide)
            {
                long cx = (range.X1 + range.X2) / 2;
                range.X1 = cx - maxTilesPerSide / 2;
                range.X2 = range.X1 + maxTilesPerSide - 1;
                partial = true;
            }
            if (range.Y2 - range.Y1 + 1 > maxTilesPerSide)
            {
                long cy = (range.Y1 + range.Y2) / 2;
                range.Y1 = cy - maxTilesPerSide / 2;
                range.Y2 = range.Y1 + maxTilesPerSide - 1;
                partial = true;
            }
            if (range.Count <= 0)
                return null;

            int tileW = (int)proj.TileSize.Width;
            int tileH = (int)proj.TileSize.Height;
            int width = (int)(range.X2 - range.X1 + 1) * tileW;
            int height = (int)(range.Y2 - range.Y1 + 1) * tileH;

            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            try
            {
                using (var g = Graphics.FromImage(bitmap))
                {
                    using (var hatch = new HatchBrush(HatchStyle.WideUpwardDiagonal, Color.FromArgb(78, 78, 78), Color.FromArgb(52, 52, 52)))
                        g.FillRectangle(hatch, 0, 0, width, height);

                    string root = CacheRoot;
                    // overlays are drawn in provider order, so e.g. hybrid labels land on top of imagery
                    foreach (var dirName in TileDirNames(region.Provider))
                    {
                        for (long x = range.X1; x <= range.X2; x++)
                        {
                            for (long y = range.Y1; y <= range.Y2; y++)
                            {
                                ct.ThrowIfCancellationRequested();

                                string path = TilePath(root, dirName, zoom, x, y);
                                if (!File.Exists(path))
                                    continue;

                                try
                                {
                                    using (var stream = File.OpenRead(path))
                                    using (var tile = Image.FromStream(stream))
                                        g.DrawImage(tile, (int)(x - range.X1) * tileW, (int)(y - range.Y1) * tileH, tileW, tileH);
                                    tilesDrawn++;
                                }
                                catch (Exception ex)
                                {
                                    log.Debug(ex);
                                }
                            }
                        }
                    }
                }

                if (partial)
                    return bitmap;

                // crop the whole tiles down to the area's own bounds
                var origin = proj.FromTileXYToPixel(new GPoint(range.X1, range.Y1));
                var topLeft = proj.FromLatLngToPixel(bounds.LocationTopLeft, zoom);
                var rightBottom = proj.FromLatLngToPixel(bounds.LocationRightBottom, zoom);
                var full = new Rectangle(0, 0, width, height);
                var crop = Rectangle.Intersect(full, Rectangle.FromLTRB(
                    (int)(topLeft.X - origin.X), (int)(topLeft.Y - origin.Y),
                    (int)(rightBottom.X - origin.X), (int)(rightBottom.Y - origin.Y)));
                if (crop.Width < 8 || crop.Height < 8 || crop == full)
                    return bitmap;

                var cropped = bitmap.Clone(crop, bitmap.PixelFormat);
                bitmap.Dispose();
                bitmap = null;
                return cropped;
            }
            catch
            {
                bitmap?.Dispose();
                throw;
            }
        }

        public static RegionStats GetStats(OfflineMapRegion region, CancellationToken ct)
        {
            var stats = new RegionStats { TilesExpected = ExpectedTiles(region) };
            foreach (var tile in EnumerateTiles(region, ct))
            {
                stats.TilesOnDisk++;
                try
                {
                    stats.BytesOnDisk += new FileInfo(tile.Value).Length;
                }
                catch
                {
                }
            }

            return stats;
        }

        /// <summary>Total number and size of all cached tiles, whatever region they belong to.</summary>
        public static void GetCacheTotals(CancellationToken ct, out long files, out long bytes)
        {
            files = 0;
            bytes = 0;
            var root = new DirectoryInfo(CacheRoot);
            if (!root.Exists)
                return;

            foreach (var file in root.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                ct.ThrowIfCancellationRequested();
                files++;
                bytes += file.Length;
            }
        }

        /// <summary>
        /// Deletes the region's tiles from the cache. Tiles shared with an overlapping region are deleted too.
        /// Returns the number of files removed.
        /// </summary>
        public static long DeleteTiles(OfflineMapRegion region, Action<long> progress, Func<bool> cancelled)
        {
            long deleted = 0;
            foreach (var tile in EnumerateTiles(region, CancellationToken.None))
            {
                if (cancelled())
                    break;

                try
                {
                    File.Delete(tile.Value);
                    deleted++;
                }
                catch (Exception ex)
                {
                    log.Debug(ex);
                }

                if (deleted % 200 == 0)
                    progress?.Invoke(deleted);
            }

            // empty zoom/row folders are left behind; they are harmless and other regions may still use them
            return deleted;
        }

        /// <summary>
        /// Writes the regions and their cached tiles to a .mappack file. Returns false if cancelled, in which
        /// case no file is left behind.
        /// </summary>
        public static bool Export(IList<OfflineMapRegion> regions, string file, Action<int, string> progress, Func<bool> cancelled)
        {
            long expected = Math.Max(1, regions.Sum(r => ExpectedTiles(r)));
            var written = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string tmp = file + ".tmp";
            long done = 0;

            try
            {
                using (var stream = File.Create(tmp))
                using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
                {
                    foreach (var region in regions)
                    {
                        foreach (var tile in EnumerateTiles(region, CancellationToken.None))
                        {
                            if (cancelled())
                                return false;

                            // overlapping regions share tiles; a zip may hold each path only once
                            if (!written.Add(tile.Key))
                                continue;

                            // tiles are already jpeg/png, so storing them uncompressed is faster and no larger
                            var entry = zip.CreateEntry("tiles/" + tile.Key, CompressionLevel.NoCompression);
                            using (var output = entry.Open())
                            using (var input = File.OpenRead(tile.Value))
                                input.CopyTo(output);

                            done++;
                            if (done % 50 == 0)
                                progress?.Invoke((int)Math.Min(99, done * 100 / expected),
                                    string.Format("Packing tiles {0:N0} / ~{1:N0}", done, expected));
                        }
                    }

                    var manifest = new Manifest
                    {
                        Application = "TitanPlanner " + Assembly.GetEntryAssembly()?.GetName().Version,
                        Created = DateTime.Now,
                        TileCount = done,
                        Regions = regions.Select(r => r.Clone()).ToList()
                    };
                    var manifestEntry = zip.CreateEntry(ManifestName, CompressionLevel.Optimal);
                    using (var writer = new StreamWriter(manifestEntry.Open()))
                        writer.Write(JsonConvert.SerializeObject(manifest, Formatting.Indented));
                }

                if (File.Exists(file))
                    File.Delete(file);
                File.Move(tmp, file);
                return true;
            }
            finally
            {
                try
                {
                    if (File.Exists(tmp))
                        File.Delete(tmp);
                }
                catch
                {
                }
            }
        }

        /// <summary>Reads what a pack contains without importing it.</summary>
        public static PackInfo Inspect(string file)
        {
            var info = new PackInfo();
            var providers = new HashSet<string>();

            using (var stream = File.OpenRead(file))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                var manifestEntry = zip.GetEntry(ManifestName);
                if (manifestEntry != null)
                {
                    using (var reader = new StreamReader(manifestEntry.Open()))
                        info.Manifest = JsonConvert.DeserializeObject<Manifest>(reader.ReadToEnd());
                }

                foreach (var entry in zip.Entries)
                {
                    var match = TileEntry.Match(entry.FullName.Replace('\\', '/'));
                    if (!match.Success)
                        continue;
                    info.TileEntries++;
                    providers.Add(match.Groups[1].Value);
                }
            }

            info.Providers = providers.OrderBy(p => p).ToList();
            return info;
        }

        /// <summary>
        /// Copies a pack's tiles into the cache (existing tiles are kept unless overwrite is set) and returns
        /// the regions it describes. Regions are not registered; the caller decides that.
        /// </summary>
        public static ImportResult Import(string file, bool overwrite, Action<int, string> progress, Func<bool> cancelled)
        {
            var result = new ImportResult();
            string root = CacheRoot;
            string rootFull = Path.GetFullPath(root + Path.DirectorySeparatorChar);
            var bounds = new Dictionary<string, TileBoundsAccumulator>();
            Manifest manifest = null;

            using (var stream = File.OpenRead(file))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                int total = Math.Max(1, zip.Entries.Count);
                int index = 0;

                foreach (var entry in zip.Entries)
                {
                    if (cancelled())
                    {
                        result.Cancelled = true;
                        break;
                    }

                    index++;
                    if (index % 50 == 0)
                        progress?.Invoke(index * 100 / total, string.Format("Copying tiles {0:N0} / {1:N0}", index, total));

                    if (string.Equals(entry.FullName, ManifestName, StringComparison.OrdinalIgnoreCase))
                    {
                        using (var reader = new StreamReader(entry.Open()))
                            manifest = JsonConvert.DeserializeObject<Manifest>(reader.ReadToEnd());
                        continue;
                    }

                    // anything that is not a tile path is ignored
                    var match = TileEntry.Match(entry.FullName.Replace('\\', '/'));
                    if (!match.Success)
                        continue;

                    string provider = match.Groups[1].Value;
                    if (provider == "." || provider == "..")
                        continue;
                    int zoom = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                    long y = long.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
                    long x = long.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);

                    // the cache names every tile .jpg regardless of its real encoding
                    string dest = Path.GetFullPath(Path.Combine(root, provider,
                        zoom.ToString(CultureInfo.InvariantCulture), y.ToString(CultureInfo.InvariantCulture),
                        x.ToString(CultureInfo.InvariantCulture) + ".jpg"));
                    if (!dest.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
                        continue; // never write outside the cache folder

                    if (!overwrite && File.Exists(dest))
                    {
                        result.Skipped++;
                    }
                    else
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(dest));
                        using (var input = entry.Open())
                        using (var output = File.Create(dest))
                            input.CopyTo(output);
                        result.Added++;
                    }

                    TileBoundsAccumulator acc;
                    if (!bounds.TryGetValue(provider, out acc))
                        bounds[provider] = acc = new TileBoundsAccumulator();
                    acc.Add(zoom, x, y);
                }
            }

            string source = "Imported from " + Path.GetFileName(file);
            if (manifest?.Regions != null && manifest.Regions.Count > 0)
            {
                foreach (var region in manifest.Regions)
                {
                    var copy = region.Clone();
                    copy.Id = Guid.NewGuid().ToString("N");
                    copy.Source = source;
                    result.Regions.Add(copy);
                }
            }
            else
            {
                // a pack without a manifest (e.g. hand-made): describe each provider's tiles by their extent
                string baseName = Path.GetFileNameWithoutExtension(file);
                foreach (var kv in bounds)
                {
                    var region = kv.Value.ToRegion(kv.Key, baseName, source);
                    if (region != null)
                        result.Regions.Add(region);
                }
            }

            foreach (var providerName in bounds.Keys.Concat(result.Regions.Select(r => r.Provider)).Distinct())
            {
                if (FindProvider(providerName) == null)
                    result.UnknownProviders.Add(providerName);
            }

            return result;
        }

        /// <summary>Tracks the tile extent of one provider folder while importing, to describe manifest-less packs.</summary>
        private class TileBoundsAccumulator
        {
            private int minZoom = int.MaxValue, maxZoom = -1;
            private long minX, minY, maxX, maxY;

            public void Add(int zoom, long x, long y)
            {
                minZoom = Math.Min(minZoom, zoom);
                if (zoom > maxZoom)
                {
                    maxZoom = zoom;
                    minX = maxX = x;
                    minY = maxY = y;
                }
                else if (zoom == maxZoom)
                {
                    minX = Math.Min(minX, x);
                    maxX = Math.Max(maxX, x);
                    minY = Math.Min(minY, y);
                    maxY = Math.Max(maxY, y);
                }
            }

            public OfflineMapRegion ToRegion(string providerDir, string name, string source)
            {
                var provider = FindProvider(providerDir);
                if (provider == null || maxZoom < 0)
                    return null;

                var proj = provider.Projection;
                var topLeft = proj.FromPixelToLatLng(proj.FromTileXYToPixel(new GPoint(minX, minY)), maxZoom);
                // last pixel of the bottom-right tile, so the bounds do not spill into the next tile column/row
                var edge = proj.FromTileXYToPixel(new GPoint(maxX + 1, maxY + 1));
                var rightBottom = proj.FromPixelToLatLng(new GPoint(edge.X - 1, edge.Y - 1), maxZoom);

                return new OfflineMapRegion
                {
                    Name = name,
                    Provider = provider.Name,
                    Bounds = RectLatLng.FromLTRB(topLeft.Lng, topLeft.Lat, rightBottom.Lng, rightBottom.Lat),
                    MinZoom = minZoom,
                    MaxZoom = maxZoom,
                    Source = source
                };
            }
        }
    }
}
