#if !LIB
using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace MissionPlanner.Controls
{
    internal static class MapFooterLayout
    {
        private static readonly ConditionalWeakTable<FlowLayoutPanel, WidthConstraint> WidthConstraints =
            new ConditionalWeakTable<FlowLayoutPanel, WidthConstraint>();

        internal static void Configure(TableLayoutPanel table, FlowLayoutPanel footer,
            Control coordinates, params CheckBox[] options)
        {
            table.SuspendLayout();
            footer.SuspendLayout();
            try
            {
                footer.AutoSize = true;
                footer.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                footer.FlowDirection = FlowDirection.LeftToRight;
                footer.WrapContents = true;

                // Use the designer control's already scaled margin for the runtime 3D option too.
                var margin = options[0].Margin;
                coordinates.Anchor = AnchorStyles.None;
                footer.Controls.SetChildIndex(coordinates, 0);
                for (int i = 0; i < options.Length; i++)
                {
                    var option = options[i];
                    option.AutoSize = true;
                    option.Anchor = AnchorStyles.None;
                    option.Margin = margin;
                    option.CheckAlign = System.Drawing.ContentAlignment.MiddleLeft;
                    option.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
                    footer.Controls.SetChildIndex(option, i + 1);
                }

                // The resource's fixed 25-pixel row clips larger fonts and wrapped controls.
                table.RowStyles[table.GetRow(footer)].SizeType = SizeType.AutoSize;
                WidthConstraints.GetValue(footer, panel => new WidthConstraint(table, panel, coordinates, options)).Update();
            }
            finally
            {
                footer.ResumeLayout(true);
                table.ResumeLayout(true);
            }
        }

        private sealed class WidthConstraint
        {
            private readonly TableLayoutPanel table;
            private readonly FlowLayoutPanel footer;
            private readonly Control coordinates;
            private readonly CheckBox[] options;
            private int naturalCoordinateWidth;
            private int appliedCoordinateWidth;
            private bool updating;

            internal WidthConstraint(TableLayoutPanel table, FlowLayoutPanel footer, Control coordinates, CheckBox[] options)
            {
                this.table = table;
                this.footer = footer;
                this.coordinates = coordinates;
                this.options = options;
                naturalCoordinateWidth = appliedCoordinateWidth = coordinates.Width;
                footer.ClientSizeChanged += (sender, args) => Update();
                footer.Layout += (sender, args) => Update();
                coordinates.SizeChanged += (sender, args) => Update();
            }

            internal void Update()
            {
                if (updating || table.IsDisposed || footer.IsDisposed || coordinates.IsDisposed)
                    return;
                int availableWidth = footer.ClientSize.Width - footer.Padding.Horizontal;
                if (availableWidth <= coordinates.Margin.Horizontal)
                    return;

                bool changed = false;
                updating = true;
                try
                {
                    // Preserve scaling of the natural width even while a narrow pane constrains it.
                    if (coordinates.Width != appliedCoordinateWidth)
                        naturalCoordinateWidth = appliedCoordinateWidth > 0
                            ? (int)Math.Round((double)naturalCoordinateWidth * coordinates.Width / appliedCoordinateWidth)
                            : coordinates.Width;

                    int width = Math.Min(naturalCoordinateWidth, availableWidth - coordinates.Margin.Horizontal);
                    if (coordinates.Width != width)
                    {
                        coordinates.Width = width;
                        changed = true;
                    }
                    appliedCoordinateWidth = coordinates.Width;

                    foreach (var option in options)
                    {
                        var maximum = new Size(Math.Max(1, availableWidth - option.Margin.Horizontal), option.MaximumSize.Height);
                        if (option.MaximumSize != maximum)
                        {
                            option.MaximumSize = maximum;
                            changed = true;
                        }
                    }
                }
                finally
                {
                    updating = false;
                }

                // TableLayoutPanel otherwise measures at the oversized child's minimum width,
                // reserves too few rows, and clips the final row in the actual narrower viewport.
                if (changed)
                    table.PerformLayout(footer, "PreferredSize");
            }
        }
    }
}
#endif
