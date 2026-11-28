using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MissionPlanner.Utilities;

namespace MissionPlanner
{
    // One alert is shared by the receive path and MainV2's high-priority readback.
    // A failed submission remains retryable; accepted prompts are never queued twice.
    internal sealed class FirmwareSpeechAlert
    {
        private readonly object gate = new object();
        private readonly Func<string> verifiedText;
        private readonly bool suppress;
        private bool submitted;

        internal FirmwareSpeechAlert(Func<string> verifiedText, bool suppress = false)
        {
            this.verifiedText = verifiedText;
            this.suppress = suppress;
        }

        internal bool Speak(ISpeech speaker, string raw)
        {
            lock (gate)
            {
                if (submitted) return true;
                if (suppress)
                {
                    submitted = true;
                    return true;
                }
                var text = verifiedText?.Invoke() ?? raw;
                if (string.IsNullOrWhiteSpace(text)) text = raw;
                if (speaker is ISpeechSubmission queue)
                    submitted = queue.TrySpeakAsync(text);
                else
                {
                    speaker.SpeakAsync(text);
                    submitted = true;
                }
                return submitted;
            }
        }

        internal bool IsSubmitted
        {
            get { lock (gate) return submitted; }
        }
    }

    // Firmware identity proof is kept independently from #41's display proof.
    // It is scoped to one MAVLinkInterface generation and each source system.
    internal sealed class FirmwareSpeechSourceProof
    {
        private sealed class Proof
        {
            internal bool HasHeartbeat, HasVersion, Conflict;
            internal MAVLink.MAV_AUTOPILOT Autopilot;
            internal MAVLink.MAV_TYPE Vehicle;
            internal uint FlightVersion;
            internal byte[] FlightHash;
        }

        private readonly object gate = new object();
        private readonly Dictionary<uint, Proof> systems = new Dictionary<uint, Proof>();

        internal void Observe(uint sysid, byte compid, MAVLink.MAVLinkMessage packet)
        {
            if (compid != 1 || packet == null) return;
            lock (gate)
            {
                if (!systems.TryGetValue(sysid, out var proof))
                    systems.Add(sysid, proof = new Proof());

                if (packet.msgid == (uint)MAVLink.MAVLINK_MSG_ID.HEARTBEAT)
                {
                    var heartbeat = packet.ToStructure<MAVLink.mavlink_heartbeat_t>();
                    var autopilot = (MAVLink.MAV_AUTOPILOT)heartbeat.autopilot;
                    var vehicle = (MAVLink.MAV_TYPE)heartbeat.type;
                    if (proof.HasHeartbeat &&
                        (proof.Autopilot != autopilot || proof.Vehicle != vehicle)) proof.Conflict = true;
                    else
                    {
                        proof.Autopilot = autopilot;
                        proof.Vehicle = vehicle;
                        proof.HasHeartbeat = true;
                    }
                }
                else if (packet.msgid == (uint)MAVLink.MAVLINK_MSG_ID.AUTOPILOT_VERSION)
                {
                    var version = packet.ToStructure<MAVLink.mavlink_autopilot_version_t>();
                    var hash = version.flight_custom_version ?? Array.Empty<byte>();
                    if (proof.HasVersion &&
                        (proof.FlightVersion != version.flight_sw_version || !EqualBytes(proof.FlightHash, hash)))
                        proof.Conflict = true;
                    else
                    {
                        proof.FlightVersion = version.flight_sw_version;
                        proof.FlightHash = (byte[])hash.Clone();
                        proof.HasVersion = true;
                    }
                }
            }
        }

        internal bool HasConsistentIdentity(uint sysid)
        {
            lock (gate)
                return systems.TryGetValue(sysid, out var proof) && IsConsistent(proof);
        }

        private static bool IsConsistent(Proof proof) =>
            proof.HasHeartbeat && proof.HasVersion && !proof.Conflict && proof.FlightHash != null;

        internal string TranslateScript(uint sysid, byte severity, string completeRawText)
        {
            if (string.IsNullOrEmpty(completeRawText)) return null;
            lock (gate)
            {
                if (!systems.TryGetValue(sysid, out var proof) || !IsConsistent(proof)) return null;
                return FirmwareScriptSpeech.Translate(proof.Autopilot, proof.Vehicle,
                    proof.FlightVersion, (byte[])proof.FlightHash.Clone(), severity, completeRawText);
            }
        }

        private static bool EqualBytes(byte[] left, byte[] right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null || left.Length != right.Length) return false;
            for (int i = 0; i < left.Length; i++) if (left[i] != right[i]) return false;
            return true;
        }
    }

    // Mirrors ArduPilot/MissionPlanner's StatusTextAssembler at 1a361220. Keep
    // display/HUD/audio decisions downstream of complete logical STATUSTEXT.
    internal sealed class SpeechStatusTextAssembler
    {
        internal const int ChunkSize = 50;
        internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(1);
        private static readonly byte[] Gap = Encoding.UTF8.GetBytes(" ... ");

        private sealed class Pending
        {
            internal byte Severity;
            internal bool MixedSeverity;
            internal DateTime LastChunk;
            internal int Expected = -1;
            internal readonly SortedDictionary<byte, byte[]> Chunks = new SortedDictionary<byte, byte[]>();
        }

        private readonly Dictionary<(uint sysid, byte compid, ushort id), Pending> pending =
            new Dictionary<(uint sysid, byte compid, ushort id), Pending>();
        private volatile bool hasPending;
        internal bool HasPending => hasPending;

        internal string Add(uint sysid, byte compid, MAVLink.mavlink_statustext_t msg)
        {
            return Add(sysid, compid, msg, out _);
        }

        internal string Add(uint sysid, byte compid, MAVLink.mavlink_statustext_t msg, out bool consistent)
        {
            return Add(sysid, compid, msg, DateTime.UtcNow, out consistent);
        }

        internal string Add(uint sysid, byte compid, MAVLink.mavlink_statustext_t msg, DateTime now)
        {
            return Add(sysid, compid, msg, now, out _);
        }

        private string Add(uint sysid, byte compid, MAVLink.mavlink_statustext_t msg, DateTime now, out bool consistent)
        {
            byte[] text = TrimAtNull(msg.text);
            consistent = msg.id == 0;
            if (consistent) return Encoding.UTF8.GetString(text);

            lock (pending)
            {
                var key = (sysid, compid, msg.id);
                if (!pending.TryGetValue(key, out var item))
                    pending[key] = item = new Pending();
                if (item.Chunks.Count != 0 && item.Severity != msg.severity)
                    item.MixedSeverity = true;
                item.Severity = msg.severity;
                item.LastChunk = now;
                item.Chunks[msg.chunk_seq] = text;
                if (text.Length < ChunkSize) item.Expected = msg.chunk_seq + 1;
                if (item.Chunks.Count != item.Expected)
                {
                    hasPending = true;
                    return null;
                }
                pending.Remove(key);
                hasPending = pending.Count > 0;
                // The original assembled text and severity still feed the upstream route.
                consistent = !item.MixedSeverity && item.Chunks.Keys.First() == 0 &&
                    item.Chunks.Keys.Last() == item.Expected - 1;
                return Join(item);
            }
        }

        internal List<(uint sysid, byte compid, byte severity, string text)> TakeExpired()
        {
            return TakeExpired(DateTime.UtcNow);
        }

        internal List<(uint sysid, byte compid, byte severity, string text)> TakeExpired(DateTime now)
        {
            var expired = new List<(uint sysid, byte compid, byte severity, string text)>();
            lock (pending)
            {
                foreach (var item in pending.Where(a => now - a.Value.LastChunk > Timeout)
                             .OrderBy(a => a.Value.LastChunk).ToList())
                {
                    pending.Remove(item.Key);
                    expired.Add((item.Key.sysid, item.Key.compid, item.Value.Severity, Join(item.Value)));
                }
                hasPending = pending.Count > 0;
            }
            return expired;
        }

        private static string Join(Pending item)
        {
            using (var data = new MemoryStream())
            {
                int next = 0;
                foreach (var chunk in item.Chunks)
                {
                    if (chunk.Key != next) data.Write(Gap, 0, Gap.Length);
                    data.Write(chunk.Value, 0, chunk.Value.Length);
                    next = chunk.Key + 1;
                }
                return Encoding.UTF8.GetString(data.ToArray());
            }
        }

        private static byte[] TrimAtNull(byte[] text)
        {
            if (text == null) return new byte[0];
            int length = Array.IndexOf(text, (byte)0);
            if (length == -1) length = text.Length;
            var ans = new byte[length];
            Array.Copy(text, ans, length);
            return ans;
        }
    }

}
