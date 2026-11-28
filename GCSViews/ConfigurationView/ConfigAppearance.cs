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
            if (!embedded)
                AddText(layout, UiText.Translate("Window icon and startup image"));
            var iconTitle = AddText(layout, UiText.Translate("Window icon · applies now"));
            layout.Controls.Add(PresetRow(AppearanceAsset.WindowIcon, out iconChoices));
            var startupTitle = AddText(layout, UiText.Translate("Startup image · applies on next start"));
            layout.Controls.Add(PresetRow(AppearanceAsset.StartupImage, out startupChoices));

            var previews = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2 };
            previews.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            previews.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 75));
            iconPreview.Name = "WindowIconPreview";
            iconPreview.AccessibleName = UiText.Translate("Active window icon preview");
            iconPreview.MaximumSize = new Size(40, 40);
            startupPreview.Name = "StartupImagePreview";
            startupPreview.AccessibleName = UiText.Translate("Next startup image preview");
            previews.Controls.Add(iconPreview, 0, 0);
            previews.Controls.Add(startupPreview, 1, 0);
            layout.Controls.Add(previews);
            details.SetToolTip(iconTitle, UiText.Translate("ICO: up to 4 MiB, 1-256 pixels per frame (up to 64 frames). Window icons apply now and on restart.") +
                "\n" + UiText.Translate("EXE, installer and Explorer icons require a rebuild. Existing or pinned shortcuts keep their own icons; change or recreate them separately. Windows may retain a cached taskbar icon."));
            details.SetToolTip(startupTitle, UiText.Translate("PNG, JPEG or BMP: up to 16 MiB, 4096 pixels per side. The full image is fitted without cropping. Applies on next startup."));
            details.SetToolTip(status, UiText.Translate("Choosing a file beside a built-in image or default imports it as custom. Vendor icons are not bundled or downloaded.") +
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

        private Control PresetRow(AppearanceAsset kind, out ComboBox combo)
        {
            var icon = kind == AppearanceAsset.WindowIcon;
            var pickerName = icon ? UiText.Translate("Window icon") : UiText.Translate("Startup image");
            var fileName = icon ? UiText.Translate("Choose local icon…") : UiText.Translate("Choose local startup image…");
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
            menu.Items.Add(new Preset("default", icon ? UiText.Translate("Packaged default icon") : UiText.Translate("TitanPlanner artwork (default)")));
            menu.Items.Add(new Preset("mission-planner", icon ? UiText.Translate("Mission Planner (local)") : UiText.Translate("Mission Planner artwork")));
            if (icon)
                menu.Items.Add(new Preset("titan-planner", UiText.Translate("TitanPlanner (local)")));
            menu.Items.Add(new Preset("mfe", UiText.Translate("MakeFlyEasy")));
            menu.Items.Add(new Preset("custom", UiText.Translate("Custom")));
            menu.SelectedIndex = 0;
            UpdatePresetCaption(menu);
            menu.DropDown += (sender, args) => SizePresetPopup(menu);
            // Closed-list arrow keys can change the index without raising SelectionChangeCommitted
            // (in particular on Mono). Do not apply intermediate choices in an open list.
            menu.SelectedIndexChanged += (sender, args) =>
            {
                UpdatePresetCaption(menu);
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
            else
            {
                Preset openedChoice = null;
                bool committed = false;
                int opening = 0;
                menu.DropDown += (sender, args) =>
                {
                    opening++;
                    openedChoice = menu.SelectedItem as Preset;
                    committed = false;
                };
                menu.SelectionChangeCommitted += (sender, args) => committed = true;
                menu.DropDownClosed += (sender, args) =>
                {
                    var closedOpening = opening;
                    var previous = openedChoice;
                    // Finish processing native notifications before restoring an unconfirmed highlight.
                    menu.BeginInvoke((Action)(() =>
                    {
                        if (menu.IsDisposed || opening != closedOpening || committed || previous == null) return;
                        SetChoice(kind, previous.Id);
                        UpdatePresetCaption(menu);
                    }));
                };
            }
            var choose = new Button
            {
                Name = icon ? "ChooseLocalWindowIcon" : "ChooseLocalStartupImage",
                Text = fileName, AccessibleName = fileName, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink, Anchor = AnchorStyles.Left
            };
            choose.Click += (sender, args) => ChooseLocalAsset(kind, (Preset)menu.SelectedItem, fileName);
            row.Controls.Add(menu, 0, 0);
            row.Controls.Add(choose, 1, 0);
            Action fitCaption = () =>
            {
                if (Disposing || IsDisposed || row.ClientSize.Width <= 0 || choose.IsDisposed)
                    return;
                int available = row.ClientSize.Width - row.Padding.Horizontal -
                    menu.Margin.Horizontal - choose.Margin.Horizontal;
                int selectorSpace = Math.Max(96, menu.Font.Height * 4);
                int fullButton = TextRenderer.MeasureText(fileName, choose.Font).Width + choose.Padding.Horizontal + 12;
                bool compact = available - fullButton < selectorSpace;
                string caption = compact ? UiText.Translate("Browse") + "…" : fileName;
                if (choose.Text != caption)
                {
                    choose.Text = caption;
                    details.SetToolTip(choose, compact ? fileName : null);
                }
            };
            row.SizeChanged += (sender, args) => fitCaption();
            choose.FontChanged += (sender, args) => fitCaption();
            fitCaption();
            combo = menu;
            return row;
        }

        private void UpdatePresetCaption(ComboBox menu)
        {
            string caption = menu.GetItemText(menu.SelectedItem);
            details.SetToolTip(menu, caption);
            menu.AccessibleDescription = caption;
        }

        private static void SizePresetPopup(ComboBox menu)
        {
            using (var graphics = menu.CreateGraphics())
            {
                int widest = 0;
                foreach (var item in menu.Items)
                    widest = Math.Max(widest, TextRenderer.MeasureText(graphics, menu.GetItemText(item), menu.Font,
                        Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width);
                int chrome = SystemInformation.VerticalScrollBarWidth + (int)Math.Ceiling(8 * graphics.DpiX / 96);
                int desired = Math.Max(Math.Max(260, menu.Width), widest + chrome);
                menu.DropDownWidth = Math.Max(1, Math.Min(desired, Screen.FromControl(menu).WorkingArea.Width));
            }
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
                status.Text = string.Format(UiText.Translate("“{0}” has no local file. Choose one; “{1}” remains active."), preset,
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
                    status.Text = UiText.Translate("Cancelled; the current appearance is unchanged.");
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
                if (ex is InvalidDataException validation)
                {
                    var messageKey = AppearanceValidationMessageKey(validation.Message);
                    if (messageKey != null)
                        status.Text += " " + UiText.Translate(messageKey);
                }
                return;
            }
            string warning;
            try { warning = RefreshPreviews(); }
            catch (Exception) { status.Text = UiText.Translate("Appearance saved; preview unavailable."); return; }
            status.Text = UiText.Translate("Appearance saved. Window icons apply now; startup image applies on next startup.") +
                (warning == null ? "" : " " + UiText.Translate(warning));
        }

        private static string AppearanceValidationMessageKey(string message)
        {
            if (message == new InvalidDataException().Message)
                return "Saved artwork is unavailable. Reimport the local file.";

            // Only known local validation text is suitable for display. Decoder errors
            // can contain file paths and implementation-specific details.
            switch (message)
            {
                case "Built-in startup image unavailable; using default.":
                case "Choose a PNG, JPEG or BMP image (up to 16 MiB and 4096 pixels per side).":
                case "Choose a valid ICO file with frames from 1 to 256 pixels.":
                case "Choose an ICO file (up to 4 MiB).":
                case "Image file exceeds the size limit or is empty.":
                case "Incomplete image file.":
                case "Invalid ICO directory.":
                case "Invalid ICO frame dimensions.":
                case "Invalid ICO frame.":
                case "No saved startup image is available.":
                case "The image format does not match its file extension.":
                case "This ICO frame is unsupported on Mono. Use a 32-bit DIB or PNG frame.":
                    return message;
                default:
                    return null;
            }
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
            catch (Exception)
            {
                ReplacePreview(iconPreview, null);
                previewWarning = "Appearance preview unavailable.";
            }
            try { ReplacePreview(startupPreview, DesktopAppearance.CreateStartupPreview(out warning)); }
            catch (Exception)
            {
                ReplacePreview(startupPreview, null);
                previewWarning = "Appearance preview unavailable.";
            }
            warning = DesktopAppearance.IconWarning ?? warning ?? previewWarning;
            status.Text = warning == null
                ? UiText.Translate("To supply a missing preset, select it, then choose a local file.")
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
