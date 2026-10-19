using System;

namespace MissionPlanner.Controls.Scene3D
{
    /// <summary>
    /// Recent frame times for the "Stats for nerds" panel and its graph: the interval between frames and the
    /// CPU time spent building each one.
    /// </summary>
    public sealed class FrameStats
    {
        readonly float[] _intervals;
        readonly float[] _cpu;
        int _next;

        public FrameStats(int capacity)
        {
            _intervals = new float[capacity];
            _cpu = new float[capacity];
        }

        public int Capacity
        {
            get { return _intervals.Length; }
        }

        /// <summary>Samples recorded so far, up to <see cref="Capacity"/>.</summary>
        public int Count { get; private set; }

        public void Add(double intervalMs, double cpuMs)
        {
            _intervals[_next] = (float)intervalMs;
            _cpu[_next] = (float)cpuMs;
            _next = (_next + 1) % _intervals.Length;
            Count = Math.Min(Count + 1, _intervals.Length);
        }

        /// <summary>Frame interval of the i-th sample, oldest first.</summary>
        public float Interval(int i)
        {
            return _intervals[Index(i)];
        }

        /// <summary>CPU time of the i-th sample, oldest first.</summary>
        public float Cpu(int i)
        {
            return _cpu[Index(i)];
        }

        int Index(int i)
        {
            return (_next - Count + i + _intervals.Length) % _intervals.Length;
        }

        /// <summary>Frames per second over the most recent samples.</summary>
        public double CurrentFps(int samples = 30)
        {
            return 1000.0 / Math.Max(0.001, MeanInterval(Math.Min(samples, Count)));
        }

        /// <summary>Frames per second over everything recorded.</summary>
        public double AverageFps()
        {
            return 1000.0 / Math.Max(0.001, MeanInterval(Count));
        }

        /// <summary>The frame rate implied by the slowest recorded frame.</summary>
        public double LowFps()
        {
            float worst = 0;
            for (int i = 0; i < Count; i++)
                worst = Math.Max(worst, Interval(i));
            return 1000.0 / Math.Max(0.001, worst);
        }

        public double MeanCpu(int samples = 30)
        {
            int n = Math.Min(samples, Count);
            if (n == 0)
                return 0;
            double sum = 0;
            for (int i = Count - n; i < Count; i++)
                sum += Cpu(i);
            return sum / n;
        }

        double MeanInterval(int samples)
        {
            if (samples == 0)
                return 0;
            double sum = 0;
            for (int i = Count - samples; i < Count; i++)
                sum += Interval(i);
            return sum / samples;
        }
    }
}
