using System;
using MissionPlanner.Utilities;

namespace MissionPlanner
{
    // Both existing speech paths can see the same received row.
    internal sealed class FirmwareSpeechAlert
    {
        private readonly object gate = new object();
        private readonly Func<string> verifiedText;
        private bool submitted;

        internal FirmwareSpeechAlert(Func<string> verifiedText)
        {
            this.verifiedText = verifiedText;
        }

        internal bool Speak(ISpeech speaker, string raw)
        {
            lock (gate)
            {
                if (submitted) return true;
                var text = verifiedText() ?? raw;
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
    }
}
