#if !LIB
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace MissionPlanner.Utilities
{
    public enum AppearanceAsset { WindowIcon, StartupImage }
    internal enum ImportedVendorIcon { MissionPlanner, TitanPlanner }

    /// <summary>
    /// A separate, per-user preference file. Never writes to the installation, source image or config.xml.
    /// Imports are immutable; the small manifest is committed last with an atomic rename/replace.
    /// </summary>
    public sealed class AppearanceAssetStore
    {
        public const int MaxIconBytes = 4 * 1024 * 1024;
        public const int MaxImageBytes = 16 * 1024 * 1024;
        public const int MaxImageDimension = 4096;
        internal const string BuiltInMissionPlanner = "built-in-mission-planner";
        internal const string BuiltInTitanPlanner = "built-in-titan-planner";
        internal const string BuiltInMfe = "built-in-mfe";
        internal const string MissionPlannerStartup = "mission-planner";
        private readonly string directory;

        public AppearanceAssetStore(string directory)
        {
            this.directory = Path.GetFullPath(directory);
        }

        private sealed class Selection
        {
            public string Icon = "";
            public string Image = "";
            public string StartupPreset = "", IconPreset = "", MfeIcon = "", MfeImage = "";
            public string MissionPlannerIcon = "", TitanPlannerIcon = "";
        }

        public Icon LoadIcon(out string warning)
        {
            return LoadIcon(null, out warning);
        }

        internal Icon LoadIcon(Func<string, Icon> loadBuiltIn, out string warning)
        {
            var selection = ReadSelection(out warning);
            if (IsBuiltInIcon(selection.IconPreset))
                return LoadBuiltIn(selection.IconPreset, loadBuiltIn,
                    "Window icon unavailable; using default.", ref warning);
            var bytes = LoadSelectedAsset(AppearanceAsset.WindowIcon, selection, ref warning);
            if (bytes == null) return null;
            try { return DecodeIcon(bytes); }
            catch (Exception) { warning = "Custom icon unavailable; using default."; return null; }
        }

        public Bitmap LoadStartupImage(out string warning)
        {
            return LoadStartupImage(null, out warning);
        }

        internal Bitmap LoadStartupImage(Func<string, Bitmap> loadBuiltIn, out string warning)
        {
            var selection = ReadSelection(out warning);
            if (selection.StartupPreset == BuiltInMfe ||
                (loadBuiltIn != null && selection.StartupPreset == MissionPlannerStartup))
                return LoadBuiltIn(selection.StartupPreset, loadBuiltIn,
                    "Built-in startup image unavailable; using default.", ref warning);
            var bytes = LoadSelectedAsset(AppearanceAsset.StartupImage, selection, ref warning);
            if (bytes == null) return null;
            try { return DecodeImage(bytes); }
            catch (Exception) { warning = "Custom startup image unavailable; using default."; return null; }
        }

        private static T LoadBuiltIn<T>(string preset, Func<string, T> loader, string fallback, ref string warning)
            where T : class
        {
            try
            {
                var asset = loader?.Invoke(preset);
                if (asset != null) return asset;
            }
            catch (Exception) { }
            warning = fallback;
            return null;
        }

        private static bool IsBuiltInIcon(string preset)
        {
            return preset == BuiltInMissionPlanner || preset == BuiltInTitanPlanner || preset == BuiltInMfe;
        }

        private static bool SaveCustomSeparately(AppearanceAsset kind, string preset)
        {
            return preset == "default" ||
                (kind == AppearanceAsset.WindowIcon ? IsBuiltInIcon(preset) : preset == BuiltInMfe);
        }

        private byte[] LoadSelectedAsset(AppearanceAsset kind, Selection selection, ref string warning)
        {
            var name = kind == AppearanceAsset.WindowIcon
                ? StoredIconName(selection, selection.IconPreset)
                : (selection.StartupPreset == "mfe" ? selection.MfeImage :
                    selection.StartupPreset == "default" ? "" : selection.Image);
            if (name.Length == 0) return null;
            try
            {
                var bytes = ReadBounded(Path.Combine(directory, name), Limit(kind));
                if (Digest(bytes) != name.Substring(0, 64)) throw new InvalidDataException();
                Validate(kind, Path.GetExtension(name), bytes);
                return bytes;
            }
            catch (Exception)
            {
                warning = kind == AppearanceAsset.WindowIcon ? "Custom icon unavailable; using default."
                    : "Custom startup image unavailable; using default.";
                return null;
            }
        }

        public void Import(AppearanceAsset kind, string source)
        {
            ImportLocal(kind, source, "");
        }

        internal void ImportMfe(AppearanceAsset kind, string source) { ImportLocal(kind, source, "mfe"); }

        internal bool CanSelectMfe(AppearanceAsset kind)
        {
            var selection = ReadSelection(out _);
            return (kind == AppearanceAsset.WindowIcon ? selection.MfeIcon : selection.MfeImage).Length != 0;
        }

        internal void SelectMfe(AppearanceAsset kind) { Save(kind, null, "", "mfe", reusePreset: true); }

        internal bool CanSelectSavedIcon() { return ReadSelection(out _).Icon.Length != 0; }
        internal void SelectSavedIcon() { Save(AppearanceAsset.WindowIcon, null, "", reusePreset: true); }

        internal void ImportVendorIcon(ImportedVendorIcon vendor, string source)
        {
            ImportLocal(AppearanceAsset.WindowIcon, source, VendorIconId(vendor));
        }

        internal bool CanSelectVendorIcon(ImportedVendorIcon vendor)
        {
            return StoredIconName(ReadSelection(out _), VendorIconId(vendor)).Length != 0;
        }

        internal void SelectVendorIcon(ImportedVendorIcon vendor)
        {
            Save(AppearanceAsset.WindowIcon, null, "", VendorIconId(vendor), reusePreset: true);
        }

        private static string VendorIconId(ImportedVendorIcon vendor)
        {
            switch (vendor)
            {
                case ImportedVendorIcon.MissionPlanner: return "mission-planner";
                case ImportedVendorIcon.TitanPlanner: return "titan-planner";
                default: throw new ArgumentOutOfRangeException(nameof(vendor));
            }
        }

        private static string StoredIconName(Selection selection, string preset)
        {
            switch (preset)
            {
                case "default": return "";
                case "mfe": return selection.MfeIcon;
                case "mission-planner": return selection.MissionPlannerIcon;
                case "titan-planner": return selection.TitanPlannerIcon;
                default: return selection.Icon;
            }
        }

        private void ImportLocal(AppearanceAsset kind, string source, string preset)
        {
            var extension = Path.GetExtension(source).ToLowerInvariant();
            if (extension == ".jpeg") extension = ".jpg";
            var bytes = ReadBounded(source, Limit(kind));
            Validate(kind, extension, bytes); // Validate before creating or changing any preference.
            Save(kind, bytes, extension, preset);
        }

        public void RestoreDefault(AppearanceAsset kind) { Save(kind, null, "", "default"); }

        internal string CurrentPreset(AppearanceAsset kind)
        {
            var selection = ReadSelection(out _);
            var preset = kind == AppearanceAsset.WindowIcon ? selection.IconPreset : selection.StartupPreset;
            var custom = kind == AppearanceAsset.WindowIcon ? selection.Icon : selection.Image;
            return preset.Length != 0 ? preset : custom.Length != 0 ? "custom" : "default";
        }

        internal void SelectBuiltInPreset(AppearanceAsset kind, string preset)
        {
            bool valid = kind == AppearanceAsset.WindowIcon ? IsBuiltInIcon(preset) :
                preset == MissionPlannerStartup || preset == BuiltInMfe;
            if (!valid) throw new ArgumentOutOfRangeException(nameof(preset));
            Save(kind, null, "", preset);
        }

        internal void SelectSavedStartupImage()
        {
            Save(AppearanceAsset.StartupImage, null, "", reuseSavedImage: true);
        }

        internal bool CanSelectSavedStartupImage()
        {
            string warning;
            var selection = ReadSelection(out warning);
            return selection.Image.Length != 0;
        }

        private void Save(AppearanceAsset kind, byte[] bytes, string extension, string preset = "",
            bool reuseSavedImage = false, bool reusePreset = false)
        {
            Directory.CreateDirectory(directory);
            // Serializes read-modify-write between application instances. Failure preserves the old selection.
            using (new FileStream(Path.Combine(directory, "settings.lock"), FileMode.OpenOrCreate,
                FileAccess.ReadWrite, FileShare.None))
            {
                string ignored;
                var selection = ReadSelection(out ignored);
                var name = "";
                if (bytes != null)
                {
                    name = Digest(bytes) + "-" + Guid.NewGuid().ToString("N") + extension;
                    using (var asset = new FileStream(Path.Combine(directory, name), FileMode.CreateNew,
                        FileAccess.Write, FileShare.None))
                    {
                        asset.Write(bytes, 0, bytes.Length);
                        asset.Flush(true);
                    }
                }
                if (kind == AppearanceAsset.WindowIcon) selection.IconPreset = preset;
                else selection.StartupPreset = preset;

                if (preset == "mfe" && !reusePreset)
                {
                    if (kind == AppearanceAsset.WindowIcon) selection.MfeIcon = name;
                    else selection.MfeImage = name;
                }
                else if (reusePreset)
                {
                    if (LoadSelectedAsset(kind, selection, ref ignored) == null)
                        throw new InvalidDataException();
                }
                else if (kind == AppearanceAsset.WindowIcon)
                {
                    if (preset == "mission-planner") selection.MissionPlannerIcon = name;
                    else if (preset == "titan-planner") selection.TitanPlannerIcon = name;
                    else if (preset.Length == 0) selection.Icon = name;
                }
                else
                {
                    selection.StartupPreset = preset;
                    // Choosing the built-in never discards the saved imported selection or its bytes.
                    if (reuseSavedImage)
                    {
                        if (LoadSelectedAsset(AppearanceAsset.StartupImage, selection, ref ignored) == null)
                            throw new InvalidDataException("No saved startup image is available.");
                    }
                    else if (preset.Length == 0) selection.Image = name;
                }

                var manifest = Path.Combine(directory, "settings.xml");
                var temporary = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".tmp");
                try
                {
                    using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        using (var writer = XmlWriter.Create(output, new XmlWriterSettings
                        { Encoding = new UTF8Encoding(false), Indent = true, CloseOutput = false }))
                        {
                            writer.WriteStartElement("appearance");
                            writer.WriteAttributeString("version", "1");
                            // Older readers fall back to the default for presets they do not recognize.
                            bool separateIcon = SaveCustomSeparately(AppearanceAsset.WindowIcon, selection.IconPreset);
                            bool separateImage = SaveCustomSeparately(AppearanceAsset.StartupImage, selection.StartupPreset);
                            writer.WriteAttributeString("icon", separateIcon ? "" : selection.Icon);
                            writer.WriteAttributeString("image", separateImage ? "" : selection.Image);
                            if (separateIcon && selection.Icon.Length != 0)
                                writer.WriteAttributeString("savedIcon", selection.Icon);
                            if (separateImage && selection.Image.Length != 0)
                                writer.WriteAttributeString("savedImage", selection.Image);
                            if (selection.IconPreset.Length != 0) writer.WriteAttributeString("iconPreset", selection.IconPreset);
                            if (selection.MissionPlannerIcon.Length != 0) writer.WriteAttributeString("missionPlannerIcon", selection.MissionPlannerIcon);
                            if (selection.TitanPlannerIcon.Length != 0) writer.WriteAttributeString("titanPlannerIcon", selection.TitanPlannerIcon);
                            if (selection.MfeIcon.Length != 0) writer.WriteAttributeString("mfeIcon", selection.MfeIcon);
                            if (selection.MfeImage.Length != 0) writer.WriteAttributeString("mfeImage", selection.MfeImage);
                            if (selection.StartupPreset.Length != 0)
                                writer.WriteAttributeString("startupPreset", selection.StartupPreset);
                            writer.WriteEndElement();
                        }
                        output.Flush(true);
                    }
                    if (File.Exists(manifest))
                        File.Replace(temporary, manifest, manifest + ".backup-" + Guid.NewGuid().ToString("N"));
                    else
                        File.Move(temporary, manifest);
                }
                finally
                {
                    if (File.Exists(temporary)) File.Delete(temporary);
                }
            }
        }

        private Selection ReadSelection(out string warning)
        {
            warning = null;
            var manifest = Path.Combine(directory, "settings.xml");
            if (!File.Exists(manifest)) return new Selection();
            try
            {
                var xml = ReadBounded(manifest, 4096);
                var document = new XmlDocument { XmlResolver = null };
                using (var input = new MemoryStream(xml))
                using (var reader = XmlReader.Create(input, new XmlReaderSettings
                { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 4096 }))
                    document.Load(reader);
                var root = document.DocumentElement;
                if (root == null || root.Name != "appearance" || root.GetAttribute("version") != "1")
                    throw new InvalidDataException();
                var icon = root.GetAttribute("icon");
                var image = root.GetAttribute("image");
                if (!ValidName(icon, @"ico") || !ValidName(image, @"png|jpg|bmp"))
                    throw new InvalidDataException();
                var startupPreset = root.GetAttribute("startupPreset");
                var iconPreset = root.GetAttribute("iconPreset");
                var savedIcon = root.GetAttribute("savedIcon");
                var savedImage = root.GetAttribute("savedImage");
                return new Selection {
                    Icon = SaveCustomSeparately(AppearanceAsset.WindowIcon, iconPreset) && ValidName(savedIcon, "ico") ? savedIcon : icon,
                    Image = SaveCustomSeparately(AppearanceAsset.StartupImage, startupPreset) && ValidName(savedImage, @"png|jpg|bmp") ? savedImage : image,
                    StartupPreset = startupPreset == MissionPlannerStartup || startupPreset == "mfe" ||
                        startupPreset == "default" || startupPreset == BuiltInMfe ? startupPreset : "",
                    IconPreset = iconPreset == "mfe" || iconPreset == "mission-planner" ||
                        iconPreset == "titan-planner" || iconPreset == "default" || IsBuiltInIcon(iconPreset) ? iconPreset : "",
                    MissionPlannerIcon = ReadOptionalIcon(root, "missionPlannerIcon"),
                    TitanPlannerIcon = ReadOptionalIcon(root, "titanPlannerIcon"),
                    MfeIcon = ValidName(root.GetAttribute("mfeIcon"), @"ico") ? root.GetAttribute("mfeIcon") : "",
                    MfeImage = ValidName(root.GetAttribute("mfeImage"), @"png|jpg|bmp") ? root.GetAttribute("mfeImage") : "" };
            }
            catch (Exception)
            {
                warning = "Appearance preferences unavailable; using defaults.";
                return new Selection();
            }
        }

        private static string ReadOptionalIcon(XmlElement root, string attribute)
        {
            var name = root.GetAttribute(attribute);
            return ValidName(name, "ico") ? name : "";
        }

        private static bool ValidName(string name, string extensions)
        {
            return name.Length == 0 || Regex.IsMatch(name,
                @"\A[0-9a-f]{64}-[0-9a-f]{32}\.(?:" + extensions + @")\z", RegexOptions.CultureInvariant);
        }

        private static int Limit(AppearanceAsset kind)
        { return kind == AppearanceAsset.WindowIcon ? MaxIconBytes : MaxImageBytes; }

        private static byte[] ReadBounded(string path, int limit)
        {
            using (var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (input.Length == 0 || input.Length > limit)
                    throw new InvalidDataException("Image file exceeds the size limit or is empty.");
                var bytes = new byte[(int)input.Length];
                var read = 0;
                while (read < bytes.Length)
                {
                    int count = input.Read(bytes, read, bytes.Length - read);
                    if (count == 0) throw new InvalidDataException("Incomplete image file.");
                    read += count;
                }
                return bytes;
            }
        }

        private static string Digest(byte[] bytes)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }

        private static void Validate(AppearanceAsset kind, string extension, byte[] bytes)
        {
            if (kind == AppearanceAsset.WindowIcon)
            {
                if (extension != ".ico") throw new InvalidDataException("Choose an ICO file (up to 4 MiB).");
                ValidateIcon(bytes);
                using (var icon = DecodeIcon(bytes)) { }
            }
            else
            {
                if (extension != ".png" && extension != ".jpg" && extension != ".bmp")
                    throw new InvalidDataException("Choose a PNG, JPEG or BMP image (up to 16 MiB and 4096 pixels per side).");
                ValidateImageHeader(bytes, extension);
                using (var bitmap = DecodeImage(bytes)) { }
            }
        }

        internal static Icon DecodeIcon(byte[] bytes)
        {
            bool windows = Environment.OSVersion.Platform == PlatformID.Win32NT;
            // Mono's Icon decoder needs an in-memory DIB copy; the stored ICO stays intact.
            if (!windows) bytes = ExpandPngIconFrames(bytes);
            using (var stream = new MemoryStream(bytes))
            using (var icon = new Icon(stream, new Size(32, 32)))
            {
                if (windows && HasPngIconFrame(bytes))
                {
                    // .NET Framework's managed ToBitmap path can interpret compressed frame bytes
                    // as a DIB. Clone the decoded Windows icon handle to preserve the actual pixels.
                    using (var nativeIcon = Icon.FromHandle(icon.Handle))
                        return (Icon)nativeIcon.Clone();
                }
                return (Icon)icon.Clone();
            }
        }

        private static bool HasPngIconFrame(byte[] bytes)
        {
            int count = BitConverter.ToUInt16(bytes, 4);
            for (int i = 0; i < count; i++)
            {
                int frame = (int)BitConverter.ToUInt32(bytes, 6 + 16 * i + 12);
                if (BitConverter.ToUInt64(bytes, frame) == 0x0a1a0a0d474e5089UL)
                    return true;
            }
            return false;
        }

        private static byte[] ExpandPngIconFrames(byte[] bytes)
        {
            int count = BitConverter.ToUInt16(bytes, 4);
            var frames = new byte[count][];
            var converted = new bool[count];
            var widths = new byte[count];
            var heights = new byte[count];
            bool expanded = false;
            for (int i = 0; i < count; i++)
            {
                int entry = 6 + i * 16;
                int start = (int)BitConverter.ToUInt32(bytes, entry + 12);
                int length = (int)BitConverter.ToUInt32(bytes, entry + 8);
                var frame = new byte[length]; Buffer.BlockCopy(bytes, start, frame, 0, length);
                bool png = BitConverter.ToUInt64(frame, 0) == 0x0a1a0a0d474e5089UL;
                bool large = bytes[entry] == 0 && bytes[entry + 1] == 0;
                if (png || large)
                {
                    if (png) ValidateImageHeader(frame, ".png");
                    using (var source = png ? DecodeImage(frame) : DecodeLargeDib(frame))
                    // Mono ignores entries whose byte dimensions are both zero, including valid 256px DIBs.
                    // A 32px in-memory display copy is sufficient for its window icon; the original stays intact.
                    using (var bitmap = large ? new Bitmap(source, new Size(32, 32)) : new Bitmap(source))
                    using (var buffer = new MemoryStream())
                    using (var writer = new BinaryWriter(buffer))
                    {
                        int width = bitmap.Width, height = bitmap.Height, stride = ((width + 31) / 32) * 4;
                        widths[i] = (byte)width; heights[i] = (byte)height;
                        writer.Write(40); writer.Write(width); writer.Write(height * 2);
                        writer.Write((short)1); writer.Write((short)32); writer.Write(0);
                        writer.Write(width * height * 4 + stride * height);
                        for (int field = 0; field < 4; field++) writer.Write(0);
                        var mask = new byte[stride * height];
                        var row = new byte[width * 4];
                        var pixels = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                        try
                        {
                            for (int y = height - 1; y >= 0; y--)
                            {
                                Marshal.Copy(IntPtr.Add(pixels.Scan0, y * pixels.Stride), row, 0, row.Length);
                                writer.Write(row);
                                for (int x = 0; x < width; x++)
                                    if (row[x * 4 + 3] == 0) mask[(height - 1 - y) * stride + x / 8] |= (byte)(0x80 >> (x % 8));
                            }
                        }
                        finally { bitmap.UnlockBits(pixels); }
                        writer.Write(mask); frames[i] = buffer.ToArray(); expanded = true; converted[i] = true;
                    }
                }
                else frames[i] = frame;
            }
            if (!expanded) return bytes;
            using (var buffer = new MemoryStream())
            using (var writer = new BinaryWriter(buffer))
            {
                writer.Write((short)0); writer.Write((short)1); writer.Write((short)count);
                int offset = 6 + 16 * count;
                for (int i = 0; i < count; i++)
                {
                    if (converted[i])
                    {
                        writer.Write(widths[i]); writer.Write(heights[i]); writer.Write((short)0);
                        writer.Write((short)1); writer.Write((short)32);
                    }
                    else writer.Write(bytes, 6 + 16 * i, 8);
                    writer.Write(frames[i].Length); writer.Write(offset); offset += frames[i].Length;
                }
                foreach (var frame in frames) writer.Write(frame);
                return buffer.ToArray();
            }
        }

        private static Bitmap DecodeLargeDib(byte[] frame)
        {
            // The common 256px uncompressed 32-bit ICO form. Other DIB variants remain native on Windows.
            int header = (int)BitConverter.ToUInt32(frame, 0), width = BitConverter.ToInt32(frame, 4);
            int height = BitConverter.ToInt32(frame, 8) / 2, stride = ((width + 31) / 32) * 4;
            if (header != 40 || BitConverter.ToUInt16(frame, 14) != 32 || BitConverter.ToUInt32(frame, 16) != 0 ||
                frame.Length < header + width * height * 4 + stride * height)
                throw new InvalidDataException("This ICO frame is unsupported on Mono. Use a 32-bit DIB or PNG frame.");
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var row = new byte[width * 4];
            bool hasAlpha = false;
            for (int i = header + 3; i < header + width * height * 4; i += 4) hasAlpha |= frame[i] != 0;
            var pixels = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                for (int y = 0; y < height; y++)
                {
                    int bottom = height - 1 - y;
                    Buffer.BlockCopy(frame, header + bottom * row.Length, row, 0, row.Length);
                    for (int x = 0; x < width; x++)
                    {
                        if (!hasAlpha) row[x * 4 + 3] = 255;
                        if ((frame[header + width * height * 4 + bottom * stride + x / 8] & (0x80 >> (x % 8))) != 0)
                            row[x * 4 + 3] = 0;
                    }
                    Marshal.Copy(row, 0, IntPtr.Add(pixels.Scan0, y * pixels.Stride), row.Length);
                }
            }
            finally { bitmap.UnlockBits(pixels); }
            return bitmap;
        }

        internal static Bitmap DecodeImage(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            using (var image = Image.FromStream(stream, false, true))
            {
                CheckDimensions(image.Width, image.Height);
                var copy = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
                using (var graphics = Graphics.FromImage(copy)) graphics.DrawImage(image, 0, 0, image.Width, image.Height);
                return copy; // Detached from the input stream and without metadata or a file lock.
            }
        }

        private static void CheckDimensions(long width, long height)
        {
            if (width <= 0 || height <= 0 || width > MaxImageDimension || height > MaxImageDimension)
                throw new InvalidDataException("Choose a PNG, JPEG or BMP image (up to 16 MiB and 4096 pixels per side).");
        }

        // Read dimensions before invoking GDI+ so tiny files with enormous declared dimensions are rejected.
        private static void ValidateImageHeader(byte[] bytes, string extension)
        {
            if (extension == ".png")
            {
                if (bytes.Length < 33 || BitConverter.ToUInt64(bytes, 0) != 0x0a1a0a0d474e5089UL ||
                    ReadBigEndian(bytes, 8, 4) != 13 || ReadBigEndian(bytes, 12, 4) != 0x49484452)
                    throw new InvalidDataException("The image format does not match its file extension.");
                CheckDimensions(ReadBigEndian(bytes, 16, 4), ReadBigEndian(bytes, 20, 4));
                return;
            }
            if (extension == ".bmp")
            {
                if (bytes.Length < 54 || bytes[0] != 'B' || bytes[1] != 'M' || BitConverter.ToUInt32(bytes, 14) < 40)
                    throw new InvalidDataException("The image format does not match its file extension.");
                CheckDimensions(BitConverter.ToInt32(bytes, 18), Math.Abs((long)BitConverter.ToInt32(bytes, 22)));
                return;
            }
            if (bytes.Length < 4 || bytes[0] != 0xff || bytes[1] != 0xd8)
                throw new InvalidDataException("The image format does not match its file extension.");
            int offset = 2;
            while (offset + 4 <= bytes.Length)
            {
                if (bytes[offset++] != 0xff) break;
                while (offset < bytes.Length && bytes[offset] == 0xff) offset++;
                if (offset + 3 > bytes.Length) break;
                int marker = bytes[offset++];
                if (marker == 0xd9 || marker == 0xda) break;
                int length = (int)ReadBigEndian(bytes, offset, 2);
                if (length < 2 || offset + length > bytes.Length) break;
                if ((marker >= 0xc0 && marker <= 0xcf) && marker != 0xc4 && marker != 0xc8 && marker != 0xcc)
                {
                    if (length < 8) break;
                    CheckDimensions(ReadBigEndian(bytes, offset + 5, 2), ReadBigEndian(bytes, offset + 3, 2));
                    return;
                }
                offset += length;
            }
            throw new InvalidDataException("The image format does not match its file extension.");
        }

        private static long ReadBigEndian(byte[] bytes, int offset, int count)
        {
            long value = 0;
            for (int i = 0; i < count; i++) value = (value << 8) | bytes[offset + i];
            return value;
        }

        private static void ValidateIcon(byte[] bytes)
        {
            if (bytes.Length < 22 || BitConverter.ToUInt16(bytes, 0) != 0 || BitConverter.ToUInt16(bytes, 2) != 1)
                throw new InvalidDataException("Choose a valid ICO file with frames from 1 to 256 pixels.");
            int count = BitConverter.ToUInt16(bytes, 4);
            if (count == 0 || count > 64 || bytes.Length < 6 + 16 * count) throw new InvalidDataException("Invalid ICO directory.");
            for (int i = 0; i < count; i++)
            {
                int entry = 6 + 16 * i;
                int width = bytes[entry] == 0 ? 256 : bytes[entry];
                int height = bytes[entry + 1] == 0 ? 256 : bytes[entry + 1];
                long size = BitConverter.ToUInt32(bytes, entry + 8);
                long offset = BitConverter.ToUInt32(bytes, entry + 12);
                if (size < 8 || offset < 6 + 16 * count || offset + size > bytes.Length)
                    throw new InvalidDataException("Invalid ICO frame.");
                int start = (int)offset;
                if (BitConverter.ToUInt64(bytes, start) == 0x0a1a0a0d474e5089UL)
                {
                    if (size < 33 || ReadBigEndian(bytes, start + 16, 4) != width || ReadBigEndian(bytes, start + 20, 4) != height)
                        throw new InvalidDataException("Invalid ICO frame dimensions.");
                }
                else if (size < 40 || BitConverter.ToUInt32(bytes, start) < 40 ||
                    BitConverter.ToInt32(bytes, start + 4) != width || BitConverter.ToInt32(bytes, start + 8) != height * 2)
                    throw new InvalidDataException("Invalid ICO frame dimensions.");
                // Decode every frame, including frames Windows would not select on this display.
                var single = new byte[22 + size];
                Buffer.BlockCopy(bytes, 0, single, 0, 6);
                single[4] = 1; single[5] = 0;
                Buffer.BlockCopy(bytes, entry, single, 6, 16);
                Buffer.BlockCopy(BitConverter.GetBytes((uint)22), 0, single, 18, 4);
                Buffer.BlockCopy(bytes, start, single, 22, (int)size);
                using (var icon = DecodeIcon(single))
                using (var bitmap = icon.ToBitmap()) { }
            }
        }
    }
}
#endif
