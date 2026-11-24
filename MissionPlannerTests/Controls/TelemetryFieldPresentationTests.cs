using System;
using System.Collections;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Globalization;
using System.Linq;
using MissionPlanner;
using MissionPlanner.ArduPilot;
using MissionPlanner.Attributes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MissionPlanner.Controls;

namespace MissionPlannerTests.Controls
{
    [TestClass]
    public class TelemetryFieldPresentationTests
    {
        [TestMethod]
        public void CombinedSatelliteCountLabelDescribesSum()
        {
            var previous = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo("zh-CN");
                Assert.AreEqual("两个 GPS 卫星数量之和", CurrentState.GetDisplayText("satcountB"));
            }
            finally { CultureInfo.CurrentUICulture = previous; }
        }

        [TestMethod]
        public void AbstractUnitLabelsDoNotInferUnitsAcrossMessagePaths()
        {
            var previousCulture = CultureInfo.CurrentUICulture;
            var previousSpeed = CurrentState.SpeedUnit;
            var previousDistance = CurrentState.DistanceUnit;
            var previousAltitude = CurrentState.AltUnit;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo("zh-CN");
                var fields = new[] { "alt", "altasl", "horizondist", "altoffsethome", "altasl2",
                    "airspeed", "accel_air", "targetairspeed", "groundspeed", "verticalspeed", "wp_dist",
                    "alt_error", "aspd_error", "climbrate", "distTraveled", "turnrate", "radius",
                    "wind_vel", "DistToHome", "DistFromMovingBase", "sonarrange" };
                foreach (var units in new[] { new[] { "km/h", "km", "ft" }, new[] { "knots", "nm", "m" } })
                {
                    CurrentState.SpeedUnit = units[0];
                    CurrentState.DistanceUnit = units[1];
                    CurrentState.AltUnit = units[2];
                    foreach (var field in fields)
                    {
                        var label = TelemetryFieldPresentation.FieldLabel(field);
                        Assert.IsTrue(label.StartsWith(field + " (", StringComparison.Ordinal), field);
                        var raw = CurrentState.GetDisplayText(field);
                        var expected = raw.Replace(" (dist)", "").Replace(" (alt)", "")
                            .Replace(" (speed)", "").Replace(" (speed/s)", "");
                        Assert.AreEqual(field + " (" + expected + ")", label, field);
                        Assert.IsFalse(label.Contains("(dist)") || label.Contains("(alt)") || label.Contains("(speed"), field);
                    }
                }
                foreach (var field in new[] { "vx", "vy", "vz" })
                {
                    var label = TelemetryFieldPresentation.FieldLabel(field);
                    Assert.IsTrue(label.StartsWith(field + " (", StringComparison.Ordinal), field);
                    Assert.IsTrue(label.Contains("(m/s)"), label);
                    Assert.IsFalse(label.Contains("(ms)"), label);
                }
            }
            finally
            {
                CultureInfo.CurrentUICulture = previousCulture;
                CurrentState.SpeedUnit = previousSpeed;
                CurrentState.DistanceUnit = previousDistance;
                CurrentState.AltUnit = previousAltitude;
            }
        }

        [TestMethod]
        public void EveryAttributedTelemetryPropertyHasChineseDescriptionAndOriginalKey()
        {
            var previous = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo("zh-CN");
                int count = 0;
                foreach (var property in typeof(CurrentState).GetProperties())
                {
                    if (!property.IsDefined(typeof(DisplayTextAttribute), false)) continue;
                    var description = CurrentState.GetDisplayText(property.Name);
                    Assert.IsTrue(description.Any(c => c >= '\u4e00' && c <= '\u9fff'), property.Name + ": " + description);
                    var label = TelemetryFieldPresentation.FieldLabel(property.Name);
                    Assert.IsTrue(label.StartsWith(property.Name + " (", StringComparison.Ordinal), property.Name);
                    count++;
                }
                Assert.IsTrue(count >= 200, "Expected the actual telemetry property catalog.");
            }
            finally { CultureInfo.CurrentUICulture = previous; }
        }

        [TestMethod]
        public void SecondaryAhrsEscAndGeneratorUseExactKeysAndEnhancedDescriptions()
        {
            var previous = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo("zh-CN");
                var ahrs = new[] { "ahrs2_alt", "ahrs2_lat", "ahrs2_lng", "ahrs2_pitch", "ahrs2_roll", "ahrs2_yaw" };
                var generator = new[] { "gen_status", "gen_speed", "gen_current", "gen_voltage", "gen_runtime", "gen_maint_time" };
                var esc = Enumerable.Range(1, 16).SelectMany(i => new[] { "curr", "rpm", "temp", "volt" }
                    .Select(kind => "esc" + i.ToString(CultureInfo.InvariantCulture) + "_" + kind));
                foreach (var key in ahrs.Concat(generator).Concat(esc))
                {
                    Assert.IsNotNull(typeof(CurrentState).GetProperty(key), key);
                    var label = TelemetryFieldPresentation.FieldLabel(key);
                    Assert.IsTrue(label.StartsWith(key + " (", StringComparison.Ordinal), label);
                    Assert.IsTrue(label.Any(c => c >= '\u4e00' && c <= '\u9fff'), label);
                    Assert.IsTrue(TelemetryFieldPresentation.Matches(key, key, label), key);
                }

                var rotor = TelemetryFieldPresentation.FieldLabel("esc16_rpm");
                var current = TelemetryFieldPresentation.FieldLabel("gen_current");
                Assert.IsTrue(TelemetryFieldPresentation.Matches("电气转速", rotor));
                Assert.IsTrue(rotor.Contains("eRPM"), rotor);
                Assert.IsTrue(TelemetryFieldPresentation.Matches("负载电流", current));
                Assert.IsTrue(current.Contains("(A)"), current);
                foreach (var key in new[] { "esc0_rpm", "esc01_rpm", "esc17_rpm", "esc1_unknown", "future_unknown" })
                    Assert.AreEqual(key, TelemetryFieldPresentation.FieldLabel(key));

                foreach (var culture in new[] { "zh-Hans", "en-US", "fr-FR" })
                {
                    CultureInfo.CurrentUICulture = new CultureInfo(culture);
                    foreach (var key in new[] { "ahrs2_lat", "esc16_rpm", "gen_current" })
                        Assert.AreEqual(key, TelemetryFieldPresentation.FieldLabel(key));
                }
            }
            finally { CultureInfo.CurrentUICulture = previous; }
        }

        [TestMethod]
        public void StatusBilingualLabelRemainsReadableAtNarrowSizes()
        {
            Exception failure = null;
            var thread = new Thread(() =>
            {
                try
                {
                    using (var form = new Form { ClientSize = new Size(320, 480) })
                    using (var status = new StatusControl { Dock = DockStyle.Fill })
                    {
                        form.Controls.Add(status);
                        form.Show();
                        var type = status.GetType();
                        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                        var list = (IList)type.GetField("_allItems", flags).GetValue(status);
                        const string shortLabel = "airspeed (空速)";
                        string longLabel = "alt (高度 / " + new string('W', 80) + ")";
                        const string longValue = "-1234567890.123456789012345678901234567890123456789";
                        list.Add(new StatusControl.StatusItem { Name = "airspeed", DisplayName = shortLabel, Value = "12.5" });
                        list.Add(new StatusControl.StatusItem { Name = "alt", DisplayName = longLabel, Value = longValue });
                        type.GetField("_filteredItems", flags).SetValue(status, list);
                        foreach (var width in new[] { 320, 640, 1280 })
                        {
                            form.ClientSize = new Size(width, 480);
                            type.GetMethod("UpdateScrollSize", flags).Invoke(status, null);
                            Application.DoEvents();
                            var panel = (Panel)type.GetField("_contentPanel", flags).GetValue(status);
                            var columns = (IList)type.GetField("_columns", flags).GetValue(status);
                            Assert.AreEqual(1, columns.Count);
                            var column = columns[0];
                            var columnType = column.GetType();
                            int left = (int)columnType.GetField("Left").GetValue(column);
                            int nameWidth = (int)columnType.GetField("NameWidth").GetValue(column);
                            int valueWidth = (int)columnType.GetField("ValueWidth").GetValue(column);
                            int columnWidth = (int)columnType.GetField("Width").GetValue(column);
                            var labels = (string[])columnType.GetField("Labels").GetValue(column);
                            int gap = (int)type.GetField("_nameValueGap", flags).GetValue(status);
                            int padding = (int)type.GetField("_padding", flags).GetValue(status);
                            int rowHeight = (int)type.GetField("_rowHeight", flags).GetValue(status);
                            using (var graphics = panel.CreateGraphics())
                            using (var bold = new Font(status.Font, FontStyle.Bold))
                            using (var normal = new Font(status.Font, FontStyle.Regular))
                            {
                                Assert.AreEqual(shortLabel, labels[0], "Short bilingual labels remain complete.");
                                Assert.IsTrue(nameWidth < graphics.MeasureString(longLabel, bold).Width,
                                    "A long label must not push every value away.");
                                Assert.IsTrue(labels[1].EndsWith("…", StringComparison.Ordinal));
                                Assert.IsTrue(graphics.MeasureString(labels[1], bold).Width <= nameWidth);
                                Assert.IsTrue(valueWidth >= Math.Ceiling(graphics.MeasureString(longValue, normal).Width),
                                    "Reserve the complete value rather than silently clipping it.");
                            }
                            Assert.IsTrue(gap > 0 && gap < rowHeight);
                            Assert.IsTrue(panel.AutoScrollMinSize.Width >= left + columnWidth + padding);
                            panel.AutoScrollPosition = new Point(panel.AutoScrollMinSize.Width, 0);
                            Assert.IsTrue(left + columnWidth + panel.AutoScrollPosition.X <= panel.ClientSize.Width,
                                "Scrolling must expose the end of the value.");
                            int x = Math.Max(0, left + nameWidth + gap + panel.AutoScrollPosition.X) + 1;
                            int y = padding + rowHeight + 1;
                            type.GetMethod("ContentPanel_MouseMove", flags).Invoke(status,
                                new object[] { panel, new MouseEventArgs(MouseButtons.None, 0, x, y, 0) });
                            var toolTip = (ToolTip)type.GetField("_toolTip", flags).GetValue(status);
                            Assert.AreEqual(longLabel + Environment.NewLine + longValue, toolTip.GetToolTip(panel));
                            Assert.AreEqual("airspeed", ((StatusControl.StatusItem)list[0]).Name);
                            Assert.AreEqual("12.5", ((StatusControl.StatusItem)list[0]).Value);
                            Assert.AreEqual(longValue, ((StatusControl.StatusItem)list[1]).Value);
                        }
                    }
                }
                catch (Exception ex) { failure = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(20)));
            if (failure != null) Assert.Fail(failure.ToString());
        }


        [TestMethod]
        public void RelativeAltitudeArmingAndBatteryLabelsKeepExactRawKeys()
        {
            var previous = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo("zh-CN");
                Assert.AreEqual("altd100 (相对高度百位分量（含小数）)", TelemetryFieldPresentation.FieldLabel("altd100"));
                Assert.AreEqual("altd1000 (相对高度千位分量（含小数）)", TelemetryFieldPresentation.FieldLabel("altd1000"));
                var armed = TelemetryFieldPresentation.FieldLabel("armed");
                Assert.AreEqual("armed (飞行器解锁状态)", armed);
                Assert.IsTrue(TelemetryFieldPresentation.Matches("解锁状态", armed));
                for (int cell = 1; cell <= 14; cell++)
                {
                    var raw = "battery_cell" + cell;
                    var label = TelemetryFieldPresentation.FieldLabel(raw);
                    Assert.AreEqual(raw + " (主电池第 " + cell + " 节电压 (V))", label);
                    Assert.IsTrue(TelemetryFieldPresentation.Matches(raw, label));
                }
                foreach (var raw in new[] { "battery_cell0", "battery_cell01", "battery_cell15", "battery_cellX", "future_unknown" })
                    Assert.AreEqual(raw, TelemetryFieldPresentation.FieldLabel(raw));

                foreach (var locale in new[] { "zh-Hans", "en-US" })
                {
                    CultureInfo.CurrentUICulture = new CultureInfo(locale);
                    foreach (var raw in new[] { "altd100", "altd1000", "armed", "battery_cell1", "battery_cell14" })
                        Assert.AreEqual(raw, TelemetryFieldPresentation.FieldLabel(raw));
                }
            }
            finally { CultureInfo.CurrentUICulture = previous; }
        }

        [TestMethod]
        public void LabelsRetainKeysAndBilingualSearch()
        {
            var label = TelemetryFieldPresentation.Combine("airspeed", "空速");
            Assert.AreEqual("airspeed (空速)", label);
            Assert.IsTrue(TelemetryFieldPresentation.Matches("空速", label));
            Assert.IsTrue(TelemetryFieldPresentation.Matches("AIRspeed", label));
            Assert.IsFalse(TelemetryFieldPresentation.Matches("电池", label));
            Assert.AreEqual("unknown", TelemetryFieldPresentation.Combine("unknown", ""));
            Assert.AreEqual("unknown", TelemetryFieldPresentation.Combine("unknown", "unknown"));
        }

        [TestMethod]
        public void EnglishAndOriginalChineseKeepFieldKeys()
        {
            var previous = CultureInfo.CurrentUICulture;
            try
            {
                foreach (var culture in new[] { "en-US", "zh-Hans" })
                {
                    CultureInfo.CurrentUICulture = new CultureInfo(culture);
                    Assert.AreEqual("airspeed", TelemetryFieldPresentation.FieldLabel("airspeed"));
                    Assert.AreEqual("Terrain", TelemetryFieldPresentation.GroupLabel("Terrain"));
                }
            }
            finally { CultureInfo.CurrentUICulture = previous; }
        }
    }
}
