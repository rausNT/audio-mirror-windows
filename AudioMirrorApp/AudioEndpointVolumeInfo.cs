namespace AudioMirrorApp;

internal sealed record AudioEndpointVolumeInfo(bool Muted, float Volume)
{
    public bool IsSilent => Muted || Volume <= 0.001f;
}
