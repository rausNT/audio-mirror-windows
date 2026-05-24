namespace AudioMirrorApp;

internal sealed class AppSettings
{
    public int SourceIndex { get; set; } = 1;
    public int FirstTargetIndex { get; set; } = 0;
    public int SecondTargetIndex { get; set; } = 2;
    public int ThirdTargetIndex { get; set; } = 0;
    public string? SourceDeviceId { get; set; }
    public string? FirstTargetDeviceId { get; set; }
    public string? SecondTargetDeviceId { get; set; }
    public string? ThirdTargetDeviceId { get; set; }
    public string? SourceDeviceName { get; set; }
    public string? FirstTargetDeviceName { get; set; }
    public string? SecondTargetDeviceName { get; set; }
    public string? ThirdTargetDeviceName { get; set; }
    public double FirstGain { get; set; } = 1.0;
    public double SecondGain { get; set; } = 1.0;
    public double ThirdGain { get; set; } = 1.0;
    public int FirstDelayMs { get; set; } = 0;
    public int SecondDelayMs { get; set; } = 0;
    public int ThirdDelayMs { get; set; } = 0;
    public bool ThirdTargetEnabled { get; set; } = false;
    public bool SplitLeftRight { get; set; } = false;
    public bool AutoRestart { get; set; } = true;
    public string LanguageCode { get; set; } = "en";
    public bool OnboardingCompleted { get; set; } = false;
    public bool TrayHintShown { get; set; } = false;
}
