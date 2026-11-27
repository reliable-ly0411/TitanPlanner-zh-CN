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

        [DataTestMethod]
        [DataRow(32, "GYR1", "GyrX")]
        [DataRow(31, "ACC1", "AccX")]
        public void MissingOrIncompleteSelectedBatchDoesNotGenerateImage(int packets, string sensor, string axis)
        {
            using (var log = new DFLogBuffer(new MemoryStream(SyntheticBatch.Make(packets))))
            using (var image = Spectrogram.GenerateImage(log, out var frequencies, out var data, sensor, axis))
            {
                Assert.AreEqual(packets, log.GetEnumeratorType("ISBD").Count());
                Assert.IsNull(image);
                Assert.AreEqual(0, frequencies.Length);
                Assert.AreEqual(0, data.Count);
            }
        }

        [TestMethod]
        public void CompleteSelectedBatchRetainsFrequencyAndImage()
        {
            using (var log = new DFLogBuffer(new MemoryStream(SyntheticBatch.Make(32))))
            using (var image = Spectrogram.GenerateImage(log, out var frequencies, out var data, "ACC1", "AccX"))
            {
                Assert.IsNotNull(image);
                Assert.AreEqual(1, image.Width);
                Assert.AreEqual(512, image.Height);
                Assert.AreEqual(1, data.Count);
                Assert.AreEqual(64.0, frequencies[64]);
                Assert.AreEqual(64, Array.IndexOf(data[0].value, data[0].value.Max()));
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

    internal static class SyntheticBatch
    {
        // Canonical packed ISBH/ISBD field order: ArduPilot Copter-4.6.3
        // libraries/AP_InertialSensor/LogStructure.h. IDs below are deliberately local
        // to each fixture and declared using DataFlash FMT, not assumed from hardware.
        private const byte FmtId=128, HeaderId=201, DataId=202;
        private const ushort BatchId=1;
        internal const int Rate=1024;

        private static void Packet(BinaryWriter writer, byte id) { writer.Write((byte)0xa3); writer.Write((byte)0x95); writer.Write(id); }
        private static void Padded(BinaryWriter writer,string value,int length)
        {
            var bytes=Encoding.ASCII.GetBytes(value);
            if(bytes.Length>length) throw new InvalidOperationException("Fixture FMT field overflow");
            writer.Write(bytes); writer.Write(new byte[length-bytes.Length]);
        }

        private static void Fmt(BinaryWriter writer,byte type,byte length,string name,string format,string columns)
        {
            Packet(writer,FmtId); writer.Write(type); writer.Write(length);
            Padded(writer,name,4); Padded(writer,format,16); Padded(writer,columns,64);
        }

        internal static byte[] Make(int packets)
        {
            if(packets!=31 && packets!=32) throw new ArgumentOutOfRangeException(nameof(packets));
            using(var stream=new MemoryStream())
            using(var writer=new BinaryWriter(stream))
            {
                Fmt(writer,FmtId,89,"FMT","BBnNZ","Type,Length,Name,Format,Columns");
                Fmt(writer,HeaderId,31,"ISBH","QHBBHHQf","TimeUS,N,type,instance,mul,smp_cnt,SampleUS,smp_rate");
                Fmt(writer,DataId,207,"ISBD","QHHaaa","TimeUS,N,seqno,x,y,z");
                Packet(writer,HeaderId);
                writer.Write(1000000UL); writer.Write(BatchId); writer.Write((byte)0); writer.Write((byte)0);
                writer.Write((ushort)128); writer.Write((ushort)1024); writer.Write(1000000UL); writer.Write((float)Rate);
                int[] periods = {16, 8, 4};
                for(int k=0;k<packets;k++)
                {
                    Packet(writer,DataId);
                    writer.Write(1001000UL+(ulong)k*31250UL); writer.Write(BatchId); writer.Write((ushort)k);
                    foreach(int period in periods)
                        for(int j=0;j<32;j++)
                        {
                            // A deterministic integer square wave; no floating rounding or true sensor data.
                            int index=k*32+j;
                            writer.Write((short)((index%period)<period/2 ? 256 : -256));
                        }
                }
                writer.Flush();
                return stream.ToArray();
            }
        }
    }
}
