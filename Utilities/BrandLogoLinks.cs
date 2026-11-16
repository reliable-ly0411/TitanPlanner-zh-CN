#if !LIB
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MissionPlanner.Utilities
{
    /// <summary>One layout item with two independently accessible brand links.</summary>
    public static class BrandLogoLinks
    {
        private static readonly object LogoMarker = new object();

        public const string ArduPilotUrl = "https://ardupilot.org";
        public const string GitHubUrl = "https://github.com/reliable-ly0411";

        public static ToolStripControlHost Configure(ToolStrip menu, ToolStripButton original,
            Action<string> open, params ToolStripItem[] connectionItems)
        {
            BrandLogoControl links;
            using (var stream = typeof(BrandLogoLinks).Assembly.GetManifestResourceStream(
                "MissionPlanner.Resources.LocalizedBrandLogo.png"))
            using (var source = Image.FromStream(stream))
                links = new BrandLogoControl(new Bitmap(source), open);

            var host = new ToolStripControlHost(links)
            {
                Name = "MenuLocalizedBrand",
                AutoSize = false,
                Alignment = ToolStripItemAlignment.Right,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                Overflow = ToolStripItemOverflow.AsNeeded,
                AccessibleName = "ArduPilot / GitHub: reliable-ly0411",
                AccessibleRole = AccessibleRole.Grouping,
                Tag = LogoMarker
            };

            menu.SuspendLayout();
            try
            {
                // Keep the public designer button as a hidden compatibility entry. Its
                // replacement is a single item: overflow can never split the picture.
                original.Available = false;
                original.Tag = LogoMarker;
                original.Click += (sender, args) => open(ArduPilotUrl);
                int index = 0;
                while (index < menu.Items.Count && menu.Items[index].Alignment != ToolStripItemAlignment.Right)
                    index++;
                menu.Items.Insert(index, host);

                // MenuStrip defaults to CanOverflow=false, which lets left-hand hosted
                // controls overlap right-hand items when their combined widths exceed
                // the window. Keep connection controls on the bar; other items can use
                // the native overflow menu. The logo stays at the far right as a unit.
                menu.CanOverflow = true;
                foreach (var item in connectionItems)
                    item.Overflow = ToolStripItemOverflow.Never;

                LayoutEventHandler updateSize = (sender, args) =>
                {
                    // Use the live toolbar height after layout/DPI changes, never the
                    // designer button's stretched or stale Height. Render the original
                    // full-resolution PNG at paint time rather than scaling old halves.
                    var size = links.SizeForToolbar(menu.DisplayRectangle.Height);
                    if (host.Size != size) host.Size = size;
                    if (links.Size != size) links.Size = size;
                    int gap = Math.Max(1, (int)Math.Round(size.Height * 8.0 / 58));
                    var margin = new Padding(gap, 0, gap, 0);
                    if (host.Margin != margin) host.Margin = margin;
                };
                menu.Layout += updateSize;
                host.Disposed += (sender, args) => menu.Layout -= updateSize;
                updateSize(menu, null);
                PreserveSurface(host);
            }
            finally
            {
                menu.ResumeLayout(true);
            }
            return host;
        }

        /// <summary>Keep navigation selection from giving the logo an opaque background.</summary>
        public static bool PreserveSurface(ToolStripItem item)
        {
            if (!ReferenceEquals(item.Tag, LogoMarker)) return false;
            item.BackColor = Color.Transparent;
            item.BackgroundImage = null;
            var host = item as ToolStripControlHost;
            if (host != null)
            {
                host.Control.BackColor = Color.Transparent;
                host.Control.BackgroundImage = null;
                host.Control.Invalidate(true);
            }
            return true;
        }

        private sealed class BrandLogoControl : UserControl
        {
            private readonly Bitmap source;
            private readonly Bitmap inkSupport;
            private readonly Button left;
            private readonly Button right;
            private readonly ToolTip toolTip = new ToolTip();

            public BrandLogoControl(Bitmap source, Action<string> open)
            {
                this.source = source;
                inkSupport = CreateInkSupport(source);
                AutoScaleMode = AutoScaleMode.None;
                BackColor = Color.Transparent;
                Margin = Padding.Empty;
                Padding = Padding.Empty;
                TabStop = false;
                AccessibleName = "ArduPilot / GitHub: reliable-ly0411";
                AccessibleRole = AccessibleRole.Grouping;
                SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer, true);
                left = CreateLink("MenuArduPilotLink", "ArduPilot", ArduPilotUrl, 0, open);
                right = CreateLink("MenuLocalizedGitHub", "GitHub: reliable-ly0411", GitHubUrl, 1, open);
                Controls.Add(left);
                Controls.Add(right);
            }

            private Button CreateLink(string name, string text, string url, int tabIndex, Action<string> open)
            {
                var button = new BrandLinkButton
                {
                    Name = name,
                    Text = text,
                    AccessibleName = text,
                    AccessibleDescription = url,
                    AccessibleRole = AccessibleRole.Link,
                    TabIndex = tabIndex,
                    TabStop = true,
                    BackColor = Color.Transparent,
                    Cursor = Cursors.Hand,
                    UseVisualStyleBackColor = false,
                    FlatStyle = FlatStyle.Flat,
                    Margin = Padding.Empty,
                    Padding = Padding.Empty
                };
                button.FlatAppearance.BorderSize = 0;
                button.Click += (sender, args) => open(url);
                // ToolStripControlHost forwards the UserControl's leave event. Relay
                // leaves from the child hit regions too, for the menu's auto-hide mode.
                button.MouseLeave += (sender, args) => OnMouseLeave(args);
                toolTip.SetToolTip(button, text + "\n" + url);
                return button;
            }

            public Size SizeForToolbar(int height)
            {
                height = Math.Max(1, height);
                int imageHeight = Math.Max(1, height * 2 / 3);
                int width = Math.Max(2, (int)Math.Round(imageHeight * (double)source.Width / source.Height));
                return new Size(width, height);
            }

            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                if (left == null) return;
                int midpoint = Width / 2;
                left.Bounds = new Rectangle(0, 0, midpoint, Height);
                right.Bounds = new Rectangle(midpoint, 0, Width - midpoint, Height);
                Invalidate(true);
            }

            protected override void OnPaintBackground(PaintEventArgs e)
            {
                PaintToolbarBackground(e.Graphics);
            }

            private void PaintToolbarBackground(Graphics graphics)
            {
                // Painting a transparent hosted control through ToolStrip.OnPaint also
                // repaints its items. Copy only the toolbar's background, never its text.
                var parent = Parent;
                var backdrop = parent;
                while (backdrop != null && backdrop.BackColor.A != 255) backdrop = backdrop.Parent;
                var color = backdrop == null ? SystemColors.Control : backdrop.BackColor;
                using (var brush = new SolidBrush(color))
                    graphics.FillRectangle(brush, ClientRectangle);
                var background = parent == null ? null : parent.BackgroundImage;
                if (background == null) return;
                var origin = Location;
                if (parent.BackgroundImageLayout == ImageLayout.Tile)
                {
                    using (var brush = new TextureBrush(background, WrapMode.Tile))
                    {
                        brush.TranslateTransform(-origin.X, -origin.Y);
                        graphics.FillRectangle(brush, ClientRectangle);
                    }
                    return;
                }
                var bounds = new Rectangle(Point.Empty, background.Size);
                if (parent.BackgroundImageLayout == ImageLayout.Stretch)
                    bounds.Size = parent.ClientSize;
                else if (parent.BackgroundImageLayout == ImageLayout.Zoom)
                {
                    double scale = Math.Min(parent.ClientSize.Width / (double)background.Width,
                        parent.ClientSize.Height / (double)background.Height);
                    bounds.Size = new Size((int)Math.Round(background.Width * scale),
                        (int)Math.Round(background.Height * scale));
                }
                if (parent.BackgroundImageLayout == ImageLayout.Center || parent.BackgroundImageLayout == ImageLayout.Zoom)
                    bounds.Location = new Point((parent.ClientSize.Width - bounds.Width) / 2,
                        (parent.ClientSize.Height - bounds.Height) / 2);
                bounds.Offset(-origin.X, -origin.Y);
                graphics.DrawImage(background, bounds);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                PaintLogo(e.Graphics);
            }

            private static Bitmap CreateInkSupport(Bitmap image)
            {
                var support = new Bitmap(image.Width, image.Height);
                for (int y = 0; y < image.Height; y++)
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = image.GetPixel(x, y);
                    // A display-only underlay follows the supplied neutral dark ink.
                    // Blue artwork and every original foreground pixel stay untouched.
                    if (pixel.R <= 110 && pixel.G <= 110 && pixel.B <= 110)
                        support.SetPixel(x, y, Color.FromArgb((pixel.A * 217 + 127) / 255, 235, 235, 235));
                }
                return support;
            }

            private bool NeedsInkSupport()
            {
                // BackColor alone can be light while the toolbar's tiled image is dark.
                // Sample the backdrop that is actually composited under the black text.
                using (var sample = new Bitmap(1, 1))
                using (var graphics = Graphics.FromImage(sample))
                {
                    graphics.TranslateTransform(-Width * 3 / 4, -Height / 2);
                    PaintToolbarBackground(graphics);
                    var color = sample.GetPixel(0, 0);
                    return (Luminance(color) + 0.05) / (Luminance(Color.FromArgb(42, 42, 42)) + 0.05) < 4.5;
                }
            }

            private static double Luminance(Color color)
            {
                return 0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);
            }

            private static double Linear(byte value)
            {
                double channel = value / 255.0;
                return channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
            }

            private void PaintLogo(Graphics graphics)
            {
                int imageHeight = Math.Max(1, (int)Math.Round(Width * (double)source.Height / source.Width));
                var imageBounds = new Rectangle(0, (Height - imageHeight) / 2, Width, imageHeight);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                if (NeedsInkSupport())
                {
                    int radius = Math.Max(1, (int)Math.Round(Height / 58.0));
                    for (int y = -radius; y <= radius; y++)
                    for (int x = -radius; x <= radius; x++)
                    {
                        if ((x == 0 && y == 0) || x * x + y * y > radius * radius + radius) continue;
                        var offset = imageBounds;
                        offset.Offset(x, y);
                        graphics.DrawImage(inkSupport, offset);
                    }
                }
                // The exact original RGBA image is always the final foreground layer.
                graphics.DrawImage(source, imageBounds);
            }

            public void PaintLinkSurface(Graphics graphics, Point location)
            {
                var state = graphics.Save();
                graphics.TranslateTransform(-location.X, -location.Y);
                PaintToolbarBackground(graphics);
                PaintLogo(graphics);
                graphics.Restore(state);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    toolTip.Dispose();
                    inkSupport.Dispose();
                    source.Dispose();
                }
                base.Dispose(disposing);
            }
        }

        private sealed class BrandLinkButton : Button
        {
            public BrandLinkButton()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.Opaque | ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            }

            protected override void OnPaintBackground(PaintEventArgs e)
            {
                // The entire surface is painted in OnPaint. ButtonBase is Opaque, so
                // normal WM_PAINT skips this method even when WM_PRINT calls it.
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                // Replace every pixel, including transparent PNG pixels, with the
                // toolbar backdrop plus artwork. Child HWNDs clip the parent's paint;
                // relying on the parent or OnPaintBackground leaves old page pixels.
                ((BrandLogoControl)Parent).PaintLinkSurface(e.Graphics, Location);
                if (Focused || (ShowFocusCues && IsDefault))
                {
                    var bounds = ClientRectangle;
                    bounds.Inflate(-2, -2);
                    ControlPaint.DrawFocusRectangle(e.Graphics, bounds);
                }
            }

            protected override bool ProcessDialogKey(Keys keyData)
            {
                if (keyData == Keys.Enter)
                {
                    PerformClick();
                    return true;
                }
                if (keyData == Keys.Left || keyData == Keys.Right)
                    return Parent.SelectNextControl(this, keyData == Keys.Right, true, false, true);
                if (keyData == Keys.Tab && TabIndex == 0)
                    return Parent.SelectNextControl(this, true, true, false, false);
                if (keyData == (Keys.Shift | Keys.Tab) && TabIndex == 1)
                    return Parent.SelectNextControl(this, false, true, false, false);
                return base.ProcessDialogKey(keyData);
            }
        }
    }
}
#endif
