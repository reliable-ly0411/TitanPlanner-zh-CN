using System.Globalization;
using System.Resources;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MissionPlanner.Attributes;
using MissionPlanner.GCSViews;

namespace MissionPlanner.Utilities.Tests
{
    [TestClass]
    public class ChineseDisplayTests
    {
        [TestMethod]
        public void EnhancedChineseDisplayDoesNotChangeProtocolKeys()
        {
            var before = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("zh-CN");
                Assert.AreEqual("电池功率 (W)", new DisplayTextAttribute("Bat Watts").Text);
                Assert.AreEqual("已用电量 (mAh)", new DisplayTextAttribute("Bat used (mah)").Text);
                Assert.AreEqual("相对高度", UiText.Translate("Relative"));
                Assert.AreEqual("battery_usedmah", UiText.Translate("battery_usedmah"));
                Assert.AreEqual("GLOBAL_RELATIVE_ALT", MAVLink.MAV_FRAME.GLOBAL_RELATIVE_ALT.ToString());
                Assert.AreEqual((int)MAVLink.MAV_FRAME.GLOBAL_RELATIVE_ALT, (int)FlightPlanner.altmode.Relative);
                var flight = new ResourceManager("MissionPlanner.GCSViews.FlightPlanner", typeof(FlightPlanner).Assembly);
                Assert.AreEqual("高度基准", flight.GetString("Frame.HeaderText"));
                var vibration = new ResourceManager("MissionPlanner.Controls.Vibration", typeof(FlightPlanner).Assembly);
                Assert.AreEqual("X", vibration.GetString("label3.Text"));
                Assert.AreEqual("加速度计削波", vibration.GetString("label8.Text"));
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
                Assert.AreEqual("Bat Watts", new DisplayTextAttribute("Bat Watts").Text);
                Assert.AreEqual("Relative", UiText.Translate("Relative"));
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("zh-Hans");
                Assert.AreEqual("Relative", UiText.Translate("Relative"));
            }
            finally { CultureInfo.CurrentUICulture = before; }
        }
    }
}
