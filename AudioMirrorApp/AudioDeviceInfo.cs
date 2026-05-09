namespace AudioMirrorApp;

internal sealed class AudioDeviceInfo
{
    public required int Index { get; init; }
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required bool IsDefault { get; init; }
    public required uint State { get; init; }
    public required CoreAudio.IMMDevice Device { get; init; }

    public bool IsActive => (State & CoreAudio.DeviceStateActive) != 0;

    public override string ToString()
    {
        var suffix = IsDefault ? " *default" : "";
        if ((State & CoreAudio.DeviceStateUnplugged) != 0)
        {
            suffix += " (unplugged)";
        }
        else if ((State & CoreAudio.DeviceStateNotPresent) != 0)
        {
            suffix += " (not present)";
        }

        return $"{Index}: {Name}{suffix}";
    }
}
