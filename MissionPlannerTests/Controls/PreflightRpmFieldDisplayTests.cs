using System;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MissionPlanner.Controls.PreFlight;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MissionPlannerTests.Controls
{
    [TestClass]
    public class PreflightRpmFieldDisplayTests
    {
        public sealed class SensorSample
        {
            public float rpm1 { get; set; } = 1240.5f;
            public float rpm2 { get; set; } = 800.25f;
            public int FutureField { get; set; } = 12;
        }

        [TestMethod]
        public void RpmFieldsUseReviewedCaptionsWithoutChangingKeysOrReadback()
        {
            Exception failure = null;
            var thread = new Thread(() =>
            {
                var source = CheckListItem.defaultsrc;
                try
                {
                    CheckListItem.defaultsrc = new SensorSample();
                    foreach (var locale in new[] { "zh-CN", "zh-Hans", "en-US" })
                    {
                        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(locale);
                        using (var input = new CheckListInput(null))
                        using (var window = new Form())
                        {
                            var item = new CheckListItem { Name = "rpm1", Description = "Synthetic text" };
                            input.CheckListItem = item;
                            window.Controls.Add(input);
                            var combo = (ComboBox)input.Controls["CMB_Source"];
                            typeof(CheckListInput).GetMethod("SetupSourcePresentation", BindingFlags.NonPublic | BindingFlags.Instance)
                                .Invoke(input, null);
                            combo.DataSource = item.GetOptions();
                            window.Show();
                            Application.DoEvents();
                            var samples = new[]
                            {
                                new { Key = "rpm1", Caption = "转速传感器 1 读数（rpm）", Value = 1240.5d },
                                new { Key = "rpm2", Caption = "转速传感器 2 读数（rpm）", Value = 800.25d },
                            };
                            foreach (var sample in samples)
                            {
                                combo.SelectedItem = sample.Key;
                                combo.DroppedDown = true;
                                Application.DoEvents();
                                Assert.AreEqual(sample.Key, combo.SelectedItem);
                                string expected = locale == "zh-CN" ? sample.Key + " (" + sample.Caption + ")" : sample.Key;
                                Assert.AreEqual(expected, combo.GetItemText(combo.SelectedItem));
                                Assert.IsTrue(combo.DroppedDown && combo.DropDownWidth >= TextRenderer.MeasureText(expected, combo.Font).Width);
                                Assert.AreEqual(sample.Key, item.Name);
                                Assert.AreEqual(sample.Value, item.GetValue);
                                Assert.AreEqual("Synthetic text", item.Description);
                                combo.DroppedDown = false;
                            }
                            Assert.AreEqual("FutureField", combo.GetItemText("FutureField"));
                        }
                    }
                }
                catch (Exception ex) { failure = ex; }
                finally { CheckListItem.defaultsrc = source; }
            }) { IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(25)), "Checklist display fixture timed out");
            if (failure != null) throw new Exception("Checklist RPM display fixture failed", failure);
        }
    }
}
