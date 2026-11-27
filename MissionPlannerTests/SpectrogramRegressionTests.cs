using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MissionPlanner.Controls;
using MissionPlanner.Utilities;
using SkiaSharp;

namespace MissionPlannerTests
{
    [TestClass]
    public class SpectrogramRegressionTests
    {
        [TestMethod]
        public void SpectrogramPreservesOpaqueRainbowColors()
        {
            Assert.AreEqual(new SKColor(191, 64, 64, 255), Spectrogram.HSL2RGB(0, 0.5, 0.5));
            Assert.AreEqual(new SKColor(64, 191, 64, 255), Spectrogram.HSL2RGB(1.0 / 3, 0.5, 0.5));
            Assert.AreEqual(new SKColor(64, 64, 191, 255), Spectrogram.HSL2RGB(2.0 / 3, 0.5, 0.5));
        }

        [TestMethod]
        public void GraphBitmapRetainsPixelsAfterNativeImageAndStreamAreDisposed()
        {
            Bitmap display;
            using (var image = new SKBitmap(3, 2))
            {
                image.Erase(SKColors.Transparent);
                image.SetPixel(0, 0, SKColors.Red);
                image.SetPixel(2, 1, SKColors.Blue);
                display = image.ToSpectrogramBitmap();
            }
            using (display)
            using (var saved = new MemoryStream())
            {
                Assert.AreEqual(3, display.Width);
                Assert.AreEqual(2, display.Height);
                Assert.AreEqual(Color.Red.ToArgb(), display.GetPixel(0, 0).ToArgb());
                Assert.AreEqual(Color.Blue.ToArgb(), display.GetPixel(2, 1).ToArgb());
                Assert.AreEqual(0, display.GetPixel(1, 0).A);
                display.Save(saved, System.Drawing.Imaging.ImageFormat.Png);
                Assert.IsTrue(saved.Length > 0);
            }
        }

        [DataTestMethod]
        [DataRow(2048, "GYR1", "GyrX")]
        [DataRow(1023, "ACC1", "AccX")]
        public void MissingOrShortSelectedSensorDoesNotGenerateImage(int samples, string sensor, string axis)
        {
            var text = new StringBuilder("FMT, 150, 23, ACC1, Qfff, TimeUS, AccX, AccY, AccZ\n");
            for (int i = 0; i < samples; i++)
                text.AppendFormat(CultureInfo.InvariantCulture, "ACC1, {0}, 1, 0, 0\n", 1000000L + i * 1000L);

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(text.ToString())))
            using (var log = new DFLogBuffer(stream))
            using (var image = Spectrogram.GenerateImage(log, out var frequencies, out var data, sensor, axis))
            {
                Assert.AreEqual(samples, log.GetEnumeratorType("ACC1").Count());
                Assert.IsNull(image);
                Assert.AreEqual(0, frequencies.Length);
                Assert.AreEqual(0, data.Count);
            }
        }

        [TestMethod]
        public void SensorLogRendersFrequencyBinsAndTransparentUnusedColumns()
        {
            var path = Path.Combine(Path.GetTempPath(), "TitanPlanner-spectrum-" + Guid.NewGuid() + ".log");
            try
            {
                var text = new StringBuilder("FMT, 150, 23, ACC1, Qfff, TimeUS, AccX, AccY, AccZ\n");
                for (int i = 0; i < 2048; i++)
                    text.AppendFormat(CultureInfo.InvariantCulture, "ACC1, {0}, {1:R}, 0, 0\n",
                        1000000L + i * 1000L, Math.Sin(2 * Math.PI * 64 * i / 1024));
                File.WriteAllText(path, text.ToString());
                using (var log = new DFLogBuffer(path))
                using (var image = Spectrogram.GenerateImage(log, out var frequencies, out var data))
                {
                    Assert.AreEqual(8, image.Width);
                    Assert.AreEqual(512, image.Height);
                    Assert.AreEqual(512, frequencies.Length);
                    Assert.AreEqual(5, data.Count);
                    Assert.AreEqual(1000000.0, data[0].timeus);
                    Assert.AreEqual(64, Array.IndexOf(data[0].value, data[0].value.Max()));
                    Assert.AreEqual(new SKColor(191, 64, 64, 255), image.GetPixel(0, 511 - 64));
                    Assert.AreEqual((byte)255, image.GetPixel(4, 511).Alpha);
                    Assert.AreEqual((byte)0, image.GetPixel(7, 0).Alpha);
                }
            }
            finally { File.Delete(path); }
        }
    }
}
