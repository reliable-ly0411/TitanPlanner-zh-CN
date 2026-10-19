namespace MissionPlanner.Controls.Scene3D
{
    /// <summary>
    /// A snapshot of the terrain engine's state for the "Stats for nerds" panel, refreshed every frame.
    /// </summary>
    public sealed class TerrainStats
    {
        public const int ZoomLevels = 21;

        public int Drawn;
        public int Resident;
        public int Nodes;
        public int Queued;
        public int InFlight;
        public int Workers;
        public int Failed;
        public int NoData;
        public int Offline;

        /// <summary>Tiles drawn with approximate heights while their elevation data downloads.</summary>
        public int WaitingForElevation;

        public long GpuBytes;
        public long GpuBudgetBytes;

        /// <summary>Drawn tiles per zoom level.</summary>
        public readonly int[] ZoomCounts = new int[ZoomLevels];
        public int MinZoom;
        public int MaxZoom;

        public long LoadedTotal;
        public double LoadedPerSecond;

        /// <summary>Smoothed imagery download/decode time per tile.</summary>
        public double FetchMs;

        /// <summary>Smoothed elevation sampling and mesh building time per tile.</summary>
        public double BuildMs;

        /// <summary>Time spent uploading finished tiles this frame.</summary>
        public double UploadMs;

        public string LastError;
    }
}
