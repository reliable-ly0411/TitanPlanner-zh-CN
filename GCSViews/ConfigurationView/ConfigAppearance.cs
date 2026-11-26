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
        private Button savedStartup, savedIcon, mfeIcon, mfeStartup, missionPlannerIcon, titanPlannerIcon;
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
            layout.Controls.Add(iconPreview);
            layout.Controls.Add(Buttons("Choose ICO...", "Restore default icon", AppearanceAsset.WindowIcon));
            AddText(layout, "PNG, JPEG or BMP: up to 16 MiB, 4096 pixels per side. The full image is fitted without cropping. Applies on next startup.");
            layout.Controls.Add(startupPreview);
            layout.Controls.Add(Buttons("Choose startup image...", "Restore default startup image", AppearanceAsset.StartupImage));
            var cultureName = System.Globalization.CultureInfo.CurrentUICulture.Name;
            bool enhancedChinese = string.Equals(cultureName, "zh-CN", StringComparison.Ordinal);
            bool vendorIconChinese = enhancedChinese || string.Equals(cultureName, "zh-Hans", StringComparison.Ordinal);
            AddText(layout, enhancedChinese ? "\u594b\u6597\u8005\u9884\u8bbe\uff1a\u53ea\u4f7f\u7528\u624b\u52a8\u5bfc\u5165\u7684\u672c\u5730\u7d20\u6750\u3002" :
                "MakeFlyEasy presets use only files you import locally.");
            AddText(layout, enhancedChinese
                ? "窗口图标预设使用你本地导入的 ICO，不附带或下载厂商图标。"
                : "Window icon presets use ICO files you import locally; vendor icons are not bundled or downloaded.");
            missionPlannerIcon = AddVendorIconRow(layout, ImportedVendorIcon.MissionPlanner, "Mission Planner", vendorIconChinese);
            titanPlannerIcon = AddVendorIconRow(layout, ImportedVendorIcon.TitanPlanner, "TitanPlanner", vendorIconChinese);
            var mfeIconRow = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
            mfeIcon = PresetButton(mfeIconRow, enhancedChinese ? "\u9009\u62e9\u594b\u6597\u8005\u7a97\u53e3\u56fe\u6807" : "Use MakeFlyEasy window icon",
                () => DesktopAppearance.SelectMfe(AppearanceAsset.WindowIcon));
            PresetButton(mfeIconRow, enhancedChinese ? "\u5bfc\u5165\u594b\u6597\u8005 ICO..." : "Import MakeFlyEasy ICO...",
                () => ImportMfe(AppearanceAsset.WindowIcon), wrapSave: false);
            savedIcon = PresetButton(mfeIconRow, enhancedChinese ? "\u6362\u56de\u81ea\u5b9a\u4e49\u7a97\u53e3\u56fe\u6807" : "Use saved custom window icon",
                DesktopAppearance.SelectSavedIcon);
            layout.Controls.Add(mfeIconRow);
            var mfeStartupRow = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
            mfeStartup = PresetButton(mfeStartupRow, enhancedChinese ? "\u9009\u62e9\u594b\u6597\u8005\u542f\u52a8\u56fe" : "Use MakeFlyEasy startup artwork",
                () => DesktopAppearance.SelectMfe(AppearanceAsset.StartupImage));
            PresetButton(mfeStartupRow, enhancedChinese ? "\u5bfc\u5165\u594b\u6597\u8005\u542f\u52a8\u56fe..." : "Import MakeFlyEasy startup artwork...",
                () => ImportMfe(AppearanceAsset.StartupImage), wrapSave: false);
            layout.Controls.Add(mfeStartupRow);
            var missionStartup = new Button
            {
                AutoSize = true, Anchor = AnchorStyles.Left,
                Text = enhancedChinese ? "选用 Mission Planner 原启动图" : "Use Mission Planner startup artwork"
            };
            missionStartup.Click += (sender, args) => Change(DesktopAppearance.SelectMissionPlannerStartup);
            layout.Controls.Add(missionStartup);
            savedStartup = new Button
            {
                AutoSize = true, Anchor = AnchorStyles.Left,
                Text = enhancedChinese ? "改回已保存的自定义启动图" : "Use saved custom startup artwork"
            };
            savedStartup.Click += (sender, args) => Change(DesktopAppearance.Store.SelectSavedStartupImage);
            layout.Controls.Add(savedStartup);
            AddText(layout, enhancedChinese ? "默认启动图即为 TitanPlanner 原图。" : "The default startup artwork is the original TitanPlanner image.");
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

        private Control Buttons(string chooseText, string resetText, AppearanceAsset kind)
        {
            var flow = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true };
            var choose = new Button { Text = UiText.Translate(chooseText), AutoSize = true };
            var reset = new Button { Text = UiText.Translate(resetText), AutoSize = true };
            choose.Click += (sender, args) =>
            {
                using (var dialog = new OpenFileDialog
                {
                    Filter = kind == AppearanceAsset.WindowIcon ? "ICO (*.ico)|*.ico" : "PNG / JPEG / BMP|*.png;*.jpg;*.jpeg;*.bmp",
                    CheckFileExists = true, Multiselect = false, RestoreDirectory = true,
                    Title = UiText.Translate(chooseText)
                })
                {
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                        Change(() => DesktopAppearance.Import(kind, dialog.FileName));
                }
            };
            reset.Click += (sender, args) => Change(() => DesktopAppearance.RestoreDefault(kind));
            flow.Controls.Add(choose);
            flow.Controls.Add(reset);
            return flow;
        }

        private Button PresetButton(FlowLayoutPanel row, string text, Action action, bool wrapSave = true)
        {
            var button = new Button { AutoSize = true, Text = text };
            button.Click += (sender, args) => { if (wrapSave) Change(action); else action(); };
            row.Controls.Add(button);
            return button;
        }

        private Button AddVendorIconRow(TableLayoutPanel layout, ImportedVendorIcon vendor, string brand, bool simplifiedChinese)
        {
            var row = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true };
            string use = simplifiedChinese ? "选用 " + brand + " 窗口图标" : "Use " + brand + " window icon";
            string import = simplifiedChinese ? "导入 " + brand + " ICO..." : "Import " + brand + " ICO...";
            var select = PresetButton(row, use, () => DesktopAppearance.SelectVendorIcon(vendor));
            PresetButton(row, import, () => ImportAppearance(AppearanceAsset.WindowIcon,
                path => DesktopAppearance.ImportVendorIcon(vendor, path), import), wrapSave: false);
            layout.Controls.Add(row);
            return select;
        }

        private void ImportMfe(AppearanceAsset kind)
        {
            ImportAppearance(kind, path => DesktopAppearance.ImportMfe(kind, path));
        }

        private void ImportAppearance(AppearanceAsset kind, Action<string> import, string title = null)
        {
            using (var dialog = new OpenFileDialog
            {
                Filter = kind == AppearanceAsset.WindowIcon ? "ICO (*.ico)|*.ico" : "PNG / JPEG / BMP|*.png;*.jpg;*.jpeg;*.bmp",
                CheckFileExists = true, Multiselect = false, RestoreDirectory = true,
                Title = title ?? ""
            })
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    Change(() => import(dialog.FileName));
        }

        private void Change(Action save)
        {
            try
            {
                save();
            }
            catch (Exception ex)
            {
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
            if (savedStartup != null)
                savedStartup.Enabled = DesktopAppearance.Store.CanSelectSavedStartupImage();
            if (mfeIcon != null)
                mfeIcon.Enabled = DesktopAppearance.Store.CanSelectMfe(AppearanceAsset.WindowIcon);
            if (mfeStartup != null)
                mfeStartup.Enabled = DesktopAppearance.Store.CanSelectMfe(AppearanceAsset.StartupImage);
            if (savedIcon != null)
                savedIcon.Enabled = DesktopAppearance.Store.CanSelectSavedIcon();
            if (missionPlannerIcon != null)
                missionPlannerIcon.Enabled = DesktopAppearance.Store.CanSelectVendorIcon(ImportedVendorIcon.MissionPlanner);
            if (titanPlannerIcon != null)
                titanPlannerIcon.Enabled = DesktopAppearance.Store.CanSelectVendorIcon(ImportedVendorIcon.TitanPlanner);
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
