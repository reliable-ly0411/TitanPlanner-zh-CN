using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MissionPlannerTests.Utilities
{
    [TestClass]
    public class Gps2DetectionStatusDisplayTests
    {
        // The proof recorder is internal to the receiver; exercise it without opening a port.
        private sealed class Fixture
        {
            private readonly Type type;
            private readonly object context;
            private readonly MAVLink.MavlinkParse codec = new MAVLink.MavlinkParse();
            internal readonly List<(DateTime time, string message, byte severity)> Rows =
                new List<(DateTime time, string message, byte severity)>();

            internal Fixture(byte? autopilot = 3, byte vehicle = 1, string hash = "d9c5097c",
                uint version = 0x040703ff)
            {
                const string typeName = "MissionPlanner.MessageDisplayContext";
                type = typeof(Fixture).Assembly.GetType(typeName) ??
                    typeof(MissionPlanner.MAVLinkInterface).Assembly.GetType(typeName, true);
                context = Activator.CreateInstance(type, true);
                if (autopilot.HasValue)
                    Call("Observe", Rows, Packet(MAVLink.MAVLINK_MSG_ID.HEARTBEAT,
                        new MAVLink.mavlink_heartbeat_t { autopilot = autopilot.Value, type = vehicle }));
                if (hash != null)
                    Call("Observe", Rows, Packet(MAVLink.MAVLINK_MSG_ID.AUTOPILOT_VERSION,
                        new MAVLink.mavlink_autopilot_version_t {
                            flight_sw_version = version, flight_custom_version = Encoding.ASCII.GetBytes(hash),
                            middleware_custom_version = new byte[8], os_custom_version = new byte[8], uid2 = new byte[18]
                        }));
            }

            private MAVLink.MAVLinkMessage Packet(MAVLink.MAVLINK_MSG_ID id, object payload)
                => new MAVLink.MAVLinkMessage(codec.GenerateMAVLinkPacket20(id, payload, sysid: 1, compid: 1));

            private object Call(string name, params object[] values)
                => type.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(context, values);

            internal string Translate(string raw, byte severity = 6, ushort id = 0, byte sequence = 0,
                string packetText = null)
            {
                var bytes = new byte[50];
                Encoding.UTF8.GetBytes(packetText ?? raw).CopyTo(bytes, 0);
                var text = new MAVLink.mavlink_statustext_t { text = bytes, severity = severity, id = id, chunk_seq = sequence };
                var packet = Packet(MAVLink.MAVLINK_MSG_ID.STATUSTEXT, text);
                var row = (new DateTime(2026, 11, 27, 2, 0, Rows.Count, DateTimeKind.Utc), raw, severity);
                Rows.Add(row);
                Call("Mark", Rows, packet, packet.ToStructure<MAVLink.mavlink_statustext_t>(), row);
                Assert.AreEqual(row, Rows[Rows.Count - 1]);
                Assert.IsNull(Call("VoiceSuffix", Rows, row));
                return (string)Call("Suffix", Rows, row);
            }
        }

        [TestMethod]
        public void Gps2DetectionOnlyAddsAnAuditedDisplaySuffix()
        {
            var fixture = new Fixture();
            const string raw = "GPS 2: detected u-blox";
            Assert.AreEqual("检测到 GPS 2：u-blox", fixture.Translate(raw));
            Assert.AreEqual("检测到 GPS 1：u-blox", fixture.Translate("GPS 1: detected u-blox"));
            Assert.AreEqual(2, fixture.Rows.Count);
        }

        [TestMethod]
        public void OtherOriginsAndMessagesKeepTheirOriginalText()
        {
            const string raw = "GPS 2: detected u-blox";
            Assert.IsNull(new Fixture(autopilot: null).Translate(raw));
            Assert.IsNull(new Fixture(autopilot: 12).Translate(raw));
            Assert.IsNull(new Fixture(vehicle: 2).Translate(raw));
            Assert.IsNull(new Fixture(hash: null).Translate(raw));
            Assert.IsNull(new Fixture(hash: "d9c50971").Translate(raw));
            Assert.IsNull(new Fixture(hash: "3fc7011a", version: 0x040603ff).Translate(raw));
            Assert.IsNull(new Fixture().Translate(raw, severity: 4));
            Assert.IsNull(new Fixture().Translate(raw, id: 99));
            Assert.IsNull(new Fixture().Translate(raw, sequence: 1));
            Assert.IsNull(new Fixture().Translate(raw, packetText: "GPS 3: detected u-blox"));
            var exact = new Fixture();
            foreach (var variant in new[] { "GPS 3: detected u-blox", "GPS 02: detected u-blox",
                "GPS 2: detected u-blo", raw + " ", "GPS 2: detected OtherBrand", "GPS 2: detected U-BLOX" })
                Assert.IsNull(exact.Translate(variant));
            Assert.AreEqual(6, exact.Rows.Count);
        }
    }
}
