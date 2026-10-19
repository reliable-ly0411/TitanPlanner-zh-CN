using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;

namespace MissionPlanner.Controls.Scene3D
{
    /// <summary>
    /// Imagery through GMap.NET's own tile pipeline: memory cache, then the shared on-disk cache (so tiles
    /// seen on the 2D map and imported .mappack files load instantly, and offline mode works), then the
    /// network. Downloaded tiles are written back to the same cache.
    /// </summary>
    public sealed class GMapImagerySource : ITileImagerySource
    {
        readonly GMapProvider _provider;

        public GMapImagerySource(GMapProvider provider)
        {
            _provider = provider;

            // A map control normally does this when it first uses a provider (Google's looks up its current
            // tile version in the background).
            if (!provider.IsInitialized)
                provider.OnInitialized();
        }

        public ImageryResult Fetch(TileId id)
        {
            Exception error;
            PureImage image = GMaps.Instance.GetImageFrom(_provider, new GPoint(id.X, id.Y), id.Z, out error);
            try
            {
                if (image == null)
                {
                    // Offline mode with nothing cached for this tile.
                    if (error == null || error.Message.StartsWith("No data in local tile cache"))
                        return new ImageryResult { Outcome = TileLoadOutcome.Offline };

                    bool notFound = error.Message.StartsWith("404") || error.Message.Contains("(404)");
                    return new ImageryResult
                    {
                        Outcome = notFound ? TileLoadOutcome.NoData : TileLoadOutcome.Failed,
                        Error = error.Message
                    };
                }

                var result = new ImageryResult { Outcome = TileLoadOutcome.Loaded };
                var decoded = (image as GMapImage)?.Img;
                if (decoded != null)
                {
                    result.Pixels = ToBgra(decoded, out result.Width, out result.Height);
                }
                else if (image.Data != null)
                {
                    image.Data.Position = 0;
                    using (var fromData = Image.FromStream(image.Data))
                        result.Pixels = ToBgra(fromData, out result.Width, out result.Height);
                }
                else
                {
                    return new ImageryResult { Outcome = TileLoadOutcome.Failed, Error = "empty image" };
                }

                return result;
            }
            finally
            {
                image?.Dispose();
            }
        }

        /// <summary>Copies an image into a tightly packed BGRA buffer, the layout GL uploads without conversion.</summary>
        public static byte[] ToBgra(Image image, out int width, out int height)
        {
            width = image.Width;
            height = image.Height;

            var bitmap = image as Bitmap;
            bool ownsBitmap = bitmap == null;
            if (ownsBitmap)
                bitmap = new Bitmap(image);

            try
            {
                var data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly,
                    PixelFormat.Format32bppArgb);
                try
                {
                    int rowBytes = width * 4;
                    var pixels = new byte[rowBytes * height];
                    if (data.Stride == rowBytes)
                    {
                        Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
                    }
                    else
                    {
                        for (int y = 0; y < height; y++)
                            Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), pixels, y * rowBytes, rowBytes);
                    }

                    return pixels;
                }
                finally
                {
                    bitmap.UnlockBits(data);
                }
            }
            finally
            {
                if (ownsBitmap)
                    bitmap.Dispose();
            }
        }
    }
}
