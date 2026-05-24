namespace AudioMirrorApp;

internal sealed class AudioDeviceInfo : IDisposable
{
    public required int Index { get; init; }
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required bool IsDefault { get; init; }
    public required uint State { get; init; }
    public required CoreAudio.IMMDevice Device { get; init; }

    public bool IsActive => (State & CoreAudio.DeviceStateActive) != 0;

    public void Dispose()
    {
        CoreAudio.ReleaseComObject(Device);
    }

    public override string ToString()
    {
        var suffix = IsDefault ? $" *{AppText.T("DefaultSuffix")}" : "";
        if ((State & CoreAudio.DeviceStateUnplugged) != 0)
        {
            suffix += $" ({AppText.T("UnpluggedSuffix")})";
        }
        else if ((State & CoreAudio.DeviceStateNotPresent) != 0)
        {
            suffix += $" ({AppText.T("NotPresentSuffix")})";
        }

        return $"{Index}: {Name}{suffix}";
    }
}
