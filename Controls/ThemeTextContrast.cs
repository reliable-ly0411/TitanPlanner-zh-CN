using System;
using System.Drawing;

namespace MissionPlanner.Controls
{
    internal static class ThemeTextContrast
    {
        internal static Color Choose(Color preferred, Color foreground, Color background)
        {
            const double minimum = 4.5; // Normal-size text, including bold UI labels.
            double bg = RelativeLuminance(background.R, background.G, background.B);
            if (ContrastRatio(preferred, background, bg) >= minimum)
                return preferred;
            if (ContrastRatio(foreground, background, bg) >= minimum)
                return foreground;

            // A custom theme may provide neither a readable preferred color nor foreground.
            return ContrastRatio(Color.Black, background, bg) >= ContrastRatio(Color.White, background, bg)
                ? Color.Black : Color.White;
        }

        private static double ContrastRatio(Color foreground, Color background, double backgroundLuminance)
        {
            double opacity = foreground.A / 255.0;
            double luminance = RelativeLuminance(
                background.R + (foreground.R - background.R) * opacity,
                background.G + (foreground.G - background.G) * opacity,
                background.B + (foreground.B - background.B) * opacity);
            return (Math.Max(luminance, backgroundLuminance) + 0.05) /
                   (Math.Min(luminance, backgroundLuminance) + 0.05);
        }

        private static double RelativeLuminance(double red, double green, double blue)
        {
            return 0.2126 * LinearChannel(red) + 0.7152 * LinearChannel(green) + 0.0722 * LinearChannel(blue);
        }

        private static double LinearChannel(double channel)
        {
            channel /= 255.0;
            return channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
        }
    }
}
