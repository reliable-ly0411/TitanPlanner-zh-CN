using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MissionPlanner.Controls.BackstageView
{
    public class BackstageViewButton : Control
    {
        private bool _isSelected;
        private bool _hasChildren;
        private bool _isExpanded;

        internal Color ContentPageColor = Color.Gray;
        internal Color PencilBorderColor = Color.White;
        internal Color SelectedTextColor = Color.White;
        internal Color UnSelectedTextColor = Color.Gray;
        internal Color HighlightColor1 = SystemColors.Highlight;
        internal Color HighlightColor2 = SystemColors.MenuHighlight;
        private bool _isMouseOver;
        internal bool FitToViewport { get; set; }

        public bool HasChildren
        {
            get { return _hasChildren; }
            set
            {
                if (_hasChildren != value)
                {
                    _hasChildren = value;
                    this.Invalidate();
                }
            }
        }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    this.Invalidate();
                }
            }
        }

        //internal Color HighlightColor1 = Color.FromArgb(0x94, 0xc1, 0x1f);
        //internal Color HighlightColor2 = Color.FromArgb(0xcd, 0xe2, 0x96);

        public BackstageViewButton()
        {
            this.SuspendLayout();

            SetStyle(ControlStyles.ResizeRedraw, true);

            this.Width = 150;
            this.Height = 30;
            
            this.ResumeLayout(false);
        }

        /// <summary>
        /// Whether this button should show the selected style
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;

                    this.Invalidate();
                }
            }
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            base.OnPaintBackground(pevent);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
        }

        private void DrawCaption(Graphics graphics, FontFamily family, Color color, int paintWidth)
        {
            using (var font = new Font(family, 10 * (100 / graphics.DpiX), FontStyle.Bold))
            using (var brush = new SolidBrush(color))
            {
                if (paintWidth == Width)
                {
                    graphics.DrawString(Text, font, brush, 5, 6);
                    return;
                }

                int available = paintWidth - 5 - (HasChildren ? 22 : IsSelected ? 9 : 1);
                if (available <= 0 || Height <= 6)
                    return;

                using (var format = new StringFormat(StringFormatFlags.NoWrap))
                {
                    format.Trimming = StringTrimming.EllipsisCharacter;
                    graphics.DrawString(Text, font, brush, new RectangleF(5, 6, available, Height - 6), format);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            if (this.Parent != null)
            {
                ((BackStageViewMenuPanel)this.Parent).PaintBackground(pevent);
            }

           Graphics g = pevent.Graphics;
           // A vertical scrollbar occupies non-client width. Keep the row and
           // its handlers intact, but paint inside the visible viewport.
           int paintWidth = FitToViewport && Parent != null
               ? Math.Max(0, Math.Min(Width, Parent.ClientSize.Width - Left))
               : Width;
           if (paintWidth == 0 || Height == 0)
               return;

            // Now the little 'arrow' thingy if we are selected and the selected bg grad
           if (_isSelected)
           {
               //var rect1 = new Rectangle(0, 0, Width / 1, Height);
               var rect2 = new Rectangle(0, 0, paintWidth, Height);

              // g.FillRectangle(new LinearGradientBrush(rect1, HighlightColor1, HighlightColor2, LinearGradientMode.Horizontal), rect1);
               g.FillRectangle(new LinearGradientBrush(rect2, HighlightColor2, HighlightColor1, LinearGradientMode.Horizontal), rect2);

               var butPen = new Pen(HighlightColor1);
               g.DrawLine(butPen, 0, 0, paintWidth, 0);
               g.DrawLine(butPen, 0, Height - 1, paintWidth, Height - 1);

               var arrowBrush = new SolidBrush(this.ContentPageColor);

               var midheight = Height / 2;
               var arSize = 8;

               var arrowPoints = new[]
                                     {
                                         new Point(paintWidth, midheight + arSize),
                                         new Point(paintWidth - arSize, midheight),
                                         new Point(paintWidth, midheight - arSize)
                                     };

               DrawCaption(g, FontFamily.GenericSansSerif, SelectedTextColor, paintWidth);

               var pencilBrush = new Pen(this.PencilBorderColor);

               g.DrawLine(pencilBrush, paintWidth - 1, 0, paintWidth - 1, Height - 1);
               g.FillPolygon(arrowBrush, arrowPoints); 

               g.DrawPolygon(pencilBrush, arrowPoints);

               
           }
           else
           {
               if (_isMouseOver)
               {
                   var brush = new SolidBrush(Color.FromArgb(10, 0xA0, 0xA0, 0xA0));

                   g.FillRectangle(brush, this.ClientRectangle);

                   var butPen = new Pen(PencilBorderColor);
                   g.DrawLine(butPen, 0, 0, paintWidth, 0);
                   g.DrawLine(butPen, 0, Height - 1, paintWidth, Height - 1);
               }

                DrawCaption(g, Font.FontFamily, UnSelectedTextColor, paintWidth);
           }

           if (_hasChildren)
           {
               var arrowColor = _isSelected ? SelectedTextColor : UnSelectedTextColor;
               var arrowBrush = new SolidBrush(arrowColor);
               var midHeight = Height / 2;
               var arrowSize = 5;
               var rightMargin = 16;

               Point[] arrowPoints;
               if (_isExpanded)
               {
                   arrowPoints = new[]
                   {
                       new Point(paintWidth - rightMargin - arrowSize, midHeight - arrowSize / 2),
                       new Point(paintWidth - rightMargin + arrowSize, midHeight - arrowSize / 2),
                       new Point(paintWidth - rightMargin, midHeight + arrowSize / 2)
                   };
               }
               else
               {
                   arrowPoints = new[]
                   {
                       new Point(paintWidth - rightMargin - arrowSize / 2, midHeight - arrowSize),
                       new Point(paintWidth - rightMargin + arrowSize / 2, midHeight),
                       new Point(paintWidth - rightMargin - arrowSize / 2, midHeight + arrowSize)
                   };
               }

               g.SmoothingMode = SmoothingMode.AntiAlias;
               g.FillPolygon(arrowBrush, arrowPoints);
           }
        }


        protected override void OnMouseEnter(EventArgs e)
        {
            _isMouseOver = true;
            base.OnMouseEnter(e);
            this.Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isMouseOver = false;
            base.OnMouseLeave(e);
            this.Invalidate();

        }

        /*
        // This IS necessary for transparency - windows only..... remove it
        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_TRANSPARENT = 0x20;
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TRANSPARENT;
                return cp;
            }
        }
         */
    }
}