using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace MissionPlanner.Utilities
{
    /// <summary>
    /// This class loads and saves some handy app level settings so UI state is preserved across sessions.
    /// </summary>
    public class Settings
    {
        static Settings _instance;

        public static string AppConfigName { get; set; } = "TitanPlanner";

        /// <summary>
        /// Folder names this app's data may still be in, highest priority first. On first launch their settings
        /// and data are copied into the <see cref="AppConfigName"/> folders (see <see cref="DataFolderMigration"/>);
        /// a file from an earlier name wins over the same file from a later one. Empty = nothing to migrate.
        /// </summary>
        public static string[] PreviousAppConfigNames { get; set; } = { "Mission Planner" };

        public static Settings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Settings();
                    try
                    {
                        _instance.Load();
                    } catch { }
                }
                return _instance;
            }
        }

        public Settings()
        {
        }

        /// <summary>
        /// use to store all internal config - use Instance
        /// </summary>
        public static Dictionary<string, string> config = new Dictionary<string, string>();

        public static string FileName { get; set; } = "config.xml";

        // Current config profile name (stored separately from the config itself)
        private static string _currentConfigName = null;
        public static string CurrentConfigName
        {
            get
            {
                if (_currentConfigName == null)
                {
                    _currentConfigName = LoadCurrentConfigName();
                }
                return _currentConfigName;
            }
            private set
            {
                _currentConfigName = value;
                SaveCurrentConfigName(value);
            }
        }

        private static string GetCurrentConfigNameFilePath()
        {
            return Path.Combine(GetUserDataDirectory(), "profile.txt");
        }

        private static string LoadCurrentConfigName()
        {
            try
            {
                var filePath = GetCurrentConfigNameFilePath();
                if (File.Exists(filePath))
                {
                    var name = File.ReadAllText(filePath).Trim();
                    if (!string.IsNullOrEmpty(name))
                        return name;
                }
            }
            catch { }
            return "Default";
        }

        private static void SaveCurrentConfigName(string name)
        {
            try
            {
                var filePath = GetCurrentConfigNameFilePath();
                var dir = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(filePath, name ?? "Default");
            }
            catch { }
        }

        /// <summary>
        /// Get list of available config profiles
        /// </summary>
        public static List<string> GetAvailableConfigs()
        {
            var configs = new List<string> { "Default" };
            try
            {
                var dir = GetUserDataDirectory();
                if (Directory.Exists(dir))
                {
                    var files = Directory.GetFiles(dir, "config_*.xml");
                    foreach (var file in files)
                    {
                        var name = Path.GetFileNameWithoutExtension(file);
                        if (name.StartsWith("config_"))
                        {
                            configs.Add(name.Substring(7)); // Remove "config_" prefix
                        }
                    }
                }
            }
            catch { }
            return configs;
        }

        /// <summary>
        /// Validate config name - only A-Z, 0-9, underscore allowed
        /// </summary>
        public static bool IsValidConfigName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            foreach (char c in name)
            {
                if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_'))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Load a specific config profile
        /// </summary>
        public bool LoadConfig(string configName)
        {
            try
            {
                string filename;
                if (configName == "Default")
                {
                    filename = Path.Combine(GetUserDataDirectory(), "config.xml");
                }
                else
                {
                    filename = Path.Combine(GetUserDataDirectory(), $"config_{configName}.xml");
                }

                if (!File.Exists(filename))
                    return false;

                // Clear current config
                config.Clear();

                // Load defaults first
                try
                {
                    if (File.Exists(GetConfigDefaultsFullPath()))
                        using (XmlTextReader xmlreader = new XmlTextReader(GetConfigDefaultsFullPath()))
                        {
                            while (xmlreader.Read())
                            {
                                if (xmlreader.NodeType == XmlNodeType.Element)
                                {
                                    try
                                    {
                                        switch (xmlreader.Name)
                                        {
                                            case "Config":
                                            case "xml":
                                                break;
                                            default:
                                                var key = xmlreader.Name;
                                                if (key.Contains("____"))
                                                    key = key.Replace("____", "/");
                                                config[key] = xmlreader.ReadString();
                                                break;
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }
                }
                catch { }

                // Load the config
                using (XmlTextReader xmlreader = new XmlTextReader(filename))
                {
                    while (xmlreader.Read())
                    {
                        if (xmlreader.NodeType == XmlNodeType.Element)
                        {
                            try
                            {
                                switch (xmlreader.Name)
                                {
                                    case "Config":
                                    case "xml":
                                        break;
                                    default:
                                        config[xmlreader.Name] = xmlreader.ReadString();
                                        break;
                                }
                            }
                            catch { }
                        }
                    }
                }

                CurrentConfigName = configName;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Save current config as a new profile
        /// </summary>
        public bool SaveConfigAs(string configName)
        {
            if (!IsValidConfigName(configName) && configName != "Default")
                return false;

            try
            {
                string filename;
                if (configName == "Default")
                {
                    filename = Path.Combine(GetUserDataDirectory(), "config.xml");
                }
                else
                {
                    filename = Path.Combine(GetUserDataDirectory(), $"config_{configName}.xml");
                }

                using (XmlTextWriter xmlwriter = new XmlTextWriter(filename, Encoding.UTF8))
                {
                    xmlwriter.Formatting = Formatting.Indented;
                    xmlwriter.WriteStartDocument();
                    xmlwriter.WriteStartElement("Config");

                    foreach (string key2 in config.Keys.OrderBy(a => a))
                    {
                        var key = key2;
                        try
                        {
                            if (key.Contains("/"))
                                key = key.Replace("/", "____");

                            if (key == "" || key.Contains("/") || key.Contains(" ")
                                || key.Contains("-") || key.Contains(":")
                                || key.Contains(";") || key.Contains("@")
                                || key.Contains("!") || key.Contains("#")
                                || key.Contains("$") || key.Contains("%"))
                            {
                                continue;
                            }

                            xmlwriter.WriteElementString(key, "" + config[key]);
                        }
                        catch { }
                    }

                    xmlwriter.WriteEndElement();
                    xmlwriter.WriteEndDocument();
                    xmlwriter.Close();
                }

                CurrentConfigName = configName;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Delete a config profile
        /// </summary>
        public static bool DeleteConfig(string configName)
        {
            if (configName == "Default") return false; // Can't delete default

            try
            {
                var filename = Path.Combine(GetUserDataDirectory(), $"config_{configName}.xml");
                if (File.Exists(filename))
                {
                    File.Delete(filename);
                    return true;
                }
            }
            catch { }
            return false;
        }

        public string this[string key]
        {
            get
            {
                string value = null;
                config.TryGetValue(key, out value);
                return value;
            }

            set
            {
                config[key] = value;
            }
        }

        public string this[string key, string defaultvalue]
        {
            get
            {
                string value = this[key];
                if (value == null)
                    value = defaultvalue;
                return value;
            }
        }

        public IEnumerable<string> Keys
        {
            // the "ToArray" makes this safe for someone to add items while enumerating.
            get { return config.Keys.ToArray(); }
        }

        public bool ContainsKey(string key)
        {
            return config.ContainsKey(key);
        }

        public string UserAgent { get; set; } = "MissionPlanner";
        
        public string ComPort
        {
            get { return this["comport"]; }
            set { this["comport"] = value; }
        }

        public string APMFirmware
        {
            get { return this["APMFirmware"]; }
            set { this["APMFirmware"] = value; }
        }

        public string GetString(string key, string @default = "")
        {
            string result = @default;
            string value;
            if (config.TryGetValue(key, out value))
            {
                result = value;
            }
            return result;
        }

        public string BaudRate
        {
            get
            {
                try
                {
                    return this[ComPort + "_BAUD"];
                }
                catch
                {
                    return "";
                }
            }
            set { this[ComPort + "_BAUD"] = value; }
        }

        public string LogDir
        {
            get
            {
                string dir = this["logdirectory"];
                if (string.IsNullOrEmpty(dir))
                {
                    dir = GetDefaultLogDir();
                }
                return dir;
            }
            set
            {
                this["logdirectory"] = value;
            }
        }

        public int Count { get { return config.Count; } }

        public static string GetDefaultLogDir()
        {
            string directory = GetUserDataDirectory() + @"logs";
            if (!Directory.Exists(directory))
            {
                try
                {
                    Directory.CreateDirectory(directory);
                }
                catch
                {
                
                }
            }

            return directory;
        }

        public IEnumerable<string> GetList(string key)
        {
            if (config.ContainsKey(key))
                return config[key].Split(';').Select(a => System.Net.WebUtility.UrlDecode(a)).Distinct();
            return new string[0];
        }

        public void SetList(string key, IEnumerable<string> list)
        {
            if (list == null || list.Count() == 0)
                return;
            config[key] = list.Distinct().Select(a => System.Net.WebUtility.UrlEncode(a)).Distinct().Aggregate((s, s1) => s + ';' + s1);
        }

        public void AppendList(string key, string item)
        {
            var list = GetList(key).ToList();
            list.Add(item);
            SetList(key, list);
        }

        public void RemoveList(string key, string item)
        {
            var list = GetList(key).ToList().Where(a => a != item);
            //if the list is empty remove the key
            if (list == null || list.Count() == 0)
            {
                if (config.ContainsKey(key))
                    config.Remove(key);
                return;
            }
            else
            {
                SetList(key, list);
            }
        }

        public int GetInt32(string key, int defaulti = 0)
        {
            int result;
            string value;
            if (config.TryGetValue(key, out value) && int.TryParse(value, out result))
            {
                return result;
            }
            return defaulti;
        }

        public DisplayView GetDisplayView(string key)
        {
            DisplayView result;
            string value;
            if (config.TryGetValue(key, out value) && DisplayViewExtensions.TryParse(value, out result))
            {
                return result;
            }
            return new DisplayView();
        }

        public bool GetBoolean(string key, bool defaultb = false)
        {
            bool result;
            string value;
            if (config.TryGetValue(key, out value) && bool.TryParse(value, out result))
            {
                return result;
            }
            return defaultb;
        }

        public float GetFloat(string key, float defaultv = 0)
        {
            float result;
            string value;
            if (config.TryGetValue(key, out value) && float.TryParse(value, out result))
            {
                return result;
            }
            return defaultv;
        }

        public double GetDouble(string key, double defaultd = 0)
        {
            double result;
            string value;
            if (config.TryGetValue(key, out value) && double.TryParse(value, out result))
            {
                return result;
            }
            return defaultd;
        }

        public decimal GetDecimal(string key, decimal defaultd = 0)
        {
            decimal result;
            string value;
            if (config.TryGetValue(key, out value) && decimal.TryParse(value, out result))
            {
                return result;
            }
            return defaultd;
        }

        public byte GetByte(string key, byte defaultb = 0)
        {
            byte result;
            string value;
            if (config.TryGetValue(key, out value) && byte.TryParse(value, out result))
            {
                return result;
            }
            return defaultb;
        }

        private static string _GetRunningDirectory = "";
        /// <summary>
        /// Install directory path
        /// </summary>
        /// <returns></returns>
        public static string GetRunningDirectory()
        {
            if(_GetRunningDirectory != "")
                return _GetRunningDirectory;

            var ass = Assembly.GetEntryAssembly();

            if (ass == null)
            {
                if (CustomUserDataDirectory != "")
                    return CustomUserDataDirectory + Path.DirectorySeparatorChar + AppConfigName +
                           Path.DirectorySeparatorChar;

                return "." + Path.DirectorySeparatorChar;
            }

            var location = ass.Location;

            var path = Path.GetDirectoryName(location);

            if (path == "")
            {
                path = Path.GetDirectoryName(GetDataDirectory());
            }

            _GetRunningDirectory = path + Path.DirectorySeparatorChar;

            return _GetRunningDirectory;
        }

        static bool isMono()
        {
            var t = Type.GetType("Mono.Runtime");
            return (t != null);
        }

        public static bool isUnix = Environment.OSVersion.Platform == PlatformID.Unix;

        /// <summary>
        /// Shared data directory
        /// </summary>
        /// <returns></returns>
        public static string GetDataDirectory()
        {
            if (isMono())
            {
                return GetUserDataDirectory();
            }

            var path = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + Path.DirectorySeparatorChar + AppConfigName +
                          Path.DirectorySeparatorChar;

            return path;
        }

        public static string CustomUserDataDirectory = "";

        /// <summary>
        /// User specific data
        /// </summary>
        /// <returns></returns>
        public static string GetUserDataDirectory()
        {
            if (CustomUserDataDirectory != "")
                return CustomUserDataDirectory + Path.DirectorySeparatorChar + AppConfigName +
                       Path.DirectorySeparatorChar;

            var oldApproachPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                Path.DirectorySeparatorChar + AppConfigName + Path.DirectorySeparatorChar;
            var path = "";
            if (isUnix && !Directory.Exists(oldApproachPath)) // Do not use new AppData path if old path already exists
            {                                                 // E.g. do not migrate to new aproach if directory exists
                path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            }
            else
            {
                path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            path += Path.DirectorySeparatorChar + AppConfigName + Path.DirectorySeparatorChar;
            return path;
        }

        /// <summary>
        /// full path to the config file
        /// </summary>
        /// <returns></returns>
        static string GetConfigFullPath()
        {
            // old path details
            string directory = GetRunningDirectory();
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var path = Path.Combine(directory, FileName);

            // get new path details
            var newdir = GetUserDataDirectory();

            if (!Directory.Exists(newdir))
            {
                Directory.CreateDirectory(newdir);
            }

            var newpath = Path.Combine(newdir, FileName);

            // check if oldpath config exists
            if (File.Exists(path))
            {
                // is new path exists already, then dont do anything
                if (!File.Exists(newpath))
                {
                    // move to new path
                    File.Move(path, newpath);

                    // copy other xmls as this will be first run
                    var files = Directory.GetFiles(directory, "*.xml", SearchOption.TopDirectoryOnly);

                    foreach (var file in files)
                    {
                        File.Copy(file, newdir + Path.GetFileName(file));
                    }
                }
            }

            return newpath;
        }

        /// <summary>
        /// Returns the full path to the custom default config 
        /// </summary>
        /// <returns></returns>
        static string GetConfigDefaultsFullPath()
        {
            // get default path details
            var newdir = GetRunningDirectory();

            var newpath = Path.Combine(newdir, "custom.config.xml");

            return newpath;
        }

        public void Load()
        {
            // Check if we should load a specific profile
            var profileName = CurrentConfigName;
            string configFilePath;

            if (profileName != "Default")
            {
                var profilePath = Path.Combine(GetUserDataDirectory(), $"config_{profileName}.xml");
                if (File.Exists(profilePath))
                {
                    configFilePath = profilePath;
                }
                else
                {
                    // Profile doesn't exist, fall back to default
                    configFilePath = GetConfigFullPath();
                    _currentConfigName = "Default";
                }
            }
            else
            {
                configFilePath = GetConfigFullPath();
            }

            // load the defaults
            try
            {
                if (File.Exists(GetConfigDefaultsFullPath()))
                    using (XmlTextReader xmlreader = new XmlTextReader(GetConfigDefaultsFullPath()))
                    {
                        while (xmlreader.Read())
                        {
                            if (xmlreader.NodeType == XmlNodeType.Element)
                            {
                                try
                                {
                                    switch (xmlreader.Name)
                                    {
                                        case "Config":
                                            break;
                                        case "xml":
                                            break;
                                        default:
                                            var key = xmlreader.Name;
                                            if (key.Contains("____"))
                                                key = key.Replace("____", "/");
                                            config[key] = xmlreader.ReadString();
                                            break;
                                    }
                                }
                                // silent fail on bad entry
                                catch (Exception)
                                {
                                }
                            }
                        }
                    }
            }
            catch
            {

            }

            if (!File.Exists(configFilePath))
                return;

            try
            {
                using (XmlTextReader xmlreader = new XmlTextReader(configFilePath))
                {
                    while (xmlreader.Read())
                    {
                        if (xmlreader.NodeType == XmlNodeType.Element)
                        {
                            try
                            {
                                switch (xmlreader.Name)
                                {
                                    case "Config":
                                        break;
                                    case "xml":
                                        break;
                                    default:
                                        config[xmlreader.Name] = xmlreader.ReadString();
                                        break;
                                }
                            }
                            // silent fail on bad entry
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                File.Copy(GetConfigFullPath(), GetConfigFullPath() + DateTime.Now.toUnixTime() + ".failed", true);
                throw;
            }
        }

        public void Save()
        {
            // Save to the current profile file
            var profileName = CurrentConfigName;
            string filename;
            if (profileName != "Default")
            {
                filename = Path.Combine(GetUserDataDirectory(), $"config_{profileName}.xml");
            }
            else
            {
                filename = GetConfigFullPath();
            }

            using (XmlTextWriter xmlwriter = new XmlTextWriter(filename, Encoding.UTF8))
            {
                xmlwriter.Formatting = Formatting.Indented;

                xmlwriter.WriteStartDocument();

                xmlwriter.WriteStartElement("Config");

                foreach (string key2 in config.Keys.OrderBy(a=>a))
                {
                    var key = key2;
                    try
                    {
                        if (key.Contains("/"))
                            key = key.Replace("/", "____");

                        if (key == "" || key.Contains("/") || key.Contains(" ")
                            || key.Contains("-") || key.Contains(":")
                            || key.Contains(";") || key.Contains("@")
                            || key.Contains("!") || key.Contains("#")
                            || key.Contains("$") || key.Contains("%"))
                        {
                            Debugger.Break();
                            Console.WriteLine("Bad config key " + key);
                            continue;
                        }

                        xmlwriter.WriteElementString(key, ""+config[key]);
                    }
                    catch
                    {
                    }
                }

                xmlwriter.WriteEndElement();

                xmlwriter.WriteEndDocument();
                xmlwriter.Close();
            }
        }

        public void Remove(string key)
        {
            if (config.ContainsKey(key))
            {
                config.Remove(key);
            }
        }

    }
}
