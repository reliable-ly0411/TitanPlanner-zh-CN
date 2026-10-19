using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using log4net;
using MissionPlanner.GCSViews;
using MissionPlanner.Utilities;

namespace MissionPlanner.Controls
{
    /// <summary>
    /// Lists the map areas that have been downloaded into the tile cache, downloads more, jumps to an
    /// area on the map, and moves areas between computers as .mappack files.
    /// Opened from the PLAN tab's Offline Maps button; one window at a time, modeless so the user can
    /// still pan the map and ALT-select an area while it is open.
    ///
    /// Layout: a detail panel on the left with a preview of the selected area composited from its cached
    /// tiles, the area list filling the right, and one row of actions along the bottom.
    /// </summary>
    public class OfflineMapsForm : Form
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static OfflineMapsForm Current { get; private set; }

        private const string LastDirSetting = "OfflineMapsLastDir";
        private const int DetailPanelWidth = 430;
        private const int PreviewTilesPerSide = 4;

        private readonly FlightPlanner _planner;
        private readonly ConcurrentDictionary<string, OfflineMapPack.RegionStats> _stats =
            new ConcurrentDictionary<string, OfflineMapPack.RegionStats>();
        private CancellationTokenSource _statsCts;

        // rendered previews by region id; cleared whenever the tiles may have changed
        private readonly Dictionary<string, Image> _previews = new Dictionary<string, Image>();
        private readonly Dictionary<string, string> _previewCaptions = new Dictionary<string, string>();
        private CancellationTokenSource _previewCts;
        private string _detailRegionId;
        private string _previewCaption = "";
        private double _coverage = -1;

        private Panel _statusStrip;
        private Panel _body;
        private Panel _detailHost;
        private Panel _separator;
        private ListView _list;
        private Label _lblEmpty;
        private PictureBox _preview;
        private Label _lblName;
        private Label _lblSubtitle;
        private Panel _coverageBar;
        private Label _valSize, _valTiles, _valCoverage, _valDisk, _valAdded, _valSource;
        private readonly List<Label> _dimLabels = new List<Label>();
        private Label _lblStatus;
        private MyButton _btnDownload, _btnImport, _btnShow, _btnRename, _btnExport, _btnDelete, _btnRefresh, _btnFolder;

        private Color _dimColor;
        private Color _lineColor;
        private Color _accentColor;

        public static void ShowFor(FlightPlanner planner)
        {
            if (Current != null && !Current.IsDisposed)
            {
                if (Current.WindowState == FormWindowState.Minimized)
                    Current.WindowState = FormWindowState.Normal;
                Current.Activate();
                return;
            }

            OfflineMapsForm form;
            try
            {
                form = new OfflineMapsForm(planner);
                ThemeManager.ApplyThemeTo(form);
            }
            catch (Exception ex)
            {
                // never leave a half-built instance behind, or later clicks would just activate it
                log.Error("Offline Maps window failed to open", ex);
                CustomMessageBox.Show("The Offline Maps window could not be opened.\n\n" + ex.Message, "Offline Maps");
                return;
            }

            Current = form;
            var owner = planner.FindForm();
            if (owner != null)
                form.Show(owner);
            else
                form.Show();
        }

        private OfflineMapsForm(FlightPlanner planner)
        {
            _planner = planner;

            BuildLayout();

            OfflineMapRegistry.Changed += OnRegistryChanged;
            FormClosed += (s, e) =>
            {
                OfflineMapRegistry.Changed -= OnRegistryChanged;
                _statsCts?.Cancel();
                _previewCts?.Cancel();
                ClearPreviews();
                if (Current == this)
                    Current = null;
            };
        }

        #region Layout

        private void BuildLayout()
        {
            Text = "Offline Maps";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1080, 650);
            MinimumSize = new Size(900, 600);
            ShowInTaskbar = false;
            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                    Close();
            };

            var tips = new ToolTip();

            // body: a fixed-width detail panel on the left, the list fills the rest
            _body = new Panel { Dock = DockStyle.Fill };
            _detailHost = new Panel { Dock = DockStyle.Left, Width = DetailPanelWidth };
            _separator = new Panel { Dock = DockStyle.Left, Width = 1 };

            var listHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 12, 16, 12) };
            _list = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                MultiSelect = true,
                ShowItemToolTips = true,
                BorderStyle = BorderStyle.None,
                // a 1px-wide image list is the standard trick for taller, more readable rows
                SmallImageList = new ImageList { ImageSize = new Size(1, 26) }
            };
            _list.Columns.Add("Name", 220);
            _list.Columns.Add("Map source", 150);
            _list.Columns.Add("Zoom", 60);
            _list.Columns.Add("Tiles on disk", 150);
            _list.Columns.Add("Size", 80);
            _list.SelectedIndexChanged += (s, e) => OnSelectionChanged();
            _list.DoubleClick += (s, e) => ShowOnMap();
            _list.Resize += (s, e) => FitNameColumn();
            _list.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Delete)
                    DeleteSelected();
                else if (e.Control && e.KeyCode == Keys.A)
                    foreach (ListViewItem item in _list.Items)
                        item.Selected = true;
            };
            _lblEmpty = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10F),
                Text = "No offline map areas yet.\n\nClick Download… while online, or Import… a .mappack file.",
                Visible = false
            };
            _dimLabels.Add(_lblEmpty);
            listHost.Controls.Add(_list);
            listHost.Controls.Add(_lblEmpty);
            _lblEmpty.BringToFront();
            _detailHost.Controls.Add(BuildDetailPanel());
            // fill first so the left-docked panels are laid out before it
            _body.Controls.Add(listHost);
            _body.Controls.Add(_separator);
            _body.Controls.Add(_detailHost);

            // bottom strip: every action on one row, cache totals underneath
            _statusStrip = new Panel { Dock = DockStyle.Bottom, Height = 86 };
            var buttonRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                Padding = new Padding(16, 12, 16, 0)
            };
            _btnDownload = MakeButton(buttonRow, tips, "Download…", 118,
                "Download the area shown on the map, or the area selected with ALT + drag", (s, e) => Download());
            _btnShow = MakeButton(buttonRow, tips, "Show on Map", 100, "Outline the selected area on the map and zoom to it", (s, e) => ShowOnMap());
            _btnRename = MakeButton(buttonRow, tips, "Rename", 80, "Rename the selected area", (s, e) => Rename());
            _btnExport = MakeButton(buttonRow, tips, "Export…", 90, "Save the selected areas and their tiles to a .mappack file", (s, e) => Export());
            _btnImport = MakeButton(buttonRow, tips, "Import…", 100,
                "Load areas and tiles from a .mappack file into this computer's map cache", (s, e) => Import());
            _btnDelete = MakeButton(buttonRow, tips, "Delete", 80, "Remove the selected areas from the list, optionally deleting their tiles", (s, e) => DeleteSelected());
            _btnRefresh = MakeButton(buttonRow, tips, "Refresh", 80, "Recount tiles on disk and redraw previews", (s, e) => RefreshList());
            _btnFolder = MakeButton(buttonRow, tips, "Open Folder", 100, "Open the tile cache folder in Explorer", (s, e) => OpenFolder());
            _lblStatus = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                Padding = new Padding(16, 0, 16, 0)
            };
            _dimLabels.Add(_lblStatus);
            // fill first so the top-docked row is laid out before it
            _statusStrip.Controls.Add(_lblStatus);
            _statusStrip.Controls.Add(buttonRow);

            // fill first so the bottom strip is laid out before it
            Controls.Add(_body);
            Controls.Add(_statusStrip);
        }

        private Control BuildDetailPanel()
        {
            var detail = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(16, 12, 16, 12)
            };
            detail.RowStyles.Add(new RowStyle(SizeType.Absolute, 250)); // preview
            detail.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // name
            detail.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // subtitle
            detail.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // facts
            detail.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // spacer

            _preview = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                Margin = new Padding(0, 0, 0, 12)
            };
            _preview.Paint += PreviewPaint;

            _lblName = new Label
            {
                Dock = DockStyle.Top,
                Height = 32,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                Margin = new Padding(0)
            };
            _lblSubtitle = new Label
            {
                Dock = DockStyle.Top,
                Height = 22,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 0, 8)
            };
            _dimLabels.Add(_lblSubtitle);

            var facts = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                Margin = new Padding(0, 4, 0, 0)
            };
            facts.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
            facts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _valSize = AddFact(facts, "Size");
            _valTiles = AddFact(facts, "Tiles");
            _valCoverage = AddCoverageFact(facts);
            _valDisk = AddFact(facts, "On disk");
            _valAdded = AddFact(facts, "Added");
            _valSource = AddFact(facts, "Source");

            detail.Controls.Add(_preview, 0, 0);
            detail.Controls.Add(_lblName, 0, 1);
            detail.Controls.Add(_lblSubtitle, 0, 2);
            detail.Controls.Add(facts, 0, 3);
            return detail;
        }

        private Label AddFact(TableLayoutPanel facts, string key)
        {
            int row = facts.RowCount++;
            facts.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var keyLabel = new Label { Text = key, AutoSize = true, Margin = new Padding(0, 4, 0, 4) };
            _dimLabels.Add(keyLabel);
            var value = new Label { Text = "—", AutoSize = true, Margin = new Padding(0, 4, 0, 4) };
            facts.Controls.Add(keyLabel, 0, row);
            facts.Controls.Add(value, 1, row);
            return value;
        }

        private Label AddCoverageFact(TableLayoutPanel facts)
        {
            int row = facts.RowCount++;
            facts.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var keyLabel = new Label { Text = "Coverage", AutoSize = true, Margin = new Padding(0, 4, 0, 4) };
            _dimLabels.Add(keyLabel);

            var host = new Panel { Height = 22, Dock = DockStyle.Fill, Margin = new Padding(0, 2, 0, 2) };
            _coverageBar = new Panel { Location = new Point(0, 7), Size = new Size(170, 8) };
            _coverageBar.Paint += CoverageBarPaint;
            var value = new Label { AutoSize = true, Location = new Point(180, 3), Text = "—" };
            host.Controls.Add(_coverageBar);
            host.Controls.Add(value);

            facts.Controls.Add(keyLabel, 0, row);
            facts.Controls.Add(host, 1, row);
            return value;
        }

        private static MyButton MakeButton(Control parent, ToolTip tips, string text, int width, string tip, EventHandler onClick)
        {
            var button = new MyButton { Text = text, Size = new Size(width, 28), Margin = new Padding(0, 0, 8, 6) };
            button.Click += onClick;
            tips.SetToolTip(button, tip);
            parent.Controls.Add(button);
            return button;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // the theme has been applied by now; derive the secondary colours from it
            var bg = BackColor;
            var fg = ForeColor;
            _dimColor = Blend(bg, fg, 0.55);
            _lineColor = Blend(bg, fg, 0.18);
            _accentColor = ThemeManager.BannerColor2.A == 0 || ThemeManager.BannerColor2 == Color.Empty
                ? Color.FromArgb(0, 150, 136)
                : ThemeManager.BannerColor2;

            _statusStrip.BackColor = Blend(bg, fg, 0.05);
            _separator.BackColor = _lineColor;
            _detailHost.BackColor = Blend(bg, fg, 0.025);
            // the list sits a shade darker than the window so it reads as a distinct surface
            _list.BackColor = Blend(bg, Color.Black, 0.22);
            _lblEmpty.BackColor = _list.BackColor;
            _preview.BackColor = Blend(bg, Color.Black, 0.35);
            foreach (var label in _dimLabels)
                label.ForeColor = _dimColor;

            RefreshList();
        }

        private void FitNameColumn()
        {
            if (_list.Columns.Count == 0)
                return;
            int others = 0;
            for (int i = 1; i < _list.Columns.Count; i++)
                others += _list.Columns[i].Width;
            _list.Columns[0].Width = Math.Max(140, _list.ClientSize.Width - others - 4);
        }

        private static Color Blend(Color from, Color to, double amount)
        {
            return Color.FromArgb(
                (int)(from.R + (to.R - from.R) * amount),
                (int)(from.G + (to.G - from.G) * amount),
                (int)(from.B + (to.B - from.B) * amount));
        }

        private void PreviewPaint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var rect = _preview.ClientRectangle;

            using (var pen = new Pen(_lineColor))
                g.DrawRectangle(pen, 0, 0, rect.Width - 1, rect.Height - 1);

            if (string.IsNullOrEmpty(_previewCaption))
                return;

            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            using (var font = new Font("Segoe UI", 8.5F))
            {
                if (_preview.Image == null)
                {
                    // nothing to show: caption sits in the middle of the empty box
                    using (var brush = new SolidBrush(_dimColor))
                    using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        g.DrawString(_previewCaption, font, brush, rect, format);
                    return;
                }

                var size = g.MeasureString(_previewCaption, font);
                var box = new RectangleF(8, rect.Height - size.Height - 10, size.Width + 10, size.Height + 4);
                using (var shade = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
                    g.FillRectangle(shade, box);
                g.DrawString(_previewCaption, font, Brushes.White, box.X + 5, box.Y + 2);
            }
        }

        private void CoverageBarPaint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var rect = _coverageBar.ClientRectangle;
            using (var track = new SolidBrush(_lineColor))
                g.FillRectangle(track, rect);
            if (_coverage <= 0)
                return;
            int width = (int)Math.Round(rect.Width * Math.Min(1.0, _coverage));
            using (var fill = new SolidBrush(_coverage >= 0.999 ? _accentColor : Color.FromArgb(230, 160, 40)))
                g.FillRectangle(fill, 0, 0, Math.Max(2, width), rect.Height);
        }

        #endregion

        #region List and details

        private void OnRegistryChanged()
        {
            if (IsDisposed || !IsHandleCreated)
                return;
            try
            {
                BeginInvoke(new Action(RefreshList));
            }
            catch (InvalidOperationException)
            {
            }
        }

        private List<OfflineMapRegion> SelectedRegions()
        {
            return _list.SelectedItems.Cast<ListViewItem>().Select(i => (OfflineMapRegion)i.Tag).ToList();
        }

        private void UpdateButtons()
        {
            int selected = _list.SelectedItems.Count;
            _btnShow.Enabled = selected == 1;
            _btnRename.Enabled = selected == 1;
            _btnExport.Enabled = selected >= 1;
            _btnDelete.Enabled = selected >= 1;
        }

        public void RefreshList()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(RefreshList));
                return;
            }

            _statsCts?.Cancel();
            _statsCts = new CancellationTokenSource();
            var ct = _statsCts.Token;
            _stats.Clear();
            ClearPreviews();

            var previouslySelected = new HashSet<string>(SelectedRegions().Select(r => r.Id));
            var regions = OfflineMapRegistry.Regions;

            _list.BeginUpdate();
            _list.Items.Clear();
            foreach (var region in regions)
            {
                var item = new ListViewItem(region.Name ?? "") { Tag = region, Selected = previouslySelected.Contains(region.Id) };
                item.SubItems.Add(region.Provider);
                item.SubItems.Add(ZoomText(region));
                item.SubItems.Add("…");
                item.SubItems.Add("…");

                if (OfflineMapPack.FindProvider(region.Provider) == null)
                {
                    item.ForeColor = Color.Gray;
                    item.ToolTipText = "No map source with this name exists on this computer, so these tiles cannot be shown.";
                }

                _list.Items.Add(item);
            }
            _list.EndUpdate();
            FitNameColumn();

            _lblEmpty.Visible = regions.Count == 0;
            if (regions.Count > 0 && _list.SelectedItems.Count == 0)
                _list.Items[0].Selected = true;

            _lblStatus.Text = regions.Count == 0
                ? "The tile cache is at " + OfflineMapPack.CacheRoot
                : regions.Count + " area(s) · counting tiles…";

            OnSelectionChanged();
            Task.Run(() => ComputeStats(regions, ct), ct);
        }

        private static string ZoomText(OfflineMapRegion region)
        {
            return region.MinZoom == region.MaxZoom
                ? region.MinZoom.ToString()
                : region.MinZoom + "–" + region.MaxZoom;
        }

        private void OnSelectionChanged()
        {
            UpdateButtons();
            var regions = SelectedRegions();
            ShowDetails(regions.Count == 1 ? regions[0] : null, regions.Count);
        }

        private void ShowDetails(OfflineMapRegion region, int selectedCount)
        {
            if (region == null)
            {
                _detailRegionId = null;
                _previewCts?.Cancel();
                _preview.Image = null;
                _previewCaption = selectedCount > 1 ? selectedCount + " areas selected" : "Select an area to preview it";
                _preview.Invalidate();

                _lblName.Text = selectedCount > 1 ? selectedCount + " areas selected" : "No area selected";
                _lblSubtitle.Text = selectedCount > 1 ? "Export and Delete act on all selected areas" : "";
                foreach (var value in new[] { _valSize, _valTiles, _valCoverage, _valDisk, _valAdded, _valSource })
                    value.Text = "—";
                _coverage = -1;
                _coverageBar.Invalidate();
                return;
            }

            _detailRegionId = region.Id;
            _lblName.Text = region.Name;
            _lblSubtitle.Text = region.Provider + "  ·  zoom " + ZoomText(region);

            double widthKm, heightKm;
            OfflineMapPack.GetSizeKm(region.Bounds, out widthKm, out heightKm);
            _valSize.Text = string.Format(CultureInfo.CurrentCulture, "{0:0.#} × {1:0.#} km", widthKm, heightKm);
            _valAdded.Text = region.Created.ToString("yyyy-MM-dd HH:mm");
            _valSource.Text = region.Source ?? "";

            OfflineMapPack.RegionStats stats;
            if (_stats.TryGetValue(region.Id, out stats))
                ApplyStats(stats);
            else
            {
                _valTiles.Text = "counting…";
                _valCoverage.Text = "";
                _valDisk.Text = "…";
                _coverage = -1;
                _coverageBar.Invalidate();
            }

            ShowPreview(region);
        }

        private void ApplyStats(OfflineMapPack.RegionStats stats)
        {
            _valTiles.Text = stats.TilesExpected > 0
                ? string.Format("{0:N0} of {1:N0}", stats.TilesOnDisk, stats.TilesExpected)
                : stats.TilesOnDisk.ToString("N0");
            _coverage = stats.TilesExpected > 0 ? stats.Coverage : -1;
            _valCoverage.Text = stats.TilesExpected > 0 ? stats.Coverage.ToString("P0") : "";
            _valDisk.Text = OfflineMapPack.FormatSize(stats.BytesOnDisk);
            _coverageBar.Invalidate();
        }

        private void ComputeStats(List<OfflineMapRegion> regions, CancellationToken ct)
        {
            try
            {
                foreach (var region in regions)
                {
                    ct.ThrowIfCancellationRequested();
                    var stats = OfflineMapPack.GetStats(region, ct);
                    _stats[region.Id] = stats;

                    string id = region.Id;
                    SafeInvoke(() =>
                    {
                        foreach (ListViewItem item in _list.Items)
                        {
                            if (((OfflineMapRegion)item.Tag).Id != id)
                                continue;
                            item.SubItems[3].Text = stats.TilesExpected > 0
                                ? string.Format("{0:N0} / {1:N0}  ({2:P0})", stats.TilesOnDisk, stats.TilesExpected, stats.Coverage)
                                : stats.TilesOnDisk.ToString("N0");
                            item.SubItems[4].Text = OfflineMapPack.FormatSize(stats.BytesOnDisk);
                        }

                        if (_detailRegionId == id)
                            ApplyStats(stats);
                    });
                }

                long files, bytes;
                OfflineMapPack.GetCacheTotals(ct, out files, out bytes);
                SafeInvoke(() => _lblStatus.Text = string.Format("{0} area(s)  ·  the whole tile cache holds {1:N0} tiles, {2}",
                    regions.Count, files, OfflineMapPack.FormatSize(bytes)));
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private void ShowPreview(OfflineMapRegion region)
        {
            Image cached;
            if (_previews.TryGetValue(region.Id, out cached))
            {
                _preview.Image = cached;
                _previewCaption = _previewCaptions[region.Id];
                _preview.Invalidate();
                return;
            }

            _previewCts?.Cancel();
            _previewCts = new CancellationTokenSource();
            var ct = _previewCts.Token;

            _preview.Image = null;
            _previewCaption = "Rendering preview…";
            _preview.Invalidate();

            Task.Run(() =>
            {
                Bitmap bitmap = null;
                string caption;
                try
                {
                    int zoom, drawn;
                    bool partial;
                    bitmap = OfflineMapPack.RenderPreview(region, PreviewTilesPerSide, ct, out zoom, out drawn, out partial);
                    if (bitmap == null)
                        caption = OfflineMapPack.FindProvider(region.Provider) == null
                            ? "Preview unavailable: map source not installed"
                            : "Preview unavailable";
                    else if (drawn == 0)
                        caption = "No tiles on disk for this area yet";
                    else if (partial)
                        caption = "Centre of the area at zoom " + zoom + "  ·  hatching = tile not downloaded";
                    else
                        caption = "Zoom " + zoom + "  ·  hatching = tile not downloaded";
                }
                catch (OperationCanceledException)
                {
                    bitmap?.Dispose();
                    return;
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                    bitmap?.Dispose();
                    bitmap = null;
                    caption = "Preview unavailable";
                }

                SafeInvoke(() =>
                {
                    if (ct.IsCancellationRequested)
                    {
                        bitmap?.Dispose();
                        return;
                    }

                    if (bitmap != null)
                    {
                        Image old;
                        if (_previews.TryGetValue(region.Id, out old) && old != _preview.Image)
                            old.Dispose();
                        _previews[region.Id] = bitmap;
                    }
                    _previewCaptions[region.Id] = caption;

                    if (_detailRegionId == region.Id)
                    {
                        _preview.Image = bitmap;
                        _previewCaption = caption;
                        _preview.Invalidate();
                    }
                });
            }, ct);
        }

        private void ClearPreviews()
        {
            _previewCts?.Cancel();
            if (_preview != null)
                _preview.Image = null;
            foreach (var image in _previews.Values)
                image.Dispose();
            _previews.Clear();
            _previewCaptions.Clear();
        }

        private void SafeInvoke(Action action)
        {
            if (IsDisposed || !IsHandleCreated)
                return;
            try
            {
                BeginInvoke(action);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        #endregion

        #region Actions

        private void Download()
        {
            var map = _planner.MainMap;
            var area = map.SelectedArea;
            bool fromSelection = !area.IsEmpty;
            if (!fromSelection)
                area = map.ViewArea;

            if (area.IsEmpty)
            {
                CustomMessageBox.Show("The map has no area to download yet.", "Offline Maps");
                return;
            }

            var mid = area.LocationMiddle;
            string defaultName = string.Format(CultureInfo.InvariantCulture, "Area {0:F4}, {1:F4}", mid.Lat, mid.Lng);
            string name = defaultName;
            string prompt = fromSelection
                ? "Name for the area selected on the map:"
                : "Name for the current map view (hold ALT and drag on the map to pick an exact area instead):";
            if (InputBox.Show("Download map area", prompt, ref name) != DialogResult.OK)
                return;
            if (string.IsNullOrWhiteSpace(name))
                name = defaultName;

            // the registry change this records refreshes the list
            _planner.PrefetchAreaForOfflineMaps(area, name.Trim());
        }

        private void ShowOnMap()
        {
            var regions = SelectedRegions();
            if (regions.Count != 1)
                return;
            _planner.ShowAreaOnMap(regions[0].Bounds);
        }

        private void Rename()
        {
            var regions = SelectedRegions();
            if (regions.Count != 1)
                return;

            var region = regions[0];
            string name = region.Name;
            if (InputBox.Show("Rename area", "Name:", ref name) != DialogResult.OK || string.IsNullOrWhiteSpace(name))
                return;

            region.Name = name.Trim();
            OfflineMapRegistry.Update(region);
        }

        private void Export()
        {
            var regions = SelectedRegions();
            if (regions.Count == 0)
            {
                CustomMessageBox.Show("Select one or more areas to export.", "Offline Maps");
                return;
            }

            string suggested = regions.Count == 1 ? regions[0].Name : regions[0].Name + " +" + (regions.Count - 1) + " more";
            using (var dlg = new SaveFileDialog
            {
                Title = "Export offline map pack",
                Filter = OfflineMapPack.FileFilter,
                DefaultExt = OfflineMapPack.FileExtension,
                AddExtension = true,
                OverwritePrompt = true,
                FileName = SafeFileName(suggested),
                InitialDirectory = Settings.Instance.GetString(LastDirSetting, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;
                Settings.Instance[LastDirSetting] = Path.GetDirectoryName(dlg.FileName);

                bool completed = false;
                RunWithProgress("Exporting offline maps", sender =>
                {
                    completed = OfflineMapPack.Export(regions, dlg.FileName, sender.UpdateProgressAndStatus,
                        () => sender.doWorkArgs.CancelRequested);
                    if (!completed)
                        sender.doWorkArgs.CancelAcknowledged = true;
                });

                if (!completed)
                    return;

                long size = 0;
                try
                {
                    size = new FileInfo(dlg.FileName).Length;
                }
                catch
                {
                }

                CustomMessageBox.Show(string.Format("Exported {0} area(s) to\n{1}\n({2})", regions.Count, dlg.FileName,
                    OfflineMapPack.FormatSize(size)), "Offline Maps");
            }
        }

        private void Import()
        {
            using (var dlg = new OpenFileDialog
            {
                Title = "Import offline map pack",
                Filter = OfflineMapPack.FileFilter,
                DefaultExt = OfflineMapPack.FileExtension,
                InitialDirectory = Settings.Instance.GetString(LastDirSetting, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;
                Settings.Instance[LastDirSetting] = Path.GetDirectoryName(dlg.FileName);

                OfflineMapPack.PackInfo info;
                try
                {
                    info = OfflineMapPack.Inspect(dlg.FileName);
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                    CustomMessageBox.Show("This file is not a readable offline map pack.\n" + ex.Message, "Offline Maps");
                    return;
                }

                if (info.TileEntries == 0)
                {
                    CustomMessageBox.Show("No map tiles were found in this file.", "Offline Maps");
                    return;
                }

                var summary = new StringBuilder();
                summary.AppendLine(string.Format("{0:N0} tiles in {1}", info.TileEntries, Path.GetFileName(dlg.FileName)));
                var regions = info.Manifest?.Regions;
                if (regions != null && regions.Count > 0)
                {
                    summary.AppendLine();
                    foreach (var region in regions.Take(12))
                        summary.AppendLine(string.Format("    {0}  ({1}, zoom {2}–{3})", region.Name, region.Provider, region.MinZoom, region.MaxZoom));
                    if (regions.Count > 12)
                        summary.AppendLine("    …and " + (regions.Count - 12) + " more");
                }
                else
                {
                    summary.AppendLine("Map sources: " + string.Join(", ", info.Providers));
                }

                var unknown = info.Providers.Where(p => OfflineMapPack.FindProvider(p) == null).ToList();
                if (unknown.Count > 0)
                {
                    summary.AppendLine();
                    summary.AppendLine("Warning: this computer has no map source named " + string.Join(", ", unknown) +
                                       ". Those tiles will be copied but cannot be displayed.");
                }

                summary.AppendLine();
                summary.Append("Import into the map cache? Tiles already present are kept as they are.");

                if (CustomMessageBox.Show(summary.ToString(), "Import offline maps", MessageBoxButtons.YesNo, MessageBoxIcon.Question) !=
                    (int)DialogResult.Yes)
                    return;

                OfflineMapPack.ImportResult result = null;
                RunWithProgress("Importing offline maps", sender =>
                {
                    result = OfflineMapPack.Import(dlg.FileName, false, sender.UpdateProgressAndStatus,
                        () => sender.doWorkArgs.CancelRequested);
                    if (result.Cancelled)
                        sender.doWorkArgs.CancelAcknowledged = true;
                });

                if (result == null)
                    return;

                // a cancelled import has only part of each area, so do not list it as available
                int added = result.Cancelled ? 0 : OfflineMapRegistry.AddRange(result.Regions);

                try
                {
                    _planner.MainMap.ReloadMap();
                }
                catch
                {
                }

                RefreshList();
                CustomMessageBox.Show(string.Format("{0}{1:N0} tiles copied, {2:N0} were already present.\n{3} area(s) added to the list.",
                    result.Cancelled ? "Import cancelled. " : "", result.Added, result.Skipped, added), "Offline Maps");
            }
        }

        private void DeleteSelected()
        {
            var regions = SelectedRegions();
            if (regions.Count == 0)
                return;

            string what = regions.Count == 1 ? "'" + regions[0].Name + "'" : regions.Count + " areas";
            if (CustomMessageBox.Show("Remove " + what + " from the offline maps list?", "Offline Maps",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != (int)DialogResult.Yes)
                return;

            long tiles = 0, bytes = 0;
            foreach (var region in regions)
            {
                OfflineMapPack.RegionStats stats;
                if (_stats.TryGetValue(region.Id, out stats))
                {
                    tiles += stats.TilesOnDisk;
                    bytes += stats.BytesOnDisk;
                }
            }

            bool deleteTiles = false;
            if (tiles > 0)
            {
                deleteTiles = CustomMessageBox.Show(
                    string.Format("Also delete the {0:N0} cached tiles ({1}) from disk?\n\n" +
                                  "No keeps the tiles: the map still uses them, they are just no longer listed here.\n" +
                                  "Yes frees the space; tiles shared with an overlapping area are deleted too.",
                        tiles, OfflineMapPack.FormatSize(bytes)),
                    "Offline Maps", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == (int)DialogResult.Yes;
            }

            if (deleteTiles)
            {
                RunWithProgress("Deleting cached tiles", sender =>
                {
                    long done = 0;
                    foreach (var region in regions)
                    {
                        if (sender.doWorkArgs.CancelRequested)
                        {
                            sender.doWorkArgs.CancelAcknowledged = true;
                            break;
                        }

                        long before = done;
                        done += OfflineMapPack.DeleteTiles(region,
                            n => sender.UpdateProgressAndStatus(-1, string.Format("Deleted {0:N0} tiles", before + n)),
                            () => sender.doWorkArgs.CancelRequested);
                    }
                });
            }

            foreach (var region in regions)
                OfflineMapRegistry.Remove(region.Id);

            if (deleteTiles)
            {
                try
                {
                    _planner.MainMap.ReloadMap();
                }
                catch
                {
                }
            }

            RefreshList();
        }

        private void OpenFolder()
        {
            try
            {
                string root = OfflineMapPack.CacheRoot;
                Directory.CreateDirectory(root);
                Process.Start("explorer.exe", "\"" + root + "\"");
            }
            catch (Exception ex)
            {
                log.Error(ex);
                CustomMessageBox.Show("Could not open the folder.\n" + ex.Message, "Offline Maps");
            }
        }

        private void RunWithProgress(string title, Utilities.DoWorkEventHandler work)
        {
            var progress = new ProgressReporterDialogue
            {
                StartPosition = FormStartPosition.CenterParent,
                Text = title
            };
            progress.DoWork += work;
            progress.UpdateProgressAndStatus(-1, title);
            ThemeManager.ApplyThemeTo(progress);
            progress.RunBackgroundOperationAsync();
            progress.Dispose();
        }

        private static string SafeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "offline-maps";
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name.Trim();
        }

        #endregion
    }
}
