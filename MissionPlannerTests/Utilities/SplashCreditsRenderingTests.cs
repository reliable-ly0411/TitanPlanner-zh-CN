using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MissionPlannerTests.Utilities
{
    [TestClass]
    public class SplashCreditsRenderingTests
    {
        [TestMethod]
        public void SplashCreditsStrokeDoesNotPaintABox()
        {
            Exception failure = null;
            var worker = new Thread(() =>
            {
                try
                {
                    VerifyAtScale(new Size(660, 400), 96, Color.FromArgb(255, 219, 117), false);
                    VerifyAtScale(new Size(660, 400), 120, Color.FromArgb(79, 38, 64), true);
                    VerifyAtScale(new Size(620, 430), 144, Color.FromArgb(255, 219, 117), true);
                    VerifyAtScale(new Size(620, 430), 144, Color.FromArgb(79, 38, 64), false);
                    // Asset store allows 3840x480. At the production canvas size it fits in an
                    // 82px-tall band: three full credits fit there but not in half of that band.
                    VerifyAtScale(new Size(660, 400), 96, Color.FromArgb(255, 219, 117), false, new Size(3840, 480));
                    VerifyAtScale(new Size(660, 400), 120, Color.FromArgb(79, 38, 64), true, new Size(3840, 480));
                    VerifyAtScale(new Size(660, 400), 144, Color.FromArgb(79, 38, 64), true, new Size(3840, 480));
                    VerifyAtScale(new Size(1320, 800), 192, Color.FromArgb(255, 219, 117), false, new Size(3840, 480));
                    VerifyAtScale(new Size(660, 400), 120, Color.FromArgb(79, 38, 64), true,
                        new Size(3840, 120), expectInArtwork: false);
                    VerifyAtScale(new Size(660, 400), 144, Color.FromArgb(255, 219, 117), false,
                        new Size(4096, 1), expectInArtwork: false);
                }
                catch (Exception ex) { failure = ex; }
            });
            worker.SetApartmentState(ApartmentState.STA);
            worker.Start();
            Assert.IsTrue(worker.Join(TimeSpan.FromSeconds(15)), "Screenshot rendering timed out.");
            if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
        }

        private static string CreditPin(string field)
        {
            var pin = typeof(MissionPlanner.Splash).GetField(field, BindingFlags.Static | BindingFlags.NonPublic);
            return ((string)pin.GetRawConstantValue()).Substring(0, 8);
        }

        private static void VerifyAtScale(Size canvas, float dpi, Color artwork, bool chinese,
            Size? artworkSize = null, bool expectInArtwork = true)
        {
            var draw = typeof(MissionPlanner.Splash).GetMethod("DrawCredits", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(draw);
            string mission = CreditPin("MissionPlannerSource");
            string titan = CreditPin("TitanPlannerSource");
            var lines = chinese
                ? new[] { "Mission Planner 上游基线 " + mission + " · Michael Oborne",
                          "TitanPlanner 上游基线 " + titan + " · Michael Oborne、Mohammad Adib",
                          "修改者：lyy0405" }
                : new[] { "Mission Planner upstream " + mission + " · Michael Oborne",
                          "TitanPlanner upstream " + titan + " · Michael Oborne & Mohammad Adib",
                          "Modified by: lyy0405" };
            Size size = artworkSize ?? new Size(800, 380);
            using (var source = new Bitmap(size.Width, size.Height))
            using (var picture = new PictureBox { Size = canvas, Image = source })
            using (var bitmap = new Bitmap(canvas.Width, canvas.Height))
            {
                bitmap.SetResolution(dpi, dpi);
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.Clear(artwork);
                    object[] arguments = { graphics, picture, lines, null, 0f };
                    var area = (Rectangle)draw.Invoke(null, arguments);
                    var rows = (Rectangle[])arguments[3];
                    Assert.AreEqual(lines.Length, rows.Length);
                    Assert.IsTrue(area.Width > 0 && area.Height > 0 && picture.ClientRectangle.Contains(area),
                        "Credits did not fit the splash canvas.");
                    foreach (var row in rows) Assert.IsTrue(area.Contains(row), "Credit text extends outside its fit rectangle.");
                    if (artworkSize.HasValue)
                    {
                        int bandHeight = (int)(source.Height * (canvas.Width / (float)source.Width));
                        var band = new Rectangle(0, (canvas.Height - bandHeight) / 2, canvas.Width, bandHeight);
                        if (expectInArtwork)
                            Assert.IsTrue(band.Contains(area), "Credit text extends into the letterbox.");
                        else
                            Assert.IsTrue(area.Top >= band.Bottom, "Fallback credits should sit below the artwork.");
                        // Check all three lines, not merely some ink somewhere in the overlay.
                        foreach (var row in rows)
                        {
                            int ink = 0;
                            for (int y = row.Top; y < row.Bottom; y++)
                            for (int x = row.Left; x < row.Right; x++)
                            {
                                Color pixel = bitmap.GetPixel(x, y);
                                if (pixel.R > 200 && pixel.G > 200 && pixel.B > 200 &&
                                    pixel.ToArgb() != artwork.ToArgb()) ink++;
                            }
                            Assert.IsTrue(ink > 12, "A full credit row is missing its fill.");
                        }
                    }
                    int pristine = 0, light = 0, dark = 0, outside = 0;
                    for (int y = 0; y < bitmap.Height; y++)
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        var color = bitmap.GetPixel(x, y);
                        if (!area.Contains(x, y))
                        {
                            if (color.ToArgb() != artwork.ToArgb()) outside++;
                            continue;
                        }
                        if (color.ToArgb() == artwork.ToArgb()) pristine++;
                        else if (color.R > 200 && color.G > 200 && color.B > 200) light++;
                        else if (color.R < 34 && color.G < 34 && color.B < 34) dark++;
                    }
                    Assert.IsTrue(pristine > area.Width * area.Height / 2, "Credits obscured the artwork behind the letters.");
                    Assert.IsTrue(light > 12 && dark > 12, "Expected both light glyphs and a dark outline.");
                    Assert.AreEqual(0, outside, "Credits modified pixels outside the artwork overlay area.");
                }
            }
        }
    }
}
