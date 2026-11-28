#if !LIB
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace MissionPlanner.Utilities
{
    /// <summary>Independent toolbar branding. No device state or caller-supplied URL is involved.</summary>
    internal sealed class TopBarBrand
    {
        public const string DefaultId = "localized";
        public const string SettingsKey = "toolbar_brand";
        private const int MaxSide = 1024;
        private const int MaxBytes = 4 * 1024 * 1024;

        public static readonly TopBarBrand Localized = new TopBarBrand(DefaultId, "可靠的 ly0411", "ArduPilot", BrandLogoLinks.ArduPilotUrl);
        public static readonly TopBarBrand ArduPilot = new TopBarBrand("ardupilot", "ArduPilot", "ArduPilot", BrandLogoLinks.ArduPilotUrl);
        public static readonly TopBarBrand Titan = new TopBarBrand("titan-planner", "TitanPlanner", "Titan Dynamics", "https://titandynamics.aero");
        public static readonly TopBarBrand Px4 = new TopBarBrand("px4", "PX4", "PX4", "https://px4.io/");
        // Verified in the original VTOL Survey 4.4.4 MainV2.MenuArduPilot_Click.
        public static readonly TopBarBrand Mfe = new TopBarBrand("mfe", "MakeFlyEasy", "ArduPilot",
            "https://ardupilot.org/?utm_source=Menu&utm_campaign=MP");

        public static TopBarBrand[] Choices => new[] { Localized, ArduPilot, Titan, Px4, Mfe };

        public string Id { get; }
        public string Title { get; }
        public string TargetName { get; }
        public string Url { get; }

        private TopBarBrand(string id, string title, string targetName, string url)
        {
            Id = id;
            Title = title;
            TargetName = targetName;
            Url = url;
        }

        public static TopBarBrand FromId(string id)
        {
            foreach (var preset in Choices)
                if (string.Equals(preset.Id, id, StringComparison.Ordinal))
                    return preset;
            return Localized;
        }

        public string GetAssetPath(string dataDirectory, bool? dark)
        {
            var filename = Id + (dark.HasValue ? dark.Value ? "-dark" : "-light" : "") + ".png";
            return Path.Combine(dataDirectory, "toolbar-brand", filename);
        }

        public Bitmap TryLoadAsset(string dataDirectory, bool? dark)
        {
            if (string.IsNullOrEmpty(dataDirectory) || Url == null)
                return null;
            return TryLoadPng(GetAssetPath(dataDirectory, dark));
        }

        private static Bitmap TryLoadPng(string file)
        {
            try
            {
                using (var stream = File.OpenRead(file))
                {
                    if (stream.Length < 24 || stream.Length > MaxBytes)
                        return null;
                    // Reject oversized IHDRs before invoking the native decoder.
                    var header = new byte[24];
                    if (stream.Read(header, 0, header.Length) != header.Length ||
                        header[0] != 137 || header[1] != 80 || header[2] != 78 || header[3] != 71 ||
                        header[4] != 13 || header[5] != 10 || header[6] != 26 || header[7] != 10 ||
                        ReadUInt(header, 8) != 13 || ReadUInt(header, 12) != 0x49484452)
                        return null;
                    uint width = ReadUInt(header, 16);
                    uint height = ReadUInt(header, 20);
                    if (width == 0 || height == 0 || width > MaxSide || height > MaxSide)
                        return null;
                    stream.Position = 0;
                    using (var image = Image.FromStream(stream, false, true))
                    {
                        if (image.RawFormat.Guid != ImageFormat.Png.Guid || image.Width != width || image.Height != height)
                            return null;
                        return new Bitmap(image);
                    }
                }
            }
            catch (IOException) { return null; }
            catch (UnauthorizedAccessException) { return null; }
            catch (ArgumentException) { return null; }
            catch (OutOfMemoryException) { return null; } // GDI+ also reports malformed PNGs this way.
        }

        /// <summary>Validate and atomically replace a local asset; never alter the source file.</summary>
        public void InstallAsset(string dataDirectory, bool dark, string file)
        {
            var target = GetAssetPath(dataDirectory, dark);
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            var temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var input = File.OpenRead(file))
                using (var output = File.Open(temporary, FileMode.CreateNew))
                {
                    var buffer = new byte[8192];
                    int read, total = 0;
                    while ((read = input.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        total += read;
                        if (total > MaxBytes)
                            throw new InvalidDataException("Toolbar PNG is too large.");
                        output.Write(buffer, 0, read);
                    }
                }
                using (var image = TryLoadPng(temporary))
                    if (image == null)
                        throw new InvalidDataException("Toolbar asset is not a valid PNG.");
                if (File.Exists(target))
                    File.Replace(temporary, target, null);
                else
                    File.Move(temporary, target);
            }
            finally
            {
                File.Delete(temporary);
            }
        }

        private static uint ReadUInt(byte[] bytes, int index)
        {
            return ((uint)bytes[index] << 24) | ((uint)bytes[index + 1] << 16) |
                ((uint)bytes[index + 2] << 8) | bytes[index + 3];
        }
    }
}
#endif
