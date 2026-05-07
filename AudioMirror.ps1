param(
    [switch]$List,
    [switch]$TestTone,
    [string]$Source = "",
    [string]$Target = "",
    [string]$SecondTarget = "",
    [int]$SourceIndex = -1,
    [int]$TargetIndex = -1,
    [int]$SecondTargetIndex = -1,
    [double]$Gain = 1.0,
    [double]$SecondGain = 1.0,
    [int]$DelayMs = 0,
    [int]$SecondDelayMs = 0,
    [switch]$DebugAudio
)

$ErrorActionPreference = "Stop"

if (-not ([System.Management.Automation.PSTypeName]'AudioMirror.WasapiMirror').Type) {
Add-Type -Language CSharp -TypeDefinition @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace AudioMirror
{
    internal enum EDataFlow
    {
        eRender = 0,
        eCapture = 1,
        eAll = 2
    }

    internal enum ERole
    {
        eConsole = 0,
        eMultimedia = 1,
        eCommunications = 2
    }

    internal enum AUDCLNT_SHAREMODE
    {
        AUDCLNT_SHAREMODE_SHARED = 0,
        AUDCLNT_SHAREMODE_EXCLUSIVE = 1
    }

    [Flags]
    internal enum AUDCLNT_STREAMFLAGS : uint
    {
        NONE = 0,
        LOOPBACK = 0x00020000,
        AUTOCONVERTPCM = 0x80000000,
        SRC_DEFAULT_QUALITY = 0x08000000
    }

    [Flags]
    internal enum AUDCLNT_BUFFERFLAGS : uint
    {
        DATA_DISCONTINUITY = 0x1,
        SILENT = 0x2,
        TIMESTAMP_ERROR = 0x4
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PROPERTYKEY
    {
        public Guid fmtid;
        public uint pid;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PROPVARIANT
    {
        public ushort vt;
        public ushort wReserved1;
        public ushort wReserved2;
        public ushort wReserved3;
        public IntPtr p;
        public int p2;
    }

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    internal class MMDeviceEnumerator
    {
    }

    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDeviceEnumerator
    {
        void EnumAudioEndpoints(EDataFlow dataFlow, uint dwStateMask, out IMMDeviceCollection ppDevices);
        void GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppEndpoint);
        void GetDevice([MarshalAs(UnmanagedType.LPWStr)] string pwstrId, out IMMDevice ppDevice);
        void RegisterEndpointNotificationCallback(IntPtr pClient);
        void UnregisterEndpointNotificationCallback(IntPtr pClient);
    }

    [ComImport]
    [Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDeviceCollection
    {
        void GetCount(out uint pcDevices);
        void Item(uint nDevice, out IMMDevice ppDevice);
    }

    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDevice
    {
        void Activate(ref Guid iid, uint dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
        void OpenPropertyStore(uint stgmAccess, out IPropertyStore ppProperties);
        void GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);
        void GetState(out uint pdwState);
    }

    [ComImport]
    [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPropertyStore
    {
        void GetCount(out uint cProps);
        void GetAt(uint iProp, out PROPERTYKEY pkey);
        void GetValue(ref PROPERTYKEY key, out PROPVARIANT pv);
        void SetValue(ref PROPERTYKEY key, ref PROPVARIANT propvar);
        void Commit();
    }

    [ComImport]
    [Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioClient
    {
        void Initialize(AUDCLNT_SHAREMODE ShareMode, AUDCLNT_STREAMFLAGS StreamFlags, long hnsBufferDuration, long hnsPeriodicity, IntPtr pFormat, ref Guid AudioSessionGuid);
        void GetBufferSize(out uint pNumBufferFrames);
        void GetStreamLatency(out long phnsLatency);
        void GetCurrentPadding(out uint pNumPaddingFrames);
        void IsFormatSupported(AUDCLNT_SHAREMODE ShareMode, IntPtr pFormat, out IntPtr ppClosestMatch);
        void GetMixFormat(out IntPtr ppDeviceFormat);
        void GetDevicePeriod(out long phnsDefaultDevicePeriod, out long phnsMinimumDevicePeriod);
        void Start();
        void Stop();
        void Reset();
        void SetEventHandle(IntPtr eventHandle);
        void GetService(ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);
    }

    [ComImport]
    [Guid("C8ADBD64-E71E-48a0-A4DE-185C395CD317")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioCaptureClient
    {
        void GetBuffer(out IntPtr ppData, out uint pNumFramesToRead, out AUDCLNT_BUFFERFLAGS pdwFlags, out ulong pu64DevicePosition, out ulong pu64QPCPosition);
        void ReleaseBuffer(uint NumFramesRead);
        void GetNextPacketSize(out uint pNumFramesInNextPacket);
    }

    [ComImport]
    [Guid("F294ACFC-3146-4483-A7BF-ADDCA7C260E2")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioRenderClient
    {
        void GetBuffer(uint NumFramesRequested, out IntPtr ppData);
        void ReleaseBuffer(uint NumFramesWritten, AUDCLNT_BUFFERFLAGS dwFlags);
    }

    public static class WasapiMirror
    {
        private const uint DEVICE_STATE_ACTIVE = 0x00000001;
        private const uint CLSCTX_ALL = 23;
        private const long REFTIMES_PER_SEC = 10000000;
        private static readonly Guid IID_IAudioClient = new Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2");
        private static readonly Guid IID_IAudioCaptureClient = new Guid("C8ADBD64-E71E-48a0-A4DE-185C395CD317");
        private static readonly Guid IID_IAudioRenderClient = new Guid("F294ACFC-3146-4483-A7BF-ADDCA7C260E2");
        private static readonly PROPERTYKEY PKEY_Device_FriendlyName = new PROPERTYKEY {
            fmtid = new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"),
            pid = 14
        };

        [DllImport("ole32.dll")]
        private static extern int PropVariantClear(ref PROPVARIANT pvar);

        [DllImport("ole32.dll")]
        private static extern void CoTaskMemFree(IntPtr pv);

        public static void ListRenderDevices()
        {
            string defaultId = GetDefaultRenderId();
            List<DeviceInfo> devices = GetRenderDevices();
            for (int i = 0; i < devices.Count; i++)
            {
                string marker = string.Equals(devices[i].Id, defaultId, StringComparison.OrdinalIgnoreCase) ? " *default" : "";
                Console.WriteLine("{0}: {1}{2}", i, devices[i].Name, marker);
            }
        }

        public static void TestTone(string targetName, int targetIndex)
        {
            List<DeviceInfo> devices = GetRenderDevices();
            DeviceInfo source = PickSource(devices, "", -1);
            DeviceInfo target = PickTarget(devices, source, targetName, targetIndex);

            Console.WriteLine("Tone target: {0}", target.Name);
            Console.WriteLine("Playing 440 Hz tone for 3 seconds.");

            IAudioClient renderAudio = ActivateAudioClient(target.Device);
            IntPtr format = IntPtr.Zero;

            try
            {
                renderAudio.GetMixFormat(out format);
                ushort formatTag = (ushort)Marshal.ReadInt16(format, 0);
                ushort channels = (ushort)Marshal.ReadInt16(format, 2);
                int sampleRate = Marshal.ReadInt32(format, 4);
                ushort blockAlign = (ushort)Marshal.ReadInt16(format, 12);
                ushort bits = (ushort)Marshal.ReadInt16(format, 14);
                ushort validBits = bits;
                Guid subFormat = Guid.Empty;

                if (formatTag == 0xFFFE)
                {
                    validBits = (ushort)Marshal.ReadInt16(format, 22);
                    byte[] guidBytes = new byte[16];
                    Marshal.Copy(IntPtr.Add(format, 24), guidBytes, 0, 16);
                    subFormat = new Guid(guidBytes);
                }

                Console.WriteLine("Format: {0} Hz, {1} ch, {2} bits", sampleRate, channels, bits);

                Guid session = Guid.Empty;
                long bufferDuration = REFTIMES_PER_SEC / 10;
                renderAudio.Initialize(
                    AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_SHARED,
                    AUDCLNT_STREAMFLAGS.AUTOCONVERTPCM | AUDCLNT_STREAMFLAGS.SRC_DEFAULT_QUALITY,
                    bufferDuration,
                    0,
                    format,
                    ref session);

                uint bufferFrames;
                renderAudio.GetBufferSize(out bufferFrames);

                object renderServiceObj;
                Guid renderGuid = IID_IAudioRenderClient;
                renderAudio.GetService(ref renderGuid, out renderServiceObj);
                IAudioRenderClient render = (IAudioRenderClient)renderServiceObj;

                renderAudio.Start();

                int totalFrames = sampleRate * 3;
                int written = 0;
                double phase = 0.0;
                double step = 2.0 * Math.PI * 440.0 / sampleRate;
                byte[] data = new byte[bufferFrames * blockAlign];

                while (written < totalFrames)
                {
                    uint padding;
                    renderAudio.GetCurrentPadding(out padding);
                    uint available = bufferFrames > padding ? bufferFrames - padding : 0;
                    if (available == 0)
                    {
                        Thread.Sleep(3);
                        continue;
                    }

                    uint frames = (uint)Math.Min((int)available, totalFrames - written);
                    int bytes = checked((int)(frames * blockAlign));
                    FillTone(data, bytes, (int)frames, channels, bits, validBits, formatTag, subFormat, ref phase, step);

                    IntPtr targetBuffer;
                    render.GetBuffer(frames, out targetBuffer);
                    Marshal.Copy(data, 0, targetBuffer, bytes);
                    render.ReleaseBuffer(frames, AUDCLNT_BUFFERFLAGS.DATA_DISCONTINUITY & ~AUDCLNT_BUFFERFLAGS.DATA_DISCONTINUITY);

                    written += (int)frames;
                }

                Thread.Sleep(250);
            }
            finally
            {
                TryStop(renderAudio);
                if (format != IntPtr.Zero)
                    CoTaskMemFree(format);
            }
        }

        private static void FillTone(byte[] data, int bytes, int frames, int channels, int bits, int validBits, int formatTag, Guid subFormat, ref double phase, double step)
        {
            Array.Clear(data, 0, bytes);
            bool isFloat = formatTag == 3 || subFormat == new Guid("00000003-0000-0010-8000-00AA00389B71");
            bool isPcm = formatTag == 1 || subFormat == new Guid("00000001-0000-0010-8000-00AA00389B71");
            int bytesPerSample = bits / 8;

            for (int frame = 0; frame < frames; frame++)
            {
                double sample = Math.Sin(phase) * 0.20;
                phase += step;
                if (phase > Math.PI * 2.0)
                    phase -= Math.PI * 2.0;

                for (int ch = 0; ch < channels; ch++)
                {
                    int offset = (frame * channels + ch) * bytesPerSample;
                    if (isFloat && bits == 32)
                    {
                        byte[] value = BitConverter.GetBytes((float)sample);
                        Buffer.BlockCopy(value, 0, data, offset, 4);
                    }
                    else if (isPcm && bits == 16)
                    {
                        short value = (short)(sample * short.MaxValue);
                        byte[] raw = BitConverter.GetBytes(value);
                        data[offset] = raw[0];
                        data[offset + 1] = raw[1];
                    }
                    else if (isPcm && bits == 24)
                    {
                        int value = (int)(sample * 8388607);
                        data[offset] = (byte)(value & 0xFF);
                        data[offset + 1] = (byte)((value >> 8) & 0xFF);
                        data[offset + 2] = (byte)((value >> 16) & 0xFF);
                    }
                    else if (isPcm && bits == 32 && validBits <= 24)
                    {
                        int value = (int)(sample * 8388607) << 8;
                        byte[] raw = BitConverter.GetBytes(value);
                        Buffer.BlockCopy(raw, 0, data, offset, 4);
                    }
                    else
                    {
                        throw new NotSupportedException("Unsupported target mix format for tone test.");
                    }
                }
            }
        }

        private static void ApplyGain(byte[] data, int bytes, int bits, int validBits, int formatTag, Guid subFormat, double gain)
        {
            bool isFloat = formatTag == 3 || subFormat == new Guid("00000003-0000-0010-8000-00AA00389B71");
            bool isPcm = formatTag == 1 || subFormat == new Guid("00000001-0000-0010-8000-00AA00389B71");

            if (isFloat && bits == 32)
            {
                for (int i = 0; i < bytes; i += 4)
                {
                    float value = BitConverter.ToSingle(data, i);
                    value = (float)Math.Max(-1.0, Math.Min(1.0, value * gain));
                    byte[] raw = BitConverter.GetBytes(value);
                    Buffer.BlockCopy(raw, 0, data, i, 4);
                }
                return;
            }

            if (isPcm && bits == 16)
            {
                for (int i = 0; i < bytes; i += 2)
                {
                    short value = BitConverter.ToInt16(data, i);
                    int scaled = (int)Math.Round(value * gain);
                    scaled = Math.Max(short.MinValue, Math.Min(short.MaxValue, scaled));
                    byte[] raw = BitConverter.GetBytes((short)scaled);
                    data[i] = raw[0];
                    data[i + 1] = raw[1];
                }
                return;
            }

            if (isPcm && bits == 32 && validBits <= 24)
            {
                for (int i = 0; i < bytes; i += 4)
                {
                    int value = BitConverter.ToInt32(data, i) >> 8;
                    int scaled = (int)Math.Round(value * gain);
                    scaled = Math.Max(-8388608, Math.Min(8388607, scaled));
                    byte[] raw = BitConverter.GetBytes(scaled << 8);
                    Buffer.BlockCopy(raw, 0, data, i, 4);
                }
            }
        }

        private sealed class DelayBuffer
        {
            private readonly byte[] buffer;
            private int offset;

            public DelayBuffer(int bytes)
            {
                buffer = new byte[bytes];
                offset = 0;
            }

            public void Process(byte[] data, int bytes)
            {
                for (int i = 0; i < bytes; i++)
                {
                    byte delayed = buffer[offset];
                    buffer[offset] = data[i];
                    data[i] = delayed;
                    offset++;
                    if (offset == buffer.Length)
                        offset = 0;
                }
            }
        }

        private sealed class RenderSink
        {
            private readonly IAudioClient audio;
            private readonly IAudioRenderClient render;
            private readonly uint bufferFrames;
            private readonly ushort blockAlign;
            private readonly int bits;
            private readonly int validBits;
            private readonly ushort formatTag;
            private readonly Guid subFormat;
            private readonly double gain;
            private readonly DelayBuffer delayBuffer;
            private byte[] transfer = new byte[0];

            public readonly string Name;
            public long WrittenFrames;
            public long DroppedFrames;

            public RenderSink(DeviceInfo target, IntPtr format, long bufferDuration, ushort blockAlign, int sampleRate, int bits, int validBits, ushort formatTag, Guid subFormat, double gain, int delayMs)
            {
                if (gain <= 0.0 || gain > 8.0)
                    throw new ArgumentOutOfRangeException("gain", "Gain must be greater than 0 and no more than 8.");
                if (delayMs < 0 || delayMs > 2000)
                    throw new ArgumentOutOfRangeException("delayMs", "DelayMs must be from 0 to 2000.");

                Name = target.Name;
                this.blockAlign = blockAlign;
                this.bits = bits;
                this.validBits = validBits;
                this.formatTag = formatTag;
                this.subFormat = subFormat;
                this.gain = gain;

                int delayBytes = checked((int)(((long)sampleRate * delayMs / 1000) * blockAlign));
                delayBuffer = delayBytes > 0 ? new DelayBuffer(delayBytes) : null;

                audio = ActivateAudioClient(target.Device);
                Guid session = Guid.Empty;
                audio.Initialize(
                    AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_SHARED,
                    AUDCLNT_STREAMFLAGS.AUTOCONVERTPCM | AUDCLNT_STREAMFLAGS.SRC_DEFAULT_QUALITY,
                    bufferDuration,
                    0,
                    format,
                    ref session);

                audio.GetBufferSize(out bufferFrames);

                object renderServiceObj;
                Guid renderGuid = IID_IAudioRenderClient;
                audio.GetService(ref renderGuid, out renderServiceObj);
                render = (IAudioRenderClient)renderServiceObj;
            }

            public void Start()
            {
                audio.Start();
            }

            public void Stop()
            {
                TryStop(audio);
            }

            public void Write(IntPtr sourceBuffer, uint frames, AUDCLNT_BUFFERFLAGS flags)
            {
                uint padding;
                audio.GetCurrentPadding(out padding);
                uint available = bufferFrames > padding ? bufferFrames - padding : 0;
                uint framesToWrite = frames < available ? frames : available;
                if (framesToWrite < frames)
                    DroppedFrames += (frames - framesToWrite);

                if (framesToWrite == 0)
                    return;

                IntPtr targetBuffer;
                render.GetBuffer(framesToWrite, out targetBuffer);

                if ((flags & AUDCLNT_BUFFERFLAGS.SILENT) != 0)
                {
                    render.ReleaseBuffer(framesToWrite, AUDCLNT_BUFFERFLAGS.SILENT);
                    WrittenFrames += framesToWrite;
                    return;
                }

                int bytes = checked((int)(framesToWrite * blockAlign));
                if (transfer.Length < bytes)
                    transfer = new byte[bytes];

                Marshal.Copy(sourceBuffer, transfer, 0, bytes);
                if (Math.Abs(gain - 1.0) > 0.001)
                    ApplyGain(transfer, bytes, bits, validBits, formatTag, subFormat, gain);
                if (delayBuffer != null)
                    delayBuffer.Process(transfer, bytes);

                Marshal.Copy(transfer, 0, targetBuffer, bytes);
                render.ReleaseBuffer(framesToWrite, AUDCLNT_BUFFERFLAGS.DATA_DISCONTINUITY & ~AUDCLNT_BUFFERFLAGS.DATA_DISCONTINUITY);
                WrittenFrames += framesToWrite;
            }
        }

        public static void MirrorPair(string sourceName, string firstTargetName, string secondTargetName, int sourceIndex, int firstTargetIndex, int secondTargetIndex, double firstGain, double secondGain, int firstDelayMs, int secondDelayMs, bool debugAudio)
        {
            List<DeviceInfo> devices = GetRenderDevices();
            if (devices.Count < 3)
                throw new InvalidOperationException("Need at least three active playback devices for pair mode.");

            DeviceInfo source = PickSource(devices, sourceName, sourceIndex);
            DeviceInfo firstTarget = PickTarget(devices, source, firstTargetName, firstTargetIndex);
            DeviceInfo secondTarget = PickTarget(devices, source, secondTargetName, secondTargetIndex);

            if (string.Equals(source.Id, firstTarget.Id, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(source.Id, secondTarget.Id, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(firstTarget.Id, secondTarget.Id, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Source and both targets must be different playback devices.");

            Console.WriteLine("Source: {0}", source.Name);
            Console.WriteLine("Target 1: {0}, gain {1:0.##}x, delay {2} ms", firstTarget.Name, firstGain, firstDelayMs);
            Console.WriteLine("Target 2: {0}, gain {1:0.##}x, delay {2} ms", secondTarget.Name, secondGain, secondDelayMs);
            Console.WriteLine("Mirroring to both targets. Press Ctrl+C to stop.");

            IAudioClient captureAudio = ActivateAudioClient(source.Device);
            RenderSink firstSink = null;
            RenderSink secondSink = null;
            IntPtr format = IntPtr.Zero;
            bool stopping = false;
            Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs e) {
                e.Cancel = true;
                stopping = true;
            };

            try
            {
                captureAudio.GetMixFormat(out format);
                ushort formatTag = (ushort)Marshal.ReadInt16(format, 0);
                ushort blockAlign = (ushort)Marshal.ReadInt16(format, 12);
                int channels = Marshal.ReadInt16(format, 2);
                int sampleRate = Marshal.ReadInt32(format, 4);
                int bits = Marshal.ReadInt16(format, 14);
                int validBits = bits;
                Guid subFormat = Guid.Empty;
                if (formatTag == 0xFFFE)
                {
                    validBits = Marshal.ReadInt16(format, 22);
                    byte[] guidBytes = new byte[16];
                    Marshal.Copy(IntPtr.Add(format, 24), guidBytes, 0, 16);
                    subFormat = new Guid(guidBytes);
                }
                Console.WriteLine("Format: {0} Hz, {1} ch, {2} bits", sampleRate, channels, bits);

                Guid session = Guid.Empty;
                long bufferDuration = REFTIMES_PER_SEC / 10;
                captureAudio.Initialize(
                    AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_SHARED,
                    AUDCLNT_STREAMFLAGS.LOOPBACK,
                    bufferDuration,
                    0,
                    format,
                    ref session);

                firstSink = new RenderSink(firstTarget, format, bufferDuration, blockAlign, sampleRate, bits, validBits, formatTag, subFormat, firstGain, firstDelayMs);
                secondSink = new RenderSink(secondTarget, format, bufferDuration, blockAlign, sampleRate, bits, validBits, formatTag, subFormat, secondGain, secondDelayMs);

                object captureServiceObj;
                Guid captureGuid = IID_IAudioCaptureClient;
                captureAudio.GetService(ref captureGuid, out captureServiceObj);
                IAudioCaptureClient capture = (IAudioCaptureClient)captureServiceObj;

                firstSink.Start();
                secondSink.Start();
                captureAudio.Start();

                long packets = 0;
                long capturedFrames = 0;
                DateTime lastDebug = DateTime.UtcNow;

                while (!stopping)
                {
                    uint packetFrames;
                    capture.GetNextPacketSize(out packetFrames);
                    if (packetFrames == 0)
                    {
                        Thread.Sleep(3);
                        continue;
                    }

                    while (packetFrames != 0)
                    {
                        IntPtr sourceBuffer;
                        uint frames;
                        AUDCLNT_BUFFERFLAGS flags;
                        ulong devicePosition;
                        ulong qpcPosition;
                        capture.GetBuffer(out sourceBuffer, out frames, out flags, out devicePosition, out qpcPosition);

                        packets++;
                        capturedFrames += frames;
                        firstSink.Write(sourceBuffer, frames, flags);
                        secondSink.Write(sourceBuffer, frames, flags);

                        capture.ReleaseBuffer(frames);
                        capture.GetNextPacketSize(out packetFrames);
                    }

                    if (debugAudio && (DateTime.UtcNow - lastDebug).TotalSeconds >= 2)
                    {
                        Console.WriteLine(
                            "debug packets={0} capturedFrames={1} target1Written={2} target1Dropped={3} target2Written={4} target2Dropped={5}",
                            packets,
                            capturedFrames,
                            firstSink.WrittenFrames,
                            firstSink.DroppedFrames,
                            secondSink.WrittenFrames,
                            secondSink.DroppedFrames);
                        lastDebug = DateTime.UtcNow;
                    }
                }
            }
            finally
            {
                TryStop(captureAudio);
                if (firstSink != null) firstSink.Stop();
                if (secondSink != null) secondSink.Stop();
                if (format != IntPtr.Zero)
                    CoTaskMemFree(format);
            }
        }

        public static void Mirror(string sourceName, string targetName, int sourceIndex, int targetIndex, double gain, int delayMs, bool debugAudio)
        {
            List<DeviceInfo> devices = GetRenderDevices();
            if (devices.Count < 2)
                throw new InvalidOperationException("Need at least two active playback devices.");

            DeviceInfo source = PickSource(devices, sourceName, sourceIndex);
            DeviceInfo target = PickTarget(devices, source, targetName, targetIndex);

            if (string.Equals(source.Id, target.Id, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Source and target must be different playback devices.");
            if (gain <= 0.0 || gain > 8.0)
                throw new ArgumentOutOfRangeException("gain", "Gain must be greater than 0 and no more than 8.");
            if (delayMs < 0 || delayMs > 2000)
                throw new ArgumentOutOfRangeException("delayMs", "DelayMs must be from 0 to 2000.");

            Console.WriteLine("Source: {0}", source.Name);
            Console.WriteLine("Target: {0}", target.Name);
            Console.WriteLine("Gain: {0:0.##}x", gain);
            Console.WriteLine("Delay: {0} ms", delayMs);
            Console.WriteLine("Mirroring. Press Ctrl+C to stop.");

            IMMDevice srcDevice = source.Device;
            IMMDevice dstDevice = target.Device;

            IAudioClient captureAudio = ActivateAudioClient(srcDevice);
            IAudioClient renderAudio = ActivateAudioClient(dstDevice);

            IntPtr format = IntPtr.Zero;
            byte[] transfer = new byte[0];
            bool stopping = false;
            Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs e) {
                e.Cancel = true;
                stopping = true;
            };

            try
            {
                captureAudio.GetMixFormat(out format);
                ushort formatTag = (ushort)Marshal.ReadInt16(format, 0);
                ushort blockAlign = (ushort)Marshal.ReadInt16(format, 12);
                int channels = Marshal.ReadInt16(format, 2);
                int sampleRate = Marshal.ReadInt32(format, 4);
                int bits = Marshal.ReadInt16(format, 14);
                int validBits = bits;
                int delayBytes = checked((int)(((long)sampleRate * delayMs / 1000) * blockAlign));
                DelayBuffer delayBuffer = delayBytes > 0 ? new DelayBuffer(delayBytes) : null;
                Guid subFormat = Guid.Empty;
                if (formatTag == 0xFFFE)
                {
                    validBits = Marshal.ReadInt16(format, 22);
                    byte[] guidBytes = new byte[16];
                    Marshal.Copy(IntPtr.Add(format, 24), guidBytes, 0, 16);
                    subFormat = new Guid(guidBytes);
                }
                Console.WriteLine("Format: {0} Hz, {1} ch, {2} bits", sampleRate, channels, bits);

                Guid session = Guid.Empty;
                long bufferDuration = REFTIMES_PER_SEC / 10;
                captureAudio.Initialize(
                    AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_SHARED,
                    AUDCLNT_STREAMFLAGS.LOOPBACK,
                    bufferDuration,
                    0,
                    format,
                    ref session);

                session = Guid.Empty;
                renderAudio.Initialize(
                    AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_SHARED,
                    AUDCLNT_STREAMFLAGS.AUTOCONVERTPCM | AUDCLNT_STREAMFLAGS.SRC_DEFAULT_QUALITY,
                    bufferDuration,
                    0,
                    format,
                    ref session);

                uint renderBufferFrames;
                renderAudio.GetBufferSize(out renderBufferFrames);

                object captureServiceObj;
                Guid captureGuid = IID_IAudioCaptureClient;
                captureAudio.GetService(ref captureGuid, out captureServiceObj);
                IAudioCaptureClient capture = (IAudioCaptureClient)captureServiceObj;

                object renderServiceObj;
                Guid renderGuid = IID_IAudioRenderClient;
                renderAudio.GetService(ref renderGuid, out renderServiceObj);
                IAudioRenderClient render = (IAudioRenderClient)renderServiceObj;

                renderAudio.Start();
                captureAudio.Start();

                long packets = 0;
                long capturedFrames = 0;
                long writtenFrames = 0;
                long droppedFrames = 0;
                DateTime lastDebug = DateTime.UtcNow;

                while (!stopping)
                {
                    uint packetFrames;
                    capture.GetNextPacketSize(out packetFrames);
                    if (packetFrames == 0)
                    {
                        Thread.Sleep(3);
                        continue;
                    }

                    while (packetFrames != 0)
                    {
                        IntPtr sourceBuffer;
                        uint frames;
                        AUDCLNT_BUFFERFLAGS flags;
                        ulong devicePosition;
                        ulong qpcPosition;
                        capture.GetBuffer(out sourceBuffer, out frames, out flags, out devicePosition, out qpcPosition);

                        uint padding;
                        renderAudio.GetCurrentPadding(out padding);
                        uint available = renderBufferFrames > padding ? renderBufferFrames - padding : 0;
                        uint framesToWrite = frames < available ? frames : available;
                        packets++;
                        capturedFrames += frames;
                        if (framesToWrite < frames)
                            droppedFrames += (frames - framesToWrite);

                        if (framesToWrite > 0)
                        {
                            IntPtr targetBuffer;
                            render.GetBuffer(framesToWrite, out targetBuffer);

                            AUDCLNT_BUFFERFLAGS renderFlags = AUDCLNT_BUFFERFLAGS.DATA_DISCONTINUITY;
                            if ((flags & AUDCLNT_BUFFERFLAGS.SILENT) != 0)
                            {
                                render.ReleaseBuffer(framesToWrite, AUDCLNT_BUFFERFLAGS.SILENT);
                            }
                            else
                            {
                                int bytes = checked((int)(framesToWrite * blockAlign));
                                if (transfer.Length < bytes)
                                    transfer = new byte[bytes];
                                Marshal.Copy(sourceBuffer, transfer, 0, bytes);
                                if (Math.Abs(gain - 1.0) > 0.001)
                                    ApplyGain(transfer, bytes, bits, validBits, formatTag, subFormat, gain);
                                if (delayBuffer != null)
                                    delayBuffer.Process(transfer, bytes);
                                Marshal.Copy(transfer, 0, targetBuffer, bytes);
                                render.ReleaseBuffer(framesToWrite, renderFlags & ~AUDCLNT_BUFFERFLAGS.DATA_DISCONTINUITY);
                            }
                            writtenFrames += framesToWrite;
                        }

                        capture.ReleaseBuffer(frames);
                        capture.GetNextPacketSize(out packetFrames);
                    }

                    if (debugAudio && (DateTime.UtcNow - lastDebug).TotalSeconds >= 2)
                    {
                        Console.WriteLine(
                            "debug packets={0} capturedFrames={1} writtenFrames={2} droppedFrames={3}",
                            packets,
                            capturedFrames,
                            writtenFrames,
                            droppedFrames);
                        lastDebug = DateTime.UtcNow;
                    }
                }
            }
            finally
            {
                TryStop(captureAudio);
                TryStop(renderAudio);
                if (format != IntPtr.Zero)
                    CoTaskMemFree(format);
            }
        }

        private static void TryStop(IAudioClient client)
        {
            try { if (client != null) client.Stop(); } catch { }
        }

        private static IAudioClient ActivateAudioClient(IMMDevice device)
        {
            object audioClientObj;
            Guid iid = IID_IAudioClient;
            device.Activate(ref iid, CLSCTX_ALL, IntPtr.Zero, out audioClientObj);
            return (IAudioClient)audioClientObj;
        }

        private static DeviceInfo PickSource(List<DeviceInfo> devices, string name, int index)
        {
            if (index >= 0)
                return PickIndex(devices, index, "source");
            if (!string.IsNullOrWhiteSpace(name))
                return PickName(devices, name, "source");
            string defaultId = GetDefaultRenderId();
            foreach (DeviceInfo device in devices)
            {
                if (string.Equals(device.Id, defaultId, StringComparison.OrdinalIgnoreCase))
                    return device;
            }
            return devices[0];
        }

        private static DeviceInfo PickTarget(List<DeviceInfo> devices, DeviceInfo source, string name, int index)
        {
            if (index >= 0)
                return PickIndex(devices, index, "target");
            if (!string.IsNullOrWhiteSpace(name))
                return PickName(devices, name, "target");
            foreach (DeviceInfo device in devices)
            {
                if (!string.Equals(device.Id, source.Id, StringComparison.OrdinalIgnoreCase))
                    return device;
            }
            throw new InvalidOperationException("No target playback device found.");
        }

        private static DeviceInfo PickIndex(List<DeviceInfo> devices, int index, string role)
        {
            if (index < 0 || index >= devices.Count)
                throw new ArgumentOutOfRangeException(role + " index", "Index is outside the playback device list.");
            return devices[index];
        }

        private static DeviceInfo PickName(List<DeviceInfo> devices, string text, string role)
        {
            foreach (DeviceInfo device in devices)
            {
                if (device.Name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                    return device;
            }
            throw new InvalidOperationException("No " + role + " playback device contains: " + text);
        }

        private static string GetDefaultRenderId()
        {
            IMMDeviceEnumerator enumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
            IMMDevice device;
            enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eConsole, out device);
            string id;
            device.GetId(out id);
            return id;
        }

        private static List<DeviceInfo> GetRenderDevices()
        {
            IMMDeviceEnumerator enumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
            IMMDeviceCollection collection;
            enumerator.EnumAudioEndpoints(EDataFlow.eRender, DEVICE_STATE_ACTIVE, out collection);

            uint count;
            collection.GetCount(out count);
            List<DeviceInfo> devices = new List<DeviceInfo>();
            for (uint i = 0; i < count; i++)
            {
                IMMDevice device;
                collection.Item(i, out device);
                string id;
                device.GetId(out id);
                devices.Add(new DeviceInfo {
                    Device = device,
                    Id = id,
                    Name = GetFriendlyName(device)
                });
            }
            return devices;
        }

        private static string GetFriendlyName(IMMDevice device)
        {
            IPropertyStore store;
            device.OpenPropertyStore(0, out store);
            PROPERTYKEY key = PKEY_Device_FriendlyName;
            PROPVARIANT value;
            store.GetValue(ref key, out value);
            try
            {
                if (value.vt == 31 && value.p != IntPtr.Zero)
                    return Marshal.PtrToStringUni(value.p);
                return "(unnamed playback device)";
            }
            finally
            {
                PropVariantClear(ref value);
            }
        }

        private sealed class DeviceInfo
        {
            public IMMDevice Device;
            public string Id;
            public string Name;
        }
    }
}
"@
}

if ($List) {
    [AudioMirror.WasapiMirror]::ListRenderDevices()
    exit
}

if ($TestTone) {
    [AudioMirror.WasapiMirror]::TestTone($Target, $TargetIndex)
    exit
}

if ($SecondTargetIndex -ge 0 -or $SecondTarget) {
    [AudioMirror.WasapiMirror]::MirrorPair($Source, $Target, $SecondTarget, $SourceIndex, $TargetIndex, $SecondTargetIndex, $Gain, $SecondGain, $DelayMs, $SecondDelayMs, [bool]$DebugAudio)
    exit
}

[AudioMirror.WasapiMirror]::Mirror($Source, $Target, $SourceIndex, $TargetIndex, $Gain, $DelayMs, [bool]$DebugAudio)
