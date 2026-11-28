using MissionPlanner.Controls;
using System.Drawing;
using System.Windows.Forms;

namespace MissionPlanner.GCSViews.ConfigurationView
{
    public partial class ConfigOptional : MyUserControl, IActivate
    {
        public ConfigOptional()
        {
            InitializeComponent();

            // Keep the resource bounds as the minimum, but allow the same text to wrap
            // farther down when scaling or font metrics require more room.
            var designSize = label1.Size;
            label1.MinimumSize = designSize;
            label1.MaximumSize = new Size(designSize.Width, 0);
            label1.AutoSize = true;
        }

        public void Activate()
        {
        }
    }
}