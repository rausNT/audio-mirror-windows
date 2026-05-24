using System.Runtime.InteropServices;

namespace AudioMirrorApp;

internal sealed class WasapiMirrorEngine : IDisposable
{
    private enum ChannelMode
    {
        Stereo,
        LeftToMono,
        RightToMono
    }

    private sealed class DelayBuffer
    {
        private readonly byte[] buffer;
        private int offset;

        public DelayBuffer(int bytes)
        {
            buffer = new byte[Math.Max(bytes, 1)];
        }

        public void Process(byte[] data, int bytes)
        {
            for (var i = 0; i < bytes; i++)
            {
                var delayed = buffer[offset];
                buffer[offset] = data[i];
                data[i] = delayed;
                offset++;
                if (offset == buffer.Length)
                {
                    offset = 0;
                }
            }
        }
    }

    private sealed class RenderSink : IDisposable
    {
        private readonly object settingsLock = new();
        private readonly CoreAudio.IAudioClient audioClient;
        private readonly CoreAudio.IAudioRenderClient renderClient;
        private readonly uint bufferFrames;
        private readonly ushort blockAlign;
        private readonly int sampleRate;
        private readonly int channels;
        private readonly int bits;
        private readonly int validBits;
        private readonly ushort formatTag;
        private readonly Guid subFormat;
        private byte[] transfer = [];
        private DelayBuffer? delayBuffer;
        private double gain;
        private int delayMs;
        private ChannelMode channelMode;
        private float level;

        public RenderSink(
            AudioDeviceInfo device,
            IntPtr format,
            long bufferDuration,
            ushort blockAlign,
            int sampleRate,
            int channels,
            int bits,
            int validBits,
            ushort formatTag,
            Guid subFormat,
            double gain,
            int delayMs,
            ChannelMode channelMode)
        {
            Name = device.Name;
            this.blockAlign = blockAlign;
            this.sampleRate = sampleRate;
            this.channels = channels;
            this.bits = bits;
            this.validBits = validBits;
            this.formatTag = formatTag;
            this.subFormat = subFormat;
            SetSettings(gain, delayMs, channelMode);

            audioClient = CoreAudio.ActivateAudioClient(device.Device);
            var session = Guid.Empty;
            audioClient.Initialize(
                CoreAudio.ShareMode.Shared,
                CoreAudio.StreamFlags.AutoconvertPcm | CoreAudio.StreamFlags.SrcDefaultQuality,
                bufferDuration,
                0,
                format,
                ref session);

            audioClient.GetBufferSize(out bufferFrames);

            var serviceId = CoreAudio.IAudioRenderClientId;
            audioClient.GetService(ref serviceId, out var service);
            renderClient = (CoreAudio.IAudioRenderClient)service;
        }

        public string Name { get; }
        public long WrittenFrames { get; private set; }
        public long DroppedFrames { get; private set; }
        public float Level => level;

        public void SetSettings(double newGain, int newDelayMs, ChannelMode newChannelMode)
        {
            if (newGain <= 0 || newGain > 8)
            {
                throw new ArgumentOutOfRangeException(nameof(newGain), "Gain must be from 0.01 to 8.");
            }

            if (newDelayMs < 0 || newDelayMs > 2000)
            {
                throw new ArgumentOutOfRangeException(nameof(newDelayMs), "Delay must be from 0 to 2000 ms.");
            }

            lock (settingsLock)
            {
                gain = newGain;
                channelMode = newChannelMode;
                if (delayMs != newDelayMs)
                {
                    delayMs = newDelayMs;
                    var delayBytes = checked((int)(((long)sampleRate * delayMs / 1000) * blockAlign));
                    delayBuffer = delayBytes > 0 ? new DelayBuffer(delayBytes) : null;
                }
            }
        }

        public void Start()
        {
            audioClient.Start();
        }

        public void Stop()
        {
            try
            {
                audioClient.Stop();
            }
            catch
            {
                // Stop is best-effort during shutdown.
            }
        }

        public void Write(IntPtr sourceBuffer, uint frames, CoreAudio.BufferFlags flags)
        {
            audioClient.GetCurrentPadding(out var padding);
            var available = bufferFrames > padding ? bufferFrames - padding : 0;
            var framesToWrite = Math.Min(frames, available);
            if (framesToWrite < frames)
            {
                DroppedFrames += frames - framesToWrite;
            }

            if (framesToWrite == 0)
            {
                return;
            }

            renderClient.GetBuffer(framesToWrite, out var targetBuffer);
            var releaseFlags = (flags & CoreAudio.BufferFlags.Silent) != 0
                ? CoreAudio.BufferFlags.Silent
                : CoreAudio.BufferFlags.None;

            try
            {
                if ((flags & CoreAudio.BufferFlags.Silent) != 0)
                {
                    WrittenFrames += framesToWrite;
                    return;
                }

                var bytes = checked((int)(framesToWrite * blockAlign));
                if (transfer.Length < bytes)
                {
                    transfer = new byte[bytes];
                }

                Marshal.Copy(sourceBuffer, transfer, 0, bytes);

                lock (settingsLock)
                {
                    if (Math.Abs(gain - 1.0) > 0.001)
                    {
                        ApplyGain(transfer, bytes, gain, bits, validBits, formatTag, subFormat);
                    }

                    if (channelMode != ChannelMode.Stereo)
                    {
                        ApplyChannelMode(transfer, (int)framesToWrite, channels, bits, validBits, formatTag, subFormat, channelMode);
                    }

                    delayBuffer?.Process(transfer, bytes);
                    level = SmoothLevel(level, CalculatePeak(transfer, bytes, bits, validBits, formatTag, subFormat));
                }

                Marshal.Copy(transfer, 0, targetBuffer, bytes);
                WrittenFrames += framesToWrite;
            }
            finally
            {
                renderClient.ReleaseBuffer(framesToWrite, releaseFlags);
            }
        }

        public void DecayLevel()
        {
            level *= 0.82f;
        }

        public void Dispose()
        {
            Stop();
            ReleaseComObject(renderClient);
            ReleaseComObject(audioClient);
        }

        private static void ApplyGain(byte[] data, int bytes, double gain, int bits, int validBits, ushort formatTag, Guid subFormat)
        {
            var isFloat = formatTag == 3 || subFormat == CoreAudio.IeeeFloatSubFormat;
            var isPcm = formatTag == 1 || subFormat == CoreAudio.PcmSubFormat;

            if (isFloat && bits == 32)
            {
                for (var i = 0; i < bytes; i += 4)
                {
                    var value = BitConverter.ToSingle(data, i);
                    value = (float)Math.Clamp(value * gain, -1.0, 1.0);
                    Buffer.BlockCopy(BitConverter.GetBytes(value), 0, data, i, 4);
                }

                return;
            }

            if (isPcm && bits == 16)
            {
                for (var i = 0; i < bytes; i += 2)
                {
                    var value = BitConverter.ToInt16(data, i);
                    var scaled = (short)Math.Clamp((int)Math.Round(value * gain), short.MinValue, short.MaxValue);
                    var raw = BitConverter.GetBytes(scaled);
                    data[i] = raw[0];
                    data[i + 1] = raw[1];
                }

                return;
            }

            if (isPcm && bits == 32 && validBits <= 24)
            {
                for (var i = 0; i < bytes; i += 4)
                {
                    var value = BitConverter.ToInt32(data, i) >> 8;
                    var scaled = Math.Clamp((int)Math.Round(value * gain), -8_388_608, 8_388_607);
                    Buffer.BlockCopy(BitConverter.GetBytes(scaled << 8), 0, data, i, 4);
                }
            }
        }

        private static void ApplyChannelMode(byte[] data, int frames, int channels, int bits, int validBits, ushort formatTag, Guid subFormat, ChannelMode mode)
        {
            if (channels < 2)
            {
                return;
            }

            var sourceChannel = mode == ChannelMode.RightToMono ? 1 : 0;
            var isFloat = formatTag == 3 || subFormat == CoreAudio.IeeeFloatSubFormat;
            var isPcm = formatTag == 1 || subFormat == CoreAudio.PcmSubFormat;
            var bytesPerSample = bits / 8;

            if (isFloat && bits == 32)
            {
                for (var frame = 0; frame < frames; frame++)
                {
                    var frameOffset = frame * channels * bytesPerSample;
                    var sourceOffset = frameOffset + sourceChannel * bytesPerSample;
                    var value = BitConverter.ToSingle(data, sourceOffset);
                    var raw = BitConverter.GetBytes(value);
                    for (var channel = 0; channel < channels; channel++)
                    {
                        Buffer.BlockCopy(raw, 0, data, frameOffset + channel * bytesPerSample, bytesPerSample);
                    }
                }

                return;
            }

            if (isPcm && bits == 16)
            {
                for (var frame = 0; frame < frames; frame++)
                {
                    var frameOffset = frame * channels * bytesPerSample;
                    var sourceOffset = frameOffset + sourceChannel * bytesPerSample;
                    var value0 = data[sourceOffset];
                    var value1 = data[sourceOffset + 1];
                    for (var channel = 0; channel < channels; channel++)
                    {
                        var offset = frameOffset + channel * bytesPerSample;
                        data[offset] = value0;
                        data[offset + 1] = value1;
                    }
                }

                return;
            }

            if (isPcm && (bits == 24 || bits == 32 && validBits <= 24))
            {
                for (var frame = 0; frame < frames; frame++)
                {
                    var frameOffset = frame * channels * bytesPerSample;
                    var sourceOffset = frameOffset + sourceChannel * bytesPerSample;
                    for (var channel = 0; channel < channels; channel++)
                    {
                        var offset = frameOffset + channel * bytesPerSample;
                        Buffer.BlockCopy(data, sourceOffset, data, offset, bytesPerSample);
                    }
                }
            }
        }
    }

    private readonly CancellationTokenSource cancellation = new();
    private readonly Task worker;
    private readonly CoreAudio.IAudioClient captureAudioClient;
    private readonly CoreAudio.IAudioCaptureClient captureClient;
    private readonly RenderSink firstSink;
    private readonly RenderSink secondSink;
    private readonly RenderSink? thirdSink;
    private readonly IntPtr format;

    public WasapiMirrorEngine(
        AudioDeviceInfo source,
        AudioDeviceInfo firstTarget,
        AudioDeviceInfo secondTarget,
        AudioDeviceInfo? thirdTarget,
        double firstGain,
        double secondGain,
        double thirdGain,
        int firstDelayMs,
        int secondDelayMs,
        int thirdDelayMs,
        bool splitLeftRight)
    {
        if (source.Id == firstTarget.Id ||
            source.Id == secondTarget.Id ||
            firstTarget.Id == secondTarget.Id ||
            thirdTarget is not null && (
                source.Id == thirdTarget.Id ||
                firstTarget.Id == thirdTarget.Id ||
                secondTarget.Id == thirdTarget.Id))
        {
            throw new InvalidOperationException("Source and targets must be different devices.");
        }

        SourceName = source.Name;
        FirstTargetName = firstTarget.Name;
        SecondTargetName = secondTarget.Name;
        ThirdTargetName = thirdTarget?.Name;

        captureAudioClient = CoreAudio.ActivateAudioClient(source.Device);
        captureAudioClient.GetMixFormat(out format);
        Format = AudioFormatInfo.FromPointer(format);

        var session = Guid.Empty;
        var bufferDuration = CoreAudio.RefTimesPerSecond / 10;
        captureAudioClient.Initialize(
            CoreAudio.ShareMode.Shared,
            CoreAudio.StreamFlags.Loopback,
            bufferDuration,
            0,
            format,
            ref session);

        firstSink = new RenderSink(
            firstTarget,
            format,
            bufferDuration,
            Format.BlockAlign,
            Format.SampleRate,
            Format.Channels,
            Format.Bits,
            Format.ValidBits,
            Format.FormatTag,
            Format.SubFormat,
            firstGain,
            firstDelayMs,
            splitLeftRight ? ChannelMode.LeftToMono : ChannelMode.Stereo);

        secondSink = new RenderSink(
            secondTarget,
            format,
            bufferDuration,
            Format.BlockAlign,
            Format.SampleRate,
            Format.Channels,
            Format.Bits,
            Format.ValidBits,
            Format.FormatTag,
            Format.SubFormat,
            secondGain,
            secondDelayMs,
            splitLeftRight ? ChannelMode.RightToMono : ChannelMode.Stereo);

        if (thirdTarget is not null)
        {
            thirdSink = new RenderSink(
                thirdTarget,
                format,
                bufferDuration,
                Format.BlockAlign,
                Format.SampleRate,
                Format.Channels,
                Format.Bits,
                Format.ValidBits,
                Format.FormatTag,
                Format.SubFormat,
                thirdGain,
                thirdDelayMs,
                ChannelMode.Stereo);
        }

        var captureServiceId = CoreAudio.IAudioCaptureClientId;
        captureAudioClient.GetService(ref captureServiceId, out var service);
        captureClient = (CoreAudio.IAudioCaptureClient)service;

        firstSink.Start();
        secondSink.Start();
        thirdSink?.Start();
        captureAudioClient.Start();

        worker = Task.Run(CaptureLoop);
    }

    public string SourceName { get; }
    public string FirstTargetName { get; }
    public string SecondTargetName { get; }
    public string? ThirdTargetName { get; }
    public string TargetNames => ThirdTargetName is null
        ? $"{FirstTargetName}, {SecondTargetName}"
        : $"{FirstTargetName}, {SecondTargetName}, {ThirdTargetName}";
    public AudioFormatInfo Format { get; }
    public long Packets { get; private set; }
    public long CapturedFrames { get; private set; }
    public long FirstWrittenFrames => firstSink.WrittenFrames;
    public long FirstDroppedFrames => firstSink.DroppedFrames;
    public long SecondWrittenFrames => secondSink.WrittenFrames;
    public long SecondDroppedFrames => secondSink.DroppedFrames;
    public long ThirdWrittenFrames => thirdSink?.WrittenFrames ?? 0;
    public long ThirdDroppedFrames => thirdSink?.DroppedFrames ?? 0;
    public float SourceLevel { get; private set; }
    public float FirstLevel => firstSink.Level;
    public float SecondLevel => secondSink.Level;
    public float ThirdLevel => thirdSink?.Level ?? 0;
    public Exception? LastError { get; private set; }

    public void UpdateSettings(
        double firstGain,
        double secondGain,
        double thirdGain,
        int firstDelayMs,
        int secondDelayMs,
        int thirdDelayMs,
        bool splitLeftRight)
    {
        firstSink.SetSettings(firstGain, firstDelayMs, splitLeftRight ? ChannelMode.LeftToMono : ChannelMode.Stereo);
        secondSink.SetSettings(secondGain, secondDelayMs, splitLeftRight ? ChannelMode.RightToMono : ChannelMode.Stereo);
        thirdSink?.SetSettings(thirdGain, thirdDelayMs, ChannelMode.Stereo);
    }

    public void Dispose()
    {
        cancellation.Cancel();
        try
        {
            worker.Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {
            // Shutdown should not throw into the UI.
        }

        try
        {
            captureAudioClient.Stop();
        }
        catch
        {
        }

        firstSink.Dispose();
        secondSink.Dispose();
        thirdSink?.Dispose();
        if (format != IntPtr.Zero)
        {
            CoreAudio.CoTaskMemFree(format);
        }

        ReleaseComObject(captureClient);
        ReleaseComObject(captureAudioClient);
        cancellation.Dispose();
    }

    private void CaptureLoop()
    {
        try
        {
            while (!cancellation.IsCancellationRequested)
            {
                captureClient.GetNextPacketSize(out var packetFrames);
                if (packetFrames == 0)
                {
                    Thread.Sleep(3);
                    continue;
                }

                while (packetFrames != 0 && !cancellation.IsCancellationRequested)
                {
                    captureClient.GetBuffer(out var sourceBuffer, out var frames, out var flags, out _, out _);
                    try
                    {
                        Packets++;
                        CapturedFrames += frames;
                        SourceLevel = (flags & CoreAudio.BufferFlags.Silent) != 0
                            ? SourceLevel * 0.82f
                            : SmoothLevel(SourceLevel, CalculatePeak(sourceBuffer, (int)(frames * Format.BlockAlign), Format.Bits, Format.ValidBits, Format.FormatTag, Format.SubFormat));
                        firstSink.Write(sourceBuffer, frames, flags);
                        secondSink.Write(sourceBuffer, frames, flags);
                        thirdSink?.Write(sourceBuffer, frames, flags);
                    }
                    finally
                    {
                        captureClient.ReleaseBuffer(frames);
                    }

                    captureClient.GetNextPacketSize(out packetFrames);
                }

                SourceLevel *= 0.96f;
                firstSink.DecayLevel();
                secondSink.DecayLevel();
                thirdSink?.DecayLevel();
            }
        }
        catch (Exception ex)
        {
            LastError = ex;
        }
    }

    private static float SmoothLevel(float previous, float current)
    {
        return Math.Max(current, previous * 0.82f);
    }

    private static void ReleaseComObject(object? instance)
    {
        if (instance is null || !Marshal.IsComObject(instance))
        {
            return;
        }

        try
        {
            Marshal.FinalReleaseComObject(instance);
        }
        catch
        {
            // COM cleanup is best-effort during audio shutdown.
        }
    }

    private static float CalculatePeak(IntPtr data, int bytes, int bits, int validBits, ushort formatTag, Guid subFormat)
    {
        if (bytes <= 0)
        {
            return 0f;
        }

        var buffer = new byte[bytes];
        Marshal.Copy(data, buffer, 0, bytes);
        return CalculatePeak(buffer, bytes, bits, validBits, formatTag, subFormat);
    }

    private static float CalculatePeak(byte[] data, int bytes, int bits, int validBits, ushort formatTag, Guid subFormat)
    {
        var isFloat = formatTag == 3 || subFormat == CoreAudio.IeeeFloatSubFormat;
        var isPcm = formatTag == 1 || subFormat == CoreAudio.PcmSubFormat;
        var peak = 0f;

        if (isFloat && bits == 32)
        {
            for (var i = 0; i + 3 < bytes; i += 4)
            {
                peak = Math.Max(peak, Math.Abs(BitConverter.ToSingle(data, i)));
            }
        }
        else if (isPcm && bits == 16)
        {
            for (var i = 0; i + 1 < bytes; i += 2)
            {
                peak = Math.Max(peak, Math.Abs(BitConverter.ToInt16(data, i) / (float)short.MaxValue));
            }
        }
        else if (isPcm && bits == 24)
        {
            for (var i = 0; i + 2 < bytes; i += 3)
            {
                var value = data[i] | (data[i + 1] << 8) | (data[i + 2] << 16);
                if ((value & 0x800000) != 0)
                {
                    value |= unchecked((int)0xFF000000);
                }

                peak = Math.Max(peak, Math.Abs(value / 8_388_607f));
            }
        }
        else if (isPcm && bits == 32 && validBits <= 24)
        {
            for (var i = 0; i + 3 < bytes; i += 4)
            {
                var value = BitConverter.ToInt32(data, i) >> 8;
                peak = Math.Max(peak, Math.Abs(value / 8_388_607f));
            }
        }

        return Math.Clamp(peak, 0f, 1f);
    }

}
