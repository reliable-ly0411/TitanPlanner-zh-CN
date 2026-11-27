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
        private readonly PictureBox iconPreview = new PictureBox { SizeMode = PictureBoxSizeMode.Zoom, Height = 40, Dock = DockStyle.Fill };
        private readonly PictureBox startupPreview = new PictureBox { SizeMode = PictureBoxSizeMode.Zoom, Height = 48, Dock = DockStyle.Fill, BackColor = Color.Black };
        private ComboBox iconChoices, startupChoices;
        private readonly bool simplifiedChinese;
        private readonly ToolTip details = new ToolTip();
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
            var culture = System.Globalization.CultureInfo.CurrentUICulture.Name;
            simplifiedChinese = culture == "zh-CN" || culture == "zh-Hans";
            if (!embedded)
                AddText(layout, Local("窗口图标和启动图", "Window icon and startup image"));
            var iconTitle = AddText(layout, Local("窗口图标 · 即时生效", "Window icon · applies now"));
            layout.Controls.Add(PresetRow(AppearanceAsset.WindowIcon, out iconChoices));
            var startupTitle = AddText(layout, Local("启动图 · 下次启动生效", "Startup image · applies on next start"));
            layout.Controls.Add(PresetRow(AppearanceAsset.StartupImage, out startupChoices));

            var previews = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2 };
            previews.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            previews.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 75));
            iconPreview.Name = "WindowIconPreview";
            iconPreview.AccessibleName = Local("当前窗口图标预览", "Active window icon preview");
            iconPreview.MaximumSize = new Size(40, 40);
            startupPreview.Name = "StartupImagePreview";
            startupPreview.AccessibleName = Local("下次启动图预览", "Next startup image preview");
            previews.Controls.Add(iconPreview, 0, 0);
            previews.Controls.Add(startupPreview, 1, 0);
            layout.Controls.Add(previews);
            details.SetToolTip(iconTitle, UiText.Translate("ICO: up to 4 MiB, 1-256 pixels per frame (up to 64 frames). Window icons apply now and on restart.") +
                "\n" + UiText.Translate("EXE, installer and Explorer icons require a rebuild. Existing or pinned shortcuts keep their own icons; change or recreate them separately. Windows may retain a cached taskbar icon."));
            details.SetToolTip(startupTitle, UiText.Translate("PNG, JPEG or BMP: up to 16 MiB, 4096 pixels per side. The full image is fitted without cropping. Applies on next startup."));
            details.SetToolTip(status, Local(
                "选中默认项或内置启动图时，文件按钮会导入为自定义素材。厂商图标不会随包附带或自动下载。",
                "Choosing a file beside a built-in image or default imports it as custom. Vendor icons are not bundled or downloaded.") +
                "\n" + UiText.Translate("Use images you have permission to use. Restoring defaults preserves imported copies and preference backups."));
            layout.Controls.Add(status);
            Controls.Add(layout);
            layout.SizeChanged += (sender, args) => WrapLabels(layout);
            // Unreadable appearance data must not prevent the settings page from opening.
            try { RefreshPreviews(); }
            catch (Exception) { status.Text = UiText.Translate("Appearance preview unavailable."); }
        }

        private static Label AddText(TableLayoutPanel layout, string text)
        {
            var label = new Label
            {
                Text = UiText.Translate(text), AutoSize = true, Dock = DockStyle.Fill,
                Margin = new Padding(3, 2, 3, 2)
            };
            layout.Controls.Add(label);
            return label;
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
            var row = new TableLayoutPanel
            {
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Fill,
                Margin = Padding.Empty, ColumnCount = 2
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var menu = new ComboBox
            {
                Name = icon ? "WindowIconPreset" : "StartupImagePreset",
                AccessibleName = pickerName, DropDownStyle = ComboBoxStyle.DropDownList,
                DropDownWidth = 260, Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            menu.Items.Add(new Preset("default", icon ? Local("包内默认图标", "Packaged default icon") : Local("TitanPlanner 原启动图（默认）", "TitanPlanner artwork (default)")));
            menu.Items.Add(new Preset("mission-planner", icon ? Local("Mission Planner（本地）", "Mission Planner (local)") : Local("Mission Planner 原启动图", "Mission Planner artwork")));
            if (icon)
                menu.Items.Add(new Preset("titan-planner", Local("TitanPlanner（本地）", "TitanPlanner (local)")));
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
            // Mono closes an expanded list before raising KeyDown for Return/Escape,
            // and may not emit SelectionChangeCommitted for Return. Wait for the key.
            if (Type.GetType("Mono.Runtime") != null)
            {
                Preset openedChoice = null;
                bool escape = false;
                bool closing = false;
                menu.DropDown += (sender, args) =>
                {
                    openedChoice = menu.SelectedItem as Preset;
                    escape = false;
                    closing = false;
                };
                menu.KeyDown += (sender, args) =>
                {
                    if ((menu.DroppedDown || closing) && args.KeyCode == Keys.Escape)
                        escape = true;
                };
                menu.DropDownClosed += (sender, args) =>
                {
                    closing = true;
                    menu.BeginInvoke((Action)(() =>
                    {
                        closing = false;
                        if (menu.IsDisposed || openedChoice == null) return;
                        if (escape) SetChoice(kind, openedChoice.Id);
                        else if (menu.SelectedItem != openedChoice)
                            SelectPreset(kind, menu.SelectedItem as Preset);
                    }));
                };
            }
            var choose = new Button
            {
                Name = icon ? "ChooseLocalWindowIcon" : "ChooseLocalStartupImage",
                Text = fileName, AutoSize = true, Anchor = AnchorStyles.Left
            };
            choose.Click += (sender, args) => ChooseLocalAsset(kind, (Preset)menu.SelectedItem, fileName);
            row.Controls.Add(menu, 0, 0);
            row.Controls.Add(choose, 1, 0);
            combo = menu;
            return row;
        }

        private void SelectPreset(AppearanceAsset kind, Preset preset)
        {
            if (synchronizingPresets || preset == null) return;
            var icon = kind == AppearanceAsset.WindowIcon;
            var store = DesktopAppearance.Store;
            var active = store.CurrentPreset(kind);
            if (active == preset.Id) return;
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
                status.Text = string.Format(Local("“{0}”尚未导入。请选本地文件；实际仍用“{1}”。",
                    "“{0}” has no local file. Choose one; “{1}” remains active."), preset,
                    CaptionFor(kind, active));
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

        private string CaptionFor(AppearanceAsset kind, string id)
        {
            var menu = kind == AppearanceAsset.WindowIcon ? iconChoices : startupChoices;
            if (menu != null)
                foreach (Preset preset in menu.Items)
                    if (preset.Id == id) return preset.ToString();
            return id;
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
                    status.Text += " " + (ex.Message == new InvalidDataException().Message
                        ? Local("保存的素材不可用，请重新导入本地文件。", "Saved artwork is unavailable. Reimport the local file.")
                        : UiText.Translate(ex.Message));
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
            // Synchronize selectors even when a thumbnail cannot be decoded or rendered.
            SetChoice(AppearanceAsset.WindowIcon, DesktopAppearance.Store.CurrentPreset(AppearanceAsset.WindowIcon));
            SetChoice(AppearanceAsset.StartupImage, DesktopAppearance.Store.CurrentPreset(AppearanceAsset.StartupImage));
            string warning = null;
            string previewWarning = null;
            try
            {
                var icon = ThemeManager.FormIcon;
                ReplacePreview(iconPreview, icon == null ? null : icon.ToBitmap());
            }
            catch (Exception) { previewWarning = "Appearance preview unavailable."; }
            try { ReplacePreview(startupPreview, DesktopAppearance.CreateStartupPreview(out warning)); }
            catch (Exception) { previewWarning = "Appearance preview unavailable."; }
            warning = DesktopAppearance.IconWarning ?? warning ?? previewWarning;
            status.Text = warning == null
                ? Local("预设缺素材时，先选该项，再导入本地文件。",
                    "To supply a missing preset, select it, then choose a local file.")
                : UiText.Translate(warning);
            return warning;
        }

        private static void ReplacePreview(PictureBox preview, Image image)
        {
            var previous = preview.Image;
            preview.Image = image;
            previous?.Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                details.Dispose();
                iconPreview.Image?.Dispose();
                startupPreview.Image?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
#endif
