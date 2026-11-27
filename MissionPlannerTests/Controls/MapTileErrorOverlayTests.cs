using GMap.NET;
using GMap.NET.Internals;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SvgNet.SvgGdi;
using System;
using System.Drawing;
using System.Globalization;
using System.Reflection;

namespace MissionPlannerTests.Controls
{
    [TestClass]
    public class MapTileErrorOverlayTests
    {
        private const string EnglishImagery =
            "We are sorry, but we don't\nhave imagery at this zoom\nlevel for this region.";
        private const string ChineseImagery = "此区域在当前缩放级别\n暂无地图影像。";
        private const string ChineseCacheMiss = "瓦片加载失败：本地缓存中没有此瓦片。";

        [DataTestMethod]
        [DataRow("zh-CN", "No data in cache", null, ChineseCacheMiss, ChineseImagery)]
        [DataRow("zh-CN", "No data in local tile cache...", null, ChineseCacheMiss, ChineseImagery)]
        [DataRow("zh-CN", "Provider detail 503", "Caller text", "瓦片加载失败：Provider detail 503", "Caller text")]
        [DataRow("zh-Hans", "No data in local tile cache...", null, "Exception: No data in local tile cache...", EnglishImagery)]
        [DataRow("en-US", "No data in cache", null, "Exception: No data in cache", EnglishImagery)]
        public void DrawMapPaintsLocalizedFailedTileWithoutChangingItsException(string culture, string diagnostic,
            string customText, string expectedError, string expectedImagery)
        {
            var previousCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
                using (var map = new GMapControl { FillEmptyTiles = false, ShowTileGridLines = false })
                using (var actual = new Bitmap(256, 256))
                using (var expected = new Bitmap(256, 256))
                using (var actualGraphics = Graphics.FromImage(actual))
                using (var expectedGraphics = Graphics.FromImage(expected))
                using (var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (var font = new Font(FontFamily.GenericSansSerif, 11, FontStyle.Bold))
                using (var background = new SolidBrush(Color.Black))
                {
                    if (customText != null)
                        map.EmptyTileText = customText;
                    map.EmptyTileColor = Color.Black;
                    // No handle, map startup, cache writes or provider requests: seed one failed tile.
                    map.Core.provider = OfflineTileProvider.Instance;
                    map.Core.tileRect = new GRect(0, 0, 256, 256);
                    var position = new GPoint(1, 1);
                    var task = new LoadTask(position, map.Core.Zoom);
                    var failure = new Exception(diagnostic);
                    map.Core.tileDrawingList.Add(new DrawTile { PosXY = position, PosPixel = GPoint.Empty });
                    map.Core.FailedLoads.Add(task, failure);

                    var drawMap = typeof(GMapControl).GetMethod("DrawMap", BindingFlags.Instance | BindingFlags.NonPublic);
                    Assert.IsNotNull(drawMap);
                    drawMap.Invoke(map, new object[] { new GdiGraphics(actualGraphics) });

                    expectedGraphics.FillRectangle(background, new RectangleF(0, 0, 256, 256));
                    expectedGraphics.DrawString(expectedError, font, Brushes.Red, new RectangleF(11, 11, 245, 245));
                    expectedGraphics.DrawString(expectedImagery, font, Brushes.Blue, new RectangleF(0, 0, 256, 256), center);
                    expectedGraphics.DrawRectangle(map.EmptyTileBorders, 0, 0, 256, 256);

                    int redPixels = 0, bluePixels = 0;
                    for (int y = 0; y < actual.Height; y++)
                    for (int x = 0; x < actual.Width; x++)
                    {
                        var pixel = actual.GetPixel(x, y);
                        if (pixel.R > pixel.B) redPixels++;
                        if (pixel.B > pixel.R) bluePixels++;
                        if (expected.GetPixel(x, y).ToArgb() != pixel.ToArgb())
                            Assert.Fail("Unexpected overlay pixel at " + x + "," + y + " in " + culture);
                    }
                    Assert.IsTrue(redPixels > 0 && bluePixels > 0, "Both diagnostics must be drawn");
                    Assert.AreSame(failure, map.Core.FailedLoads[task]);
                    Assert.AreEqual(diagnostic, failure.Message);
                }
            }
            finally
            {
                CultureInfo.CurrentUICulture = previousCulture;
            }
        }

        private sealed class OfflineTileProvider : GMapProvider
        {
            public static readonly OfflineTileProvider Instance = new OfflineTileProvider();
            public override Guid Id { get; } = new Guid("eb92af77-9efc-4a9c-8cb0-763fb13473e6");
            public override string Name => "Offline tile overlay fixture";
            public override PureProjection Projection => GMap.NET.Projections.MercatorProjection.Instance;
            public override GMapProvider[] Overlays => new GMapProvider[] { this };
            public override PureImage GetTileImage(GPoint pos, int zoom)
                => throw new InvalidOperationException("The rendering fixture must not fetch a tile.");
        }
    }
}
