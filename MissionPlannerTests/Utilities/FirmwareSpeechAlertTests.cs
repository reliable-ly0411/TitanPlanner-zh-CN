using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MissionPlanner;
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
        public void FullFiftyByteChunkFollowedByEmptyChunkCompletesOnce()
        {
            var type = typeof(MAVLinkInterface).Assembly.GetType("MissionPlanner.SpeechStatusTextAssembler", true);
            var assembler = Activator.CreateInstance(type, true);
            var append = type.GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(uint), typeof(byte), typeof(MAVLink.mavlink_statustext_t) }, null);

            var first = new MAVLink.mavlink_statustext_t
            {
                severity = (byte)MAVLink.MAV_SEVERITY.NOTICE,
                id = 44,
                chunk_seq = 0,
                text = Encoding.UTF8.GetBytes(new string('A', 50))
            };
            var second = new MAVLink.mavlink_statustext_t
            {
                severity = (byte)MAVLink.MAV_SEVERITY.NOTICE,
                id = 44,
                chunk_seq = 1,
                text = new byte[50]
            };
            Assert.IsNull(append.Invoke(assembler, new object[] { (uint)1, (byte)1, first }));
            Assert.AreEqual(new string('A', 50), append.Invoke(assembler, new object[] { (uint)1, (byte)1, second }));
        }

        [TestMethod]
        public void OutOfOrderInterleavedChunksRemainScopedBySystemAndComponent()
        {
            var type = typeof(MAVLinkInterface).Assembly.GetType("MissionPlanner.SpeechStatusTextAssembler", true);
            var assembler = Activator.CreateInstance(type, true);
            var append = type.GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(uint), typeof(byte), typeof(MAVLink.mavlink_statustext_t) }, null);
            var a0 = Chunk(7, 0, new string('A', 50), (byte)MAVLink.MAV_SEVERITY.NOTICE);
            var a1 = Chunk(7, 1, new string('C', 50), (byte)MAVLink.MAV_SEVERITY.NOTICE);
            var a2 = Chunk(7, 2, "tail", (byte)MAVLink.MAV_SEVERITY.NOTICE);
            var b0 = Chunk(7, 0, new string('B', 50), (byte)MAVLink.MAV_SEVERITY.WARNING);
            var b1 = Chunk(7, 1, "end", (byte)MAVLink.MAV_SEVERITY.WARNING);

            Assert.IsNull(append.Invoke(assembler, new object[] { (uint)1, (byte)1, a0 }));
            Assert.IsNull(append.Invoke(assembler, new object[] { (uint)2, (byte)1, b0 }));
            // The upstream assembler accepts out-of-order chunks, then joins once all are present.
            Assert.IsNull(append.Invoke(assembler, new object[] { (uint)1, (byte)1, a2 }));
            Assert.AreEqual(new string('A', 50) + new string('C', 50) + "tail",
                append.Invoke(assembler, new object[] { (uint)1, (byte)1, a1 }));
            Assert.AreEqual(new string('B', 50) + "end",
                append.Invoke(assembler, new object[] { (uint)2, (byte)1, b1 }));
        }

        [TestMethod]
        public void ExpiredPartialKeepsUpstreamSeverityAndMissingChunkMarker()
        {
            var type = typeof(MAVLinkInterface).Assembly.GetType("MissionPlanner.SpeechStatusTextAssembler", true);
            var assembler = Activator.CreateInstance(type, true);
            var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var add = type.GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(uint), typeof(byte), typeof(MAVLink.mavlink_statustext_t), typeof(DateTime) }, null);
            var lateChunk = Chunk(9, 1, "Tuning: unknown applet", (byte)MAVLink.MAV_SEVERITY.NOTICE);
            Assert.IsNull(add.Invoke(assembler, new object[] { (uint)1, (byte)1, lateChunk, start }));

            var expire = type.GetMethod("TakeExpired", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(DateTime) }, null);
            var rows = (IList)expire.Invoke(assembler, new object[] { start.AddSeconds(2) });
            Assert.AreEqual(1, rows.Count);
            var row = rows[0];
            Assert.AreEqual((byte)MAVLink.MAV_SEVERITY.NOTICE, row.GetType().GetField("Item3").GetValue(row));
            Assert.AreEqual(" ... Tuning: unknown applet", row.GetType().GetField("Item4").GetValue(row));
        }

        [TestMethod]
        public void CompleteSplitQuickTuneUsesPinnedTemplateAndUnknownTuningStaysRaw()
        {
            var type = typeof(MAVLinkInterface).Assembly.GetType("MissionPlanner.SpeechStatusTextAssembler", true);
            var assembler = Activator.CreateInstance(type, true);
            var add = type.GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(uint), typeof(byte), typeof(MAVLink.mavlink_statustext_t) }, null);
            const string quickTune = "Quicktune: attitude error 123456789012345.1deg - ABORTING";
            var bytes = Encoding.UTF8.GetBytes(quickTune);
            var first = new byte[50];
            var last = new byte[50];
            Array.Copy(bytes, 0, first, 0, first.Length);
            Array.Copy(bytes, 50, last, 0, bytes.Length - 50);
            var severity = (byte)MAVLink.MAV_SEVERITY.CRITICAL;
            Assert.IsNull(add.Invoke(assembler, new object[] { (uint)1, (byte)1,
                new MAVLink.mavlink_statustext_t { severity = severity, id = 19, chunk_seq = 0, text = first } }));
            var complete = (string)add.Invoke(assembler, new object[] { (uint)1, (byte)1,
                new MAVLink.mavlink_statustext_t { severity = severity, id = 19, chunk_seq = 1, text = last } });
            Assert.AreEqual(quickTune, complete);

            var translatorType = typeof(MAVLinkInterface).Assembly.GetType("MissionPlanner.FirmwareScriptSpeech", true);
            var translate = translatorType.GetMethod("Translate", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("zh-CN");
                var proof = new object[] { MAVLink.MAV_AUTOPILOT.ARDUPILOTMEGA, MAVLink.MAV_TYPE.QUADROTOR,
                    0x040703ffu, Encoding.ASCII.GetBytes("d9c5097c"), severity, complete };
                Assert.IsNotNull(translate.Invoke(null, proof));

                const string unknownTuning = "Tuning: unknown applet";
                proof[4] = (byte)MAVLink.MAV_SEVERITY.NOTICE;
                proof[5] = unknownTuning;
                Assert.IsNull(translate.Invoke(null, proof));
            }
            finally { CultureInfo.CurrentUICulture = previous; }
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
