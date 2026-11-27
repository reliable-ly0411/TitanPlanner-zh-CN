using MissionPlanner.ArduPilot;
using MissionPlanner.Controls;
using MissionPlanner.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace MissionPlanner.GCSViews.ConfigurationView
{
    public partial class ConfigAC_Fence : MyUserControl, IActivate
    {
        private readonly ToolTip fenceTooltip = new ToolTip();
        private bool showFenceActionDetails;

        public ConfigAC_Fence()
        {
            InitializeComponent();
            mavlinkComboBox2.DropDown += (sender, args) => UpdateFenceActionDropDownWidth();
            mavlinkComboBox2.MouseEnter += (sender, args) => UpdateFenceActionTooltip();
            mavlinkComboBox2.SelectedIndexChanged += (sender, args) => UpdateFenceActionTooltip();
            Disposed += (sender, args) => fenceTooltip.Dispose();

            label6maxalt.Text += "[" + CurrentState.DistanceUnit + "]";
            label8minalt.Text += "[" + CurrentState.DistanceUnit + "]";
            label7maxrad.Text += "[" + CurrentState.DistanceUnit + "]";
            label2rtlalt.Text += "[" + CurrentState.DistanceUnit + "]";
        }

        public void Activate()
        {
            mavlinkCheckBox1.setup(1, 0, "FENCE_ENABLE", MainV2.comPort.MAV.param, null, () => { if (mavlinkCheckBox1.Checked) MainV2.comPort.getParamList(); });

            mavlinkComboBox1.setup(
                ParameterMetaDataRepository.GetParameterOptionsInt("FENCE_TYPE",
                    MainV2.comPort.MAV.cs.firmware.ToString()), "FENCE_TYPE", MainV2.comPort.MAV.param);


            var firmware = MainV2.comPort.MAV.cs.firmware;
            UpdateFenceTypeReadback(MainV2.comPort.MAV.param, firmware);
            var actionOptions = ParameterMetaDataRepository.GetParameterOptionsInt("FENCE_ACTION", firmware.ToString());
            BindFenceAction(actionOptions, firmware, MainV2.comPort.MAV.param);


            // 3
            mavlinkNumericUpDown1.setup(10, 1000, (float)CurrentState.fromDistDisplayUnit(1), 1, "FENCE_ALT_MAX",
                MainV2.comPort.MAV.param);

            mavlinkNumericUpDown4.setup(-100, 100, (float)CurrentState.fromDistDisplayUnit(1), 1, "FENCE_ALT_MIN",
                MainV2.comPort.MAV.param);

            mavlinkNumericUpDown2.setup(30, 65536, (float)CurrentState.fromDistDisplayUnit(1), 1, "FENCE_RADIUS",
                MainV2.comPort.MAV.param);

            if (MainV2.comPort.MAV.param.ContainsKey("RTL_ALT_M"))
                mavlinkNumericUpDown3.setup(1, 500, (float)CurrentState.fromDistDisplayUnit(1), 1, "RTL_ALT_M",
                    MainV2.comPort.MAV.param);
            else
                mavlinkNumericUpDown3.setup(1, 500, (float)CurrentState.fromDistDisplayUnit(100), 1, "RTL_ALT",
                    MainV2.comPort.MAV.param);
        }
        private Label fenceTypeReadback;

        private void UpdateFenceTypeReadback(MAVLink.MAVLinkParamList parameters, Firmwares firmware)
        {
            if (firmware != Firmwares.ArduCopter2 ||
                !string.Equals(CultureInfo.CurrentUICulture.Name, "zh-CN", StringComparison.Ordinal))
            {
                if (fenceTypeReadback != null)
                {
                    fenceTooltip.SetToolTip(fenceTypeReadback, null);
                    fenceTypeReadback.Visible = false;
                }
                return;
            }

            if (fenceTypeReadback == null)
            {
                fenceTypeReadback = new Label { AutoSize = true, TabStop = false };
                Controls.Add(fenceTypeReadback);
                SizeChanged += (sender, args) => LayoutFenceTypeReadback();
                tableLayoutPanel1.SizeChanged += (sender, args) => LayoutFenceTypeReadback();
                tableLayoutPanel1.LocationChanged += (sender, args) => LayoutFenceTypeReadback();
            }

            var item = parameters["FENCE_TYPE"];
            string details = null;
            string text = "围栏类型缓存：未读取";
            if (item != null)
            {
                double value = item.Value;
                bool valid = value >= 0 && value <= int.MaxValue && value == Math.Truncate(value);
                text = valid ? "围栏类型缓存：" + value.ToString("0", CultureInfo.InvariantCulture)
                    : "围栏类型缓存：数值无效";
                // The upstream TYPE metadata uses bit indices 0–3, not dropdown enum values.
                // Preserve the entire number and do not guess what extra bits mean.
                if (valid && ((int)value & ~15) != 0)
                    text += "（含未识别位）";
                if (valid && ((int)value & 15) != 0)
                {
                    details = DescribeFenceTypeBits((int)value,
                        ParameterMetaDataRepository.GetParameterBitMaskInt("FENCE_TYPE", firmware.ToString()));
                    if (details != null)
                        text += " · 悬停查看类型";
                }
            }

            text += FenceTypeListStatus(item != null);

            fenceTypeReadback.Text = text;
            fenceTooltip.SetToolTip(fenceTypeReadback, details);
            fenceTypeReadback.Visible = true;
            LayoutFenceTypeReadback();
        }

        internal static string DescribeFenceTypeBits(int value, IEnumerable<KeyValuePair<int, string>> metadata)
        {
            if (value <= 0 || metadata == null)
                return null;

            var labels = new List<string>();
            foreach (var match in metadata.Where(option => option.Key >= 0 && option.Key <= 3)
                         .GroupBy(option => option.Key).OrderBy(group => group.Key))
            {
                if ((value & (1 << match.Key)) == 0 || match.Count() != 1)
                    continue;

                // Verify index and text against Copter's AC_Fence TYPE plus the packaged short names.
                string source = match.First().Value;
                string zh = null;
                switch (match.Key)
                {
                    case 0: if (source == "Max altitude") zh = "最高高度"; break;
                    case 1:
                        if (source == "Circle Centered on Home") zh = "以起始点为中心的圆形围栏";
                        else if (source == "Circle") zh = "圆形围栏";
                        break;
                    case 2:
                        if (source == "Inclusion/Exclusion Circles+Polygons") zh = "允许区／禁入区的圆形及多边形围栏";
                        else if (source == "Polygon") zh = "多边形围栏";
                        break;
                    case 3: if (source == "Min altitude") zh = "最低高度"; break;
                }
                if (zh != null)
                    labels.Add(source + " · " + zh);
            }
            return labels.Count == 0 ? null : string.Join(Environment.NewLine, labels) +
                Environment.NewLine + "仅说明缓存中的已识别类型，不代表围栏正在生效。";
        }

        private void LayoutFenceTypeReadback()
        {
            int inset = Math.Max(1, label4type.Left);
            var table = tableLayoutPanel1;
            int x = table.Left + inset;
            fenceTypeReadback.Location = new System.Drawing.Point(x, table.Bottom + inset);
            fenceTypeReadback.MaximumSize = new System.Drawing.Size(
                Math.Max(1, Math.Min(table.Right, ClientSize.Width) - x - inset), 0);
        }


        private string FenceTypeListStatus(bool hasCachedValue)
        {
            // Read the bound list only; never give the editor a synthetic selectable value.
            var boundOptions = mavlinkComboBox1.DataSource as List<KeyValuePair<int, string>>;
            var key = boundOptions != null && boundOptions.Count == 0 ? "Fence type list has no options"
                : hasCachedValue && mavlinkComboBox1.SelectedIndex < 0 ? "Fence type list has no matching option"
                : null;
            if (key == null)
                return "";
            var caption = UiText.Translate(key);
            return string.IsNullOrEmpty(caption) || string.Equals(caption, key, StringComparison.Ordinal)
                ? "" : " · " + caption;
        }

        private void BindFenceAction(List<KeyValuePair<int, string>> options, Firmwares firmware,
            MAVLink.MAVLinkParamList parameters)
        {
            var presented = PresentFenceActions(options, firmware);
            showFenceActionDetails = presented.Where((option, index) => option.Value != options[index].Value).Any();
            mavlinkComboBox2.setup(presented, "FENCE_ACTION", parameters);
            UpdateFenceActionTooltip();
        }

        private void UpdateFenceActionTooltip()
        {
            fenceTooltip.SetToolTip(mavlinkComboBox2, showFenceActionDetails ? mavlinkComboBox2.Text : null);
        }

        private void UpdateFenceActionDropDownWidth()
        {
            var combo = mavlinkComboBox2;
            if (!showFenceActionDetails)
            {
                combo.DropDownWidth = combo.Width;
                return;
            }

            int width = combo.Items.Cast<KeyValuePair<int, string>>()
                .Select(option => TextRenderer.MeasureText(option.Value, combo.Font).Width)
                .DefaultIfEmpty(combo.Width).Max() + SystemInformation.VerticalScrollBarWidth;
            combo.DropDownWidth = Math.Max(combo.Width,
                Math.Min(Screen.FromControl(combo).WorkingArea.Width, width));
        }

        internal static List<KeyValuePair<int, string>> PresentFenceActions(
            List<KeyValuePair<int, string>> options, Firmwares firmware)
        {
            if (firmware != Firmwares.ArduCopter2 ||
                !string.Equals(CultureInfo.CurrentUICulture.Name, "zh-CN", StringComparison.Ordinal))
                return options;

            return options.Select(option =>
            {
                string zh = null;
                switch (option.Key)
                {
                    case 0: if (option.Value == "Report Only") zh = "仅报告"; break;
                    case 1: if (option.Value == "RTL or Land") zh = "返航或降落"; break;
                    case 2: if (option.Value == "Always Land") zh = "始终降落"; break;
                    case 3: if (option.Value == "SmartRTL or RTL or Land") zh = "智能返航、返航或降落"; break;
                    case 4: if (option.Value == "Brake or Land") zh = "刹停或降落"; break;
                    case 5: if (option.Value == "SmartRTL or Land") zh = "智能返航或降落"; break;
                }
                return zh == null ? option : new KeyValuePair<int, string>(option.Key, option.Value + " · " + zh);
            }).ToList();
        }

    }
}