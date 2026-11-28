using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using MissionPlanner.Utilities;

namespace MissionPlanner.Controls
{
    /// <summary>
    /// An (i) button in the 2D map's bottom-right corner that shows and hides, above it, the legend of the lines
    /// drawn from the vehicle. Drawn on the map like its zoom buttons, so it blends over the imagery, and styled
    /// like the 3D map's settings button (MapButton). Hit testing is pure maths.
    /// </summary>
    public sealed class MapLegendButton
    {
        public const int Size = 26;
        // from the map's bottom edge and from the zoom slider
        const int Margin = 8;
        const int Bottom = 8;
        const int Gap = 6;
        const int Pad = 10;
        const int RowHeight = 20;
        const int SwatchWidth = 18;

        // the lines GMapMarkerPlane draws, in its colours
        static readonly (string Text, Color Color)[] Entries =
        {
            ("Current Heading", Color.Red),
            ("Direction to WP", Color.Orange),
            ("Target Heading", Color.Green),
            ("Ground Track", Color.Black),
        };

        static readonly Font LegendFont = new Font("Segoe UI", 9f);

        public bool Hovered { get; private set; }

        /// <summary>Whether the legend is showing.</summary>
        public bool Open { get; private set; }

        public Rectangle Bounds(Size map)
        {
            return new Rectangle(map.Width - Margin - Size, map.Height - Bottom - Size, Size, Size);
        }

        public bool HitTest(Size map, Point point)
        {
            var bounds = Bounds(map);
            float dx = point.X - (bounds.Left + Size / 2f), dy = point.Y - (bounds.Top + Size / 2f);
            return dx * dx + dy * dy <= (Size / 2f + 2) * (Size / 2f + 2);
        }

        /// <summary>Returns true if the hover state changed.</summary>
        public bool SetHovered(bool hovered)
        {
            if (hovered == Hovered)
                return false;
            Hovered = hovered;
            return true;
        }

        public void Toggle()
        {
            Open = !Open;
        }

        public void Draw(Graphics g, Size map)
        {
            var smoothing = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var bounds = Bounds(map);
            var iconColor = MapButton.DrawDisc(g, new RectangleF(bounds.X + 0.5f, bounds.Y + 0.5f, Size - 1, Size - 1),
                Hovered || Open, MapButton.Accent, 1.5f);

            // "i": a dot over a stem
            float cx = bounds.Left + Size / 2f;
            using (var brush = new SolidBrush(iconColor))
            {
                float dot = Size * 0.14f;
                g.FillEllipse(brush, cx - dot / 2, bounds.Top + Size * 0.22f, dot, dot);
                float stem = Size * 0.12f;
                g.FillRectangle(brush, cx - stem / 2, bounds.Top + Size * 0.42f, stem, Size * 0.36f);
            }

            if (Open)
                DrawLegend(g, bounds);

            g.SmoothingMode = smoothing;
        }

        void DrawLegend(Graphics g, Rectangle button)
        {
            var labels = Entries.Select(e => (Text: MapGpsPresentation.LegendLabel(e.Text), Color: e.Color)).ToArray();
            bool enhanced = MapGpsPresentation.Enhanced;
            var layout = LegendLayout(g, button, labels, enhanced);
            var panel = layout.panel;
            if (panel.IsEmpty)
                return;
            float rowHeight = layout.rowHeight;

            using (var path = MapButton.RoundedRect(panel, 6))
            using (var fill = new SolidBrush(Color.FromArgb(214, 13, 15, 19)))
            using (var border = new Pen(Color.FromArgb(150, MapButton.Accent), 1f))
            {
                g.FillPath(fill, path);
                g.DrawPath(border, path);
            }

            float x = panel.Left + Pad;
            float y = panel.Top + Pad - 2;
            using (var text = new SolidBrush(Color.FromArgb(232, 236, 241)))
            using (var format = enhanced ? new StringFormat {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            } : null)
            {
                foreach (var entry in labels)
                {
                    float mid = y + rowHeight / 2f;

                    // a short stroke of the line; the black one gets a light edge to show on the dark panel
                    if (entry.Color.R + entry.Color.G + entry.Color.B < 150)
                    {
                        using (var edge = new Pen(Color.FromArgb(160, 255, 255, 255), 5f))
                            g.DrawLine(edge, x, mid, x + SwatchWidth, mid);
                    }

                    using (var line = new Pen(entry.Color, 3f))
                        g.DrawLine(line, x, mid, x + SwatchWidth, mid);

                    var textX = x + SwatchWidth + 8;
                    if (enhanced)
                    {
                        var cell = new RectangleF(textX, y, panel.Right - Pad - textX, rowHeight);
                        g.DrawString(entry.Text, LegendFont, text, cell, format);
                    }
                    else
                    {
                        var size = g.MeasureString(entry.Text, LegendFont);
                        g.DrawString(entry.Text, LegendFont, text, textX, mid - size.Height / 2);
                    }
                    y += rowHeight;
                }
            }
        }
        private static (RectangleF panel, float rowHeight) LegendLayout(Graphics graphics, Rectangle button,
            (string Text, Color Color)[] labels, bool enhanced)
        {
            float textWidth = 0;
            float textHeight = 0;
            foreach (var label in labels)
            {
                var measured = graphics.MeasureString(label.Text, LegendFont);
                textWidth = System.Math.Max(textWidth, measured.Width);
                textHeight = System.Math.Max(textHeight, measured.Height);
            }
            float chromeWidth = Pad + SwatchWidth + 8 + Pad;
            float width = chromeWidth + textWidth;
            float rowHeight = enhanced ? System.Math.Max(RowHeight, (float)System.Math.Ceiling(textHeight) + 4) : RowHeight;
            float bottom = button.Top - Gap;
            if (enhanced)
            {
                width = System.Math.Min(width, button.Right - Margin);
                if (width <= chromeWidth || bottom <= 2 * Pad + labels.Length * 9 - 4)
                    return (RectangleF.Empty, rowHeight); // there is no drawable space
                rowHeight = System.Math.Min(rowHeight, (bottom - (2 * Pad - 4f)) / labels.Length);
            }
            float height = 2 * Pad + labels.Length * rowHeight - 4;
            return (new RectangleF(button.Right - width, bottom - height, width, height), rowHeight);
        }
    }
}
