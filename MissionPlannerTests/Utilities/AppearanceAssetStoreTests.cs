using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MissionPlanner.Utilities;

namespace MissionPlannerTests.Utilities
{
    [TestClass]
    public class AppearanceAssetStoreTests
    {
        private static void InDirectory(Action<string, AppearanceAssetStore> test)
        {
            var root = Path.Combine(Path.GetTempPath(), "TitanPlanner-appearance-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try { test(root, new AppearanceAssetStore(Path.Combine(root, "appearance"))); }
            finally { Directory.Delete(root, true); }
        }

        public static string ImageFile(string root, string extension, Color color)
        {
            var file = Path.Combine(root, "source" + extension);
            using (var image = new Bitmap(80, 40))
            {
                using (var graphics = Graphics.FromImage(image)) graphics.Clear(color);
                image.Save(file, extension == ".bmp" ? ImageFormat.Bmp :
                    extension == ".jpg" || extension == ".jpeg" ? ImageFormat.Jpeg : ImageFormat.Png);
            }
            return file;
        }

        public static string IconFile(string root, params int[] sizes)
        {
            if (sizes.Length == 0) sizes = new[] { 16, 32 };
            var file = Path.Combine(root, "source.ico");
            using (var output = new BinaryWriter(File.Create(file)))
            {
                output.Write((short)0); output.Write((short)1); output.Write((short)sizes.Length);
                int offset = 6 + 16 * sizes.Length;
                foreach (int size in sizes)
                {
                    int length = 40 + size * size * 4 + size * ((size + 31) / 32) * 4;
                    output.Write((byte)size); output.Write((byte)size); output.Write((short)0);
                    output.Write((short)1); output.Write((short)32); output.Write(length); output.Write(offset);
                    offset += length;
                }
                foreach (int size in sizes)
                {
                    output.Write(40); output.Write(size); output.Write(size * 2); output.Write((short)1); output.Write((short)32);
                    for (int i = 0; i < 6; i++) output.Write(0);
                    for (int i = 0; i < size * size; i++) output.Write(unchecked((int)0xff008080));
                    output.Write(new byte[size * ((size + 31) / 32) * 4]);
                }
            }
            return file;
        }

        [TestMethod]
        public void DefaultsDoNotCreateFiles()
        {
            InDirectory((root, store) =>
            {
                string warning;
                Assert.IsNull(store.LoadIcon(out warning)); Assert.IsNull(warning);
                Assert.IsNull(store.LoadStartupImage(out warning)); Assert.IsNull(warning);
                Assert.IsFalse(Directory.Exists(Path.Combine(root, "appearance")));
            });
        }

        [TestMethod]
        public void ImportsSurviveFreshStoreAndRemovedSources()
        {
            InDirectory((root, store) =>
            {
                var icon = IconFile(root); var image = ImageFile(root, ".png", Color.Magenta);
                var originalIcon = File.ReadAllBytes(icon); var originalImage = File.ReadAllBytes(image);
                store.Import(AppearanceAsset.WindowIcon, icon); store.Import(AppearanceAsset.StartupImage, image);
                CollectionAssert.AreEqual(originalIcon, File.ReadAllBytes(icon));
                CollectionAssert.AreEqual(originalImage, File.ReadAllBytes(image));
                File.Delete(icon); File.Delete(image); // Neither decoder nor preference keeps source files open.
                store = new AppearanceAssetStore(Path.Combine(root, "appearance"));
                string warning;
                using (var loaded = store.LoadIcon(out warning)) { Assert.IsNotNull(loaded); Assert.IsNull(warning); }
                using (var loaded = store.LoadStartupImage(out warning))
                {
                    Assert.IsNotNull(loaded); Assert.IsNull(warning);
                    Assert.AreEqual(80, loaded.Width); Assert.AreEqual(40, loaded.Height);
                    Assert.AreEqual(Color.Magenta.ToArgb(), loaded.GetPixel(40, 20).ToArgb());
                }
            });
        }

        [TestMethod]
        public void ResetsAreIndependentAndPreserveImportsAndUserData()
        {
            InDirectory((root, store) =>
            {
                File.WriteAllText(Path.Combine(root, "config.xml"), "existing settings");
                File.WriteAllText(Path.Combine(root, "flight.tlog"), "existing log");
                store.Import(AppearanceAsset.WindowIcon, IconFile(root));
                store.Import(AppearanceAsset.StartupImage, ImageFile(root, ".png", Color.Black));
                var assets = Directory.GetFiles(Path.Combine(root, "appearance")).Where(p => p.EndsWith(".ico") || p.EndsWith(".png")).ToArray();
                store.RestoreDefault(AppearanceAsset.WindowIcon);
                string warning;
                Assert.IsNull(store.LoadIcon(out warning)); Assert.IsNull(warning);
                using (var image = store.LoadStartupImage(out warning)) Assert.IsNotNull(image);
                store.RestoreDefault(AppearanceAsset.StartupImage);
                Assert.IsNull(store.LoadStartupImage(out warning)); Assert.IsNull(warning);
                Assert.IsTrue(assets.All(File.Exists));
                Assert.AreEqual("existing settings", File.ReadAllText(Path.Combine(root, "config.xml")));
                Assert.AreEqual("existing log", File.ReadAllText(Path.Combine(root, "flight.tlog")));
            });
        }

        [TestMethod]
        public void InvalidInputNeverCommitsSelection()
        {
            InDirectory((root, store) =>
            {
                store.Import(AppearanceAsset.WindowIcon, IconFile(root));
                var manifest = File.ReadAllBytes(Path.Combine(root, "appearance", "settings.xml"));
                var invalid = Path.Combine(root, "invalid.ico");
                foreach (var bytes in new[] { new byte[0], Encoding.UTF8.GetBytes("not an icon"), new byte[AppearanceAssetStore.MaxIconBytes + 1] })
                {
                    File.WriteAllBytes(invalid, bytes);
                    Assert.ThrowsException<InvalidDataException>(() => store.Import(AppearanceAsset.WindowIcon, invalid));
                    CollectionAssert.AreEqual(manifest, File.ReadAllBytes(Path.Combine(root, "appearance", "settings.xml")));
                }
                var png = ImageFile(root, ".png", Color.White);
                Assert.ThrowsException<InvalidDataException>(() => store.Import(AppearanceAsset.WindowIcon, png));
                File.Copy(png, invalid, true);
                Assert.ThrowsException<InvalidDataException>(() => store.Import(AppearanceAsset.WindowIcon, invalid));
                var fakeJpeg = Path.Combine(root, "fake.jpg"); File.Copy(png, fakeJpeg);
                Assert.ThrowsException<InvalidDataException>(() => store.Import(AppearanceAsset.StartupImage, fakeJpeg));
                var gif = Path.Combine(root, "animation.gif"); File.Copy(png, gif);
                Assert.ThrowsException<InvalidDataException>(() => store.Import(AppearanceAsset.StartupImage, gif));
                CollectionAssert.AreEqual(manifest, File.ReadAllBytes(Path.Combine(root, "appearance", "settings.xml")));
            });
        }

        [TestMethod]
        public void OversizedImageHeadersAndFilesAreRejected()
        {
            InDirectory((root, store) =>
            {
                var file = ImageFile(root, ".png", Color.Black);
                var bytes = File.ReadAllBytes(file);
                bytes[16] = 0x7f; bytes[17] = 0xff; bytes[18] = 0xff; bytes[19] = 0xff;
                File.WriteAllBytes(file, bytes);
                Assert.ThrowsException<InvalidDataException>(() => store.Import(AppearanceAsset.StartupImage, file));
                File.WriteAllBytes(file, new byte[AppearanceAssetStore.MaxImageBytes + 1]);
                Assert.ThrowsException<InvalidDataException>(() => store.Import(AppearanceAsset.StartupImage, file));
                Assert.IsFalse(Directory.Exists(Path.Combine(root, "appearance")));
            });
        }

        [TestMethod]
        public void MissingAndCorruptCopiesFallbackIndependentlyWithoutRewriting()
        {
            InDirectory((root, store) =>
            {
                store.Import(AppearanceAsset.WindowIcon, IconFile(root));
                store.Import(AppearanceAsset.StartupImage, ImageFile(root, ".png", Color.White));
                var directory = Path.Combine(root, "appearance"); var manifest = File.ReadAllBytes(Path.Combine(directory, "settings.xml"));
                File.WriteAllText(Directory.GetFiles(directory, "*.ico").Single(), "damaged");
                string warning; Assert.IsNull(store.LoadIcon(out warning)); Assert.IsNotNull(warning);
                using (var image = store.LoadStartupImage(out warning)) { Assert.IsNotNull(image); Assert.IsNull(warning); }
                File.Delete(Directory.GetFiles(directory, "*.png").Single());
                Assert.IsNull(store.LoadStartupImage(out warning)); Assert.IsNotNull(warning);
                CollectionAssert.AreEqual(manifest, File.ReadAllBytes(Path.Combine(directory, "settings.xml")));
            });
        }

        [TestMethod]
        public void UnsafeOrBrokenManifestFallsBackAndPreservesEvidence()
        {
            InDirectory((root, store) =>
            {
                var directory = Path.Combine(root, "appearance"); Directory.CreateDirectory(directory);
                foreach (var text in new[] { "<broken", "<appearance version='2'/>", "<other version='1'/>",
                    "<appearance version='1' icon='../source.ico'/>", "<appearance version='1' image='/private/image.png'/>",
                    "<!DOCTYPE appearance [<!ENTITY x SYSTEM 'file:///private'>]><appearance version='1' icon='&x;'/>", new string('x', 5000) })
                {
                    var path = Path.Combine(directory, "settings.xml"); File.WriteAllText(path, text);
                    string warning; Assert.IsNull(store.LoadIcon(out warning)); Assert.IsNotNull(warning);
                    Assert.IsNull(store.LoadStartupImage(out warning)); Assert.IsNotNull(warning);
                    Assert.AreEqual(text, File.ReadAllText(path));
                }
                store.RestoreDefault(AppearanceAsset.WindowIcon);
                Assert.AreEqual(new string('x', 5000), File.ReadAllText(Directory.GetFiles(directory, "settings.xml.backup-*").Single()));
            });
        }

        [TestMethod]
        public void SaveLockFailurePreservesCommittedState()
        {
            InDirectory((root, store) =>
            {
                var source = IconFile(root); store.Import(AppearanceAsset.WindowIcon, source);
                var directory = Path.Combine(root, "appearance"); var manifest = File.ReadAllBytes(Path.Combine(directory, "settings.xml"));
                using (new FileStream(Path.Combine(directory, "settings.lock"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    Assert.ThrowsException<IOException>(() => store.RestoreDefault(AppearanceAsset.WindowIcon));
                    Assert.ThrowsException<IOException>(() => store.Import(AppearanceAsset.WindowIcon, source));
                }
                CollectionAssert.AreEqual(manifest, File.ReadAllBytes(Path.Combine(directory, "settings.xml")));
                Assert.AreEqual(1, Directory.GetFiles(directory, "*.ico").Length);
            });
        }

        [TestMethod]
        public void ReplacementsKeepBackupsAndDoNotLeaveTemporaryManifests()
        {
            InDirectory((root, store) =>
            {
                var source = IconFile(root); store.Import(AppearanceAsset.WindowIcon, source);
                var directory = Path.Combine(root, "appearance"); var original = File.ReadAllText(Path.Combine(directory, "settings.xml"));
                for (int i = 0; i < 12; i++) store.Import(AppearanceAsset.WindowIcon, source);
                Assert.AreEqual(13, Directory.GetFiles(directory, "*.ico").Length);
                Assert.AreEqual(12, Directory.GetFiles(directory, "settings.xml.backup-*").Length);
                Assert.IsTrue(Directory.GetFiles(directory, "settings.xml.backup-*").Any(p => File.ReadAllText(p) == original));
                Assert.AreEqual(0, Directory.GetFiles(directory, "*.tmp").Length);
            });
        }

        [TestMethod]
        public void AllDocumentedImageFormatsLoadWithoutSourceLocks()
        {
            InDirectory((root, store) =>
            {
                foreach (var extension in new[] { ".png", ".jpg", ".jpeg", ".bmp" })
                {
                    var source = ImageFile(root, extension, Color.Lime);
                    store.Import(AppearanceAsset.StartupImage, source); File.Delete(source);
                    string warning;
                    using (var image = store.LoadStartupImage(out warning))
                    { Assert.IsNotNull(image); Assert.IsNull(warning); Assert.AreEqual(80, image.Width); Assert.AreEqual(40, image.Height); }
                }
            });
        }

        [TestMethod]
        public void EveryIconFrameIsValidated()
        {
            InDirectory((root, store) =>
            {
                var source = IconFile(root); store.Import(AppearanceAsset.WindowIcon, source);
                var bytes = File.ReadAllBytes(source); bytes[6] = 17; // Corrupt the 16px frame; 32px selected frame remains valid.
                File.WriteAllBytes(source, bytes);
                Assert.ThrowsException<InvalidDataException>(() => store.Import(AppearanceAsset.WindowIcon, source));
                Assert.AreEqual(1, Directory.GetFiles(Path.Combine(root, "appearance"), "*.ico").Length);
            });
        }

        [TestMethod]
        public void PngCompressed256PixelIcoIsSupported()
        {
            InDirectory((root, store) =>
            {
                byte[] png;
                using (var bitmap = new Bitmap(256, 256))
                using (var buffer = new MemoryStream())
                {
                    using (var graphics = Graphics.FromImage(bitmap))
                    { graphics.Clear(Color.Transparent); graphics.FillRectangle(Brushes.Blue, 64, 64, 128, 128); }
                    bitmap.Save(buffer, ImageFormat.Png); png = buffer.ToArray();
                }
                var file = Path.Combine(root, "compressed.ico");
                using (var writer = new BinaryWriter(File.Create(file)))
                {
                    writer.Write((short)0); writer.Write((short)1); writer.Write((short)1);
                    writer.Write(0); writer.Write((short)1); writer.Write((short)32);
                    writer.Write(png.Length); writer.Write(22); writer.Write(png);
                }
                var original = File.ReadAllBytes(file);
                store.Import(AppearanceAsset.WindowIcon, file);
                CollectionAssert.AreEqual(original, File.ReadAllBytes(file));
                string warning;
                using (var icon = store.LoadIcon(out warning))
                using (var image = icon.ToBitmap())
                {
                    Assert.IsNull(warning);
                    Assert.AreEqual(Color.Blue.ToArgb(), image.GetPixel(image.Width / 2, image.Height / 2).ToArgb());
                    Assert.AreEqual(0, image.GetPixel(0, 0).A);
                }
            });
        }

        [TestMethod]
        public void Dib256PixelIcoIsSupported()
        {
            InDirectory((root, store) =>
            {
                store.Import(AppearanceAsset.WindowIcon, IconFile(root, 256));
                string warning;
                using (var icon = store.LoadIcon(out warning))
                using (var bitmap = icon.ToBitmap())
                {
                    Assert.IsNull(warning);
                    Assert.AreEqual(unchecked((int)0xff008080), bitmap.GetPixel(bitmap.Width / 2, bitmap.Height / 2).ToArgb());
                }
            });
        }

        private static object VendorIconCall(AppearanceAssetStore store, string method, string vendor, params object[] tail)
        {
            var type = typeof(AppearanceAssetStore);
            var call = type.GetMethod(method, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(call);
            var selection = Enum.Parse(call.GetParameters()[0].ParameterType, vendor);
            return call.Invoke(store, new object[] { selection }.Concat(tail).ToArray());
        }

        [TestMethod]
        public void VendorIconSlotsPersistSeparatelyFromDefaultCustomAndStartup()
        {
            InDirectory((root, store) =>
            {
                var source = IconFile(root);
                store.Import(AppearanceAsset.WindowIcon, source);
                store.Import(AppearanceAsset.StartupImage, ImageFile(root, ".png", Color.Magenta));
                Assert.AreEqual(false, VendorIconCall(store, "CanSelectVendorIcon", "MissionPlanner"));
                Assert.AreEqual(false, VendorIconCall(store, "CanSelectVendorIcon", "TitanPlanner"));

                VendorIconCall(store, "ImportVendorIcon", "MissionPlanner", source);
                VendorIconCall(store, "ImportVendorIcon", "TitanPlanner", source);
                File.Delete(source);
                var directory = Path.Combine(root, "appearance");
                var manifest = Path.Combine(directory, "settings.xml");
                var xml = new System.Xml.XmlDocument();
                xml.Load(manifest);
                var mp = xml.DocumentElement.GetAttribute("missionPlannerIcon");
                var tp = xml.DocumentElement.GetAttribute("titanPlannerIcon");
                Assert.AreNotEqual(mp, tp);
                Assert.IsTrue(File.Exists(Path.Combine(directory, mp)));
                Assert.IsTrue(File.Exists(Path.Combine(directory, tp)));

                store = new AppearanceAssetStore(directory);
                Assert.AreEqual(true, VendorIconCall(store, "CanSelectVendorIcon", "MissionPlanner"));
                Assert.AreEqual(true, VendorIconCall(store, "CanSelectVendorIcon", "TitanPlanner"));
                VendorIconCall(store, "SelectVendorIcon", "MissionPlanner");
                xml.Load(manifest);
                Assert.AreEqual("mission-planner", xml.DocumentElement.GetAttribute("iconPreset"));
                store.RestoreDefault(AppearanceAsset.WindowIcon);
                Assert.IsNull(store.LoadIcon(out _));
                using (var image = store.LoadStartupImage(out _))
                    Assert.AreEqual(Color.Magenta.ToArgb(), image.GetPixel(40, 20).ToArgb());
                VendorIconCall(store, "SelectVendorIcon", "TitanPlanner");
                xml.Load(manifest);
                Assert.AreEqual("titan-planner", xml.DocumentElement.GetAttribute("iconPreset"));
                using (var icon = store.LoadIcon(out _)) Assert.IsNotNull(icon);
            });
        }

        [TestMethod]
        public void SeparateInstancesPreserveTheOtherSelection()
        {
            InDirectory((root, store) =>
            {
                var second = new AppearanceAssetStore(Path.Combine(root, "appearance"));
                store.Import(AppearanceAsset.WindowIcon, IconFile(root));
                second.Import(AppearanceAsset.StartupImage, ImageFile(root, ".png", Color.White));
                string warning;
                using (var icon = store.LoadIcon(out warning)) Assert.IsNotNull(icon);
                using (var image = second.LoadStartupImage(out warning)) Assert.IsNotNull(image);
            });
        }
    }
}
