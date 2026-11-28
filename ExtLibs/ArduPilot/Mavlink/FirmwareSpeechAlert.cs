using System;
using System.Collections.Generic;
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

        internal string TranslateScript(uint sysid, byte severity, string completeRawText)
        {
            if (string.IsNullOrEmpty(completeRawText)) return null;
            lock (gate)
            {
                if (!systems.TryGetValue(sysid, out var proof) || proof.Conflict ||
                    !proof.HasHeartbeat || !proof.HasVersion || proof.FlightHash == null) return null;
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

    // Speech-only STATUSTEXT reassembly. Display rows and logs remain the original
    // packet chunks; incomplete, reordered, oversized or invalid UTF-8 text is never translated.
    internal sealed class SpeechStatusTextAssembler
    {
        private struct Key : IEquatable<Key>
        {
            internal uint System;
            internal byte Component;
            internal ushort Id;
            public bool Equals(Key other) => System == other.System && Component == other.Component && Id == other.Id;
            public override bool Equals(object obj) => obj is Key other && Equals(other);
            public override int GetHashCode() => unchecked(((int)System * 397) ^ (Component << 16) ^ Id);
        }

        private sealed class Partial
        {
            internal byte Severity, NextSequence;
            internal DateTime Updated;
            internal readonly List<byte> Bytes = new List<byte>();
        }

        private const int MaxActive = 8;
        private const int MaxBytes = 2048;
        private const int MaxChunks = 40;
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private readonly object gate = new object();
        private readonly Dictionary<Key, Partial> partials = new Dictionary<Key, Partial>();

        internal bool TryAppend(uint system, byte component, MAVLink.mavlink_statustext_t packet,
            out string completeText)
        {
            completeText = null;
            var source = packet.text ?? Array.Empty<byte>();
            int end = Array.IndexOf(source, (byte)0);
            int length = end < 0 ? source.Length : end;
            if (length > 50) return false;

            lock (gate)
            {
                var now = DateTime.UtcNow;
                foreach (var expired in new List<Key>(partials.Keys))
                    if (now - partials[expired].Updated > TimeSpan.FromSeconds(2)) partials.Remove(expired);

                if (packet.id == 0)
                {
                    if (packet.chunk_seq != 0) return false;
                    try { completeText = StrictUtf8.GetString(source, 0, length); return true; }
                    catch (DecoderFallbackException) { return false; }
                }

                var key = new Key { System = system, Component = component, Id = packet.id };
                if (packet.chunk_seq == 0)
                {
                    partials.Remove(key);
                    if (partials.Count >= MaxActive)
                    {
                        Key oldest = default(Key);
                        DateTime time = DateTime.MaxValue;
                        foreach (var pair in partials)
                            if (pair.Value.Updated < time) { oldest = pair.Key; time = pair.Value.Updated; }
                        partials.Remove(oldest);
                    }
                    var current = new Partial { Severity = packet.severity, NextSequence = 1, Updated = now };
                    AddBytes(current.Bytes, source, length);
                    if (current.Bytes.Count > MaxBytes) return false;
                    if (length < source.Length)
                        return Decode(current, out completeText);
                    partials[key] = current;
                    return false;
                }

                if (!partials.TryGetValue(key, out var partial) || packet.chunk_seq != partial.NextSequence ||
                    packet.severity != partial.Severity || packet.chunk_seq >= MaxChunks)
                {
                    partials.Remove(key);
                    return false;
                }
                partial.Updated = now;
                if (partial.Bytes.Count + length > MaxBytes)
                {
                    partials.Remove(key);
                    return false;
                }
                AddBytes(partial.Bytes, source, length);
                partial.NextSequence++;
                if (length == source.Length) return false;
                partials.Remove(key);
                return Decode(partial, out completeText);
            }
        }

        private static bool Decode(Partial value, out string text)
        {
            try { text = StrictUtf8.GetString(value.Bytes.ToArray()); return true; }
            catch (DecoderFallbackException) { text = null; return false; }
        }

        private static void AddBytes(List<byte> target, byte[] source, int length)
        {
            for (int i = 0; i < length; i++) target.Add(source[i]);
        }
    }

}
