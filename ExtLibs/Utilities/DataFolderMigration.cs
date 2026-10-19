using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using log4net;

namespace MissionPlanner.Utilities
{
    /// <summary>
    /// First-run copy of settings and data from the folders this app's data may still be in
    /// (<see cref="Settings.PreviousAppConfigNames"/>, e.g. "Mission Planner") into this build's folders
    /// (<see cref="Settings.AppConfigName"/>).
    ///
    /// Sources are copied in order and a file that already exists in the new folders is never overwritten,
    /// so when several sources have the same file the earliest one wins and later ones only fill in what is
    /// missing. Nothing is moved or deleted, so another program still using an old folder keeps working.
    /// Settings are copied before startup continues; everything else (logs, map tiles, offline maps,
    /// elevation data, caches) copies on a background thread. A marker file in each new folder records that
    /// the copy finished; until it exists, every launch resumes where the last one stopped.
    /// </summary>
    public static class DataFolderMigration
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private const string MarkerName = ".migration-complete";
        private const string TempSuffix = ".migrating";
        private const long FreeSpaceMarginBytes = 512L * 1024 * 1024;

        private class FolderPair
        {
            /// <summary>Existing source folders, highest priority first.</summary>
            public List<string> From = new List<string>();
            public string To;
            public bool IsUserFolder;
        }

        /// <summary>
        /// Copies the old user folders' top-level files (config.xml, configuration profiles) and the
        /// offline-maps list. Call before anything reads <see cref="Settings.Instance"/>.
        /// </summary>
        public static void CopySettings()
        {
            try
            {
                foreach (var pair in PendingPairs())
                {
                    Directory.CreateDirectory(pair.To);

                    foreach (var from in pair.From)
                    {
                        if (pair.IsUserFolder)
                        {
                            foreach (var file in Directory.GetFiles(from))
                                CopyIfMissing(file, Path.Combine(pair.To, Path.GetFileName(file)));
                        }

                        // tiny, and has to arrive before the map writes a new one
                        var offlineMaps = Path.Combine(from, "gmapcache", "offlinemaps.json");
                        if (File.Exists(offlineMaps))
                            CopyIfMissing(offlineMaps, Path.Combine(pair.To, "gmapcache", "offlinemaps.json"));
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Copying settings from the previous data folder failed", ex);
            }
        }

        /// <summary>Copies everything else on a low-priority background thread.</summary>
        public static void StartBackgroundCopy()
        {
            List<FolderPair> pending;
            try
            {
                pending = PendingPairs();
            }
            catch (Exception ex)
            {
                log.Error(ex);
                return;
            }

            if (pending.Count == 0)
                return;

            new Thread(() => CopyAll(pending))
            {
                IsBackground = true,
                Name = "Data folder migration",
                Priority = ThreadPriority.BelowNormal
            }.Start();
        }

        /// <summary>New folders whose copy has not finished yet, each with its existing sources.</summary>
        private static List<FolderPair> PendingPairs()
        {
            var current = Settings.AppConfigName;
            var previous = (Settings.PreviousAppConfigNames ?? new string[0])
                .Where(n => !string.IsNullOrEmpty(n) && n != current)
                .Distinct()
                .ToList();
            if (previous.Count == 0)
                return new List<FolderPair>();

            var user = new FolderPair { To = Trim(Settings.GetUserDataDirectory()), IsUserFolder = true };
            var data = new FolderPair { To = Trim(Settings.GetDataDirectory()) };
            // on Mono the user and data folders are the same
            bool sameFolder = string.Equals(user.To, data.To, StringComparison.OrdinalIgnoreCase);

            try
            {
                foreach (var name in previous)
                {
                    Settings.AppConfigName = name;
                    var oldUser = Trim(Settings.GetUserDataDirectory());
                    var oldData = Trim(Settings.GetDataDirectory());
                    if (Directory.Exists(oldUser))
                        user.From.Add(oldUser);
                    if (!sameFolder && Directory.Exists(oldData))
                        data.From.Add(oldData);
                }
            }
            finally
            {
                Settings.AppConfigName = current;
            }

            var pairs = sameFolder ? new List<FolderPair> { user } : new List<FolderPair> { user, data };
            return pairs.Where(p => p.From.Count > 0 && !File.Exists(Path.Combine(p.To, MarkerName))).ToList();
        }

        private static void CopyAll(List<FolderPair> pairs)
        {
            var sources = string.Join("\n", pairs.SelectMany(p => p.From));
            try
            {
                var todo = new List<KeyValuePair<string, string>>();
                var queued = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var bytesByRoot = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

                foreach (var pair in pairs)
                {
                    foreach (var from in pair.From)
                    {
                        foreach (var src in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
                        {
                            if (src.EndsWith(TempSuffix, StringComparison.OrdinalIgnoreCase) ||
                                Path.GetFileName(src) == MarkerName)
                                continue;
                            var dst = Path.Combine(pair.To, src.Substring(from.Length).TrimStart('\\', '/'));
                            // an earlier source already provides this file
                            if (File.Exists(dst) || !queued.Add(dst))
                                continue;

                            long size = 0;
                            try
                            {
                                size = new FileInfo(src).Length;
                            }
                            catch
                            {
                            }

                            todo.Add(new KeyValuePair<string, string>(src, dst));
                            var root = Path.GetPathRoot(pair.To) ?? "";
                            bytesByRoot[root] = (bytesByRoot.TryGetValue(root, out var b) ? b : 0) + size;
                        }
                    }
                }

                foreach (var kv in bytesByRoot)
                {
                    long free;
                    try
                    {
                        free = new DriveInfo(kv.Key).AvailableFreeSpace;
                    }
                    catch
                    {
                        continue; // unknown drive type: try anyway
                    }

                    if (free < kv.Value + FreeSpaceMarginBytes)
                    {
                        var msg = string.Format(
                            "Not enough free disk space to copy your data into the \"{0}\" folders ({1:0.0} GB needed, {2:0.0} GB free).\n\n" +
                            "Free up space and restart to finish the copy. Your old data is untouched in:\n{3}",
                            Settings.AppConfigName, kv.Value / 1073741824.0, free / 1073741824.0, sources);
                        log.Warn(msg);
                        CustomMessageBox.Show(msg, "Data migration");
                        return; // no marker: retried on the next launch
                    }
                }

                log.InfoFormat("Data folder migration: copying {0} files ({1:0.0} MB)", todo.Count,
                    bytesByRoot.Values.Sum() / 1048576.0);

                int failed = 0, done = 0;
                foreach (var item in todo)
                {
                    try
                    {
                        CopyIfMissing(item.Key, item.Value);
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        log.Debug("Data folder migration: could not copy " + item.Key, ex);
                    }

                    if (++done % 1000 == 0)
                        log.InfoFormat("Data folder migration: {0} / {1}", done, todo.Count);
                }

                if (failed > 0)
                {
                    log.WarnFormat("Data folder migration: {0} file(s) could not be copied; retrying on the next launch", failed);
                    return;
                }

                foreach (var pair in pairs)
                    File.WriteAllText(Path.Combine(pair.To, MarkerName),
                        "Copied from " + string.Join(", ", pair.From) + " on " + DateTime.Now.ToString("u") + Environment.NewLine);
                log.Info("Data folder migration complete");
            }
            catch (Exception ex)
            {
                log.Error("Data folder migration failed; retrying on the next launch", ex);
            }
        }

        /// <summary>Copies via a temporary name so an interrupted copy never leaves a truncated file behind.</summary>
        private static void CopyIfMissing(string src, string dst)
        {
            if (File.Exists(dst))
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(dst));
            var tmp = dst + TempSuffix;
            File.Copy(src, tmp, true);
            try
            {
                File.Move(tmp, dst);
            }
            catch (IOException) when (File.Exists(dst))
            {
                // the app created it meanwhile; keep the newer one
                File.Delete(tmp);
            }
        }

        private static string Trim(string path)
        {
            return path.TrimEnd('\\', '/');
        }
    }
}
