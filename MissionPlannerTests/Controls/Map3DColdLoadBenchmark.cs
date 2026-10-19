using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenTK;

namespace MissionPlanner.Controls.Scene3D.Tests
{
    /// <summary>
    /// Times the real tile selection, loader and Google imagery download with the tile cache bypassed, at
    /// several locations. Needs the network, so it only runs with MAP3D_BENCHMARK=1:
    /// <code>set MAP3D_BENCHMARK=1 &amp;&amp; vstest.console.exe MissionPlannerTests.dll /Tests:ColdLoad /logger:console;verbosity=detailed</code>
    /// </summary>
    [TestClass]
    public class Map3DColdLoadBenchmark
    {
        [TestMethod]
        [TestCategory("Benchmark")]
        public void ColdLoad()
        {
            if (Environment.GetEnvironmentVariable("MAP3D_BENCHMARK") != "1")
                Assert.Inconclusive("Set MAP3D_BENCHMARK=1 to run the network benchmark.");

            ServicePointManager.DefaultConnectionLimit = 10;
            GMapImageProxy.Enable();
            GMaps.Instance.Mode = AccessMode.ServerOnly;

            // Let Google's tile version lookup finish, as the 2D map does long before the 3D view opens.
            var imagery = new GMapImagerySource(GoogleSatelliteMapProvider.Instance);
            Thread.Sleep(3000);
            foreach (var place in new[] { new[] { 39.7392, -104.9903 }, new[] { 47.3769, 8.5417 }, new[] { -33.8688, 151.2093 }, new[] { 35.6762, 139.6503 } })
                Run(imagery, place[0], place[1]);
        }

        static void Run(GMapImagerySource imagery, double lat, double lng)
        {
            var frame = new WorldFrame();
            frame.Update(lat, lng);
            var tree = new TerrainQuadtree(frame);

            double x, y;
            frame.ToLocal(lat, lng, out x, out y);
            double ground = 0, z = ground + 150;
            double pitch = -15 * Math.PI / 180;
            double fx = 0, fy = Math.Cos(pitch), fz = Math.Sin(pitch);
            double radius = Map3D.ViewRadius(150);
            var view = Matrix4.LookAt((float)x, (float)y, (float)z, (float)(x + fx * 100), (float)(y + fy * 100), (float)(z + fz * 100), 0, 0, 1);
            var projection = Matrix4.CreatePerspectiveFieldOfView((float)(Math.PI / 3), 1.8f, 1f, (float)(radius * 1.05));
            var p = new SelectionParams
            {
                CameraX = x, CameraY = y, CameraZ = z, ForwardX = fx, ForwardY = fy, ForwardZ = fz,
                CameraLat = lat, CameraLng = lng,
                Frustum = Frustum.FromViewProjection(Matrix4.Mult(view, projection)),
                ViewRadius = radius, ViewportHeight = 520, FovY = Math.PI / 3, MaxZoom = 18, ReferenceHeight = ground
            };

            var ahead = frame.ToLatLng(x, y + 600, 0);
            var watch = Stopwatch.StartNew();
            long firstCover = -1, fineCover = -1, done = -1;
            int fetched = 0, failed = 0;

            using (var loader = new TileLoader(imagery, TileLoader.DefaultWorkerCount, (a, b, h) => DemStatus.Ready))
            {
                while (watch.ElapsedMilliseconds < 60000)
                {
                    TileLoadResult r;
                    while (loader.TryGetResult(out r))
                    {
                        var tile = tree.GetOrCreate(r.Id);
                        if (r.Outcome == TileLoadOutcome.Loaded)
                        {
                            fetched++;
                            tile.State = TileState.Ready;
                            tile.HeightsKnown = true;
                            tile.MinHeight = r.Mesh.MinHeight;
                            tile.MaxHeight = r.Mesh.MaxHeight;
                            tile.Heights = r.Mesh.Heights;
                            tile.GridN = r.Mesh.GridN;
                        }
                        else if (r.Outcome == TileLoadOutcome.NoData)
                        {
                            tile.State = TileState.NoData;
                            tile.RetryAtTicks = long.MaxValue;
                        }
                        else
                        {
                            failed++;
                            tile.State = TileState.Failed;
                            tile.RetryAtTicks = DateTime.UtcNow.AddSeconds(2).Ticks;
                            Console.WriteLine("failed " + r.Id + " " + r.Outcome + " " + r.Error);
                        }
                    }

                    tree.Select(p, DateTime.UtcNow.Ticks);
                    loader.SetRequests(tree.Requests);

                    var cover = tree.DrawList.FirstOrDefault(t => Contains(t.Id, ahead.Lat, ahead.Lng));
                    if (cover != null && firstCover < 0)
                        firstCover = watch.ElapsedMilliseconds;
                    if (cover != null && cover.Id.Z >= 16 && fineCover < 0)
                        fineCover = watch.ElapsedMilliseconds;
                    if (tree.Requests.Count == 0 && loader.InFlightCount == 0)
                    {
                        done = watch.ElapsedMilliseconds;
                        break;
                    }

                    Thread.Sleep(16);
                }

                Console.WriteLine("{0:0.000},{1:0.000}: first coverage {2} ms, z16+ at view centre {3} ms, all detail {4} ms, {5} tiles fetched ({6:0} tiles/s), {7} failed, {8} drawn, zooms {9}",
                    lat, lng, firstCover, fineCover, done, fetched, fetched / (watch.ElapsedMilliseconds / 1000.0), failed,
                    tree.DrawList.Count, string.Join(",", tree.DrawList.GroupBy(t => t.Id.Z).OrderBy(g => g.Key).Select(g => g.Key + ":" + g.Count())));
            }
        }

        static bool Contains(TileId tile, double lat, double lng)
        {
            double x, y;
            TileMath.LatLngToTile(lat, lng, tile.Z, out x, out y);
            return (int)Math.Floor(x) == tile.X && (int)Math.Floor(y) == tile.Y;
        }
    }
}
