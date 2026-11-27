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
            public uint vibeclip0 { get; set; } = 2;
            public uint vibeclip1 { get; set; } = 41;
            public uint vibeclip2 { get; set; } = 238;
            public int FutureField { get; set; } = 12;
        }

        [TestMethod]
        public void RpmFieldsUseReviewedCaptionsWithoutChangingKeysOrReadback()
        {
            CheckFields(new[]
            {
                Tuple.Create("rpm1", "转速传感器 1 读数（rpm）", 1240.5d),
                Tuple.Create("rpm2", "转速传感器 2 读数（rpm）", 800.25d),
            });
        }

        [TestMethod]
        public void VibrationClippingFieldsUseReviewedCaptionsWithoutChangingKeysOrReadback()
        {
            CheckFields(new[]
            {
                Tuple.Create("vibeclip0", "第 1 个加速度计削波次数", 2d),
                Tuple.Create("vibeclip1", "第 2 个加速度计削波次数", 41d),
                Tuple.Create("vibeclip2", "第 3 个加速度计削波次数", 238d),
            });
        }

        private static void CheckFields(Tuple<string, string, double>[] samples)
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
                            var item = new CheckListItem { Name = samples[0].Item1, Description = "Synthetic text" };
                            input.CheckListItem = item;
                            window.Controls.Add(input);
                            var combo = (ComboBox)input.Controls["CMB_Source"];
                            typeof(CheckListInput).GetMethod("SetupSourcePresentation", BindingFlags.NonPublic | BindingFlags.Instance)
                                .Invoke(input, null);
                            combo.DataSource = item.GetOptions();
                            window.Show();
                            Application.DoEvents();
                            foreach (var sample in samples)
                            {
                                combo.SelectedItem = sample.Item1;
                                combo.DroppedDown = true;
                                Application.DoEvents();
                                Assert.AreEqual(sample.Item1, combo.SelectedItem);
                                string expected = locale == "zh-CN" ? sample.Item1 + " (" + sample.Item2 + ")" : sample.Item1;
                                Assert.AreEqual(expected, combo.GetItemText(combo.SelectedItem));
                                Assert.IsTrue(combo.DroppedDown && combo.DropDownWidth >= TextRenderer.MeasureText(expected, combo.Font).Width);
                                Assert.AreEqual(sample.Item1, item.Name);
                                Assert.AreEqual(sample.Item3, item.GetValue);
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
            if (failure != null) throw new Exception("Checklist sensor display fixture failed", failure);
        }
    }
}
