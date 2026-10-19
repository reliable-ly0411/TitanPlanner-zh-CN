using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using MissionPlanner.Controls;
using MissionPlanner.Controls.PreFlight;
using System.Windows.Forms;
using System.Xml.Serialization;
using MissionPlanner;
using MissionPlanner.Utilities;

// Run in the extracted package, with an isolated profile and an X11 test display.
class LinuxSmokeTest
{
    private static Timer timer;
    // This is a startup test in a disposable process. Mono 6.8 can hang while
    // aborting background plugin/network threads during Environment.Exit.
    [DllImport("libc", EntryPoint = "_exit")]
    private static extern void Exit(int status);

    private static void Finish(int status)
    {
        Console.Out.Flush();
        Console.Error.Flush();
        Exit(status);
    }

    private static void CheckChecklistResize()
    {
        using (var checklist = new CheckListControl())
        {
            // A resize can arrive before the timer has drawn any rows.
            checklist.Controls_Resize(checklist, EventArgs.Empty);
            checklist.CheckListItems.Clear();
            checklist.CheckListItems.Add(new CheckListItem { Description = "Test item", Text = "Test" });
            checklist.Draw();
            checklist.Controls_Resize(checklist, EventArgs.Empty);
            checklist.CheckListItems.Clear();
            checklist.Draw();
            checklist.Controls_Resize(checklist, EventArgs.Empty);
        }
    }

    [STAThread]
    static void Main()
    {
        Settings.CustomUserDataDirectory = Path.Combine(Path.GetTempPath(), "missionplanner-smoke-" + Guid.NewGuid());
        Directory.CreateDirectory(Settings.GetUserDataDirectory());
        Settings.Instance["update_check"] = DateTime.Now.ToShortDateString();
        Settings.Instance["MainWidth"] = "1200";
        Settings.Instance["MainHeight"] = "900";
        Settings.Instance["AutoConnect"] = "[]";
        Settings.Instance["language"] = "zh-CN";
        // Select a map explicitly in this disposable profile so the Chinese
        // first-run map preference notice does not block the startup test.
        Settings.Instance["MapType"] = "GoogleSatelliteMap";
        Settings.Instance.Save();
        timer = new Timer { Interval = 10000 };
        timer.Tick += (sender, args) => {
            timer.Stop();
            try
            {
                var flightData = MainV2.instance.FlightData;
                var tabs = (ThemedTabStrip)flightData.Controls.Find("themedTabStrip", true).Single();
                var actionTab = tabs.TabPages.Single(tab => tab.Name == "tabActions");
                tabs.SelectedIndex = tabs.TabPages.ToList().IndexOf(actionTab);
                var actions = (FlightDataActions)flightData.Controls.Find("flightDataActions1", true).Single();
                var combo = actions.CMB_action;
                if (!MainV2.instance.Visible || !actions.Visible || tabs.SelectedTab != actionTab || combo.Items.Count == 0)
                    throw new Exception("FlightData did not load its actions.");
                var table = (TableLayoutPanel)actions.Controls.Find("tableSetpoints", true).Single();
                var modifiers = table.Controls.OfType<MissionPlanner.Controls.ModifyandSet>().ToArray();
                if (modifiers.Length != 3)
                    throw new Exception("Missing numeric action controls.");
                foreach (var modify in modifiers)
                {
                    var button = modify.Button;
                    var number = modify.NumericUpDown;
                    // Titan uses different control heights, centered in one row.
                    // Require alignment, no overlap, and complete visible bounds.
                    if (Math.Abs((button.Top * 2 + button.Height) - (number.Top * 2 + number.Height)) > 2 ||
                        button.Width <= 0 || number.Width <= 0 || number.Right > button.Left ||
                        button.Left < 0 || number.Left < 0 || button.Top < 0 || number.Top < 0 ||
                        button.Right > modify.ClientSize.Width || number.Right > modify.ClientSize.Width ||
                        button.Bottom > modify.ClientSize.Height || number.Bottom > modify.ClientSize.Height)
                        throw new Exception("Clipped action control: " + modify.Name);
                }
                CheckChecklistResize();
                // Check the packaged native library and its managed ABI together.
                using (var bitmap = new SkiaSharp.SKBitmap(8, 8))
                using (var canvas = new SkiaSharp.SKCanvas(bitmap))
                {
                    canvas.Clear(SkiaSharp.SKColors.Red);
                    if (bitmap.GetPixel(0, 0) != SkiaSharp.SKColors.Red)
                        throw new Exception("Native Skia rendering failed.");
                }
                // Theme serialization can fall back to Mono's compiler helper.
                var serializer = new XmlSerializer(typeof(ThemeColorTable));
                var theme = new ThemeColorTable();
                theme.InitColors();
                var xml = new StringWriter();
                serializer.Serialize(xml, theme);
                var restored = (ThemeColorTable)serializer.Deserialize(new StringReader(xml.ToString()));
                if (restored.colors.Count != theme.colors.Count)
                    throw new Exception("Theme serialization failed.");
                Console.WriteLine("LINUX_SMOKE_TEST_PASS");
                Finish(0);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                Finish(1);
            }
        };
        timer.Start();
        Program.Main(new string[0]);
    }
}
