namespace MissionPlanner.Utilities
{
    // Optional for speech backends that can report whether a prompt was accepted.
    public interface ISpeechSubmission
    {
        bool TrySpeakAsync(string text);
    }

    public interface ISpeech
    {
        bool speechEnable { get; set; }
        bool IsReady { get; }
        void SpeakAsync(string text);
        void SpeakAsyncCancelAll();
    }
}
