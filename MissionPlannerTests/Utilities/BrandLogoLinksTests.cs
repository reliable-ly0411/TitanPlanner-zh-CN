using Microsoft.VisualStudio.TestTools.UnitTesting;
using MissionPlanner.Utilities;
using System.Collections.Generic;
using System.Windows.Forms;

namespace MissionPlannerTests.Utilities
{
    [TestClass]
    public class BrandLogoLinksTests
    {
        [TestMethod]
        public void NavigationAndThemeRefreshPreserveBothLogoSurfaces()
        {
            using (var menu = new ToolStrip())
            using (var left = new ToolStripButton { AutoSize = false, Height = 40 })
            using (var navigation = new ToolStripButton())
            using (var background = new System.Drawing.Bitmap(1, 1))
            {
                menu.Items.Add(left);
                menu.Items.Add(navigation);
                BrandLogoLinks.Configure(menu, left, url => { });
                var right = menu.Items[0];
                for (int page = 0; page < 4; page++)
                {
                    foreach (var logo in new ToolStripItem[] { left, right })
                    {
                        logo.BackColor = System.Drawing.Color.Black;
                        logo.BackgroundImage = background;
                        Assert.IsTrue(BrandLogoLinks.PreserveSurface(logo));
                        Assert.AreEqual(System.Drawing.Color.WhiteSmoke, logo.BackColor);
                        Assert.IsNull(logo.BackgroundImage);
                    }
                    Assert.IsFalse(BrandLogoLinks.PreserveSurface(navigation));
                }
            }
        }

        [TestMethod]
        public void LogoHasTwoAccessibleProportionalDestinations()
        {
            using (var menu = new ToolStrip())
            using (var left = new ToolStripButton { AutoSize = false, Height = 40 })
            {
                menu.Items.Add(left);
                var opened = new List<string>();
                BrandLogoLinks.Configure(menu, left, opened.Add);
                Assert.AreEqual(2, menu.Items.Count);
                var right = (ToolStripButton)menu.Items[0];
                Assert.AreEqual("MenuLocalizedGitHub", right.Name);
                Assert.AreEqual(left.Size, right.Size);
                Assert.AreEqual(ToolStripItemAlignment.Right, right.Alignment);
                Assert.AreEqual("ArduPilot", left.AccessibleName);
                Assert.AreEqual(BrandLogoLinks.ArduPilotUrl, left.AccessibleDescription);
                Assert.AreEqual(BrandLogoLinks.GitHubUrl, right.AccessibleDescription);
                Assert.IsTrue(left.Image.Width > left.Image.Height);
                Assert.IsTrue(left.Image.Height >= 24);
                right.PerformClick();
                right.PerformClick();
                CollectionAssert.AreEqual(new[] { BrandLogoLinks.GitHubUrl, BrandLogoLinks.GitHubUrl }, opened);
            }
        }
    }
}
