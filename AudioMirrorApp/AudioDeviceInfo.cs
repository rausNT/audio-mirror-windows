namespace AudioMirrorApp;

internal sealed class AudioDeviceInfo
{
    public required int Index { get; init; }
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required bool IsDefault { get; init; }
    public required CoreAudio.IMMDevice Device { get; init; }

    public override string ToString()
    {
        return IsDefault ? $"{Index}: {Name} *default" : $"{Index}: {Name}";
    }
}
