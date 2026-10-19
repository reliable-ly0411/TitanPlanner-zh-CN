using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GMap.NET;
using log4net;
using Newtonsoft.Json;

namespace MissionPlanner.Utilities
{
    /// <summary>
    /// One rectangular map area whose tiles have been downloaded into (or imported into) the tile cache.
    /// </summary>
    public class OfflineMapRegion
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        public string Name { get; set; }

        /// <summary>GMapProvider.Name of the map source the tiles were fetched with.</summary>
        public string Provider { get; set; }

        public double TopLat { get; set; }
        public double LeftLng { get; set; }
        public double BottomLat { get; set; }
        public double RightLng { get; set; }

        public int MinZoom { get; set; }
        public int MaxZoom { get; set; }

        public DateTime Created { get; set; } = DateTime.Now;

        /// <summary>Where the tiles came from, e.g. "Downloaded" or "Imported from x.mappack".</summary>
        public string Source { get; set; }

        [JsonIgnore]
        public RectLatLng Bounds
        {
            get { return RectLatLng.FromLTRB(LeftLng, TopLat, RightLng, BottomLat); }
            set
            {
                TopLat = value.Lat;
                LeftLng = value.Lng;
                BottomLat = value.Bottom;
                RightLng = value.Right;
            }
        }

        public OfflineMapRegion Clone()
        {
            return (OfflineMapRegion)MemberwiseClone();
        }

        /// <summary>True when both describe the same tiles (same source, bounds and zoom range).</summary>
        public bool CoversSameTilesAs(OfflineMapRegion other)
        {
            const double eps = 1e-7;
            return other != null &&
                   Provider == other.Provider &&
                   MinZoom == other.MinZoom && MaxZoom == other.MaxZoom &&
                   Math.Abs(TopLat - other.TopLat) < eps && Math.Abs(LeftLng - other.LeftLng) < eps &&
                   Math.Abs(BottomLat - other.BottomLat) < eps && Math.Abs(RightLng - other.RightLng) < eps;
        }
    }

    /// <summary>
    /// The list of offline map areas on this computer, kept in gmapcache/offlinemaps.json next to the tiles.
    /// The tile cache itself has no record of what was deliberately downloaded, so this is what the
    /// Offline Maps window lists and what gets written into exported map packs.
    /// </summary>
    public static class OfflineMapRegistry
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly object _lock = new object();
        private static List<OfflineMapRegion> _regions;

        /// <summary>Raised after any change is saved. May fire on a background thread.</summary>
        public static event Action Changed;

        public static string FilePath
        {
            get { return Path.Combine(Settings.GetDataDirectory(), "gmapcache", "offlinemaps.json"); }
        }

        /// <summary>A snapshot of the registered areas, newest first.</summary>
        public static List<OfflineMapRegion> Regions
        {
            get
            {
                lock (_lock)
                {
                    EnsureLoaded();
                    return _regions.OrderByDescending(r => r.Created).Select(r => r.Clone()).ToList();
                }
            }
        }

        public static OfflineMapRegion Find(string id)
        {
            lock (_lock)
            {
                EnsureLoaded();
                return _regions.FirstOrDefault(r => r.Id == id)?.Clone();
            }
        }

        public static void Add(OfflineMapRegion region)
        {
            AddRange(new[] { region });
        }

        /// <summary>Adds the regions, skipping any that duplicate an existing entry. Returns how many were added.</summary>
        public static int AddRange(IEnumerable<OfflineMapRegion> regions)
        {
            int added = 0;
            lock (_lock)
            {
                EnsureLoaded();
                foreach (var region in regions)
                {
                    if (region == null || _regions.Any(r => r.CoversSameTilesAs(region)))
                        continue;
                    _regions.Add(region.Clone());
                    added++;
                }

                if (added > 0)
                    Save();
            }

            if (added > 0)
                Changed?.Invoke();
            return added;
        }

        public static void Update(OfflineMapRegion region)
        {
            lock (_lock)
            {
                EnsureLoaded();
                int index = _regions.FindIndex(r => r.Id == region.Id);
                if (index < 0)
                    return;
                _regions[index] = region.Clone();
                Save();
            }

            Changed?.Invoke();
        }

        public static void Remove(string id)
        {
            lock (_lock)
            {
                EnsureLoaded();
                if (_regions.RemoveAll(r => r.Id == id) == 0)
                    return;
                Save();
            }

            Changed?.Invoke();
        }

        private static void EnsureLoaded()
        {
            if (_regions != null)
                return;

            _regions = new List<OfflineMapRegion>();
            try
            {
                if (File.Exists(FilePath))
                {
                    var loaded = JsonConvert.DeserializeObject<List<OfflineMapRegion>>(File.ReadAllText(FilePath));
                    if (loaded != null)
                        _regions = loaded.Where(r => r != null && !string.IsNullOrEmpty(r.Provider)).ToList();
                }
            }
            catch (Exception ex)
            {
                log.Error("Failed to read " + FilePath, ex);
            }
        }

        private static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(_regions, Formatting.Indented));
            }
            catch (Exception ex)
            {
                log.Error("Failed to write " + FilePath, ex);
            }
        }
    }
}
