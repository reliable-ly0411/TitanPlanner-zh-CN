using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MissionPlanner;
using Vehicle = MAVLink.MAV_TYPE;
using Severity = MAVLink.MAV_SEVERITY;
using Autopilot = MAVLink.MAV_AUTOPILOT;

namespace MissionPlannerTests.Utilities
{
    [TestClass]
    public class OfficialAppletSpeechTests
    {
        private static string Resolve(Vehicle vehicle, Severity severity, string raw,
            string culture = "zh-CN", string hash = "d9c5097c", uint version = 0x040703ff,
            Autopilot autopilot = Autopilot.ARDUPILOTMEGA)
        {
            var previous = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
                var type = typeof(MAVLinkInterface).Assembly.GetType("MissionPlanner.FirmwareScriptSpeech", true);
                return (string)type.GetMethod("Translate", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { autopilot, vehicle, version, Encoding.ASCII.GetBytes(hash), (byte)severity, raw });
            }
            finally { CultureInfo.CurrentUICulture = previous; }
        }

        [TestMethod]
        public void ExactPinnedAppletMessagesPreservePayload()
        {
            var cases = new[]
            {
                Tuple.Create(Vehicle.QUADROTOR, Severity.NOTICE, "Loaded MissionSelector.lua", "航线选择脚本已加载"),
                Tuple.Create(Vehicle.QUADROTOR, Severity.INFO, "loaded mission: missionM.txt", "已加载航线文件：missionM.txt"),
                Tuple.Create(Vehicle.FIXED_WING, Severity.INFO, "PLND: Target Lost", "降落目标已丢失"),
                Tuple.Create(Vehicle.HELICOPTER, Severity.CRITICAL, "LUA: IM_COL tune complete", "直升机总距调参完成"),
                Tuple.Create(Vehicle.FIXED_WING, Severity.NOTICE, "TerrAvoid: activated", "地形避障：已启用"),
                Tuple.Create(Vehicle.QUADROTOR, Severity.INFO, "LUA: Gimbal-Cam mode auto/manual", "云台相机模式：自动与手动"),
                Tuple.Create(Vehicle.QUADROTOR, Severity.NOTICE, "Arming controls RUNCAM recording", "解锁状态控制 RUNCAM 录像"),
                Tuple.Create(Vehicle.GROUND_ROVER, Severity.INFO, "Loaded BattEstimate for 2 batteries", "电池估算脚本已加载，电池数量 2"),
                Tuple.Create(Vehicle.GROUND_ROVER, Severity.ERROR, "Crash Actions: RELAY3 not configured", "坠机动作：RELAY3 未配置"),
                Tuple.Create(Vehicle.FIXED_WING, Severity.CRITICAL, "TerrAvoid: Quading 123.5 above: 100", "地形避障：Quading 高度 123.5 超过 100"),
            };
            foreach (var c in cases) Assert.AreEqual(c.Item4, Resolve(c.Item1,c.Item2,c.Item3), c.Item3);
        }

        [TestMethod]
        public void UnknownAndAmbiguousTextRemainsRaw()
        {
            var cases = new[]
            {
                Tuple.Create(Vehicle.QUADROTOR, Severity.INFO, "loaded mission: private.txt"),
                Tuple.Create(Vehicle.GROUND_ROVER, Severity.INFO, "PLND: Target Lost"),
                Tuple.Create(Vehicle.QUADROTOR, Severity.CRITICAL, "LUA: IM_COL tune complete"),
                Tuple.Create(Vehicle.HELICOPTER, Severity.CRITICAL, "LUA: params saved"),
                Tuple.Create(Vehicle.HELICOPTER, Severity.CRITICAL, "LUA: param set failed"),
                Tuple.Create(Vehicle.FIXED_WING, Severity.EMERGENCY, "Internal Error: example"),
                Tuple.Create(Vehicle.FIXED_WING, Severity.NOTICE, "TerrAvoid: activated\nextra"),
                Tuple.Create(Vehicle.QUADROTOR, Severity.ERROR, "Crash Actions: RELAY33 not configured"),
                Tuple.Create(Vehicle.QUADROTOR, Severity.WARNING, "Loaded MissionSelector.lua"),
            };
            foreach (var c in cases) Assert.IsNull(Resolve(c.Item1,c.Item2,c.Item3), c.Item3);
        }

        [TestMethod]
        public void RequiresFirmwareIdentityAndEnhancedChinese()
        {
            const string message = "Crash Actions: Recovered";
            Assert.IsNull(Resolve(Vehicle.QUADROTOR,Severity.NOTICE,message,hash:"other000"));
            Assert.IsNull(Resolve(Vehicle.QUADROTOR,Severity.NOTICE,message,version:0x040702ff));
            Assert.IsNull(Resolve(Vehicle.QUADROTOR,Severity.NOTICE,message,autopilot:Autopilot.PX4));
            Assert.IsNull(Resolve(Vehicle.QUADROTOR,Severity.NOTICE,message,culture:"en-US"));
            Assert.IsNull(Resolve(Vehicle.QUADROTOR,Severity.NOTICE,message,culture:"zh-Hans"));
        }
    }
}
