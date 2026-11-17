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
            label1.Text = UiText.Translate("by Michael Oborne | Modified by lyy0405");
#if !LIB
            InitializeAppearance();
#endif

            Console.WriteLine(strVersion);

            Console.WriteLine("Splash .ctor");
        }

#if !LIB
        private void InitializeAppearance()
        {
            string warning;
            var custom = DesktopAppearance.Store.LoadStartupImage(out warning);
            if (custom != null)
            {
                titanLogo.Image = custom;
                Disposed += (sender, args) => custom.Dispose();
            }
            Icon = ThemeManager.FormIcon;

            // An opaque, content-sized footer keeps provenance readable over every user image.
            var footer = new TableLayoutPanel
            {
                AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 1,
                BackColor = Color.FromArgb(32, 32, 32), ForeColor = Color.White,
                Padding = new Padding(12, 6, 12, 6)
            };
            TXT_version.AutoSize = true;
            TXT_version.ForeColor = Color.White;
            label1.ForeColor = Color.White;
            var upstream = new Label
            {
                AutoSize = true, Text = UiText.Translate("Upstream TitanPlanner: Mohammad Adib"),
                ForeColor = Color.White, BackColor = Color.Transparent
            };
            footer.Controls.Add(TXT_version);
            footer.Controls.Add(label1);
            footer.Controls.Add(upstream);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(titanLogo, 0, 0);
            layout.Controls.Add(footer, 0, 1);
            titanLogo.Margin = new Padding(0);
            Controls.Clear();
            Controls.Add(layout);
            footer.SizeChanged += (sender, args) =>
            {
                foreach (Control label in footer.Controls)
                    label.MaximumSize = new Size(Math.Max(1, footer.ClientSize.Width - footer.Padding.Horizontal - 6), 0);
            };
        }
#endif

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