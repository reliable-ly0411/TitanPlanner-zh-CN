using System;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MissionPlanner.Controls;
using MissionPlanner.GCSViews.ConfigurationView;

namespace MissionPlannerTests.Utilities
{
    [TestClass]
    public class TuningFilterCaptionTests
    {
        [TestMethod]
        public void ReviewedFilterCaptionReachesTheNumericControlWithoutChangingItsValue()
        {
            Exception failure = null;
            var thread = new Thread(() =>
            {
                var previous = CultureInfo.CurrentUICulture;
                try
                {
                    const BindingFlags instance = BindingFlags.Instance | BindingFlags.NonPublic;
                    var type = typeof(ConfigArducopter);
                    var firmwareField = type.GetField("rcOptionFirmware", instance);
                    var presenceField = type.GetField("canPresentRcOption", instance);
                    var apply = type.GetMethod("ApplyFilterCaption", instance);
                    var tooltip = type.GetMethod("WithFilterTooltipCaption", instance);
                    using (var view = new ConfigArducopter())
                    using (var label = new Label { Text = "Filter", Width = 60 })
                    using (var number = new MavlinkNumericUpDown { Value = 17.5m, Width = 75 })
                    {
                        foreach (var culture in new[] { "zh-CN", "zh-Hans", "en-US", "zh-TW" })
                        foreach (var firmware in new[] { "ArduCopter2", "ArduPlane", "PX4" })
                        foreach (var axis in new[] { "PIT", "RLL", "YAW" })
                        foreach (var filter in new[] {
                            Tuple.Create("FLTE", "误差信号"),
                            Tuple.Create("FLTD", "微分项"),
                            Tuple.Create("FLTT", "目标信号")
                        })
                        {
                            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
                            firmwareField.SetValue(view, firmware);
                            var slot = axis + "_" + filter.Item1;
                            var key = (firmware == "ArduPlane" ? "Q_A_RAT_" : "ATC_RAT_") + slot;
                            number.ParamName = key;
                            string original = key + ":\nOriginal description";
                            Func<string, bool> present = candidate => candidate == key;
                            presenceField.SetValue(view, present);
                            apply.Invoke(view, new object[] { label, number, slot, present });

                            var expected = culture == "zh-CN" && firmware != "PX4"
                                ? filter.Item1 + " · " + filter.Item2 + "滤波频率 (Hz)" : null;
                            Assert.AreEqual(expected, number.AccessibleDescription);
                            Assert.AreEqual(expected == null ? original : expected + "\n" + original,
                                tooltip.Invoke(view, new object[] { number, original }));
                            Assert.AreEqual(original.Replace(key, "previous_key"), tooltip.Invoke(view,
                                new object[] { number, original.Replace(key, "previous_key") }));
                            presenceField.SetValue(view, new Func<string, bool>(_ => false));
                            Assert.AreEqual(original, tooltip.Invoke(view, new object[] { number, original }));
                            Assert.AreEqual(key, number.ParamName);
                            Assert.AreEqual(17.5m, number.Value);
                            Assert.AreEqual("Filter", label.Text);
                            Assert.AreEqual(75, number.Width);
                        }
                        foreach (var key in new[] { "ATC_RAT_PIT_FILT", "Q_A_RAT_PIT_FLTD", "ATC_RAT_ALT_FLTD", "ATC_RAT_PIT_FUTU" })
                        {
                            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("zh-CN");
                            firmwareField.SetValue(view, "ArduCopter2");
                            number.ParamName = key;
                            Func<string, bool> present = _ => true;
                            presenceField.SetValue(view, present);
                            apply.Invoke(view, new object[] { label, number, "PIT_FLTD", present });
                            Assert.IsNull(number.AccessibleDescription);
                            var original = key + ":\nMetadata";
                            Assert.AreEqual(original, tooltip.Invoke(view, new object[] { number, original }));
                        }
                    }
                }
                catch (Exception error) { failure = error; }
                finally { CultureInfo.CurrentUICulture = previous; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(25)), "Filter caption fixture timed out");
            if (failure != null) throw new Exception("Filter caption fixture failed", failure);
        }
    }
}
