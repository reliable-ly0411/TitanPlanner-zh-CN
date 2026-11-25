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
                footer.AutoSize = false;
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
            private bool updating, updateQueued;
            private int observedTableWidth = -1;

            internal WidthConstraint(TableLayoutPanel table, FlowLayoutPanel footer, Control coordinates, CheckBox[] options)
            {
                this.table = table;
                this.footer = footer;
                this.coordinates = coordinates;
                this.options = options;
                naturalCoordinateWidth = appliedCoordinateWidth = coordinates.Width;
                footer.ClientSizeChanged += (sender, args) => Update();
                table.ClientSizeChanged += (sender, args) => QueueUpdate();
                coordinates.HandleCreated += (sender, args) => Update();
                coordinates.SizeChanged += (sender, args) => Update();
                foreach (var option in options)
                    option.FontChanged += (sender, args) => Update();
            }

            private void QueueUpdate()
            {
                if (observedTableWidth == table.ClientSize.Width || updateQueued || !table.IsHandleCreated || table.IsDisposed)
                    return;
                updateQueued = true;
                table.BeginInvoke((Action)(() =>
                {
                    updateQueued = false;
                    observedTableWidth = table.ClientSize.Width;
                    Update();
                    int row = table.GetRow(footer);
                    var actual = table.GetRowHeights();
                    if (row >= 0 && row < actual.Length && table.RowStyles[row].SizeType == SizeType.Absolute &&
                        Math.Abs(actual[row] - table.RowStyles[row].Height) > 2)
                        table.PerformLayout(null, null);
                }));
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

                    // With the legacy WinForms configuration, native text may render at a
                    // higher DPI than the coordinate control's designer-scaled width.
                    int renderedCoordinateWidth = naturalCoordinateWidth;
                    if (coordinates.IsHandleCreated)
                    {
                        using (var graphics = coordinates.CreateGraphics())
                            renderedCoordinateWidth = Math.Max(renderedCoordinateWidth,
                                (int)Math.Ceiling(naturalCoordinateWidth * graphics.DpiX / coordinates.DeviceDpi));
                    }
                    int width = Math.Min(renderedCoordinateWidth, availableWidth - coordinates.Margin.Horizontal);
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
                    // Measure at the actual viewport; the table's unconstrained probe can
                    // otherwise allocate one fewer line than the flow panel needs.
                    int preferredHeight = footer.Padding.Vertical;
                    int lineWidth = 0, lineHeight = 0;
                    foreach (Control child in footer.Controls)
                    {
                        if (!child.Visible)
                            continue;
                        var size = child.AutoSize ? child.GetPreferredSize(new Size(availableWidth, 0)) : child.Size;
                        if (child.MaximumSize.Width > 0)
                            size.Width = Math.Min(size.Width, child.MaximumSize.Width);
                        int childWidth = size.Width + child.Margin.Horizontal;
                        if (lineWidth != 0 && lineWidth + childWidth > availableWidth)
                        {
                            preferredHeight += lineHeight;
                            lineWidth = lineHeight = 0;
                        }
                        lineWidth += childWidth;
                        lineHeight = Math.Max(lineHeight, size.Height + child.Margin.Vertical);
                    }
                    preferredHeight += lineHeight;
                    var row = table.RowStyles[table.GetRow(footer)];
                    int height = preferredHeight + footer.Margin.Vertical;
                    if (preferredHeight > 0 && (row.SizeType != SizeType.Absolute || row.Height != height))
                    {
                        row.SizeType = SizeType.Absolute;
                        row.Height = height;
                        footer.Height = preferredHeight;
                        changed = true;
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
