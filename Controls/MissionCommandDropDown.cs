#if !LIB
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MissionPlanner.Controls
{
    /// <summary>Sizes the mission editor's native list after the grid applies its cell settings.</summary>
    internal static class MissionCommandDropDown
    {
        private const TextFormatFlags TextFlags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
        private static readonly ConditionalWeakTable<ComboBox, Layout> Layouts = new ConditionalWeakTable<ComboBox, Layout>();

        internal static void Configure(ComboBox combo, DataGridViewComboBoxCell cell)
        {
            Detach(combo);
            var layout = new Layout(combo, cell);
            Layouts.Add(combo, layout);
            combo.DrawItem += layout.DrawItem;
            combo.DropDown += layout.DropDown;
            combo.DrawMode = DrawMode.OwnerDrawFixed;
            layout.Refresh();
        }

        internal static void Detach(ComboBox combo)
        {
            Layout layout;
            if (!Layouts.TryGetValue(combo, out layout)) return;
            combo.DrawItem -= layout.DrawItem;
            combo.DropDown -= layout.DropDown;
            Layouts.Remove(combo);
            combo.DrawMode = layout.OriginalDrawMode;
            combo.ItemHeight = layout.OriginalItemHeight;
            combo.DropDownHeight = layout.OriginalDropDownHeight;
            combo.IntegralHeight = layout.OriginalIntegralHeight;
        }

        // Grapheme boundaries also allow an unbroken protocol name to wrap without losing characters.
        internal static List<string> Lines(Graphics graphics, Font font, string text, int width)
        {
            var lines = new List<string>();
            if (string.IsNullOrEmpty(text)) { lines.Add(""); return lines; }
            var starts = StringInfo.ParseCombiningCharacters(text).Concat(new[] { text.Length }).ToArray();
            int first = 0;
            while (first < starts.Length - 1)
            {
                int low = first + 1, high = starts.Length - 1, last = low;
                while (low <= high)
                {
                    int middle = low + (high - low) / 2;
                    var part = text.Substring(starts[first], starts[middle] - starts[first]);
                    if (TextRenderer.MeasureText(graphics, part, font, Size.Empty, TextFlags).Width <= width)
                    { last = middle; low = middle + 1; }
                    else high = middle - 1;
                }
                lines.Add(text.Substring(starts[first], starts[last] - starts[first]));
                first = last;
            }
            return lines;
        }

        private sealed class Layout
        {
            private readonly ComboBox combo;
            private readonly DataGridViewComboBoxCell cell;
            internal readonly DrawMode OriginalDrawMode;
            internal readonly int OriginalItemHeight, OriginalDropDownHeight;
            internal readonly bool OriginalIntegralHeight;
            private int padding = 3, contentWidth = 1, lineHeight = 1;

            internal Layout(ComboBox combo, DataGridViewComboBoxCell cell)
            {
                this.combo = combo; this.cell = cell;
                OriginalDrawMode = combo.DrawMode;
                OriginalItemHeight = combo.ItemHeight;
                OriginalDropDownHeight = combo.DropDownHeight;
                OriginalIntegralHeight = combo.IntegralHeight;
            }

            internal void Refresh()
            {
                var workArea = Screen.FromControl(combo).WorkingArea;
                using (var graphics = combo.CreateGraphics())
                {
                    padding = Math.Max(1, (int)Math.Ceiling(3 * graphics.DpiX / 96));
                    int border = Math.Max(1, (int)Math.Ceiling(2 * graphics.DpiX / 96));
                    int scrollbar = Math.Max(SystemInformation.VerticalScrollBarWidth, (int)Math.Ceiling(17 * graphics.DpiX / 96));
                    var labels = combo.Items.Cast<object>().Select(item => combo.GetItemText(item)).ToArray();
                    int widest = labels.Select(label => TextRenderer.MeasureText(graphics, label, combo.Font, Size.Empty, TextFlags).Width)
                        .DefaultIfEmpty(0).Max();
                    int width = Math.Min(Math.Max(1, workArea.Width - 2 * border),
                        Math.Max(combo.Width, widest + scrollbar + 2 * padding + 2 * border));
                    contentWidth = Math.Max(1, width - scrollbar - 2 * padding - 2 * border);
                    lineHeight = TextRenderer.MeasureText(graphics, "Ag国", combo.Font, Size.Empty, TextFlags).Height;
                    int lines = labels.Select(label => Lines(graphics, combo.Font, label, contentWidth).Count).DefaultIfEmpty(1).Max();
                    combo.ItemHeight = Math.Max(OriginalItemHeight, lines * lineHeight + 2 * padding);
                    combo.IntegralHeight = false;
                    combo.DropDownHeight = Math.Min(Math.Max(1, workArea.Height / 2),
                        Math.Max(OriginalDropDownHeight, combo.ItemHeight * Math.Min(combo.MaxDropDownItems, Math.Max(1, combo.Items.Count)) + 2 * border));
                    // The cell's DropDown handler applies this value after EditingControlShowing.
                    cell.DropDownWidth = width;
                    combo.DropDownWidth = width;
                }
            }

            internal void DrawItem(object sender, DrawItemEventArgs e)
            {
                e.DrawBackground();
                if (e.Index < 0 || e.Index >= combo.Items.Count) return;
                var text = combo.GetItemText(combo.Items[e.Index]);
                var bounds = Rectangle.Inflate(e.Bounds, -padding, 0);
                if ((e.State & DrawItemState.ComboBoxEdit) != 0)
                    TextRenderer.DrawText(e.Graphics, text, combo.Font, bounds, e.ForeColor, TextFlags | TextFormatFlags.EndEllipsis);
                else
                {
                    int y = bounds.Top + padding;
                    foreach (var line in Lines(e.Graphics, combo.Font, text, Math.Max(1, Math.Min(contentWidth, bounds.Width))))
                    {
                        TextRenderer.DrawText(e.Graphics, line, combo.Font,
                            new Rectangle(bounds.Left, y, bounds.Width, lineHeight), e.ForeColor, TextFlags);
                        y += lineHeight;
                    }
                }
                e.DrawFocusRectangle();
            }

            internal void DropDown(object sender, EventArgs e)
            {
                Refresh();
                if (Environment.OSVersion.Platform != PlatformID.Win32NT) return;
                // The native popup is positioned after DropDown. Correct its final bounds on the next UI turn.
                combo.BeginInvoke((Action)(() =>
                {
                    Layout current;
                    if (combo.IsDisposed || !combo.DroppedDown || !Layouts.TryGetValue(combo, out current) || current != this) return;
                    var info = new ComboBoxInfo { Size = Marshal.SizeOf(typeof(ComboBoxInfo)) };
                    NativeRect rectangle;
                    if (!GetComboBoxInfo(combo.Handle, ref info) || !GetWindowRect(info.List, out rectangle)) return;
                    var area = Screen.FromControl(combo).WorkingArea;
                    int width = Math.Min(rectangle.Right - rectangle.Left, area.Width);
                    int height = Math.Min(rectangle.Bottom - rectangle.Top, area.Height);
                    int x = Math.Max(area.Left, Math.Min(rectangle.Left, area.Right - width));
                    int y = Math.Max(area.Top, Math.Min(rectangle.Top, area.Bottom - height));
                    SetWindowPos(info.List, IntPtr.Zero, x, y, width, height, 0x0014); // NOZORDER | NOACTIVATE
                }));
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect { internal int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential)]
        private struct ComboBoxInfo
        {
            internal int Size;
            internal NativeRect Item, Button;
            internal int ButtonState;
            internal IntPtr Combo, Edit, List;
        }
        [DllImport("user32.dll")]
        private static extern bool GetComboBoxInfo(IntPtr combo, ref ComboBoxInfo info);
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr window, out NativeRect rectangle);
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
    }
}
#endif
