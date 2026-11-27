using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MissionPlanner.Controls;

namespace MissionPlannerTests.Controls
{
    [TestClass]
    public class AuxiliaryOptionCaptionTests
    {
        private static string Caption(string firmware, int id, string original, bool enhanced)
        {
            var format = typeof(FlightDataActions).GetMethod("FormatAuxiliaryOption",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(format);
            return (string)format.Invoke(null, new object[] { firmware, id, original, enhanced });
        }

        [DataTestMethod]
        [DataRow("ArduCopter2", "Lost Copter Sound", "多旋翼寻机提示音")]
        [DataRow("ArduRover", "Lost Rover Sound", "车船寻回提示音")]
        [DataRow("ArduPlane", "Lost Plane Sound", "固定翼寻机提示音")]
        public void LostSoundCaptionsMatchOnlyTheExactFirmwareIdAndEnglish(string firmware, string original, string chinese)
        {
            Assert.AreEqual(original + " · " + chinese, Caption(firmware, 30, original, true));
            Assert.AreEqual(original, Caption(firmware, 30, original, false));
            Assert.AreEqual(original, Caption(firmware, 29, original, true));
            Assert.AreEqual(original, Caption(firmware, 31, original, true));
            foreach (var other in new[] { "ArduCopter2", "ArduRover", "ArduPlane", "ArduSub", "PX4", "Unknown", null })
                if (other != firmware)
                    Assert.AreEqual(original, Caption(other, 30, original, true), other);
            Assert.AreEqual("Lost Vehicle Sound", Caption(firmware, 30, "Lost Vehicle Sound", true));
            Assert.AreEqual(original.ToLowerInvariant(), Caption(firmware, 30, original.ToLowerInvariant(), true));
        }

        [TestMethod]
        public void ExistingAuxiliaryMappingsAndUnknownValuesStayUnchanged()
        {
            Assert.AreEqual("Do Nothing · 无操作", Caption("ArduCopter2", 0, "Do Nothing", true));
            Assert.AreEqual("ACRO Mode · 转向率与速度控制", Caption("ArduRover", 52, "ACRO Mode", true));
            Assert.AreEqual("RunCam Control · RunCam 控制", Caption("ArduPlane", 78, "RunCam Control", true));
            Assert.AreEqual("Scripting16 · 脚本功能 16", Caption("ArduCopter2", 315, "Scripting16", true));
            Assert.AreEqual(" ", Caption("ArduCopter2", 30, " ", true));
            Assert.IsNull(Caption("ArduCopter2", 30, null, true));
        }
    }
}
