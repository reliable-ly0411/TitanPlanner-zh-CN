using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MissionPlanner.Utilities;

namespace MissionPlannerTests.Utilities
{
    [TestClass]
    public class FirmwareSpeechAlertTests
    {
        private sealed class SpeechQueue : ISpeech, ISpeechSubmission
        {
            internal readonly List<string> Accepted = new List<string>();
            internal bool Accept = true;
            public bool speechEnable { get; set; } = true;
            public bool IsReady => true;
            public void SpeakAsync(string text) => Accepted.Add(text);
            public void SpeakAsyncCancelAll() { }
            public bool TrySpeakAsync(string text)
            {
                if (Accept) Accepted.Add(text);
                return Accept;
            }
        }

        private sealed class AlertFixture
        {
            private readonly object instance;
            private readonly MethodInfo speak;
            private readonly PropertyInfo isSubmitted;

            internal AlertFixture(Func<string> text, bool suppress = false)
            {
                var type = typeof(MissionPlanner.MAVLinkInterface).Assembly.GetType("MissionPlanner.FirmwareSpeechAlert", true);
                instance = Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.NonPublic,
                    null, new object[] { text, suppress }, null);
                speak = type.GetMethod("Speak", BindingFlags.Instance | BindingFlags.NonPublic);
                isSubmitted = type.GetProperty("IsSubmitted", BindingFlags.Instance | BindingFlags.NonPublic);
            }

            internal bool Speak(ISpeech speaker, string raw) => (bool)speak.Invoke(instance, new object[] { speaker, raw });
            internal bool IsSubmitted => (bool)isSubmitted.GetValue(instance);
        }

        [TestMethod]
        public void AcceptedAlertIsSharedByReceiveAndHudReadback()
        {
            string current = "已停止记录日志";
            var alert = new AlertFixture(() => current);
            var speaker = new SpeechQueue();

            Assert.IsTrue(alert.Speak(speaker, "Chip full, logging stopped"));
            Assert.IsTrue(alert.Speak(speaker, "Chip full, logging stopped"));
            Assert.AreEqual(1, speaker.Accepted.Count);
            Assert.AreEqual("已停止记录日志", speaker.Accepted[0]);
        }

        [TestMethod]
        public void RejectedAlertCanBeRetriedByHudReadback()
        {
            var alert = new AlertFixture(() => "译文");
            var speaker = new SpeechQueue { Accept = false };

            Assert.IsFalse(alert.Speak(speaker, "raw"));
            Assert.IsFalse(alert.IsSubmitted);
            speaker.Accept = true;
            Assert.IsTrue(alert.Speak(speaker, "raw"));
            Assert.IsTrue(alert.IsSubmitted);
            CollectionAssert.AreEqual(new[] { "译文" }, speaker.Accepted.ToArray());
        }

        [TestMethod]
        public void IncompleteStatustextNeverReturnsPartialSpeechText()
        {
            var type = typeof(MAVLinkInterface).Assembly.GetType("MissionPlanner.SpeechStatusTextAssembler", true);
            var assembler = Activator.CreateInstance(type, true);
            var append = type.GetMethod("TryAppend", BindingFlags.Instance | BindingFlags.NonPublic);

            var first = new MAVLink.mavlink_statustext_t
            {
                severity = (byte)MAVLink.MAV_SEVERITY.NOTICE,
                id = 44,
                chunk_seq = 0,
                text = Encoding.UTF8.GetBytes(new string('A', 50))
            };
            var firstArgs = new object[] { (uint)1, (byte)1, first, null };
            Assert.IsFalse((bool)append.Invoke(assembler, firstArgs));
            Assert.IsNull(firstArgs[3]);

            var secondText = new byte[50];
            Encoding.UTF8.GetBytes("tail").CopyTo(secondText, 0);
            var second = new MAVLink.mavlink_statustext_t
            {
                severity = (byte)MAVLink.MAV_SEVERITY.NOTICE,
                id = 44,
                chunk_seq = 1,
                text = secondText
            };
            var secondArgs = new object[] { (uint)1, (byte)1, second, null };
            Assert.IsTrue((bool)append.Invoke(assembler, secondArgs));
            Assert.AreEqual(new string('A', 50) + "tail", secondArgs[3]);
        }

        [TestMethod]
        public void OutOfOrderOrCrossVehicleChunksAreNotAssembled()
        {
            var type = typeof(MAVLinkInterface).Assembly.GetType("MissionPlanner.SpeechStatusTextAssembler", true);
            var assembler = Activator.CreateInstance(type, true);
            var append = type.GetMethod("TryAppend", BindingFlags.Instance | BindingFlags.NonPublic);
            var first = Chunk(7, 0, new string('A', 50), (byte)MAVLink.MAV_SEVERITY.NOTICE);
            var second = Chunk(7, 2, "B", (byte)MAVLink.MAV_SEVERITY.NOTICE);

            Assert.IsFalse((bool)append.Invoke(assembler, new object[] { (uint)1, (byte)1, first, null }));
            Assert.IsFalse((bool)append.Invoke(assembler, new object[] { (uint)1, (byte)1, second, null }));

            Assert.IsFalse((bool)append.Invoke(assembler, new object[] { (uint)1, (byte)1, first, null }));
            var otherVehicleFinal = Chunk(7, 1, "B", (byte)MAVLink.MAV_SEVERITY.NOTICE);
            Assert.IsFalse((bool)append.Invoke(assembler,
                new object[] { (uint)2, (byte)1, otherVehicleFinal, null }));
        }

        private static MAVLink.mavlink_statustext_t Chunk(ushort id, byte sequence, string text, byte severity)
        {
            var bytes = new byte[50];
            Encoding.UTF8.GetBytes(text).CopyTo(bytes, 0);
            return new MAVLink.mavlink_statustext_t
            {
                severity = severity,
                id = id,
                chunk_seq = sequence,
                text = bytes
            };
        }
    }
}
