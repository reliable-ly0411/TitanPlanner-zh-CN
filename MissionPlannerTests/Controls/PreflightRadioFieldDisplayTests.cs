using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using MissionPlanner.Controls.PreFlight;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MissionPlannerTests.Controls
{
    [TestClass]
    public class PreflightRadioFieldDisplayTests
    {
        public sealed class RadioSample
        {
            public byte txbuffer { get; set; } = 67;
            public ushort rxerrors { get; set; } = 24;
            public ushort fixedp { get; set; } = 180;
            public int FutureField { get; set; } = 12;
        }

        [TestMethod]
        public void RadioStatusFieldsOnlyChangeEnhancedChineseEditorCaptions()
        {
            Exception failure = null;
            var thread = new Thread(() =>
            {
                var previous = CultureInfo.CurrentUICulture;
                var oldSource = CheckListItem.defaultsrc;
                try
                {
                    CheckListItem.defaultsrc = new RadioSample();
                    var fields = new[] {
                        Tuple.Create("txbuffer", "数传发送缓冲区剩余（%）", 67d),
                        Tuple.Create("rxerrors", "数传接收错误次数", 24d),
                        Tuple.Create("fixedp", "数传已纠错的数据包数", 180d)
                    };
                    foreach (var locale in new[] { "zh-CN", "zh-Hans", "en-US", "zh-TW" })
                    {
                        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(locale);
                        using (var input = new CheckListInput(null))
                        using (var window = new Form())
                        {
                            window.Controls.Add(input);
                            var item = new CheckListItem {
                                Name = "txbuffer", Text = "{name}: {value}", Description = "My description",
                                ConditionType = CheckListItem.Conditional.GTEQ, TriggerValue = 15,
                                TrueColor = "Lime", FalseColor = "Red"
                            };
                            input.CheckListItem = item;
                            var combo = (ComboBox)input.Controls["CMB_Source"];
                            typeof(CheckListInput).GetMethod("SetupSourcePresentation", BindingFlags.NonPublic | BindingFlags.Instance)
                                .Invoke(input, null);
                            combo.DataSource = item.GetOptions();
                            window.Show();
                            Application.DoEvents();
                            foreach (var field in fields)
                            {
                                combo.SelectedItem = field.Item1;
                                Assert.AreEqual(field.Item1, combo.SelectedItem);
                                Assert.AreEqual(locale == "zh-CN" ? field.Item1 + " (" + field.Item2 + ")" : field.Item1,
                                    combo.GetItemText(combo.SelectedItem));
                                Assert.AreEqual(field.Item1, item.Name);
                                Assert.AreEqual(field.Item3, item.GetValue);
                            }
                            Assert.AreEqual("FutureField", combo.GetItemText("FutureField"));
                            combo.SelectedItem = "rxerrors";
                            var serializer = new XmlSerializer(typeof(CheckListItem));
                            using (var buffer = new MemoryStream())
                            {
                                serializer.Serialize(buffer, item);
                                buffer.Position = 0;
                                var restored = (CheckListItem)serializer.Deserialize(buffer);
                                Assert.AreEqual("rxerrors", restored.Name);
                                Assert.AreEqual(item.Text, restored.Text);
                                Assert.AreEqual(item.Description, restored.Description);
                                Assert.AreEqual(item.ConditionType, restored.ConditionType);
                                Assert.AreEqual(item.TriggerValue, restored.TriggerValue);
                                Assert.AreEqual(item.TrueColor, restored.TrueColor);
                                Assert.AreEqual(item.FalseColor, restored.FalseColor);
                            }
                        }
                    }
                }
                catch (Exception error) { failure = error; }
                finally
                {
                    CheckListItem.defaultsrc = oldSource;
                    CultureInfo.CurrentUICulture = previous;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(25)), "Checklist editor fixture timed out");
            if (failure != null) throw new Exception("Checklist editor fixture failed", failure);
        }
    }
}
