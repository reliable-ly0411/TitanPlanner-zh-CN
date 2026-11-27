using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using MissionPlanner.Utilities;

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
        private const string PlaneGps2UbloxDetection = "GPS 2: detected u-blox";
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        private static bool IsPx4(int version) => version == 2 || version == 5 || version == 6 || version == 9;
        private static int FamilyForVersion(int version) => IsPx4(version) ? 2 : version != 0 ? 1 : 0;

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
                    int version = 0;
                    if (hash != null && hash.Length == 8)
                    {
                        var ascii = Encoding.ASCII.GetString(hash);
                        if (ver.flight_sw_version == 0x040703ff && ascii == "d9c5097c") version = 1;
                        else if (ver.flight_sw_version == 0x040203ff && ascii == "55685c43") version = 3;
                        else if (ver.flight_sw_version == 0x040603ff && ascii == "3fc7011a") version = 4;
                        else if (ver.flight_sw_version == 0x040404ff && ascii == "16b78382") version = 7;
                        else if (ver.flight_sw_version == 0x040507ff && ascii == "0358a9c2") version = 8;
                        else if (ver.flight_sw_version == 0x010f04ff &&
                                 BinaryPrefix(hash, 0xff, 0x07, 0x04, 0xc4, 0x99)) version = 9;
                        else if (ver.flight_sw_version == 0x011200ff &&
                                 BinaryPrefix(hash, 0xd6, 0xf1, 0xdd, 0x5a, 0x93)) version = 2;
                        else if (ver.flight_sw_version == 0x011002ff &&
                                 BinaryPrefix(hash, 0xfc, 0x5f, 0x45, 0xf0, 0x54)) version = 5;
                        else if (ver.flight_sw_version == 0x011100ff &&
                                 BinaryPrefix(hash, 0xc4, 0xd1, 0x2a, 0xf1, 0xd6)) version = 6;
                    }
                    if (IsPx4(version) && (hash[0] != 0 || hash[1] != 0 || hash[2] != 0))
                        s.UnconfirmedVoiceVendor = true;
                    if (version == 0 || (s.Version != 0 && s.Version != version)) s.Conflict = true;
                    else s.Version = version;
                }
            }
        }

        private static bool BinaryPrefix(byte[] hash, byte b3, byte b4, byte b5, byte b6, byte b7)
            => hash[3] == b3 && hash[4] == b4 && hash[5] == b5 && hash[6] == b6 && hash[7] == b7;

        private static string Px4BatteryText(int version) => version == 9 ? "Battery 1 disconnected\t" : "Battery 1 missing\t";

        internal void Mark(List<(DateTime time, string message, byte severity)> list, MAVLink.MAVLinkMessage packet,
            MAVLink.mavlink_statustext_t text, (DateTime time, string message, byte severity) row)
        {
            State s;
            if (!states.TryGetValue(list, out s) || s.Heartbeat == 0 || s.Heartbeat != FamilyForVersion(s.Version) || s.Conflict ||
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
            bool known = IsPx4(s.Version)
                ? row.message == Px4BatteryText(s.Version) && row.severity == 2
                : (row.message == "EKF3 IMU0 origin set" && (s.Version == 1 || row.severity == 6)) ||
                  (s.Version == 1 && row.message == "GPS 1: detected u-blox");
            // ArduPlane 4.7.3 AP_GPS INFO: accept the verified second receiver only.
            if (!known && !(s.Version == 1 && row.severity == 6 && row.message == PlaneGps2UbloxDetection)) return;
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
                if (!states.TryGetValue(list, out var state) || !IsPx4(state.Version) ||
                    state.UnconfirmedVoiceVendor || row.message != Px4BatteryText(state.Version))
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

                if (IsPx4(s.Version))
                    return row.message == Px4BatteryText(s.Version) ? (s.Version == 9 ? "1 号电池已断开" : "未检测到 1 号电池") : null;
                if (row.message == "GPS 1: detected u-blox") return "检测到 GPS 1：u-blox";
                if (row.message == PlaneGps2UbloxDetection)
                {
                    var translated = UiText.Translate(PlaneGps2UbloxDetection);
                    return string.Equals(translated, row.message, StringComparison.Ordinal) ? null : translated;
                }
                if (row.message == "EKF3 IMU0 origin set") return "EKF3 IMU0 原点已设置";
                return null;
            }
        }
    }
}