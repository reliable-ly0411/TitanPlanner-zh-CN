using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MissionPlannerTests.Utilities
{
    [TestClass]
    public class Plane444DiagnosticFileDisplayTests
    {
        // The proof recorder is internal to the receiver; exercise it without opening a port.
        private sealed class Fixture
        {
            private readonly Type type;
            private readonly object context;
            private readonly MAVLink.MavlinkParse codec = new MAVLink.MavlinkParse();
            internal readonly List<(DateTime time, string message, byte severity)> Rows =
                new List<(DateTime time, string message, byte severity)>();

            internal Fixture(byte? autopilot = 3, byte vehicle = 1, string hash = "16b78382",
                uint version = 0x040404ff)
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

            internal string Translate(string raw, byte severity = 5, ushort id = 0, byte sequence = 0,
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

        private static void InCulture(string name, Action run)
        {
            var saved = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);
                run();
            }
            finally
            {
                CultureInfo.CurrentUICulture = saved;
            }
        }

        private const string Raw = "Saved crash_dump.bin";

        [TestMethod]
        public void AddsOnlyPinnedFileSavedNotice() => InCulture("zh-CN", () =>
        {
            Assert.AreEqual("已保存 crash_dump.bin", new Fixture().Translate(Raw));
            foreach (byte severity in new byte[] { 2, 4, 6, 7 })
                Assert.IsNull(new Fixture().Translate(Raw, severity: severity));
            foreach (string other in new[] { "Saved crash_dump.BIN", "Saved crash_dump.bin ",
                         "Saved other.bin", "Forcing logging for watchdog reset", "Saved crash_dump.bi" })
                Assert.IsNull(new Fixture().Translate(other));
            Assert.IsNull(new Fixture().Translate(Raw, id: 3));
            Assert.IsNull(new Fixture().Translate(Raw, sequence: 1));
            Assert.IsNull(new Fixture().Translate(Raw, packetText: Raw + "X"));
            Assert.IsNull(new Fixture(hash: null).Translate(Raw));
            Assert.IsNull(new Fixture(hash: "16b78381").Translate(Raw));
            Assert.IsNull(new Fixture(hash: "0358a9c2", version: 0x040507ff).Translate(Raw));
            Assert.IsNull(new Fixture(hash: "3fc7011a", version: 0x040603ff).Translate(Raw));
            Assert.IsNull(new Fixture(hash: "d9c5097c", version: 0x040703ff).Translate(Raw));
            Assert.IsNull(new Fixture(vehicle: 2).Translate(Raw));
            Assert.IsNull(new Fixture(autopilot: 12).Translate(Raw));
        });

        [TestMethod]
        public void MissingEnhancedCatalogKeepsOriginal()
        {
            foreach (var name in new[] { "en-US", "zh-Hans" })
                InCulture(name, () =>
                {
                    Assert.AreEqual(Raw, MissionPlanner.Utilities.UiText.Translate(Raw));
                    Assert.IsNull(new Fixture().Translate(Raw));
                });
        }
    }
}
