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
        // Exact unique upstream merge bases for the development source used here.
        // Keep both pins aligned with future upstream integrations, not the moving mirror heads.
        internal const string MissionPlannerSource = "82dd96dfb44bccc51a6e77fcc7c309553b794f39";
        internal const string TitanPlannerSource = "399939a98eaf9f31ee2ae1d003f918419838025b";

        private void InitializeAppearance()
        {
            string warning;
            var custom = DesktopAppearance.LoadSelectedStartupImage(out warning);
            if (custom != null)
            {
                titanLogo.Image = custom;
                Disposed += (sender, args) => custom.Dispose();
            }
            Icon = ThemeManager.FormIcon;
            label1.Hide();
            TXT_version.Hide();

            bool enhancedChinese = string.Equals(System.Globalization.CultureInfo.CurrentUICulture.Name, "zh-CN", StringComparison.Ordinal);
            string mission = MissionPlannerSource.Substring(0, 8);
            string titan = TitanPlannerSource.Substring(0, 8);
            var lines = enhancedChinese
                ? new[] { "Mission Planner 上游基线 " + mission + " · Michael Oborne",
                          "TitanPlanner 上游基线 " + titan + " · Michael Oborne、Mohammad Adib",
                          "修改者：lyy0405" }
                : new[] { "Mission Planner upstream " + mission + " · Michael Oborne",
                          "TitanPlanner upstream " + titan + " · Michael Oborne & Mohammad Adib",
                          "Modified by: lyy0405" };
            titanLogo.AccessibleDescription = string.Join(Environment.NewLine, lines);
            titanLogo.Paint += (sender, args) =>
            {
                DrawCredits(args.Graphics, titanLogo, lines, out _, out _);
            };
        }

        private static Rectangle Fit(Size image, Rectangle canvas)
        {
            if (image.Width <= 0 || image.Height <= 0 || canvas.Width <= 0 || canvas.Height <= 0)
                return Rectangle.Empty;
            float scale = Math.Min(canvas.Width / (float)image.Width, canvas.Height / (float)image.Height);
            int width = (int)(image.Width * scale), height = (int)(image.Height * scale);
            return new Rectangle(canvas.X + (canvas.Width - width) / 2,
                canvas.Y + (canvas.Height - height) / 2, width, height);
        }

        private static Rectangle DrawCredits(Graphics graphics, PictureBox picture, string[] lines,
            out Rectangle[] rows, out float fontPx)
        {
            rows = new Rectangle[0];
            fontPx = 0;
            if (picture.Image == null) return Rectangle.Empty;
            Rectangle image = Fit(picture.Image.Size, picture.ClientRectangle);
            int pad = Math.Max(5, (int)Math.Round(graphics.DpiY / 16.0));
            int stroke = Math.Max(1, Math.Min(3, (int)Math.Round(graphics.DpiY / 72.0)));
            int maxWidth = image.Width - 2 * pad;
            if (maxWidth <= 0 || image.Height <= 2 * pad) return Rectangle.Empty;
            const TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix |
                                          TextFormatFlags.WordBreak | TextFormatFlags.PreserveGraphicsClipping;
            FontStyle style = picture.Font.Style | FontStyle.Bold;
            if (!picture.Font.FontFamily.IsStyleAvailable(style)) style = picture.Font.Style;
            float preferred = picture.Font.SizeInPoints;
            // Preserve the normal half-height limit. A very wide, letterboxed image can
            // fit these credits inside the image but not its bottom half.
            for (int pass = 0; pass < 2; pass++)
            for (float points = preferred; points >= 6; points -= .5f)
            {
                using (var font = new Font(picture.Font.FontFamily, points, style, GraphicsUnit.Point))
                {
                    int width = 0, height = 2 * pad;
                    var heights = new int[lines.Length];
                    for (int i = 0; i < lines.Length; i++)
                    {
                        Size measured = TextRenderer.MeasureText(graphics, lines[i], font,
                            new Size(maxWidth, int.MaxValue), flags);
                        heights[i] = measured.Height;
                        width = Math.Max(width, measured.Width);
                        height += measured.Height;
                    }

                    int maxHeight = pass == 0 ? image.Height / 2 : image.Height;
                    if (width > maxWidth || height > maxHeight) continue;
                    fontPx = points;
                    var bounds = new Rectangle(image.Left, image.Bottom - height, width + 2 * pad, height);
                    rows = new Rectangle[lines.Length];
                    int y = bounds.Top + pad;
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var row = new Rectangle(bounds.Left + pad, y, width, heights[i]);
                        rows[i] = row;
                        y += heights[i];
                        // Draw only glyphs, never a background rectangle. Use the same native text
                        // renderer for the outline and fill so font fallback and wrapping stay aligned.
                        for (int dy = -stroke; dy <= stroke; dy++)
                        for (int dx = -stroke; dx <= stroke; dx++)
                        {
                            if (dx == 0 && dy == 0 || dx * dx + dy * dy > stroke * (stroke + 1)) continue;
                            Rectangle offset = row;
                            offset.Offset(dx, dy);
                            TextRenderer.DrawText(graphics, lines[i], font, offset, Color.FromArgb(20, 20, 20), flags);
                        }
                        TextRenderer.DrawText(graphics, lines[i], font, row, Color.White, flags);
                    }
                    return bounds;
                }
            }
            return Rectangle.Empty;
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