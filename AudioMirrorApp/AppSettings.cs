namespace AudioMirrorApp;

internal sealed class AppSettings
{
    public int SourceIndex { get; set; } = 1;
    public int FirstTargetIndex { get; set; } = 0;
    public int SecondTargetIndex { get; set; } = 2;
    public double FirstGain { get; set; } = 1.0;
    public double SecondGain { get; set; } = 1.0;
    public int FirstDelayMs { get; set; } = 0;
    public int SecondDelayMs { get; set; } = 0;
    public bool SplitLeftRight { get; set; } = false;
    public bool AutoRestart { get; set; } = true;
}
