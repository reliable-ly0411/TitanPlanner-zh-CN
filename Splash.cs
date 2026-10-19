using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows.Forms;
using MissionPlanner.Utilities;

namespace MissionPlanner
{
    public partial class Splash : Form
    {
        public Splash()
        {
            InitializeComponent();

            string strVersion = File.Exists("version.txt")
                ? File.ReadAllText("version.txt").Trim()
                : Assembly.GetExecutingAssembly().GetName().Version.ToString();

            TXT_version.Text = UiText.Translate("Version: Titan ") + strVersion;

            // Use theme color for bottom line instead of hardcoded green
            label1.ForeColor = ThemeManager.BannerColor2;

            Console.WriteLine(strVersion);

            Console.WriteLine("Splash .ctor");
        }

        protected override void OnBackgroundImageChanged(EventArgs e)
        {
            // Mono cannot draw part of a 24 bit image, which the transparent labels need,
            // so they show as blank boxes. A 32 bit copy draws everywhere.
            if (BackgroundImage is Bitmap bitmap && bitmap.PixelFormat != PixelFormat.Format32bppArgb)
            {
                BackgroundImage = new Bitmap(bitmap);
                return;
            }

            base.OnBackgroundImageChanged(e);
        }
    }
}