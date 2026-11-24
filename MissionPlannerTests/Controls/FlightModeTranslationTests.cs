using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
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
                        StringAssert.StartsWith(FlightModeDisplay.EnglishThenChinese(mode.Value),
                            original + " · ", firmware + ": " + original);
                        Assert.AreEqual(original, mode.Value);
                        Assert.AreEqual(originalKey, mode.Key);
                    }
                }
                Assert.AreEqual(0, missing.Count, "Untranslated real firmware modes: " + string.Join(", ", missing));
            }
            finally { CultureInfo.CurrentUICulture = previous; }
        }

        [DataTestMethod]
        [DataRow("zh-CN")]
        [DataRow("zh-Hans")]
        [DataRow("en-US")]
        public void TopBarPopupBilingualLabelsKeepRawSelectionPinAndFavorite(string language)
        {
            Exception error = null;
            var thread = new Thread(() =>
            {
                try
                {
                    CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);
                    var type = typeof(ToolStripVehicleState).Assembly.GetType("MissionPlanner.Controls.ModeDropdownPopup", true);
                    using (var popup = (Form)Activator.CreateInstance(type, true))
                    {
                        const string longMode = "Loiter To QLAND";
                        var rawModes = new List<KeyValuePair<int, string>>
                        {
                            new KeyValuePair<int, string>(25, longMode),
                            new KeyValuePair<int, string>(1, "Acro"),
                            new KeyValuePair<int, string>(65000, "Settings")
                        };
                        const BindingFlags hiddenInstance = BindingFlags.Instance | BindingFlags.NonPublic;
                        var panel = (FlowLayoutPanel)type.GetField("_modesPanel", hiddenInstance).GetValue(popup);
                        type.GetMethod("SetFltModes").Invoke(popup, new object[] { new List<string> { longMode } });
                        type.GetMethod("SetModes").Invoke(popup, new object[] { rawModes });

                        string[] titles = panel.Controls.OfType<Label>().Select(l => l.Text).ToArray();
                        CollectionAssert.AreEqual(new[] { UiText.Translate("Configured modes"), UiText.Translate("Flight modes") }, titles);
                        Assert.AreEqual(UiText.Translate("Pin All"), panel.Controls.OfType<CheckBox>().Single().Text);
                        var rows = panel.Controls.OfType<Panel>().Where(p => p.Controls.Count != 0).ToArray();
                        var labels = rows.Select(r => r.Controls.OfType<Label>().First()).ToArray();
                        CollectionAssert.AreEqual(new[] { longMode, "Acro", "Settings" }, labels.Select(l => (string)l.Tag).ToArray());
                        Assert.AreEqual(language == "zh-CN" ? longMode + " · 盘旋后垂直降落" : longMode, labels[0].Text);
                        Assert.AreEqual("Settings", labels[2].Text);
                        for (int i = 0; i < rows.Length; i++)
                        {
                            Assert.AreEqual(rows[i].Height, labels[i].Height);
                            Assert.IsTrue(rows[i].Controls.OfType<Label>().All(l => l.Bottom <= rows[i].Height));
                        }

                        var actual = new List<string>();
                        EventHandler<string> selection = (sender, raw) => actual.Add("select:" + raw);
                        EventHandler<string> pin = (sender, raw) => actual.Add("pin:" + raw);
                        EventHandler<string> favorite = (sender, raw) => actual.Add("favorite:" + raw);
                        type.GetEvent("ModeSelected").AddEventHandler(popup, selection);
                        type.GetEvent("PinToggled").AddEventHandler(popup, pin);
                        type.GetEvent("FavoriteToggled").AddEventHandler(popup, favorite);
                        var click = typeof(Control).GetMethod("OnClick", hiddenInstance);
                        click.Invoke(labels[0], new object[] { EventArgs.Empty });
                        click.Invoke(rows[0].Controls.OfType<Label>().Single(l => l.Text == "◇"), new object[] { EventArgs.Empty });
                        click.Invoke(rows[1].Controls.OfType<Label>().Single(l => l.Text == "☆"), new object[] { EventArgs.Empty });
                        CollectionAssert.AreEqual(new[] { "select:" + longMode, "pin:" + longMode, "favorite:Acro" }, actual.ToArray());
                        CollectionAssert.AreEqual(new[] { 25, 1, 65000 }, rawModes.Select(x => x.Key).ToArray());
                    }
                }
                catch (Exception ex) { error = ex; }
            }) { IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(30)), "Top-bar popup fixture timed out");
            if (error != null) Assert.Fail(error.ToString());
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
