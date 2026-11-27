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
    public class PreflightVdopFieldDisplayTests
    {
        public sealed class GpsSample
        {
            public float gpsvdop { get; set; } = 1.75f;
            public int FutureField { get; set; } = 12;
        }

        [TestMethod]
        public void VdopUsesTheReviewedCaptionWithoutChangingTheChecklistField()
        {
            Exception failure = null;
            var thread = new Thread(() =>
            {
                var source = CheckListItem.defaultsrc;
                try
                {
                    CheckListItem.defaultsrc = new GpsSample();
                    foreach (var locale in new[] { "zh-CN", "zh-Hans", "en-US" })
                    {
                        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(locale);
                        using (var input = new CheckListInput(null))
                        using (var window = new Form())
                        {
                            var item = new CheckListItem { Name = "gpsvdop", Description = "Synthetic text" };
                            input.CheckListItem = item;
                            window.Controls.Add(input);
                            var combo = (ComboBox)input.Controls["CMB_Source"];
                            typeof(CheckListInput).GetMethod("SetupSourcePresentation", BindingFlags.NonPublic | BindingFlags.Instance)
                                .Invoke(input, null);
                            combo.DataSource = item.GetOptions();
                            window.Show();
                            Application.DoEvents();
                            combo.SelectedItem = "gpsvdop";
                            combo.DroppedDown = true;
                            Application.DoEvents();
                            Assert.AreEqual("gpsvdop", combo.SelectedItem);
                            string expected = locale == "zh-CN" ? "gpsvdop (GPS 垂直精度因子)" : "gpsvdop";
                            Assert.AreEqual(expected, combo.GetItemText(combo.SelectedItem));
                            Assert.IsTrue(combo.DroppedDown && combo.DropDownWidth >= TextRenderer.MeasureText(expected, combo.Font).Width);
                            Assert.AreEqual("gpsvdop", item.Name);
                            Assert.AreEqual(1.75d, item.GetValue);
                            Assert.AreEqual("Synthetic text", item.Description);
                            Assert.AreEqual("FutureField", combo.GetItemText("FutureField"));
                            combo.DroppedDown = false;
                        }
                    }
                }
                catch (Exception ex) { failure = ex; }
                finally { CheckListItem.defaultsrc = source; }
            }) { IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(25)), "Checklist display fixture timed out");
            if (failure != null) throw new Exception("Checklist VDOP display fixture failed", failure);
        }
    }
}
