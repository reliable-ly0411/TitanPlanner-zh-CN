using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace MissionPlanner.Utilities
{
    /// <summary>A stable release identity for display metadata, never a parameter write target.</summary>
    public sealed class ParameterDescriptionContext
    {
        private static readonly Dictionary<string, string> Families = new Dictionary<string, string>
        {
            { "ArduCopter2", "Copter" }, { "ArduPlane", "Plane" }, { "ArduRover", "Rover" },
            { "ArduSub", "Sub" }, { "ArduTracker", "Tracker" }
        };
        private static readonly Dictionary<string, string> Names = new Dictionary<string, string>
        {
            { "Copter", "ArduCopter" }, { "Plane", "ArduPlane" }, { "Rover", "Rover" },
            { "Sub", "ArduSub" }, { "Tracker", "AntennaTracker" }
        };

        public string Family { get; private set; }
        public string VehicleName { get { return Names[Family]; } }
        public string Version { get; private set; }
        public string ReportedGitSha { get; private set; }
        public string Key { get { return Family + "/" + Version + "/" + ReportedGitSha; } }
        public Uri SourceUri { get { return new Uri("https://autotest.ardupilot.org/Parameters/versioned/" + Family + "/stable-" + Version + "/apm.pdef.xml"); } }
        public IList<Uri> SourceUris
        {
            get
            {
                var sources = new List<Uri> { SourceUri };
                // Every fallback still names the actual release. Never substitute the moving stable alias.
                var directory = Family == "Tracker" ? "AntennaTracker" : Family;
                sources.Add(new Uri("https://firmware.ardupilot.org/" + directory + "/stable-" + Version + "/__METADATA__/apm.pdef.xml"));
                return sources.AsReadOnly();
            }
        }

        public static ParameterDescriptionContext TryCreate(string firmware, string versionText, Version reportedVersion)
        {
            return TryCreateWithBuild(firmware, versionText, reportedVersion, null);
        }

        public static ParameterDescriptionContext TryCreateWithBuild(string firmware, string versionText,
            Version reportedVersion, byte[] flightCustomVersion)
        {
            string family;
            if (firmware == null || !Families.TryGetValue(firmware, out family)) return null;
            var hasReportedVersion = reportedVersion != null && reportedVersion.Build >= 0
                && (reportedVersion.Major != 0 || reportedVersion.Minor != 0 || reportedVersion.Build != 0);
            if (hasReportedVersion && reportedVersion.Revision >= 0 && reportedVersion.Revision != 255)
                return null;

            Version parsed;
            var gitSha = "";
            if (string.IsNullOrEmpty(versionText))
            {
                // Some vehicles only report the MAVLink version message. Its type must be explicitly official.
                if (!hasReportedVersion || reportedVersion.Revision != 255) return null;
                parsed = reportedVersion;
            }
            else
            {
                // A development, prerelease or custom banner may not be promoted to an exact stable release.
                var match = Regex.Match(versionText, @"^(ArduCopter|ArduPlane|ArduRover|APM:Rover|Rover|ArduSub|AntennaTracker) V([0-9]+\.[0-9]+\.[0-9]+)(?: \(([0-9a-fA-F]{7,40})\))?$");
                if (!match.Success) return null;
                var reportedName = match.Groups[1].Value;
                if (reportedName != Names[family]
                    && !(family == "Rover" && (reportedName == "ArduRover" || reportedName == "APM:Rover"))) return null;
                if (!System.Version.TryParse(match.Groups[2].Value, out parsed)) return null;
                if (hasReportedVersion && (reportedVersion.Major != parsed.Major
                    || reportedVersion.Minor != parsed.Minor || reportedVersion.Build != parsed.Build)) return null;
                gitSha = match.Groups[3].Value.ToLowerInvariant();
            }
            if (flightCustomVersion != null)
            {
                // ArduPilot sends the abbreviated source hash as ASCII in AUTOPILOT_VERSION.
                var packetHash = Encoding.ASCII.GetString(flightCustomVersion).TrimEnd('\0').ToLowerInvariant();
                if (packetHash.Length > 0)
                {
                    if (!Regex.IsMatch(packetHash, @"\A[0-9a-f]{7,8}\z")) return null;
                    if (!gitSha.StartsWith(packetHash, StringComparison.Ordinal)
                        && !packetHash.StartsWith(gitSha, StringComparison.Ordinal)) return null;
                    if (packetHash.Length > gitSha.Length) gitSha = packetHash;
                }
            }
            return new ParameterDescriptionContext { Family = family, Version = parsed.ToString(3), ReportedGitSha = gitSha };
        }
    }

    public sealed class ParameterDescriptionChoice
    {
        public string Code { get; internal set; }
        public string English { get; internal set; }
    }

    public sealed class ParameterDescriptionRecord
    {
        public string Name { get; internal set; }
        public string English { get; internal set; }
        public string Units { get; internal set; }
        public string Range { get; internal set; }
        public bool Ambiguous { get; internal set; }
        public string Fingerprint { get; internal set; }
        public IList<ParameterDescriptionChoice> Values { get; internal set; }
        public IList<ParameterDescriptionChoice> Bits { get; internal set; }
    }

    public sealed class ParameterDescriptionSnapshot
    {
        private readonly Dictionary<string, ParameterDescriptionRecord> records;
        public ParameterDescriptionContext Context { get; private set; }
        public string SourceSha256 { get; private set; }
        public string GitSha { get; private set; }
        public string ReleaseTag { get; private set; }
        public Uri SourceUri { get; internal set; }
        public bool HasReleaseProvenance { get { return !string.IsNullOrEmpty(ReleaseTag) && !string.IsNullOrEmpty(GitSha); } }
        public bool Partial { get; private set; }
        public bool MatchesReportedBuild { get; private set; }
        internal bool IsReconstructed { get; set; }
        public bool MatchesFirmware { get { return IsReconstructed || MatchesReportedBuild; } }
        public string Origin { get; internal set; }
        public int Count { get { return records.Count; } }

        private ParameterDescriptionSnapshot(ParameterDescriptionContext context, Dictionary<string, ParameterDescriptionRecord> records)
        {
            Context = context;
            this.records = records;
        }

        internal static XDocument ReadXml(byte[] bytes, bool embedded = false)
        {
            // The embedded registry holds many releases. Network data remains limited per document.
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = (embedded ? 64 : 24) * 1024 * 1024
            };
            using (var stream = new MemoryStream(bytes))
            using (var reader = XmlReader.Create(stream, settings)) return XDocument.Load(reader);
        }

        public static ParameterDescriptionSnapshot Parse(byte[] bytes, ParameterDescriptionContext context, bool partial = false, string sourceSha = null, string bundledGitSha = null)
        {
            if (context == null) throw new ArgumentNullException("context");
            var doc = ReadXml(bytes);
            return Parse(doc.Root, doc.Nodes().OfType<XComment>(), context, context.VehicleName, partial,
                sourceSha ?? ParameterDescriptionCatalog.Hash(bytes), bundledGitSha);
        }

        internal static ParameterDescriptionSnapshot Parse(XElement root, IEnumerable<XComment> comments,
            ParameterDescriptionContext context, string vehicleName, bool partial, string sourceSha, string bundledGitSha)
        {
            if (root == null || root.Name != "paramfile") throw new InvalidDataException("Expected ArduPilot paramfile metadata.");
            var group = root.Element("vehicles");
            if (group == null || !group.Elements("parameters").Any(p => (string)p.Attribute("name") == vehicleName))
                throw new InvalidDataException("Parameter metadata belongs to another vehicle.");
            var firmware = root.Element("firmware");
            var gitSha = (string)firmware?.Attribute("git_sha") ?? bundledGitSha ?? "";
            var gitTag = (string)firmware?.Attribute("git_tag") ?? "";
            foreach (var comment in comments)
            {
                var provenance = Regex.Match(comment.Value, @"Generated from git tag (\S+) \(([0-9a-fA-F]{7,40})\)");
                if (provenance.Success) { gitTag = provenance.Groups[1].Value; gitSha = provenance.Groups[2].Value; }
            }
            if (gitTag.Length > 0 && gitTag != context.Family + "-" + context.Version && !(context.Family == "Sub" && gitTag == "ArduSub-" + context.Version))
                throw new InvalidDataException("Parameter metadata belongs to another release.");
            if (gitSha.Length > 0 && !Regex.IsMatch(gitSha, "^[0-9a-fA-F]{7,40}$")) throw new InvalidDataException("Invalid metadata source commit.");
            var records = new Dictionary<string, ParameterDescriptionRecord>(StringComparer.Ordinal);
            var definitions = new Dictionary<string, List<XElement>>(StringComparer.Ordinal);
            var vehicleGroups = group.Elements("parameters").Where(p => (string)p.Attribute("name") == vehicleName);
            var libraryGroups = root.Elements("libraries").Elements("parameters");
            foreach (var p in vehicleGroups.Concat(libraryGroups).Elements("param"))
            {
                var name = (string)p.Attribute("name") ?? "";
                if (name.Contains(":"))
                {
                    if (!name.StartsWith(vehicleName + ":", StringComparison.Ordinal)) continue;
                    name = name.Substring(vehicleName.Length + 1);
                }
                if (name.Length == 0) continue;
                List<XElement> entries;
                if (!definitions.TryGetValue(name, out entries)) definitions[name] = entries = new List<XElement>();
                entries.Add(p);
            }
            foreach (var definition in definitions)
            {
                var nodes = definition.Value;
                var p = nodes[0];
                // Some official files contain several backend/frame definitions with the same name.
                // Preserve their text/ranges and do not bless the first definition with a Chinese explanation.
                var ambiguous = nodes.Select(DefinitionSignature).Distinct(StringComparer.Ordinal).Count() > 1;
                Func<string, string> field = key => string.Join(" / ", nodes.Select(n => n.Elements("field").FirstOrDefault(f => (string)f.Attribute("name") == key)?.Value ?? "").Where(s => s.Length > 0).Distinct(StringComparer.Ordinal));
                records[definition.Key] = new ParameterDescriptionRecord
                {
                    Name = definition.Key, Ambiguous = ambiguous,
                    Fingerprint = ParameterDescriptionCatalog.Hash(Encoding.UTF8.GetBytes(string.Join("\u001d", nodes.Select(DefinitionSignature).Distinct(StringComparer.Ordinal).OrderBy(s => s, StringComparer.Ordinal)))),
                    English = string.Join("\n\n", nodes.Select(n => (string)n.Attribute("documentation") ?? "").Distinct(StringComparer.Ordinal)),
                    Units = field("Units"), Range = field("Range"),
                    Values = Choices(nodes.SelectMany(n => n.Elements("values").Elements("value"))),
                    Bits = Choices(nodes.SelectMany(n => n.Elements("bitmask").Elements("bit")))
                };
                if (records[definition.Key].Bits.Count == 0)
                {
                    var rawBits = field("Bitmask");
                    records[definition.Key].Bits = rawBits.Split(',').Select(s => s.Split(new[] { ':' }, 2)).Where(a => a.Length == 2)
                        .Select(a => new ParameterDescriptionChoice { Code = a[0].Trim(), English = a[1].Trim() }).ToList().AsReadOnly();
                }
            }
            return new ParameterDescriptionSnapshot(context, records)
            {
                Partial = partial, GitSha = gitSha, ReleaseTag = gitTag, SourceUri = context.SourceUri,
                SourceSha256 = sourceSha,
                MatchesReportedBuild = context.ReportedGitSha.Length == 0 || (gitSha.Length >= context.ReportedGitSha.Length && gitSha.StartsWith(context.ReportedGitSha, StringComparison.OrdinalIgnoreCase))
            };
        }

        private static IList<ParameterDescriptionChoice> Choices(IEnumerable<XElement> nodes)
        {
            return nodes.Select(n => new ParameterDescriptionChoice { Code = (string)n.Attribute("code") ?? "", English = n.Value })
                .GroupBy(c => c.Code + "\0" + c.English, StringComparer.Ordinal).Select(g => g.First()).ToList().AsReadOnly();
        }

        private static string DefinitionSignature(XElement p)
        {
            return string.Join("\0", p.Attributes().OrderBy(a => a.Name.LocalName, StringComparer.Ordinal).Select(a => "attr:" + a.Name.LocalName + "\u001f" + a.Value)
                .Concat(p.Elements("field").OrderBy(f => (string)f.Attribute("name"), StringComparer.Ordinal).Select(f => "field:" + (string)f.Attribute("name") + "\u001f" + f.Value))
                .Concat(p.Elements("values").Elements("value").Select(v => "value:" + (string)v.Attribute("code") + "\u001f" + v.Value))
                .Concat(p.Elements("bitmask").Elements("bit").Select(v => "bit:" + (string)v.Attribute("code") + "\u001f" + v.Value)));
        }

        public ParameterDescriptionRecord Find(string name)
        {
            ParameterDescriptionRecord record;
            return records.TryGetValue(name, out record) ? record : null;
        }
    }

    public sealed class ParameterDescriptionText
    {
        public ParameterDescriptionText(string description, string units, string options, string source)
        {
            Description = description; Units = units; Options = options; Source = source;
        }
        internal ParameterDescriptionText() { }
        public string Description { get; internal set; }
        public string Units { get; internal set; }
        public string Options { get; internal set; }
        public string Source { get; internal set; }
        public string SearchText { get { return Description + "\n" + Options; } }
    }

    /// <summary>Independent, source-fingerprinted Chinese display notes; no mutation of raw metadata.</summary>
    public sealed class ParameterDescriptionCatalog
    {
        private readonly Dictionary<string, XElement[]> translations;
        public ParameterDescriptionCatalog(byte[] xml)
        {
            var doc = ParameterDescriptionSnapshot.ReadXml(xml, embedded: true);
            if (doc.Root == null || doc.Root.Name != "parameterTranslations") throw new InvalidDataException("Invalid parameter translation catalog.");
            translations = doc.Root.Elements("translation").GroupBy(t => ScopeKey((string)t.Attribute("family"), (string)t.Attribute("parameter"),
                (string)t.Attribute("field"), (string)t.Attribute("code")), StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.Ordinal);
        }

        private static string ScopeKey(string family, string name, string field, string code)
        {
            return family + "\0" + name + "\0" + field + "\0" + (code ?? "");
        }

        public static string Hash(byte[] bytes)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }

        public string Chinese(ParameterDescriptionContext context, string name, string field, string code, string english, out bool needsReview, string recordFingerprint = null)
        {
            needsReview = false;
            if (context == null) return null;
            XElement[] scoped;
            if (!translations.TryGetValue(ScopeKey(context.Family, name, field, code), out scoped)) return null;
            var candidates = scoped.Where(t => ((string)t.Attribute("versions") ?? "").Split(',').Contains(context.Version)).ToArray();
            if (candidates.Length == 0) return null;
            var fingerprint = Hash(Encoding.UTF8.GetBytes(english ?? ""));
            var exact = candidates.FirstOrDefault(t => (string)t.Attribute("englishSha256") == fingerprint
                && (recordFingerprint == null || (string)t.Attribute("recordSha256") == recordFingerprint));
            needsReview = exact == null && candidates.Length > 0;
            return exact?.Element("chinese")?.Value;
        }

        public string ChoiceLabel(ParameterDescriptionContext context, string name, string field, string code, string english, bool chinese, string recordFingerprint = null)
        {
            if (!chinese) return english;
            bool review;
            var zh = Chinese(context, name, field, code, english, out review, recordFingerprint);
            return english + (zh != null ? " / " + zh : review ? " / [中文待复核]" : "");
        }

        public ParameterDescriptionText Present(ParameterDescriptionSnapshot snapshot, string name, bool chinese)
        {
            var record = snapshot?.Find(name);
            if (record == null || !snapshot.MatchesFirmware) return null;
            var desc = record.English;
            if (chinese && record.Ambiguous) desc += "\n\n[官方含多个定义，需核对适用机型或后端]";
            else if (chinese)
            {
                bool review;
                var zh = Chinese(snapshot.Context, name, "Description", null, record.English, out review, record.Fingerprint);
                if (zh != null) desc += "\n\n中文说明：\n" + zh;
                else if (review) desc += "\n\n[中文说明待复核：英文或关联字段已变化]";
                else desc += "\n\n[暂无已审阅的中文说明，保留英文]";
            }
            var values = record.Values.Select(c => c.Code + ": " + ChoiceLabel(snapshot.Context, name, "Value", c.Code, c.English, chinese && !record.Ambiguous, record.Fingerprint));
            var bits = record.Bits.Select(c => "bit " + c.Code + ": " + ChoiceLabel(snapshot.Context, name, "Bitmask", c.Code, c.English, chinese && !record.Ambiguous, record.Fingerprint));
            return new ParameterDescriptionText
            {
                Description = desc, Units = record.Units,
                Options = string.Join("\n", new[] { record.Range }.Concat(values).Concat(bits).Where(s => s.Length > 0)),
                Source = snapshot.Context.Family + " " + snapshot.Context.Version + (snapshot.Partial ? " (offline subset)" : "") + " [" + snapshot.Origin + "]\n"
                    + snapshot.SourceUri + "\nSHA-256: " + snapshot.SourceSha256
            };
        }
    }

    /// <summary>Version-scoped, validated disk cache. Network failure keeps the previous good file.</summary>
    public sealed class ParameterDescriptionStore
    {
        private readonly string directory;
        private readonly Lazy<XElement> bundle;
        private readonly Func<Uri, CancellationToken, Task<byte[]>> fetch;
        private readonly Func<DateTime> utcNow;
        private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
        private static readonly HttpClient Client = new HttpClient();

        public ParameterDescriptionStore(string directory, byte[] bundle, Func<Uri, CancellationToken, Task<byte[]>> fetch = null, Func<DateTime> utcNow = null)
        {
            this.directory = directory;
            this.bundle = new Lazy<XElement>(() => bundle == null ? null : ParameterDescriptionSnapshot.ReadXml(bundle, embedded: true).Root);
            this.fetch = fetch ?? Download;
            this.utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        private static async Task<byte[]> Download(Uri uri, CancellationToken token)
        {
            using (var response = await Client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var output = new MemoryStream())
                {
                    var buffer = new byte[81920];
                    int count;
                    while ((count = await input.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false)) > 0)
                    {
                        if (output.Length + count > 24 * 1024 * 1024) throw new InvalidDataException("Parameter metadata is too large.");
                        output.Write(buffer, 0, count);
                    }
                    return output.ToArray();
                }
            }
        }

        private XElement FindRelease(ParameterDescriptionContext context)
        {
            return context == null ? null : bundle.Value?.Elements("release").FirstOrDefault(r =>
                (string)r.Attribute("family") == context.Family && (string)r.Attribute("version") == context.Version);
        }

        public ParameterDescriptionContext IdentifyFirmware(string firmware, string banner, Version version, byte[] reportedBuild)
        {
            var exact = ParameterDescriptionContext.TryCreateWithBuild(firmware, banner, version, reportedBuild);
            if (exact != null || reportedBuild == null) return exact;

            // Historical reconstructions use the consensus on the official version even if build information is unavailable.
            var versionOnly = ParameterDescriptionContext.TryCreate(firmware, banner, version);
            return (string)FindRelease(versionOnly)?.Attribute("sourceKind") == "reconstructed" ? versionOnly : null;
        }

        public ParameterDescriptionSnapshot Bundled(ParameterDescriptionContext context)
        {
            return ReadBundled(context, FindRelease(context));
        }

        private static ParameterDescriptionSnapshot ReadBundled(ParameterDescriptionContext context, XElement release)
        {
            var paramfile = release?.Element("paramfile");
            if (paramfile == null) return null;
            var sourceUri = new Uri((string)release.Attribute("sourceUrl"));
            var digest = (string)release.Attribute("sourceSha256");
            var gitSha = (string)release.Attribute("gitSha");
            var sourceKind = (string)release.Attribute("sourceKind");
            var vehicleName = context.VehicleName;

            if (sourceKind == "reconstructed")
            {
                const string archive = "https://codeload.github.com/ArduPilot/ardupilot/tar.gz/";
                if (!Regex.IsMatch(gitSha ?? "", "^[0-9a-f]{40}$") || sourceUri.AbsoluteUri != archive + gitSha
                    || !Regex.IsMatch(digest ?? "", "^[0-9a-f]{64}$") || paramfile.Element("firmware") != null)
                    throw new InvalidDataException("Invalid reconstructed metadata identity.");

                // The reconstruction commit identifies its source. The firmware itself is selected by its stable version.
                if (context.Family == "Rover" && paramfile.Element("vehicles")?.Elements("parameters")
                    .Any(p => (string)p.Attribute("name") == "APMrover2") == true)
                    vehicleName = "APMrover2";
            }
            else if (sourceKind != null || !context.SourceUris.Contains(sourceUri))
            {
                throw new InvalidDataException("Unrecognized bundled metadata source.");
            }

            var snapshot = ParameterDescriptionSnapshot.Parse(paramfile, Enumerable.Empty<XComment>(),
                context, vehicleName, sourceKind == null, digest, gitSha);
            snapshot.SourceUri = sourceUri;
            snapshot.IsReconstructed = sourceKind == "reconstructed";
            snapshot.Origin = sourceKind ?? "bundled";
            return snapshot;
        }

        public async Task<ParameterDescriptionSnapshot> LoadAsync(ParameterDescriptionContext context)
        {
            if (context == null) return null;
            var release = FindRelease(context);
            if ((string)release?.Attribute("sourceKind") == "reconstructed") return ReadBundled(context, release);
            await gate.WaitAsync().ConfigureAwait(false);
            try
            {
                var releaseDirectory = Path.Combine(directory, context.Family, context.Version);
                var pointer = Path.Combine(releaseDirectory, "current.txt");
                ParameterDescriptionSnapshot cached = null;
                ParameterDescriptionSnapshot mismatched = null;
                try
                {
                    if (File.Exists(pointer))
                    {
                        var identity = File.ReadAllLines(pointer);
                        Uri sourceUri;
                        if (identity.Length != 2 || !Regex.IsMatch(identity[0], "^[0-9a-f]{64}$") || !Uri.TryCreate(identity[1], UriKind.Absolute, out sourceUri) || !context.SourceUris.Contains(sourceUri))
                            throw new InvalidDataException("Cache source identity is invalid.");
                        var bytes = File.ReadAllBytes(Path.Combine(releaseDirectory, identity[0] + ".apm.pdef.xml"));
                        if (ParameterDescriptionCatalog.Hash(bytes) != identity[0]) throw new InvalidDataException("Cache fingerprint does not match.");
                        cached = ParseValidated(bytes, context, sourceUri);
                        cached.Origin = "cache";
                    }
                    if (cached != null && cached.MatchesReportedBuild && File.GetLastWriteTimeUtc(pointer).AddDays(7) > utcNow()) return cached;
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is XmlException || ex is InvalidDataException) { }
                foreach (var sourceUri in context.SourceUris)
                {
                    try
                    {
                        byte[] bytes;
                        using (var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(8))) bytes = await fetch(sourceUri, cancel.Token).ConfigureAwait(false);
                        // Validate the complete document before replacing anything on disk.
                        var snapshot = ParseValidated(bytes, context, sourceUri);
                        snapshot.Origin = "network";
                        if (!snapshot.MatchesReportedBuild) { mismatched = snapshot; continue; }
                        try
                        {
                            Directory.CreateDirectory(releaseDirectory);
                            // Retain each valid source revision under its own digest. Advance the pointer last.
                            WriteAtomic(Path.Combine(releaseDirectory, snapshot.SourceSha256 + ".apm.pdef.xml"), bytes);
                            WriteAtomic(pointer, Encoding.UTF8.GetBytes(snapshot.SourceSha256 + "\n" + sourceUri.AbsoluteUri));
                            File.SetLastWriteTimeUtc(pointer, utcNow());
                        }
                        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) { }
                        return snapshot;
                    }
                    catch (Exception ex) when (ex is HttpRequestException || ex is OperationCanceledException || ex is IOException || ex is UnauthorizedAccessException || ex is XmlException || ex is InvalidDataException) { }
                }
                if (cached != null) cached.Origin = "cache; refresh unavailable";
                var bundled = ReadBundled(context, release);
                if (cached != null && cached.MatchesReportedBuild) return cached;
                if (bundled != null && bundled.MatchesReportedBuild) return bundled;
                return mismatched ?? cached ?? bundled;
            }
            finally { gate.Release(); }
        }

        private static void WriteAtomic(string path, byte[] bytes)
        {
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".new";
            try
            {
                File.WriteAllBytes(temporary, bytes);
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
            finally { try { File.Delete(temporary); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
        }

        private ParameterDescriptionSnapshot ParseValidated(byte[] bytes, ParameterDescriptionContext context, Uri sourceUri)
        {
            // Older official files omit the commit SHA. Only supplement it when their complete bytes
            // match a bundled, pinned source digest whose release tag was independently resolved.
            var pinned = FindRelease(context);
            var digest = ParameterDescriptionCatalog.Hash(bytes);
            var snapshot = ParameterDescriptionSnapshot.Parse(bytes, context, false, digest,
                (string)pinned?.Attribute("sourceSha256") == digest ? (string)pinned.Attribute("gitSha") : null);
            // The firmware-server fallback must identify its release/build in the document itself.
            // A version-looking URL alone is insufficient, including when a server redirects it.
            if (sourceUri != context.SourceUri && !snapshot.HasReleaseProvenance)
                throw new InvalidDataException("Fallback metadata lacks release provenance.");
            snapshot.SourceUri = sourceUri;
            return snapshot;
        }
    }
}
