using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MissionPlanner.Utilities;

namespace MissionPlannerTests.Controls
{
    [TestClass]
    public class ThemeManagerTests
    {
        [TestMethod]
        public void PropertyGridThemePreservesSupportedColors()
        {
            Exception failure = null;
            var thread = new Thread(() =>
            {
                try
                {
                    using (var root = new Panel())
                    using (var splitter = new SplitContainer())
                    using (var grid = new PropertyGrid())
                    {
                        root.Controls.Add(splitter);
                        splitter.Panel1.Controls.Add(grid);
                        ThemeManager.ApplyThemeTo(root);
                        Assert.AreEqual(ThemeManager.BGColor.ToArgb(), grid.LineColor.ToArgb());
                        Assert.AreEqual(ThemeManager.ControlBGColor.ToArgb(), grid.ViewBackColor.ToArgb());
                        Assert.AreEqual(ThemeManager.TextColor.ToArgb(), grid.ViewForeColor.ToArgb());
                        Assert.AreEqual(ThemeManager.TextColor.ToArgb(), grid.CategoryForeColor.ToArgb());
                        var optional = typeof(PropertyGrid).GetProperty("CategorySplitterColor");
                        if (optional != null)
                            Assert.AreEqual(ThemeManager.BGColor.ToArgb(), ((Color)optional.GetValue(grid, null)).ToArgb());
                    }
                }
                catch (Exception ex) { failure = ex; }
            }) { IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(20)), "Property-grid theming timed out.");
            if (failure != null)
                Assert.Fail(failure.ToString());
        }
    }
}
