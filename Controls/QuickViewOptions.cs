using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using MissionPlanner.Utilities;

namespace MissionPlanner.Controls
{
    public partial class QuickViewOptions : Form
    {
        private QuickView _qv;
        private string _displaySourceKey;
        private bool _initializing = true;  // Flag to suppress event handlers during form setup

        public QuickViewOptions(QuickView qv)
        {
            InitializeComponent();
            _qv = qv;
            this.Icon = qv.FindForm()?.Icon;
        }

        List<Tuple<string, string>> all_fields = new List<Tuple<string, string>>();

        // Stores the original value of the textbox background color to restore after error highlighting
        private Color backup_backcolor = Color.Empty;

        private void QuickViewOptions_Shown(object sender, EventArgs e)
        {
            backup_backcolor = TXT_color.BackColor;

            // Populate combobox with variables
            object thisBoxed = MainV2.comPort.MAV.cs;
            Type test = thisBoxed.GetType();

            foreach (var field in test.GetProperties())
            {
                object fieldValue = field.GetValue(thisBoxed, null); // Get value
                if (fieldValue == null)
                    continue;

                if (!fieldValue.IsNumber() && !(fieldValue is bool))
                {
                    continue;
                }

                if (field.Name.Contains("customfield"))
                {
                    if (CurrentState.custom_field_names.ContainsKey(field.Name))
                    {
                        string name = CurrentState.custom_field_names[field.Name];
                        all_fields.Add(new Tuple<string, string>(field.Name, name));
                    }
                }
                else
                {
                    // Enhanced Chinese shares the HUD and Status catalog. Keep the established fallback.
                    var display = field.Name;
                    if (string.Equals(System.Globalization.CultureInfo.CurrentUICulture.Name, "zh-CN", StringComparison.OrdinalIgnoreCase))
                    {
                        display = TelemetryFieldPresentation.FieldLabel(field.Name);
                        if (display == field.Name)
                            display = TelemetryFieldPresentation.Combine(field.Name, PressureFieldDescription(field.Name));
                    }
                    if (display == field.Name)
                    {
                        var description = UiText.Translate(MainV2.comPort.MAV.cs.GetNameandUnit(field.Name));
                        display = description == field.Name ? field.Name : field.Name + " (" + description + ")";
                    }
                    all_fields.Add(new Tuple<string, string>(field.Name, display));
                }
            }

            // Sort all_fields by display name
            all_fields.Sort((x, y) => x.Item2.CompareTo(y.Item2));

            // Fetch the selected variable name from the tag of the QuickView
            string variable_name = "";
            try
            {
                variable_name = (string)_qv.Tag;
            }
            catch
            {
                ; // silently fail
            }


            if (string.Equals(System.Globalization.CultureInfo.CurrentUICulture.Name, "zh-CN", StringComparison.OrdinalIgnoreCase))
            {
                CMB_Source.DrawMode = DrawMode.OwnerDrawFixed;
                CMB_Source.ItemHeight = Math.Max(CMB_Source.ItemHeight, CMB_Source.Font.Height + 2);
                CMB_Source.DrawItem += DrawSourceItem;
                CMB_Source.DropDown += SizeSourceDropDown;
            }

            // Initialize combobox
            CMB_Source.ValueMember = "Item1";
            CMB_Source.DisplayMember = "Item2";
            CMB_Source.DataSource = all_fields;
            CMB_Source.SelectedValue = variable_name;
            _displaySourceKey = (CMB_Source.SelectedItem as Tuple<string, string>)?.Item1;
            if (CMB_Source.DrawMode == DrawMode.OwnerDrawFixed)
                SizeSourceDropDown(CMB_Source, EventArgs.Empty);

            // Initialize precision field, check if _qv.numberformat is in the form "0" or "0.00" etc.
            // and count the zeros after the decimal if it is
            if (_qv.numberformat == "0")
            {
                NUM_precision.Value = 0;
            }
            else if (Regex.IsMatch(_qv.numberformat, @"^0\.0+$"))
            {
                int precision = _qv.numberformat.Substring(_qv.numberformat.IndexOf('.') + 1).Length;
                if (precision > NUM_precision.Maximum)
                {
                    NUM_precision.Maximum = precision;
                }
                NUM_precision.Value = precision;
            }
            else
            {
                // Non-standard format - will be handled by custom format
                NUM_precision.Value = 2;
            }

            // Initialize label
            TXT_customlabel.Text = _qv.desc;

            // Initialize color
            string colorname = _qv.numberColorBackup.Name;
            if (colorname.ToLower().StartsWith("ff") && colorname.Length == 8)
            {
                TXT_color.Text = colorname.Substring(2);
            }
            else
            {
                TXT_color.Text = colorname;
            }
            BUT_colorpicker.BackColor = _qv.numberColorBackup;

            // Initialize format
            string currentFormat = _qv.numberformat;
            TXT_customformat.Text = currentFormat.Replace("\\:", ":");

            // Initialize offset and scale
            double scale = _qv.scale;
            double offset = _qv.offset;
            TXT_scale.Text = scale.ToString("0.0########");
            TXT_offset.Text = offset.ToString("0.0########");

            // Initialize gauge settings - set text values BEFORE checkbox to avoid
            // triggering CHK_gauge_CheckedChanged with empty/default values
            TXT_gaugeMin.Text = _qv.gaugeMin.ToString("0.#######");
            TXT_gaugeMax.Text = _qv.gaugeMax.ToString("0.#######");
            CHK_gauge.Checked = _qv.isGauge;
            TXT_gaugeMin.Enabled = CHK_gauge.Checked;
            TXT_gaugeMax.Enabled = CHK_gauge.Checked;
            label3.Enabled = CHK_gauge.Checked;
            label4.Enabled = CHK_gauge.Checked;

            // Initialize color-coding settings - set text values BEFORE checkbox
            TXT_redValue.Text = _qv.colorRedValue.ToString("0.#######");
            TXT_yellowValue.Text = _qv.colorYellowValue.ToString("0.#######");
            TXT_greenValue.Text = _qv.colorGreenValue.ToString("0.#######");
            CHK_colorCode.Checked = _qv.isColorCoded;
            SetColorCodeFieldsEnabled(CHK_colorCode.Checked);

            // Done initializing - now event handlers can save settings
            _initializing = false;
        }


        private static string PressureFieldDescription(string key)
        {
            // CurrentState keeps the input values rather than normalizing them for display.
            // Primary temperature can be cdegC (SCALED_PRESSURE) or degC
            // (HIGHRES_IMU or HIGH_LATENCY); only attach units shared by all input paths.
            switch (key)
            {
                case "press_abs": return "主绝对气压 (hPa)";
                case "press_temp": return "主温度原值（单位随消息来源）";
                case "press_abs2": return "第二气压计绝对气压 (hPa)";
                case "press_temp2": return "第二气压计温度 (cdegC)";
                default: return null;
            }
        }

        private int _sourceKeyWidth;

        private const TextFormatFlags SourceTextFlags = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding |
            TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;

        private static string SourceDescription(Tuple<string, string> item)
        {
            string prefix = item.Item1 + " (";
            return !item.Item1.Contains("customfield") && item.Item2.StartsWith(prefix, StringComparison.Ordinal) &&
                item.Item2.EndsWith(")", StringComparison.Ordinal)
                ? item.Item2.Substring(prefix.Length, item.Item2.Length - prefix.Length - 1) : null;
        }

        private void SizeSourceDropDown(object sender, EventArgs e)
        {
            // Always use the full catalog so search results do not shift the description column.
            int descriptionWidth = 0;
            int singleWidth = 0;
            using (var graphics = CMB_Source.CreateGraphics())
            {
                int Measure(string value) => TextRenderer.MeasureText(graphics, value, CMB_Source.Font,
                    new Size(32767, 32767), TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;
                _sourceKeyWidth = 0;
                foreach (var item in all_fields)
                {
                    var description = SourceDescription(item);
                    if (description == null)
                        singleWidth = Math.Max(singleWidth, Measure(item.Item2));
                    else
                    {
                        _sourceKeyWidth = Math.Max(_sourceKeyWidth, Measure(item.Item1));
                        descriptionWidth = Math.Max(descriptionWidth, Measure(description));
                    }
                }
            }

            int padding = CMB_Source.Font.Height;
            var workArea = Screen.FromControl(CMB_Source).WorkingArea;
            var comboBounds = CMB_Source.RectangleToScreen(CMB_Source.ClientRectangle);
            int available = Math.Max(workArea.Right - comboBounds.Left, comboBounds.Right - workArea.Left);
            int wanted = Math.Max(singleWidth, _sourceKeyWidth + padding / 2 + descriptionWidth) +
                padding + SystemInformation.VerticalScrollBarWidth;
            CMB_Source.DropDownWidth = Math.Max(CMB_Source.Width, Math.Min(available, wanted));
        }

        private int SourceDescriptionOffset(int availableWidth)
        {
            int start = _sourceKeyWidth + Math.Max(4, CMB_Source.Font.Height / 2);
            return _sourceKeyWidth > 0 && availableWidth - start >= CMB_Source.Font.Height * 4 ? start : 0;
        }

        private void DrawSourceItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            if (e.Index < 0 || e.Index >= CMB_Source.Items.Count)
                return;
            var item = (Tuple<string, string>)CMB_Source.Items[e.Index];
            Rectangle bounds = Rectangle.Inflate(e.Bounds, -3, 0);
            if (bounds.Width <= 0)
                return;

            // The editable box and constrained dropdown keep the complete textual label.
            var description = (e.State & DrawItemState.ComboBoxEdit) == 0 ? SourceDescription(item) : null;
            int offset = description == null ? 0 : SourceDescriptionOffset(bounds.Width);
            if (offset > 0)
            {
                var keyBounds = new Rectangle(bounds.X, bounds.Y, _sourceKeyWidth, bounds.Height);
                var descriptionBounds = new Rectangle(bounds.X + offset, bounds.Y, bounds.Width - offset, bounds.Height);
                TextRenderer.DrawText(e.Graphics, item.Item1, e.Font, keyBounds, e.ForeColor, SourceTextFlags);
                TextRenderer.DrawText(e.Graphics, description, e.Font, descriptionBounds, e.ForeColor, SourceTextFlags);
            }
            else
                TextRenderer.DrawText(e.Graphics, item.Item2, e.Font, bounds, e.ForeColor, SourceTextFlags);
            e.DrawFocusRectangle();
        }

        private void SetColorCodeFieldsEnabled(bool enabled)
        {
            TXT_redValue.Enabled = enabled;
            TXT_yellowValue.Enabled = enabled;
            TXT_greenValue.Enabled = enabled;
            LBL_redValue.Enabled = enabled;
            LBL_yellowValue.Enabled = enabled;
            LBL_greenValue.Enabled = enabled;
        }

        private void NUM_precision_ValueChanged(object sender, EventArgs e)
        {
            if (_initializing)
                return;

            // Update the format textbox based on precision
            if (NUM_precision.Value == 0)
            {
                TXT_customformat.Text = "0";
            }
            else
            {
                TXT_customformat.Text = "0." + new string('0', (int)NUM_precision.Value);
            }
        }

        private void TXT_customlabel_TextChanged(object sender, EventArgs e)
        {
            // No action needed - value is read on form close
        }

        private void TXT_color_TextChanged(object sender, EventArgs e)
        {
            if (_initializing)
                return;

            if (Regex.Match(TXT_color.Text, "^(?:[0-9a-fA-F]{2}){3,4}$").Success)
            {
                BUT_colorpicker.BackColor = System.Drawing.ColorTranslator.FromHtml("#"+TXT_color.Text);
                return;
            }
            try
            {
                BUT_colorpicker.BackColor = System.Drawing.ColorTranslator.FromHtml(TXT_color.Text);
            }
            catch(Exception)
            {
                // Not a valid color string
            }
        }

        private void TXT_customformat_TextChanged(object sender, EventArgs e)
        {
            if (_initializing)
                return;

            // Check if this is a valid format
            string numberformat = TXT_customformat.Text.Replace(":", "\\:");
            double test_number = 125.2;
            try
            {
                if (numberformat.Contains(":"))
                {
                    TimeSpan.FromSeconds(test_number).ToString(numberformat);
                }
                else
                {
                    test_number.ToString(numberformat);
                }
                TXT_customformat.BackColor = backup_backcolor;
            }
            catch (FormatException)
            {
                TXT_customformat.BackColor = Color.Red;
            }
        }

        private void BUT_colorpicker_Click(object sender, EventArgs e)
        {
            ColorDialog colorDlg = new ColorDialog();
            colorDlg.Color = BUT_colorpicker.BackColor;

            if (colorDlg.ShowDialog() == DialogResult.OK)
            {
                BUT_colorpicker.BackColor = colorDlg.Color;
                // Get rid of opacity from name
                string colorname = colorDlg.Color.Name;
                if(colorname.ToLower().StartsWith("ff") && colorname.Length == 8)
                {
                    TXT_color.Text = colorname.Substring(2);
                }
                else
                {
                    TXT_color.Text = colorname;
                }
            }
        }

        private void TXT_scale_offset_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Match the culture already used to display and parse these values.
            var separator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            // KeyPress supplies one character; keep the previous behavior for custom multi-character separators.
            char decimalKey = separator.Length == 1 ? separator[0] : '.';
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != decimalKey) && (e.KeyChar != '-'))
            {
                e.Handled = true;
            }

            // only allow one decimal point
            if ((e.KeyChar == decimalKey) && ((sender as TextBox).Text.IndexOf(decimalKey) > -1))
            {
                e.Handled = true;
            }

            // only allow negative sign first
            if ((e.KeyChar == '-') && ((sender as TextBox).SelectionStart > 0))
            {
                e.Handled = true;
            }
        }

        private void TXT_scale_TextChanged(object sender, EventArgs e)
        {
            // Validate input - show red if invalid
            if (!double.TryParse(((TextBox)sender).Text, out _))
            {
                ((Control)sender).BackColor = Color.Red;
            }
            else
            {
                ((Control)sender).BackColor = backup_backcolor;
            }
        }

        private void TXT_offset_TextChanged(object sender, EventArgs e)
        {
            // Validate input - show red if invalid
            if (!double.TryParse(((TextBox)sender).Text, out _))
            {
                ((Control)sender).BackColor = Color.Red;
            }
            else
            {
                ((Control)sender).BackColor = backup_backcolor;
            }
        }

        private void CHK_gauge_CheckedChanged(object sender, EventArgs e)
        {
            bool is_checked = CHK_gauge.Checked;
            TXT_gaugeMin.Enabled = is_checked;
            TXT_gaugeMax.Enabled = is_checked;
            label3.Enabled = is_checked;
            label4.Enabled = is_checked;
        }

        private void TXT_gaugeMin_TextChanged(object sender, EventArgs e)
        {
            // Validate input - show red if invalid
            if (!double.TryParse(TXT_gaugeMin.Text, out _))
            {
                TXT_gaugeMin.BackColor = Color.Red;
            }
            else
            {
                TXT_gaugeMin.BackColor = backup_backcolor;
            }
        }

        private void TXT_gaugeMax_TextChanged(object sender, EventArgs e)
        {
            // Validate input - show red if invalid
            if (!double.TryParse(TXT_gaugeMax.Text, out _))
            {
                TXT_gaugeMax.BackColor = Color.Red;
            }
            else
            {
                TXT_gaugeMax.BackColor = backup_backcolor;
            }
        }

        private void CHK_colorCode_CheckedChanged(object sender, EventArgs e)
        {
            SetColorCodeFieldsEnabled(CHK_colorCode.Checked);
        }

        private void TXT_colorValue_TextChanged(object sender, EventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;

            // Validate input - show red if invalid
            if (!double.TryParse(tb.Text, out _))
            {
                tb.BackColor = Color.Red;
            }
            else
            {
                tb.BackColor = backup_backcolor;
            }
        }

        // Runs when the user types in the box, but not when the text is changed programatically
        private void CMB_Source_TextUpdate(object sender, EventArgs e)
        {
            List<Tuple<string, string>> filtered_fields;
            
            // Back up cursor position and text
            int cursor_pos = CMB_Source.SelectionStart;
            string searchText = CMB_Source.Text.ToLower();
            if (false)//CMB_Source.SelectedIndex > 0 || searchText == "")
            {
                filtered_fields = new List<Tuple<string, string>>(all_fields);
            }
            else
            {
                // Check all the fields in the table and see if any of them contain `searchText` as a DisplayName
                filtered_fields = all_fields.Where(f => f.Item2.ToLower().Contains(searchText.ToLower())).ToList();
                CMB_Source.DroppedDown = true;
                Cursor.Current = Cursors.Default;
            }

            CMB_Source.DataSource = filtered_fields;
            CMB_Source.Text = searchText;
            CMB_Source.SelectionStart = cursor_pos;
        }

        private void CMB_Source_DropDownClosed(object sender, EventArgs e)
        {
            if (CMB_Source.SelectedItem is Tuple<string, string> field)
            {
                if (!string.Equals(_displaySourceKey, field.Item1, StringComparison.Ordinal))
                {
                    // Reset display settings only when a different source is chosen.
                    TXT_customlabel.Text = MainV2.comPort.MAV.cs.GetNameandUnit((string)CMB_Source.SelectedValue);
                    TXT_scale.Text = "1.0";
                    TXT_offset.Text = "0.0";
                    _displaySourceKey = field.Item1;
                }

                // Restore the full list
                CMB_Source.DataSource = all_fields;
                CMB_Source.SelectedItem = field;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            ApplySettingsToQuickView();
        }

        private void ApplySettingsToQuickView()
        {
            // Suspend layout on the parent TableLayoutPanel while applying changes
            var parentTable = _qv.Parent as System.Windows.Forms.TableLayoutPanel;
            parentTable?.SuspendLayout();
            _qv.SuspendLayout();

            try
            {
                // Apply label from form
                _qv.desc = TXT_customlabel.Text;

                // Apply color from form
                _qv.numberColor = BUT_colorpicker.BackColor;
                _qv.numberColorBackup = _qv.numberColor;

                // Apply format from form
                string format = TXT_customformat.Text.Replace(":", "\\:");
                _qv.numberformat = !string.IsNullOrEmpty(format) ? format : "0.00";

                // Apply scale and offset from form
                if (double.TryParse(TXT_scale.Text, out double scale))
                    _qv.scale = scale;
                else
                    _qv.scale = 1;

                if (double.TryParse(TXT_offset.Text, out double offset))
                    _qv.offset = offset;
                else
                    _qv.offset = 0;

                // Apply gauge settings from form
                _qv.isGauge = CHK_gauge.Checked;

                if (double.TryParse(TXT_gaugeMin.Text, out double gaugeMin))
                    _qv.gaugeMin = gaugeMin;
                else
                    _qv.gaugeMin = 0;

                if (double.TryParse(TXT_gaugeMax.Text, out double gaugeMax))
                    _qv.gaugeMax = gaugeMax;
                else
                    _qv.gaugeMax = 100;

                // Apply color-coding settings from form
                _qv.isColorCoded = CHK_colorCode.Checked;

                if (double.TryParse(TXT_redValue.Text, out double redValue))
                    _qv.colorRedValue = redValue;
                else
                    _qv.colorRedValue = 0;

                if (double.TryParse(TXT_yellowValue.Text, out double yellowValue))
                    _qv.colorYellowValue = yellowValue;
                else
                    _qv.colorYellowValue = 50;

                if (double.TryParse(TXT_greenValue.Text, out double greenValue))
                    _qv.colorGreenValue = greenValue;
                else
                    _qv.colorGreenValue = 100;

                // Update the data source if changed
                if (CMB_Source.SelectedItem is Tuple<string, string> selectedField)
                {
                    string desc = selectedField.Item1;

                    // Handle customfield
                    if (desc.StartsWith("customfield"))
                    {
                        _qv.Tag = "customfield:" + selectedField.Item2;
                    }
                    else
                    {
                        _qv.Tag = desc;
                    }

                    // Rebind to new source
                    _qv.DataBindings.Clear();
                    try
                    {
                        var bindingSource = _qv.Parent?.Parent?.Controls.OfType<System.Windows.Forms.BindingSource>().FirstOrDefault();
                        if (bindingSource == null)
                        {
                            // Try to find it from the form
                            var form = _qv.FindForm();
                            if (form != null)
                            {
                                var field = form.GetType().GetField("bindingSourceQuickTab",
                                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                if (field != null)
                                {
                                    bindingSource = field.GetValue(form) as System.Windows.Forms.BindingSource;
                                }
                            }
                        }

                        if (bindingSource != null)
                        {
                            var b = new System.Windows.Forms.Binding("number", bindingSource, selectedField.Item1, true);
                            _qv.DataBindings.Add(b);
                        }
                    }
                    catch { }
                }

                _qv.Invalidate();

                // Trigger save of all dashboard items
                try
                {
                    // Find the FlightData instance
                    if (MissionPlanner.GCSViews.FlightData.instance != null)
                    {
                        // Use reflection to call the private SaveQuickViewArrangement method
                        var method = typeof(MissionPlanner.GCSViews.FlightData).GetMethod("SaveQuickViewArrangement",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        method?.Invoke(MissionPlanner.GCSViews.FlightData.instance, null);
                    }
                }
                catch { }
            }
            finally
            {
                // Resume layout
                _qv.ResumeLayout(true);
                parentTable?.ResumeLayout(true);
            }
        }
    }
}
