using System.Runtime.InteropServices;

namespace AudioMirrorApp;

internal sealed class AudioFormatInfo
{
    public required ushort FormatTag { get; init; }
    public required int Channels { get; init; }
    public required ushort BlockAlign { get; init; }
    public required int SampleRate { get; init; }
    public required int Bits { get; init; }
    public required int ValidBits { get; init; }
    public required Guid SubFormat { get; init; }

    public string DisplayName
    {
        get
        {
            var type = FormatTag == 3 || SubFormat == CoreAudio.IeeeFloatSubFormat
                ? "float"
                : "PCM";
            var shownBits = ValidBits > 0 && ValidBits < Bits ? $"{ValidBits}/{Bits}" : Bits.ToString();
            return $"{SampleRate} Hz, {Channels} ch, {shownBits} bit, {type}";
        }
    }

    public bool Matches(AudioFormatInfo other)
    {
        return SampleRate == other.SampleRate &&
               Bits == other.Bits &&
               ValidBits == other.ValidBits &&
               Channels == other.Channels &&
               (FormatTag == other.FormatTag || SubFormat == other.SubFormat);
    }

    public bool NeedsConversionFrom(AudioFormatInfo source)
    {
        return !Matches(source);
    }

    public static AudioFormatInfo FromPointer(IntPtr format)
    {
        var formatTag = (ushort)Marshal.ReadInt16(format, 0);
        var bits = Marshal.ReadInt16(format, 14);
        var validBits = bits;
        var subFormat = Guid.Empty;

        if (formatTag == 0xFFFE)
        {
            validBits = Marshal.ReadInt16(format, 22);
            var guidBytes = new byte[16];
            Marshal.Copy(IntPtr.Add(format, 24), guidBytes, 0, 16);
            subFormat = new Guid(guidBytes);
        }

        return new AudioFormatInfo
        {
            FormatTag = formatTag,
            Channels = Marshal.ReadInt16(format, 2),
            BlockAlign = (ushort)Marshal.ReadInt16(format, 12),
            SampleRate = Marshal.ReadInt32(format, 4),
            Bits = bits,
            ValidBits = validBits,
            SubFormat = subFormat
        };
    }
}
