using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MissionPlanner;
using MissionPlanner.Utilities;

namespace MissionPlannerTests.Utilities
{
    [TestClass]
    public class FirmwareSpeechAlertTests
    {
        private const string Raw = "Battery 1 disconnected";
        private const string Translated = "1 号电池已断开";
        private const BindingFlags InternalInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly Type AlertType = typeof(CurrentState).Assembly.GetType("MissionPlanner.FirmwareSpeechAlert", true);
        private static readonly MethodInfo DirectSpeech = AlertType.GetMethod("Speak", InternalInstance);
        private static readonly MethodInfo SetFirmwareHudText = typeof(CurrentState).GetMethod("SetFirmwareHudText", InternalInstance);

        private sealed class Speaker : ISpeech, ISpeechSubmission
        {
            internal readonly ConcurrentQueue<string> Submitted = new ConcurrentQueue<string>();
            internal bool Accept = true;
            public bool speechEnable { get; set; } = true;
            public bool IsReady => Accept;

            public void SpeakAsync(string text) => TrySpeakAsync(text);
            public void SpeakAsyncCancelAll() { }

            public bool TrySpeakAsync(string text)
            {
                if (!Accept) return false;
                Submitted.Enqueue(text);
                return true;
            }
        }

        private static object Publish(CurrentState state, Func<string> verifiedText)
        {
            var alert = Activator.CreateInstance(AlertType, InternalInstance, null, new object[] { verifiedText }, null);
            SetFirmwareHudText.Invoke(state, new object[] { Raw + "\t", verifiedText, alert });
            return alert;
        }

        private static bool SpeakDirectly(object alert, Speaker speaker) =>
            (bool)DirectSpeech.Invoke(alert, new object[] { speaker, Raw + "\t" });

        [TestMethod]
        public void DirectAndHudPathsSubmitTheSameEventOnce()
        {
            using (var state = new CurrentState())
            using (var start = new Barrier(2))
            {
                var speaker = new Speaker();
                var alert = Publish(state, () => Translated);
                var direct = Task.Factory.StartNew(() =>
                {
                    if (!start.SignalAndWait(TimeSpan.FromSeconds(5)))
                        throw new TimeoutException();
                    return SpeakDirectly(alert, speaker);
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                Assert.IsTrue(start.SignalAndWait(TimeSpan.FromSeconds(5)));
                Assert.AreEqual(Raw, state.SpeakHighMessageIfChanged(speaker, "previous"));
                Assert.IsTrue(direct.GetAwaiter().GetResult());
                CollectionAssert.AreEqual(new[] { Translated }, speaker.Submitted.ToArray());
            }
        }

        [TestMethod]
        public void RejectedSubmissionCanRetryAndExpiredProofUsesRawText()
        {
            using (var state = new CurrentState())
            {
                var speaker = new Speaker { Accept = false };
                string verifiedText = Translated;
                var alert = Publish(state, () => verifiedText);

                Assert.IsFalse(SpeakDirectly(alert, speaker));
                Assert.AreEqual("previous", state.SpeakHighMessageIfChanged(speaker, "previous"));
                Assert.AreEqual(0, speaker.Submitted.Count);

                verifiedText = null;
                speaker.Accept = true;
                Assert.AreEqual(Raw, state.SpeakHighMessageIfChanged(speaker, "previous"));
                Assert.IsTrue(SpeakDirectly(alert, speaker));
                CollectionAssert.AreEqual(new[] { Raw }, speaker.Submitted.ToArray());
            }
        }

        [TestMethod]
        public void IdenticalLocalHudTextClearsFirmwareSpeechProvenance()
        {
            using (var state = new CurrentState())
            {
                var speaker = new Speaker();
                Publish(state, () => Translated);
                Assert.AreEqual(Raw + "  " + Translated, state.messageHighDisplay);

                state.messageHigh = Raw;
                Assert.AreEqual(Raw, state.messageHighDisplay);
                Assert.AreEqual(Raw, state.SpeakHighMessageIfChanged(speaker, "previous"));
                CollectionAssert.AreEqual(new[] { Raw }, speaker.Submitted.ToArray());
            }
        }
    }
}
