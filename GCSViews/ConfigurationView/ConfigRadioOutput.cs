using MissionPlanner.Controls;
using MissionPlanner.ArduPilot;
using MissionPlanner.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace MissionPlanner.GCSViews.ConfigurationView
{
    public partial class ConfigRadioOutput : MyUserControl, IActivate, IDeactivate
    {
        // changes are sent once editing pauses for this long, so stepping a value writes it once
        private const int WriteDelayMs = 250;
        // PWM range the min/trim/max boxes and trim slider allow
        private const int ServoMin = 500;
        private const int ServoMax = 2500;
        private const int OutputBarWidth = 200;
        private const int TrimSliderWidth = 420;
        private bool resizingOutputColumns;

        public ConfigRadioOutput()
        {
            InitializeComponent();

            bindingSource1.DataSource = typeof(CurrentState);

            var num_servos = 16;

            // See if 32 servo support is enabled
            if (MainV2.comPort.MAV.param.ContainsKey("SERVO_32_ENABLE") &&
                    (MainV2.comPort.MAV.param["SERVO_32_ENABLE"].Value > 0))
            {
                num_servos = 32;
            }

            SuspendLayout();
            foreach (var i in Enumerable.Range(1, num_servos))
            {
                setup(i);
            }

            ResumeLayout(true);
            flowLayoutPanel1.ClientSizeChanged += (sender, args) => SizeOutputColumns();
            SizeOutputColumns();
        }

        private void SizeOutputColumns()
        {
            if (resizingOutputColumns || flowLayoutPanel1.ClientSize.Width <= 0)
                return;

            const int minOutputWidth = 90, minTrimWidth = 160;
            var minimumColumns = new int[tableLayoutPanel1.ColumnCount];
            var readoutWidths = new Dictionary<Font, int>();
            var readoutsToWiden = new List<KeyValuePair<Control, int>>();
            var bars = tableLayoutPanel1.Controls.OfType<HorizontalProgressBar2>().ToArray();
            var sliders = tableLayoutPanel1.Controls.OfType<TrackBar>().ToArray();
            if (bars.Length == 0 || sliders.Length == 0)
                return;

            foreach (Control control in tableLayoutPanel1.Controls)
            {
                if (control is HorizontalProgressBar2 || control is TrackBar)
                    continue;
                int column = tableLayoutPanel1.GetColumn(control);
                if (column < 0)
                    continue;

                int width = control.AutoSize ? control.GetPreferredSize(Size.Empty).Width : control.Width;
                if (control is NumericUpDown && (column == 4 || column == 6 || column == 7))
                {
                    if (!readoutWidths.TryGetValue(control.Font, out int required))
                    {
                        required = MinimumReadoutWidth(control.Font);
                        readoutWidths.Add(control.Font, required);
                    }
                    if (control.Width < required)
                        readoutsToWiden.Add(new KeyValuePair<Control, int>(control, required));
                    width = Math.Max(width, required);
                }
                minimumColumns[column] = Math.Max(minimumColumns[column], width + control.Margin.Horizontal);
            }

            int barMargin = bars[0].Margin.Horizontal;
            int sliderMargin = sliders[0].Margin.Horizontal;
            int minBar = Math.Min(OutputBarWidth, Math.Max(minOutputWidth, minimumColumns[1] - barMargin));
            int minSlider = Math.Min(TrimSliderWidth, Math.Max(minTrimWidth, minimumColumns[5] - sliderMargin));
            minimumColumns[1] = minimumColumns[5] = 0;
            int available = flowLayoutPanel1.ClientSize.Width - flowLayoutPanel1.Padding.Horizontal -
                tableLayoutPanel1.Margin.Horizontal - tableLayoutPanel1.Padding.Horizontal -
                minimumColumns.Sum() - barMargin - sliderMargin;
            // Keep both controls at a usable minimum and let the existing horizontal scroll reveal the rest.
            int minTotal = minBar + minSlider, maxTotal = OutputBarWidth + TrimSliderWidth;
            int budget = Math.Max(minTotal, Math.Min(available, maxTotal));
            int barWidth = minBar + (budget - minTotal) * (OutputBarWidth - minBar) / Math.Max(1, maxTotal - minTotal);
            int sliderWidth = budget - barWidth;
            if (readoutsToWiden.Count == 0 && bars.All(bar => bar.Width == barWidth) &&
                sliders.All(slider => slider.Width == sliderWidth))
                return;

            resizingOutputColumns = true;
            tableLayoutPanel1.SuspendLayout();
            try
            {
                foreach (var readout in readoutsToWiden)
                    readout.Key.Width = readout.Value;
                foreach (var bar in bars)
                    bar.Width = barWidth;
                foreach (var slider in sliders)
                    slider.Width = sliderWidth;
                // A growing table can retain the old extent after its children shrink.
                tableLayoutPanel1.Width = Math.Max(tableLayoutPanel1.GetPreferredSize(System.Drawing.Size.Empty).Width,
                    flowLayoutPanel1.ClientSize.Width - flowLayoutPanel1.Padding.Horizontal - tableLayoutPanel1.Margin.Horizontal);
            }
            finally
            {
                try { tableLayoutPanel1.ResumeLayout(true); }
                finally { resizingOutputColumns = false; }
            }
        }

        private static int MinimumReadoutWidth(Font font)
        {
            int digits = ServoMax.ToString(CultureInfo.InvariantCulture).Length;
            int textWidth = 0;
            for (char digit = '0'; digit <= '9'; digit++)
                textWidth = Math.Max(textWidth, TextRenderer.MeasureText(new string(digit, digits), font,
                    Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width);
            // A NumericUpDown needs room for its spinner, border and side bearings as well as text.
            return textWidth + SystemInformation.VerticalScrollBarWidth + 12;
        }

        private void setup(int servono)
        {
            var servo = String.Format("SERVO{0}", servono);
            var initializing = true;
            var applyingEquidistant = false;
            var syncingTrimSlider = false;
            System.Windows.Forms.Timer sliderDebounceTimer = null;

            const int controlHeight = 25;
            var horizontalMargin = new System.Windows.Forms.Padding(2, 0, 2, 0);

            var label = new Label()
            {
                Text = servono.ToString(),
                AutoSize = true,
                MinimumSize = new Size(20, controlHeight),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.None,
                Margin = horizontalMargin
            };
            var bAR1 = new HorizontalProgressBar2()
            {
                Minimum = 500,
                Maximum = 2500,
                Value = 1500,
                DrawLabel = true,
                Name = "BAR" + servono,
                Height = controlHeight,
                Width = OutputBarWidth,
                Anchor = AnchorStyles.None,
                Margin = horizontalMargin
            };
            var rev1 = new MissionPlanner.Controls.MavlinkCheckBox()
            {
                WriteInBackground = true,
                BackgroundWriteDelay = WriteDelayMs,
                Enabled = false,
                AutoSize = false,
                Height = controlHeight,
                Width = 20,
                Anchor = AnchorStyles.None,
                Margin = horizontalMargin,
                CheckAlign = System.Drawing.ContentAlignment.MiddleCenter
            };
            var func1 = new MavlinkComboBox()
            {
                WriteInBackground = true,
                BackgroundWriteDelay = WriteDelayMs,
                Enabled = false,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 160,
                Height = controlHeight,
                Anchor = AnchorStyles.None,
                Margin = horizontalMargin
            };
            var min1 = new MavlinkNumericUpDown()
            {
                WriteInBackground = true,
                BackgroundWriteDelay = WriteDelayMs,
                Minimum = 500,
                Maximum = 2500,
                Value = 1500,
                Enabled = false,
                Width = 50,
                Height = controlHeight,
                Anchor = AnchorStyles.None,
                Margin = horizontalMargin
            };
            var trimSlider = new TrackBar()
            {
                Minimum = 500,
                Maximum = 2500,
                Value = 1500,
                Width = TrimSliderWidth,
                Height = controlHeight,
                AutoSize = false,
                TickFrequency = 100,
                SmallChange = 10,
                LargeChange = 10,
                Enabled = false,
                Anchor = AnchorStyles.None,
                Margin = horizontalMargin
            };
            var trim1 = new MavlinkNumericUpDown()
            {
                WriteInBackground = true,
                BackgroundWriteDelay = WriteDelayMs,
                Minimum = 500,
                Maximum = 2500,
                Value = 1500,
                Enabled = false,
                Width = 50,
                Height = controlHeight,
                Anchor = AnchorStyles.None,
                Margin = horizontalMargin
            };
            var max1 = new MavlinkNumericUpDown()
            {
                WriteInBackground = true,
                BackgroundWriteDelay = WriteDelayMs,
                Minimum = 500,
                Maximum = 2500,
                Value = 1500,
                Enabled = false,
                Width = 50,
                Height = controlHeight,
                Anchor = AnchorStyles.None,
                Margin = horizontalMargin
            };
            var minMaxFromTrim1 = new NumericUpDown()
            {
                Minimum = 0,
                Maximum = 1000,
                Value = 0,
                Width = 50,
                Height = controlHeight,
                Visible = false,
                Increment = 10
            };
            var equidistantCheck = new CheckBox()
            {
                Text = "",
                AutoSize = false,
                Height = controlHeight,
                Width = 20,
                Anchor = AnchorStyles.None,
                Margin = new System.Windows.Forms.Padding(8, 0, 0, 0)
            };
            var equidistantPanel = new FlowLayoutPanel()
            {
                AutoSize = false,
                Height = controlHeight,
                Width = 80,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Anchor = AnchorStyles.None,
                Margin = horizontalMargin
            };
            equidistantPanel.Controls.Add(equidistantCheck);
            equidistantPanel.Controls.Add(minMaxFromTrim1);

            this.tableLayoutPanel1.Controls.Add(label, 0, servono);
            this.tableLayoutPanel1.Controls.Add(bAR1, 1, servono);
            this.tableLayoutPanel1.Controls.Add(rev1, 2, servono);
            this.tableLayoutPanel1.Controls.Add(func1, 3, servono);
            this.tableLayoutPanel1.Controls.Add(min1, 4, servono);
            this.tableLayoutPanel1.Controls.Add(trimSlider, 5, servono);
            this.tableLayoutPanel1.Controls.Add(trim1, 6, servono);
            this.tableLayoutPanel1.Controls.Add(max1, 7, servono);
            this.tableLayoutPanel1.Controls.Add(equidistantPanel, 8, servono);

            bAR1.DataBindings.Add("Value", bindingSource1, "ch" + servono + "out");
            rev1.setup(1, 0, servo + "_REVERSED", MainV2.comPort.MAV.param);
            var firmware = MainV2.comPort.MAV.cs.firmware;
            var functions = ParameterMetaDataRepository.GetParameterOptionsInt(servo + "_FUNCTION", firmware.ToString());
            ServoFunctionCaptions.Attach(func1, functions, firmware);
            func1.setup(functions, servo + "_FUNCTION", MainV2.comPort.MAV.param);
            min1.setup(ServoMin, ServoMax, 1, 1, servo + "_MIN", MainV2.comPort.MAV.param);
            trim1.setup(ServoMin, ServoMax, 1, 1, servo + "_TRIM", MainV2.comPort.MAV.param);
            max1.setup(ServoMin, ServoMax, 1, 1, servo + "_MAX", MainV2.comPort.MAV.param);
            // range and step like the trim slider; set after setup, which takes them from the param metadata
            foreach (var pwm in new[] { min1, trim1, max1 })
            {
                pwm.Minimum = Math.Min(ServoMin, pwm.Value);
                pwm.Maximum = Math.Max(ServoMax, pwm.Value);
                pwm.Increment = 10;
            }

            // Enable slider if trim parameter exists
            if (MainV2.comPort.MAV.param.ContainsKey(servo + "_TRIM"))
            {
                trimSlider.Enabled = true;
            }

            equidistantCheck.CheckedChanged += (sender, e) =>
            {
                if (initializing)
                    return;

                var checkedState = equidistantCheck.Checked;
                min1.Enabled = !checkedState;
                max1.Enabled = !checkedState;
                minMaxFromTrim1.Visible = checkedState;

                if (checkedState)
                {
                    applyingEquidistant = true;
                    ApplyEquidistantRange(servo, trim1, min1, max1, minMaxFromTrim1);
                    applyingEquidistant = false;
                }
            };

            // Add event handler for min/max from trim control
            minMaxFromTrim1.ValueChanged += (sender, e) =>
            {
                if (initializing)
                    return;

                if (!equidistantCheck.Checked)
                    return;

                applyingEquidistant = true;
                ApplyEquidistantRange(servo, trim1, min1, max1, minMaxFromTrim1);
                applyingEquidistant = false;
            };

            // Add event handler for trim changes - uncheck equidistant when trim changes in ANY way
            trim1.ValueChanged += (sender, e) =>
            {
                if (initializing || applyingEquidistant)
                    return;

                // Sync slider with trim value (only if not already syncing)
                if (!syncingTrimSlider)
                {
                    syncingTrimSlider = true;
                    try
                    {
                        var newSliderValue = Math.Max(trimSlider.Minimum, Math.Min(trimSlider.Maximum, (int)trim1.Value));
                        if (trimSlider.Value != newSliderValue)
                        {
                            trimSlider.Value = newSliderValue;
                        }
                    }
                    finally
                    {
                        syncingTrimSlider = false;
                    }
                }

                // Always uncheck equidistant when trim changes
                if (equidistantCheck.Checked)
                {
                    equidistantCheck.Checked = false;
                }
            };

            // Add event handler for slider changes - sync with trim (debounced)
            trimSlider.ValueChanged += (sender, e) =>
            {
                if (initializing || syncingTrimSlider)
                    return;

                // Round to nearest 10 immediately for visual feedback
                var roundedValue = (int)(Math.Round(trimSlider.Value / 10.0) * 10);
                if (trimSlider.Value != roundedValue)
                {
                    syncingTrimSlider = true;
                    try
                    {
                        trimSlider.Value = roundedValue;
                    }
                    finally
                    {
                        syncingTrimSlider = false;
                    }
                    // Don't return - continue to debounce with the rounded value
                }

                // Debounce: wait 0.3 seconds before syncing to trim
                if (sliderDebounceTimer != null)
                {
                    sliderDebounceTimer.Stop();
                    sliderDebounceTimer.Dispose();
                    sliderDebounceTimer = null;
                }

                var currentSliderValue = trimSlider.Value;
                sliderDebounceTimer = new System.Windows.Forms.Timer();
                sliderDebounceTimer.Interval = 300; // 0.3 seconds
                sliderDebounceTimer.Tick += (s, args) =>
                {
                    sliderDebounceTimer.Stop();
                    sliderDebounceTimer.Dispose();
                    sliderDebounceTimer = null;

                    // Sync trim with slider value
                    syncingTrimSlider = true;
                    try
                    {
                        if (trim1.Value != currentSliderValue)
                        {
                            trim1.Value = currentSliderValue;
                        }
                    }
                    finally
                    {
                        syncingTrimSlider = false;
                    }
                };
                sliderDebounceTimer.Start();
            };

            // Add event handlers for min/max changes - uncheck equidistant if values change
            min1.ValueChanged += (sender, e) =>
            {
                if (initializing || applyingEquidistant)
                    return;

                if (equidistantCheck.Checked)
                {
                    equidistantCheck.Checked = false;
                }
            };

            max1.ValueChanged += (sender, e) =>
            {
                if (initializing || applyingEquidistant)
                    return;

                if (equidistantCheck.Checked)
                {
                    equidistantCheck.Checked = false;
                }
            };

            if (IsEquidistant(min1.Value, trim1.Value, max1.Value, out var offset))
            {
                minMaxFromTrim1.Value = Math.Min(minMaxFromTrim1.Maximum, Math.Max(minMaxFromTrim1.Minimum, offset));
                equidistantCheck.Checked = true;
            }

            // Sync slider with trim value after all setup is complete
            if (MainV2.comPort.MAV.param.ContainsKey(servo + "_TRIM"))
            {
                trimSlider.Value = Math.Max(trimSlider.Minimum, Math.Min(trimSlider.Maximum, (int)trim1.Value));
            }

            initializing = false;

            if (equidistantCheck.Checked)
            {
                min1.Enabled = false;
                max1.Enabled = false;
                minMaxFromTrim1.Visible = true;
            }

            // Subscribe to trim1 value changes that happen after setup (e.g., param refresh)
            trim1.EnabledChanged += (sender, e) =>
            {
                if (trim1.Enabled && !syncingTrimSlider)
                {
                    syncingTrimSlider = true;
                    try
                    {
                        var newValue = Math.Max(trimSlider.Minimum, Math.Min(trimSlider.Maximum, (int)trim1.Value));
                        if (trimSlider.Value != newValue)
                        {
                            trimSlider.Value = newValue;
                        }
                    }
                    finally
                    {
                        syncingTrimSlider = false;
                    }
                }
            };
        }

        private static void ApplyEquidistantRange(string servo, NumericUpDown trim, NumericUpDown min, NumericUpDown max,
            NumericUpDown offset)
        {
            var trimValue = trim.Value;
            var newMin = ClampServoValue(trimValue - offset.Value);
            var newMax = ClampServoValue(trimValue + offset.Value);

            min.Value = newMin;
            max.Value = newMax;

            // Explicitly write the new limits so the change is not UI-only
            WriteServoRange(servo, newMin, newMax);
        }

        private static decimal ClampServoValue(decimal value)
        {
            if (value < ServoMin) return ServoMin;
            if (value > ServoMax) return ServoMax;
            return value;
        }

        private static bool IsEquidistant(decimal min, decimal trim, decimal max, out decimal offset)
        {
            offset = 0;
            if (trim < min || trim > max)
                return false;

            var diffLow = trim - min;
            var diffHigh = max - trim;

            if (diffLow == diffHigh)
            {
                offset = diffLow;
                return true;
            }

            return false;
        }

        private static void WriteServoRange(string servo, decimal newMin, decimal newMax)
        {
            if (MainV2.comPort?.BaseStream == null || !MainV2.comPort.BaseStream.IsOpen)
                return;

            var minParam = servo + "_MIN";
            var maxParam = servo + "_MAX";

            if (MainV2.comPort.MAV?.param == null)
                return;

            if (MainV2.comPort.MAV.param.ContainsKey(minParam))
                ParamWriteQueue.Write(minParam, (double)newMin, null, WriteDelayMs);

            if (MainV2.comPort.MAV.param.ContainsKey(maxParam))
                ParamWriteQueue.Write(maxParam, (double)newMax, null, WriteDelayMs);
        }

        public void Activate()
        {
            timer1.Start();
        }

        public void Deactivate()
        {
            timer1.Stop();
            // leaving the page sends what is still waiting out the write delay
            ParamWriteQueue.Flush();
        }


        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                MainV2.comPort.MAV.cs.UpdateCurrentSettings(bindingSource1.UpdateDataSource(MainV2.comPort.MAV.cs));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
