#if !LIB
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Speech.Synthesis;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MissionPlanner.Utilities
{
    public partial class Speech
    {
        internal const string XiaoxiaoVoiceSetting = "Azure:zh-CN-XiaoxiaoNeural";
        internal const string XiaoxiaoVoiceLabel = "Microsoft Xiaoxiao (Azure Neural)";
        private const string XiaoxiaoVoice = "zh-CN-XiaoxiaoNeural";
        private const string RegionVariable = "TITANPLANNER_AZURE_SPEECH_REGION";
        private const string KeyVariable = "TITANPLANNER_AZURE_SPEECH_KEY";
        private static readonly HttpClient SharedAzureSpeechClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            Timeout = TimeSpan.FromSeconds(2),
            MaxResponseContentBufferSize = 4 * 1024 * 1024
        };

        private readonly HttpClient azureSpeechClient;
        private readonly Queue<AzureSpeechRequest> azureSpeechQueue = new Queue<AzureSpeechRequest>();
        private readonly Dictionary<Prompt, string> azureSpeechFiles = new Dictionary<Prompt, string>();
        private AzureSpeechRequest activeAzureSpeech;
        private AzureSpeechRequest azurePreview;
        private bool azureSpeechWorker;

        private sealed class AzureSpeechRequest
        {
            internal string Text;
            internal string SelectedVoice;
            internal Uri Endpoint;
            internal string Key;
            internal bool Preview;
            internal readonly CancellationTokenSource Cancellation = new CancellationTokenSource();
        }

        internal Speech(HttpClient azureSpeechClient) : this()
        {
            this.azureSpeechClient = azureSpeechClient;
        }

        internal static bool IsXiaoxiaoConfigured => GetAzureSpeechEndpoint() != null && GetAzureSpeechKey() != null;

        private static string GetAzureSpeechKey()
        {
            var key = Environment.GetEnvironmentVariable(KeyVariable)?.Trim();
            return string.IsNullOrEmpty(key) ? null : key;
        }

        private static Uri GetAzureSpeechEndpoint()
        {
            var region = Environment.GetEnvironmentVariable(RegionVariable)?.Trim().ToLowerInvariant();
            return region != null && Regex.IsMatch(region, @"\A[a-z0-9]{1,40}\z")
                ? new Uri("https://" + region + ".tts.speech.microsoft.com/cognitiveservices/v1")
                : null;
        }

        // Called under windowsSpeechLock. SAPI owns playback, including canceling an active WAV preview.
        private bool QueueAzureSpeech(string text, string selected, bool preview)
        {
            var endpoint = selected == XiaoxiaoVoiceSetting ? GetAzureSpeechEndpoint() : null;
            var key = endpoint == null ? null : GetAzureSpeechKey();
            if (key == null && !HasQueuedAzureSpeech())
                return false;

            var request = new AzureSpeechRequest
            {
                Text = text, SelectedVoice = selected, Endpoint = endpoint, Key = key, Preview = preview
            };
            azureSpeechQueue.Enqueue(request);
            if (preview)
                azurePreview = request;
            if (!azureSpeechWorker)
            {
                azureSpeechWorker = true;
                _ = Task.Run(ProcessAzureSpeechAsync);
            }
            return true;
        }

        private bool HasQueuedAzureSpeech() =>
            (activeAzureSpeech != null && !activeAzureSpeech.Cancellation.IsCancellationRequested) ||
            azureSpeechQueue.Any(item => !item.Cancellation.IsCancellationRequested);

        private bool HasQueuedAzureAlert() =>
            (activeAzureSpeech != null && !activeAzureSpeech.Preview && !activeAzureSpeech.Cancellation.IsCancellationRequested) ||
            azureSpeechQueue.Any(item => !item.Preview);

        private void CancelAzurePreview()
        {
            azurePreview?.Cancellation.Cancel();
            azurePreview = null;
        }

        private void CancelAzureSpeech()
        {
            CancelAzurePreview();
            activeAzureSpeech?.Cancellation.Cancel();
            while (azureSpeechQueue.Count != 0)
                azureSpeechQueue.Dequeue().Cancellation.Dispose();
        }

        private async Task ProcessAzureSpeechAsync()
        {
            while (true)
            {
                AzureSpeechRequest request;
                lock (windowsSpeechLock)
                {
                    if (azureSpeechQueue.Count == 0)
                    {
                        azureSpeechWorker = false;
                        return;
                    }
                    request = activeAzureSpeech = azureSpeechQueue.Dequeue();
                }

                string path = null;
                try
                {
                    if (request.Key != null)
                    {
                        var data = await SynthesizeXiaoxiaoAsync(azureSpeechClient ?? SharedAzureSpeechClient, request.Endpoint, request.Key,
                            request.Text, request.Cancellation.Token).ConfigureAwait(false);
                        request.Cancellation.Token.ThrowIfCancellationRequested();
                        path = Path.Combine(Path.GetTempPath(), "titanplanner-speech-" + Guid.NewGuid().ToString("N") + ".wav");
                        using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                            await file.WriteAsync(data, 0, data.Length, request.Cancellation.Token).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    DeleteSpeechWave(path);
                    path = null;
                    if (!request.Cancellation.IsCancellationRequested)
                        log.Warn("TTS: Azure speech unavailable, using Windows speech (" + ex.GetType().Name + ")");
                }

                lock (windowsSpeechLock)
                {
                    try
                    {
                        if (_speechwindows != null && !request.Cancellation.IsCancellationRequested)
                        {
                            bool queued = false;
                            if (path != null)
                            {
                                try
                                {
                                    var builder = new PromptBuilder(CultureInfo.GetCultureInfo("zh-CN"));
                                    builder.AppendAudio(new Uri(path));
                                    QueueWindowsPrompt(new Prompt(builder), request.Preview, path);
                                    path = null; // SpeakCompleted now owns the temporary file.
                                    queued = true;
                                }
                                catch (Exception ex)
                                {
                                    log.Warn("TTS: Azure audio could not be queued, using Windows speech (" + ex.GetType().Name + ")");
                                }
                            }
                            if (!queued)
                                QueueLocalWindowsSpeech(request.Text, request.SelectedVoice, request.Preview, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Warn("TTS: could not queue Windows fallback", ex);
                    }
                    finally
                    {
                        DeleteSpeechWave(path);
                        if (ReferenceEquals(azurePreview, request))
                            azurePreview = null;
                        activeAzureSpeech = null;
                        request.Cancellation.Dispose();
                    }
                }
            }
        }

        internal static async Task<byte[]> SynthesizeXiaoxiaoAsync(HttpClient client, Uri endpoint, string key,
            string text, CancellationToken cancellation)
        {
            XNamespace ssml = "http://www.w3.org/2001/10/synthesis";
            var body = new XElement(ssml + "speak", new XAttribute("version", "1.0"),
                new XAttribute(XNamespace.Xml + "lang", "zh-CN"),
                new XElement(ssml + "voice", new XAttribute("name", XiaoxiaoVoice), text));
            using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
            {
                request.Headers.Add("Ocp-Apim-Subscription-Key", key);
                request.Headers.Add("X-Microsoft-OutputFormat", "riff-24khz-16bit-mono-pcm");
                request.Headers.UserAgent.ParseAdd("TitanPlanner");
                request.Content = new StringContent(body.ToString(SaveOptions.DisableFormatting), Encoding.UTF8,
                    "application/ssml+xml");
                using (var response = await client.SendAsync(request, cancellation).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    var wave = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                    if (!IsSpeechWave(wave))
                        throw new InvalidDataException("Invalid speech WAV");
                    return wave;
                }
            }
        }

        private static bool IsSpeechWave(byte[] wave)
        {
            using (var reader = new BinaryReader(new MemoryStream(wave, false)))
            {
                if (wave.Length <= 44 || new string(reader.ReadChars(4)) != "RIFF" ||
                    reader.ReadUInt32() != wave.Length - 8 || new string(reader.ReadChars(4)) != "WAVE")
                    return false;

                bool hasFormat = false;
                while (reader.BaseStream.Position + 8 <= wave.Length)
                {
                    string tag = new string(reader.ReadChars(4));
                    uint length = reader.ReadUInt32();
                    long end = reader.BaseStream.Position + length + (length & 1);
                    if (end > wave.Length)
                        return false;
                    if (tag == "fmt " && length >= 16)
                    {
                        hasFormat = reader.ReadUInt16() == 1 && reader.ReadUInt16() == 1 && reader.ReadUInt32() == 24000 &&
                            reader.ReadUInt32() == 48000 && reader.ReadUInt16() == 2 && reader.ReadUInt16() == 16;
                    }
                    if (tag == "data")
                        return hasFormat && length > 0 && length % 2 == 0;
                    reader.BaseStream.Position = end;
                }
                return false;
            }
        }

        private static void DeleteSpeechWave(string path)
        {
            if (path == null) return;
            try { File.Delete(path); }
            catch (IOException) { log.Warn("TTS: could not remove temporary speech audio"); }
            catch (UnauthorizedAccessException) { log.Warn("TTS: could not remove temporary speech audio"); }
        }
    }
}
#endif
