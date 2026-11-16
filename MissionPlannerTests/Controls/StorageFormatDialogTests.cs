using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MissionPlanner.Controls;
using CustomMessageBox = MissionPlanner.MsgBox.CustomMessageBox;

namespace MissionPlannerTests.Controls
{
    [TestClass]
    public class StorageFormatDialogTests
    {
        private static void OnSta(Action action)
        {
            Exception failure = null;
            var thread = new Thread(() => { try { action(); } catch (Exception ex) { failure = ex; } });
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
            Assert.IsTrue(thread.Join(10000), "Dialog test did not complete.");
            if (failure != null) throw failure;
        }

        private static void WithDialog(Action<Form> interaction, int expectedSends)
        {
            OnSta(() =>
            {
                const string title = "Offline storage format confirmation test";
                int sends = 0;
                Exception failure = null;
                using (var timer = new System.Windows.Forms.Timer { Interval = 50 })
                {
                    timer.Tick += (sender, args) =>
                    {
                        var form = Application.OpenForms.Cast<Form>().FirstOrDefault(f => f.Text == title);
                        if (form == null) return;
                        timer.Stop();
                        try { interaction(form); }
                        catch (Exception ex) { failure = ex; form.Close(); }
                    };
                    timer.Start();
                    StorageFormatConfirmation.Execute(
                        () => CustomMessageBox.ShowDestructiveConfirmation("Offline test. No device is connected.", title),
                        () => sends++);
                }
                if (failure != null) throw failure;
                Assert.AreEqual(expectedSends, sends);
            });
        }

        [TestMethod]
        public void DefaultEnterAndCancelDoNotSend()
        {
            WithDialog(form =>
            {
                Assert.AreSame(form.CancelButton, form.AcceptButton);
                Assert.AreSame(form.CancelButton, form.ActiveControl);
                form.AcceptButton.PerformClick();
            }, 0);
            WithDialog(form => form.CancelButton.PerformClick(), 0);
        }

        [TestMethod]
        public void ClosingDoesNotSend()
        {
            WithDialog(form => form.Close(), 0);
        }

        [TestMethod]
        public void ExplicitYesSendsOnce()
        {
            WithDialog(form =>
            {
                var yes = form.Controls.Cast<Control>().OfType<IButtonControl>()
                    .Single(button => !ReferenceEquals(button, form.CancelButton));
                yes.PerformClick();
            }, 1);
        }

        [TestMethod]
        public void NestedOrdinaryYesThenClosingDoesNotSend()
        {
            WithDialog(outer =>
            {
                const string nestedTitle = "Nested ordinary confirmation test";
                using (var timer = new System.Windows.Forms.Timer { Interval = 50 })
                {
                    timer.Tick += (sender, args) =>
                    {
                        var nested = Application.OpenForms.Cast<Form>().FirstOrDefault(f => f.Text == nestedTitle);
                        if (nested == null) return;
                        timer.Stop();
                        nested.AcceptButton.PerformClick();
                    };
                    timer.Start();
                    Assert.AreEqual(DialogResult.Yes,
                        CustomMessageBox.Show("Ordinary dialog", nestedTitle, MessageBoxButtons.YesNo));
                }
                outer.Close();
            }, 0);
        }
    }
}
