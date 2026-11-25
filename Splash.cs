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

 static Rectangle Fit(Size image,Rectangle canvas){
  if(image.Width<=0||image.Height<=0||canvas.Width<=0||canvas.Height<=0)return Rectangle.Empty;
  float scale=Math.Min(canvas.Width/(float)image.Width,canvas.Height/(float)image.Height);
  int w=(int)(image.Width*scale),h=(int)(image.Height*scale);
  return new Rectangle(canvas.X+(canvas.Width-w)/2,canvas.Y+(canvas.Height-h)/2,w,h);
 }
 static Rectangle DrawCredits(Graphics g,PictureBox picture,string[] lines,out Rectangle[] rows,out float fontPx){
  rows=new Rectangle[0];fontPx=0;
  if(picture.Image==null)return Rectangle.Empty;
  Rectangle image=Fit(picture.Image.Size,picture.ClientRectangle);
  int pad=Math.Max(5,(int)Math.Round(picture.DeviceDpi/16.0));
  const TextFormatFlags flags=TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix|TextFormatFlags.WordBreak;
  float preferred=picture.Font.SizeInPoints;
  for(float points=preferred;points>=6;points-=.5f){
   using(var f=new Font(picture.Font.FontFamily,points,picture.Font.Style,GraphicsUnit.Point)){
    int maxW=0;
    var heights=new int[lines.Length];
    for(int i=0;i<lines.Length;i++){var s=TextRenderer.MeasureText(g,lines[i],f,new Size(image.Width-2*pad,int.MaxValue),flags);heights[i]=s.Height;maxW=Math.Max(maxW,s.Width);}
    int neededHeight=2*pad;foreach(int h in heights)neededHeight+=h;
    if(maxW+2*pad>image.Width || neededHeight>image.Height/2)continue;
    fontPx=points;
    var panel=new Rectangle(image.Left,image.Bottom-neededHeight,maxW+2*pad,neededHeight);
    using(var bg=new SolidBrush(Color.FromArgb(218,18,18,18)))g.FillRectangle(bg,panel);
    rows=new Rectangle[lines.Length];
    int y=panel.Top+pad;
    for(int i=0;i<lines.Length;i++){
      rows[i]=new Rectangle(panel.Left+pad,y,panel.Width-2*pad,heights[i]);y+=heights[i];
      if(!panel.Contains(rows[i]))throw new InvalidOperationException("credit box");
      TextRenderer.DrawText(g,lines[i],f,rows[i],Color.White,flags);
    }
    return panel;
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