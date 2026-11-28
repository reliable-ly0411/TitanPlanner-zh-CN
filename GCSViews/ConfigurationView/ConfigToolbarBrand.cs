#if !LIB
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using MissionPlanner.Utilities;

namespace MissionPlanner.GCSViews.ConfigurationView
{
    internal sealed class ConfigToolbarBrand : FlowLayoutPanel
    {
        private readonly ComboBox presets;
        private readonly Label active;
        private bool resetting;

        private static string TextFor(string chinese, string english)
        {
            var culture = CultureInfo.CurrentUICulture.Name;
            return culture == "zh-Hans" || culture == "zh-CN" ? chinese : english;
        }

        public ConfigToolbarBrand()
        {
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Dock = DockStyle.Top;
            WrapContents = true;

            presets = new ComboBox { Name = "ToolbarBrandPreset", DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = "Title", FormattingEnabled = true, Width = 160 };
            presets.Format += (sender, args) =>
            {
                if (ReferenceEquals(args.ListItem, TopBarBrand.Mfe))
                    args.Value = TextFor("奋斗者 MFE", "MakeFlyEasy");
            };
            // Set the initial selection before wiring the change handler.
            presets.Items.AddRange(TopBarBrand.Choices);
            presets.SelectedItem = TopBarBrand.FromId(Settings.Instance[TopBarBrand.SettingsKey]);
            presets.SelectedIndexChanged += (sender, args) => SelectPreset();
            var light = new Button { AutoSize = true, Text = TextFor("选择浅色 PNG", "Choose light PNG") };
            var dark = new Button { AutoSize = true, Text = TextFor("选择深色 PNG", "Choose dark PNG") };
            var reset = new Button { AutoSize = true, Text = TextFor("恢复默认", "Restore default") };
            active = new Label { AutoSize = true, Margin = new Padding(4) };

            light.Click += (sender, args) => Pick(false);
            dark.Click += (sender, args) => Pick(true);
            reset.Click += (sender, args) =>
            {
                resetting = true;
                try { presets.SelectedItem = TopBarBrand.Localized; }
                finally { resetting = false; }
                // The original logo is always the bundled fallback. Custom files remain
                // available on disk, but restoring default clears the selection.
                Settings.Instance.Remove(TopBarBrand.SettingsKey);
                BrandLogoLinks.NotifyUserBrandChanged();
                UpdateStatus();
            };

            Controls.Add(presets);
            Controls.Add(light);
            Controls.Add(dark);
            Controls.Add(reset);
            Controls.Add(active);
            UpdateStatus();
        }

        private void SelectPreset()
        {
            if (resetting || !(presets.SelectedItem is TopBarBrand preset))
                return;
            Settings.Instance[TopBarBrand.SettingsKey] = preset.Id;
            BrandLogoLinks.NotifyUserBrandChanged();
            UpdateStatus();
        }

        private void Pick(bool dark)
        {
            if (!(presets.SelectedItem is TopBarBrand preset))
                return;
            using (var picker = new OpenFileDialog { Filter = "PNG (*.png)|*.png", RestoreDirectory = true })
            {
                if (picker.ShowDialog(this) != DialogResult.OK)
                    return;
                try
                {
                    preset.InstallAsset(Settings.GetUserDataDirectory(), dark, picker.FileName);
                    if (preset.Id == TopBarBrand.DefaultId)
                        Settings.Instance[TopBarBrand.SettingsKey] = preset.Id;
                    BrandLogoLinks.NotifyUserBrandChanged();
                    UpdateStatus();
                }
                catch (IOException)
                {
                    MessageBox.Show(this, TextFor("无法加载或保存该 PNG。需要不大于 4 MiB、最长边不超过 1024 像素的有效 PNG。",
                        "Could not load or save the PNG. Choose a valid PNG up to 4 MiB and 1024 pixels per side."));
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show(this, TextFor("无法读取或保存该 PNG。", "Could not read or save the PNG."));
                }
            }
        }

        private void UpdateStatus()
        {
            var preset = presets.SelectedItem as TopBarBrand ?? TopBarBrand.Localized;
            if (string.IsNullOrEmpty(Settings.Instance[TopBarBrand.SettingsKey]))
            {
                active.Text = TextFor("使用随软件附带的默认标志", "Using bundled default logo");
                return;
            }
            var directory = Settings.GetUserDataDirectory();
            using (var a = preset.TryLoadAsset(directory, false))
            using (var b = preset.TryLoadAsset(directory, true))
            using (var c = preset.TryLoadAsset(directory, null))
            {
                active.Text = a != null || b != null || c != null
                    ? TextFor("已找到本地素材", "Local artwork available")
                    : TextFor("未导入素材时使用默认标志", "No local artwork; using default logo");
            }
        }
    }
}
#endif
