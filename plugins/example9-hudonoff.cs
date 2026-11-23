using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MissionPlanner.Utilities;
using MissionPlanner.Controls;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using System.Globalization;
using MissionPlanner;
using System.Drawing;
using GMap.NET.WindowsForms;
using MissionPlanner.GCSViews;
using MissionPlanner.Maps;


namespace hudonoff
{
    public class Plugin : MissionPlanner.Plugin.Plugin
    {

        public override string Name
        {
            get { return "HUD"; }
        }

        public override string Version
        {
            get { return "0.2"; }
        }

        public override string Author
        {
            get { return "Michael Oborne"; }
        }

        public override bool Init()
        {
            return true;
        }

        private string configname = "HudItems";

        // Use the saved English values only as lookup keys; never persist these display labels.
        private static string MenuLabel(string english)
        {
            if (!string.Equals(CultureInfo.CurrentUICulture.Name, "zh-CN", StringComparison.OrdinalIgnoreCase))
                return english;

            switch (english)
            {
                case "HUD Items": return "显示项目";
                case "Heading": return "航向";
                case "Speed": return "速度";
                case "WP Distance": return "航点距离";
                case "Wind": return "风况";
                case "Alt": return "高度";
                case "Connection": return "连接状态";
                case "X-Track": return "航迹偏差";
                case "Roll/Pitch": return "横滚／俯仰";
                case "GPS": return "GPS 状态";
                case "Battery": return "电池";
                case "Battery2": return "第二电池";
                case "EKF": return "EKF 状态";
                case "Vibe": return "振动";
                case "Prearm Status": return "解锁前检查状态";
                case "AOA": return "迎角／侧滑角";
                default: return english;
            }
        }

        public override bool Loaded()
        {
            var rootbut = new ToolStripMenuItem(MenuLabel("HUD Items"));
            ToolStripItemCollection col = Host.FDMenuHud.Items;
            col.Add(rootbut);

            var items = new Dictionary<string,string>()
            {
                {"displayheading", "Heading"},
                {"displayspeed", "Speed"},
                {"displaywp", "WP Distance"},
                {"displaywind", "Wind"},
                {"displayalt", "Alt"},
                {"displayconninfo", "Connection"},
                {"displayxtrack", "X-Track"},
                {"displayrollpitch", "Roll/Pitch"},
                {"displaygps", "GPS"},
                {"batteryon", "Battery"},
                {"batteryon2", "Battery2"},
                {"displayekf", "EKF"},
                {"displayvibe", "Vibe"},
                {"displayprearm", "Prearm Status"},
                {"displayAOASSA", "AOA"},
            };

            var hide = Settings.Instance.GetList(configname);

            foreach (var item in items)
            {
                var but = new ToolStripMenuItem(MenuLabel(item.Value));
                but.CheckOnClick = true;
                but.Checked = true;
                but.Click += (s,e)=>
                {
                    FlightData.myhud.GetType().GetProperty(item.Key).SetValue(FlightData.myhud, but.Checked);
                    if (!but.Checked)
                        Settings.Instance.AppendList(configname, item.Value);
                    if (but.Checked)
                        Settings.Instance.RemoveList(configname, item.Value);
                };
                if (hide.Contains(item.Value))
                {
                    but.Checked = false;
                    FlightData.myhud.GetType().GetProperty(item.Key).SetValue(FlightData.myhud, but.Checked);
                }
                rootbut.DropDownItems.Add(but);
            }

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
    }
}