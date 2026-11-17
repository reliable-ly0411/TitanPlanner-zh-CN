using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MissionPlanner.ArduPilot;
using MissionPlanner.Controls;
using MissionPlanner.Utilities;

namespace MissionPlannerTests.Controls
{
    [TestClass]
    public class FlightModeTranslationTests
    {
        [TestMethod]
        public void RealFirmwareModeListsHaveEnhancedTranslationsWithoutChangingValues()
        {
            var previous = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo("zh-CN");
                foreach (var firmware in new[] { Firmwares.ArduCopter2, Firmwares.ArduPlane,
                    Firmwares.ArduRover, Firmwares.PX4, Firmwares.ArduTracker })
                {
                    var modes = Common.getModesList(firmware);
                    Assert.IsNotNull(modes, firmware.ToString());
                    Assert.IsTrue(modes.Count > 0, firmware.ToString());
                    foreach (var mode in modes)
                    {
                        var original = mode.Value;
                        Assert.AreNotEqual(original, UiText.Translate(original), firmware + ": " + original);
                        Assert.AreEqual(original, mode.Value);
                    }
                }
            }
            finally { CultureInfo.CurrentUICulture = previous; }
        }

        [TestMethod]
        public void MixedCaseModeNamesMatchRealFormatter()
        {
            var previous = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo("zh-CN");
                foreach (var name in new[] { "AltHold", "PosHold", "SmartRTL", "Guided_NoGPS", "Loiter to QLand" })
                {
                    var formatted = Common.FormatModeName(name);
                    Assert.AreNotEqual(formatted, UiText.Translate(formatted), formatted);
                }
                Assert.AreEqual("重启飞控", UiText.Translate("Preflight_Reboot_Shutdown"));
                Assert.AreEqual("重启飞控 (Preflight_Reboot_Shutdown)", FlightDataActions.DisplayCommandLabel("Preflight_Reboot_Shutdown"));
                Assert.AreEqual("Unknown_Plugin_Action", FlightDataActions.DisplayCommandLabel("Unknown_Plugin_Action"));
                Assert.AreEqual("持续悬停/盘旋", UiText.Translate("Loiter_Unlim"));
            }
            finally { CultureInfo.CurrentUICulture = previous; }
        }

        [TestMethod]
        public void CurrentModePresentationKeepsTurtleAndFlipValuesDistinct()
        {
            var previous = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo("zh-CN");
                Assert.AreEqual(28, (int)MAVLink.COPTER_MODE.TURTLE);
                Assert.AreEqual(14, (int)MAVLink.COPTER_MODE.FLIP);
                var raw = Common.FormatModeName("TURTLE");
                var display = FlightModeDisplay.FromMode(raw);
                Assert.AreEqual("Turtle", raw);
                Assert.AreEqual("翻正自救", display.Primary);
                Assert.AreEqual(raw, display.Secondary);
                Assert.AreEqual("翻转", FlightModeDisplay.FromMode("Flip").Primary);
            }
            finally { CultureInfo.CurrentUICulture = previous; }
        }
    }
}
