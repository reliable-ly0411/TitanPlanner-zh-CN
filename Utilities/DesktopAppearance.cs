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
            windowIcon = Store.LoadIcon(out warning);
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

        public static Bitmap CreateStartupPreview(out string warning)
        {
            var custom = Store.LoadStartupImage(out warning);
            if (custom != null) return custom;
            // Read the exact embedded default used by Splash.Designer; do not open a second splash window.
            var resources = new ComponentResourceManager(typeof(Splash));
            return new Bitmap((Image)resources.GetObject("titanLogo.Image"));
        }
    }
}
#endif
