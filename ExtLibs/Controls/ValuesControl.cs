using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MissionPlanner.Controls
{
   public partial class ValuesControl : MyUserControl, IDynamicParameterControl
   {
      #region Properties

       public event EventValueChanged ValueChanged;

      public string LabelText { get { return myLabel1.Text; } set { myLabel1.Text = value; } }
      public string DescriptionText
      {
          get { return label1.Text; }
          set
          {
              //label1.MaximumSize = new Size(this.Width - 30, 0);
              label1.Text = value;
              //this.Height += label1.Height;
          }
      }
      // Opt-in: measure the same WinForms label that renders the unchanged text.
      public void SetMeasuredDescription(string text)
      {
          label1.Text = text;
          var available = Math.Max(1, label1.ClientSize.Width);
          var needed = label1.GetPreferredSize(new Size(available, 0)).Height;
          // The label is top/bottom anchored and the dropdown is bottom anchored.
          // Grow the row before the host adds it so both bounds remain valid.
          Height += Math.Max(0, needed - label1.Height);
      }

      public ComboBox ComboBoxControl { get { return comboBox1; } set { comboBox1 = value; } }

      #region Interface Properties

      public string Value
      {
          get { return comboBox1.SelectedValue.ToString(); }
          set
          {
              comboBox1.SelectedValue = value;
              if (ValueChanged != null)
                  ValueChanged(this,Name, Value);
          }
      }

      #endregion

      #endregion

      #region Constructor

      public ValuesControl()
      {
         InitializeComponent();
      }

      #endregion

      protected override void OnPaint(PaintEventArgs e)
      {
          if (e.ClipRectangle.IsEmpty)
              return;
          // this is to improve first render time when not onscreen.
          if (!ComboBoxControl.Visible)
              ComboBoxControl.Visible = true;
          base.OnPaint(e);
      }

      private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
      {
          if (ValueChanged != null)
              ValueChanged(this,Name, Value);
      }

      public void DeAttachEvents()
      {
          Delegate[] subscribers = ValueChanged.GetInvocationList();
          for (int i = 0; i < subscribers.Length; i++)
          {
              ValueChanged -= subscribers[i] as EventValueChanged;
          }
      }
   }
}
