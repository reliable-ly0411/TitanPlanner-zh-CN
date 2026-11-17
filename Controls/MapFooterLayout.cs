#if !LIB
using System.Windows.Forms;

namespace MissionPlanner.Controls
{
    internal static class MapFooterLayout
    {
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
            }
            finally
            {
                footer.ResumeLayout(true);
                table.ResumeLayout(true);
            }
        }
    }
}
#endif
