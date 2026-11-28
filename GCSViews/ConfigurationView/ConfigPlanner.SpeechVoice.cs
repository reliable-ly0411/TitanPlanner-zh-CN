#if !LIB
using System;
using System.Drawing;
using System.Windows.Forms;
using MissionPlanner.Controls;
using MissionPlanner.Utilities;

namespace MissionPlanner.GCSViews.ConfigurationView
{
    public partial class ConfigPlanner
    {
        private ComboBox voiceNames;
        private MyButton voicePreview;
        private Label voiceHint;

        private void AddChineseVoiceSettings(GroupBox group, TableLayoutPanel table)
        {
            var row = CreateWrapFlow();
            string label = UiText.Translate("Windows speech voice");
            row.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left });
            voiceNames = new ComboBox
            {
                Name = "CMB_speechVoice", AccessibleName = label,
                DropDownStyle = ComboBoxStyle.DropDownList, Width = 180, DropDownWidth = 300
            };
            voicePreview = new MyButton { Name = "BUT_previewVoice", Text = UiText.Translate("Preview voice"), AutoSize = true };
            voiceHint = new Label { AutoSize = true };
            row.Controls.Add(voiceNames);
            row.Controls.Add(voicePreview);
            table.Controls.Add(row, 0, 2);
            table.Controls.Add(voiceHint, 0, 3);
            var hint = voiceHint;
            group.SizeChanged += (sender, args) =>
            {
                int available = group.DisplayRectangle.Width - table.Padding.Horizontal;
                row.MaximumSize = new Size(Math.Max(1, available - row.Margin.Horizontal), 0);
                hint.MaximumSize = new Size(Math.Max(1, available - hint.Margin.Horizontal), 0);
            };
            voiceNames.SelectionChangeCommitted += (sender, args) =>
            {
                Settings.Instance[Speech.ChineseVoiceSetting] =
                    voiceNames.SelectedIndex == voiceNames.Items.Count - 1
                        ? Speech.OfflineXiaoxiaoVoiceSetting
                        : voiceNames.SelectedIndex > 0 ? (string)voiceNames.SelectedItem : "";
                RefreshChineseVoiceSettings();
            };
            voicePreview.Click += (sender, args) =>
            {
                if (MainV2.speechEngine is Speech speech && speech.TryPreviewChineseVoice())
                    RefreshChineseVoiceSettings();
                else
                    SetVoiceHint(UiText.Translate("Voice preview is unavailable. Try again when speech is idle."));
            };
            RefreshChineseVoiceSettings();
        }

        private void RefreshChineseVoiceSettings()
        {
            if (voiceNames == null) return;
            var speech = MainV2.speechEngine as Speech;
            var names = speech?.RefreshChineseVoiceNames() ?? Array.Empty<string>();
            string saved = Settings.Instance[Speech.ChineseVoiceSetting];
            bool offlineXiaoxiao = saved == Speech.OfflineXiaoxiaoVoiceSetting;
            int index = offlineXiaoxiao ? names.Length : Array.IndexOf(names, saved);

            voiceNames.BeginUpdate();
            try
            {
                voiceNames.Items.Clear();
                voiceNames.Items.Add(UiText.Translate("Automatic (system default)"));
                voiceNames.Items.AddRange(names);
                voiceNames.Items.Add(UiText.Translate(Speech.OfflineXiaoxiaoVoiceLabel));
                voiceNames.SelectedIndex = index + 1;
            }
            finally { voiceNames.EndUpdate(); }
            voicePreview.Enabled = CHK_enablespeech.Checked && speech != null;

            string hint = "";
            if (offlineXiaoxiao)
                hint = Speech.HasOfflineXiaoxiaoWave(UiText.Translate("This is the ground station voice preview.")) &&
                       Speech.HasOfflineXiaoxiaoWave(UiText.Translate("Chip full, logging stopped"))
                    ? UiText.Translate("Offline Xiaoxiao audio pack is ready.")
                    : UiText.Translate("Offline Xiaoxiao audio pack is missing or invalid. Windows speech will be used instead.");
            else if (!string.IsNullOrEmpty(saved) && index < 0)
                hint = UiText.Translate("The saved voice is unavailable. Speech will use the system default.");
            else if (names.Length == 0)
                hint = UiText.Translate("No Microsoft Chinese voice is available. Check the Windows speech language pack.");
            SetVoiceHint(hint);
        }

        private void SetVoiceHint(string text)
        {
            voiceHint.Text = text;
            voiceHint.Visible = text.Length != 0;
        }
    }
}
#endif
