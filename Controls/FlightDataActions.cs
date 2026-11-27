using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MissionPlanner.Utilities;

namespace MissionPlanner.Controls
{
    public partial class FlightDataActions : UserControl
    {
        private const int MinGroupBoxWidth = 350;
        private const int GroupBoxHeight = 154;

        public FlightDataActions()
        {
            InitializeComponent();
            ConfigureCommandDisplay(cmbAction);
            ConfigureCommandDisplay(cmbModes);
            CreateAuxRows();
            CreateServoRows();
            scrollPanel.Resize += ScrollPanel_Resize;
        }

        public static string DisplayCommandLabel(string raw)
        {
            var translated = UiText.Translate(raw);
            return translated == raw ? raw : translated + " (" + raw + ")";
        }

        // Paint bilingual labels without changing Text/SelectedValue used by command handlers.
        private static void ConfigureCommandDisplay(ComboBox combo)
        {
            combo.DrawMode = DrawMode.OwnerDrawFixed;
            combo.ItemHeight = Math.Max(combo.ItemHeight, combo.Font.Height + 6);
            combo.DrawItem += (sender, e) =>
            {
                e.DrawBackground();
                if (e.Index >= 0 && e.Index < combo.Items.Count)
                {
                    var raw = combo.GetItemText(combo.Items[e.Index]);
                    var label = DisplayCommandLabel(raw);
                    TextRenderer.DrawText(e.Graphics, label, combo.Font, e.Bounds, e.ForeColor,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                }
                e.DrawFocusRectangle();
            };
            combo.DropDown += (sender, e) =>
            {
                var width = combo.Width;
                foreach (var item in combo.Items)
                {
                    var raw = combo.GetItemText(item);
                    var label = DisplayCommandLabel(raw);
                    width = Math.Max(width, TextRenderer.MeasureText(label, combo.Font).Width + 30);
                }
                combo.DropDownWidth = Math.Min(width, Screen.FromControl(combo).WorkingArea.Width);
            };
        }

        private void ScrollPanel_Resize(object sender, EventArgs e)
        {
            UpdateGroupBoxWidths();
        }

        private void UpdateGroupBoxWidths()
        {
            if (scrollPanel == null) return;

            // Account for vertical scrollbar if visible
            int scrollBarWidth = scrollPanel.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0;
            int availableWidth = scrollPanel.ClientSize.Width - scrollBarWidth - mainFlowLayout.Padding.Horizontal;

            // FlowLayoutPanel default margin is 3 on each side = 6 total per control
            int marginPerBox = 6;

            // Calculate how many boxes fit per row at minimum width
            int boxesPerRow = Math.Max(1, availableWidth / (MinGroupBoxWidth + marginPerBox));
            if (boxesPerRow > 4) boxesPerRow = 4;

            // Calculate the width for each box to fill the row
            int boxWidth = (availableWidth / boxesPerRow) - marginPerBox;
            boxWidth = Math.Max(boxWidth, MinGroupBoxWidth);

            grpCommand.Width = boxWidth;
            grpCommand.Height = GroupBoxHeight;
            grpSetpoints.Width = boxWidth;
            grpSetpoints.Height = GroupBoxHeight;
            grpTools.Width = boxWidth;
            grpTools.Height = GroupBoxHeight;
            grpAux.Width = boxWidth;
            grpAux.Height = GroupBoxHeight;
            grpPayload.Width = boxWidth;
            grpPayload.Height = GroupBoxHeight;

            // Servos and Relays share the width of the row of boxes above them, or take a row each when only one
            // box fits, as tall as their rows wrapped to that width
            int rowWidth = boxesPerRow * (boxWidth + marginPerBox) - marginPerBox;
            bool sideBySide = boxesPerRow > 1;
            int width = sideBySide ? (rowWidth - marginPerBox) / 2 : rowWidth;
            int servosHeight = FitToRows(grpServos, flowServos, width);
            int relaysHeight = FitToRows(grpRelays, flowRelays, width);
            if (sideBySide)
                grpServos.Height = grpRelays.Height = Math.Max(servosHeight, relaysHeight);
        }

        // Sizes the box to the width and to the height of its rows wrapped to it, and returns that height
        private static int FitToRows(GroupBox box, FlowLayoutPanel rows, int width)
        {
            box.Width = width;
            var inner = box.DisplayRectangle;
            box.Height += rows.GetPreferredSize(new Size(inner.Width, 0)).Height - inner.Height;
            return box.Height;
        }

        #region Aux functions

        private static readonly Dictionary<(int, string), string> AuxiliaryOptionLabels =
            new Dictionary<(int, string), string>
            {
                [(0, "Do Nothing")] = "无操作",
                [(3, "Simple Mode")] = "简单模式",
                [(4, "ModeRTL")] = "返航模式",
                [(4, "RTL")] = "返航",
                [(5, "Save Trim")] = "保存微调",
                [(5, "Save Trim (4.1 and lower)")] = "保存微调（4.1 及更早）",
                [(7, "Save WP")] = "保存航点",
                [(9, "Camera Trigger")] = "相机触发",
                [(10, "RangeFinder")] = "测距仪",
                [(11, "Fence")] = "地理围栏",
                [(13, "Super Simple Mode")] = "超简单模式",
                [(15, "Sprayer")] = "喷洒器",
                [(16, "Auto")] = "自动模式",
                [(16, "ModeAuto")] = "自动模式",
                [(17, "AutoTune")] = "自动调参",
                [(18, "Land")] = "降落",
                [(19, "Gripper")] = "抓取器",
                [(27, "Retract Mount")] = "收起云台",
                [(28, "Relay On/Off")] = "继电器开／关",
                [(29, "Landing Gear")] = "起落架",
                [(34, "Relay2 On/Off")] = "继电器 2 开／关",
                [(35, "Relay3 On/Off")] = "继电器 3 开／关",
                [(36, "Relay4 On/Off")] = "继电器 4 开／关",
                [(42, "SmartRTL")] = "智能返航",
                [(47, "User Function 1")] = "用户功能 1",
                [(48, "User Function 2")] = "用户功能 2",
                [(49, "User Function 3")] = "用户功能 3",
                [(51, "Manual")] = "手动模式",
                [(51, "ModeManual")] = "手动模式",
                [(53, "Steering")] = "转向模式",
                [(54, "Hold")] = "保持模式",
                [(55, "Guided")] = "引导模式",
                [(55, "ModeGuided")] = "引导模式",
                [(57, "Follow")] = "跟随模式",
                [(58, "Clear Waypoints")] = "清除航点",
                [(59, "Simple Mode")] = "简单模式",
                [(62, "Compass Learn")] = "罗盘学习",
                [(66, "Relay5 On/Off")] = "继电器 5 开／关",
                [(67, "Relay6 On/Off")] = "继电器 6 开／关",
                [(68, "Stabilize")] = "自稳模式",
                [(69, "PosHold")] = "定点模式",
                [(70, "AltHold")] = "定高模式",
                [(72, "ModeCircle")] = "盘旋模式",
                [(77, "ModeTakeoff")] = "起飞模式",
                [(90, "EKF Pos Source")] = "EKF 位置来源",
                [(94, "VTX Power")] = "图传发射功率",
                [(102, "Camera Mode Toggle")] = "切换相机模式",
                [(108, "ModeQRTL")] = "垂直返航模式",
            };

        // Camera captions verified against the ArduPilot RC input-option metadata.
        private static readonly Dictionary<(int, string), string> AuxiliaryCameraOptionLabels =
            new Dictionary<(int, string), string>
            {
                [(78, "RunCam Control")] = "RunCam 控制",
                [(79, "RunCam OSD Control")] = "RunCam 屏显控制",
                [(166, "Camera Record Video")] = "相机录像",
                [(167, "Camera Zoom")] = "相机变焦",
                [(168, "Camera Manual Focus")] = "相机手动对焦",
                [(169, "Camera Auto Focus")] = "相机自动对焦",
            };

        internal static string FormatAuxiliaryOption(string firmware, int id, string raw, bool enhancedChinese)
        {
            if (!enhancedChinese || string.IsNullOrWhiteSpace(raw))
                return raw;

            string translation = null;
            // Verify both metadata id and original label: unknown/new aliases remain unchanged.
            // ArduPilot RC_Channel.cpp at 7250f9d4: option 52 is ACRO Mode for Copter, Rover, and Plane.
            if (id == 52)
            {
                if (firmware == "ArduPlane" && (raw == "ModeACRO" || raw == "ACRO Mode")) translation = "特技模式";
                else if (firmware == "ArduCopter2" && (raw == "Acro" || raw == "ACRO Mode"))
                    translation = "特技模式";
                else if (firmware == "ArduRover" && (raw == "Acro" || raw == "ACRO Mode"))
                    translation = "转向率与速度控制";
            }

            if (translation == null &&
                (firmware == "ArduCopter2" || firmware == "ArduRover" || firmware == "ArduPlane"))
                AuxiliaryCameraOptionLabels.TryGetValue((id, raw), out translation);

            // The pinned ArduPilot OPTION metadata declares 300–315 as Scripting1–Scripting16.
            // Keep other firmware, alternate names, and nearby options on the existing fallback path.
            if (translation == null && id >= 300 && id <= 315 &&
                (firmware == "ArduCopter2" || firmware == "ArduRover" || firmware == "ArduPlane"))
            {
                var scriptNumber = (id - 299).ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (raw == "Scripting" + scriptNumber)
                    translation = "脚本功能 " + scriptNumber;
            }

            if (translation == null)
                AuxiliaryOptionLabels.TryGetValue((id, raw), out translation);

            // Option 158 uses this exact label only for Copter and Plane in RC_Channel.cpp at 7250f9d4.
            if (translation == null && id == 158 && raw == "Optflow Calibration" &&
                (firmware == "ArduCopter2" || firmware == "ArduPlane"))
            {
                var caption = UiText.Translate(raw);
                if (!string.Equals(caption, raw, StringComparison.Ordinal))
                    translation = caption;
            }

            // RC_Channel.cpp at 7250f9d4 lists option 30 separately for each firmware family.
            if (translation == null && id == 30 &&
                ((firmware == "ArduCopter2" && raw == "Lost Copter Sound") ||
                 (firmware == "ArduRover" && raw == "Lost Rover Sound") ||
                 (firmware == "ArduPlane" && raw == "Lost Plane Sound")))
            {
                var caption = UiText.Translate(raw);
                if (!string.IsNullOrEmpty(caption) && !string.Equals(caption, raw, StringComparison.Ordinal))
                    translation = caption;
            }

            // Copter and Plane 4.6.3 RC6_OPTION metadata, and RC_Channel.cpp at 7250f9d4.
            if (translation == null && id == 174 && raw == "Camera Image Tracking" &&
                (firmware == "ArduCopter2" || firmware == "ArduPlane"))
            {
                var caption = UiText.Translate(raw);
                if (!string.IsNullOrEmpty(caption) && !string.Equals(caption, raw, StringComparison.Ordinal))
                    translation = caption;
            }

            // Copter and Plane 4.6.3 RC6_OPTION metadata, and RC_Channel.cpp at 7250f9d4.
            if (translation == null && id == 175 && raw == "Camera Lens" &&
                (firmware == "ArduCopter2" || firmware == "ArduPlane"))
            {
                var caption = UiText.Translate(raw);
                if (!string.IsNullOrEmpty(caption) && !string.Equals(caption, raw, StringComparison.Ordinal))
                    translation = caption;
            }

            return translation == null ? raw : raw + " · " + translation;
        }

        private string DisplayAuxiliaryOption(ComboBox combo, int index)
        {
            if (index < 0 || index >= combo.Items.Count) return "";
            var item = combo.Items[index];
            var raw = combo.GetItemText(item);
            return item is KeyValuePair<int, string> option
                ? FormatAuxiliaryOption(_auxFunctionsFirmware, option.Key, raw, true)
                : raw;
        }

        private void ConfigureAuxiliaryDisplay(ComboBox combo)
        {
            if (!string.Equals(System.Globalization.CultureInfo.CurrentUICulture.Name,
                "zh-CN", StringComparison.Ordinal))
                return;

            const TextFormatFlags flags = TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
            var tip = new ToolTip();
            combo.Disposed += (sender, args) => tip.Dispose();
            combo.DrawMode = DrawMode.OwnerDrawFixed;
            combo.ItemHeight = Math.Max(combo.ItemHeight, combo.Font.Height + 6);
            combo.DrawItem += (sender, args) =>
            {
                args.DrawBackground();
                if (args.Index >= 0 && args.Index < combo.Items.Count)
                    TextRenderer.DrawText(args.Graphics, DisplayAuxiliaryOption(combo, args.Index), combo.Font,
                        args.Bounds, args.ForeColor, flags | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                args.DrawFocusRectangle();
            };
            Action refreshSelectedTip = () =>
            {
                if (combo.Disposing || combo.IsDisposed) return;
                var label = DisplayAuxiliaryOption(combo, combo.SelectedIndex);
                // Owner drawing does not expose its caption as the native ComboBox value.
                if (combo.AccessibleDescription != label)
                    combo.AccessibleDescription = label;
                if (tip.GetToolTip(combo) == label) return;
                tip.Hide(combo);
                tip.SetToolTip(combo, label);
            };
            combo.MouseEnter += (sender, args) => refreshSelectedTip();
            combo.SelectedIndexChanged += (sender, args) => refreshSelectedTip();
            combo.DataSourceChanged += (sender, args) => refreshSelectedTip();
            combo.DropDownClosed += (sender, args) => refreshSelectedTip();
            combo.DropDown += (sender, args) =>
            {
                int width = combo.Width;
                var area = Screen.FromControl(combo).WorkingArea;
                int padding = Math.Max(24, SystemInformation.VerticalScrollBarWidth + 10);
                using (var graphics = combo.CreateGraphics())
                    for (int i = 0; i < combo.Items.Count; i++)
                        width = Math.Max(width, TextRenderer.MeasureText(graphics,
                            DisplayAuxiliaryOption(combo, i), combo.Font, new Size(int.MaxValue, int.MaxValue), flags).Width + padding);
                combo.DropDownWidth = Math.Max(1, Math.Min(width, area.Width));
            };
        }

        // DO_AUX_FUNCTION switch positions
        private static readonly (string Text, int Position)[] AuxPositions = { ("Low", 0), ("Mid", 1), ("High", 2) };

        private readonly List<(ComboBox Function, MyButton[] Buttons)> _auxRows =
            new List<(ComboBox Function, MyButton[] Buttons)>();

        // the vehicle type the function lists were made for: the functions differ between vehicle types
        private string _auxFunctionsFirmware;

        /// <summary>
        /// Rows that each run an aux function (any RCx_OPTION) on the vehicle at a switch position, as an RC switch
        /// assigned to it would. The function a row runs is kept in the settings (AuxN_desc).
        /// </summary>
        internal static string AuxiliaryFunctionAccessibleName(string groupCaption, int rowIndex)
            => groupCaption + " " + (rowIndex + 1).ToString(System.Globalization.CultureInfo.CurrentUICulture);

        private void CreateAuxRows()
        {
            tableAux.SuspendLayout();
            for (int row = 0; row < tableAux.RowCount; row++)
            {
                // sized and styled from the Control box, which the designer scaled
                var function = new ComboBox
                {
                    AccessibleName = AuxiliaryFunctionAccessibleName(grpAux.Text, row),
                    Anchor = cmbAction.Anchor,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    DropDownWidth = 250,
                    Font = cmbAction.Font,
                    FormattingEnabled = true,
                    ItemHeight = cmbAction.ItemHeight,
                    Margin = cmbAction.Margin,
                };
                // a vehicle of another type connected since the lists were made
                function.DropDown += (sender, e) => UpdateAuxFunctions();
                ConfigureAuxiliaryDisplay(function);
                tableAux.Controls.Add(function, 0, row);

                var buttons = new MyButton[AuxPositions.Length];
                for (int i = 0; i < AuxPositions.Length; i++)
                {
                    int index = row, position = AuxPositions[i].Position;
                    buttons[i] = new MyButton
                    {
                        Anchor = btnDoAction.Anchor,
                        Enabled = false,
                        Height = btnDoAction.Height,
                        Margin = btnDoAction.Margin,
                        Text = UiText.Translate(AuxPositions[i].Text),
                        TextColorNotEnabled = btnDoAction.TextColorNotEnabled,
                        UseVisualStyleBackColor = true,
                    };
                    buttons[i].Click += (sender, e) => RunAuxFunction(index, position);
                    tableAux.Controls.Add(buttons[i], i + 1, row);
                }

                _auxRows.Add((function, buttons));
            }

            tableAux.ResumeLayout(false);

            // the lists are made when the box is first shown
            grpAux.VisibleChanged += (sender, e) =>
            {
                if (grpAux.Visible)
                    UpdateAuxFunctions();
            };
        }

        /// <summary>
        /// Fills the function lists for the vehicle type, the first time and again when the type changes.
        /// </summary>
        private void UpdateAuxFunctions()
        {
            string firmware;
            try
            {
                firmware = MainV2.comPort.MAV.cs.firmware.ToString();
            }
            catch
            {
                return;
            }

            if (firmware == _auxFunctionsFirmware)
                return;
            _auxFunctionsFirmware = firmware;

            for (int row = 0; row < _auxRows.Count; row++)
            {
                var function = _auxRows[row].Function;
                function.SelectedValueChanged -= AuxFunction_SelectedValueChanged;

                // each row its own list: rows bound to the same one would share a selection
                function.DisplayMember = "Value";
                function.ValueMember = "Key";
                function.DataSource = ParameterMetaDataRepository.GetParameterOptionsInt("RC6_OPTION", firmware);
                if (int.TryParse(Settings.Instance["Aux" + row + "_desc"], out var saved))
                    function.SelectedValue = saved;

                function.SelectedValueChanged += AuxFunction_SelectedValueChanged;
                UpdateAuxButtons(row);
            }
        }

        private void AuxFunction_SelectedValueChanged(object sender, EventArgs e)
        {
            int row = _auxRows.FindIndex(r => r.Function == sender);
            if (row < 0 || !(_auxRows[row].Function.SelectedValue is int function))
                return;

            Settings.Instance["Aux" + row + "_desc"] = function.ToString();
            UpdateAuxButtons(row);
        }

        // a row on Do Nothing (0) has nothing to run
        private void UpdateAuxButtons(int row)
        {
            bool set = _auxRows[row].Function.SelectedValue is int function && function != 0;
            foreach (var button in _auxRows[row].Buttons)
                button.Enabled = set;
        }

        private void RunAuxFunction(int row, int position)
        {
            if (!(_auxRows[row].Function.SelectedValue is int function))
                return;

            try
            {
                if (!MainV2.comPort.doCommand(MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent,
                        MAVLink.MAV_CMD.DO_AUX_FUNCTION, function, position, 0, 0, 0, 0, 0))
                    CustomMessageBox.Show(Strings.CommandFailed, Strings.ERROR);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show(Strings.CommandFailed + ex, Strings.ERROR);
            }
        }

        #endregion

        /// <summary>
        /// The servo and relay rows. Each numbers itself in the order made, servos 5 to 16 and relays 1 to 16, which
        /// their saved names and PWM values are kept under.
        /// </summary>
        private void CreateServoRows()
        {
            flowServos.SuspendLayout();
            for (int i = 0; i < 12; i++)
                flowServos.Controls.Add(new ServoOptions());
            flowServos.ResumeLayout(false);

            flowRelays.SuspendLayout();
            for (int i = 0; i < 16; i++)
            {
                // narrowed to its buttons and name, so more fit on a line
                var relay = new RelayOptions();
                relay.Width = relay.Controls.Cast<Control>().Max(c => c.Right) + relay.Margin.Right;
                flowRelays.Controls.Add(relay);
            }
            flowRelays.ResumeLayout(false);

            // Servos and Relays start a row of their own (the Payload box between is never shown)
            mainFlowLayout.SetFlowBreak(grpAux, true);
        }

        public void Initialize()
        {
            ThemeManager.ApplyThemeTo(this);
            UpdateGroupBoxWidths();
        }

        #region Exposed Controls - for FlightData.cs to wire up existing event handlers

        // Command group
        public ComboBox CMB_action => cmbAction;
        public MyButton BUTactiondo => btnDoAction;
        public ComboBox CMB_setwp => cmbWaypoint;
        public MyButton BUT_setwp => btnSetWP;
        public ComboBox CMB_modes => cmbModes;
        public MyButton BUT_setmode => btnSetMode;
        public ComboBox CMB_mountmode => cmbMount;
        public MyButton BUT_mountmode => btnSetMount;
        public MyButton BUTrestartmission => btnRestartMission;
        public MyButton BUT_resumemis => btnResumeMission;

        // Setpoints group
        public ModifyandSet modifyandSetSpeed => modSetSpeed;
        public ModifyandSet modifyandSetAlt => modSetAlt;
        public ModifyandSet modifyandSetLoiterRad => modSetLoiterRad;
        public MyButton BUT_Homealt => btnHomeAlt;

        // Tools group
        public MyButton BUT_RAWSensor => btnRawSensor;
        public MyButton BUT_joystick => btnJoystick;
        public MyButton BUT_SendMSG => btnMessage;
        public MyButton BUT_clear_track => btnClearTrack;
        public MyButton BUT_Reboot => btnReboot;
        public MyButton BUT_abortland => btnAbortLanding;

        // Payload group
        public TrackBar TrackBarPitch => trackBarPitch;
        public TrackBar TrackBarRoll => trackBarRoll;
        public TrackBar TrackBarYaw => trackBarYaw;
        public TextBox TXT_gimbalPitchPos => txtPitchPos;
        public TextBox TXT_gimbalRollPos => txtRollPos;
        public TextBox TXT_gimbalYawPos => txtYawPos;
        public MyButton BUT_resetGimbalPos => btnResetGimbal;
        public MyButton BUT_GimbalVideo => btnGimbalVideo;

        #endregion
    }
}
