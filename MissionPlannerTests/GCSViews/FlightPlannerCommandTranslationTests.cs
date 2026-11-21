using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using GMap.NET;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MissionPlanner.Controls;
using MissionPlanner.GCSViews;
using MissionPlanner.Utilities;
using Newtonsoft.Json;

namespace MissionPlannerTests.GCSViews
{
    [TestClass]
    [DoNotParallelize]
    public class FlightPlannerCommandTranslationTests
    {
        // Exercise the real planner handlers without starting maps, networking or a vehicle connection.
        private sealed class PlannerGrid : IDisposable
        {
            internal readonly FlightPlanner Planner = (FlightPlanner)FormatterServices.GetUninitializedObject(typeof(FlightPlanner));
            internal readonly MyDataGridView Grid = new MyDataGridView { AllowUserToAddRows = false, BindingContext = new BindingContext() };
            internal readonly Dictionary<string, string[]> Names = new Dictionary<string, string[]>();
            private readonly string[] columns = { "Command", "Param1", "Param2", "Param3", "Param4", "Lat", "Lon", "Alt", "Frame",
                "coordZone", "coordEasting", "coordNorthing", "MGRS", "TagData" };

            internal PlannerGrid()
            {
                Planner.Commands = Grid;
                Planner.quickadd = true;
                foreach (var name in columns)
                {
                    DataGridViewColumn column = name == "Command" || name == "Frame"
                        ? (DataGridViewColumn)new DataGridViewComboBoxColumn() : new DataGridViewTextBoxColumn();
                    column.Name = name;
                    Grid.Columns.Add(column);
                    Set(name, column);
                }
                var command = (DataGridViewComboBoxColumn)Grid.Columns["Command"];
                command.DisplayMember = "Value";
                command.ValueMember = "Key";
                command.DataSource = MissionCommandDisplay.CreateOptions(new[] {
                    "WAYPOINT", "TAKEOFF", "LAND", "DO_CHANGE_SPEED", "DO_SET_SERVO", "DO_DIGICAM_CONTROL", "UNKNOWN", "TP_CUSTOM_60000" });
                ((DataGridViewComboBoxColumn)Grid.Columns["Frame"]).Items.AddRange(0, 3, 10);
                Grid.Rows.Add("WAYPOINT", 1.25, 2.5, 3.75, -4.5, 1.125, -2.25, 100.0, 3, 0, 0, 0, "", null);
                Names["WAYPOINT"] = new[] { "Delay", "", "", "", "Lat", "Long", "Alt (ft)" };
                Names["DO_SET_SERVO"] = new[] { "Ser No", "PWM", "", "", "", "", "" };
                foreach (var name in new[] { "TAKEOFF", "LAND", "DO_CHANGE_SPEED", "DO_DIGICAM_CONTROL", "UNKNOWN", "TP_CUSTOM_60000" })
                    Names[name] = new[] { "", "", "", "", "", "", "" };
                Set("cmdParamNames", Names);
                Set("cmdParamMultipliers", Names.Keys.ToDictionary(name => name,
                    name => new[] { 1.0, 2.0, 1.0, 1.0, 1.0, 1.0, 2.0 }));
                Planner.TXT_DefaultAlt = new TextBox { Text = "60" };
                Planner.TXT_homealt = new TextBox { Text = "10" };
                Planner.CHK_verifyheight = new CheckBox { Checked = false };
            }

            private void Set(string name, object value)
            {
                typeof(FlightPlanner).GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(Planner, value);
            }

            internal object Invoke(string method, params object[] args)
            {
                return typeof(FlightPlanner).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(Planner, args);
            }

            internal DataGridViewCell Cell(string name) { return Grid.Rows[0].Cells[name]; }
            internal Locationwp Read() { return (Locationwp)Invoke("DataViewtoLocationwp", 0); }

            public void Dispose()
            {
                Grid.Dispose();
                Planner.TXT_DefaultAlt.Dispose();
                Planner.TXT_homealt.Dispose();
                Planner.CHK_verifyheight.Dispose();
            }
        }

        private static void OnSta(Action body)
        {
            Exception failure = null;
            var thread = new Thread(() =>
            {
                try { CultureInfo.CurrentCulture = CultureInfo.InvariantCulture; body(); }
                catch (Exception ex) { failure = ex; }
            }) { IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(30)), "Mission command regression timed out.");
            if (failure != null) Assert.Fail(failure.ToString());
        }

        [DataTestMethod]
        [DataRow("zh-CN")]
        [DataRow("zh-Hans")]
        [DataRow("en-US")]
        public void RealConstructorAndRepeatedCommandRefreshPreserveBindingMembers(string language)
        {
            OnSta(() =>
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);
                // Load the settings singleton before replacing its in-memory config.
                var settings = Settings.Instance;
                var previousConfig = Settings.config;
                var previousUserDirectory = Settings.CustomUserDataDirectory;
                var previousAppName = Settings.AppConfigName;
                var previousMapMode = GMaps.Instance.Mode;
                var previousPlanner = FlightPlanner.instance;
                var temporaryDirectory = Path.Combine(Path.GetTempPath(), "mission-constructor-" + Guid.NewGuid().ToString("N"));
                Settings.config = new Dictionary<string, string>
                {
                    { "language", language }, { "MapType", "BingSatelliteMap" }
                };
                Settings.CustomUserDataDirectory = temporaryDirectory;
                Settings.AppConfigName = "TitanPlanner-MissionConstructor-" + Guid.NewGuid().ToString("N");
                GMaps.Instance.Mode = AccessMode.CacheOnly;
                try
                {
                    // Same order as MainV2: construct the real planner, then parent it.
                    // Do not create an uninitialized planner or supply a BindingContext.
                    using (var planner = new FlightPlanner())
                    using (var host = new Form())
                    {
                        try
                        {
                            Assert.IsNull(planner.Parent);
                            var command = (DataGridViewComboBoxColumn)planner.Commands.Columns["Command"];
                            Assert.AreEqual("Value", command.DisplayMember);
                            Assert.AreEqual("Key", command.ValueMember);
                            var expectedKeys = ((IEnumerable<KeyValuePair<string, string>>)command.DataSource)
                                .Select(option => option.Key).ToArray();
                            Assert.IsTrue(expectedKeys.Contains("WAYPOINT"));
                            Assert.IsTrue(expectedKeys.Contains("UNKNOWN"));
                            for (int phase = 0; phase < 2; phase++)
                            {
                                if (phase == 1) host.Controls.Add(planner);
                                for (int refresh = 0; refresh < 3; refresh++)
                                {
                                    // Populate the template's Items/CurrencyManager cache
                                    // before calling the actual rebind path again.
                                    Assert.AreEqual(expectedKeys.Length, command.Items.Count);
                                    planner.updateCMDParams();
                                    Assert.AreEqual("Value", command.DisplayMember);
                                    Assert.AreEqual("Key", command.ValueMember);
                                    var options = (IEnumerable<KeyValuePair<string, string>>)command.DataSource;
                                    CollectionAssert.AreEqual(expectedKeys, options.Select(option => option.Key).ToArray());
                                    Assert.AreEqual(MissionCommandDisplay.CommandName("WAYPOINT"),
                                        options.Single(option => option.Key == "WAYPOINT").Value);
                                }
                            }
                        }
                        finally
                        {
                            planner.FlightPlanner_FormClosing(planner, new FormClosingEventArgs(CloseReason.UserClosing, false));
                        }
                    }
                }
                finally
                {
                    GMaps.Instance.Mode = previousMapMode;
                    FlightPlanner.instance = previousPlanner;
                    Settings.config = previousConfig;
                    Settings.CustomUserDataDirectory = previousUserDirectory;
                    Settings.AppConfigName = previousAppName;
                }
            });
        }

        [TestMethod]
        public void TranslatedCellsPreserveMissionIdsParametersFramesJsonAndMavlinkPayloads()
        {
            OnSta(() =>
            {
                var baselineJson = new Dictionary<string, string>();
                var baselinePayload = new Dictionary<string, byte[]>();
                foreach (var language in new[] { "en-US", "zh-CN", "zh-Hans" })
                {
                    CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);
                    using (var test = new PlannerGrid())
                    foreach (var command in new[] { "WAYPOINT", "TAKEOFF", "DO_CHANGE_SPEED", "DO_SET_SERVO", "UNKNOWN" })
                    foreach (var frame in new[] { 0, 3, 10 })
                    {
                        test.Cell("Command").Value = command;
                        test.Cell("Command").Tag = (ushort)60001;
                        test.Cell("Frame").Value = frame;
                        // Read the formatted cell as well as the bound key; formatting must not rewrite data.
                        var display = test.Cell("Command").FormattedValue.ToString();
                        Assert.AreEqual(MissionCommandDisplay.CommandName(command), display);
                        Assert.AreEqual(command, test.Cell("Command").Value);
                        var item = test.Read();
                        Assert.AreEqual(command == "UNKNOWN" ? (ushort)60001 : test.Planner.getCmdID(command), item.id);
                        Assert.AreEqual((byte)frame, item.frame);
                        Assert.AreEqual(1.25f, item.p1);
                        Assert.AreEqual(1.25f, item.p2);
                        Assert.AreEqual(3.75f, item.p3);
                        Assert.AreEqual(-4.5f, item.p4);
                        Assert.AreEqual(1.125, item.lat);
                        Assert.AreEqual(-2.25, item.lng);
                        Assert.AreEqual(50f, item.alt);
                        var json = JsonConvert.SerializeObject(MissionFile.ConvertFromLocationwps(
                            new List<Locationwp> { new Locationwp(), item }, (byte)frame));
                        MAVLink.mavlink_mission_item_int_t packet = item;
                        var bytes = MavlinkUtil.StructureToByteArray(packet);
                        var key = command + ":" + frame;
                        if (language == "en-US") { baselineJson[key] = json; baselinePayload[key] = bytes; }
                        else
                        {
                            Assert.AreEqual(baselineJson[key], json);
                            CollectionAssert.AreEqual(baselinePayload[key], bytes);
                        }
                    }
                }
            });
        }

        [TestMethod]
        public void ChineseHeadersPreserveMapCoordinatesAltitudeDelayAndNonCoordinateParameters()
        {
            OnSta(() =>
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("zh-CN");
                using (var test = new PlannerGrid())
                {
                    var metadata = test.Names["WAYPOINT"].ToArray();
                    test.Invoke("ChangeColumnHeader", "WAYPOINT");
                    Assert.AreEqual("纬度", test.Grid.Columns["Lat"].HeaderText);
                    Assert.AreEqual("高度 (ft)", test.Grid.Columns["Alt"].HeaderText);
                    Assert.AreEqual("延时", test.Grid.Columns["Param1"].HeaderText);
                    CollectionAssert.AreEqual(metadata, test.Names["WAYPOINT"]);
                    test.Planner.setfromMap(1.5, -2.5, 80, 6);
                    Assert.AreEqual(1.5, double.Parse(test.Cell("Lat").Value.ToString()));
                    Assert.AreEqual(-2.5, double.Parse(test.Cell("Lon").Value.ToString()));
                    Assert.AreEqual(80, int.Parse(test.Cell("Alt").Value.ToString()));
                    Assert.AreEqual(6.0, test.Cell("Param1").Value);
                    test.Cell("Command").Value = "DO_SET_SERVO";
                    test.Invoke("ChangeColumnHeader", "DO_SET_SERVO");
                    var before = new[] { test.Cell("Param1").Value, test.Cell("Lat").Value,
                        test.Cell("Lon").Value, test.Cell("Alt").Value };
                    test.Planner.setfromMap(3.5, -4.5, 90, 7);
                    CollectionAssert.AreEqual(before, new[] { test.Cell("Param1").Value,
                        test.Cell("Lat").Value, test.Cell("Lon").Value, test.Cell("Alt").Value });
                }
            });
        }

        [TestMethod]
        public void ChineseSelectionRetainsTakeoffLandAndCameraDefaultsAndIgnoresFrameLabels()
        {
            OnSta(() =>
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("zh-CN");
                using (var test = new PlannerGrid())
                using (var choices = new ComboBox { BindingContext = new BindingContext(), DisplayMember = "Value", ValueMember = "Key",
                    DataSource = MissionCommandDisplay.CreateOptions(new[] { "TAKEOFF", "LAND", "DO_DIGICAM_CONTROL" }) })
                {
                    foreach (var command in new[] { "TAKEOFF", "LAND", "DO_DIGICAM_CONTROL" })
                    {
                        choices.SelectedValue = command;
                        test.Cell("Command").Value = command;
                        test.Cell("Alt").Value = command == "LAND" ? "50" : "0";
                        test.Cell("Lat").Value = "0";
                        test.Invoke("Commands_SelectionChangeCommitted", choices, EventArgs.Empty);
                        Assert.AreEqual(test.Planner.getCmdID(command), test.Cell("Command").Tag);
                        Assert.AreEqual(command == "TAKEOFF" ? "60" : "0", test.Cell("Alt").Value.ToString());
                        if (command == "TAKEOFF") Assert.AreEqual("0", test.Cell("Lon").Value.ToString());
                        if (command == "DO_DIGICAM_CONTROL") Assert.AreEqual("1", test.Cell("Lat").Value.ToString());
                    }
                    using (var frames = new ComboBox { BindingContext = new BindingContext(), DisplayMember = "Value", ValueMember = "Key",
                        DataSource = new[] { new KeyValuePair<int, string>(3, "TAKEOFF") } })
                    {
                        test.Cell("Alt").Value = "42";
                        test.Invoke("Commands_SelectionChangeCommitted", frames, EventArgs.Empty);
                        Assert.AreEqual("42", test.Cell("Alt").Value);
                    }
                }
            });
        }
    }
}
