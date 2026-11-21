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
using GMap.NET.WindowsForms;
using MissionPlanner.GCSViews;
using MissionPlanner.Maps;
using MissionPlanner.Comms;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;

namespace mass
{
    public class Pluginmass : MissionPlanner.Plugin.Plugin
    {
        public override string Name
        {
            get { return "mass Control"; }
        }

        public override string Version
        {
            get { return "0.10"; }
        }

        public override string Author
        {
            get { return "Michael Oborne"; }
        }

        public override bool Init()
        {
            return true;
        }

        private static string MapMenuText(string original)
        {
            if (System.Globalization.CultureInfo.CurrentUICulture.Name != "zh-CN")
                return original;
            string key = "Flight Data mass menu: " + original;
            string text = UiText.Translate(key);
            return text == key ? original : text;
        }

        public override bool Loaded()
        {
            var rootbut = new ToolStripMenuItem(MissionPlanner.Utilities.UiText.Translate("Mass"));
            //rootbut.Click += but_Click;
            ToolStripItemCollection col = Host.FDMenuMap.Items;
            col.Add(rootbut);

            var but = new ToolStripMenuItem(MapMenuText("Arm"));
            but.Click += (s, e) => {
                Parallel.ForEach(MainV2.Comports, mav =>
                {
                    try
                    {
                        mav.doARM(true);
                    }
                    catch { }
                });
            };
            rootbut.DropDownItems.Add(but);

            but = new ToolStripMenuItem(MapMenuText("Arm(Force)"));
            but.Click += (s, e) => {
                Parallel.ForEach(MainV2.Comports, mav => {
                    try
                    {
                        mav.doARM(true, true);
                    } catch { }
                });
            };
            rootbut.DropDownItems.Add(but);

            but = new ToolStripMenuItem(MapMenuText("GUIDED"));
            but.Click += (s, e) => {
                Parallel.ForEach(MainV2.Comports, mav => {
                    try
                    {
                        mav.setMode("GUIDED");
                    }
                    catch { }
                });
            };
            rootbut.DropDownItems.Add(but);

            but = new ToolStripMenuItem(MapMenuText("AUTO"));
            but.Click += (s, e) => {
                Parallel.ForEach(MainV2.Comports, mav => {
                    try
                    {
                        mav.setMode("AUTO");
                    }
                    catch { }
                });
            };
            rootbut.DropDownItems.Add(but);

            but = new ToolStripMenuItem(MapMenuText("TAKEOFF"));
            but.Click += (s, e) => {
                Parallel.ForEach(MainV2.Comports, mav => {
                    try
                    {
                        mav.setMode("TAKEOFF");
                    }
                    catch { }
                });
            };
            rootbut.DropDownItems.Add(but);

            but = new ToolStripMenuItem(MapMenuText("TakeOff (2m)"));
            but.Click += (s, e) => {
                Parallel.ForEach(MainV2.Comports, mav => {
                    try
                    {
                        mav.doCommand(mav.sysidcurrent, (byte)mav.compidcurrent,
                            MAVLink.MAV_CMD.TAKEOFF, 0, 0, 0, 0, 0, 0, 2);
                    }
                    catch { }
                });
            };
            rootbut.DropDownItems.Add(but);

            but = new ToolStripMenuItem(MapMenuText("Fly To"));
            but.Click += (s, e) => {
                Parallel.ForEach(MainV2.Comports, mav => {
                    try
                    {
                        mav.setGuidedModeWP(new Locationwp
                        {
                            alt = 50,
                            lat = Host.FDMenuMapPosition.Lat / 1e7,
                            lng = Host.FDMenuMapPosition.Lng / 1e7,
                            frame = (byte)MAVLink.MAV_FRAME.GLOBAL_RELATIVE_ALT
                        });
                    }
                    catch { }
                });
            };
            rootbut.DropDownItems.Add(but);


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