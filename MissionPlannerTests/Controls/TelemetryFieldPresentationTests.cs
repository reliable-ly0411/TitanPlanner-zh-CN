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
        public void RcPulseWidthsAndVibrationLabelsAreExactAndLocaleScoped()
        {
            var previous = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo("zh-CN");
                foreach (var group in new[] { Tuple.Create("in", 16, "遥控输入"), Tuple.Create("out", 32, "舵机输出") })
                    for (int channel = 1; channel <= group.Item2; channel++)
                    {
                        var raw = "ch" + channel + group.Item1;
                        var label = TelemetryFieldPresentation.FieldLabel(raw);
                        Assert.AreEqual(raw + " (" + group.Item3 + "第 " + channel + " 通道脉宽 (µs))", label);
                        Assert.IsTrue(TelemetryFieldPresentation.Matches(raw, label));
                        Assert.IsTrue(TelemetryFieldPresentation.Matches("脉宽", label));
                    }
                for (int sensor = 0; sensor < 3; sensor++)
                {
                    var raw = "vibeclip" + sensor;
                    Assert.AreEqual(raw + " (第 " + (sensor + 1) + " 个加速度计削波次数)", TelemetryFieldPresentation.FieldLabel(raw));
                }
                foreach (var axis in new[] { "x", "y", "z" })
                {
                    var raw = "vibe" + axis;
                    Assert.AreEqual(raw + " (" + axis.ToUpperInvariant() + " 轴振动水平)", TelemetryFieldPresentation.FieldLabel(raw));
                }

                foreach (var raw in new[] { "ch0in", "ch01in", "ch17in", "ch18in", "ch0out", "ch01out", "ch33out",
                    "ch1percent", "ch3percent", "vibeclip3", "vibew", "future_unknown" })
                    Assert.AreEqual(raw, TelemetryFieldPresentation.FieldLabel(raw));
                foreach (var locale in new[] { "zh-Hans", "en-US" })
                {
                    CultureInfo.CurrentUICulture = new CultureInfo(locale);
                    foreach (var raw in new[] { "ch1in", "ch16in", "ch1out", "ch32out", "vibeclip0", "vibez" })
                        Assert.AreEqual(raw, TelemetryFieldPresentation.FieldLabel(raw));
                }
            }
            finally { CultureInfo.CurrentUICulture = previous; }
        }


        [TestMethod]
        public void BatteryTemperatureAndEstimatedRuntimeKeepMonitorIdsAndLocaleFallback()
        {
            var previous = CultureInfo.CurrentUICulture;
            try
            {
                foreach (var locale in new[] { "zh-CN", "zh-Hans", "en-US" })
                {
                    CultureInfo.CurrentUICulture = new CultureInfo(locale);
                    for (int id = 0; id <= 8; id++)
                    {
                        string suffix = id == 0 ? "" : (id + 1).ToString(CultureInfo.InvariantCulture);
                        string temp = "battery_temp" + suffix;
                        string time = "battery_remainmin" + suffix;
                        Assert.AreEqual(locale == "zh-CN" ? temp + " (电池 ID " + id + " 温度 (°C))" : temp,
                            TelemetryFieldPresentation.FieldLabel(temp));
                        Assert.AreEqual(locale == "zh-CN" ? time + " (电池 ID " + id + " 预计剩余时间 (分钟))" : time,
                            TelemetryFieldPresentation.FieldLabel(time));
                        if (locale == "zh-CN")
                        {
                            Assert.IsTrue(TelemetryFieldPresentation.Matches("预计剩余时间", TelemetryFieldPresentation.FieldLabel(time)));
                            Assert.IsTrue(TelemetryFieldPresentation.Matches(temp, TelemetryFieldPresentation.FieldLabel(temp)));
                        }
                    }
                    foreach (var raw in new[] { "battery_temp0", "battery_temp1", "battery_temp10", "battery_temp02",
                        "battery_remainmin0", "battery_remainmin1", "battery_remainmin10", "battery_remainminX" })
                        Assert.AreEqual(raw, TelemetryFieldPresentation.FieldLabel(raw));
                }
            }
            finally { CultureInfo.CurrentUICulture = previous; }
        }


        [TestMethod]
        public void TerrainLabelsPreserveRawKeysAndDistinguishStatusFromCounts()
        {
            var previous = CultureInfo.CurrentUICulture;
            try
            {
                foreach (var locale in new[] { "zh-CN", "zh-Hans", "en-US" })
                {
                    CultureInfo.CurrentUICulture = new CultureInfo(locale);
                    foreach (var pair in new[] { Tuple.Create("ter_load", "内存中已加载的地形块数"),
                        Tuple.Create("ter_pend", "待接收或从磁盘读取的地形块数"),
                        Tuple.Create("ter_space", "地形网格间距"),
                        Tuple.Create("terrainactive", "地形子系统存在、已启用且状态正常") })
                    {
                        string label = TelemetryFieldPresentation.FieldLabel(pair.Item1);
                        Assert.AreEqual(locale == "zh-CN" ? pair.Item1 + " (" + pair.Item2 + ")" : pair.Item1, label);
                        Assert.IsTrue(TelemetryFieldPresentation.Matches(pair.Item1, label));
                        if (locale == "zh-CN")
                            Assert.IsTrue(TelemetryFieldPresentation.Matches(pair.Item2, label));
                    }
                    Assert.AreEqual("terrain_future", TelemetryFieldPresentation.FieldLabel("terrain_future"));
                }
            }
            finally { CultureInfo.CurrentUICulture = previous; }
        }


        [TestMethod]
        public void EstimatorPidAndStreamLabelsPreserveKeysAndLocale()
        {
            var previous = CultureInfo.CurrentUICulture;
            try
            {
                foreach (var locale in new[] { "zh-CN", "zh-Hans", "en-US" })
                {
                    CultureInfo.CurrentUICulture = new CultureInfo(locale);
                    foreach (var pair in new[] {
                        Tuple.Create("ekfcompv", "EKF 罗盘方差指标"),
                        Tuple.Create("ekfflags", "EKF 状态标志位"),
                        Tuple.Create("ekfposhor", "EKF 水平位置方差指标"),
                        Tuple.Create("ekfposvert", "EKF 垂直位置方差指标"),
                        Tuple.Create("ekfstatus", "地面站计算的 EKF 健康指标"),
                        Tuple.Create("ekfteralt", "EKF 地形高度方差指标"),
                        Tuple.Create("ekfvelv", "EKF 速度方差指标"),
                        Tuple.Create("pidachieved", "PID 实际速率"),
                        Tuple.Create("pidaxis", "PID 调试轴编号"),
                        Tuple.Create("pidD", "PID 微分分量"),
                        Tuple.Create("piddesired", "PID 目标速率"),
                        Tuple.Create("pidff", "PID 前馈分量"),
                        Tuple.Create("pidI", "PID 积分分量"),
                        Tuple.Create("pidP", "PID 比例分量"),
                        Tuple.Create("pidPDmod", "PID 比例和微分分量的振荡修正系数"),
                        Tuple.Create("pidSRate", "PID 上报的变化率"),
                        Tuple.Create("pidSRateAccZ", "PID 垂直加速度环变化率"),
                        Tuple.Create("pidSRateLanding", "PID 着陆控制环变化率"),
                        Tuple.Create("pidSRatePitch", "PID 俯仰控制环变化率"),
                        Tuple.Create("pidSRateRoll", "PID 横滚控制环变化率"),
                        Tuple.Create("pidSRateSteer", "PID 转向控制环变化率"),
                        Tuple.Create("pidSRateYaw", "PID 偏航控制环变化率"),
                        Tuple.Create("rateattitude", "请求的姿态和飞行数据流频率 (Hz)"),
                        Tuple.Create("rateposition", "请求的位置流频率 (Hz)"),
                        Tuple.Create("raterc", "请求的遥控通道流频率 (Hz)"),
                        Tuple.Create("ratesensors", "请求的传感器流频率 (Hz)"),
                        Tuple.Create("ratestatus", "请求的扩展状态流频率 (Hz)"),
                    })
                    {
                        string label = TelemetryFieldPresentation.FieldLabel(pair.Item1);
                        Assert.AreEqual(locale == "zh-CN" ? pair.Item1 + " (" + pair.Item2 + ")" : pair.Item1, label);
                        Assert.IsTrue(TelemetryFieldPresentation.Matches(pair.Item1, label));
                        if (locale == "zh-CN")
                            Assert.IsTrue(TelemetryFieldPresentation.Matches(pair.Item2, label));
                    }
                    foreach (var raw in new[] { "ekf_future", "pid_future", "rate_future", "ch3percent", "press_temp2" })
                        Assert.AreEqual(raw, TelemetryFieldPresentation.FieldLabel(raw));
                }
            }
            finally { CultureInfo.CurrentUICulture = previous; }
        }

        [TestMethod]
        public void DirectTelemetryLabelsPreserveKeysAndLocale()
        {
            var previous = CultureInfo.CurrentUICulture;
            try
            {
                foreach (var locale in new[] { "zh-CN", "zh-Hans", "en-US" })
                {
                    CultureInfo.CurrentUICulture = new CultureInfo(locale);
                    foreach (var pair in new[] {
                        Tuple.Create("i2cerrors", "上报的 I²C 错误次数"),
                        Tuple.Create("rxerrors", "数传接收错误次数"),
                        Tuple.Create("txbuffer", "数传发送缓冲区剩余（%）"),
                        Tuple.Create("fixedp", "数传已纠错的数据包数"),
                        Tuple.Create("brklevel", "上报的堆顶"),
                        Tuple.Create("freemem", "上报的可用内存（字节）"),
                        Tuple.Create("rpm1", "转速传感器 1 读数（rpm）"),
                        Tuple.Create("rpm2", "转速传感器 2 读数（rpm）"),
                        Tuple.Create("uid", "飞控硬件唯一标识"),
                        Tuple.Create("landed_state", "上报的着陆状态代码"),
                    })
                    {
                        string label = TelemetryFieldPresentation.FieldLabel(pair.Item1);
                        Assert.AreEqual(locale == "zh-CN" ? pair.Item1 + " (" + pair.Item2 + ")" : pair.Item1, label);
                        Assert.IsTrue(TelemetryFieldPresentation.Matches(pair.Item1, label));
                        if (locale == "zh-CN")
                            Assert.IsTrue(TelemetryFieldPresentation.Matches(pair.Item2, label));
                    }
                    foreach (var key in new[] { "landed", "packetdropremote", "rpm3" })
                        Assert.AreEqual(key, TelemetryFieldPresentation.FieldLabel(key));
                }
            }
            finally { CultureInfo.CurrentUICulture = previous; }
        }

        [TestMethod]
        public void DerivedTelemetryLabelsPreserveKeysAndLocale()
        {
            var previous = CultureInfo.CurrentUICulture;
            try
            {
                foreach (var locale in new[] { "zh-CN", "zh-Hans", "en-US" })
                {
                    CultureInfo.CurrentUICulture = new CultureInfo(locale);
                    foreach (var pair in new[] {
                        Tuple.Create("capabilities", "上报的 MAVLink 协议能力标志位"),
                        Tuple.Create("connected", "链路已打开或正在回放日志"),
                        Tuple.Create("vehicleClass", "地面站识别的机型类别"),
                        Tuple.Create("gimballat", "云台指向点纬度（°）"),
                        Tuple.Create("gimballng", "云台指向点经度（°）"),
                        Tuple.Create("timesincelastshot", "相邻两次相机触发记录的时间间隔（秒）"),
                        Tuple.Create("linkqualitygcs", "地面站数据包接收质量（%）"),
                    })
                    {
                        string label = TelemetryFieldPresentation.FieldLabel(pair.Item1);
                        Assert.AreEqual(locale == "zh-CN" ? pair.Item1 + " (" + pair.Item2 + ")" : pair.Item1, label);
                        Assert.IsTrue(TelemetryFieldPresentation.Matches(pair.Item1, label));
                        if (locale == "zh-CN")
                            Assert.IsTrue(TelemetryFieldPresentation.Matches(pair.Item2, label));
                    }
                    foreach (var key in new[] { "landed", "GimbalPointFuture" })
                        Assert.AreEqual(key, TelemetryFieldPresentation.FieldLabel(key));
                }
            }
            finally { CultureInfo.CurrentUICulture = previous; }
        }

        [TestMethod]
        public void GimbalAngleLabelsPreserveExactAxesAndBilingualSearch()
        {
            var previous = CultureInfo.CurrentUICulture;
            try
            {
                var axes = new[] {
                    Tuple.Create("campointa", "云台俯仰角（°）", "俯仰角"),
                    Tuple.Create("campointb", "云台横滚角（°）", "横滚角"),
                    Tuple.Create("campointc", "云台偏航角（°）", "偏航角"),
                };
                foreach (var locale in new[] { "zh-CN", "zh-Hans", "en-US", "fr-FR" })
                {
                    CultureInfo.CurrentUICulture = new CultureInfo(locale);
                    foreach (var axis in axes)
                    {
                        var property = typeof(CurrentState).GetProperty(axis.Item1);
                        Assert.IsNotNull(property, axis.Item1);
                        Assert.AreEqual(typeof(float), property.PropertyType, axis.Item1);
                        Assert.AreEqual("Mount", CurrentState.GetGroupText(axis.Item1), axis.Item1);
                        var label = TelemetryFieldPresentation.FieldLabel(axis.Item1);
                        Assert.AreEqual(locale == "zh-CN" ? axis.Item1 + " (" + axis.Item2 + ")" : axis.Item1, label);
                        Assert.IsTrue(TelemetryFieldPresentation.Matches(axis.Item1.ToUpperInvariant(), label), label);
                        foreach (var search in axes)
                            Assert.AreEqual(locale == "zh-CN" && search.Item1 == axis.Item1,
                                TelemetryFieldPresentation.Matches(search.Item3, label), label);
                    }
                    foreach (var key in new[] { "campointd", "campointa_future" })
                        Assert.AreEqual(key, TelemetryFieldPresentation.FieldLabel(key));
                }
            }
            finally { CultureInfo.CurrentUICulture = previous; }
        }

        [TestMethod]
        public void NavigationAndDiagnosticLabelsPreserveKeys()
        {
            var previous = CultureInfo.CurrentUICulture;
            try
            {
                foreach (var locale in new[] { "zh-CN", "zh-Hans", "en-US" })
                {
                    CultureInfo.CurrentUICulture = new CultureInfo(locale);
                    foreach (var pair in new[] {
                        Tuple.Create("groundspeed2", "第二路 GPS 地速（m/s）"),
                        Tuple.Create("vlen", "三维速度大小（m/s）"),
                        Tuple.Create("HomeAlt", "返航点高度"),
                        Tuple.Create("targetalt", "导航目标高度"),
                        Tuple.Create("targetaltd100", "目标高度百位分量（含小数）"),
                        Tuple.Create("load", "飞控上报的处理负载（%）"),
                        Tuple.Create("KIndex", "地磁活动 K 指数"),
                        Tuple.Create("speedup", "IMU 时间相对于实际时间的估算倍率"),
                    })
                    {
                        var label = TelemetryFieldPresentation.FieldLabel(pair.Item1);
                        Assert.AreEqual(locale == "zh-CN" ? pair.Item1 + " (" + pair.Item2 + ")" : pair.Item1, label);
                        Assert.IsTrue(TelemetryFieldPresentation.Matches(pair.Item1, label));
                        if (locale == "zh-CN")
                            Assert.IsTrue(TelemetryFieldPresentation.Matches(pair.Item2, label));
                    }
                    foreach (var key in new[] { "press_temp2", "targetaltd1000", "ch3percent" })
                        Assert.AreEqual(key, TelemetryFieldPresentation.FieldLabel(key));
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
