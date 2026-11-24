using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MissionPlanner.Utilities;
using MissionPlanner.Controls;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using MissionPlanner;
using System.Drawing;

// this example taken from https://discuss.ardupilot.org/t/adding-mission-parts-at-the-beginning-and-end-of-new-mission/56579/12
// modified for this sample

namespace Shortcuts
{
    public class Plugin : MissionPlanner.Plugin.Plugin
    {
        ToolStripMenuItem but;
		MissionPlanner.Controls.MyDataGridView commands;

        public override string Name
        {
            get { return "Small stuff"; }
        }

        public override string Version
        {
            get { return "0.10"; }
        }

        public override string Author
        {
            get { return "EOSBandi"; }
        }

        public override bool Init()
        {
            return true;
        }

        private static string ExampleText(string original)
        {
            if (System.Globalization.CultureInfo.CurrentUICulture.Name != "zh-CN")
                return original;
            string key = "Flight Planner sample menu: " + original;
            string text = UiText.Translate(key);
            return text == key ? original : text;
        }

        public override bool Loaded()
        {
            but = new ToolStripMenuItem(ExampleText("Fix mission top/bottom"));
            but.Click += but_Click;
            ToolStripItemCollection col = Host.FPMenuMap.Items;
            col.Add(but);
            commands =
                Host.MainForm.FlightPlanner.Controls.Find("Commands", true).FirstOrDefault() as
                    MissionPlanner.Controls.MyDataGridView;
            return true;
        }

        public override bool Loop()
        {
            return true;
        }

        public override bool Exit()
        {
            return true;
        }

        void but_Click(object sender, EventArgs e)
        {
            CustomMessageBox.Show(ExampleText("This is a sample plugin\nSee the source in the plugins folder"));

            string angle = "0";
            if (InputBox.Show(ExampleText("Enter Angle"), ExampleText("This will be the heading"), ref angle) != DialogResult.OK)
                return;

            int angle_in_number;
            if (!Int32.TryParse(angle, out angle_in_number))
            {
                CustomMessageBox.Show(ExampleText("Enter a valid integer."));
                return;
            }

            // The existing sequence inserts into and removes from the planner's unbound command grid.
            if (commands == null || commands.IsDisposed || commands.VirtualMode ||
                commands.DataSource != null || commands.ColumnCount == 0)
            {
                CustomMessageBox.Show(ExampleText("The mission command list is not ready."));
                return;
            }

			Host.InsertWP(0, MAVLink.MAV_CMD.DO_SET_SERVO, 9, angle_in_number, 0, 0, 0, 0, 0);
            Host.InsertWP(1, MAVLink.MAV_CMD.DO_SET_SERVO, 10, 1000, 0, 0, 0, 0, 0);
 
			Host.AddWPtoList(MAVLink.MAV_CMD.DO_SET_SERVO, 9, 1000, 0, 0, 0, 0, 0);
            Host.AddWPtoList(MAVLink.MAV_CMD.DO_SET_SERVO, 10, 1000, 0, 0, 0, 0, 0);

            commands.Rows.RemoveAt(1);
        }
    }
}