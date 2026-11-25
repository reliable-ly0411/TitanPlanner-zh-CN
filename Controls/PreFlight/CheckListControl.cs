using MissionPlanner.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace MissionPlanner.Controls.PreFlight
{
    public partial class CheckListControl : UserControl
    {
        public List<CheckListItem> CheckListItems = new List<CheckListItem>();

        public string configfile = Settings.GetUserDataDirectory() + "checklist.xml";

        public string configfiledefault = Settings.GetRunningDirectory() + "checklistDefault.xml";

        int rowcount = 0;

        // Change this identity together with the translations if the bundled XML changes.
        private const string StockChecklistSha256 =
            "7943DB015A7754C6C55870645750222EA5D5CBF62FC99D0047E6A48827495DE4";
        private readonly Dictionary<CheckListItem, KeyValuePair<string, string>> stockDescriptions =
            new Dictionary<CheckListItem, KeyValuePair<string, string>>();

        private static readonly Dictionary<string, string> StockDescriptionTranslations =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "Veryify GPS", "检查 GPS" },
                { "Gps Sat Count", "GPS 卫星数量" },
                { "Telemetry Signal", "遥测信号" },
                { "Battery Level", "电池电量" },
                { "Mode", "飞行模式" },
                { "Check Altitude", "检查高度" },
                { "Tail and wings secured?", "机尾和机翼是否固定？" },
                { "All servos respond to input?", "所有舵机是否响应操控？" },
                { "All servos respond to pitch and roll?", "所有舵机是否响应俯仰和横滚？" },
                { "Center of gravity at indicated point?", "重心是否位于标示位置？" },
                { "Servo linkages are secure?", "舵机连杆是否牢固？" },
                { "Camera is on and ready to fly?", "相机是否开启并准备就绪？" }
            };

        internal string GetChecklistDescription(CheckListItem item)
        {
            if (item == null)
                return null;
            if (string.Equals(CultureInfo.CurrentUICulture.Name, "zh-CN", StringComparison.Ordinal)
                && stockDescriptions.TryGetValue(item, out var entry)
                && string.Equals(item.Description, entry.Key, StringComparison.Ordinal))
                return entry.Value;
            return item.Description;
        }

        internal void RemoveStockDescription(CheckListItem item)
        {
            if (item != null)
                stockDescriptions.Remove(item);
        }

        private static bool IsVerifiedStockChecklist(string xml)
        {
            using (var hash = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(xml.Replace("\r\n", "\n"));
                return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "") == StockChecklistSha256;
            }
        }

        //Controls and the lists for the controls
        private GroupBox gb = new GroupBox();
        private Label desc = new Label();
        private Label text = new Label();
        private CheckBox tickbox = new CheckBox();
        private List<GroupBox> groupboxes = new List<GroupBox>();
        private List<Label> descLabels = new List<Label>();
        private List<Label> labels = new List<Label>();
        private List<CheckBox> checkboxes = new List<CheckBox>();

        internal struct internaldata
        {
            internal Label desc;
            internal Label text;
            internal CheckBox tickbox;
            internal CheckListItem CLItem;
        }


        public CheckListControl()
        {
            InitializeComponent();

            try
            {
                MissionPlanner.Controls.PreFlight.CheckListItem.defaultsrc = MainV2.comPort.MAV.cs;

                LoadConfig();
            }
            catch
            {
                Console.WriteLine("Failed to read CheckList config file " + configfile);
            }

            timer1.Start();
        }

        public void Draw()
        {
            lock (this.CheckListItems)
            {
                if (rowcount == this.CheckListItems.Count)
                    return;

                panel1.Visible = false;
                panel1.Controls.Clear();

                int y = 0;

                rowcount = 0;
                groupboxes.Clear();
                descLabels.Clear();
                labels.Clear();
                checkboxes.Clear();
                foreach (var item in this.CheckListItems)
                {
                    var wrnctl = addwarningcontrol(5, y, item);

                    rowcount++;

                    y = wrnctl.Bottom;
                }
            }
            panel1.Visible = true;
        }

        void UpdateDisplay()
        {
            foreach (Control itemp in panel1.Controls)
            {
                foreach (Control item in itemp.Controls)
                {
                    if (item.Tag == null)
                        continue;

                    internaldata data = (internaldata)item.Tag;

                    if (item.Name.StartsWith("utext"))
                    {
                        item.Text = data.CLItem.DisplayText();
                        data.desc.Text = GetChecklistDescription(data.CLItem);
                    }
                    if (item.Name.StartsWith("utickbox"))
                    {
                        var tickbox = item as CheckBox;
                        if (data.CLItem.ConditionType != CheckListItem.Conditional.NONE)
                            tickbox.Checked = data.CLItem.checkCond(data.CLItem);

                        if (tickbox.Checked)
                        {
                            data.text.ForeColor = data.CLItem._TrueColor;
                            data.desc.ForeColor = data.CLItem._TrueColor;
                        }
                        else
                        {
                            data.text.ForeColor = data.CLItem._FalseColor;
                            data.desc.ForeColor = data.CLItem._FalseColor;
                        }
                    }
                }
            }
        }

        Control addwarningcontrol(int x, int y, CheckListItem item, bool hideforchild = false)
        {
            var desctext = GetChecklistDescription(item);
            var texttext = item.DisplayText();

            var height = TextRenderer.MeasureText(desctext, this.Font).Height;

            var x0 = (int)(panel1.Width * 0.94);
            var x1 = (int)(x0 * 0.66);
            var x2 = (int)(x0 * 0.26);
            var x3 = (int)(x0 * 0.08);

            gb = new GroupBox() { Text = "", Location = new Point(x, y), Size = new Size(x0, 17 + height), Name = "gb" + y };

            desc = new Label() { Text = desctext, Location = new Point(5, 9), Size = new Size(x1, height), Name = "udesc" + y };
            text = new Label() { Text = texttext, Location = new Point(desc.Right, 9), Size = new Size(x2, height), Name = "utext" + y };
            tickbox = new CheckBox() { Checked = item.checkCond(item), Location = new Point((text.Right), 7), Size = new Size(21, 21), Name = "utickbox" + y };

            desc.Tag = text.Tag = tickbox.Tag = new internaldata { CLItem = item, desc = desc, text = text, tickbox = tickbox };

            //Changing the font size of the desc labels text according to amount of characters contained in the label
            desc.TextAlign = ContentAlignment.MiddleLeft;
            if (desc.Text.ToCharArray().Length > 35)
            {
                if (desc.Text.ToCharArray().Length > 35 && desc.Text.ToCharArray().Length < 40)
                {
                    desc.Font = new Font(desc.Font.FontFamily, desc.Font.Size - 0.7f, desc.Font.Style);
                }
                else if (desc.Text.ToCharArray().Length >= 40)
                {
                    desc.Font = new Font(desc.Font.FontFamily, desc.Font.Size - 1.35f, desc.Font.Style);
                }
            }
            else if (desc.Text.ToCharArray().Length <= 35)
            {
                desc.Font = new Font(desc.Font.FontFamily, desc.Font.Size - 0.4f, desc.Font.Style);
            }

            //Add the controls to the main control
            gb.Controls.Add(desc);
            gb.Controls.Add(text);
            gb.Controls.Add(tickbox);

            panel1.Controls.Add(gb);

            //Add controls to relevant control lists
            groupboxes.Add(gb);
            descLabels.Add(desc);
            labels.Add(text);
            checkboxes.Add(tickbox);

            y = gb.Bottom;

            if (item.Child != null)
            {
                //return addwarningcontrol(x += 5, y, item.Child, true);
            }

            return gb;
        }

        public void LoadConfig()
        {
            stockDescriptions.Clear();
            bool bundledFallback = false;
            string loadfile = configfile;

            if (!File.Exists(configfile))
            {
                if (!File.Exists(configfiledefault))
                {
                    return;
                }
                else
                {
                    loadfile = configfiledefault;
                    bundledFallback = true;
                }
            }

            System.Xml.Serialization.XmlSerializer reader =
                new System.Xml.Serialization.XmlSerializer(typeof(List<CheckListItem>),
                    new Type[] { typeof(CheckListItem) });

            using (StreamReader sr = new StreamReader(loadfile))
            {
                if (!bundledFallback)
                {
                    CheckListItems = (List<CheckListItem>)reader.Deserialize(sr);
                    return;
                }

                // Parse and identify the same snapshot. Never infer an identity from user XML.
                string xml = sr.ReadToEnd();
                using (var snapshot = new StringReader(xml))
                    CheckListItems = (List<CheckListItem>)reader.Deserialize(snapshot);
                if (IsVerifiedStockChecklist(xml))
                    foreach (var item in CheckListItems)
                        if (item.Description != null &&
                            StockDescriptionTranslations.TryGetValue(item.Description, out var translation))
                            stockDescriptions[item] = new KeyValuePair<string, string>(item.Description, translation);
            }
        }

        public void SaveConfig()
        {
            // save config
            System.Xml.Serialization.XmlSerializer writer =
                new System.Xml.Serialization.XmlSerializer(typeof(List<CheckListItem>),
                    new Type[] { typeof(CheckListItem), typeof(Color) });

            using (StreamWriter sw = new StreamWriter(configfile))
            {
                lock (CheckListItems)
                {
                    writer.Serialize(sw, CheckListItems);
                }
            }
        }

        private void BUT_edit_Click(object sender, EventArgs e)
        {
            CheckListEditor form = new CheckListEditor(this);
            form.Show();
            lock (this.CheckListItems)
                rowcount = 0;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Draw();
            UpdateDisplay();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (!MainV2.DisplayConfiguration.displayPreFlightTabEdit)
            {
                BUT_edit.Visible = false;
            }

            if (Visible)
            {
                timer1.Enabled = true;
            }
            else
            {
                timer1.Enabled = false;
            }
        }

        public void Controls_Resize(object sender, EventArgs e)
        {
            // Resize can run before the first Draw, or with an empty checklist.
            if (groupboxes.Count == 0 || descLabels.Count == 0 || labels.Count == 0 || checkboxes.Count == 0)
                return;

            //initialize controls x and y
            int gbsX = 0;
            int gbsY = 0;
            int lblsOneX = 0;
            int lblsOneY = 0;
            int lblsTwoX = 0;
            int lblsTwoY = 0;
            int cbxsX = 0;
            int cbxsY = 0;

            //Width of the controls
            //Panel1 variable width
            var panelOneWidth = panel1.Width;
            //Group boxes variable width
            var gbsWidth = groupboxes[0].Width = (int)(panelOneWidth * 0.9);
            //Desc label variable width
            var descLabelWidth = descLabels[0].Width = (int)(gbsWidth * 0.6204);
            //Second label variable width
            var labeTwolWidth = labels[0].Width = (int)(gbsWidth * 0.2444);
            //Checkbox variable width
            var checkboxWidth = checkboxes[0].Width = (int)(gbsWidth * 0.0752);

            //Setting Locations
            //Set the first groupboxes location
            groupboxes[0].Location = new Point(groupboxes[0].Location.X, groupboxes[0].Location.Y);
            gbsX = groupboxes[0].Location.X;
            gbsY = groupboxes[0].Location.Y;

            //location of the desc Label
            descLabels[0].Location = new Point(descLabels[0].Location.X, descLabels[0].Location.Y);
            lblsOneX = descLabels[0].Location.X;
            lblsOneY = descLabels[0].Location.Y;
            //set the second labels location
            var labelTwo = labels[0].Location = new Point(labels[0].Location.X, labels[0].Location.Y);
            lblsTwoX = descLabels[0].Width + (int)(gbsWidth * 0.02);
            lblsTwoY = labels[0].Location.Y;
            //set the first checkboxes location
            var checkboxOne = checkboxes[0].Location = new Point(checkboxes[0].Location.X, checkboxes[0].Location.Y);
            cbxsX = labels[0].Location.X + labels[0].Width + (int)(gbsWidth * 0.02);
            cbxsY = checkboxes[0].Location.Y;

            for (int i = 0; i < groupboxes.Count; i++)
            {
                if (groupboxes.Count > 0 && labels.Count > 0 && checkboxes.Count > 0)
                {
                    //Set the group box Y Location
                    gbsY = groupboxes[i].Bottom;
                    //Set the width of the desc labels
                    descLabelWidth = descLabels[i].Width;

                    if (i == 0)
                    {
                        //check the height of the panel1 = not to do with the change
                        var panelOneHeight = panel1.Height;

                        //ratio needs to be rechecked
                        if (this.panel1.Width > 0)
                        {
                            //set the width of the group boxes
                            groupboxes[i].Width = (int)(panelOneWidth * 0.9);

                            //desc Label X Location
                            lblsOneX = descLabels[i].Location.X;

                            //Second label X Location
                            lblsTwoX = labelTwo.X = (int)(groupboxes[i].Width * 0.66);

                            labels[i].Width = (int)(groupboxes[i].Width * 0.3);

                            //checkbox location
                            cbxsX = checkboxOne.X = (int)(groupboxes[i].Width * 0.9);
                        }
                    }
                    else if (i > 0)
                    {
                        //setting the groupboxes width
                        groupboxes[i].Width = (int)(panelOneWidth * 0.9);
                    }

                    //set the location in the group box for labels
                    descLabels[i].Location = new Point(lblsOneX, lblsOneY);
                    //set the location in the group box for the second labels
                    labels[i].Location = new Point(lblsTwoX, lblsTwoY);
                    //set the location in the group box for the checkboxes
                    checkboxes[i].Location = new Point(cbxsX, cbxsY);

                    //Bring controls to the front
                    groupboxes[i].BringToFront();
                    labels[i].BringToFront();
                    checkboxes[i].BringToFront();
                }
            }
        }

        private void CheckListControl_Load(object sender, EventArgs e)
        {
            this.Resize += new EventHandler(Controls_Resize);
        }
    }
}
