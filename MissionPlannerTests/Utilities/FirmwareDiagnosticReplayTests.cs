using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MissionPlannerTests.Utilities
{
    [TestClass]
    public class FirmwareDiagnosticReplayTests
    {
        // 按固定版本源码构造的回放输入，无需连接飞控。
        // ArduPilot Plane-4.7.3 d9c5097c，AP_Logger_Block.cpp。
        // PX4 v1.17.0 d6f12ad1，EKF2.cpp 与 MAVLink STATUSTEXT.hpp。
        private const string PlaneText = "Chip full, logging stopped";
        private const string Px4Text = "EKF2_MAG_TYPE invalid, resetting to default";
        private const string PlaneChinese = "日志存储芯片已满，已停止记录日志";
        private const string Px4Chinese = "EKF2_MAG_TYPE 无效，正在恢复默认值";

        private sealed class Replay
        {
            private static readonly DateTime Start = new DateTime(2026, 11, 27, 2, 5, 0, DateTimeKind.Utc);
            private readonly Type contextType;
            private readonly object context;
            private readonly MAVLink.MavlinkParse codec = new MAVLink.MavlinkParse();
            internal readonly List<(DateTime time, string message, byte severity)> Rows =
                new List<(DateTime time, string message, byte severity)>();

            internal Replay(bool px4 = false, byte? vehicle = null, uint? version = null, byte? autopilot = null,
                byte[] hash = null, byte proofComponent = 1)
            {
                const string name = "MissionPlanner.MessageDisplayContext";
                contextType = typeof(Replay).Assembly.GetType(name) ??
                    typeof(MissionPlanner.MAVLinkInterface).Assembly.GetType(name, true);
                context = Activator.CreateInstance(contextType, true);
                Observe(MAVLink.MAVLINK_MSG_ID.HEARTBEAT, new MAVLink.mavlink_heartbeat_t
                {
                    autopilot = autopilot ?? (byte)(px4 ? 12 : 3),
                    type = vehicle ?? (byte)(px4 ? 2 : 1)
                }, proofComponent);
                Observe(MAVLink.MAVLINK_MSG_ID.AUTOPILOT_VERSION, new MAVLink.mavlink_autopilot_version_t
                {
                    flight_sw_version = version ?? (px4 ? 0x011100ffu : 0x040703ffu),
                    flight_custom_version = hash ?? (px4
                        ? new byte[] { 0, 0, 0, 0xc4, 0xd1, 0x2a, 0xf1, 0xd6 }
                        : Encoding.ASCII.GetBytes("d9c5097c")),
                    middleware_custom_version = new byte[8],
                    os_custom_version = new byte[8],
                    uid2 = new byte[18]
                }, proofComponent);
            }

            private object Call(string method, params object[] parameters) =>
                contextType.GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(context, parameters);

            private MAVLink.MAVLinkMessage Decode(MAVLink.MAVLINK_MSG_ID id, object data, byte component)
            {
                var wire = codec.GenerateMAVLinkPacket20(id, data, sysid: 1, compid: component);
                using (var stream = new MemoryStream(wire))
                {
                    var packet = codec.ReadPacket(stream);
                    Assert.IsNotNull(packet);
                    Assert.AreEqual((uint)id, packet.msgid);
                    Assert.AreEqual((uint)component, (uint)packet.compid);
                    Assert.AreEqual(stream.Length, stream.Position);
                    return packet;
                }
            }

            private void Observe(MAVLink.MAVLINK_MSG_ID id, object data, byte component)
            {
                var packet = Decode(id, data, component);
                // 模拟接收器外侧的主组件门槛。上下文按消息列表保存每路证明。
                if (packet.compid == 1)
                    Call("Observe", Rows, packet);
            }

            internal (DateTime time, string message, byte severity) Status(string payload, byte severity,
                ushort id = 0, byte sequence = 0, byte component = 1, string rowText = null)
            {
                var bytes = new byte[50];
                Encoding.UTF8.GetBytes(payload).CopyTo(bytes, 0);
                var data = new MAVLink.mavlink_statustext_t { text = bytes, severity = severity, id = id, chunk_seq = sequence };
                var packet = Decode(MAVLink.MAVLINK_MSG_ID.STATUSTEXT, data, component);
                var decoded = packet.ToStructure<MAVLink.mavlink_statustext_t>();
                Assert.AreEqual(severity, decoded.severity);
                Assert.AreEqual(id, decoded.id);
                Assert.AreEqual(sequence, decoded.chunk_seq);
                var raw = Encoding.UTF8.GetString(decoded.text);
                int terminator = raw.IndexOf('\0');
                if (terminator >= 0)
                    raw = raw.Substring(0, terminator);
                var row = (Start.AddSeconds(Rows.Count), rowText ?? raw, decoded.severity);
                Rows.Add(row);
                if (packet.compid == 1)
                    Call("Mark", Rows, packet, decoded, row);
                return row;
            }

            internal string Display((DateTime time, string message, byte severity) row) =>
                (string)Call("Suffix", Rows, row);
        }

        private static void InCulture(string name, Action check)
        {
            var previous = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);
                check();
            }
            finally
            {
                CultureInfo.CurrentUICulture = previous;
            }
        }

        [TestMethod]
        public void DisplayAddsChineseForProvenSamplesAndPreservesRows() => InCulture("zh-CN", () =>
        {
            var plane = new Replay();
            var warning = plane.Status(PlaneText, 4);
            Assert.AreEqual(PlaneChinese, plane.Display(warning));

            var px4 = new Replay(px4: true);
            var critical = px4.Status(Px4Text, 2, id: 1);
            Assert.AreEqual(Px4Chinese, px4.Display(critical));

            var unknown = px4.Status("Unknown diagnostic", 2, id: 2);
            var repeated = px4.Status(Px4Text, 2, id: 3);
            Assert.IsNull(px4.Display(unknown));
            Assert.AreEqual(Px4Chinese, px4.Display(critical));
            Assert.AreEqual(Px4Chinese, px4.Display(repeated));

            var start = new DateTime(2026, 11, 27, 2, 5, 0, DateTimeKind.Utc);
            CollectionAssert.AreEqual(new[] { (start, PlaneText, (byte)4) }, plane.Rows);
            CollectionAssert.AreEqual(new[]
            {
                (start, Px4Text, (byte)2),
                (start.AddSeconds(1), "Unknown diagnostic", (byte)2),
                (start.AddSeconds(2), Px4Text, (byte)2)
            }, px4.Rows);
            Assert.IsNull(plane.Display(critical));
            Assert.IsNull(px4.Display((critical.time.AddSeconds(1), critical.message, critical.severity)));
            Assert.IsNull(px4.Display((critical.time, critical.message, 4)));
            Assert.IsNull(px4.Display((critical.time, new string(critical.message.ToCharArray()), critical.severity)));
        });

        [TestMethod]
        public void DisplayKeepsOriginalForOtherSources() => InCulture("zh-CN", () =>
        {
            foreach (var replay in new[]
            {
                new Replay(px4: true, vehicle: 1),
                new Replay(px4: true, autopilot: 3),
                new Replay(px4: true, version: 0x011101ff),
                new Replay(px4: true, hash: new byte[] { 0, 0, 0, 0xc4, 0xd1, 0x2a, 0xf1, 0xff }),
                new Replay(px4: true, hash: new byte[] { 1, 0, 0, 0xc4, 0xd1, 0x2a, 0xf1, 0xd6 }),
                new Replay(px4: true, proofComponent: 2),
                new Replay()
            })
                Assert.IsNull(replay.Display(replay.Status(Px4Text, 2, id: 1)));

            foreach (var replay in new[]
            {
                new Replay(vehicle: 2),
                new Replay(version: 0x040603ff),
                new Replay(hash: Encoding.ASCII.GetBytes("abcdef01")),
                new Replay(px4: true)
            })
                Assert.IsNull(replay.Display(replay.Status(PlaneText, 4)));
        });

        [TestMethod]
        public void DisplayKeepsOriginalForUnverifiedFrames() => InCulture("zh-CN", () =>
        {
            var plane = new Replay();
            Assert.IsNull(plane.Display(plane.Status(PlaneText, 2)));
            Assert.IsNull(plane.Display(plane.Status(PlaneText, 4, id: 1)));
            Assert.IsNull(plane.Display(plane.Status(PlaneText + " ", 4)));
            Assert.IsNull(plane.Display(plane.Status(PlaneText + "\0hidden", 4)));

            var px4 = new Replay(px4: true);
            Assert.IsNull(px4.Display(px4.Status(Px4Text, 4, id: 1)));
            Assert.IsNull(px4.Display(px4.Status(Px4Text, 2, id: 2, sequence: 1)));
            Assert.IsNull(px4.Display(px4.Status(Px4Text, 2, id: 3, component: 2)));
            Assert.IsNull(px4.Display(px4.Status(Px4Text + "\0hidden", 2, id: 4)));
            Assert.IsNull(px4.Display(px4.Status(Px4Text + " suffix", 2, id: 5, rowText: Px4Text)));
            Assert.IsNull(px4.Display(px4.Status(Px4Text.Substring(0, Px4Text.Length - 1), 2, id: 6, rowText: Px4Text)));
        });

        [TestMethod]
        public void DisplayKeepsOriginalOutsideEnhancedChinese()
        {
            foreach (var culture in new[] { "zh-Hans", "en-US" })
                InCulture(culture, () =>
                {
                    var plane = new Replay();
                    var px4 = new Replay(px4: true);
                    Assert.IsNull(plane.Display(plane.Status(PlaneText, 4)));
                    Assert.IsNull(px4.Display(px4.Status(Px4Text, 2, id: 1)));
                });
        }
    }
}
