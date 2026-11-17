using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
                var missing = new List<string>();
                foreach (var firmware in new[] { Firmwares.ArduCopter2, Firmwares.ArduPlane,
                    Firmwares.ArduRover, Firmwares.PX4, Firmwares.ArduTracker })
                {
                    var modes = Common.getModesList(firmware);
                    Assert.IsNotNull(modes, firmware.ToString());
                    Assert.IsTrue(modes.Count > 0, firmware.ToString());
                    foreach (var mode in modes)
                    {
                        var original = mode.Value;
                        var originalKey = mode.Key;
                        if (original == UiText.Translate(original))
                            missing.Add(firmware + ": " + original + " (" + originalKey + ")");
                        Assert.AreEqual(original, mode.Value);
                        Assert.AreEqual(originalKey, mode.Key);
                    }
                }
                Assert.AreEqual(0, missing.Count, "Untranslated real firmware modes: " + string.Join(", ", missing));
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

        [DataTestMethod]
        [DataRow(Firmwares.ArduPlane, 26, "AUTOLAND", "Autoland", "自动着陆")]
        [DataRow(Firmwares.ArduRover, 8, "DOCK", "Dock", "自动停靠")]
        public void RecentFirmwareModesUseRealListsAndKeepValuesAcrossThreeLanguages(
            Firmwares firmware, int value, string protocolName, string formatted, string chinese)
        {
            var previous = CultureInfo.CurrentUICulture;
            try
            {
                Assert.AreEqual(26, (int)MAVLink.PLANE_MODE.AUTOLAND);
                Assert.AreEqual(8, (int)MAVLink.ROVER_MODE.DOCK);
                var modes = Common.getModesList(firmware);
                Assert.IsNotNull(modes);
                var mode = modes.Single(item => item.Key == value);
                Assert.AreEqual(formatted, mode.Value);
                Assert.AreEqual(mode.Value, Common.FormatModeName(protocolName));
                foreach (var language in new[] { "en-US", "zh-Hans", "zh-CN" })
                {
                    CultureInfo.CurrentUICulture = new CultureInfo(language);
                    bool enhanced = language == "zh-CN";
                    var display = FlightModeDisplay.FromMode(mode.Value);
                    Assert.AreEqual(enhanced ? chinese : mode.Value, display.Primary, language);
                    Assert.AreEqual(enhanced ? mode.Value : "", display.Secondary, language);
                    Assert.AreEqual(enhanced, display.IsBilingual, language);
                    Assert.AreEqual(value, mode.Key);
                    Assert.AreEqual(formatted, mode.Value);
                }
            }
            finally { CultureInfo.CurrentUICulture = previous; }
        }
    }
}
