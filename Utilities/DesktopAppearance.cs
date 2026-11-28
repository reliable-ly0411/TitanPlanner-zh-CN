#if !LIB
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MissionPlanner.Utilities
{
    /// <summary>Desktop-only bridge. The store and its images never replace Program.IconFile or upstream resources.</summary>
    public static class DesktopAppearance
    {
        private static readonly Lazy<AppearanceAssetStore> store = new Lazy<AppearanceAssetStore>(() =>
            new AppearanceAssetStore(Path.Combine(Settings.GetUserDataDirectory(), "appearance")));
        private static bool iconLoaded;
        private static Icon windowIcon;
        public static AppearanceAssetStore Store { get { return store.Value; } }
        public static string IconWarning { get; private set; }

        public static Icon WindowIcon
        {
            get
            {
                if (!iconLoaded) ReloadIcon();
                return windowIcon;
            }
        }

        private static void ReloadIcon()
        {
            string warning;
            windowIcon = Store.LoadIcon(LoadBuiltInIcon, out warning);
            IconWarning = warning;
            iconLoaded = true;
            // Forms can still reference the preceding shared Icon. It is collected when their final reference
            // disappears; disposing it here would invalidate icons on retained/closed modal windows.
        }

        public static void Import(AppearanceAsset kind, string path)
        {
            Store.Import(kind, path); // Only update the running UI after the manifest commit succeeds.
            if (kind == AppearanceAsset.WindowIcon) RefreshWindowIcons();
        }

        internal static void ImportMfe(AppearanceAsset kind, string path)
        {
            Store.ImportMfe(kind, path);
            if (kind == AppearanceAsset.WindowIcon) RefreshWindowIcons();
        }

        internal static void SelectMfe(AppearanceAsset kind)
        {
            Store.SelectMfe(kind);
            if (kind == AppearanceAsset.WindowIcon) RefreshWindowIcons();
        }

        internal static void ImportVendorIcon(ImportedVendorIcon vendor, string source)
        {
            Store.ImportVendorIcon(vendor, source);
            RefreshWindowIcons();
        }

        internal static void SelectVendorIcon(ImportedVendorIcon vendor)
        {
            Store.SelectVendorIcon(vendor);
            RefreshWindowIcons();
        }

        internal static void SelectSavedIcon()
        {
            Store.SelectSavedIcon();
            RefreshWindowIcons();
        }

        public static void RestoreDefault(AppearanceAsset kind)
        {
            Store.RestoreDefault(kind);
            if (kind == AppearanceAsset.WindowIcon) RefreshWindowIcons();
        }

        private static void RefreshWindowIcons()
        {
            try
            {
                ReloadIcon();
                ApplyWindowIcon(ThemeManager.FormIcon, MainV2.instance);
            }
            catch (Exception) { IconWarning = "Appearance saved; reopen any window whose icon did not refresh."; }
        }

        private static void ApplyWindowIcon(Icon icon, Form mainWindow)
        {
            // MainV2.instance is established by its constructor. A live main window can be absent from OpenForms;
            // keep that authoritative instance in the snapshot even during its handle lifecycle.
            var forms = Application.OpenForms.Cast<Form>().Concat(new[] { mainWindow })
                .Where(form => form != null).Distinct().ToArray();
            foreach (var form in forms)
            {
                try { if (!form.IsDisposed) form.Icon = icon; }
                catch (Exception) { IconWarning = "Appearance saved; reopen any window whose icon did not refresh."; }
            }
        }

        internal static void SelectBuiltInPreset(AppearanceAsset kind, string preset)
        {
            bool windowIcon = kind == AppearanceAsset.WindowIcon;
            using (IDisposable asset = windowIcon ? (IDisposable)LoadBuiltInIcon(preset) : LoadBuiltInStartup(preset))
                if (asset == null)
                    throw new InvalidDataException(windowIcon ? "Window icon unavailable; using default." :
                        "Built-in startup image unavailable; using default.");
            Store.SelectBuiltInPreset(kind, preset);
            if (windowIcon) RefreshWindowIcons();
        }

        private static Icon LoadBuiltInIcon(string preset)
        {
            var bytes = ReadBuiltInResource(preset + ".ico");
            return bytes == null ? null : AppearanceAssetStore.DecodeIcon(bytes);
        }

        private static Bitmap LoadBuiltInStartup(string preset)
        {
            if (preset == AppearanceAssetStore.MissionPlannerStartup)
            {
                var image = global::MissionPlanner.Properties.Resources.splashdark;
                return image == null ? null : new Bitmap(image);
            }
            var bytes = ReadBuiltInResource(preset + ".jpg");
            return bytes == null ? null : AppearanceAssetStore.DecodeImage(bytes);
        }

        private static byte[] ReadBuiltInResource(string filename)
        {
            using (var stream = typeof(DesktopAppearance).Assembly.GetManifestResourceStream("MissionPlanner.Appearance." + filename))
            {
                if (stream == null) return null;
                using (var bytes = new MemoryStream())
                {
                    stream.CopyTo(bytes);
                    return bytes.ToArray();
                }
            }
        }

        internal static Bitmap LoadSelectedStartupImage(out string warning)
        {
            return Store.LoadStartupImage(LoadBuiltInStartup, out warning);
        }

        public static Bitmap CreateStartupPreview(out string warning)
        {
            var custom = LoadSelectedStartupImage(out warning);
            if (custom != null) return custom;
            // Read the exact embedded default used by Splash.Designer; do not open a second splash window.
            var resources = new ComponentResourceManager(typeof(Splash));
            return new Bitmap((Image)resources.GetObject("titanLogo.Image"));
        }
    }
}
#endif
