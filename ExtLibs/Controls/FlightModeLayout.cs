using System;
using System.Drawing;
using MissionPlanner.Utilities;

namespace MissionPlanner.Controls
{
    /// <summary>Bounded text layout shared by the HUD and the toolbar; measurement uses each renderer's fonts.</summary>
    public sealed class FlightModeLayout
    {
        public string PrimaryText { get; private set; } = "";
        public string SecondaryText { get; private set; } = "";
        public float PrimarySize { get; private set; }
        public float SecondarySize { get; private set; }
        public RectangleF PrimaryBounds { get; private set; }
        public RectangleF SecondaryBounds { get; private set; }

        public static RectangleF HudArea(int width, int height, float fontSize, float left, float right)
        {
            // Stay below pre-arm text (H - 4 * fontSize), and below the waypoint / altitude tapes.
            left = Math.Max(left, width * 0.25f);
            right = Math.Min(right, width * 0.75f);
            return new RectangleF(left, Math.Max(0, height - 2.4f * fontSize),
                Math.Max(0, right - left), Math.Max(0, Math.Min(height, 2.4f * fontSize) - 1));
        }

        public static FlightModeLayout Create(FlightModeDisplay text, RectangleF area, float fontSize,
            Func<string, float, SizeF> measure)
        {
            var result = new FlightModeLayout();
            if (area.Width <= 0 || area.Height <= 0 || fontSize <= 0)
                return result;

            // Half-pixel steps bound the HUD glyph cache during repeated resizing / mode updates.
            var size = Math.Max(1, (float)Math.Floor(Math.Min(fontSize, area.Height) * 2) / 2);
            for (; size >= 1; size -= 0.5f)
            {
                var secondarySize = Math.Max(0.5f, (float)Math.Floor(size * 0.72f * 2) / 2);
                var primary = Fit(text.Primary, size, area.Width, measure);
                var secondary = text.IsBilingual ? Fit(text.Secondary, secondarySize, area.Width, measure) : "";
                var primaryExtent = measure(primary, size);
                var secondaryExtent = secondary.Length == 0 ? SizeF.Empty : measure(secondary, secondarySize);
                var gap = secondary.Length == 0 ? 0 : Math.Max(1, size * 0.08f);
                var totalHeight = primaryExtent.Height + gap + secondaryExtent.Height;
                if (totalHeight > area.Height)
                    continue;

                var y = area.Top + (area.Height - totalHeight) / 2;
                result.PrimaryText = primary;
                result.SecondaryText = secondary;
                result.PrimarySize = size;
                result.SecondarySize = secondarySize;
                result.PrimaryBounds = new RectangleF(area.Left + (area.Width - primaryExtent.Width) / 2,
                    y, primaryExtent.Width, primaryExtent.Height);
                result.SecondaryBounds = new RectangleF(area.Left + (area.Width - secondaryExtent.Width) / 2,
                    y + primaryExtent.Height + gap, secondaryExtent.Width, secondaryExtent.Height);
                return result;
            }
            return result;
        }

        private static string Fit(string text, float size, float width, Func<string, float, SizeF> measure)
        {
            if (text.Length == 0 || measure(text, size).Width <= width)
                return text;
            const string ellipsis = "…";
            if (measure(ellipsis, size).Width > width)
                return "";

            int low = 0, high = text.Length;
            while (low < high)
            {
                int middle = (low + high + 1) / 2;
                if (measure(text.Substring(0, middle) + ellipsis, size).Width <= width)
                    low = middle;
                else
                    high = middle - 1;
            }
            // A future plugin mode may contain a Unicode supplementary character.
            if (low > 0 && char.IsHighSurrogate(text[low - 1])) low--;
            return text.Substring(0, low) + ellipsis;
        }
    }
}
