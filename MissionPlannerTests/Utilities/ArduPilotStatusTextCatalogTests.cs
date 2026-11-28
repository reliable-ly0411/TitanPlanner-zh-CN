using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MissionPlanner;
using Autopilot = MAVLink.MAV_AUTOPILOT;
using Severity = MAVLink.MAV_SEVERITY;
using Vehicle = MAVLink.MAV_TYPE;

namespace MissionPlannerTests.Utilities
{
    [TestClass]
    public class ArduPilotStatusTextCatalogTests
    {
        private static string Translate(Vehicle vehicle, uint version, string hash, Severity severity, string raw,
            string language = "zh-CN", Autopilot autopilot = Autopilot.ARDUPILOTMEGA)
        {
            var previous = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);
                var catalog = typeof(MAVLinkInterface).Assembly.GetType("MissionPlanner.ArduPilotStatusTextCatalog", true);
                return (string)catalog.GetMethod("Translate", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { autopilot, vehicle, version, Encoding.ASCII.GetBytes(hash),
                        (byte)severity, raw });
            }
            finally
            {
                CultureInfo.CurrentUICulture = previous;
            }
        }

        [TestMethod]
        public void ArduPilotCatalogUsesVerifiedBuildsAcrossSixFamilies()
        {
            const uint latest = 0x040703ff;
            Assert.AreEqual("偏航不平衡 17%", Translate(Vehicle.HELICOPTER, latest, "d9c5097c",
                Severity.EMERGENCY, "Yaw Imbalance 17%"));
            Assert.AreEqual("高度辅助 -3.2 米", Translate(Vehicle.VTOL_QUADROTOR, latest, "d9c5097c",
                Severity.WARNING, "Alt assist -3.2m"));
            Assert.AreEqual("电机测试：PWM 值 17 过高", Translate(Vehicle.SURFACE_BOAT, 0x040603ff, "3fc7011a",
                Severity.CRITICAL, "Motor Test: pwm (17) too high"));
            Assert.AreEqual("深度传感器故障！", Translate(Vehicle.SUBMARINE, 0x040507ff, "30257f01",
                Severity.CRITICAL, "Depth sensor error!"));
            Assert.AreEqual("目标偏航角 -3.252525，俯仰角 2.000000", Translate(Vehicle.ANTENNA_TRACKER,
                latest, "d9c5097c", Severity.INFO, "target_yaw=-3.252525 target_pitch=2.000000"));
            Assert.AreEqual("正在设置 FishBlimp。", Translate(Vehicle.AIRSHIP, 0x04080000, "6034266b",
                Severity.INFO, "Setting up FishBlimp."));
        }

        [TestMethod]
        public void ArduPilotWildcardsPreserveValuesAndValidateTheWireShape()
        {
            const uint latest = 0x040703ff;
            const string hash = "d9c5097c";
            const string low = "Battery 2 is low 14.25V used 382 mAh";
            Assert.AreEqual("电池 2电量低，电压 14.25 伏，已用 382 毫安时",
                Translate(Vehicle.QUADROTOR, latest, hash, Severity.WARNING, low));
            Assert.IsNull(Translate(Vehicle.QUADROTOR, latest, hash, Severity.WARNING,
                "Battery 27 is low 14.25V used 382 mAh"));
            Assert.IsNull(Translate(Vehicle.QUADROTOR, latest, hash, Severity.WARNING,
                "Battery 2 is unstable 14.25V used 382 mAh"));
            Assert.IsNull(Translate(Vehicle.QUADROTOR, latest, hash, Severity.WARNING,
                "Battery 2 is low 14,25V used 382 mAh"));
            Assert.IsNull(Translate(Vehicle.QUADROTOR, latest, hash, Severity.WARNING, low + "\nnext"));
            Assert.IsNull(Translate(Vehicle.QUADROTOR, latest, hash, Severity.EMERGENCY, "Yaw Imbalance 17*"));
        }

        [TestMethod]
        public void ArduPilotCatalogRejectsMismatchedIdentitySeverityAndCulture()
        {
            const string loss = "Depth sensor error!";
            Assert.IsNull(Translate(Vehicle.QUADROTOR, 0x040703ff, "d9c5097c", Severity.CRITICAL, loss));
            Assert.IsNull(Translate(Vehicle.SUBMARINE, 0x040703ff, "00000000", Severity.CRITICAL, loss));
            Assert.IsNull(Translate(Vehicle.SUBMARINE, 0x040703ff, "30257f01", Severity.CRITICAL, loss));
            Assert.IsNull(Translate(Vehicle.SUBMARINE, 0x040703ff, "d9c5097c", Severity.INFO, loss));
            Assert.IsNull(Translate(Vehicle.SUBMARINE, 0x040703ff, "d9c5097c", Severity.CRITICAL, loss, "zh-Hans"));
            Assert.IsNull(Translate(Vehicle.SUBMARINE, 0x040703ff, "d9c5097c", Severity.CRITICAL, loss,
                autopilot: Autopilot.PX4));
            Assert.IsNull(Translate(Vehicle.AIRSHIP, 0x040800ff, "6034266b", Severity.INFO, "Setting up FishBlimp."));
        }
    }
}
