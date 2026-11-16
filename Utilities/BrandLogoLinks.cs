#if !LIB
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MissionPlanner.Utilities
{
    /// <summary>Two keyboard-accessible destinations forming one proportional brand image.</summary>
    public static class BrandLogoLinks
    {
        public const string ArduPilotUrl = "https://ardupilot.org";
        public const string GitHubUrl = "https://github.com/reliable-ly0411";

        public static void Configure(ToolStrip menu, ToolStripButton left, Action<string> open)
        {
            using (var stream = typeof(BrandLogoLinks).Assembly.GetManifestResourceStream(
                "MissionPlanner.Resources.LocalizedBrandLogo.png"))
            using (var source = Image.FromStream(stream))
            {
                // Keep the existing toolbar height and scale proportionally at its actual DPI.
                int height = Math.Max(24, left.Height - 4);
                int width = Math.Max(2, (int)Math.Round(height * (double)source.Width / source.Height));
                if (width % 2 != 0) width++;
                var right = new ToolStripButton { Name = "MenuLocalizedGitHub" };
                ConfigureHalf(left, source, width, height, false, "ArduPilot", ArduPilotUrl);
                ConfigureHalf(right, source, width, height, true, "GitHub: reliable-ly0411", GitHubUrl);
                right.Click += (sender, args) => open(GitHubUrl);
                // Right-aligned ToolStrip items are arranged from right to left.
                menu.Items.Insert(menu.Items.IndexOf(left), right);
            }
        }

        private static void ConfigureHalf(ToolStripButton button, Image source, int width,
            int height, bool right, string name, string url)
        {
            var image = new Bitmap(width / 2, height);
            using (var graphics = Graphics.FromImage(image))
            {
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(source, new Rectangle(right ? -width / 2 : 0, 0, width, height));
            }
            button.Image = image;
            button.AutoSize = false;
            button.Size = new Size(width / 2, height + 4);
            button.Margin = Padding.Empty;
            button.Padding = Padding.Empty;
            button.Alignment = ToolStripItemAlignment.Right;
            button.DisplayStyle = ToolStripItemDisplayStyle.Image;
            button.ImageScaling = ToolStripItemImageScaling.None;
            button.AccessibleName = name;
            button.AccessibleDescription = url;
            button.ToolTipText = name + "\n" + url;
            button.Text = name;
            // A neutral light surface preserves the supplied black lettering on dark themes.
            button.BackColor = Color.WhiteSmoke;
            button.Disposed += (sender, args) => image.Dispose();
        }
    }
}
#endif
