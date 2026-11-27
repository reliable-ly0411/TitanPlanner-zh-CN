using log4net;
using MissionPlanner.ArduPilot;
using MissionPlanner.Comms;
using MissionPlanner.Controls;
using MissionPlanner.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MissionPlanner.GCSViews.ConfigurationView
{
    [KeyboardShortcutScope("Install Firmware", 5)]
    public partial class ConfigFirmware : MyUserControl, IActivate, IDeactivate
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private List<Firmware.software> softwares = new List<Firmware.software>();
        private readonly Firmware fw = new Firmware();
        private string custom_fw_dir = Settings.Instance["FirmwareFileDirectory"] ?? "";
        private string firmwareurl = "";
        private readonly FirmwareListRequest<string, List<Firmware.software>> listRequest;
        private readonly Dictionary<Control, string> listControls;
        private readonly Func<string, List<Firmware.software>> fetchList;
        private bool listReady;
        private string detectedport;

        public ConfigFirmware() : this(url => new Firmware().getFWList(url))
        {
        }

        internal ConfigFirmware(Func<string, List<Firmware.software>> fetchList)
        {
            InitializeComponent();
            this.fetchList = fetchList;
            listRequest = new FirmwareListRequest<string, List<Firmware.software>>(this);
            listControls = new Control[] { pictureAntennaTracker, pictureBoxAPM, pictureBoxHeli, pictureBoxSub,
                pictureBoxRover, pictureBoxOctaQuad, pictureBoxOcta, pictureBoxY6, pictureBoxTri, pictureBoxHexa,
                pictureBoxQuad }.ToDictionary(control => control, control => control.Text);
        }

        public void Activate()
        {
            var main = MainV2.instance;
            if (main != null)
            {
                main.DeviceChanged -= Instance_DeviceChanged;
                main.DeviceChanged += Instance_DeviceChanged;
            }
            UpdateFWList();

            if (Program.WindowsStoreApp)
            {
                //  CustomMessageBox.Show("Not Available", "Unfortunately the windows store version of this app does not support uploading.", MessageBoxButtons.OK);
                //  this.Enabled = false;
                //  return;
            }

            if (MainV2.DisplayConfiguration.isAdvancedMode)
            {
                lbl_devfw.Visible = true;
                lbl_Custom_firmware_label.Visible = true;
                lbl_dlfw.Visible = true;
                CMB_history_label.Visible = true;
            }
            else
            {
                lbl_devfw.Visible = false;
                lbl_Custom_firmware_label.Visible = false;
                lbl_dlfw.Visible = false;
                CMB_history_label.Visible = false;
            }
        }

        private void Instance_DeviceChanged(MainV2.WM_DEVICECHANGE_enum cause)
        {
            if (cause != MainV2.WM_DEVICECHANGE_enum.DBT_DEVICEARRIVAL)
                return;

            Parallel.ForEach(SerialPort.GetPortNames(), port =>
            //Task.Run(delegate
            {
                px4uploader.Uploader up;

                try
                {                        
                    // time to appear
                    Thread.Sleep(20);
                    up = new px4uploader.Uploader(port, 115200);
                }
                catch (Exception ex)
                {
                    //System.Threading.Thread.Sleep(50);
                    Console.WriteLine(ex.Message);
                    return;
                }

                try
                {
                    up.identify();
                    log.InfoFormat("Found board type {0} brdrev {1} blrev {2} fwmax {3} chip {5:X} chipdes {6} on {4}", up.board_type,
                        up.board_rev, up.bl_rev, up.fw_maxsize, port, up.chip, up.chip_desc);

                    detectedport = port;

                    up.close();
                }
                catch (Exception)
                {
                    Console.WriteLine("Not There..");
                    //Console.WriteLine(ex.Message);
                    up.close();
                }
            });
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            //CTRL+R moved to pictureBoxRover_Click
            //CTRL+O moved to CMB_history_label_Click
            //CTRL+C moved to Custom_firmware_label_Click

            // the shortcuts are the [KeyboardShortcut] methods below
            if (KeyboardShortcuts.Handle(this, ref msg, keyData))
                return true;

            return base.ProcessCmdKey(ref msg, keyData);
        }

        // the firmware keys go on to the focused control too
        [KeyboardShortcut(Keys.Control | Keys.Q, "List the development (latest) firmware")]
        private bool ShortcutDevFirmware()
        {
            CustomMessageBox.Show(Strings.TrunkWarning, Strings.Trunk);
            firmwareurl = "https://github.com/ArduPilot/binary/raw/master/dev/firmwarelatest.xml;https://firmware.ardupilot.org/Tools/MissionPlanner/dev/firmwarelatest.xml";

            UpdateFWList();
            CMB_history.Visible = false;
            return false;
        }

        [KeyboardShortcut(Keys.Control | Keys.P, "Install the PX4 firmware")]
        private bool ShortcutPx4Firmware()
        {
            if (!listReady)
                return false;
            var px4 = softwares.FirstOrDefault(a => a.name?.IndexOf("px4", StringComparison.OrdinalIgnoreCase) >= 0);
            if (px4 != null)
                findfirmware(px4);
            return false;
        }

        private void UpdateFWList()
        {
            listReady = false;
            foreach (var item in listControls)
            {
                item.Key.Enabled = false;
                item.Key.Tag = null;
                item.Key.Text = item.Value;
            }
            lbl_status.Text = Strings.GettingFWList;
            listRequest.Start(firmwareurl, fetchList, FirmwareListSettings.LoadInBackground, list =>
            {
                if (list == null || list.Count == 0)
                    throw new InvalidDataException("No firmware in the list.");
                foreach (var firmware in list.Where(item => item != null))
                {
                    try { updateDisplayName(firmware); }
                    catch (Exception error) { log.Error(error); }
                }
                softwares = list.Where(item => item != null).ToList();
                listReady = true;
                foreach (var control in listControls.Keys)
                    control.Enabled = control.Tag is Firmware.software;
                lbl_status.Text = Strings.ReceivedList;
            }, error =>
            {
                log.Error(error);
                lbl_status.Text = FirmwareListSettings.FailureText(error);
            });
        }

        private List<Firmware.software> ConvertToOld(List<APFirmware.FirmwareInfo> official)
        {
            var ans = new List<Firmware.software>();

            foreach (var mavtype in official.GroupBy(a => a.MavType))
            {

                var soft = new Firmware.software()
                {
                    url = "",
                    url2560 = "" + mavtype.Where(a => a.Platform.ToLower() == "apm1" || a.Platform.ToLower() == "apm1-quad")?.FirstOrDefault()?.Url?.ToString(),
                    url2560_2 = "" + mavtype.Where(a => a.Platform.ToLower() == "apm2" || a.Platform.ToLower() == "apm2-quad")?.FirstOrDefault()?.Url?.ToString(),
                    urlpx4v1 = "" + mavtype.Where(a => a.Platform.ToLower() == "px4-v1")?.FirstOrDefault()?.Url?.ToString(),
                    urlpx4rl = "",
                    urlpx4v2 = "" + mavtype.Where(a => a.Platform.ToLower() == "px4-v2")?.FirstOrDefault()?.Url?.ToString(),
                    urlpx4v3 = "" + mavtype.Where(a => a.Platform.ToLower() == "px4-v3")?.FirstOrDefault()?.Url?.ToString(),
                    urlpx4v4 = "" + mavtype.Where(a => a.Platform.ToLower() == "px4-v4")?.FirstOrDefault()?.Url?.ToString(),
                    urlpx4v4pro = "",
                    urlvrbrainv40 = "",
                    urlvrbrainv45 = "",
                    urlvrbrainv50 = "",
                    urlvrbrainv51 = "",
                    urlvrbrainv52 = "",
                    urlvrbrainv54 = "",
                    urlvrcorev10 = "",
                    urlvrubrainv51 = "",
                    urlvrubrainv52 = "",
                    urlbebop2 = "" + mavtype.Where(a => a.Platform.ToLower() == "bebop2")?.FirstOrDefault()?.Url?.ToString(),
                    urldisco = "" + mavtype.Where(a => a.Platform.ToLower() == "disco")?.FirstOrDefault()?.Url?.ToString(),


                    urlfmuv2 = "" + mavtype.Where(a => a.Platform.ToLower() == "fmuv2")?.FirstOrDefault()?.Url?.ToString(),
                    urlfmuv3 = "" + mavtype.Where(a => a.Platform.ToLower() == "fmuv3")?.FirstOrDefault()?.Url?.ToString(),
                    urlfmuv4 = "" + mavtype.Where(a => a.Platform.ToLower() == "fmuv4")?.FirstOrDefault()?.Url?.ToString(),
                    urlfmuv5 = "" + mavtype.Where(a => a.Platform.ToLower() == "fmuv5")?.FirstOrDefault()?.Url?.ToString(),
                    urlrevomini = "",
                    urlmindpxv2 = "" + mavtype.Where(a => a.Platform.ToLower() == "mindpx-v2")?.FirstOrDefault()?.Url?.ToString(),

                    name = mavtype.FirstOrDefault().VehicleType?.ToString() + " " + mavtype.FirstOrDefault().MavFirmwareVersion.ToString() + " " + mavtype.FirstOrDefault().MavFirmwareVersionType,
                    desc = mavtype.FirstOrDefault().VehicleType?.ToString() + " " + mavtype.FirstOrDefault().MavFirmwareVersionType,

                };
                ans.Add(soft);
            }

            return ans;
        }

        /// <summary>
        ///     for when updating fw to hardware
        /// </summary>
        /// <param name="progress"></param>
        /// <param name="status"></param>
        private void fw_Progress1(int progress, string status)
        {
            this.BeginInvokeIfRequired(() =>
            {
                var change = false;

                if (progress != -1)
                {
                    if (this.progress.Value != progress)
                    {
                        this.progress.Value = progress;
                        change = true;
                    }
                }

                if (lbl_status.Text != status)
                {
                    lbl_status.Text = status;
                    change = true;
                }

                if (change)
                    this.Refresh();
            });
        }

        private void updateDisplayName(Firmware.software temp)
        {
            if (temp.url2560.ToLower().Contains("AR2".ToLower()) ||
                temp.url2560.ToLower().Contains("apm1/APMRover".ToLower()))
            {
                pictureBoxRover.Text = temp.name;
                pictureBoxRover.Tag = temp;
            }
            else if (temp.url2560.ToLower().Contains("AP-".ToLower()) ||
                     temp.url2560.ToLower().Contains("apm1/ArduPlane".ToLower()))
            {
                pictureBoxAPM.Text = temp.name;
                pictureBoxAPM.Tag = temp;
            }
            else if (temp.url2560.ToLower().Contains("ac2-quad-".ToLower()) ||
                     temp.url2560.ToLower().Contains("1-quad/ArduCopter".ToLower()) ||
                     temp.name.ToLower().Contains("arducopter quad") ||
                     temp.desc.ToLower().Contains("arducopter quad")
                )
            {
                pictureBoxQuad.Text = temp.name + UiText.Translate(" Quad");
                pictureBoxQuad.Tag = temp;
            }
            else if (temp.url2560.ToLower().Contains("ac2-tri".ToLower()) ||
                     temp.url2560.ToLower().Contains("-tri/ArduCopter".ToLower()) ||
                     temp.name.ToLower().Contains("arducopter tri") ||
                     temp.desc.ToLower().Contains("arducopter tri"))
            {
                pictureBoxTri.Text = temp.name + UiText.Translate(" Tri");
                pictureBoxTri.Tag = temp;
            }
            else if (temp.url2560.ToLower().Contains("ac2-hexa".ToLower()) ||
                     temp.url2560.ToLower().Contains("-hexa/ArduCopter".ToLower()) ||
                     temp.name.ToLower().Contains("arducopter hexa") ||
                     temp.desc.ToLower().Contains("arducopter hexa"))
            {
                pictureBoxHexa.Text = temp.name + UiText.Translate(" Hexa");
                pictureBoxHexa.Tag = temp;
            }
            else if (temp.url2560.ToLower().Contains("ac2-y6".ToLower()) ||
                     temp.url2560.ToLower().Contains("-y6/ArduCopter".ToLower()) ||
                     temp.name.ToLower().Contains("arducopter y6") ||
                     temp.desc.ToLower().Contains("arducopter y6"))
            {
                pictureBoxY6.Text = temp.name + " Y6";
                pictureBoxY6.Tag = temp;
            }
            else if (temp.url2560.ToLower().Contains("ac2-heli-".ToLower()) ||
                     temp.url2560.ToLower().Contains("-heli/ArduCopter".ToLower()) ||
                     temp.name.ToLower().Contains("arducopter heli") ||
                     temp.desc.ToLower().Contains("arducopter heli") ||
                     temp.urlfmuv2.ToLower().Contains("-heli"))
            {
                pictureBoxHeli.Text = temp.name + UiText.Translate(" heli");
                pictureBoxHeli.Tag = temp;
            }
            else if (temp.url2560.ToLower().Contains("ac2-octaquad-".ToLower()) ||
                     temp.url2560.ToLower().Contains("-octa-quad/ArduCopter".ToLower()) ||
                     temp.name.ToLower().Contains("arducopter octa quad") ||
                     temp.desc.ToLower().Contains("arducopter octa quad"))
            {
                pictureBoxOctaQuad.Text = temp.name + UiText.Translate(" Octa Quad");
                pictureBoxOctaQuad.Tag = temp;
            }
            else if (temp.url2560.ToLower().Contains("ac2-octa-".ToLower()) ||
                     temp.url2560.ToLower().Contains("-octa/ArduCopter".ToLower()) ||
                     temp.name.ToLower().Contains("arducopter octa") ||
                     temp.desc.ToLower().Contains("arducopter octa"))
            {
                pictureBoxOcta.Text = temp.name + UiText.Translate(" Octa");
                pictureBoxOcta.Tag = temp;
            }
            else if (temp.url2560_2.ToLower().Contains("antennatracker") ||
                     temp.urlpx4v2.ToLower().Contains("antennatracker") ||
                     temp.urlfmuv3.ToLower().Contains("antennatracker"))
            {
                pictureAntennaTracker.Text = temp.name;
                pictureAntennaTracker.Tag = temp;
            }
            else if (temp.urlpx4v2.ToLower().Contains("ardusub") ||
                     temp.urlfmuv2.ToLower().Contains("ardusub"))
            {
                pictureBoxSub.Text = temp.name;
                pictureBoxSub.Tag = temp;
            }
            else if (temp.urlpx4v2.ToLower().Contains("copter") && !temp.urlpx4v2.ToLower().Contains("heli") ||
                     temp.urlfmuv3.ToLower().Contains("copter") && !temp.urlfmuv3.ToLower().Contains("heli"))
            {
                pictureBoxOcta.Text = temp.name + UiText.Translate(" Octa");
                pictureBoxOcta.Tag = temp;
                pictureBoxOctaQuad.Text = temp.name + UiText.Translate(" Octa Quad");
                pictureBoxOctaQuad.Tag = temp;
                pictureBoxHeli.Text = temp.name + UiText.Translate(" heli");
                pictureBoxHeli.Tag = temp;
                pictureBoxY6.Text = temp.name + " Y6";
                pictureBoxY6.Tag = temp;
                pictureBoxHexa.Text = temp.name + UiText.Translate(" Hexa");
                pictureBoxHexa.Tag = temp;
                pictureBoxTri.Text = temp.name + UiText.Translate(" Tri");
                pictureBoxTri.Tag = temp;
                pictureBoxQuad.Text = temp.name + UiText.Translate(" Quad");
                pictureBoxQuad.Tag = temp;
            }
            else if (temp.urlpx4v2.ToLower().Contains("rover") ||
                     temp.urlfmuv2.ToLower().Contains("rover"))
            {
                pictureBoxRover.Text = temp.name;
                pictureBoxRover.Tag = temp;
            }
            else if (temp.urlpx4v2.ToLower().Contains("plane") ||
                     temp.urlfmuv2.ToLower().Contains("plane"))
            {
                pictureBoxAPM.Text = temp.name;
                pictureBoxAPM.Tag = temp;
            }
            else
            {
                log.Info("No Home " + temp.name + " " + temp.url2560);
            }
        }

        public static Func<List<ArduPilot.DeviceInfo>> ExtraDeviceInfo;

        private void findfirmware(Firmware.software fwtoupload)
        {
            var dr = CustomMessageBox.Show(Strings.AreYouSureYouWantToUpload + fwtoupload.name + Strings.QuestionMark,
                Strings.Continue, MessageBoxButtons.YesNo);
            if (dr == (int)DialogResult.Yes)
            {
                try
                {
                    MainV2.comPort.BaseStream.Close();
                }
                catch
                {
                }
                fw.Progress += fw_Progress1;

                var history = (CMB_history.SelectedValue == null) ? "" : CMB_history.SelectedValue.ToString();

                if (history != "")
                {
                    foreach (var propertyInfo in fwtoupload.GetType().GetFields())
                    {
                        try
                        {
                            if (propertyInfo.Name.Contains("url"))
                            {
                                var oldurl = propertyInfo.GetValue(fwtoupload).ToString();
                                if (oldurl == "")
                                    continue;
                                var newurl = Firmware.getUrl(history, oldurl);
                                propertyInfo.SetValue(fwtoupload, newurl);
                            }
                        }
                        catch { }
                    }

                    //history = "";
                }

                var ports = Win32DeviceMgmt.GetAllCOMPorts();
                ports.AddRange(Linux.GetAllCOMPorts());

                if (ExtraDeviceInfo != null)
                {
                    try
                    {
                        ports.AddRange(ExtraDeviceInfo.Invoke());
                    }
                    catch
                    {

                    }
                }

                var updated = fw.updateLegacy(MainV2.comPortName, fwtoupload, history, ports);

                if (updated)
                {
                    if (fwtoupload.url2560_2 != null && fwtoupload.url2560_2.ToLower().Contains("copter") &&
                        fwtoupload.name.ToLower().Contains("3.1"))
                        CustomMessageBox.Show(Strings.WarningAC31, Strings.Warning);

                    if (fwtoupload.url2560_2 != null && fwtoupload.url2560_2.ToLower().Contains("copter") &&
                        fwtoupload.name.ToLower().Contains("3.2"))
                        CustomMessageBox.Show(Strings.WarningAC32, Strings.Warning);
                }
                else
                {
                    CustomMessageBox.Show(Strings.ErrorUploadingFirmware, Strings.ERROR);
                }
            }
        }

        private void pictureBoxFW_Click(object sender, EventArgs e)
        {
            if (listReady && (sender as Control)?.Tag is Firmware.software firmware)
                findfirmware(firmware);
        }

        private void up_LogEvent(string message, int level = 0)
        {
            lbl_status.Text = message;
            Application.DoEvents();
        }

        private void up_ProgressEvent(double completed)
        {
            progress.Value = (int)completed;
            Application.DoEvents();
        }

        private void port_Progress(int progress, string status)
        {
            log.InfoFormat("Progress {0} ", progress);
            this.progress.Value = progress;
            this.progress.Refresh();
        }

        private void CMB_history_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (CMB_history.SelectedValue == null)
                return;
            firmwareurl = Firmware.getUrl(CMB_history.SelectedValue.ToString(), "");
            UpdateFWList();
        }

        //Show list of previous firmware versions (old CTRL+O shortcut)
        private void CMB_history_label_Click(object sender, EventArgs e)
        {
            CMB_history.Enabled = false;

            //CMB_history.Items.Clear();
            //CMB_history.Items.AddRange(fw.gholdurls);
            //CMB_history.Items.AddRange(fw.gcoldurls);
            CMB_history.DisplayMember = "Value";
            CMB_history.ValueMember = "Key";
            CMB_history.DataSource = Firmware.niceNames;

            CMB_history.Enabled = true;
            CMB_history.Visible = true;
            CMB_history_label.Visible = false;
        }

        //Load custom firmware (old CTRL+C shortcut)
        private void Custom_firmware_label_Click(object sender, EventArgs e)
        {
            using (var fd = new OpenFileDialog
                {Filter = "Firmware (*.hex;*.px4;*.vrx;*.apj)|*.hex;*.px4;*.vrx;*.apj|All files (*.*)|*.*"})
            {
                if (Directory.Exists(custom_fw_dir))
                    fd.InitialDirectory = custom_fw_dir;
                fd.ShowDialog();
                if (File.Exists(fd.FileName))
                {
                    custom_fw_dir = Path.GetDirectoryName(fd.FileName);
                    Settings.Instance["FirmwareFileDirectory"] = custom_fw_dir;

                    fw.Progress += fw_Progress1;

                    var boardtype = BoardDetect.boards.none;
                    try
                    {
                        if (fd.FileName.ToLower().EndsWith(".px4") || fd.FileName.ToLower().EndsWith(".apj"))
                        {
                            if (solo.Solo.is_solo_alive &&
                                CustomMessageBox.Show("Solo", UiText.Translate("Is this a Solo?"),
                                    CustomMessageBox.MessageBoxButtons.YesNo) == CustomMessageBox.DialogResult.Yes)
                            {
                                boardtype = BoardDetect.boards.solo;
                            }
                            else
                            {
                                boardtype = BoardDetect.boards.px4v3;
                            }
                        }
                        else
                        {
                            var ports = Win32DeviceMgmt.GetAllCOMPorts();
                            ports.AddRange(Linux.GetAllCOMPorts());

                            if (ExtraDeviceInfo != null)
                            {
                                try
                                {
                                    ports.AddRange(ExtraDeviceInfo.Invoke());
                                }
                                catch
                                {

                                }
                            }

                            boardtype = BoardDetect.DetectBoard(MainV2.comPortName, ports);
                        }

                        if (boardtype == BoardDetect.boards.none)
                        {
                            CustomMessageBox.Show(Strings.CantDetectBoardVersion);
                            return;
                        }
                    }
                    catch
                    {
                        CustomMessageBox.Show(Strings.CanNotConnectToComPortAnd, Strings.ERROR);
                        return;
                    }

                    try
                    {
                        fw.UploadFlash(MainV2.comPortName, fd.FileName, boardtype);
                    }
                    catch (Exception ex)
                    {
                        CustomMessageBox.Show(ex.ToString(), Strings.ERROR);
                    }
                }
            }
        }

        private void lbl_devfw_Click(object sender, EventArgs e)
        {
            CustomMessageBox.Show(Strings.BetaWarning, Strings.Beta);
            firmwareurl = "https://github.com/ArduPilot/binary/raw/master/dev/firmware2.xml;https://firmware.ardupilot.org/Tools/MissionPlanner/dev/firmware2.xml";
            UpdateFWList();
            CMB_history.Visible = false;
        }


        private void lbl_dlfw_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://firmware.ardupilot.org/") { UseShellExecute = true });
            }
            catch
            {
                CustomMessageBox.Show(UiText.Translate("Can not open url https://firmware.ardupilot.org/"), Strings.ERROR);
            }
        }

        private void lbl_px4bl_Click(object sender, EventArgs e)
        {
            try
            {
                MainV2.comPort.Open(false);

                if (MainV2.comPort.BaseStream.IsOpen)
                {
                    MainV2.comPort.doReboot(true, false);
                    CustomMessageBox.Show(UiText.Translate("Please ignore the unplug and plug back in when uploading flight firmware."));
                }
                else
                {
                    throw new Exception();
                }
            }
            catch
            {
                CustomMessageBox.Show(UiText.Translate("Failed to connect and send the reboot command"), Strings.ERROR);
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(@"http://copter.ardupilot.com/wiki/connect-escs-and-motors/#motor_order_diagrams") { UseShellExecute = true });
            }
            catch
            {
                CustomMessageBox.Show("http://copter.ardupilot.com/wiki/connect-escs-and-motors/#motor_order_diagrams", Strings.ERROR);
            }
        }

        private void picturebox_ph2_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(@"http://www.proficnc.com/?utm_source=missionplanner&utm_medium=click&utm_campaign=mission") { UseShellExecute = true });
            }
            catch
            {
                CustomMessageBox.Show("http://www.proficnc.com/?utm_source=missionplanner&utm_medium=click&utm_campaign=mission", Strings.ERROR);
            }
        }

        public void Deactivate()
        {
            listRequest.Invalidate();
            listReady = false;
            var main = MainV2.instance;
            if (main != null)
                main.DeviceChanged -= Instance_DeviceChanged;

            // try reboot device on screen close.
            if (!String.IsNullOrEmpty(detectedport))
            {
            }
        }
    }
}