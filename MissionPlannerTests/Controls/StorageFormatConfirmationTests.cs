using System;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MissionPlanner.Controls;

namespace MissionPlannerTests.Controls
{
    [TestClass]
    public class StorageFormatConfirmationTests
    {
        [TestMethod]
        public void EveryNonYesResultSendsNothing()
        {
            foreach (DialogResult result in Enum.GetValues(typeof(DialogResult)))
            {
                if (result == DialogResult.Yes) continue;
                int sends = 0;
                StorageFormatConfirmation.Execute(() => result, () => sends++);
                Assert.AreEqual(0, sends, result.ToString());
            }
        }

        [TestMethod]
        public void ExplicitYesConfirmsOnceAndSendsOnce()
        {
            int confirmations = 0, sends = 0;
            StorageFormatConfirmation.Execute(() => { confirmations++; return DialogResult.Yes; }, () => sends++);
            Assert.AreEqual(1, confirmations);
            Assert.AreEqual(1, sends);
        }

        [TestMethod]
        public void ConfirmationFailureDoesNotSend()
        {
            int sends = 0;
            Assert.ThrowsException<InvalidOperationException>(() =>
                StorageFormatConfirmation.Execute(() => { throw new InvalidOperationException(); }, () => sends++));
            Assert.AreEqual(0, sends);
        }

        [TestMethod]
        public void SendFailureIsNotRetried()
        {
            int sends = 0;
            Assert.ThrowsException<InvalidOperationException>(() =>
                StorageFormatConfirmation.Execute(() => DialogResult.Yes, () => { sends++; throw new InvalidOperationException(); }));
            Assert.AreEqual(1, sends);
        }
    }
}
