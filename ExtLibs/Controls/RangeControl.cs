using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MissionPlanner.Controls
{
    public partial class RangeControl : MyUserControl, IDynamicParameterControl
    {
        #region Properties

        public event EventValueChanged ValueChanged;

        public NumericUpDown NumericUpDownControl
        {
            get { return numericUpDown1; }
            set { numericUpDown1 = value; }
        }

        public string DescriptionText;

        private int measuredDescriptionHeight;
        private int fittedTitleHeight;
        private int fittedDescriptionHeight;

        public void FitTextAboveInputs()
        {
            int available = Math.Max(1, ClientSize.Width - 6);
            using (var graphics = CreateGraphics())
            {
                fittedTitleHeight = (int)Math.Ceiling(graphics.MeasureString(LabelText, FontLabel, available).Height);
                fittedDescriptionHeight = (int)Math.Ceiling(graphics.MeasureString(DescriptionText, Font, available).Height);
            }

            int textBottom = fittedTitleHeight + fittedDescriptionHeight + 4;
            int defaultTop = (int)Math.Ceiling(58 * DeviceDpi / 96.0);
            int targetTop = Math.Max(defaultTop, textBottom);

            // The native trackbar can grow beyond the original bottom anchor at high DPI.
            trackBar1.CreateControl();
            Control[] inputs = { trackBar1, numericUpDown1, LBL_min, LBL_max };
            int offset = targetTop - trackBar1.Top;
            int[] positions = inputs.Select(input => input.Top + offset).ToArray();
            int inputBottom = inputs.Max(input => input.Bottom);
            int padding = Math.Max(5, ClientSize.Height - inputBottom);
            ClientSize = new Size(ClientSize.Width, inputBottom + offset + padding);
            for (int i = 0; i < inputs.Length; i++)
                inputs[i].Top = positions[i];
            Invalidate();
        }

        // Opt-in GDI+ measurement for ADS-B; the input controls remain bottom-anchored.
        public void SetMeasuredDescription(string text)
        {
            DescriptionText = text;
            int previous = Math.Max(39, measuredDescriptionHeight);
            int available = Math.Max(1, ClientSize.Width - 6);
            using (var g = CreateGraphics())
                measuredDescriptionHeight = Math.Max(previous, (int)Math.Ceiling(g.MeasureString(text, Font, available).Height));
            Height += measuredDescriptionHeight - previous;
        }

        public string LabelText;

        public TrackBar TrackBarControl
        {
            get { return trackBar1; }
            set { trackBar1 = value; }
        }

        public float Increment
        {
            get { return _increment; }
            set
            {
                _increment = value;
                numericUpDown1.Increment = (decimal) _increment;
                numericUpDown1.DecimalPlaces = _increment.ToString().Length - 1;
            }
        }

        public float DisplayScale { get; set; }

        public float MinRange
        {
            get { return _minrange; }
            set
            {
                _minrange = value;
                numericUpDown1.Minimum = (decimal) (value/DisplayScale);
                LBL_min.Text = numericUpDown1.Minimum.ToString();
            }
        }

        public float MaxRange
        {
            get { return _maxrange; }
            set
            {
                _maxrange = value;
                numericUpDown1.Maximum = (decimal) (value/DisplayScale);
                LBL_max.Text = numericUpDown1.Maximum.ToString();
            }
        }

        private float _minrange = 0;
        private float _maxrange = 10;
        private float _increment = 1;
        private bool intrackbarchange = false;

        #region Interface Properties

        public string Value
        {
            get { return ((float) numericUpDown1.Value*DisplayScale).ToString(CultureInfo.InvariantCulture); }
            set
            {
                float back1 = _minrange;
                float back2 = _maxrange;

                MinRange = (float) Math.Min(MinRange, double.Parse(value));
                MaxRange = (float) Math.Max(MaxRange, double.Parse(value));

                _minrange = back1;
                _maxrange = back2;

                if (double.Parse(value) > _maxrange || double.Parse(value) < _minrange)
                {
                    numericUpDown1.BackColor = Color.Orange;
                }

                numericUpDown1.Value = (decimal) ((float) decimal.Parse(value)/DisplayScale);
                numericUpDown1_ValueChanged(null, null);

                if (ValueChanged != null)
                    ValueChanged(this, Name, Value);
            }
        }

        #endregion

        #endregion

        #region Constructor

        public RangeControl()
        {
            InitializeComponent();
            DisplayScale = 1;

            // disable the mouse wheel
            trackBar1.MouseWheel +=
                delegate (object sender, MouseEventArgs args) { ((HandledMouseEventArgs)args).Handled = true; };
        }

        public RangeControl(string param, String Desc, string Label, float increment, float Displayscale, float minrange,
            float maxrange, string value)
        {
            InitializeComponent();
            setup(param, Desc, Label, increment, Displayscale, minrange, maxrange, value);
        }

        public void setup(string param, string Desc, string Label, float increment, float Displayscale, float minrange,
            float maxrange, string value)
        {
            this.DisplayScale = Displayscale;

            // disable the mouse wheel
            trackBar1.MouseWheel +=
                delegate(object sender, MouseEventArgs args) { ((HandledMouseEventArgs) args).Handled = true; };

            Name = param;
            Increment = increment;
            DescriptionText = Desc;
            LabelText = Label;
            MinRange = minrange;
            MaxRange = maxrange;
            float delta = maxrange - minrange;

            this.Value = value;

            AttachEvents();
        }

        #endregion

        Font FontLabel = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));


        #region Methods

        protected override void OnPaint(PaintEventArgs e)
        {
            if (e.ClipRectangle.IsEmpty)
                return;
            // this is to improve first render time when not onscreen.
            if (!numericUpDown1.Visible)
                numericUpDown1.Visible = true;
            base.OnPaint(e);

            using (var brush = new SolidBrush(Parent.ForeColor))
            {
                if (fittedTitleHeight > 0)
                {
                    int available = Math.Max(1, ClientSize.Width - 6);
                    e.Graphics.DrawString(LabelText, FontLabel, brush, new RectangleF(3, 0, available, fittedTitleHeight));
                    e.Graphics.DrawString(DescriptionText, Font, brush,
                        new RectangleF(3, fittedTitleHeight, available, fittedDescriptionHeight));
                }
                else
                {
                    e.Graphics.DrawString(LabelText, FontLabel, brush, 3, 0);
                    e.Graphics.DrawString(DescriptionText, Font, brush,
                        new RectangleF(3, 15, measuredDescriptionHeight == 0 ? Width : Math.Max(1, ClientSize.Width - 6),
                            measuredDescriptionHeight == 0 ? 39 : measuredDescriptionHeight));
                }
            }
            /*
            e.Graphics.DrawString(LBL_min, this.Font, new SolidBrush(this.Parent.ForeColor),
                new RectangleF(trackBar1.Left, trackBar1.Bottom - this.Font.Height, 100, this.Font.Height));

            var size = e.Graphics.MeasureString(LBL_max, this.Font);
            e.Graphics.DrawString(LBL_max, this.Font, new SolidBrush(this.Parent.ForeColor),
                new RectangleF(trackBar1.Right - size.Width, trackBar1.Bottom - this.Font.Height, size.Width,
                    this.Font.Height));
            */
        }

        public void AttachEvents()
        {
            numericUpDown1.ValueChanged += numericUpDown1_ValueChanged;
            trackBar1.ValueChanged += trackBar1_ValueChanged;
        }

        public void DeAttachEvents()
        {
            numericUpDown1.ValueChanged -= numericUpDown1_ValueChanged;
            trackBar1.ValueChanged -= trackBar1_ValueChanged;
        }

        #endregion

        #region Events

        private decimal map(decimal x, decimal in_min, decimal in_max, decimal out_min, decimal out_max)
        {
            return (x - in_min)*(out_max - out_min)/(in_max - in_min) + out_min;
        }

        protected void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            // update trackbar value
            if (!intrackbarchange)
                trackBar1.Value =
                    (int) map(numericUpDown1.Value, numericUpDown1.Minimum, numericUpDown1.Maximum, 0, 1000);

            // if the increment is divisible by one, always round to an int
            if ((Increment % 1) == 0)
            {
                var round = Math.Round(numericUpDown1.Value, 0);
                if(round > numericUpDown1.Minimum)
                    numericUpDown1.Value = (int) round;
            }

            numericUpDown1.BackColor = Color.Green;

            if ((float) numericUpDown1.Value < (_minrange))
                numericUpDown1.BackColor = Color.Orange;

            if ((float) numericUpDown1.Value > (_maxrange))
                numericUpDown1.BackColor = Color.Orange;

            if (ValueChanged != null)
                ValueChanged(this, Name, Value);
        }

        protected void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            intrackbarchange = true;
            numericUpDown1.Value = map(trackBar1.Value, 0, 1000, numericUpDown1.Minimum, numericUpDown1.Maximum);
            intrackbarchange = false;
        }

        #endregion
    }
}