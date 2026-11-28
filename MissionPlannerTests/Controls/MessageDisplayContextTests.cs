using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MissionPlanner;

namespace MissionPlannerTests.Controls
{
    [TestClass]
    public class MessageDisplayContextTests
    {
        private const string Origin = "EKF3 IMU1 origin set";
        private const byte Info = (byte)MAVLink.MAV_SEVERITY.INFO;

        private sealed class Fixture
        {
            private static readonly Type ContextType = typeof(MAVLinkInterface).Assembly
                .GetType("MissionPlanner.MessageDisplayContext", true);
            private readonly object context = Activator.CreateInstance(ContextType, true);
            private readonly MAVLink.MavlinkParse wire = new MAVLink.MavlinkParse();
            internal readonly List<(DateTime time, string message, byte severity)> Rows =
                new List<(DateTime, string, byte)>();

            internal void Heartbeat(MAVLink.MAV_TYPE type) =>
                Call("Observe", Rows, Frame(MAVLink.MAVLINK_MSG_ID.HEARTBEAT,
                    new MAVLink.mavlink_heartbeat_t
                    {
                        autopilot = (byte)MAVLink.MAV_AUTOPILOT.ARDUPILOTMEGA,
                        type = (byte)type,
                        mavlink_version = 3
                    }));

            internal void Version(uint value, string hash) =>
                Call("Observe", Rows, Frame(MAVLink.MAVLINK_MSG_ID.AUTOPILOT_VERSION,
                    new MAVLink.mavlink_autopilot_version_t
                    {
                        flight_sw_version = value,
                        flight_custom_version = Encoding.ASCII.GetBytes(hash),
                        middleware_custom_version = new byte[8],
                        os_custom_version = new byte[8],
                        uid2 = new byte[18]
                    }));

            internal (DateTime time, string message, byte severity) Message(string text, byte severity,
                ushort id = 0, byte chunk = 0, string wireText = null)
            {
                var bytes = new byte[50];
                Encoding.UTF8.GetBytes(wireText ?? text).CopyTo(bytes, 0);
                var packet = Frame(MAVLink.MAVLINK_MSG_ID.STATUSTEXT,
                    new MAVLink.mavlink_statustext_t { severity = severity, text = bytes, id = id, chunk_seq = chunk });
                var row = (new DateTime(2026, 1, 8, 9, 10, 11, DateTimeKind.Utc), text, severity);
                Rows.Add(row);
                Call("Mark", Rows, packet, packet.ToStructure<MAVLink.mavlink_statustext_t>(), row);
                return row;
            }

            private MAVLink.MAVLinkMessage Frame(MAVLink.MAVLINK_MSG_ID id, object payload) =>
                new MAVLink.MAVLinkMessage(wire.GenerateMAVLinkPacket20(id, payload, false, 47, 1));

            private object Call(string method, params object[] args) =>
                ContextType.GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(context, args);

            internal string Suffix((DateTime time, string message, byte severity) row) =>
                (string)Call("Suffix", Rows, row);

            internal string Voice((DateTime time, string message, byte severity) row) =>
                (string)Call("VoiceSuffix", Rows, row);
        }

        private static Fixture Plane463()
        {
            var f = new Fixture();
            f.Heartbeat(MAVLink.MAV_TYPE.FIXED_WING);
            f.Version(0x040603ff, "3fc7011a");
            return f;
        }

        [TestMethod]
        public void Plane463Imu1AddsMessagesOnlySuffixAndPreservesTheOriginalRow()
        {
            var f = Plane463();
            var row = f.Message(Origin, Info);
            Assert.AreEqual("EKF3 IMU1 原点已设置", f.Suffix(row));
            Assert.IsNull(f.Voice(row));
            Assert.AreEqual(1, f.Rows.Count);
            Assert.AreEqual(row, f.Rows[0]);
            Assert.IsNull(f.Suffix((row.time.AddSeconds(1), row.message, row.severity)));
            Assert.IsNull(f.Suffix((row.time, new string(row.message.ToCharArray()), row.severity)));
            Assert.IsNull(f.Suffix((row.time, row.message, (byte)MAVLink.MAV_SEVERITY.WARNING)));
            f.Version(0x040603ff, "deadbeef");
            Assert.IsNull(f.Suffix(row));
            Assert.AreEqual(row, f.Rows[0]);
        }

        [TestMethod]
        public void NewCaptionRequiresExactSenderVersionSeverityAndCompleteLiteral()
        {
            var wrongVersion = new Fixture();
            wrongVersion.Heartbeat(MAVLink.MAV_TYPE.FIXED_WING);
            wrongVersion.Version(0x040507ff, "0358a9c2");
            Assert.IsNull(wrongVersion.Suffix(wrongVersion.Message(Origin, Info)));
            var wrongBuild = new Fixture();
            wrongBuild.Heartbeat(MAVLink.MAV_TYPE.FIXED_WING);
            wrongBuild.Version(0x040603ff, "deadbeef");
            Assert.IsNull(wrongBuild.Suffix(wrongBuild.Message(Origin, Info)));
            var wrongVehicle = new Fixture();
            wrongVehicle.Heartbeat(MAVLink.MAV_TYPE.QUADROTOR);
            wrongVehicle.Version(0x040603ff, "3fc7011a");
            Assert.IsNull(wrongVehicle.Suffix(wrongVehicle.Message(Origin, Info)));
            var noHeartbeat = new Fixture();
            noHeartbeat.Version(0x040603ff, "3fc7011a");
            Assert.IsNull(noHeartbeat.Suffix(noHeartbeat.Message(Origin, Info)));
            var noVersion = new Fixture();
            noVersion.Heartbeat(MAVLink.MAV_TYPE.FIXED_WING);
            Assert.IsNull(noVersion.Suffix(noVersion.Message(Origin, Info)));

            var f = Plane463();
            Assert.IsNull(f.Suffix(f.Message(Origin, (byte)MAVLink.MAV_SEVERITY.WARNING)));
            Assert.IsNull(f.Suffix(f.Message(Origin, Info, id: 12)));
            Assert.IsNull(f.Suffix(f.Message(Origin, Info, chunk: 1)));
            Assert.IsNull(f.Suffix(f.Message(Origin, Info, wireText: "EKF3 IMU1 origin se")));
            Assert.IsNull(f.Suffix(f.Message("EKF3 IMU2 origin set", Info)));
            Assert.IsNull(f.Suffix(f.Message(Origin + ".", Info)));
            var existing = f.Message("EKF3 IMU0 origin set", Info);
            Assert.AreEqual("EKF3 IMU0 原点已设置", f.Suffix(existing));
        }
    }
}
