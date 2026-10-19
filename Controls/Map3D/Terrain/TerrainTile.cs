namespace MissionPlanner.Controls.Scene3D
{
    public enum TileState
    {
        /// <summary>Not on the GPU (never loaded, or evicted).</summary>
        Unloaded,

        /// <summary>Texture and mesh are on the GPU.</summary>
        Ready,

        /// <summary>The last load failed; retried after RetryAtTicks.</summary>
        Failed,

        /// <summary>The imagery provider has nothing for this tile, so it has nothing deeper either.</summary>
        NoData,

        /// <summary>
        /// Not cached while offline. Deeper tiles may still be cached (offline map packs often start at a
        /// higher zoom), so this does not stop refinement.
        /// </summary>
        Unavailable
    }

    /// <summary>
    /// One node of the terrain quadtree. Owned by the render thread; workers only ever see TileRequests.
    /// </summary>
    public sealed class TerrainTile
    {
        public readonly TileId Id;

        public TileState State;

        /// <summary>Absolute UTM bounds, valid while BoundsZoneVersion matches the world frame.</summary>
        public int BoundsZoneVersion = -1;
        public double MinEasting, MaxEasting, MinNorthing, MaxNorthing;

        /// <summary>Height range including skirts, known once a mesh has been built.</summary>
        public bool HeightsKnown;
        public float MinHeight, MaxHeight;

        /// <summary>The last built height grid, kept so children can start from it while their data loads.</summary>
        public float[] Heights;
        public int GridN;

        /// <summary>True while the mesh uses approximate heights because elevation data was not ready.</summary>
        public bool HeightsAreFallback;
        public long RemeshAtTicks;

        // GPU resources, managed by TerrainEngine.
        public int TextureId;
        public int VertexBufferId;
        public long GpuBytes;
        public int MeshZoneVersion;
        public double OriginEasting, OriginNorthing;

        public int LastVisitedFrame;
        public int FailCount;
        public long RetryAtTicks;

        public TerrainTile(TileId id)
        {
            Id = id;
        }

        public override string ToString()
        {
            return Id + " " + State;
        }
    }

    public enum TileJobKind
    {
        /// <summary>Fetch imagery, sample elevation and build the mesh.</summary>
        Full,

        /// <summary>Rebuild the mesh only, once elevation data that was missing has arrived.</summary>
        Remesh
    }

    /// <summary>
    /// Work for the loader: immutable once queued.
    /// </summary>
    public sealed class TileRequest
    {
        public TileId Id;
        public TileJobKind Kind;
        public double Priority;
        public int Zone;
        public int ZoneVersion;
        public int GridN;

        /// <summary>Heights to approximate from while elevation data is missing (an ancestor's grid), or null.</summary>
        public float[] FallbackHeights;
        public TileId FallbackSource;
        public int FallbackGridN;

        /// <summary>Flat height used when there is no ancestor grid either.</summary>
        public float FallbackHeight;
    }

    public enum TileLoadOutcome
    {
        Loaded,
        NoData,
        Offline,
        Failed,

        /// <summary>A remesh found elevation data still missing.</summary>
        StillPending
    }

    public sealed class TileLoadResult
    {
        public TileId Id;
        public TileJobKind Kind;
        public TileLoadOutcome Outcome;
        public int ZoneVersion;

        /// <summary>BGRA pixels, or null for a remesh.</summary>
        public byte[] Pixels;
        public int Width;
        public int Height;

        public TileMesh Mesh;
        public bool HeightsAreFallback;
        public string Error;

        /// <summary>Imagery fetch and decode time (0 for a remesh).</summary>
        public double FetchMs;

        /// <summary>Elevation sampling and mesh building time.</summary>
        public double BuildMs;
    }
}
