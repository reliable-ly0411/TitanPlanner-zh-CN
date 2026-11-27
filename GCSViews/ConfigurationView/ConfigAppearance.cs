#if !LIB
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MissionPlanner.Controls;
using MissionPlanner.Utilities;

namespace MissionPlanner.GCSViews.ConfigurationView
{
    /// <summary>Desktop appearance controls; imports and resets remain independent of vehicle settings.</summary>
    public sealed class ConfigAppearance : MyUserControl
    {
        private readonly PictureBox iconPreview = new PictureBox { SizeMode = PictureBoxSizeMode.CenterImage, Height = 64, Dock = DockStyle.Fill };
        private readonly PictureBox startupPreview = new PictureBox { SizeMode = PictureBoxSizeMode.Zoom, Height = 200, Dock = DockStyle.Fill, BackColor = Color.Black };
        private ComboBox iconChoices, startupChoices;
        private readonly bool simplifiedChinese;
        private bool synchronizingPresets;

        private sealed class Preset
        {
            public readonly string Id;
            private readonly string caption;

            public Preset(string id, string caption) { Id = id; this.caption = caption; }
            public override string ToString() => caption;
        }
        private readonly Label status = new Label { AutoSize = true, Dock = DockStyle.Fill };

        public ConfigAppearance() : this(embedded: false)
        {
        }

        internal ConfigAppearance(bool embedded)
        {
            Name = "ConfigAppearance";
            AutoScroll = !embedded;
            if (embedded)
            {
                // ConfigPlanner owns the scroll position and the available width.
                AutoSize = true;
                AutoSizeMode = AutoSizeMode.GrowAndShrink;
                Dock = DockStyle.Top;
                Margin = Padding.Empty;
            }
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1,
                Padding = embedded ? Padding.Empty : new Padding(12)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            if (!embedded)
                AddText(layout, "Window icon and startup image");
            AddText(layout, "ICO: up to 4 MiB, 1-256 pixels per frame (up to 64 frames). Window icons apply now and on restart.");
            var culture = System.Globalization.CultureInfo.CurrentUICulture.Name;
            simplifiedChinese = culture == "zh-CN" || culture == "zh-Hans";
            layout.Controls.Add(iconPreview);
            layout.Controls.Add(PresetRow(AppearanceAsset.WindowIcon, out iconChoices));
            AddText(layout, "PNG, JPEG or BMP: up to 16 MiB, 4096 pixels per side. The full image is fitted without cropping. Applies on next startup.");
            layout.Controls.Add(startupPreview);
            layout.Controls.Add(PresetRow(AppearanceAsset.StartupImage, out startupChoices));
            AddText(layout, Local(
                "先选预设再选文件，可为该预设导入本地素材。选中默认项或内置启动图时，文件按钮会导入为自定义素材。厂商图标不会随包附带或自动下载。",
                "Select a preset before choosing a file to import into that preset. Choosing a file beside a built-in image or default imports it as custom. Vendor icons are not bundled or downloaded."));
            AddText(layout, "These preferences apply to this user's profiles. Imported copies are saved separately; original files, logs and flight settings are preserved.");
            AddText(layout, "EXE, installer and Explorer icons require a rebuild. Existing or pinned shortcuts keep their own icons; change or recreate them separately. Windows may retain a cached taskbar icon.");
            AddText(layout, "Use images you have permission to use. Restoring defaults preserves imported copies and preference backups.");
            layout.Controls.Add(status);
            Controls.Add(layout);
            layout.SizeChanged += (sender, args) => WrapLabels(layout);
            // Unreadable appearance data must not prevent the settings page from opening.
            try { RefreshPreviews(); }
            catch (Exception) { status.Text = UiText.Translate("Appearance preview unavailable."); }
        }

        private static void AddText(TableLayoutPanel layout, string text)
        {
            layout.Controls.Add(new Label
            {
                Text = UiText.Translate(text), AutoSize = true, Dock = DockStyle.Fill,
                Margin = new Padding(3, 6, 3, 6)
            });
        }

        private static void WrapLabels(TableLayoutPanel layout)
        {
            foreach (Control control in layout.Controls)
                if (control is Label) control.MaximumSize = new Size(Math.Max(1, layout.ClientSize.Width - layout.Padding.Horizontal - 6), 0);
        }

        private string Local(string chinese, string english) =>
            simplifiedChinese ? chinese : UiText.Translate(english);

        private Control PresetRow(AppearanceAsset kind, out ComboBox combo)
        {
            var icon = kind == AppearanceAsset.WindowIcon;
            var pickerName = icon ? Local("窗口图标", "Window icon") : Local("启动图", "Startup image");
            var fileName = icon ? Local("选择本地图标…", "Choose local icon…") : Local("选择本地启动图…", "Choose local startup image…");
            var row = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true };
            var menu = new ComboBox
            {
                Name = icon ? "WindowIconPreset" : "StartupImagePreset",
                AccessibleName = pickerName, DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 200, Anchor = AnchorStyles.Left
            };
            menu.Items.Add(new Preset("default", icon ? Local("默认图标", "Default icon") : Local("TitanPlanner 原启动图（默认）", "TitanPlanner artwork (default)")));
            menu.Items.Add(new Preset("mission-planner", icon ? "Mission Planner" : Local("Mission Planner 原启动图", "Mission Planner artwork")));
            if (icon)
                menu.Items.Add(new Preset("titan-planner", "TitanPlanner"));
            menu.Items.Add(new Preset("mfe", Local("奋斗者", "MakeFlyEasy")));
            menu.Items.Add(new Preset("custom", Local("自定义", "Custom")));
            menu.SelectedIndex = 0;
            // Closed-list arrow keys can change the index without raising SelectionChangeCommitted
            // (in particular on Mono). Do not apply intermediate choices in an open list.
            menu.SelectedIndexChanged += (sender, args) =>
            {
                if (!menu.DroppedDown) SelectPreset(kind, menu.SelectedItem as Preset);
            };
            menu.SelectionChangeCommitted += (sender, args) => SelectPreset(kind, menu.SelectedItem as Preset);
            var choose = new Button
            {
                Name = icon ? "ChooseLocalWindowIcon" : "ChooseLocalStartupImage",
                Text = fileName, AutoSize = true, Anchor = AnchorStyles.Left
            };
            choose.Click += (sender, args) => ChooseLocalAsset(kind, (Preset)menu.SelectedItem, fileName);
            row.SizeChanged += (sender, args) =>
            {
                int available = row.ClientSize.Width - choose.PreferredSize.Width - menu.Margin.Horizontal - choose.Margin.Horizontal;
                int width = Math.Min(240, Math.Max(132, available));
                if (menu.Width != width) menu.Width = width;
            };
            row.Controls.Add(menu);
            row.Controls.Add(choose);
            combo = menu;
            return row;
        }

        private void SelectPreset(AppearanceAsset kind, Preset preset)
        {
            if (synchronizingPresets || preset == null) return;
            var icon = kind == AppearanceAsset.WindowIcon;
            var store = DesktopAppearance.Store;
            if (store.CurrentPreset(kind) == preset.Id) return;
            Action select;
            bool available;
            switch (preset.Id)
            {
                case "default":
                    select = () => DesktopAppearance.RestoreDefault(kind);
                    available = true;
                    break;
                case "mission-planner":
                    select = icon ? (Action)(() => DesktopAppearance.SelectVendorIcon(ImportedVendorIcon.MissionPlanner))
                        : DesktopAppearance.SelectMissionPlannerStartup;
                    available = !icon || store.CanSelectVendorIcon(ImportedVendorIcon.MissionPlanner);
                    break;
                case "titan-planner":
                    select = () => DesktopAppearance.SelectVendorIcon(ImportedVendorIcon.TitanPlanner);
                    available = store.CanSelectVendorIcon(ImportedVendorIcon.TitanPlanner);
                    break;
                case "mfe":
                    select = () => DesktopAppearance.SelectMfe(kind);
                    available = store.CanSelectMfe(kind);
                    break;
                default:
                    select = icon ? (Action)DesktopAppearance.SelectSavedIcon : store.SelectSavedStartupImage;
                    available = icon ? store.CanSelectSavedIcon() : store.CanSelectSavedStartupImage();
                    break;
            }
            if (!available)
            {
                status.Text = Local("该选项尚未导入素材。请使用旁边的文件按钮；当前外观保持不变。",
                    "No local file has been imported for this option. Use the adjacent file button; the current appearance is unchanged.");
                return;
            }
            Change(select);
        }

        private void ChooseLocalAsset(AppearanceAsset kind, Preset preset, string title)
        {
            var icon = kind == AppearanceAsset.WindowIcon;
            using (var dialog = new OpenFileDialog
            {
                Filter = icon ? "ICO (*.ico)|*.ico" : "PNG / JPEG / BMP|*.png;*.jpg;*.jpeg;*.bmp",
                CheckFileExists = true, Multiselect = false, RestoreDirectory = true, Title = title
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    SetChoice(kind, DesktopAppearance.Store.CurrentPreset(kind));
                    status.Text = Local("已取消，当前外观保持不变。", "Cancelled; the current appearance is unchanged.");
                    return;
                }
                Change(() =>
                {
                    if (icon && preset.Id == "mission-planner")
                        DesktopAppearance.ImportVendorIcon(ImportedVendorIcon.MissionPlanner, dialog.FileName);
                    else if (icon && preset.Id == "titan-planner")
                        DesktopAppearance.ImportVendorIcon(ImportedVendorIcon.TitanPlanner, dialog.FileName);
                    else if (preset.Id == "mfe")
                        DesktopAppearance.ImportMfe(kind, dialog.FileName);
                    else
                        DesktopAppearance.Import(kind, dialog.FileName);
                });
            }
        }

        private void SetChoice(AppearanceAsset kind, string id)
        {
            var menu = kind == AppearanceAsset.WindowIcon ? iconChoices : startupChoices;
            if (menu == null) return;
            foreach (Preset preset in menu.Items)
                if (preset.Id == id)
                {
                    synchronizingPresets = true;
                    try { menu.SelectedItem = preset; }
                    finally { synchronizingPresets = false; }
                    return;
                }
        }

        private void Change(Action save)
        {
            try
            {
                save();
            }
            catch (Exception ex)
            {
                SetChoice(AppearanceAsset.WindowIcon, DesktopAppearance.Store.CurrentPreset(AppearanceAsset.WindowIcon));
                SetChoice(AppearanceAsset.StartupImage, DesktopAppearance.Store.CurrentPreset(AppearanceAsset.StartupImage));
                status.Text = UiText.Translate("Unable to save appearance. Previous selection is unchanged.");
                // Paths and decoder exception text can contain personal information. Show only known validation messages.
                if (ex is InvalidDataException)
                    status.Text += " " + UiText.Translate(ex.Message);
                return;
            }
            string warning;
            try { warning = RefreshPreviews(); }
            catch (Exception) { status.Text = UiText.Translate("Appearance saved; preview unavailable."); return; }
            status.Text = UiText.Translate("Appearance saved. Window icons apply now; startup image applies on next startup.") +
                (warning == null ? "" : " " + UiText.Translate(warning));
        }

        private string RefreshPreviews()
        {
            var icon = ThemeManager.FormIcon;
            var previous = iconPreview.Image;
            iconPreview.Image = icon == null ? null : icon.ToBitmap();
            previous?.Dispose();
            string warning;
            var image = DesktopAppearance.CreateStartupPreview(out warning);
            previous = startupPreview.Image;
            startupPreview.Image = image;
            previous?.Dispose();
            SetChoice(AppearanceAsset.WindowIcon, DesktopAppearance.Store.CurrentPreset(AppearanceAsset.WindowIcon));
            SetChoice(AppearanceAsset.StartupImage, DesktopAppearance.Store.CurrentPreset(AppearanceAsset.StartupImage));
            warning = DesktopAppearance.IconWarning ?? warning;
            status.Text = UiText.Translate(warning ?? "Default or saved appearance is shown above.");
            return warning;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                iconPreview.Image?.Dispose();
                startupPreview.Image?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
#endif
