using DirectShowLib;
using MissionPlanner.Controls;
using MissionPlanner.Joystick;
using MissionPlanner.Maps;
using MissionPlanner.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebCamService;

namespace MissionPlanner.GCSViews.ConfigurationView
{
    public partial class ConfigPlanner : MyUserControl, IActivate
    {
        private List<CultureInfo> _languages;
        private bool startup;
        static temp temp;
        private GroupColumnsPanel layoutRoot;
        private readonly CheckBox CHK_startFullscreen;

        public ConfigPlanner()
        {
            startup = true;

            CHK_startFullscreen = new CheckBox
            {
                Name = "CHK_startFullscreen",
                AutoSize = true,
                Text = UiText.Translate("Always start fullscreen")
            };
            CHK_startFullscreen.CheckedChanged += CHK_startFullscreen_CheckedChanged;

            InitializeComponent();
            CMB_distunits.Format += FormatUnit;
            CMB_speedunits.Format += FormatUnit;
            CMB_altunits.Format += FormatUnit;
            CMB_theme.Format += (sender, e) =>
            {
                if (e.ListItem is string filename)
                    e.Value = FormatThemeCaption(filename);
            };
            CMB_mapCache.Format += (sender, e) =>
            {
                if (e.ListItem is string key)
                    e.Value = UiText.Translate(key);
            };
            // Format display text only; SelectedItem remains the original enum.
            CMB_Layout.Format += (sender, e) => e.Value = UiText.Translate(e.ListItem.ToString());
            CMB_severity.Format += (sender, e) => e.Value = UiText.Translate(e.ListItem.ToString());
            CMB_language.Format += (sender, e) =>
            {
                var culture = e.ListItem as CultureInfo;
                if (culture != null)
                    e.Value = culture.Name == "zh-CN" ? "中文(简体)2"
                        : culture.Name == "zh-Hans" ? "中文(简体)"
                        : culture.NativeName + " [" + culture.Name + "]";
            };
            CMB_language.DropDownWidth = 300;
            CMB_Layout.Items.Add(DisplayNames.Basic);
            CMB_Layout.Items.Add(DisplayNames.Advanced);
            CMB_Layout.Items.Add(DisplayNames.Custom);

            txt_log_dir.TextChanged += OnLogDirTextChanged;

            CMB_severity.Items.Add(SeverityLevel.Emergency);
            CMB_severity.Items.Add(SeverityLevel.Alert);
            CMB_severity.Items.Add(SeverityLevel.Critical);
            CMB_severity.Items.Add(SeverityLevel.Error);
            CMB_severity.Items.Add(SeverityLevel.Warning);
            CMB_severity.Items.Add(SeverityLevel.Notice);
            CMB_severity.Items.Add(SeverityLevel.Info);
            CMB_severity.Items.Add(SeverityLevel.Debug);

            if (string.Equals(CultureInfo.CurrentUICulture.Name, "zh-CN", StringComparison.Ordinal))
                cmb_secondarydisplaystyle.Format += (sender, e) =>
                {
                    switch (e.ListItem as string)
                    {
                        case "Normal": e.Value = "\u6b63\u5e38\u663e\u793a"; break;
                        case "Transparent": e.Value = "\u534a\u900f\u660e\u663e\u793a"; break;
                        case "Hidden": e.Value = "\u9690\u85cf"; break;
                    }
                };
            cmb_secondarydisplaystyle.DataSource = Enum.GetNames(typeof(Maps.GMapMarkerBase.InactiveDisplayStyleEnum));
            cmb_secondarydisplaystyle.SelectedItem = Maps.GMapMarkerBase.InactiveDisplayStyle.ToString();

            BuildLayout();
        }

        private void BuildLayout()
        {
            SuspendLayout();

            layoutRoot = new GroupColumnsPanel
            {
                Name = "layoutRoot",
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10),
                Margin = new Padding(0)
            };

            layoutRoot.Controls.Add(CreateConnectivityGroup());
            layoutRoot.Controls.Add(CreateLayoutAndAppearanceGroup());
            layoutRoot.Controls.Add(CreateMapAndDisplayGroup());
            layoutRoot.Controls.Add(CreateTelemetryGroup());
            layoutRoot.Controls.Add(CreateUnitsGroup());
            layoutRoot.Controls.Add(CreateSpeechAndAudioGroup());
            layoutRoot.Controls.Add(CreateUpdatesGroup());
            layoutRoot.Controls.Add(CreateConfigurationGroup());
#if !LIB
            layoutRoot.Controls.Add(CreateDesktopAppearanceGroup());
#endif
            layoutRoot.Controls.Add(CreateKeyboardShortcutsGroup());

            PrepareLayoutControls(layoutRoot);
            Controls.Clear();
            Controls.Add(layoutRoot);

            ResumeLayout(true);
        }

        /// <summary>
        /// Lays the setting groups out in equal-width columns, putting each in turn under the shortest column, so
        /// boxes of different heights pack without gaps. Groups tagged <see cref="FullWidth"/> go under all the
        /// columns, as wide as they are together.
        /// </summary>
        private class GroupColumnsPanel : Panel
        {
            private const int MinColumnWidth = 400;
            private const int Gap = 10;

            public static readonly object FullWidth = new object();

            protected override void OnLayout(LayoutEventArgs levent)
            {
                var visible = Controls.Cast<Control>().Where(c => c.Visible).ToList();
                var groups = visible.Where(c => c.Tag != FullWidth).ToList();
                var wide = visible.Where(c => c.Tag == FullWidth).ToList();
                int width = ClientSize.Width - Padding.Horizontal;
                if (visible.Count == 0 || width <= 0)
                {
                    base.OnLayout(levent);
                    return;
                }

                int minColumnWidth = (int)Math.Ceiling(MinColumnWidth * DeviceDpi / 96.0);
                int columns = Math.Max(1, Math.Min(3, Math.Min(groups.Count, (width + Gap) / (minColumnWidth + Gap))));
                int columnWidth = Math.Max(1, (width - (columns - 1) * Gap) / columns);
                int fullWidth = columns * columnWidth + (columns - 1) * Gap;

                // the groups size their height to their content; pin their width to the column's (or all of them)
                SuspendLayout();
                foreach (var group in visible)
                {
                    // Each column includes the group's outer margins, just as the
                    // previous flow layout did. Otherwise its right margin overflows
                    // the padded viewport even when the group's border fits.
                    int groupWidth = Math.Max(1, (wide.Contains(group) ? fullWidth : columnWidth) - group.Margin.Horizontal);
                    group.MinimumSize = Size.Empty;
                    group.MaximumSize = new Size(groupWidth, 0);
                    group.MinimumSize = new Size(groupWidth, 0);
                    group.Width = groupWidth;
                }
                ResumeLayout(false);

                base.OnLayout(levent);

                var bottoms = new int[columns];
                var origin = DisplayRectangle.Location;
                SuspendLayout();
                foreach (var group in groups)
                {
                    int column = Array.IndexOf(bottoms, bottoms.Min());
                    group.Location = new Point(origin.X + column * (columnWidth + Gap) + group.Margin.Left,
                        origin.Y + bottoms[column] + group.Margin.Top);
                    bottoms[column] += group.Height + group.Margin.Vertical + Gap;
                }

                int bottom = bottoms.Max();
                foreach (var group in wide)
                {
                    // Width excludes the outer margins, so position and stacking
                    // must account for them just like ordinary column groups.
                    group.Location = new Point(origin.X + group.Margin.Left, origin.Y + bottom + group.Margin.Top);
                    bottom += group.Height + group.Margin.Vertical + Gap;
                }
                ResumeLayout(false);

                // the scroll range follows the groups' new places
                AdjustFormScrollbars(AutoScroll);
            }
        }

        private static void PrepareLayoutControls(Control parent)
        {
            foreach (Control child in parent.Controls)
            {
                if (child is Label || child is CheckBox || child is RadioButton)
                    child.AutoSize = true;
                if (child is Button button)
                {
                    button.AutoSize = true;
                    button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                }
                if (child is ComboBox || child is TextBox)
                    child.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                PrepareLayoutControls(child);
            }
        }

        private GroupBox CreateGroupBox(string title)
        {
            return new GroupBox
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10),
                Text = UiText.Translate(title),
                Margin = new Padding(5)
            };
        }

        private TableLayoutPanel CreateTableLayout(int columns)
        {
            var table = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                ColumnCount = columns
            };

            if (columns > 0)
            {
                table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            }
            if (columns > 1)
            {
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            }
            if (columns > 2)
            {
                table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            }

            return table;
        }

        private FlowLayoutPanel CreateWrapFlow(FlowDirection direction = FlowDirection.LeftToRight)
        {
            return new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = direction,
                WrapContents = true,
                Dock = DockStyle.Top
            };
        }

        private Control CreateLayoutAndAppearanceGroup()
        {
            var group = CreateGroupBox($"{label5.Text} / {label4.Text}");
            var table = CreateTableLayout(3);

            table.Controls.Add(label5, 0, 0);
            table.Controls.Add(CMB_Layout, 1, 0);
            table.SetColumnSpan(CMB_Layout, 2);

            table.Controls.Add(label4, 0, 1);
            table.Controls.Add(CMB_theme, 1, 1);
            table.Controls.Add(BUT_themecustom, 2, 1);

            table.Controls.Add(label93, 0, 2);
            table.Controls.Add(CMB_language, 1, 2);
            table.SetColumnSpan(CMB_language, 2);

            table.Controls.Add(label94, 0, 3);
            table.Controls.Add(CMB_osdcolor, 1, 3);
            table.SetColumnSpan(CMB_osdcolor, 2);

            var severityLabel = new Label { AutoSize = true, Text = UiText.Translate("Severity") };
            table.Controls.Add(severityLabel, 0, 4);
            table.Controls.Add(CMB_severity, 1, 4);
            table.SetColumnSpan(CMB_severity, 2);

            table.Controls.Add(label8, 0, 5);
            table.SetColumnSpan(label8, 3);

            var optionsFlow = CreateWrapFlow();
            optionsFlow.Controls.Add(chk_analytics);
            optionsFlow.Controls.Add(CHK_beta);
            optionsFlow.Controls.Add(CHK_Password);
            optionsFlow.Controls.Add(CHK_params_bg);
            optionsFlow.Controls.Add(CHK_AutoParamCommit);
            optionsFlow.Controls.Add(chk_slowMachine);
            optionsFlow.Controls.Add(CHK_startFullscreen);
            table.Controls.Add(optionsFlow, 0, 6);
            table.SetColumnSpan(optionsFlow, 3);

            group.Controls.Add(table);
            return group;
        }

#if !LIB
        private Control CreateDesktopAppearanceGroup()
        {
            var group = CreateGroupBox("Window icon and startup image");
            group.Name = "desktopAppearanceGroup";
            group.Tag = GroupColumnsPanel.FullWidth;
            group.Controls.Add(new ConfigAppearance(embedded: true));
            return group;
        }
#endif

        private Control CreateConfigurationGroup()
        {
            var group = CreateGroupBox("Configuration");
            var table = CreateTableLayout(3);

            table.Controls.Add(label_configprofile, 0, 0);
            table.Controls.Add(CMB_configprofile, 1, 0);
            table.SetColumnSpan(CMB_configprofile, 2);

            var saveFlow = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };
            saveFlow.Controls.Add(TXT_newconfigname);
            saveFlow.Controls.Add(BUT_saveconfig);
            table.Controls.Add(saveFlow, 1, 1);
            table.SetColumnSpan(saveFlow, 2);

            group.Controls.Add(table);
            return group;
        }

        /// <summary>
        /// The keyboard shortcuts, by where they work, read from their [KeyboardShortcut] declarations: a shortcut
        /// added or changed in the code shows here as it is.
        /// </summary>
        private static readonly Dictionary<string, string> ShortcutHelpChinese = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Keyboard Shortcuts"] = "键盘快捷键",
            ["Everywhere"] = "全局",
            ["FLY screen"] = "飞行数据页",
            ["PLAN screen"] = "航线规划页",
            ["Parameter pages"] = "参数页",
            ["SIMULATION screen"] = "仿真页",
            ["Install Firmware"] = "固件安装页",
            ["Survey (Grid)"] = "测绘（网格）",
            ["Log Browser"] = "日志浏览器",
            ["Open the FLY screen"] = "打开飞行数据页",
            ["Open the PLAN screen"] = "打开航线规划页",
            ["Open the CONFIG screen"] = "打开配置／调试页",
            ["Download the parameters again and reload the screen"] = "重新读取参数并刷新页面",
            ["Connect or disconnect"] = "连接或断开",
            ["Open the Tools window"] = "打开工具窗口",
            ["Send the vehicle's position out as NMEA"] = "打开 NMEA 输出设置窗口",
            ["Open the DevOps tools"] = "打开开发运维工具",
            ["Open the spectrogram"] = "打开频谱图",
            ["Open the plugin manager"] = "打开插件管理器",
            ["Reboot the autopilot"] = "重启飞控",
            ["Open the connection without downloading the parameters"] = "连接但不下载参数",
            ["Open the propagation settings"] = "打开地图叠加层设置",
            ["Open the map cache"] = "打开地图缓存",
            ["Have the autopilot save its parameters to storage"] = "让飞控将参数写入存储",
            ["Ask the camera for its information and start its video stream"] = "读取相机信息并启动视频流",
            ["Play or pause the log being played back"] = "播放或暂停当前日志",
            ["Speed the log playback up"] = "加快日志回放",
            ["Slow the log playback down"] = "减慢日志回放",
            ["Open the tab at that place along the tabs (0 for the tenth)"] = "打开对应序号的标签页（0 对应第十项）",
            ["Swap the 3D map between FPV and the chase view"] = "在第一人称与跟随视角之间切换 3D 地图",
            ["Show or hide the HUD over the video (the Video tab's HUD Overlay)"] = "显示或隐藏视频上的抬头显示（视频页的 HUD 叠加）",
            ["Take the plan as the vehicle's mission here, without writing it"] = "在本地将规划作为任务，不向飞行器写入",
            ["Switch the maps' GDI drawing on or off"] = "切换地图 GDI 绘制",
            ["Load a mission file"] = "打开任务文件",
            ["Save the mission to a file"] = "将任务保存为文件",
            ["Undo the last change to the plan"] = "撤销上一次规划更改",
            ["Write the mission to the vehicle, fast"] = "快速写入任务到飞行器",
            ["Read the mission from the vehicle"] = "从飞行器读取任务",
            ["Write the mission to the vehicle"] = "将任务写入飞行器",
            ["Write the changed parameters"] = "写入更改过的参数",
            ["Start a swarm of separate simulated copters"] = "启动相互独立的多旋翼编队仿真",
            ["Start a swarm of simulated vehicles, each following the one before"] = "启动逐一跟随前机的编队仿真",
            ["Install the PX4 firmware"] = "安装 PX4 固件",
            ["List the development (latest) firmware"] = "列出最新开发版固件",
            ["Load a survey grid"] = "打开测绘网格",
            ["Save the survey grid"] = "保存测绘网格",
            ["Go to a line"] = "跳转到指定行",
        };

        private static string ShortcutHelpText(string source, bool enhancedChinese, string separator)
        {
            if (!enhancedChinese)
                return UiText.Translate(source);
            return ShortcutHelpChinese.TryGetValue(source, out var translated) ? translated + separator + source : source;
        }

        private Control CreateKeyboardShortcutsGroup()
        {
            bool enhancedChinese = string.Equals(CultureInfo.CurrentUICulture.Name, "zh-CN", StringComparison.Ordinal);
            var group = CreateGroupBox("Keyboard Shortcuts");
            group.Text = ShortcutHelpText("Keyboard Shortcuts", enhancedChinese, " / ");
            group.Tag = GroupColumnsPanel.FullWidth;
            var table = CreateTableLayout(2);
            var descriptions = new List<Label>();
            var keysLabels = new List<Label>();

            string scope = null;
            int row = 0;
            foreach (var shortcut in KeyboardShortcuts.All)
            {
                if (shortcut.Scope != scope)
                {
                    scope = shortcut.Scope;
                    var header = new Label
                    {
                        AutoSize = true,
                        Text = ShortcutHelpText(scope, enhancedChinese, " / "),
                        Margin = new Padding(3, row == 0 ? 3 : 12, 3, 3)
                    };
                    header.Font = new Font(header.Font, FontStyle.Bold);
                    table.Controls.Add(header, 0, row);
                    table.SetColumnSpan(header, 2);
                    row++;
                }

                var keys = new Label
                {
                    AutoSize = true,
                    Text = shortcut.KeysText,
                    Margin = new Padding(15, 3, 20, 3)
                };
                var description = new Label
                {
                    AutoSize = true,
                    Text = ShortcutHelpText(shortcut.Description, enhancedChinese, Environment.NewLine)
                };
                keysLabels.Add(keys);
                descriptions.Add(description);
                table.Controls.Add(keys, 0, row);
                table.Controls.Add(description, 1, row);
                row++;
            }

            group.Controls.Add(table);
            if (enhancedChinese)
            {
                Action constrainDescriptions = () =>
                {
                    if (group.ClientSize.Width <= 0)
                        return;
                    int keysWidth = keysLabels.Count == 0 ? 0 : keysLabels.Max(label => label.PreferredWidth + label.Margin.Horizontal);
                    int width = Math.Max(1, group.ClientSize.Width - group.Padding.Horizontal - table.Margin.Horizontal - keysWidth - 6);
                    foreach (var description in descriptions)
                        if (description.MaximumSize.Width != width)
                            description.MaximumSize = new Size(width, 0);
                };
                group.SizeChanged += (sender, args) => constrainDescriptions();
                constrainDescriptions();
            }
            return group;
        }

        private Control CreateSpeechAndAudioGroup()
        {
            var group = CreateGroupBox(label95.Text);
            var table = CreateTableLayout(1);

            table.Controls.Add(CHK_enablespeech, 0, 0);

            var speechFlow = CreateWrapFlow();
            speechFlow.Controls.Add(CHK_speechArmedOnly);
            speechFlow.Controls.Add(CHK_speechwaypoint);
            speechFlow.Controls.Add(CHK_speechmode);
            speechFlow.Controls.Add(CHK_speechcustom);
            speechFlow.Controls.Add(CHK_speechbattery);
            speechFlow.Controls.Add(CHK_speechaltwarning);
            speechFlow.Controls.Add(CHK_speecharmdisarm);
            speechFlow.Controls.Add(CHK_speechlowspeed);
            speechFlow.Controls.Add(CHK_speechadsb);
            table.Controls.Add(speechFlow, 0, 1);

            var audioFlow = CreateWrapFlow();
            audioFlow.Controls.Add(BUT_Vario);
            table.Controls.Add(audioFlow, 0, 2);

            group.Controls.Add(table);

            // In a one-column table the flow sets the column's width, so it would be measured as one row and the
            // group sized too short for the rows it wraps into; capping it at the group's width measures it wrapped.
            group.SizeChanged += (s, e) => speechFlow.MaximumSize = new Size(
                Math.Max(1, group.DisplayRectangle.Width - table.Padding.Horizontal - speechFlow.Margin.Horizontal), 0);
            return group;
        }

        private Control CreateUpdatesGroup()
        {
            var group = CreateGroupBox("Updates");
            var table = CreateTableLayout(1);

            // the running build and how many releases behind it is, filled in once GitHub answers
            var lblBuild = new Label
            {
                Text = UiText.Translate("Comparing this build with the releases..."),
                AutoSize = true,
                Margin = new Padding(3, 3, 3, 6)
            };
            table.Controls.Add(lblBuild, 0, 0);
            Task.Run(() => Utilities.Update.DescribeRunningBuild()).ContinueWith(t =>
            {
                if (!lblBuild.IsDisposed)
                    lblBuild.Text = t.Status == TaskStatus.RanToCompletion
                        ? t.Result
                        : UiText.Translate("Couldn't compare this build with the releases.");
            }, TaskScheduler.FromCurrentSynchronizationContext());

            var btnCheckUpdate = new MyButton
            {
                Text = UiText.Translate("Check for Updates"),
                AutoSize = true
            };
            btnCheckUpdate.Click += BtnCheckUpdate_Click;
            table.Controls.Add(btnCheckUpdate, 0, 1);

            var btnBetaUpdate = new MyButton
            {
                Text = UiText.Translate("Download tested build"),
                AutoSize = true
            };
            btnBetaUpdate.Click += BtnBetaUpdate_Click;
            table.Controls.Add(btnBetaUpdate, 0, 2);

            var lnkReleases = new LinkLabel
            {
                Text = UiText.Translate("Manual downloads (GitHub releases)"),
                AutoSize = true,
                Margin = new Padding(3, 6, 3, 3)
            };
            lnkReleases.LinkClicked += LnkReleases_LinkClicked;
            table.Controls.Add(lnkReleases, 0, 3);

            // wrap the text to the group's width, as the speech options do
            group.SizeChanged += (s, e) => lblBuild.MaximumSize = new Size(
                Math.Max(1, group.DisplayRectangle.Width - table.Padding.Horizontal - lblBuild.Margin.Horizontal), 0);

            var updateNotice = new Label
            {
                AutoSize = true,
                Text = UiText.Translate("Download updates from this fork to keep both Chinese language options. Extract the complete package to a new folder."),
                Margin = new Padding(3, 8, 3, 3)
            };
            table.SizeChanged += (sender, e) => updateNotice.MaximumSize =
                new Size(Math.Max(1, table.ClientSize.Width - updateNotice.Margin.Horizontal), 0);
            table.Controls.Add(updateNotice, 0, 4);

            group.Controls.Add(table);
            return group;
        }

        private void LnkReleases_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(Utilities.Update.ForkReleasePage ?? "https://github.com/reliable-ly0411/TitanPlanner-zh-CN/releases")
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show(ex.ToString(), Strings.ERROR);
            }
        }

        private void BtnCheckUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                if (Program.WindowsStoreApp)
                {
                    return;
                }
                Utilities.Update.CheckForUpdate(true);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show(ex.ToString(), Strings.ERROR);
            }
        }

        private void BtnBetaUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                Utilities.Update.dobeta = true;
                if (Control.ModifierKeys == Keys.Control)
                {
                    Utilities.Update.domaster = true;
                    CustomMessageBox.Show(UiText.Translate("This will update to MASTER release"));
                }

                Utilities.Update.DoUpdate();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show(ex.ToString(), Strings.ERROR);
            }
        }

        private Control CreateTelemetryGroup()
        {
            var group = CreateGroupBox(label101.Text);
            var table = CreateTableLayout(2);

            table.Controls.Add(label102, 0, 0);
            table.Controls.Add(CMB_rateattitude, 1, 0);

            table.Controls.Add(label103, 0, 1);
            table.Controls.Add(CMB_rateposition, 1, 1);

            table.Controls.Add(label104, 0, 2);
            table.Controls.Add(CMB_ratestatus, 1, 2);

            table.Controls.Add(label107, 0, 3);
            table.Controls.Add(CMB_raterc, 1, 3);

            table.Controls.Add(label33, 0, 4);
            table.Controls.Add(CMB_ratesensors, 1, 4);

            group.Controls.Add(table);
            return group;
        }

        private Control CreateUnitsGroup()
        {
            var group = CreateGroupBox("Units & Measurement");
            var table = CreateTableLayout(2);

            table.Controls.Add(label97, 0, 0);
            table.Controls.Add(CMB_distunits, 1, 0);

            table.Controls.Add(label98, 0, 1);
            table.Controls.Add(CMB_speedunits, 1, 1);

            table.Controls.Add(label6, 0, 2);
            table.Controls.Add(CMB_altunits, 1, 2);

            table.Controls.Add(label99, 0, 3);
            table.SetColumnSpan(label99, 2);

            group.Controls.Add(table);
            return group;
        }

        private Control CreateMapAndDisplayGroup()
        {
            var group = CreateGroupBox("Map & Display");
            var table = CreateTableLayout(3);

            table.Controls.Add(label1, 0, 0);
            table.Controls.Add(CHK_maprotation, 1, 0);
            table.SetColumnSpan(CHK_maprotation, 2);

            table.Controls.Add(label2, 0, 1);
            table.Controls.Add(CHK_disttohomeflightdata, 1, 1);
            table.SetColumnSpan(CHK_disttohomeflightdata, 2);

            table.Controls.Add(label23, 0, 2);
            table.Controls.Add(NUM_tracklength, 1, 2);
            table.SetColumnSpan(NUM_tracklength, 2);

            table.Controls.Add(label11, 0, 3);
            table.Controls.Add(num_linelength, 1, 3);
            table.SetColumnSpan(num_linelength, 2);

            table.Controls.Add(label13, 0, 4);
            table.Controls.Add(CMB_mapCache, 1, 4);
            table.Controls.Add(BUT_mapCacheDir, 2, 4);

            table.Controls.Add(label9, 0, 5);
            table.Controls.Add(cmb_secondarydisplaystyle, 1, 5);
            table.SetColumnSpan(cmb_secondarydisplaystyle, 2);

            table.Controls.Add(label10, 0, 6);
            var iconFlow = CreateWrapFlow();
            iconFlow.Controls.Add(chk_displaycog);
            iconFlow.Controls.Add(chk_displayheading);
            iconFlow.Controls.Add(chk_displaynavbearing);
            iconFlow.Controls.Add(chk_displayradius);
            iconFlow.Controls.Add(chk_displaytarget);
            iconFlow.Controls.Add(chk_displaytooltip);
            table.Controls.Add(iconFlow, 1, 6);
            table.SetColumnSpan(iconFlow, 2);

            var overlaysFlow = CreateWrapFlow();
            overlaysFlow.Controls.Add(CHK_showairports);
            overlaysFlow.Controls.Add(chk_shownofly);
            overlaysFlow.Controls.Add(chk_ADSB);
            overlaysFlow.Controls.Add(chk_tfr);
            overlaysFlow.Controls.Add(chk_temp);
            table.Controls.Add(overlaysFlow, 0, 7);
            table.SetColumnSpan(overlaysFlow, 3);

            table.Controls.Add(label_customicon, 0, 8);
            table.Controls.Add(BUT_customicon, 1, 8);

            group.Controls.Add(table);
            return group;
        }

        private Control CreateConnectivityGroup()
        {
            var group = CreateGroupBox("Connection && Data");
            var table = CreateTableLayout(3);

            table.Controls.Add(label7, 0, 0);
            table.Controls.Add(num_gcsid, 1, 0);
            table.SetColumnSpan(num_gcsid, 2);

            table.Controls.Add(label24, 0, 1);
            table.Controls.Add(CHK_loadwponconnect, 1, 1);
            table.SetColumnSpan(CHK_loadwponconnect, 2);

            table.Controls.Add(label108, 0, 2);
            table.Controls.Add(CHK_resetapmonconnect, 1, 2);
            table.SetColumnSpan(CHK_resetapmonconnect, 2);

            table.Controls.Add(CHK_rtsresetesp32, 0, 3);
            table.SetColumnSpan(CHK_rtsresetesp32, 3);

            table.Controls.Add(CHK_autoconnectusb, 0, 4);
            table.SetColumnSpan(CHK_autoconnectusb, 3);

            table.Controls.Add(CHK_mavdebug, 0, 5);
            table.SetColumnSpan(CHK_mavdebug, 3);

            table.Controls.Add(chk_norcreceiver, 0, 6);
            table.SetColumnSpan(chk_norcreceiver, 3);

            txt_log_dir.Width = 260;
            txt_log_dir.MaximumSize = new Size(500, txt_log_dir.Height);
            // stretch with the column rather than run into the Browse button when the group is narrower
            txt_log_dir.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            table.Controls.Add(label3, 0, 7);
            table.Controls.Add(txt_log_dir, 1, 7);
            table.Controls.Add(BUT_logdirbrowse, 2, 7);

            table.Controls.Add(BUT_Joystick, 0, 8);
            table.SetColumnSpan(BUT_Joystick, 3);

            group.Controls.Add(table);
            return group;
        }

        // Called every time that this control is made current in the backstage view
        public void Activate()
        {
            startup = true; // flag to ignore changes while we programatically populate controls
            if (MainV2.DisplayConfiguration.displayName == DisplayNames.Advanced)
            {
                CMB_Layout.SelectedIndex = 1;
            }
            else if (MainV2.DisplayConfiguration.displayName == DisplayNames.Basic)
            {
                CMB_Layout.SelectedIndex = 0;
            }
            else if (MainV2.DisplayConfiguration.displayName == DisplayNames.Custom)
            {
                CMB_Layout.SelectedIndex = 2;
            }
            else
            {
                CMB_Layout.SelectedIndex = 0;
            }

            if (!MainV2.DisplayConfiguration.displayPlannerLayout)
            {
                label5.Visible = false;
                CMB_Layout.Visible = false;
            }

            CMB_osdcolor.DataSource = Enum.GetNames(typeof(KnownColor));

            // set distance/speed unit states
            CMB_distunits.DataSource = Enum.GetNames(typeof(distances));
            CMB_speedunits.DataSource = Enum.GetNames(typeof(speeds));
            CMB_altunits.DataSource = Enum.GetNames(typeof(altitudes));

            CMB_theme.DataSource = ThemeManager.ThemeNames;

            CMB_theme.SelectedItem = ThemeManager.thmColor.strThemeName;

            num_gcsid.Maximum = uint.MaxValue;
            num_gcsid.Value = MAVLinkInterface.gcssysid;

            // setup severity selection
            if (Settings.Instance["severity"] != null)
            {
                CMB_severity.SelectedIndex = Settings.Instance.GetInt32("severity");
            }
            else
            {
                CMB_severity.SelectedIndex = 4;  // SeverityLevel.Warning
                Settings.Instance["severity"] = CMB_severity.SelectedIndex.ToString();
            }

            SetupLanguageSelector();

            // setup up camera button states
            if (MainV2.cam != null)
            {
                BUT_videostart.Enabled = false;
            }
            else
            {
                BUT_videostart.Enabled = true;
            }

            // Sync HUD overlay checkbox with saved setting
            CHK_hudshow.Checked = Settings.Instance.GetBoolean("CHK_hudshow", FlightData.myhud.hudon);

            // setup speech states
            SetCheckboxFromConfig("speechenable", CHK_enablespeech);
            SetCheckboxFromConfig("speechwaypointenabled", CHK_speechwaypoint);
            SetCheckboxFromConfig("speechmodeenabled", CHK_speechmode);
            SetCheckboxFromConfig("speechcustomenabled", CHK_speechcustom);
            SetCheckboxFromConfig("speechbatteryenabled", CHK_speechbattery);
            SetCheckboxFromConfig("speechaltenabled", CHK_speechaltwarning);
            SetCheckboxFromConfig("speecharmenabled", CHK_speecharmdisarm);
            SetCheckboxFromConfig("speechlowspeedenabled", CHK_speechlowspeed);
            SetCheckboxFromConfig("speechadsbenabled", CHK_speechadsb);
            SetCheckboxFromConfig("beta_updates", CHK_beta);
            SetCheckboxFromConfig("password_protect", CHK_Password);
            SetCheckboxFromConfig("showairports", CHK_showairports);
            SetCheckboxFromConfig("enableadsb", chk_ADSB);
            SetCheckboxFromConfig("norcreceiver", chk_norcreceiver);
            SetCheckboxFromConfig("showtfr", chk_tfr);
            SetCheckboxFromConfig("autoParamCommit", CHK_AutoParamCommit);
            SetCheckboxFromConfig("ShowNoFly", chk_shownofly);
            SetCheckboxFromConfig("Params_BG", CHK_params_bg);
            SetCheckboxFromConfig("SlowMachine", chk_slowMachine);
            SetCheckboxFromConfig("speech_armed_only", CHK_speechArmedOnly);

            // this can't fail because it set at startup
            NUM_tracklength.Value = Settings.Instance.GetInt32("NUM_tracklength", 200);

            // get wps on connect
            SetCheckboxFromConfig("loadwpsonconnect", CHK_loadwponconnect);

            // setup other config state
            SetCheckboxFromConfig("CHK_resetapmonconnect", CHK_resetapmonconnect);
            SetCheckboxFromConfig("CHK_rtsresetesp32", CHK_rtsresetesp32);

            CMB_rateattitude.Text = MainV2.comPort.MAV.cs.rateattitude.ToString();
            CMB_rateposition.Text = MainV2.comPort.MAV.cs.rateposition.ToString();
            CMB_raterc.Text = MainV2.comPort.MAV.cs.raterc.ToString();
            CMB_ratestatus.Text = MainV2.comPort.MAV.cs.ratestatus.ToString();
            CMB_ratesensors.Text = MainV2.comPort.MAV.cs.ratesensors.ToString();

            SetCheckboxFromConfig("analyticsoptout", chk_analytics);

            SetCheckboxFromConfig("CHK_GDIPlus", CHK_GDIPlus);
            SetCheckboxFromConfig("CHK_maprotation", CHK_maprotation);

            SetCheckboxFromConfig("CHK_disttohomeflightdata", CHK_disttohomeflightdata);

            CHK_AutoParamCommit.Visible = MainV2.DisplayConfiguration.displayParamCommitButton;

            CHK_startFullscreen.Checked = Settings.Instance.GetBoolean("always_fullscreen", true);

            //set hud color state
            var hudcolor = Settings.Instance["hudcolor"];
            if (hudcolor != null)
            {
                var index = CMB_osdcolor.Items.IndexOf(hudcolor ?? "White");
                try
                {
                    CMB_osdcolor.SelectedIndex = index;
                }
                catch
                {
                }
            }


            RestoreUnitSelection(CMB_distunits, Settings.Instance["distunits"]);
            RestoreUnitSelection(CMB_speedunits, Settings.Instance["speedunits"]);
            RestoreUnitSelection(CMB_altunits, Settings.Instance["altunits"]);

            try
            {
                if (Settings.Instance["video_device"] != null)
                {
                    CMB_videosources_Click(this, null);
                    var device = Settings.Instance.GetInt32("video_device");
                    if(CMB_videosources.Items.Count > device)
                        CMB_videosources.SelectedIndex = device;

                    if (Settings.Instance["video_options"] != "" && CMB_videosources.Text != "")
                    {
                        CMB_videoresolutions.SelectedIndex = Settings.Instance.GetInt32("video_options");
                    }
                }
            }
            catch
            {
            }


            txt_log_dir.Text = Settings.Instance.LogDir;

            // Setup aircraft icon settings
            chk_displaycog.Checked = Settings.Instance.GetBoolean("GMapMarkerBase_DisplayCOG", true);
            chk_displayheading.Checked = Settings.Instance.GetBoolean("GMapMarkerBase_DisplayHeading", true);
            chk_displaynavbearing.Checked = Settings.Instance.GetBoolean("GMapMarkerBase_DisplayNavBearing", true);
            chk_displayradius.Checked = Settings.Instance.GetBoolean("GMapMarkerBase_DisplayRadius", true);
            chk_displaytarget.Checked = Settings.Instance.GetBoolean("GMapMarkerBase_DisplayTarget", true);
            chk_displaytooltip.Checked = Settings.Instance.GetString("mapicondesc", "") != "";
            num_linelength.Value = Settings.Instance.GetInt32("GMapMarkerBase_Length", 500);

            CMB_mapCache.DataSource = Enum.GetNames(typeof(GMap.NET.AccessMode));
            try
            {
                CMB_mapCache.SelectedIndex = CMB_mapCache.Items.IndexOf(Settings.Instance["mapCache"] ?? GMap.NET.GMaps.Instance.Mode.ToString());
            }
            catch
            {
            }

            CHK_autoconnectusb.Checked = Settings.Instance.GetBoolean("auto_connect_usb", true);

            EnsureVideoSourcesLoaded();

            UpdateCustomIconUI();
            LoadConfigProfiles();

            startup = false;
        }

        private void EnsureVideoSourcesLoaded()
        {
            if (MainV2.MONO)
                return;

            try
            {
                if (CMB_videosources.Items.Count == 0)
                {
                    CMB_videosources_Click(this, null);
                }

                if (Settings.Instance["video_device"] != null)
                {
                    var device = Settings.Instance.GetInt32("video_device");
                    if (CMB_videosources.Items.Count > device)
                        CMB_videosources.SelectedIndex = device;
                }

                if (Settings.Instance["video_options"] != null && CMB_videosources.Text != "" && CMB_videoresolutions.Items.Count > 0)
                {
                    CMB_videoresolutions.SelectedIndex = Math.Min(Settings.Instance.GetInt32("video_options"), CMB_videoresolutions.Items.Count - 1);
                }
            }
            catch
            {
            }
        }

        private void BUT_videostart_Click(object sender, EventArgs e)
        {
            if (MainV2.MONO)
                return;

            // stop first
            BUT_videostop_Click(sender, e);

            var bmp = (GCSBitmapInfo)CMB_videoresolutions.SelectedItem;

            try
            {
                MainV2.cam = new Capture(CMB_videosources.SelectedIndex, bmp.Media);

                MainV2.cam.Start();

                Settings.Instance["video_device"] = CMB_videosources.SelectedIndex.ToString();

                Settings.Instance["video_options"] = CMB_videoresolutions.SelectedIndex.ToString();

                BUT_videostart.Enabled = false;
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show(UiText.Translate("Camera Fail: ") + ex.Message);
            }
        }

        private void BUT_videostop_Click(object sender, EventArgs e)
        {
            BUT_videostart.Enabled = true;
            if (MainV2.cam != null)
            {
                MainV2.cam.Dispose();
                MainV2.cam = null;
            }
        }

        private void CMB_videosources_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (MainV2.MONO)
                return;

            int hr;
            int count;
            int size;
            object o;
            IBaseFilter capFilter = null;
            ICaptureGraphBuilder2 capGraph = null;
            AMMediaType media = null;
            VideoInfoHeader v;
            VideoStreamConfigCaps c;
            var modes = new List<GCSBitmapInfo>();

            // Get the ICaptureGraphBuilder2
            capGraph = (ICaptureGraphBuilder2)new CaptureGraphBuilder2();
            var m_FilterGraph = (IFilterGraph2)new FilterGraph();

            DsDevice[] capDevices;
            capDevices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);

            // Add the video device
            hr = m_FilterGraph.AddSourceFilterForMoniker(capDevices[CMB_videosources.SelectedIndex].Mon, null,
                "Video input", out capFilter);
            try
            {
                DsError.ThrowExceptionForHR(hr);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show(UiText.Translate("Can not add video source\n") + ex);
                return;
            }

            // Find the stream config interface
            hr = capGraph.FindInterface(PinCategory.Capture, MediaType.Video, capFilter, typeof(IAMStreamConfig).GUID,
                out o);
            DsError.ThrowExceptionForHR(hr);

            var videoStreamConfig = o as IAMStreamConfig;
            if (videoStreamConfig == null)
            {
                CustomMessageBox.Show(UiText.Translate("Failed to get IAMStreamConfig"));
                return;
            }

            hr = videoStreamConfig.GetNumberOfCapabilities(out count, out size);
            DsError.ThrowExceptionForHR(hr);
            var TaskMemPointer = Marshal.AllocCoTaskMem(size);
            for (var i = 0; i < count; i++)
            {
                var ptr = IntPtr.Zero;

                hr = videoStreamConfig.GetStreamCaps(i, out media, TaskMemPointer);
                v = (VideoInfoHeader)Marshal.PtrToStructure(media.formatPtr, typeof(VideoInfoHeader));
                c = (VideoStreamConfigCaps)Marshal.PtrToStructure(TaskMemPointer, typeof(VideoStreamConfigCaps));
                modes.Add(new GCSBitmapInfo(v.BmiHeader.Width, v.BmiHeader.Height, c.MaxFrameInterval,
                    c.VideoStandard.ToString(), media));
            }
            Marshal.FreeCoTaskMem(TaskMemPointer);
            DsUtils.FreeAMMediaType(media);

            CMB_videoresolutions.DataSource = modes;

            if (Settings.Instance["video_options"] != "" && CMB_videosources.Text != "")
            {
                try
                {
                    CMB_videoresolutions.SelectedIndex = Settings.Instance.GetInt32("video_options");
                }
                catch
                {
                } // ignore bad entries
            }
        }

        private void CHK_hudshow_CheckedChanged(object sender, EventArgs e)
        {
            FlightData.myhud.hudon = CHK_hudshow.Checked;
            Settings.Instance["CHK_hudshow"] = CHK_hudshow.Checked.ToString();
        }

        private void CHK_enablespeech_CheckedChanged(object sender, EventArgs e)
        {
            MainV2.speechEnable = CHK_enablespeech.Checked;
            Settings.Instance["speechenable"] = CHK_enablespeech.Checked.ToString();
            if (MainV2.speechEngine != null)
                MainV2.speechEngine.SpeakAsyncCancelAll();

            if (CHK_enablespeech.Checked)
            {
                CHK_speechArmedOnly.Visible = true;
                CHK_speechwaypoint.Visible = true;
                CHK_speechaltwarning.Visible = true;
                CHK_speechbattery.Visible = true;
                CHK_speechcustom.Visible = true;
                CHK_speechmode.Visible = true;
                CHK_speecharmdisarm.Visible = true;
                CHK_speechlowspeed.Visible = true;
                CHK_speechadsb.Visible = true;
            }
            else
            {
                CHK_speechArmedOnly.Visible = false;
                CHK_speechwaypoint.Visible = false;
                CHK_speechaltwarning.Visible = false;
                CHK_speechbattery.Visible = false;
                CHK_speechcustom.Visible = false;
                CHK_speechmode.Visible = false;
                CHK_speecharmdisarm.Visible = false;
                CHK_speechlowspeed.Visible = false;
                CHK_speechadsb.Visible = false;
            }
        }

        private void CMB_severity_SelectedIndexChanged(object sender, EventArgs e)
        {
            Settings.Instance["severity"] = CMB_severity.SelectedIndex.ToString();
        }

        private void SetupLanguageSelector()
        {
            // setup language selection
            var cultureCodes = new[]
            {
                "en-US", "zh-Hans", "zh-CN", "zh-TW", "ru-RU", "Fr", "Pl", "it-IT", "es-ES", "de-DE", "ja-JP", "id-ID", "ko-KR",
                "ar", "pt", "tr", "ru-KZ", "uk"
            };

            _languages = cultureCodes
                .Select(CultureInfoEx.GetCultureInfo)
                .Where(c => c != null)
                .ToList();

            CMB_language.DisplayMember = "NativeName";
            CMB_language.DataSource = _languages;
            var currentUiCulture = string.IsNullOrEmpty(Settings.Instance["language"])
                ? Thread.CurrentThread.CurrentUICulture
                : CultureInfoEx.GetCultureInfo(Settings.Instance["language"]) ?? Thread.CurrentThread.CurrentUICulture;

            // Prefer the exact variant before its parent (zh-CN inherits zh-Hans).
            CMB_language.SelectedItem = _languages.FirstOrDefault(c => c.Name == currentUiCulture.Name)
                ?? _languages.FirstOrDefault(c => currentUiCulture.IsChildOf(c));

        }

        private void CMB_language_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedCulture = CMB_language.SelectedItem as CultureInfo;
            if (startup || selectedCulture == null || Settings.Instance["language"] == selectedCulture.Name)
                return;

            // Save the preference for the next launch. Keep this session's UI and
            // vehicle connection intact instead of partially relocalizing and closing.
            var previousLanguage = Settings.Instance["language"];
            Settings.Instance["language"] = selectedCulture.Name;
            try
            {
                Settings.Instance.Save();
            }
            catch (Exception ex)
            {
                Settings.Instance["language"] = previousLanguage;
                startup = true;
                try
                {
                    CMB_language.SelectedItem = _languages.FirstOrDefault(c => c.Name == previousLanguage)
                        ?? _languages.FirstOrDefault(c => c.Name == Thread.CurrentThread.CurrentUICulture.Name)
                        ?? _languages.FirstOrDefault(c => Thread.CurrentThread.CurrentUICulture.IsChildOf(c));
                }
                finally
                {
                    startup = false;
                }
                CustomMessageBox.Show(ex.Message, Strings.ERROR);
                return;
            }

            CustomMessageBox.Show(UiText.Translate(
                "Language saved. It will take effect the next time you start TitanPlanner. You can continue working in this session."));
        }

        private void CMB_osdcolor_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (startup)
                return;
/*
            if (CMB_osdcolor.Text != "")
            {
                Settings.Instance["hudcolor"] = CMB_osdcolor.Text;
                FlightData.myhud.hudcolor =
                    Color.FromKnownColor((KnownColor)Enum.Parse(typeof(KnownColor), CMB_osdcolor.Text));
            }
            */
        }

        private void CHK_speechwaypoint_CheckedChanged(object sender, EventArgs e)
        {
            if (startup)
                return;
            Settings.Instance["speechwaypointenabled"] = ((CheckBox)sender).Checked.ToString();

            if (((CheckBox)sender).Checked)
            {
                var speechstring = "Heading to Waypoint {wpn}";
                if (Settings.Instance["speechwaypoint"] != null)
                    speechstring = Settings.Instance["speechwaypoint"].ToString();
                if (DialogResult.Cancel ==
                    InputBox.Show(UiText.Translate("Notification"), UiText.Translate("What do you want it to say?"), ref speechstring))
                    return;
                Settings.Instance["speechwaypoint"] = speechstring;
            }
        }

        private void CHK_speechmode_CheckedChanged(object sender, EventArgs e)
        {
            if (startup)
                return;
            Settings.Instance["speechmodeenabled"] = ((CheckBox)sender).Checked.ToString();

            if (((CheckBox)sender).Checked)
            {
                var speechstring = "Mode changed to {mode}";
                if (Settings.Instance["speechmode"] != null)
                    speechstring = Settings.Instance["speechmode"].ToString();
                if (DialogResult.Cancel ==
                    InputBox.Show(UiText.Translate("Notification"), UiText.Translate("What do you want it to say?"), ref speechstring))
                    return;
                Settings.Instance["speechmode"] = speechstring;
            }
        }

        private void CHK_speechcustom_CheckedChanged(object sender, EventArgs e)
        {
            if (startup)
                return;
            Settings.Instance["speechcustomenabled"] = ((CheckBox)sender).Checked.ToString();

            if (((CheckBox)sender).Checked)
            {
                var speechstring = "Heading to Waypoint {wpn}, altitude is {alt}, Ground speed is {gsp} ";
                if (Settings.Instance["speechcustom"] != null)
                    speechstring = Settings.Instance["speechcustom"].ToString();
                if (DialogResult.Cancel ==
                    InputBox.Show(UiText.Translate("Notification"), UiText.Translate("What do you want it to say?"), ref speechstring))
                    return;
                Settings.Instance["speechcustom"] = speechstring;
            }
        }

        private void BUT_rerequestparams_Click(object sender, EventArgs e)
        {
            if (!MainV2.comPort.BaseStream.IsOpen)
                return;
            ((MyButton)sender).Enabled = false;
            try
            {
                MainV2.comPort.getParamList();
            }
            catch
            {
                CustomMessageBox.Show(UiText.Translate("Error: getting param list"));
            }


            ((MyButton)sender).Enabled = true;
            startup = true;


            startup = false;
        }

        private void CHK_speechbattery_CheckedChanged(object sender, EventArgs e)
        {
            if (startup)
                return;
            Settings.Instance["speechbatteryenabled"] = ((CheckBox)sender).Checked.ToString();

            if (((CheckBox)sender).Checked)
            {
                var speechstring = "WARNING, Battery at {batv} Volt, {batp} percent";
                if (Settings.Instance["speechbattery"] != null)
                    speechstring = Settings.Instance["speechbattery"].ToString();
                if (DialogResult.Cancel ==
                    InputBox.Show(UiText.Translate("Notification"), UiText.Translate("What do you want it to say?"), ref speechstring))
                    return;
                Settings.Instance["speechbattery"] = speechstring;

                speechstring = "9.6";
                if (Settings.Instance["speechbatteryvolt"] != null)
                    speechstring = Settings.Instance["speechbatteryvolt"].ToString();
                if (DialogResult.Cancel ==
                    InputBox.Show(UiText.Translate("Battery Level"), UiText.Translate("What Voltage do you want to warn at?"), ref speechstring))
                    return;
                Settings.Instance["speechbatteryvolt"] = speechstring;

                speechstring = "20";
                if (Settings.Instance["speechbatterypercent"] != null)
                    speechstring = Settings.Instance["speechbatterypercent"].ToString();
                if (DialogResult.Cancel ==
                    InputBox.Show(UiText.Translate("Battery Level"), UiText.Translate("What percentage do you want to warn at?"), ref speechstring))
                    return;
                Settings.Instance["speechbatterypercent"] = speechstring;
            }
        }

        private void BUT_Joystick_Click(object sender, EventArgs e)
        {
            new JoystickSetup().ShowUserControl();
        }

        private static void FormatUnit(object sender, ListControlConvertEventArgs e)
        {
            if (e.ListItem is string key)
                e.Value = UiText.Translate(key);
        }

        private static void RestoreUnitSelection(ComboBox combo, string savedKey)
        {
            if (savedKey == null)
                return;
            for (int i = 0; i < combo.Items.Count; i++)
            {
                if (string.Equals(combo.Items[i] as string, savedKey, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
        }

        private void CMB_distunits_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (startup || !(CMB_distunits.SelectedItem is string key))
                return;
            Settings.Instance["distunits"] = key;
            MainV2.instance.ChangeUnits();
        }

        private void CMB_speedunits_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (startup || !(CMB_speedunits.SelectedItem is string key))
                return;
            Settings.Instance["speedunits"] = key;
            MainV2.instance.ChangeUnits();
        }

        private void CMB_rateattitude_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (startup)
                return;
            Settings.Instance[((ComboBox)sender).Name] = ((ComboBox)sender).Text;
            MainV2.comPort.MAV.cs.rateattitude = int.Parse(((ComboBox)sender).Text);

            CurrentState.rateattitudebackup = MainV2.comPort.MAV.cs.rateattitude;

            MainV2.comPort.requestDatastream(MAVLink.MAV_DATA_STREAM.EXTRA1, MainV2.comPort.MAV.cs.rateattitude);
            // request attitude
            MainV2.comPort.requestDatastream(MAVLink.MAV_DATA_STREAM.EXTRA2, MainV2.comPort.MAV.cs.rateattitude);
            // request vfr
        }

        private void CMB_rateposition_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (startup)
                return;
            Settings.Instance[((ComboBox)sender).Name] = ((ComboBox)sender).Text;
            MainV2.comPort.MAV.cs.rateposition = int.Parse(((ComboBox)sender).Text);

            CurrentState.ratepositionbackup = MainV2.comPort.MAV.cs.rateposition;

            MainV2.comPort.requestDatastream(MAVLink.MAV_DATA_STREAM.POSITION, MainV2.comPort.MAV.cs.rateposition);
            // request gps
        }

        private void CMB_ratestatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (startup)
                return;
            Settings.Instance[((ComboBox)sender).Name] = ((ComboBox)sender).Text;
            MainV2.comPort.MAV.cs.ratestatus = int.Parse(((ComboBox)sender).Text);

            CurrentState.ratestatusbackup = MainV2.comPort.MAV.cs.ratestatus;

            MainV2.comPort.requestDatastream(MAVLink.MAV_DATA_STREAM.EXTENDED_STATUS, MainV2.comPort.MAV.cs.ratestatus);
            // mode
        }

        private void CMB_raterc_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (startup)
                return;
            Settings.Instance[((ComboBox)sender).Name] = ((ComboBox)sender).Text;
            MainV2.comPort.MAV.cs.raterc = int.Parse(((ComboBox)sender).Text);

            CurrentState.ratercbackup = MainV2.comPort.MAV.cs.raterc;

            MainV2.comPort.requestDatastream(MAVLink.MAV_DATA_STREAM.RC_CHANNELS, MainV2.comPort.MAV.cs.raterc);
            // request rc info
        }

        private void CMB_ratesensors_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (startup)
                return;
            Settings.Instance[((ComboBox)sender).Name] = ((ComboBox)sender).Text;
            MainV2.comPort.MAV.cs.ratesensors = int.Parse(((ComboBox)sender).Text);

            CurrentState.ratesensorsbackup = MainV2.comPort.MAV.cs.ratesensors;

            MainV2.comPort.requestDatastream(MAVLink.MAV_DATA_STREAM.EXTRA3, MainV2.comPort.MAV.cs.ratesensors);
            // request extra stuff - tridge
            MainV2.comPort.requestDatastream(MAVLink.MAV_DATA_STREAM.RAW_SENSORS, MainV2.comPort.MAV.cs.ratesensors);
            // request raw sensor
        }

        private void CHK_mavdebug_CheckedChanged(object sender, EventArgs e)
        {
            MainV2.comPort.debugmavlink = CHK_mavdebug.Checked;
        }

        private void CHK_resetapmonconnect_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Instance[((CheckBox)sender).Name] = ((CheckBox)sender).Checked.ToString();
        }

        private void CHK_rtsresetesp32_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Instance[((CheckBox)sender).Name] = ((CheckBox)sender).Checked.ToString();
        }




        private void CHK_speechaltwarning_CheckedChanged(object sender, EventArgs e)
        {
            if (startup)
                return;
            Settings.Instance["speechaltenabled"] = ((CheckBox)sender).Checked.ToString();

            if (((CheckBox)sender).Checked)
            {
                var speechstring = "WARNING, low altitude {alt}";
                if (Settings.Instance["speechalt"] != null)
                    speechstring = Settings.Instance["speechalt"].ToString();
                if (DialogResult.Cancel ==
                    InputBox.Show(UiText.Translate("Notification"), UiText.Translate("What do you want it to say?"), ref speechstring))
                    return;
                Settings.Instance["speechalt"] = speechstring;

                speechstring = "2";
                if (Settings.Instance["speechaltheight"] != null)
                    speechstring = Settings.Instance["speechaltheight"].ToString();
                if (DialogResult.Cancel ==
                    InputBox.Show(UiText.Translate("Min Alt"), UiText.Translate("What altitude do you want to warn at? (relative to home)"),
                        ref speechstring))
                    return;
                Settings.Instance["speechaltheight"] = (double.Parse(speechstring) / CurrentState.multiplieralt).ToString();
                // save as m
            }
        }

        private void NUM_tracklength_ValueChanged(object sender, EventArgs e)
        {
            Settings.Instance["NUM_tracklength"] = NUM_tracklength.Value.ToString();
        }

        private void CHK_loadwponconnect_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Instance["loadwpsonconnect"] = CHK_loadwponconnect.Checked.ToString();
        }

        private void CHK_GDIPlus_CheckedChanged(object sender, EventArgs e)
        {
            if (startup)
                return;
            CustomMessageBox.Show(UiText.Translate("You need to restart the planner for this to take effect"));
            Settings.Instance["CHK_GDIPlus"] = CHK_GDIPlus.Checked.ToString();
        }

        // This load handler now only contains code that should execute once
        // on start up. See Activate() for the remainder
        private void ConfigPlanner_Load(object sender, EventArgs e)
        {
        }

        private void CMB_osdcolor_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0)
                return;

            var g = e.Graphics;
            var rect = e.Bounds;
            Brush brush = null;

            if ((e.State & DrawItemState.Selected) == 0)
                brush = new SolidBrush(CMB_osdcolor.BackColor);
            else
                brush = SystemBrushes.Highlight;

            g.FillRectangle(brush, rect);

            brush = new SolidBrush(Color.FromName((string)CMB_osdcolor.Items[e.Index]));

            g.FillRectangle(brush, rect.X + 2, rect.Y + 2, 30, rect.Height - 4);
            g.DrawRectangle(Pens.Black, rect.X + 2, rect.Y + 2, 30, rect.Height - 4);

            if ((e.State & DrawItemState.Selected) == 0)
                brush = new SolidBrush(CMB_osdcolor.ForeColor);
            else
                brush = SystemBrushes.HighlightText;
            g.DrawString(CMB_osdcolor.Items[e.Index].ToString(),
                CMB_osdcolor.Font, brush, rect.X + 35, rect.Top + rect.Height - CMB_osdcolor.Font.Height);
        }

        private void CMB_videosources_Click(object sender, EventArgs e)
        {
            if (MainV2.MONO)
                return;
            // the reason why i dont populate this list is because on linux/mac this call will fail.
            var capt = new Capture();

            var devices = WebCamService.Capture.getDevices();

            CMB_videosources.DataSource = devices;

            capt.Dispose();
        }

        private void CHK_maprotation_CheckedChanged(object sender, EventArgs e)
        {
            if (startup)
                return;
            Settings.Instance["CHK_maprotation"] = CHK_maprotation.Checked.ToString();
            if (CHK_maprotation.Checked)
            {
                chk_shownofly.Checked = false;
            }
            FlightData.instance.gMapControl1.Bearing = 0;
        }

        private static void SetCheckboxFromConfig(string configKey, CheckBox chk)
        {
            if (Settings.Instance[configKey] != null)
                chk.Checked = Settings.Instance.GetBoolean(configKey);
        }

        private void CHK_disttohomeflightdata_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Instance["CHK_disttohomeflightdata"] = CHK_disttohomeflightdata.Checked.ToString();
        }

        private void BUT_logdirbrowse_Click(object sender, EventArgs e)
        {
            var ofd = new FolderBrowserDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txt_log_dir.Text = ofd.SelectedPath;
            }
        }

        private void OnLogDirTextChanged(object sender, EventArgs e)
        {
            string path = txt_log_dir.Text;
            if (!string.IsNullOrEmpty(path) && System.IO.Directory.Exists(path))
            {
                Settings.Instance.LogDir = path;
            }
        }

        private static string FormatThemeCaption(string filename)
        {
            var key = "Theme file: " + filename;
            var translated = UiText.Translate(key);
            return translated == key ? filename : translated + " (" + filename + ")";
        }

        private void CMB_theme_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (startup || !(CMB_theme.SelectedItem is string filename))
                return;

            ThemeManager.LoadTheme(filename);
            ThemeManager.ApplyThemeTo(MainV2.instance);
            CustomMessageBox.Show(UiText.Translate("You may need to select another tab or restart to see the full effect."));
        }

        private void BUT_themecustom_Click(object sender, EventArgs e)
        {
            ThemeManager.StartThemeEditor();
        }

        private void CHK_speecharmdisarm_CheckedChanged(object sender, EventArgs e)
        {
            if (startup)
                return;
            Settings.Instance["speecharmenabled"] = ((CheckBox)sender).Checked.ToString();

            if (((CheckBox)sender).Checked)
            {
                var speechstring = "Armed";
                if (Settings.Instance["speecharm"] != null)
                    speechstring = Settings.Instance["speecharm"];
                if (DialogResult.Cancel == InputBox.Show(UiText.Translate("Arm"), UiText.Translate("What do you want it to say?"), ref speechstring))
                    return;
                Settings.Instance["speecharm"] = speechstring;

                speechstring = UiText.Translate("Disarmed");
                if (Settings.Instance["speechdisarm"] != null)
                    speechstring = Settings.Instance["speechdisarm"];
                if (DialogResult.Cancel == InputBox.Show(UiText.Translate("Disarmed"), UiText.Translate("What do you want it to say?"), ref speechstring))
                    return;
                Settings.Instance["speechdisarm"] = speechstring;
            }
        }

        private void CHK_speechadsb_CheckedChanged(object sender, EventArgs e)
        {
            if (startup)
                return;
            Settings.Instance["speechadsbenabled"] = ((CheckBox)sender).Checked.ToString();
        }   

        private void BUT_Vario_Click(object sender, EventArgs e)
        {
            if (Vario.run)
            {
                Vario.Stop();
            }
            else
            {
                Vario.Start();
            }
        }

        private void chk_analytics_CheckedChanged(object sender, EventArgs e)
        {
            Tracking.OptOut = chk_analytics.Checked;
            Settings.Instance["analyticsoptout"] = chk_analytics.Checked.ToString();
        }

        private void CHK_beta_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Instance["beta_updates"] = CHK_beta.Checked.ToString();

            MissionPlanner.Utilities.Update.dobeta = CHK_beta.Checked;

            // turned on by the user (not loaded from settings): offer the development build now
            if (!startup && CHK_beta.Checked)
                BtnCheckUpdate_Click(sender, e);
        }

        private void CHK_Password_CheckedChanged(object sender, EventArgs e)
        {
            if (startup)
                return;

            Settings.Instance["password_protect"] = CHK_Password.Checked.ToString();
            if (CHK_Password.Checked)
            {
                // keep this one local
                string pw = "";

                InputBox.Show(UiText.Translate("Enter Password"), UiText.Translate("Please enter a password"), ref pw, true);

                Password.EnterPassword(pw);
            }
        }

        private void CHK_speechlowspeed_CheckedChanged(object sender, EventArgs e)
        {
            if (startup)
                return;
            Settings.Instance["speechlowspeedenabled"] = ((CheckBox)sender).Checked.ToString();

            if (((CheckBox)sender).Checked)
            {
                var speechstring = "Low Ground Speed {gsp}";
                if (Settings.Instance["speechlowgroundspeed"] != null)
                    speechstring = Settings.Instance["speechlowgroundspeed"];
                if (DialogResult.Cancel ==
                    InputBox.Show(UiText.Translate("Ground Speed"), UiText.Translate("What do you want it to say?"), ref speechstring))
                    return;
                Settings.Instance["speechlowgroundspeed"] = speechstring;

                speechstring = "0";
                if (Settings.Instance["speechlowgroundspeedtrigger"] != null)
                    speechstring = Settings.Instance["speechlowgroundspeedtrigger"];
                if (DialogResult.Cancel ==
                    InputBox.Show(UiText.Translate("speed trigger"), UiText.Translate("What speed do you want to warn at (m/s)?"), ref speechstring))
                    return;
                Settings.Instance["speechlowgroundspeedtrigger"] = speechstring;

                speechstring = "Low Air Speed {asp}";
                if (Settings.Instance["speechlowairspeed"] != null)
                    speechstring = Settings.Instance["speechlowairspeed"];
                if (DialogResult.Cancel == InputBox.Show(UiText.Translate("Air Speed"), UiText.Translate("What do you want it to say?"), ref speechstring))
                    return;
                Settings.Instance["speechlowairspeed"] = speechstring;

                speechstring = "0";
                if (Settings.Instance["speechlowairspeedtrigger"] != null)
                    speechstring = Settings.Instance["speechlowairspeedtrigger"];
                if (DialogResult.Cancel ==
                    InputBox.Show(UiText.Translate("speed trigger"), UiText.Translate("What speed do you want to warn at (m/s)?"), ref speechstring))
                    return;
                Settings.Instance["speechlowairspeedtrigger"] = speechstring;
            }
        }

        private void CHK_showairports_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Instance["showairports"] = CHK_showairports.Checked.ToString();
            MainV2.ShowAirports = CHK_showairports.Checked;
        }

        private void chk_ADSB_CheckedChanged(object sender, EventArgs e)
        {
            if (startup)
                return;

            if (((CheckBox)sender).Checked)
            {
                var server = adsb.MigrateServer(Settings.Instance["adsbserverv2"] ?? Settings.Instance["adsbserver"]) ?? adsb.DefaultServer;
                if (DialogResult.Cancel == InputBox.Show(UiText.Translate("ADSB Server"), UiText.Translate("Server IP or API base URL (see https://ardupilot.org/planner/docs/common-adsb.html)\nDefault: live data from adsb.fi (https://adsb.fi), for personal, non-commercial use"), ref server))
                    return;
                // Strip ending slash off server
                if (server.EndsWith("/"))
                    server = server.Substring(0, server.Length - 1);
                Settings.Instance["adsbserverv2"] = server;

                // if server isn't an HTTP(S) URL, assume TCP and ask for a port
                if (!server.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                {
                    var port = "30003";
                    if (Settings.Instance["adsbport"] != null)
                        port = Settings.Instance["adsbport"];
                    if (DialogResult.Cancel == InputBox.Show(UiText.Translate("Server port"), UiText.Translate("Server port?"), ref port))
                        return;
                    Settings.Instance["adsbport"] = port;
                }
            }
            else
            {
                // if we're disabling ADSB, clear the list of planes
                MainV2.instance.adsbPlanes.Clear();
            }

            Settings.Instance["enableadsb"] = chk_ADSB.Checked.ToString();
            MainV2.instance.EnableADSB = chk_ADSB.Checked;
        }

        private void chk_tfr_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Instance["showtfr"] = chk_tfr.Checked.ToString();
            MainV2.ShowTFR = chk_tfr.Checked;
        }

        public class GCSBitmapInfo
        {
            public GCSBitmapInfo(int width, int height, long fps, string standard, AMMediaType media)
            {
                Width = width;
                Height = height;
                Fps = fps;
                Standard = standard;
                Media = media;
            }

            public int Width { get; set; }
            public int Height { get; set; }
            public long Fps { get; set; }
            public string Standard { get; set; }
            public AMMediaType Media { get; set; }

            public override string ToString()
            {
                return Width + " x " + Height + string.Format(" {0:0.00} fps ", 10000000.0 / Fps) + Standard;
            }
        }

        private void chk_temp_CheckedChanged(object sender, EventArgs e)
        {
            if (chk_temp.Checked)
            {
                temp = new temp();
                temp.FormClosing += chk_temp_FormClosing;
                temp.Show();
            }
            else
            {
                if (temp != null)
                {
                    temp.Close();
                }
            }
        }

        private void chk_temp_FormClosing(object sender, EventArgs e)
        {
            chk_temp.Checked = false;
        }

        private void chk_norcreceiver_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Instance["norcreceiver"] = chk_norcreceiver.Checked.ToString();
        }

        private void CMB_Layout_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ((DisplayNames)CMB_Layout.SelectedItem == DisplayNames.Advanced)
            {
                MainV2.DisplayConfiguration = MainV2.DisplayConfiguration.Advanced();
            }
            else if ((DisplayNames)CMB_Layout.SelectedItem == DisplayNames.Basic)
            {
                MainV2.DisplayConfiguration = MainV2.DisplayConfiguration.Basic();
            }
            else if ((DisplayNames)CMB_Layout.SelectedItem == DisplayNames.Custom)
            {
                MainV2.DisplayConfiguration = MainV2.DisplayConfiguration.Custom();
            }
            Settings.Instance["displayview"] = MainV2.DisplayConfiguration.ConvertToString();
        }

        private void CHK_AutoParamCommit_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Instance["autoParamCommit"] = CHK_AutoParamCommit.Checked.ToString();
        }

        private void chk_shownofly_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Instance["ShowNoFly"] = chk_shownofly.Checked.ToString();
            if (chk_shownofly.Checked)
            {
                CHK_maprotation.Checked = false;
            }
        }

        private void CMB_altunits_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (startup || !(CMB_altunits.SelectedItem is string key))
                return;
            Settings.Instance["altunits"] = key;
            MainV2.instance.ChangeUnits();
        }

        private void num_gcsid_ValueChanged(object sender, EventArgs e)
        {
            MAVLinkInterface.gcssysid = (uint)num_gcsid.Value;
            Settings.Instance["gcsid"] = num_gcsid.Value.ToString();
        }

        private void CHK_params_bg_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Instance["Params_BG"] = CHK_params_bg.Checked.ToString();
        }

        private void chk_slowMachine_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Instance["SlowMachine"] = chk_slowMachine.Checked.ToString();
        }

        private void CHK_speechArmedOnly_CheckedChanged(object sender, EventArgs e)
        {
            MainV2.speech_armed_only = CHK_speechArmedOnly.Checked;
            Settings.Instance["speech_armed_only"] = CHK_speechArmedOnly.Checked.ToString();
        }

        private void chk_displaycog_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Instance["GMapMarkerBase_DisplayCOG"] = chk_displaycog.Checked.ToString();
            Maps.GMapMarkerBase.DisplayCOGSetting = chk_displaycog.Checked;
        }

        private void chk_displayheading_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Instance["GMapMarkerBase_DisplayHeading"] = chk_displayheading.Checked.ToString();
            Maps.GMapMarkerBase.DisplayHeadingSetting = chk_displayheading.Checked;
        }

        private void chk_displaynavbearing_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Instance["GMapMarkerBase_DisplayNavBearing"] = chk_displaynavbearing.Checked.ToString();
            Maps.GMapMarkerBase.DisplayNavBearingSetting = chk_displaynavbearing.Checked;
        }

        private void chk_displayradius_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Instance["GMapMarkerBase_DisplayRadius"] = chk_displayradius.Checked.ToString();
            Maps.GMapMarkerBase.DisplayRadiusSetting = chk_displayradius.Checked;
        }

        private void chk_displaytarget_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Instance["GMapMarkerBase_DisplayTarget"] = chk_displaytarget.Checked.ToString();
            Maps.GMapMarkerBase.DisplayTargetSetting = chk_displaytarget.Checked;
        }

        private void chk_displaytooltip_CheckedChanged(object sender, EventArgs e)
        {
            if (startup)
            {
                return;
            }
            if (chk_displaytooltip.Checked)
            {
                // Prompt user for text
                var descstring = Settings.Instance["mapicondesc_default",
                    "{alt}{altunit} {airspeed}{speedunit} id:{sysid} Sats:{satcount} HDOP:{gpshdop} Volts:{battery_voltage}"];

                if (DialogResult.Cancel == InputBox.Show(UiText.Translate("Description"), UiText.Translate("What do you want it to show?"), ref descstring))
                {
                    return;
                }

                Settings.Instance["mapicondesc"] = descstring;
                Settings.Instance["mapicondesc_default"] = descstring;
            }
            else
            {
                Settings.Instance["mapicondesc"] = "";
            }

        }

        private void num_linelength_ValueChanged(object sender, EventArgs e)
        {
            Settings.Instance["GMapMarkerBase_length"] = num_linelength.Value.ToString();
            Maps.GMapMarkerBase.length = (int)(num_linelength.Value);
        }

        private void cmb_secondarydisplaystyle_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(startup)
            {
                return;
            }
            if (cmb_secondarydisplaystyle.SelectedItem is string key &&
                Enum.TryParse(key, out Maps.GMapMarkerBase.InactiveDisplayStyleEnum result))
            {
                Settings.Instance["GMapMarkerBase_InactiveStyle"] = key;
                Maps.GMapMarkerBase.InactiveDisplayStyle = result;
            }
            else
            {
                Settings.Instance["GMapMarkerBase_InactiveStyle"] = Maps.GMapMarkerBase.InactiveDisplayStyleEnum.Transparent.ToString();
                Maps.GMapMarkerBase.InactiveDisplayStyle = Maps.GMapMarkerBase.InactiveDisplayStyleEnum.Transparent;
            }
        }

        private void CMB_mapCache_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (startup || !(CMB_mapCache.SelectedItem is string key) ||
                !Enum.TryParse(key, out GMap.NET.AccessMode mode))
                return;
            Settings.Instance["mapCache"] = key;
            GMap.NET.GMaps.Instance.Mode = mode;
        }

        private void BUT_mapCacheDir_Click(object sender, EventArgs e)
        {
            try
            {
                string folderPath = MyImageCache.Instance.CacheLocation;
                if (Directory.Exists(folderPath))
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        Arguments = folderPath,
                        FileName = "explorer.exe"
                    };

                    Process.Start(startInfo);
                }
                else
                {
                    MessageBox.Show(string.Format("{0} Directory does not exist!", folderPath));
                }
            }
            catch (Exception)
            {
            }
        }

        private void CHK_autoconnectusb_CheckedChanged(object sender, EventArgs e)
        {
            if (startup)
                return;
            Settings.Instance["auto_connect_usb"] = CHK_autoconnectusb.Checked.ToString();
            MainV2.instance?.SetAutoConnectUSB(CHK_autoconnectusb.Checked);
        }

        private void CHK_startFullscreen_CheckedChanged(object sender, EventArgs e)
        {
            if (startup)
                return;
            Settings.Instance["always_fullscreen"] = CHK_startFullscreen.Checked.ToString();
        }

        private void BUT_customicon_Click(object sender, EventArgs e)
        {
            var customIconPath = Settings.Instance["custom_aircraft_icon"];
            bool hasCustomIcon = !string.IsNullOrEmpty(customIconPath);

            if (hasCustomIcon)
            {
                // Reset - clear the setting
                Settings.Instance["custom_aircraft_icon"] = null;
                GMapMarkerBase.ClearCustomIcon();
                UpdateCustomIconUI();
            }
            else
            {
                // Set - open file dialog
                var ofd = new OpenFileDialog();
                ofd.Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp";
                ofd.Title = UiText.Translate("Select Custom Aircraft Icon");

                if (ofd.ShowDialog(MainV2.instance) == DialogResult.OK)
                {
                    Settings.Instance["custom_aircraft_icon"] = ofd.FileName;
                    if (GMapMarkerBase.LoadCustomIcon())
                    {
                        UpdateCustomIconUI();
                    }
                    else
                    {
                        Settings.Instance["custom_aircraft_icon"] = null;
                    }
                }
            }
        }

        private void UpdateCustomIconUI()
        {
            try
            {
                var customIconPath = Settings.Instance["custom_aircraft_icon"];
                if (!string.IsNullOrEmpty(customIconPath))
                {
                    BUT_customicon.Text = Path.GetFileName(customIconPath);
                }
                else
                {
                    BUT_customicon.Text = UiText.Translate("Set");
                }
            }
            catch
            {
                BUT_customicon.Text = UiText.Translate("Set");
            }
        }

        private void LoadConfigProfiles()
        {
            try
            {
                CMB_configprofile.Items.Clear();
                var configs = Settings.GetAvailableConfigs();
                foreach (var config in configs)
                {
                    CMB_configprofile.Items.Add(config);
                }

                // Select current config
                var currentConfig = Settings.CurrentConfigName;
                var index = CMB_configprofile.Items.IndexOf(currentConfig);
                if (index >= 0)
                {
                    CMB_configprofile.SelectedIndex = index;
                }
                else if (CMB_configprofile.Items.Count > 0)
                {
                    CMB_configprofile.SelectedIndex = 0;
                }
            }
            catch { }
        }

        private void CMB_configprofile_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (startup)
                return;

            var selectedConfig = CMB_configprofile.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedConfig))
                return;

            if (selectedConfig != Settings.CurrentConfigName)
            {
                // Save current config first
                Settings.Instance.Save();

                // Load the selected config
                if (Settings.Instance.LoadConfig(selectedConfig))
                {
                    // Apply the theme from the loaded config
                    var themeName = Settings.Instance["theme"];
                    if (!string.IsNullOrEmpty(themeName))
                    {
                        ThemeManager.LoadTheme(themeName);
                        ThemeManager.ApplyThemeTo(MainV2.instance);
                    }

                    // Reload ConfigPlanner to reflect new settings
                    startup = true;
                    Visible = false;
                    SuspendLayout();
                    BuildLayout();
                    ResumeLayout(true);
                    // Apply theme to the newly created controls
                    ThemeManager.ApplyThemeTo(this);
                    Visible = true;
                    startup = false;
                }
            }
        }

        private void BUT_saveconfig_Click(object sender, EventArgs e)
        {
            var newName = TXT_newconfigname.Text.Trim();

            if (string.IsNullOrEmpty(newName))
            {
                CustomMessageBox.Show(UiText.Translate("Please enter a configuration name."), UiText.Translate("Save Configuration"));
                return;
            }

            if (!Settings.IsValidConfigName(newName))
            {
                CustomMessageBox.Show(UiText.Translate("Configuration name can only contain letters (A-Z), numbers (0-9), and underscores."), UiText.Translate("Save Configuration"));
                return;
            }

            if (Settings.Instance.SaveConfigAs(newName))
            {
                TXT_newconfigname.Text = "";
                LoadConfigProfiles();
                CustomMessageBox.Show(UiText.Format($"Configuration saved as '{newName}'."), UiText.Translate("Save Configuration"));
            }
            else
            {
                CustomMessageBox.Show(UiText.Translate("Failed to save configuration."), UiText.Translate("Save Configuration"));
            }
        }
    }
}
