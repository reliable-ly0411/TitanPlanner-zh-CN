using MissionPlanner.Utilities;
using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace MissionPlanner.Controls.PreFlight
{
    public class CheckListInput : UserControl
    {
        public event EventHandler ReloadList;

        CheckListControl _parent;
        private ToolTip conditionTooltip;
        private ToolTip colorTooltip;
        private ToolTip sourceTooltip;
        private ToolTip stockDescriptionTooltip;

        public CheckListInput(CheckListControl parent)
        {
            _parent = parent;

            InitializeComponent();
        }

        public CheckListInput(CheckListControl parent, CheckListItem item)
        {
            _parent = parent;

            InitializeComponent();

            CheckListItem.defaultsrc = MainV2.comPort.MAV.cs;
            item.SetField(item.Name);

            SetupConditionPresentation();
            CMB_condition.DataSource = Enum.GetNames(typeof(CheckListItem.Conditional));

            SetupSourcePresentation();
            CMB_Source.DataSource = item.GetOptions();

            SetupColorPresentation();
            CMB_colour1.DataSource = Enum.GetNames(typeof(KnownColor));
            CMB_colour2.DataSource = Enum.GetNames(typeof(KnownColor));

            CheckListItem = item;

            updateDisplay();
        }

        public void updateDisplay()
        {
            CMB_condition.SelectedItem = CheckListItem.ConditionType.ToString();
            CMB_Source.SelectedItem = CheckListItem.Name;
            NUM_trigger.Value = (decimal)CheckListItem.TriggerValue;
            TXT_text.Text = CheckListItem.Text;
            TXT_desc.Text = CheckListItem.Description;
            UpdateStockDescriptionHint();
            CMB_colour1.SelectedItem = CheckListItem.TrueColor;
            CMB_colour2.SelectedItem = CheckListItem.FalseColor;
        }

        private void SetupConditionPresentation()
        {
            if (!string.Equals(CultureInfo.CurrentUICulture.Name, "zh-CN", StringComparison.Ordinal))
                return;

            conditionTooltip = new ToolTip();
            CMB_condition.Format += (sender, args) =>
            {
                var token = args.ListItem as string;
                switch (token)
                {
                    case "NONE": args.Value = token + " · 不比较"; break;
                    case "LT": args.Value = token + " · 小于"; break;
                    case "LTEQ": args.Value = token + " · 小于或等于"; break;
                    case "EQ": args.Value = token + " · 等于"; break;
                    case "GT": args.Value = token + " · 大于"; break;
                    case "GTEQ": args.Value = token + " · 大于或等于"; break;
                    case "NEQ": args.Value = token + " · 不等于"; break;
                }
            };
            CMB_condition.DropDown += (sender, args) => UpdateConditionDropDownWidth();
            CMB_condition.MouseEnter += (sender, args) => UpdateConditionTooltip();
            Disposed += (sender, args) => conditionTooltip.Dispose();
        }

        private void UpdateConditionDropDownWidth()
        {
            var combo = CMB_condition;
            int width = combo.Width;
            foreach (var item in combo.Items)
                width = Math.Max(width, TextRenderer.MeasureText(combo.GetItemText(item), combo.Font).Width
                    + SystemInformation.VerticalScrollBarWidth);
            combo.DropDownWidth = Math.Max(combo.Width, Math.Min(width, Screen.FromControl(combo).WorkingArea.Width));
        }

        private void UpdateConditionTooltip()
        {
            if (conditionTooltip != null)
                conditionTooltip.SetToolTip(CMB_condition, CMB_condition.GetItemText(CMB_condition.SelectedItem));
        }

        private void SetupSourcePresentation()
        {
            if (!string.Equals(CultureInfo.CurrentUICulture.Name, "zh-CN", StringComparison.Ordinal))
                return;

            sourceTooltip = new ToolTip();
            CMB_Source.Format += (sender, args) =>
            {
                if (args.ListItem is string key)
                    args.Value = PresentSource(key);
            };
            CMB_Source.DropDown += (sender, args) => UpdateSourceDropDownWidth();
            CMB_Source.MouseEnter += (sender, args) => UpdateSourceTooltip();
            Disposed += (sender, args) => sourceTooltip.Dispose();
        }

        private static string PresentSource(string key)
        {
            switch (key)
            {
                case "gpsstatus":
                case "gpshdop":
                case "gpsvdop":
                case "satcount":
                case "roll":
                case "pitch":
                case "yaw":
                case "alt":
                case "airspeed":
                case "groundspeed":
                case "battery_voltage":
                case "wp_dist":
                case "armed":
                case "ahrs2_alt":
                case "ahrs2_lat":
                case "ahrs2_lng":
                case "ahrs2_pitch":
                case "ahrs2_roll":
                case "ahrs2_yaw":
                case "hygrotemp1":
                case "hygrohumi1":
                case "hygrotemp2":
                case "hygrohumi2":
                // RADIO / RADIO_STATUS map these counters directly in CurrentState.
                case "txbuffer":
                case "rxerrors":
                case "fixedp":
                    string caption = TelemetryFieldPresentation.FieldLabel(key);
                    if (!caption.StartsWith(key + " (", StringComparison.Ordinal))
                        return key;
                    // If the localized description is missing, leave the source field unchanged.
                    foreach (char ch in caption)
                        if (ch >= '\u4e00' && ch <= '\u9fff')
                            return caption;
                    break;
            }
            return key;
        }

        private void UpdateSourceDropDownWidth()
        {
            var combo = CMB_Source;
            int width = combo.Width;
            foreach (var item in combo.Items)
                width = Math.Max(width, TextRenderer.MeasureText(combo.GetItemText(item), combo.Font).Width
                    + SystemInformation.VerticalScrollBarWidth);
            combo.DropDownWidth = Math.Max(combo.Width, Math.Min(width, Screen.FromControl(combo).WorkingArea.Width));
        }

        private void UpdateSourceTooltip()
        {
            if (sourceTooltip != null)
                sourceTooltip.SetToolTip(CMB_Source, CMB_Source.GetItemText(CMB_Source.SelectedItem));
        }

        private void SetupColorPresentation()
        {
            if (!string.Equals(CultureInfo.CurrentUICulture.Name, "zh-CN", StringComparison.Ordinal))
                return;

            colorTooltip = new ToolTip();
            Disposed += (sender, args) => colorTooltip.Dispose();
            foreach (var combo in new[] { CMB_colour1, CMB_colour2 })
            {
                combo.DrawMode = DrawMode.OwnerDrawFixed;
                combo.Format += (sender, args) =>
                {
                    if (args.ListItem is string token)
                        args.Value = PresentColor(token);
                };
                combo.DrawItem += DrawColorItem;
                combo.DropDown += (sender, args) => UpdateColorDropDownWidth(combo);
                combo.MouseEnter += (sender, args) => UpdateColorTooltip(combo);
            }
        }

        private static string PresentColor(string token)
        {
            switch (token)
            {
                case "Black": return token + " · 黑色";
                case "Blue": return token + " · 蓝色";
                case "Cyan": return token + " · 青色";
                case "Gray": return token + " · 灰色";
                case "Green": return token + " · 绿色";
                case "LightGray": return token + " · 浅灰色";
                case "Lime": return token + " · 亮绿色";
                case "Magenta": return token + " · 品红";
                case "Orange": return token + " · 橙色";
                case "Purple": return token + " · 紫色";
                case "Red": return token + " · 红色";
                case "Silver": return token + " · 银色";
                case "Transparent": return token + " · 透明";
                case "White": return token + " · 白色";
                case "Yellow": return token + " · 黄色";
                default: return token;
            }
        }

        private static int ColorSwatchWidth(ComboBox combo)
        {
            return Math.Max(10, combo.Font.Height - 2);
        }

        private static void DrawColorItem(object sender, DrawItemEventArgs args)
        {
            if (args.Index < 0)
                return;

            var combo = (ComboBox)sender;
            var item = combo.Items[args.Index];
            var color = Color.FromName((string)item);
            int size = Math.Min(ColorSwatchWidth(combo), Math.Max(1, args.Bounds.Height - 4));
            var swatch = new Rectangle(args.Bounds.Left + 3, args.Bounds.Top + (args.Bounds.Height - size) / 2, size, size);
            args.DrawBackground();
            if (color.A != 0)
            {
                using (var brush = new SolidBrush(color))
                    args.Graphics.FillRectangle(brush, swatch);
            }
            using (var pen = new Pen(args.ForeColor))
            {
                args.Graphics.DrawRectangle(pen, swatch);
                if (color.A == 0)
                    args.Graphics.DrawLine(pen, swatch.Left, swatch.Bottom, swatch.Right, swatch.Top);
            }

            int textLeft = swatch.Right + 5;
            var body = Rectangle.FromLTRB(textLeft, args.Bounds.Top, args.Bounds.Right, args.Bounds.Bottom);
            if (body.Width > 0)
                TextRenderer.DrawText(args.Graphics, combo.GetItemText(item), combo.Font, body, args.ForeColor,
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix |
                    TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            args.DrawFocusRectangle();
        }

        private static void UpdateColorDropDownWidth(ComboBox combo)
        {
            int width = combo.Width;
            foreach (var item in combo.Items)
                width = Math.Max(width, TextRenderer.MeasureText(combo.GetItemText(item), combo.Font).Width
                    + ColorSwatchWidth(combo) + 8 + SystemInformation.VerticalScrollBarWidth);
            combo.DropDownWidth = Math.Max(combo.Width, Math.Min(width, Screen.FromControl(combo).WorkingArea.Width));
        }

        private void UpdateColorTooltip(ComboBox combo)
        {
            if (colorTooltip != null)
                colorTooltip.SetToolTip(combo, combo.GetItemText(combo.SelectedItem));
        }

        CheckListItem _checklistitem;

        public CheckListItem CheckListItem
        {
            get { return _checklistitem; }
            set { _checklistitem = value; }
        }

        private ComboBox CMB_condition;
        private NumericUpDown NUM_trigger;
        public TextBox TXT_text;
        private Controls.MyButton but_addchild;
        private Controls.MyButton but_remove;
        public ComboBox CMB_colour1;
        public ComboBox CMB_colour2;
        public TextBox TXT_desc;
        private ComboBox CMB_Source;

        private void InitializeComponent()
        {
            this.CMB_Source = new System.Windows.Forms.ComboBox();
            this.CMB_condition = new System.Windows.Forms.ComboBox();
            this.NUM_trigger = new System.Windows.Forms.NumericUpDown();
            this.TXT_text = new System.Windows.Forms.TextBox();
            this.but_addchild = new MissionPlanner.Controls.MyButton();
            this.but_remove = new MissionPlanner.Controls.MyButton();
            this.CMB_colour1 = new System.Windows.Forms.ComboBox();
            this.CMB_colour2 = new System.Windows.Forms.ComboBox();
            this.TXT_desc = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.NUM_trigger)).BeginInit();
            this.SuspendLayout();
            // 
            // CMB_Source
            // 
            this.CMB_Source.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CMB_Source.FormattingEnabled = true;
            this.CMB_Source.Location = new System.Drawing.Point(3, 2);
            this.CMB_Source.Name = "CMB_Source";
            this.CMB_Source.Size = new System.Drawing.Size(121, 21);
            this.CMB_Source.TabIndex = 0;
            this.CMB_Source.SelectedIndexChanged += new System.EventHandler(this.CMB_Source_SelectedIndexChanged);
            // 
            // CMB_condition
            // 
            this.CMB_condition.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CMB_condition.FormattingEnabled = true;
            this.CMB_condition.Location = new System.Drawing.Point(130, 2);
            this.CMB_condition.Name = "CMB_condition";
            this.CMB_condition.Size = new System.Drawing.Size(54, 21);
            this.CMB_condition.TabIndex = 1;
            this.CMB_condition.SelectedIndexChanged += new System.EventHandler(this.CMB_condition_SelectedIndexChanged);
            // 
            // NUM_trigger
            // 
            this.NUM_trigger.DecimalPlaces = 2;
            this.NUM_trigger.Location = new System.Drawing.Point(190, 3);
            this.NUM_trigger.Maximum = new decimal(new int[] {
            99999,
            0,
            0,
            0});
            this.NUM_trigger.Minimum = new decimal(new int[] {
            99999,
            0,
            0,
            -2147483648});
            this.NUM_trigger.Name = "NUM_trigger";
            this.NUM_trigger.Size = new System.Drawing.Size(65, 20);
            this.NUM_trigger.TabIndex = 2;
            this.NUM_trigger.ValueChanged += new System.EventHandler(this.NUM_warning_ValueChanged);
            // 
            // TXT_text
            // 
            this.TXT_text.Location = new System.Drawing.Point(261, 29);
            this.TXT_text.Name = "TXT_text";
            this.TXT_text.Size = new System.Drawing.Size(236, 20);
            this.TXT_text.TabIndex = 4;
            this.TXT_text.Text = UiText.Translate("{name} is {value}");
            this.TXT_text.TextChanged += new System.EventHandler(this.TXT_warningtext_TextChanged);
            // 
            // but_addchild
            // 
            this.but_addchild.Location = new System.Drawing.Point(631, 1);
            this.but_addchild.Name = "but_addchild";
            this.but_addchild.Size = new System.Drawing.Size(25, 20);
            this.but_addchild.TabIndex = 7;
            this.but_addchild.Text = "+";
            this.but_addchild.UseVisualStyleBackColor = true;
            this.but_addchild.Click += new System.EventHandler(this.but_addchild_Click);
            // 
            // but_remove
            // 
            this.but_remove.Location = new System.Drawing.Point(662, 1);
            this.but_remove.Name = "but_remove";
            this.but_remove.Size = new System.Drawing.Size(25, 20);
            this.but_remove.TabIndex = 8;
            this.but_remove.Text = "-";
            this.but_remove.UseVisualStyleBackColor = true;
            this.but_remove.Click += new System.EventHandler(this.but_remove_Click);
            // 
            // CMB_colour1
            // 
            this.CMB_colour1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CMB_colour1.DropDownWidth = 100;
            this.CMB_colour1.FormattingEnabled = true;
            this.CMB_colour1.Location = new System.Drawing.Point(503, 1);
            this.CMB_colour1.Name = "CMB_colour1";
            this.CMB_colour1.Size = new System.Drawing.Size(50, 21);
            this.CMB_colour1.TabIndex = 5;
            this.CMB_colour1.SelectedIndexChanged += new System.EventHandler(this.CMB_colour1_SelectedIndexChanged);
            // 
            // CMB_colour2
            // 
            this.CMB_colour2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CMB_colour2.DropDownWidth = 100;
            this.CMB_colour2.FormattingEnabled = true;
            this.CMB_colour2.Location = new System.Drawing.Point(559, 1);
            this.CMB_colour2.Name = "CMB_colour2";
            this.CMB_colour2.Size = new System.Drawing.Size(50, 21);
            this.CMB_colour2.TabIndex = 6;
            this.CMB_colour2.SelectedIndexChanged += new System.EventHandler(this.CMB_colour2_SelectedIndexChanged);
            // 
            // TXT_desc
            // 
            this.TXT_desc.Location = new System.Drawing.Point(261, 3);
            this.TXT_desc.Name = "TXT_desc";
            this.TXT_desc.Size = new System.Drawing.Size(236, 20);
            this.TXT_desc.TabIndex = 3;
            this.TXT_desc.Text = "GPS Hdop";
            this.TXT_desc.TextChanged += new System.EventHandler(this.TXT_desc_TextChanged);
            // 
            // CheckListInput
            // 
            this.Controls.Add(this.TXT_desc);
            this.Controls.Add(this.CMB_colour2);
            this.Controls.Add(this.CMB_colour1);
            this.Controls.Add(this.but_remove);
            this.Controls.Add(this.but_addchild);
            this.Controls.Add(this.TXT_text);
            this.Controls.Add(this.NUM_trigger);
            this.Controls.Add(this.CMB_condition);
            this.Controls.Add(this.CMB_Source);
            this.Name = "CheckListInput";
            this.Size = new System.Drawing.Size(695, 51);
            ((System.ComponentModel.ISupportInitialize)(this.NUM_trigger)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void CMB_Source_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateSourceTooltip();
            if (CheckListItem != null && CMB_Source.SelectedItem is string source)
                CheckListItem.SetField(source);
        }

        private void CMB_condition_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateConditionTooltip();
            CheckListItem.Conditional condition;
            if (CheckListItem != null && Enum.TryParse(CMB_condition.SelectedItem as string, out condition))
                CheckListItem.ConditionType = condition;
        }

        private void NUM_warning_ValueChanged(object sender, EventArgs e)
        {
            if (CheckListItem != null)
                CheckListItem.TriggerValue = (double)NUM_trigger.Value;
        }

        private void TXT_warningtext_TextChanged(object sender, EventArgs e)
        {
            if (CheckListItem != null)
                CheckListItem.Text = TXT_text.Text;
        }

        private void but_addchild_Click(object sender, EventArgs e)
        {
            CheckListItem.Child = new CheckListItem();

            if (ReloadList != null)
                ReloadList(this, null);
        }

        private void but_remove_Click(object sender, EventArgs e)
        {
            // Commented out the below, as a quicker remove method has been added into the ChecklistEditor.cs file
            //lock (_parent.CheckListItems)
            //{
            //    //_parent.CheckListItems.Remove(CheckListItem);
                        
            //    foreach (var item in _parent.CheckListItems)
            //    {
            //        removewarning(item, CheckListItem);
            //    }
            //}

            if (ReloadList != null)
                ReloadList(this, null);
        }

        void removewarning(CheckListItem lookin, CheckListItem removeme)
        {
            // depth first check children
            if (lookin.Child != null)
                removewarning(lookin.Child, removeme);

            if (lookin.Child == removeme)
            {
                if (lookin.Child.Child != null)
                {
                    lookin.Child = lookin.Child.Child;
                }
                else
                {
                    lookin.Child = null;
                }
                return;
            }
        }

        private void UpdateStockDescriptionHint()
        {
            string translated = _parent.GetChecklistDescription(CheckListItem);
            if (translated != CheckListItem.Description)
            {
                if (stockDescriptionTooltip == null)
                {
                    stockDescriptionTooltip = new ToolTip();
                    Disposed += (sender, args) => stockDescriptionTooltip.Dispose();
                }
                stockDescriptionTooltip.SetToolTip(TXT_desc, translated);
            }
            else
                stockDescriptionTooltip?.SetToolTip(TXT_desc, null);
        }

        private void TXT_desc_TextChanged(object sender, EventArgs e)
        {
            if (CheckListItem == null)
                return;
            if (!string.Equals(CheckListItem.Description ?? "", TXT_desc.Text, StringComparison.Ordinal))
                _parent.RemoveStockDescription(CheckListItem);
            CheckListItem.Description = TXT_desc.Text;
            UpdateStockDescriptionHint();
        }

        private void CMB_colour1_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateColorTooltip(CMB_colour1);
            if (CheckListItem != null)
                CheckListItem.TrueColor = CMB_colour1.SelectedValue.ToString();
        }

        private void CMB_colour2_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateColorTooltip(CMB_colour2);
            if (CheckListItem != null)
                CheckListItem.FalseColor = CMB_colour2.SelectedValue.ToString();
        }
    }
}