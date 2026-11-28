using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MissionPlanner.Controls;
using MissionPlanner.GCSViews.ConfigurationView;

namespace MissionPlannerTests.Controls
{
    [TestClass]
    public class ConfigTuningDisplayTests
    {
        [TestMethod]
        public void TuneOptionsStageOriginalKeysAndRetainTheOriginalMetadata()
        {
            RunOnSta(() =>
            {
                const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
                var type = typeof(ConfigArducopter);
                var format = type.GetMethod("FormatCopterTuneOption", BindingFlags.Static | BindingFlags.NonPublic);
                foreach (var language in new[] { "zh-CN", "zh-Hans", "en-US" })
                {
                    CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);
                    using (var host = new Form())
                    using (var view = new ConfigArducopter())
                    {
                        host.Controls.Add(view);
                        var combo = (MavlinkComboBox)type.GetField("TUNE", fields).GetValue(view);
                        type.GetField("rcOptionFirmware", fields).SetValue(view, "ArduCopter2");
                        type.GetField("canPresentRcOption", fields).SetValue(view, new Func<string, bool>(key => key == "TUNE"));
                        var options = new List<KeyValuePair<int, string>>
                        {
                            new KeyValuePair<int, string>(0, "None"),
                            new KeyValuePair<int, string>(49, "Rate Roll kP"),
                            new KeyValuePair<int, string>(253, "Future option")
                        };
                        var parameters = new MAVLink.MAVLinkParamList();
                        parameters.Add(new MAVLink.MAVLinkParam("TUNE", 0, MAVLink.MAV_PARAM_TYPE.INT32));
                        combo.setup(options, "TUNE", parameters);
                        type.GetField("startup", fields).SetValue(view, false);
                        host.Show();
                        combo.SelectedValue = 49;

                        var enhanced = language == "zh-CN";
                        var expected = enhanced ? "横滚角速度 P 增益 (Rate Roll kP)" : "Rate Roll kP";
                        Assert.AreEqual(expected, format.Invoke(null, new object[] { "Rate Roll kP" }));
                        Assert.AreEqual(enhanced ? DrawMode.OwnerDrawFixed : DrawMode.Normal, combo.DrawMode);
                        if (enhanced)
                            Assert.AreEqual(expected, combo.AccessibleDescription);
                        Assert.AreEqual("Future option", format.Invoke(null, new object[] { "Future option" }));
                        Assert.AreEqual("TUNE", combo.ParamName);
                        Assert.AreEqual(options[1], combo.SelectedItem);
                        Assert.AreEqual("Rate Roll kP", combo.GetItemText(combo.SelectedItem));

                        var pending = (Hashtable)type.GetField("changes", fields).GetValue(view);
                        Assert.AreEqual(49f, pending["TUNE"]);
                        Assert.AreEqual(0f, parameters["TUNE"].Value, "Selection should only stage the change.");
                    }
                }
            });
        }

        [TestMethod]
        public void WarningPreservesParameterNamesAndUnitsInAllThreeLanguages()
        {
            RunOnSta(() =>
            {
                var format = typeof(ConfigArducopter).GetMethod("FormatUnitChangeWarning",
                    BindingFlags.Static | BindingFlags.NonPublic);
                const string prefix = "ATC_ACCEL_R_MAX → ATC_ACC_R_MAX: ";
                const string original = "units changed from cm/s/s to m/s/s";
                foreach (var language in new[] { "zh-CN", "zh-Hans", "en-US" })
                {
                    CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);
                    string expected = language == "zh-CN" ? "单位由 cm/s/s 改为 m/s/s" : original;
                    Assert.AreEqual(prefix + expected, format.Invoke(null, new object[] { prefix + original }));
                    Assert.AreEqual(prefix + "unknown detail",
                        format.Invoke(null, new object[] { prefix + "unknown detail" }));
                }
            });
        }

        [TestMethod]
        public void BasicTuningTextFitsAboveTheSliderWithoutChangingTheParameter()
        {
            RunOnSta(() =>
            {
                using (var host = new Form())
                using (var range = new RangeControl("ATC_RAT_RLL_P",
                    string.Concat(Enumerable.Repeat("响应迟缓时向右调，响应过于敏感时向左调。", 12)),
                    "横滚／俯仰灵敏度：", 0.01f, 1, 0, 1, "0.25"))
                {
                    host.Controls.Add(range);
                    int events = 0;
                    range.ValueChanged += (sender, name, value) => events++;
                    range.Width = 640;
                    range.FitTextAboveInputs();
                    int wideHeight = range.Height;
                    range.Width = 320;
                    range.FitTextAboveInputs();
                    Assert.IsTrue(range.Height > wideHeight);
                    int narrowHeight = range.Height;
                    using (var largerFont = new Font(range.Font.FontFamily, range.Font.Size * 1.5f))
                    {
                        range.Font = largerFont;
                        range.FitTextAboveInputs();
                        Assert.IsTrue(range.Height > narrowHeight);
                    }
                    Assert.AreEqual("ATC_RAT_RLL_P", range.Name);
                    Assert.AreEqual("0.25", range.Value);
                    Assert.AreEqual(0, events);
                }
            });
        }

        private static void RunOnSta(Action action)
        {
            Exception failure = null;
            var thread = new Thread(() =>
            {
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                try { action(); }
                catch (Exception error) { failure = error; }
            }) { IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(25)), "Tuning display test timed out.");
            if (failure != null)
                throw new Exception("Tuning display test failed.", failure);
        }
    }
}
