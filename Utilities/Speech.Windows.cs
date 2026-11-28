#if !LIB
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Speech.Synthesis;
using System.Text;

namespace MissionPlanner.Utilities
{
    public partial class Speech
    {
        internal const string ChineseVoiceSetting = "speechvoice_zh_cn";
        internal const string OfflineXiaoxiaoVoiceSetting = "Offline:zh-CN-XiaoxiaoNeural";
        internal const string OfflineXiaoxiaoVoiceLabel = "Microsoft Xiaoxiao (offline pack)";
        private const string OfflineXiaoxiaoFolder = "zh-CN-XiaoxiaoNeural";
        private const long MaximumOfflineWaveBytes = 32 * 1024 * 1024;
        private readonly object windowsSpeechLock = new object();
        private readonly HashSet<Prompt> pendingSpeech = new HashSet<Prompt>();
        private Prompt previewPrompt;
        private bool watchCompletion;
        private string[] chineseVoices;
        private bool hasMainlandChineseVoice;

        internal static bool EnhancedSpeechLanguage =>
            CultureInfo.CurrentUICulture.Name == "zh-CN";

        internal static string GetOfflineXiaoxiaoWavePath(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            try
            {
                string normalized = text.Normalize(NormalizationForm.FormC);
                byte[] digest;
                using (var sha = SHA256.Create())
                    digest = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));
                string name = BitConverter.ToString(digest).Replace("-", "").ToLowerInvariant() + ".wav";
                return Path.Combine(Settings.GetUserDataDirectory(), "Speech", OfflineXiaoxiaoFolder, name);
            }
            catch (ArgumentException) { return null; }
        }

        internal static bool HasOfflineXiaoxiaoWave(string text)
        {
            var path = GetOfflineXiaoxiaoWavePath(text);
            return path != null && IsOfflineXiaoxiaoWave(path);
        }

        private static bool IsOfflineXiaoxiaoWave(string path)
        {
            try
            {
                using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var reader = new BinaryReader(file, Encoding.ASCII))
                {
                    if (file.Length <= 44 || file.Length > MaximumOfflineWaveBytes ||
                        ReadStringFixed(reader, 4) != "RIFF" || reader.ReadUInt32() != file.Length - 8 ||
                        ReadStringFixed(reader, 4) != "WAVE" || ReadStringFixed(reader, 4) != "fmt " ||
                        reader.ReadUInt32() != 16 || reader.ReadUInt16() != 1 || reader.ReadUInt16() != 1 ||
                        reader.ReadUInt32() != 24000 || reader.ReadUInt32() != 48000 ||
                        reader.ReadUInt16() != 2 || reader.ReadUInt16() != 16 ||
                        ReadStringFixed(reader, 4) != "data")
                        return false;
                    uint dataLength = reader.ReadUInt32();
                    return dataLength > 0 && dataLength % 2 == 0 && dataLength == file.Length - 44;
                }
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
            catch (ArgumentException) { return false; }
            catch (System.Security.SecurityException) { return false; }
        }

        private static string ReadStringFixed(BinaryReader reader, int length)
        {
            var bytes = reader.ReadBytes(length);
            return bytes.Length == length ? Encoding.ASCII.GetString(bytes) : null;
        }

        internal string[] RefreshChineseVoiceNames()
        {
            lock (windowsSpeechLock)
            {
                if (_speechwindows == null) return Array.Empty<string>();
                try
                {
                    var installed = _speechwindows.GetInstalledVoices().Where(v => v.Enabled).ToArray();
                    hasMainlandChineseVoice = installed.Any(v => v.VoiceInfo.Culture?.Name == "zh-CN");
                    chineseVoices = installed.Where(v => IsMicrosoftChineseVoice(v.VoiceInfo))
                        .Select(v => v.VoiceInfo.Name).Distinct(StringComparer.Ordinal)
                        .OrderBy(name => name, StringComparer.Ordinal).ToArray();
                }
                catch (Exception ex)
                {
                    log.Warn("TTS: could not enumerate installed voices", ex);
                    chineseVoices = Array.Empty<string>();
                    hasMainlandChineseVoice = false;
                }
                return chineseVoices;
            }
        }

        private static bool IsMicrosoftChineseVoice(VoiceInfo voice)
        {
            if (voice.Culture?.TwoLetterISOLanguageName != "zh" || string.IsNullOrEmpty(voice.Name))
                return false;
            if (voice.AdditionalInfo.TryGetValue("Vendor", out var vendor) && !string.IsNullOrEmpty(vendor))
                return string.Equals(vendor, "Microsoft", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(vendor, "Microsoft Corporation", StringComparison.OrdinalIgnoreCase);
            return voice.Name.StartsWith("Microsoft ", StringComparison.OrdinalIgnoreCase);
        }

        private bool HasVoicePreview()
        {
            lock (windowsSpeechLock) return previewPrompt != null;
        }

        internal bool TryPreviewChineseVoice()
        {
            return !MONO && EnhancedSpeechLanguage &&
                   QueueWindowsSpeech(UiText.Translate("This is the ground station voice preview."), true);
        }

        private bool QueueWindowsSpeech(string text, bool preview)
        {
            lock (windowsSpeechLock)
            {
                if (_speechwindows == null) return false;
                try
                {
                    if (preview && (!EnhancedSpeechLanguage || !speechEnable || previewPrompt != null || pendingSpeech.Count != 0 ||
                                    _speechwindows.State != SynthesizerState.Ready))
                        return false;
                    if (!watchCompletion)
                    {
                        _speechwindows.SpeakCompleted += WindowsSpeechCompleted;
                        watchCompletion = true;
                    }
                    if (!preview && previewPrompt != null)
                    {
                        var interrupted = previewPrompt;
                        previewPrompt = null;
                        try { _speechwindows.SpeakAsyncCancel(interrupted); }
                        catch (Exception ex) { log.Warn("TTS: could not cancel voice preview", ex); }
                    }

                    bool enhanced = EnhancedSpeechLanguage;
                    string selected = enhanced ? Settings.Instance[ChineseVoiceSetting] : null;
                    if (enhanced && selected == OfflineXiaoxiaoVoiceSetting && TryQueueOfflineXiaoxiao(text, preview))
                        return true;
                    if (enhanced && chineseVoices == null)
                        RefreshChineseVoiceNames();
                    if (!string.IsNullOrEmpty(selected) && chineseVoices.Contains(selected, StringComparer.Ordinal))
                    {
                        try
                        {
                            QueueWindowsPrompt(text, selected, preview);
                            return true;
                        }
                        catch (Exception ex)
                        {
                            log.Warn("TTS: selected voice unavailable, using system default", ex);
                            chineseVoices = null;
                        }
                    }
                    try
                    {
                        var preferredCulture = enhanced && hasMainlandChineseVoice
                            ? CultureInfo.CurrentUICulture : _speechwindows.Voice.Culture;
                        QueueWindowsPrompt(text, null, preview, preferredCulture);
                    }
                    catch (Exception) when (enhanced && hasMainlandChineseVoice)
                    {
                        // A voice can disappear after enumeration. Retry with the synthesizer default.
                        chineseVoices = null;
                        QueueWindowsPrompt(text, null, preview, _speechwindows.Voice.Culture);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    log.Warn("TTS: could not queue speech", ex);
                    return false;
                }
            }
        }

        private bool TryQueueOfflineXiaoxiao(string text, bool preview)
        {
            var path = GetOfflineXiaoxiaoWavePath(text);
            if (path == null || !IsOfflineXiaoxiaoWave(path)) return false;
            try
            {
                var builder = new PromptBuilder(_speechwindows.Voice.Culture ?? CultureInfo.CurrentUICulture);
                builder.AppendAudio(new Uri(path));
                QueueWindowsPrompt(new Prompt(builder), preview);
                return true;
            }
            catch (Exception ex)
            {
                log.Warn("TTS: offline Xiaoxiao audio could not be queued, using Windows speech (" +
                         ex.GetType().Name + ")");
                return false;
            }
        }

        private void QueueWindowsPrompt(string text, string name, bool preview, CultureInfo culture = null)
        {
            var builder = new PromptBuilder(culture ?? CultureInfo.CurrentUICulture);
            if (name != null) builder.StartVoice(name);
            builder.AppendText(text);
            if (name != null) builder.EndVoice();

            // Specify the voice on this prompt so queued speech keeps its original voice.
            QueueWindowsPrompt(new Prompt(builder), preview);
        }

        private void QueueWindowsPrompt(Prompt prompt, bool preview)
        {
            if (preview) previewPrompt = prompt;
            else pendingSpeech.Add(prompt);
            try
            {
                _speechwindows.SpeakAsync(prompt);
            }
            catch
            {
                ForgetWindowsPrompt(prompt);
                throw;
            }
        }

        private void WindowsSpeechCompleted(object sender, SpeakCompletedEventArgs args)
        {
            lock (windowsSpeechLock) ForgetWindowsPrompt(args.Prompt);
        }

        private void ForgetWindowsPrompt(Prompt prompt)
        {
            if (ReferenceEquals(previewPrompt, prompt)) previewPrompt = null;
            pendingSpeech.Remove(prompt);
        }
    }
}
#endif
