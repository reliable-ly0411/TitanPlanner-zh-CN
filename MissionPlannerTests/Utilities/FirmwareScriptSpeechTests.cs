using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MissionPlanner;
using MissionPlanner.Utilities;
using Autopilot = MAVLink.MAV_AUTOPILOT;
using Severity = MAVLink.MAV_SEVERITY;
using Vehicle = MAVLink.MAV_TYPE;

namespace MissionPlannerTests.Utilities
{
    [TestClass]
    public class FirmwareScriptSpeechTests
    {
        private static string Resolve(Vehicle vehicle, Severity severity, string raw,
            string culture = "zh-CN", string hash = "d9c5097c",
            uint version = 0x040703ff, Autopilot autopilot = Autopilot.ARDUPILOTMEGA)
        {
            var previous = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
                var resolver = typeof(MAVLinkInterface).Assembly.GetType("MissionPlanner.FirmwareScriptSpeech", true);
                return (string)resolver.GetMethod("Translate", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { autopilot, vehicle, version, Encoding.ASCII.GetBytes(hash), (byte)severity, raw });
            }
            finally
            {
                CultureInfo.CurrentUICulture = previous;
            }
        }

        [TestMethod]
        public void ResolvesPinnedTextAndPreservesParameters()
        {
            var fixtures = new[]
            {
                Tuple.Create(Vehicle.QUADROTOR, Severity.INFO, "Tuning: RLL_D done", "快速调参已完成，轴 RLL_D"),
                Tuple.Create(Vehicle.QUADROTOR, Severity.INFO, "Quicktune: Roll D done", "原生快速调参已完成，轴 Roll D"),
                Tuple.Create(Vehicle.QUADROTOR, Severity.INFO, "adjusting RLL_P 0.123 -> 0.045", "快速调参：调整 RLL_P，从 0.123 到 0.045"),
                Tuple.Create(Vehicle.QUADROTOR, Severity.INFO, "Limiting Roll D 0.123 -> 0.045", "原生快速调参：限制 Roll D，从 0.123 到 0.045"),
                Tuple.Create(Vehicle.GROUND_ROVER, Severity.INFO, "RTun: adjusted ATC_STR_RAT_FF 0.100 -> 0.200", "已调整 ATC_STR_RAT_FF，从 0.100 到 0.200"),
                Tuple.Create(Vehicle.FIXED_WING, Severity.INFO, "Takeoff complete dist=12.3", "起飞完成，距离 12.3"),
                Tuple.Create(Vehicle.FIXED_WING, Severity.NOTICE, "Staring KnifeEdge Circle", "开始刀刃圆周飞行"),
                Tuple.Create(Vehicle.FIXED_WING, Severity.NOTICE, "Trick 2 selected (Loop(s)/Turnaround)", "已选择特技动作 2（Loop(s)/Turnaround）"),
                Tuple.Create(Vehicle.QUADROTOR, Severity.NOTICE, "Tuning: RLL done", "快速调参已完成，轴 RLL"),
                Tuple.Create(Vehicle.FIXED_WING, Severity.CRITICAL, "Tuning: attitude error -1.3deg - ABORTING", "姿态误差 -1.3deg，快速调参已中止"),
                Tuple.Create(Vehicle.VTOL_QUADROTOR, Severity.NOTICE, "Quicktune: DONE", "原生快速调参已完成"),
                Tuple.Create(Vehicle.GROUND_ROVER, Severity.INFO, "RTun: ATC_STR_RAT_FF 38% complete", "ATC_STR_RAT_FF 调参完成 38%"),
                Tuple.Create(Vehicle.SURFACE_BOAT, Severity.INFO, "RTun: adjusted ATC_SPEED_I -0.143 -> 0.024", "已调整 ATC_SPEED_I，从 -0.143 到 0.024"),
                Tuple.Create(Vehicle.GROUND_ROVER, Severity.WARNING, "RTun: increase turn rate (6 deg/s < 12)", "增大转向速率，6 deg/s 小于 12"),
                Tuple.Create(Vehicle.FIXED_WING, Severity.NOTICE, "Starting 3 Roll(s)", "开始 3 次横滚"),
                Tuple.Create(Vehicle.FIXED_WING, Severity.NOTICE, "Finished Turnaround ", "转向返回完成"),
                Tuple.Create(Vehicle.FIXED_WING, Severity.INFO, "Trick 12 started (Roll)", "特技动作 12 已开始（Roll）"),
            };
            foreach (var fixture in fixtures)
                Assert.AreEqual(fixture.Item4, Resolve(fixture.Item1, fixture.Item2, fixture.Item3), fixture.Item3);
        }

        [TestMethod]
        public void LeavesUnverifiedTextsForTheExistingSpeechRoute()
        {
            var fixtures = new[]
            {
                Tuple.Create(Vehicle.QUADROTOR, Severity.NOTICE, "Tuning: RLL_D done"),
                Tuple.Create(Vehicle.QUADROTOR, Severity.INFO, "Quicktune: RLL_D done"),
                Tuple.Create(Vehicle.QUADROTOR, Severity.INFO, "Tuning: ATC_RAT_RLL_D done"),
                Tuple.Create(Vehicle.FIXED_WING, Severity.NOTICE, "Starting KnifeEdge Circle"),
                Tuple.Create(Vehicle.FIXED_WING, Severity.NOTICE, "Finished Split-S"),
                Tuple.Create(Vehicle.QUADROTOR, Severity.NOTICE, "Tuning: RLL done\r\nnext"),
                Tuple.Create(Vehicle.QUADROTOR, Severity.NOTICE, "Tuning: PITCH done"),
                Tuple.Create(Vehicle.QUADROTOR, Severity.NOTICE, "Tuning: unknown applet"),
                Tuple.Create(Vehicle.QUADROTOR, Severity.INFO, "Tuning: RLL done"),
                Tuple.Create(Vehicle.QUADROTOR, Severity.NOTICE, "Starting 3 Roll(s)"),
                Tuple.Create(Vehicle.GROUND_ROVER, Severity.INFO, "RTun: ATC_UNKNOWN_FF 38% complete"),
                Tuple.Create(Vehicle.GROUND_ROVER, Severity.INFO, "RTun: ATC_STR_RAT_FF % complete"),
                Tuple.Create(Vehicle.GROUND_ROVER, Severity.INFO, @"RTun: adjusted ATC_SPEED_I \-0.143 -> 0.024"),
                Tuple.Create(Vehicle.SUBMARINE, Severity.NOTICE, "Tuning: RLL done"),
            };
            foreach (var fixture in fixtures)
                Assert.IsNull(Resolve(fixture.Item1, fixture.Item2, fixture.Item3), fixture.Item3);
        }

        [TestMethod]
        public void RequiresTheExactFirmwareAndEnhancedCatalog()
        {
            const string text = "Tuning: DONE";
            Assert.IsNull(Resolve(Vehicle.QUADROTOR, Severity.NOTICE, text, hash: "00000000"));
            Assert.IsNull(Resolve(Vehicle.QUADROTOR, Severity.NOTICE, text, version: 0x040702ff));
            Assert.IsNull(Resolve(Vehicle.QUADROTOR, Severity.NOTICE, text, autopilot: Autopilot.PX4));
            Assert.IsNull(Resolve(Vehicle.QUADROTOR, Severity.NOTICE, text, culture: "zh-Hans"));
            Assert.IsNull(Resolve(Vehicle.QUADROTOR, Severity.NOTICE, text, culture: "en-US"));
        }

        [TestMethod]
        public void SpokenModeUsesKnownVocabularyAndVehicleContext()
        {
            var previous = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("zh-CN");
                Assert.AreEqual("定点盘旋", FlightModeDisplay.ForSpeech("Loiter", ModeMenuPresentation.Circle));
                Assert.AreEqual("定点悬停", FlightModeDisplay.ForSpeech("Loiter", ModeMenuPresentation.Hover));
                Assert.AreEqual("自动跟踪", FlightModeDisplay.ForSpeech("Auto", ModeMenuPresentation.Tracker));
                Assert.AreEqual("Settings", FlightModeDisplay.ForSpeech("Settings", ModeMenuPresentation.Unspecified));
            }
            finally
            {
                CultureInfo.CurrentUICulture = previous;
            }
        }
    }
}
