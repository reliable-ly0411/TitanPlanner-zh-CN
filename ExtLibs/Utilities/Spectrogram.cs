using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using SharpKml.Dom;
using SkiaSharp;
using static MissionPlanner.Log.LogOutput;

namespace MissionPlanner.Utilities
{
    public class Spectrogram
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static MemoryCache cache = new MemoryCache(new MemoryCacheOptions() { });

        public static SKBitmap GenerateImage(DFLogBuffer cb, out double[] freqtout,
            out List<(double timeus, double[] value)> allfftdata, string type = "ACC1", string field = "AccX",
            string timeus = "TimeUS", int min = -80, int max = -20)
        {
            return GenerateImage(cb, out freqtout, out allfftdata, out _, type, field, timeus, min, max);
        }

        public static SKBitmap GenerateImage(DFLogBuffer cb, out double[] freqtout,
            out List<(double timeus, double[] value)> allfftdata, out double singleBatchWindowSeconds,
            string type = "ACC1", string field = "AccX", string timeus = "TimeUS", int min = -80, int max = -20)
        {
            singleBatchWindowSeconds = 0;
            Console.WriteLine("GenerateImage " + type + " " + field);
            allfftdata = new List<(double timeus, double[] value)>();

            if (cb.SeenMessageTypes.Contains("ISBH"))
            {
                Console.WriteLine("GenerateImage seen ISBH");
                int sensorno = (type.Contains("1") ? 0 : (type.Contains("2") ? 1 : 2));
                int sensor = type.Contains("ACC") ? 0 : 1;

                (double, (double, double)[], double?) cachedata;
                const int bins = 10;
                int N = 1 << bins;

                double sample_rate = -1;
                double? firstWindowRate = null;

                (double time, double d)[] data = new (double time, double d)[0];
                if (!cache.TryGetValue(cb.GetHashCode() + sensorno + "" + sensor, out cachedata))
                {
                    int Ns = -1;
                    int type1 = -1;
                    int instance = -1;
                    double multiplier = -1;
                    int firstWindowSamples = 0;
                    int headerSequence = 0, firstWindowHeader = 0;

                    data = cb.GetEnumeratorType(new string[] { "ISBH", "ISBD" })
                        .SelectMany(
                            item =>
                            {
                                if (item.msgtype == "ISBH")
                                {
                                    headerSequence++;
                                    Ns = int.Parse(item.items[cb.dflog.FindMessageOffset(item.msgtype, "N")],
                                        CultureInfo.InvariantCulture);
                                    type1 = int.Parse(item.items[cb.dflog.FindMessageOffset(item.msgtype, "type")],
                                        CultureInfo.InvariantCulture);
                                    instance = int.Parse(
                                        item.items[cb.dflog.FindMessageOffset(item.msgtype, "instance")],
                                        CultureInfo.InvariantCulture);

                                    if (instance != sensorno || type1 != sensor)
                                        return new (double time, double d)[0];

                                    sample_rate = double.Parse(
                                        item.items[cb.dflog.FindMessageOffset(item.msgtype, "smp_rate")],
                                        CultureInfo.InvariantCulture);

                                    multiplier = double.Parse(
                                        item.items[cb.dflog.FindMessageOffset(item.msgtype, "mul")],
                                        CultureInfo.InvariantCulture);
                                }
                                else if (item.msgtype.StartsWith("ISBD"))
                                {
                                    var Nsdata = int.Parse(item.items[cb.dflog.FindMessageOffset(item.msgtype, "N")],
                                        CultureInfo.InvariantCulture);

                                    if (Ns != Nsdata)
                                        return new (double time, double d)[0];

                                    if (instance != sensorno || type1 != sensor)
                                        return new (double time, double d)[0];

                                    //int offsetX = cb.dflog.FindMessageOffset(item.msgtype, "x");
                                    //int offsetY = cb.dflog.FindMessageOffset(item.msgtype, "y");
                                    //int offsetZ = cb.dflog.FindMessageOffset(item.msgtype, "z");
                                    int offsetTime = cb.dflog.FindMessageOffset(item.msgtype, "TimeUS");

                                    double time = double.Parse(item.items[offsetTime], CultureInfo.InvariantCulture);

                                    var ua = (BinaryLog.UnionArray)item.GetRaw(field.ToLower()
                                        .Substring(field.Length - 1));

                                    int samplesInPacket = ua.Shorts.Length;
                                    if (firstWindowSamples < N && samplesInPacket > 0)
                                    {
                                        // Track only packets that contribute to the first FFT window.
                                        // An incomplete trailing batch must not dictate the painted duration.
                                        if (!firstWindowRate.HasValue)
                                        {
                                            firstWindowRate = sample_rate;
                                            firstWindowHeader = headerSequence;
                                        }
                                        else if (firstWindowHeader != headerSequence || firstWindowRate.Value != sample_rate)
                                            firstWindowRate = double.NaN;
                                        firstWindowSamples += Math.Min(N - firstWindowSamples, samplesInPacket);
                                    }

                                    return ua.Shorts.Select(ds =>
                                        {
                                            double d = ((double)ds / multiplier);
                                            return (time, d);
                                        })
                                        .ToArray();
                                }

                                return new (double time, double d)[0];
                            }).ToArray();
                    cache.Set(cb.GetHashCode() + sensorno + "" + sensor, (sample_rate, data, firstWindowRate),
                        new MemoryCacheEntryOptions()
                        {
                            AbsoluteExpirationRelativeToNow = System.TimeSpan.FromMinutes(10)
                        });
                }
                else 
                {
                    sample_rate = cachedata.Item1;
                    data = cachedata.Item2;
                    firstWindowRate = cachedata.Item3;
                }

                if (data.Length < N)
                {
                    freqtout = Array.Empty<double>();
                    return null;
                }

                var fft = new FFT2();

                var freqt = fft.FreqTable(N, (int)sample_rate);
                
                // time , freq , [color] &freqcount

                double lasttime = 0;
                int totalsamples = data.Count();
                int count = totalsamples / N;
                int done = 0;
                // batch sampling is non continuous
                int divisor = 1;
                count *= divisor;
                var img = new SKBitmap(count, freqt.Length);
                log.Debug("done and count ");

                foreach (var fftdata in data.Windowed(N, divisor))
                {
                    if (fftdata.Count() < N)
                    {
                        break;
                    }

                    var timeusvalue = double.MaxValue;
                    var data1 = new double[N];
                    int c = 0;
                    foreach (var a in fftdata)
                    {
                        timeusvalue = Math.Min(timeusvalue, a.time);
                        data1[c++] = a.d;
                    }

                    var fftanswerz = fft.rin(data1, (uint)bins);

                    allfftdata.Add((timeusvalue, fftanswerz));

                    var i = 0;
                    foreach (var y in freqt)
                    {
                        img.SetPixel(done, (freqt.Length - 1) - i, GetColor(fftanswerz[i], min, max));
                        i++;
                    }

                    count--;
                    done++;
                }

                if (allfftdata.Count == 1 && firstWindowRate.HasValue &&
                    firstWindowRate.Value > 0 && !double.IsInfinity(firstWindowRate.Value))
                {
                    double duration = N / firstWindowRate.Value;
                    if (!double.IsInfinity(duration))
                        singleBatchWindowSeconds = duration;

                    // Only the unique, verified FFT window may override the legacy grid.
                    // A short trailing batch can change sample_rate without contributing any samples to it.
                    double windowRate = firstWindowRate.Value;
                    if (windowRate != sample_rate && windowRate >= 1 &&
                        windowRate <= int.MaxValue / (freqt.Length - 1))
                        freqt = fft.FreqTable(N, (int)windowRate);
                }

                freqtout = freqt;

                return img;
            }

            {
                var bins = 10;
                int N = 1 << bins;

                var fft = new FFT2();

                DFLog.DFItem[] acc1data = null;
                if (!cache.TryGetValue(cb.GetHashCode() + type, out acc1data))
                {
                    acc1data = cb.GetEnumeratorType(type).ToArray();
                    cache.Set(cb.GetHashCode() + type, acc1data,
                        new MemoryCacheEntryOptions()
                        {
                            AbsoluteExpirationRelativeToNow = System.TimeSpan.FromMinutes(10)
                        });
                }
                // The selected sensor may be missing, or may have no complete FFT block.
                if (acc1data.Length < N)
                {
                    freqtout = Array.Empty<double>();
                    return null;
                }

                log.Debug(type);
                if (cb.dflog.FindMessageOffset(acc1data[0].msgtype, "TimeUS") == -1)
                    timeus = "SampleUS";

                int totalsamples = acc1data.Count();
                int count = totalsamples / N;
                int done = 0;
                double timedelta = 0;
                // 50% overlap
                int divisor = 4;
                if (count > 2048)
                    divisor = 1;
                count *= divisor;
                Console.WriteLine("Image Size " + count + " " + N / 2);
                var img = new SKBitmap(count, N / 2);
                log.Debug("done and count ");



                foreach (var fftdata in acc1data.Windowed(N, divisor))
                {
                    if (fftdata.Count() < N)
                    {
                        break;
                    }

                    var timeusvalue = double.MaxValue;
                    var timeusvalueend = double.MinValue;
                    var data = new double[N];
                    int c = 0;
                    foreach (var a in fftdata)
                    {
                        var time = Convert.ToDouble(a.GetRaw(timeus));
                        timeusvalue = Math.Min(timeusvalue, time);
                        timeusvalueend = Math.Max(timeusvalueend, time);
                        data[c++] = Convert.ToDouble(a.GetRaw(field));
                    }

                    timedelta = timedelta * 0.99 + ((timeusvalueend - timeusvalue)) * 0.01;

                    var fftanswerz = fft.rin(data, (uint)bins);

                    allfftdata.Add((timeusvalue, fftanswerz));

                    var i = 0;
                    foreach (var y in Enumerable.Range(0, N/2))
                    {
                        img.SetPixel(done, ((N / 2) - 1) - i, GetColor(fftanswerz[i], min, max));
                        i++;
                    }

                    count--;
                    done++;
                }

                // 1s / (1/ (N/delta))
                var sample_rate = (int)(1000000 / (1.0 / (N / (timedelta))));
                freqtout = fft.FreqTable(N, sample_rate);

                return img;
            }
        }

        static double SCALE = 20 / Math.Log(10);

        static SKColor GetRainbowColor(byte i)
        {
            return HSL2RGB(i / 255.0, 0.5, 0.5);
        }

        public static SKColor HSL2RGB(double h, double sl, double l)
        {
            double v;
            double r, g, b;

            r = l; // default to gray
            g = l;
            b = l;
            v = (l <= 0.5) ? (l * (1.0 + sl)) : (l + sl - l * sl);
            if (v > 0)
            {
                double m;
                double sv;
                int sextant;
                double fract, vsf, mid1, mid2;

                m = l + l - v;
                sv = (v - m) / v;
                h *= 6.0;
                sextant = (int) h;
                fract = h - sextant;
                vsf = v * sv * fract;
                mid1 = m + vsf;
                mid2 = v - vsf;
                switch (sextant)
                {
                    case 0:
                        r = v;
                        g = mid1;
                        b = m;
                        break;
                    case 1:
                        r = mid2;
                        g = v;
                        b = m;
                        break;
                    case 2:
                        r = m;
                        g = v;
                        b = mid1;
                        break;
                    case 3:
                        r = m;
                        g = mid2;
                        b = v;
                        break;
                    case 4:
                        r = mid1;
                        g = m;
                        b = v;
                        break;
                    case 5:
                        r = v;
                        g = m;
                        b = mid2;
                        break;
                }
            }
            return new SKColor(Convert.ToByte(r * 255), Convert.ToByte(g * 255),
                Convert.ToByte(b * 255), 255);
        }

        static SKColor GetColor(double actualValue, int min = -80, int max = -20)
        {
            var scale = actualValue;// SCALE * Math.Log(actualValue + double.Epsilon);

            scale = MathHelper.mapConstrained(scale, min, max, 0, 255);

            return GetRainbowColor((byte) (255-scale));
        }
    }
}