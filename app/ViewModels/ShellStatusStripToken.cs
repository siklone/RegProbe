namespace RegProbe.App.ViewModels;

public sealed class ShellStatusStripToken
{
    public ShellStatusStripToken(string text, string tone = "neutral")
    {
        Text = text;
        Tone = tone;
    }

    public string Text { get; }

    public string Tone { get; }
}
