using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MissionPlannerTests.Utilities
{
    [TestClass]
    public class PlaneReleaseBannerDisplayTests
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

        private static readonly (string Version, uint VersionBits, string Hash)[] Supported = {
            ("4.4.4", 0x040404ff, "16b78382"), ("4.5.7", 0x040507ff, "0358a9c2"),
            ("4.6.3", 0x040603ff, "3fc7011a"), ("4.7.3", 0x040703ff, "d9c5097c")
        };

        [TestMethod]
        public void AddsOnlyTheReleasedLiteralForItsOwnFirmware() => InCulture("zh-CN", () =>
        {
            foreach (var (version, bits, hash) in Supported)
            {
                var raw = $"ArduPlane V{version} ({hash})";
                var fixture = new Fixture(hash: hash, version: bits);
                Assert.AreEqual($"ArduPlane 固件版本 {version}（{hash}）", fixture.Translate(raw));
                foreach (var (other, otherBits, otherHash) in Supported)
                    if (otherBits != bits)
                        Assert.IsNull(fixture.Translate($"ArduPlane V{other} ({otherHash})"));
                Assert.IsNull(fixture.Translate("ArduPlane V" + version));
                Assert.IsNull(fixture.Translate(raw + " "));
                Assert.IsNull(fixture.Translate(raw.Replace("ArduPlane", "VendorPlane")));
                Assert.IsNull(fixture.Translate(raw, severity: 4));
                Assert.IsNull(fixture.Translate(raw, id: 8));
                Assert.IsNull(fixture.Translate(raw, sequence: 1));
                Assert.IsNull(fixture.Translate(raw, packetText: raw + "X"));
                Assert.IsNull(new Fixture(hash: "abcdef01", version: bits).Translate(raw));
                Assert.IsNull(new Fixture(hash: hash, version: bits, vehicle: 2).Translate(raw));
                Assert.IsNull(new Fixture(hash: hash, version: bits, autopilot: null).Translate(raw));
            }
        });

        [TestMethod]
        public void MissingEnhancedCatalogKeepsAllFourOriginals()
        {
            foreach (var name in new[] { "en-US", "zh-Hans" })
                InCulture(name, () =>
                {
                    foreach (var (version, bits, hash) in Supported)
                    {
                        var raw = $"ArduPlane V{version} ({hash})";
                        Assert.AreEqual(raw, MissionPlanner.Utilities.UiText.Translate(raw));
                        Assert.IsNull(new Fixture(hash: hash, version: bits).Translate(raw));
                    }
                });
        }
    }
}
