using MissionPlanner.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SkiaSharp;
using ZedGraph;
using Image = System.Drawing.Image;

namespace MissionPlanner.Controls
{
    public partial class SpectrogramUI : Form
    {
        private DFLogBuffer file;

        public SpectrogramUI()
        {
            InitializeComponent();

            zedGraphControl1.MasterPane[0].Title.Text = UiText.Translate("X");
            zedGraphControl1.MasterPane[0].XAxis.Title.Text = "T";
            zedGraphControl1.MasterPane[0].YAxis.Title.Text = UiText.Translate("Frequency");

            zedGraphControl1.MasterPane.Add(new GraphPane());
            zedGraphControl1.MasterPane[1].Title.Text = "Y";
            zedGraphControl1.MasterPane[1].XAxis.Title.Text = "T";
            zedGraphControl1.MasterPane[1].YAxis.Title.Text = UiText.Translate("Frequency");

            zedGraphControl1.MasterPane.Add(new GraphPane());
            zedGraphControl1.MasterPane[2].Title.Text = "Z";
            zedGraphControl1.MasterPane[2].XAxis.Title.Text = "T";
            zedGraphControl1.MasterPane[2].YAxis.Title.Text = UiText.Translate("Frequency");

            zedGraphControl1.AxisChange();

            ThemeManager.ApplyThemeTo(zedGraphControl1);

            zedGraphControl1.Invalidate();
        }

        private void but_loadlog_Click(object sender, EventArgs e)
        {
            using (
                OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "*.log;*.bin|*.log;*.bin;*.BIN;*.LOG";

                ofd.ShowDialog();

                if (!File.Exists(ofd.FileName))
                    return;

                file = new DFLogBuffer(File.OpenRead(ofd.FileName));
            }
        }

        private void SpectrogramUI_Resize(object sender, EventArgs e)
        {

        }

        private void cmb_sensor_SelectedIndexChanged(object sender, EventArgs e)
        {
            // The analysis requires a loaded log; controls can change before the file is chosen.
            if (file == null)
                return;

            var field = new string[] {"AccX", "AccY", "AccZ"};

            if (cmb_sensor.Text.Contains("GYR"))
            {
                field = new string[] {"GyrX", "GyrY", "GyrZ"};
            }

            List<(double timeus, double[] value)> allfftdata;
            double[] freqt;

            // create X Y Z
            using (var img1 = Spectrogram.GenerateImage(file, out freqt, out allfftdata, cmb_sensor.Text, field[0],
                min: (int) num_min.Value, max: (int) num_max.Value))
            using (var img2 = Spectrogram.GenerateImage(file, out freqt, out allfftdata, cmb_sensor.Text, field[1],
                min: (int) num_min.Value, max: (int) num_max.Value))
            using (var img3 = Spectrogram.GenerateImage(file, out freqt, out allfftdata,
                out var singleBatchWindowSeconds, cmb_sensor.Text, field[2],
                min: (int) num_min.Value, max: (int) num_max.Value))
            {
                if (img1 == null || img2 == null || img3 == null)
                {
                    // An empty selection must not display the previous sensor's image.
                    for (int i = 0; i < 3; i++)
                        zedGraphControl1.MasterPane[i].GraphObjList.Clear();
                    zedGraphControl1.Invalidate();
                    return;
                }

                var mintime = allfftdata.Min(a => a.timeus)  / 1000000.0;
                var maxtime = allfftdata.Max(a => a.timeus)  / 1000000.0;

                // A single batch column still spans a full FFT window. Anchor it to the
                // original plotted timestamp, and give the image and axis the same right edge.
                if (img3.Width == 1 && singleBatchWindowSeconds > 0)
                    maxtime = mintime + singleBatchWindowSeconds;

                var tdelta = (maxtime - mintime);

                zedGraphControl1.MasterPane[0].GraphObjList.Clear();
                zedGraphControl1.MasterPane[0].GraphObjList.Add(new ImageObj(img1.ToSpectrogramBitmap(), mintime, freqt.Max(), tdelta, freqt.Max())
                    {ZOrder = ZOrder.F_BehindGrid});
                zedGraphControl1.MasterPane[0].XAxis.Scale.Min = mintime;
                zedGraphControl1.MasterPane[0].XAxis.Scale.Max = maxtime;
                zedGraphControl1.MasterPane[0].YAxis.Scale.Min = 0;
                zedGraphControl1.MasterPane[0].YAxis.Scale.Max = freqt.Max();

                zedGraphControl1.MasterPane[1].GraphObjList.Clear();
                zedGraphControl1.MasterPane[1].GraphObjList.Add(new ImageObj(img2.ToSpectrogramBitmap(), mintime, freqt.Max(), tdelta, freqt.Max())
                    {ZOrder = ZOrder.F_BehindGrid});
                zedGraphControl1.MasterPane[1].XAxis.Scale.Min = mintime;
                zedGraphControl1.MasterPane[1].XAxis.Scale.Max = maxtime;
                zedGraphControl1.MasterPane[1].YAxis.Scale.Min = 0;
                zedGraphControl1.MasterPane[1].YAxis.Scale.Max = freqt.Max();

                zedGraphControl1.MasterPane[2].GraphObjList.Clear();
                zedGraphControl1.MasterPane[2].GraphObjList.Add(new ImageObj(img3.ToSpectrogramBitmap(), mintime, freqt.Max(), tdelta, freqt.Max())
                    {ZOrder = ZOrder.F_BehindGrid});
                zedGraphControl1.MasterPane[2].XAxis.Scale.Min = mintime;
                zedGraphControl1.MasterPane[2].XAxis.Scale.Max = maxtime;
                zedGraphControl1.MasterPane[2].YAxis.Scale.Min = 0;
                zedGraphControl1.MasterPane[2].YAxis.Scale.Max = freqt.Max();

                zedGraphControl1.MasterPane[0].YAxis.Scale.MajorStep = 20;
                zedGraphControl1.MasterPane[0].YAxis.MajorGrid.IsVisible = true;
                zedGraphControl1.MasterPane[1].YAxis.Scale.MajorStep = 20;
                zedGraphControl1.MasterPane[1].YAxis.MajorGrid.IsVisible = true;
                zedGraphControl1.MasterPane[2].YAxis.Scale.MajorStep = 20;
                zedGraphControl1.MasterPane[2].YAxis.MajorGrid.IsVisible = true;

                Bitmap bitmap = new Bitmap( 10, 10 );
                Graphics g = Graphics.FromImage( bitmap );
                zedGraphControl1.MasterPane.DoLayout(g);

                zedGraphControl1.AxisChange();

                zedGraphControl1.Invalidate();
            }
        }

        private void num_min_ValueChanged(object sender, EventArgs e)
        {
            cmb_sensor_SelectedIndexChanged(null, null);
        }

        private void num_max_ValueChanged(object sender, EventArgs e)
        {
            cmb_sensor_SelectedIndexChanged(null, null);
        }

        private void but_redraw_Click(object sender, EventArgs e)
        {
            cmb_sensor_SelectedIndexChanged(null, null);
        }
    }

    public static class SpectrogramBitmapExtensions
    {
        public static Bitmap ToSpectrogramBitmap(this SKBitmap image)
        {
            using (var encoded = image.Encode(SKEncodedImageFormat.Png, 100))
            using (var stream = encoded.AsStream())
            using (var decoded = new Bitmap(stream))
            {
                // The graph owns the copy after the stream and native bitmap are disposed.
                return new Bitmap(decoded);
            }
        }
    }
}
