using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace MissionPlanner
{
    // Owned by one receiver generation; all per-list operations use its existing lock.
    internal sealed class MessageDisplayContext
    {
        private readonly ConditionalWeakTable<List<(DateTime time, string message, byte severity)>, State> states =
            new ConditionalWeakTable<List<(DateTime time, string message, byte severity)>, State>();
        private sealed class State
        {
            internal int Heartbeat, Version;
            internal bool Conflict;
            internal bool UnconfirmedVoiceVendor;
            internal readonly ConditionalWeakTable<string, Row> Rows = new ConditionalWeakTable<string, Row>();
        }
        private sealed class Row
        {
            internal DateTime Time;
            internal byte Severity;
        }
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        internal void Observe(List<(DateTime time, string message, byte severity)> list, MAVLink.MAVLinkMessage packet)
        {
            lock (list)
            {
                var s = states.GetValue(list, ignored => new State());
                if (packet.msgid == (uint)MAVLink.MAVLINK_MSG_ID.HEARTBEAT)
                {
                    var hb = packet.ToStructure<MAVLink.mavlink_heartbeat_t>();
                    int family = hb.autopilot == 3 && hb.type == 1 ? 1 : hb.autopilot == 12 ? 2 : 0;
                    if (family == 0 || (s.Heartbeat != 0 && s.Heartbeat != family)) s.Conflict = true;
                    else s.Heartbeat = family;
                }
                else
                {
                    var ver = packet.ToStructure<MAVLink.mavlink_autopilot_version_t>();
                    var hash = ver.flight_custom_version;
                    int version = ver.flight_sw_version == 0x040703ff && hash != null &&
                        Encoding.ASCII.GetString(hash) == "d9c5097c" ? 1 :
                        ver.flight_sw_version == 0x011200ff && hash != null && hash.Length == 8 &&
                        hash[3] == 0xd6 && hash[4] == 0xf1 && hash[5] == 0xdd && hash[6] == 0x5a && hash[7] == 0x93 ? 2 : 0;
                    if (version == 2 && (hash[0] != 0 || hash[1] != 0 || hash[2] != 0))
                        s.UnconfirmedVoiceVendor = true;
                    if (version == 0 || (s.Version != 0 && s.Version != version)) s.Conflict = true;
                    else s.Version = version;
                }
            }
        }

        internal void Mark(List<(DateTime time, string message, byte severity)> list, MAVLink.MAVLinkMessage packet,
            MAVLink.mavlink_statustext_t text, (DateTime time, string message, byte severity) row)
        {
            State s;
            if (!states.TryGetValue(list, out s) || s.Heartbeat == 0 || s.Heartbeat != s.Version || s.Conflict ||
                text.id != 0 || text.chunk_seq != 0 || packet.payloadlength < 2 ||
                (packet.header == 254 && packet.payloadlength != 51))
                return;
            var length = Math.Min(50, packet.payloadlength - 1);
            var start = packet.headerlength + 1;
            var end = Array.IndexOf(packet.buffer, (byte)0, start, length);
            if (end < 0 && !(packet.header == 253 && length < 50)) return;
            try
            {
                if (StrictUtf8.GetString(packet.buffer, start, end < 0 ? length : end - start) != row.message) return;
            }
            catch (DecoderFallbackException) { return; }

            // Keep only known supported raw literals, never translated text at receive time.
            if (s.Version == 1
                ? row.message != "GPS 1: detected u-blox" && row.message != "EKF3 IMU0 origin set"
                : row.message != "Battery 1 missing\t" || row.severity != 2) return;
            if (s.Rows.TryGetValue(row.message, out var existing))
            {
                existing.Time = DateTime.MinValue; // unexpectedly reused token
                return;
            }
            s.Rows.Add(row.message, new Row { Time = row.time, Severity = row.severity });
        }

        internal string VoiceSuffix(List<(DateTime time, string message, byte severity)> list,
            (DateTime time, string message, byte severity) row)
        {
            lock (list)
            {
                if (row.message != "Battery 1 missing\t" ||
                    !states.TryGetValue(list, out var state) || state.Version != 2 ||
                    state.UnconfirmedVoiceVendor)
                    return null;
                return Suffix(list, row);
            }
        }

        internal string Suffix(List<(DateTime time, string message, byte severity)> list,
            (DateTime time, string message, byte severity) row)
        {
            lock (list)
            {
                State s;
                Row proof;
                if (row.message == null || !states.TryGetValue(list, out s) || s.Conflict ||
                    !s.Rows.TryGetValue(row.message, out proof) || proof.Time != row.time ||
                    proof.Severity != row.severity) return null;

                if (s.Version == 2)
                    return row.message == "Battery 1 missing\t" ? "未检测到 1 号电池" : null;
                if (row.message == "GPS 1: detected u-blox") return "检测到 GPS 1：u-blox";
                if (row.message == "EKF3 IMU0 origin set") return "EKF3 IMU0 原点已设置";
                return null;
            }
        }
    }
}