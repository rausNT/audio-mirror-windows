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

    private sealed class RenderSink
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

            if ((flags & CoreAudio.BufferFlags.Silent) != 0)
            {
                renderClient.ReleaseBuffer(framesToWrite, CoreAudio.BufferFlags.Silent);
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
            }

            Marshal.Copy(transfer, 0, targetBuffer, bytes);
            renderClient.ReleaseBuffer(framesToWrite, CoreAudio.BufferFlags.None);
            WrittenFrames += framesToWrite;
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
    private readonly IntPtr format;

    public WasapiMirrorEngine(
        AudioDeviceInfo source,
        AudioDeviceInfo firstTarget,
        AudioDeviceInfo secondTarget,
        double firstGain,
        double secondGain,
        int firstDelayMs,
        int secondDelayMs,
        bool splitLeftRight)
    {
        if (source.Id == firstTarget.Id || source.Id == secondTarget.Id || firstTarget.Id == secondTarget.Id)
        {
            throw new InvalidOperationException("Source and targets must be different devices.");
        }

        SourceName = source.Name;
        FirstTargetName = firstTarget.Name;
        SecondTargetName = secondTarget.Name;

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

        var captureServiceId = CoreAudio.IAudioCaptureClientId;
        captureAudioClient.GetService(ref captureServiceId, out var service);
        captureClient = (CoreAudio.IAudioCaptureClient)service;

        firstSink.Start();
        secondSink.Start();
        captureAudioClient.Start();

        worker = Task.Run(CaptureLoop);
    }

    public string SourceName { get; }
    public string FirstTargetName { get; }
    public string SecondTargetName { get; }
    public AudioFormatInfo Format { get; }
    public long Packets { get; private set; }
    public long CapturedFrames { get; private set; }
    public long FirstWrittenFrames => firstSink.WrittenFrames;
    public long FirstDroppedFrames => firstSink.DroppedFrames;
    public long SecondWrittenFrames => secondSink.WrittenFrames;
    public long SecondDroppedFrames => secondSink.DroppedFrames;
    public Exception? LastError { get; private set; }

    public void UpdateSettings(double firstGain, double secondGain, int firstDelayMs, int secondDelayMs, bool splitLeftRight)
    {
        firstSink.SetSettings(firstGain, firstDelayMs, splitLeftRight ? ChannelMode.LeftToMono : ChannelMode.Stereo);
        secondSink.SetSettings(secondGain, secondDelayMs, splitLeftRight ? ChannelMode.RightToMono : ChannelMode.Stereo);
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

        firstSink.Stop();
        secondSink.Stop();
        if (format != IntPtr.Zero)
        {
            CoreAudio.CoTaskMemFree(format);
        }

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
                    Packets++;
                    CapturedFrames += frames;
                    firstSink.Write(sourceBuffer, frames, flags);
                    secondSink.Write(sourceBuffer, frames, flags);
                    captureClient.ReleaseBuffer(frames);
                    captureClient.GetNextPacketSize(out packetFrames);
                }
            }
        }
        catch (Exception ex)
        {
            LastError = ex;
        }
    }

}
