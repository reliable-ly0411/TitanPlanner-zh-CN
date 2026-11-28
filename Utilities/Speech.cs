using log4net;
using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;

namespace MissionPlanner.Utilities
{
    public partial class Speech : IDisposable, ISpeech, ISpeechSubmission
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public bool speechEnable { get; set; } = false;

        SpeechSynthesizer _speechwindows;
        System.Diagnostics.Process _speechlinux;

        System.Speech.Synthesis.SynthesizerState _state = SynthesizerState.Ready;

        bool MONO = false;

        public bool IsReady
        {
            get
            {
                if (MONO)
                {
                    return _state == SynthesizerState.Ready;
                }
                else
                {
                    try
                    {
#if !LIB
                        lock (windowsSpeechLock)
                            return _speechwindows != null && !HasQueuedAzureAlert() && (_speechwindows.State == SynthesizerState.Ready ||
                                (previewPrompt != null && pendingSpeech.Count == 0));
#else
                        return _speechwindows != null && _speechwindows.State == SynthesizerState.Ready;
#endif
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
        }

        public Speech()
        {
            var t = Type.GetType("Mono.Runtime");
            MONO = (t != null);

            log.Info("TTS: init, mono = " + MONO);

            if (MONO)
            {
                _state = SynthesizerState.Ready;
            }
            else
            {
                _speechwindows = new SpeechSynthesizer();
            }
        }

        public void SpeakAsync(string text) => TrySpeakAsync(text);

        public bool TrySpeakAsync(string text)
        {
            if (!MainV2.speechEnabled() || String.IsNullOrWhiteSpace(text))
                return false;

            bool submitted = false;

            text = Regex.Replace(text, @"\bPreArm\b", "Pre Arm", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\bdist\b", "distance", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\bNAV\b", "Navigation", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\b([0-9]+)m\b", "$1 meters", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\b([0-9]+)ft\b", "$1 feet", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\b([0-9]+)\bbaud\b", "$1 baudrate", RegexOptions.IgnoreCase);
            // Stop, for example, QHover from being pronounced "kohver"
            text = Regex.Replace(text, @"\bq((?!u)[a-z]+)", "q $1", RegexOptions.IgnoreCase);

            if (MONO)
            {
                try
                {
                    //if (_speechlinux == null)
                    {
                        _state = SynthesizerState.Speaking;
                        _speechlinux = new System.Diagnostics.Process();
                        _speechlinux.StartInfo.RedirectStandardInput = true;
                        _speechlinux.StartInfo.UseShellExecute = false;
                        _speechlinux.StartInfo.FileName = "/bin/bash";
                        _speechlinux.StartInfo.Arguments = "-c festival";
                        _speechlinux.StartInfo.WorkingDirectory = "/bin";
                        _speechlinux.Start();
                        _speechlinux.Exited += new EventHandler(_speechlinux_Exited);

                        log.Info("TTS: start " + _speechlinux.StartTime);

                    }

                    _state = SynthesizerState.Speaking;
                    _speechlinux.StandardInput.WriteLine("(SayText \"" + text + "\")");
                    _speechlinux.StandardInput.WriteLine("(quit)");
                    submitted = true;

                    _speechlinux.Close();
                }
                catch (Exception ex) { log.Error(ex); } // ignore errors


                _state = SynthesizerState.Ready;
            }
            else
            {
#if !LIB
                if (EnhancedSpeechLanguage || HasVoicePreview())
                {
                    submitted = QueueWindowsSpeech(text, false);
                }
                else
#endif
                {
                    try
                    {
                        if (_speechwindows != null)
                        {
                            var builder = new PromptBuilder(CultureInfo.CurrentUICulture);
                            builder.AppendText(text);
                            _speechwindows.SpeakAsync(builder);
                            submitted = true;
                        }
                    }
                    catch (COMException) { }
                }
            }

            log.Info("TTS: say " + text);
            return submitted;
        }

        void _speechlinux_Exited(object sender, EventArgs e)
        {
            log.Info("TTS: exit " + _speechlinux.ExitTime);
            _state = SynthesizerState.Ready;
        }

        public void SpeakAsyncCancelAll()
        {
            if (MONO)
            {
                try
                {
                    if (_speechlinux != null)
                        _speechlinux.Close();
                }
                catch { }
                _state = SynthesizerState.Ready;
            }
            else
            {
                try
                {
#if !LIB
                    lock (windowsSpeechLock)
                    {
                        CancelAzureSpeech();
                        _speechwindows?.SpeakAsyncCancelAll();
                        previewPrompt = null;
                    }
#else
                    if (_speechwindows != null)
                        _speechwindows.SpeakAsyncCancelAll();
#endif
                }
                catch (System.PlatformNotSupportedException)
                {
                    _speechwindows = null;
                }
                catch { }
            }
        }

        public void Dispose()
        {
#if !LIB
            SpeechSynthesizer synthesizer;
            lock (windowsSpeechLock)
            {
                CancelAzureSpeech();
                synthesizer = _speechwindows;
                _speechwindows = null;
            }
            synthesizer?.Dispose();
            lock (windowsSpeechLock)
            {
                foreach (var path in azureSpeechFiles.Values)
                    DeleteSpeechWave(path);
                azureSpeechFiles.Clear();
            }
#else
            _speechwindows?.Dispose();
#endif
            if (_speechlinux != null)
                _speechlinux.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}