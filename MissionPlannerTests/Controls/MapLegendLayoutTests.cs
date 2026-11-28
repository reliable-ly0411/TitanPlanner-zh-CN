using System;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MissionPlanner.Controls;
using MissionPlanner.Utilities;

namespace MissionPlannerTests.Controls
{
    [TestClass]
    public class MapLegendLayoutTests
    {
        [TestMethod]
        public void EnhancedLegendKeepsRowsSeparateAndWithinTheMap()
        {
            var prior = CultureInfo.CurrentUICulture;
            try
            {
                var legend = new MapLegendButton();
                legend.Toggle();
                var layout = typeof(MapLegendButton).GetMethod("LegendLayout", BindingFlags.Static | BindingFlags.NonPublic);
                var titles = new[] { "Current Heading", "Direction to WP", "Target Heading", "Ground Track" };
                var colors = new[] { Color.Red, Color.Orange, Color.Green, Color.Black };
                var translations = new[] { "当前航向", "航点方位", "目标航向", "地面航迹" };
                foreach (var locale in new[] { "zh-CN", "zh-Hans", "en-US", "zh-TW" })
                foreach (int dpi in new[] { 96, 144, 192 })
                foreach (int width in new[] { 96, 185, 360 })
                {
                    CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(locale);
                    bool enhanced = locale == "zh-CN";
                    var entries = new (string Text, Color Color)[titles.Length];
                    for (int i = 0; i < entries.Length; i++)
                    {
                        entries[i] = (MapGpsPresentation.LegendLabel(titles[i]), colors[i]);
                        Assert.AreEqual(enhanced ? translations[i] : titles[i], entries[i].Text);
                    }
                    var size = new Size(width, 250);
                    using (var bitmap = new Bitmap(size.Width, size.Height))
                    {
                        bitmap.SetResolution(dpi, dpi);
                        using (var graphics = Graphics.FromImage(bitmap))
                        using (var font = new Font("Segoe UI", 9f))
                        {
                            var button = legend.Bounds(size);
                            var result = (ValueTuple<RectangleF, float>)layout.Invoke(null,
                                new object[] { graphics, button, entries, enhanced });
                            var (panel, rowHeight) = result;
                            Assert.AreEqual((float)button.Right, panel.Right, 0.1f);
                            if (!enhanced)
                            {
                                Assert.AreEqual(20f, rowHeight);
                                Assert.AreEqual(96f, panel.Height);
                                continue;
                            }

                            Assert.IsTrue(panel.Left >= 7.9f && panel.Top >= 0f);
                            Assert.IsTrue(panel.Bottom < button.Top);
                            Assert.AreEqual(rowHeight * 4 + 16, panel.Height, 0.1f);
                            foreach (var entry in entries)
                                Assert.IsTrue(rowHeight >= graphics.MeasureString(entry.Text, font).Height);
                            legend.Draw(graphics, size);

                            // Verify all four original color swatches were actually rendered.
                            for (int i = 0; i < colors.Length; i++)
                            {
                                int sampleX = (int)Math.Round(panel.Left + 19);
                                int sampleY = (int)Math.Round(panel.Top + 8 + (i + 0.5f) * rowHeight);
                                Assert.AreEqual(colors[i].ToArgb(), bitmap.GetPixel(sampleX, sampleY).ToArgb());
                            }
                        }
                    }
                }
            }
            finally { CultureInfo.CurrentUICulture = prior; }
        }
    }
}
