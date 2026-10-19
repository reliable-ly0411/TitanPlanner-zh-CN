using System.Drawing;

namespace MissionPlanner.Controls.Scene3D
{
    /// <summary>
    /// Colours the 3D map's own UI takes from the app theme.
    /// </summary>
    public static class ThemeColors
    {
        static readonly Color Fallback = Color.FromArgb(255, 145, 27);

        /// <summary>The theme's button colour, as used by the Settings dialog's buttons.</summary>
        public static Color Accent
        {
            get
            {
                var color = MissionPlanner.Utilities.ThemeManager.ButBG;
                return color.A > 0 ? Color.FromArgb(255, color) : Fallback;
            }
        }
    }
}
