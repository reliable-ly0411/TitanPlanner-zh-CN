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
        public void BilingualLabelReservesWidthAndScrollsAtNarrowSizes()
        {
            Exception failure = null;
            var thread = new Thread(() =>
            {
                try
                {
                    using (var status = new StatusControl())
                    using (var chart = new DisplayThisForm(_ => false, (sender, e) => { }))
                    {
                        foreach (var control in new Control[] { status, chart })
                        {
                            var type = control.GetType();
                            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                            var list = (IList)type.GetField("_allItems", flags).GetValue(control);
                            var itemType = list.GetType().GetGenericArguments()[0];
                            var item = Activator.CreateInstance(itemType);
                            itemType.GetProperty("Name").SetValue(item, "airspeed");
                            itemType.GetProperty("DisplayName").SetValue(item, "airspeed (空速 / Airspeed long bilingual label)");
                            itemType.GetProperty("Value").SetValue(item, "12.5");
                            itemType.GetProperty("Group")?.SetValue(item, "Position");
                            itemType.GetProperty("GroupLabel")?.SetValue(item, "Position (位置)");
                            list.Add(item);
                            type.GetField("_filteredItems", flags).SetValue(control, list);
                            foreach (var width in new[] { 320, 640, 1280 })
                            {
                                control.Size = new Size(width, 480);
                                control.CreateControl();
                                type.GetMethod("UpdateScrollSize", flags).Invoke(control, null);
                                var panel = (Panel)type.GetField("_contentPanel", flags).GetValue(control);
                                using (var graphics = panel.CreateGraphics())
                                using (var font = new Font(control.Font, control is StatusControl ? FontStyle.Bold : FontStyle.Regular))
                                {
                                    int nameWidth = (int)type.GetField("_nameWidth", flags).GetValue(control);
                                    var measured = graphics.MeasureString((string)itemType.GetProperty("DisplayName").GetValue(item), font).Width;
                                    Assert.IsTrue(nameWidth >= Math.Ceiling(measured) + 8);
                                    Assert.IsTrue(panel.AutoScrollMinSize.Width > nameWidth);
                                }
                            }
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
