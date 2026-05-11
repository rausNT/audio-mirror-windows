using System.Runtime.InteropServices;

namespace AudioMirrorApp;

internal enum TestToneMode
{
    None,
    Left,
    Right,
    Third,
    Both
}

internal sealed class TestTonePlayer : IDisposable
{
    private sealed class RenderOutput
    {
        private readonly CoreAudio.IAudioClient audioClient;
        private readonly CoreAudio.IAudioRenderClient renderClient;
        private readonly uint bufferFrames;
        private readonly AudioFormatInfo format;
        private readonly byte[] buffer;
        private double phase;

        public RenderOutput(AudioDeviceInfo device)
        {
            audioClient = CoreAudio.ActivateAudioClient(device.Device);
            IntPtr formatPointer = IntPtr.Zero;
            try
            {
                audioClient.GetMixFormat(out formatPointer);
                format = AudioFormatInfo.FromPointer(formatPointer);
                var session = Guid.Empty;
                audioClient.Initialize(
                    CoreAudio.ShareMode.Shared,
                    CoreAudio.StreamFlags.AutoconvertPcm | CoreAudio.StreamFlags.SrcDefaultQuality,
                    CoreAudio.RefTimesPerSecond / 10,
                    0,
                    formatPointer,
                    ref session);
            }
            finally
            {
                if (formatPointer != IntPtr.Zero)
                {
                    CoreAudio.CoTaskMemFree(formatPointer);
                }
            }

            audioClient.GetBufferSize(out bufferFrames);
            buffer = new byte[bufferFrames * format.BlockAlign];

            var renderId = CoreAudio.IAudioRenderClientId;
            audioClient.GetService(ref renderId, out var service);
            renderClient = (CoreAudio.IAudioRenderClient)service;
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
            }
        }

        public void WriteTone(double frequency, double amplitude)
        {
            audioClient.GetCurrentPadding(out var padding);
            var available = bufferFrames > padding ? bufferFrames - padding : 0;
            if (available == 0)
            {
                return;
            }

            var frames = Math.Min(available, (uint)(format.SampleRate / 50));
            var bytes = checked((int)(frames * format.BlockAlign));
            FillTone(buffer, bytes, (int)frames, format, frequency, amplitude, ref phase);

            renderClient.GetBuffer(frames, out var target);
            Marshal.Copy(buffer, 0, target, bytes);
            renderClient.ReleaseBuffer(frames, CoreAudio.BufferFlags.None);
        }

        private static void FillTone(byte[] data, int bytes, int frames, AudioFormatInfo format, double frequency, double amplitude, ref double phase)
        {
            Array.Clear(data, 0, bytes);
            var isFloat = format.FormatTag == 3 || format.SubFormat == CoreAudio.IeeeFloatSubFormat;
            var isPcm = format.FormatTag == 1 || format.SubFormat == CoreAudio.PcmSubFormat;
            var bytesPerSample = format.Bits / 8;
            var step = 2.0 * Math.PI * frequency / format.SampleRate;

            for (var frame = 0; frame < frames; frame++)
            {
                var sample = Math.Sin(phase) * amplitude;
                phase += step;
                if (phase > Math.PI * 2.0)
                {
                    phase -= Math.PI * 2.0;
                }

                for (var channel = 0; channel < format.Channels; channel++)
                {
                    var offset = (frame * format.Channels + channel) * bytesPerSample;
                    if (isFloat && format.Bits == 32)
                    {
                        Buffer.BlockCopy(BitConverter.GetBytes((float)sample), 0, data, offset, 4);
                    }
                    else if (isPcm && format.Bits == 16)
                    {
                        var value = (short)Math.Clamp((int)(sample * short.MaxValue), short.MinValue, short.MaxValue);
                        var raw = BitConverter.GetBytes(value);
                        data[offset] = raw[0];
                        data[offset + 1] = raw[1];
                    }
                    else if (isPcm && format.Bits == 24)
                    {
                        var value = (int)Math.Clamp(sample * 8_388_607, -8_388_608, 8_388_607);
                        data[offset] = (byte)(value & 0xFF);
                        data[offset + 1] = (byte)((value >> 8) & 0xFF);
                        data[offset + 2] = (byte)((value >> 16) & 0xFF);
                    }
                    else if (isPcm && format.Bits == 32 && format.ValidBits <= 24)
                    {
                        var value = (int)Math.Clamp(sample * 8_388_607, -8_388_608, 8_388_607) << 8;
                        Buffer.BlockCopy(BitConverter.GetBytes(value), 0, data, offset, 4);
                    }
                }
            }
        }
    }

    private readonly CancellationTokenSource cancellation = new();
    private readonly RenderOutput left;
    private readonly RenderOutput right;
    private readonly RenderOutput? third;
    private readonly Task worker;
    private volatile TestToneMode mode;

    public TestTonePlayer(AudioDeviceInfo leftDevice, AudioDeviceInfo rightDevice, AudioDeviceInfo? thirdDevice)
    {
        left = new RenderOutput(leftDevice);
        right = new RenderOutput(rightDevice);
        if (thirdDevice is not null)
        {
            third = new RenderOutput(thirdDevice);
        }

        worker = Task.Run(RenderLoop);
    }

    public TestToneMode Mode
    {
        get => mode;
        set => mode = value;
    }

    public void Dispose()
    {
        cancellation.Cancel();
        try
        {
            worker.Wait(TimeSpan.FromSeconds(1));
        }
        catch
        {
        }

        left.Stop();
        right.Stop();
        third?.Stop();
        cancellation.Dispose();
    }

    private void RenderLoop()
    {
        while (!cancellation.IsCancellationRequested)
        {
            var current = mode;
            if (current is TestToneMode.Left or TestToneMode.Both)
            {
                left.WriteTone(current == TestToneMode.Both ? 523.25 : 440.0, 0.16);
            }

            if (current is TestToneMode.Right or TestToneMode.Both)
            {
                right.WriteTone(current == TestToneMode.Both ? 523.25 : 660.0, 0.16);
            }

            if (third is not null && current is (TestToneMode.Third or TestToneMode.Both))
            {
                third.WriteTone(current == TestToneMode.Both ? 523.25 : 880.0, 0.16);
            }

            Thread.Sleep(5);
        }
    }
}
