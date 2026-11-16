using System;
using System.Windows.Forms;

namespace MissionPlanner.Controls
{
    /// <summary>Separates destructive-action consent from the command transport for offline testing.</summary>
    public static class StorageFormatConfirmation
    {
        public static void Execute(Func<DialogResult> confirm, Action send)
        {
            if (confirm() != DialogResult.Yes)
                return;
            send();
        }
    }
}
