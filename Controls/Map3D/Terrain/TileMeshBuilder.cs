using System;

namespace MissionPlanner.Controls.Scene3D
{
    /// <summary>
    /// CPU-side geometry for one terrain tile, ready to upload.
    /// </summary>
    public sealed class TileMesh
    {
        /// <summary>x, y, z, u, v per vertex. x/y are metres from (OriginEasting, OriginNorthing); z is AMSL.</summary>
        public float[] Vertices;

        public int GridN;

        /// <summary>(GridN + 1)^2 heights, row-major from the north-west corner.</summary>
        public float[] Heights;

        public float MinHeight;
        public float MaxHeight;
        public int Zone;
        public double OriginEasting;
        public double OriginNorthing;
        public double MinEasting, MaxEasting, MinNorthing, MaxNorthing;
    }

    /// <summary>
    /// Builds a regular (N+1) x (N+1) height grid per tile, plus skirts that hang below the edges to hide
    /// cracks between neighbours at different levels of detail. All tiles with the same N share one index
    /// buffer, so a tile only needs a small vertex buffer.
    /// </summary>
    public static class TileMeshBuilder
    {
        public const int FloatsPerVertex = 5;
        public const int MinGrid = 4;
        public const int MaxGrid = 32;

        /// <summary>Elevation data is ~30 m (SRTM1) at best; finer grids add vertices without detail.</summary>
        public const double DemResolutionMeters = 30;

        /// <summary>Grid cells per tile edge: about one per DEM sample, as a power of two in [4, 32].</summary>
        public static int GridSizeFor(TileId id)
        {
            double tileMeters = TileMath.TileSizeMeters(TileMath.CenterLat(id), id.Z);
            int n = MinGrid;
            while (n < MaxGrid && tileMeters / n > DemResolutionMeters)
                n *= 2;
            return n;
        }

        public static int VertexCount(int gridN)
        {
            return (gridN + 1) * (gridN + 1) + 4 * (gridN + 1);
        }

        public static int IndexCount(int gridN)
        {
            return gridN * gridN * 6 + 4 * gridN * 6;
        }

        /// <summary>Latitudes of the grid rows (north to south) and longitudes of the columns (west to east).</summary>
        public static void GridLatLngs(TileId id, int gridN, out double[] rowLats, out double[] colLngs)
        {
            rowLats = new double[gridN + 1];
            colLngs = new double[gridN + 1];
            for (int k = 0; k <= gridN; k++)
            {
                rowLats[k] = TileMath.TileYToLat(id.Y + (double)k / gridN, id.Z);
                colLngs[k] = TileMath.TileXToLng(id.X + (double)k / gridN, id.Z);
            }
        }

        public static TileMesh Build(TileId id, int gridN, double[] rowLats, double[] colLngs, float[] heights, int zone)
        {
            int cols = gridN + 1;
            int gridVerts = cols * cols;

            var mesh = new TileMesh
            {
                GridN = gridN,
                Heights = heights,
                Zone = zone,
                Vertices = new float[VertexCount(gridN) * FloatsPerVertex]
            };

            UtmProjection.Forward(TileMath.CenterLat(id), TileMath.CenterLng(id), zone,
                out mesh.OriginEasting, out mesh.OriginNorthing);

            var eastings = new double[gridVerts];
            var northings = new double[gridVerts];
            double minE = double.MaxValue, maxE = double.MinValue, minN = double.MaxValue, maxN = double.MinValue;
            float minH = float.MaxValue, maxH = float.MinValue;

            for (int j = 0; j < cols; j++)
            {
                for (int i = 0; i < cols; i++)
                {
                    int k = j * cols + i;
                    UtmProjection.Forward(rowLats[j], colLngs[i], zone, out eastings[k], out northings[k]);
                    minE = Math.Min(minE, eastings[k]);
                    maxE = Math.Max(maxE, eastings[k]);
                    minN = Math.Min(minN, northings[k]);
                    maxN = Math.Max(maxN, northings[k]);
                    minH = Math.Min(minH, heights[k]);
                    maxH = Math.Max(maxH, heights[k]);
                }
            }

            mesh.MinEasting = minE;
            mesh.MaxEasting = maxE;
            mesh.MinNorthing = minN;
            mesh.MaxNorthing = maxN;

            double cellMeters = (maxE - minE) / gridN;
            float skirt = SkirtDepth(cellMeters, minH, maxH);
            mesh.MinHeight = minH - skirt;
            mesh.MaxHeight = maxH;

            var v = mesh.Vertices;
            int o = 0;
            for (int j = 0; j < cols; j++)
            {
                for (int i = 0; i < cols; i++)
                {
                    int k = j * cols + i;
                    o = Write(v, o, eastings[k] - mesh.OriginEasting, northings[k] - mesh.OriginNorthing, heights[k],
                        (float)i / gridN, (float)j / gridN);
                }
            }

            // Skirts: north, south, west, east edges, each a copy of the edge dropped by the skirt depth.
            for (int edge = 0; edge < 4; edge++)
            {
                for (int s = 0; s < cols; s++)
                {
                    int i, j;
                    EdgeCell(edge, s, gridN, out i, out j);
                    int k = j * cols + i;
                    o = Write(v, o, eastings[k] - mesh.OriginEasting, northings[k] - mesh.OriginNorthing,
                        heights[k] - skirt, (float)i / gridN, (float)j / gridN);
                }
            }

            return mesh;
        }

        static int Write(float[] v, int o, double x, double y, double z, float u, float t)
        {
            v[o++] = (float)x;
            v[o++] = (float)y;
            v[o++] = (float)z;
            v[o++] = u;
            v[o++] = t;
            return o;
        }

        /// <summary>Grid coordinates of step s along an edge (0 north, 1 south, 2 west, 3 east).</summary>
        static void EdgeCell(int edge, int s, int gridN, out int i, out int j)
        {
            switch (edge)
            {
                case 0: i = s; j = 0; break;
                case 1: i = s; j = gridN; break;
                case 2: i = 0; j = s; break;
                default: i = gridN; j = s; break;
            }
        }

        /// <summary>
        /// Deep enough to cover the largest crack a coarser neighbour can leave: about half a cell of slope
        /// plus a margin for rugged tiles.
        /// </summary>
        public static float SkirtDepth(double cellMeters, float minH, float maxH)
        {
            return (float)Math.Max(5.0, cellMeters * 0.5 + (maxH - minH) * 0.02);
        }

        /// <summary>Triangle indices shared by every tile with this grid size.</summary>
        public static ushort[] BuildIndices(int gridN)
        {
            int cols = gridN + 1;
            var indices = new ushort[IndexCount(gridN)];
            int o = 0;

            for (int j = 0; j < gridN; j++)
            {
                for (int i = 0; i < gridN; i++)
                {
                    int a = j * cols + i;
                    int b = a + 1;
                    int c = a + cols;
                    int d = c + 1;
                    indices[o++] = (ushort)a;
                    indices[o++] = (ushort)c;
                    indices[o++] = (ushort)b;
                    indices[o++] = (ushort)b;
                    indices[o++] = (ushort)c;
                    indices[o++] = (ushort)d;
                }
            }

            int skirtBase = cols * cols;
            for (int edge = 0; edge < 4; edge++)
            {
                for (int s = 0; s < gridN; s++)
                {
                    int i0, j0, i1, j1;
                    EdgeCell(edge, s, gridN, out i0, out j0);
                    EdgeCell(edge, s + 1, gridN, out i1, out j1);
                    int top0 = j0 * cols + i0;
                    int top1 = j1 * cols + i1;
                    int low0 = skirtBase + edge * cols + s;
                    int low1 = low0 + 1;
                    indices[o++] = (ushort)top0;
                    indices[o++] = (ushort)low0;
                    indices[o++] = (ushort)top1;
                    indices[o++] = (ushort)top1;
                    indices[o++] = (ushort)low0;
                    indices[o++] = (ushort)low1;
                }
            }

            return indices;
        }

        /// <summary>
        /// Approximate absolute UTM bounds of a tile before it is loaded, from a 3x3 sample of its corners and
        /// edge midpoints.
        /// </summary>
        public static void EstimateBounds(TileId id, int zone, out double minE, out double maxE, out double minN, out double maxN)
        {
            minE = double.MaxValue;
            maxE = double.MinValue;
            minN = double.MaxValue;
            maxN = double.MinValue;
            for (int j = 0; j <= 2; j++)
            {
                double lat = TileMath.TileYToLat(id.Y + j * 0.5, id.Z);
                for (int i = 0; i <= 2; i++)
                {
                    double lng = TileMath.TileXToLng(id.X + i * 0.5, id.Z);
                    double e, n;
                    UtmProjection.Forward(lat, lng, zone, out e, out n);
                    minE = Math.Min(minE, e);
                    maxE = Math.Max(maxE, e);
                    minN = Math.Min(minN, n);
                    maxN = Math.Max(maxN, n);
                }
            }
        }
    }
}
