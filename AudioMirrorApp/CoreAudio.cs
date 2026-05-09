using System.Runtime.InteropServices;

namespace AudioMirrorApp;

internal static class CoreAudio
{
    public enum EDataFlow
    {
        Render = 0,
        Capture = 1,
        All = 2
    }

    public enum ERole
    {
        Console = 0,
        Multimedia = 1,
        Communications = 2
    }

    public enum ShareMode
    {
        Shared = 0,
        Exclusive = 1
    }

    [Flags]
    public enum StreamFlags : uint
    {
        None = 0,
        Loopback = 0x00020000,
        AutoconvertPcm = 0x80000000,
        SrcDefaultQuality = 0x08000000
    }

    [Flags]
    public enum BufferFlags : uint
    {
        None = 0,
        DataDiscontinuity = 0x1,
        Silent = 0x2,
        TimestampError = 0x4
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PropertyKey
    {
        public Guid FmtId;
        public uint Pid;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PropVariant
    {
        public ushort Vt;
        public ushort Reserved1;
        public ushort Reserved2;
        public ushort Reserved3;
        public IntPtr Pointer;
        public int Pointer2;
    }

    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMMDeviceEnumerator
    {
        void EnumAudioEndpoints(EDataFlow dataFlow, uint stateMask, out IMMDeviceCollection devices);
        void GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice endpoint);
        void GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IMMDevice device);
        void RegisterEndpointNotificationCallback(IntPtr client);
        void UnregisterEndpointNotificationCallback(IntPtr client);
    }

    [ComImport]
    [Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMMDeviceCollection
    {
        void GetCount(out uint count);
        void Item(uint index, out IMMDevice device);
    }

    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMMDevice
    {
        void Activate(ref Guid iid, uint clsCtx, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object activatedInterface);
        void OpenPropertyStore(uint accessMode, out IPropertyStore properties);
        void GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
        void GetState(out uint state);
    }

    [ComImport]
    [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IPropertyStore
    {
        void GetCount(out uint properties);
        void GetAt(uint property, out PropertyKey key);
        void GetValue(ref PropertyKey key, out PropVariant value);
        void SetValue(ref PropertyKey key, ref PropVariant value);
        void Commit();
    }

    [ComImport]
    [Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAudioClient
    {
        void Initialize(ShareMode shareMode, StreamFlags streamFlags, long bufferDuration, long periodicity, IntPtr format, ref Guid sessionGuid);
        void GetBufferSize(out uint frames);
        void GetStreamLatency(out long latency);
        void GetCurrentPadding(out uint frames);
        void IsFormatSupported(ShareMode shareMode, IntPtr format, out IntPtr closestMatch);
        void GetMixFormat(out IntPtr format);
        void GetDevicePeriod(out long defaultPeriod, out long minimumPeriod);
        void Start();
        void Stop();
        void Reset();
        void SetEventHandle(IntPtr eventHandle);
        void GetService(ref Guid iid, [MarshalAs(UnmanagedType.IUnknown)] out object service);
    }

    [ComImport]
    [Guid("C8ADBD64-E71E-48a0-A4DE-185C395CD317")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAudioCaptureClient
    {
        void GetBuffer(out IntPtr data, out uint framesToRead, out BufferFlags flags, out ulong devicePosition, out ulong qpcPosition);
        void ReleaseBuffer(uint framesRead);
        void GetNextPacketSize(out uint framesInNextPacket);
    }

    [ComImport]
    [Guid("F294ACFC-3146-4483-A7BF-ADDCA7C260E2")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAudioRenderClient
    {
        void GetBuffer(uint framesRequested, out IntPtr data);
        void ReleaseBuffer(uint framesWritten, BufferFlags flags);
    }

    public const uint DeviceStateActive = 0x00000001;
    public const uint DeviceStateDisabled = 0x00000002;
    public const uint DeviceStateNotPresent = 0x00000004;
    public const uint DeviceStateUnplugged = 0x00000008;
    public const uint DeviceStateUsable = DeviceStateActive | DeviceStateNotPresent | DeviceStateUnplugged;
    public const uint ClsCtxAll = 23;
    public const long RefTimesPerSecond = 10_000_000;

    public static readonly Guid IAudioClientId = new("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2");
    public static readonly Guid IAudioCaptureClientId = new("C8ADBD64-E71E-48a0-A4DE-185C395CD317");
    public static readonly Guid IAudioRenderClientId = new("F294ACFC-3146-4483-A7BF-ADDCA7C260E2");
    private static readonly Guid MMDeviceEnumeratorId = new("BCDE0395-E52F-467C-8E3D-C4579291692E");
    public static readonly Guid IeeeFloatSubFormat = new("00000003-0000-0010-8000-00AA00389B71");
    public static readonly Guid PcmSubFormat = new("00000001-0000-0010-8000-00AA00389B71");

    private static readonly PropertyKey FriendlyNameKey = new()
    {
        FmtId = new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"),
        Pid = 14
    };

    [DllImport("ole32.dll")]
    private static extern int PropVariantClear(ref PropVariant variant);

    [DllImport("ole32.dll")]
    public static extern void CoTaskMemFree(IntPtr pointer);

    public static IReadOnlyList<AudioDeviceInfo> GetRenderDevices()
    {
        var enumerator = CreateDeviceEnumerator();
        enumerator.EnumAudioEndpoints(EDataFlow.Render, DeviceStateUsable, out var collection);
        var defaultId = string.Empty;
        try
        {
            enumerator.GetDefaultAudioEndpoint(EDataFlow.Render, ERole.Console, out var defaultDevice);
            defaultDevice.GetId(out defaultId);
        }
        catch
        {
            // Windows can briefly have no default render endpoint while displays wake up.
        }

        collection.GetCount(out var count);
        var devices = new List<AudioDeviceInfo>();
        for (uint i = 0; i < count; i++)
        {
            collection.Item(i, out var device);
            device.GetId(out var id);
            device.GetState(out var state);
            devices.Add(new AudioDeviceInfo
            {
                Index = (int)i,
                Id = id,
                Name = GetFriendlyName(device),
                IsDefault = string.Equals(id, defaultId, StringComparison.OrdinalIgnoreCase),
                State = state,
                Device = device
            });
        }

        return devices;
    }

    public static IAudioClient ActivateAudioClient(IMMDevice device)
    {
        var iid = IAudioClientId;
        device.Activate(ref iid, ClsCtxAll, IntPtr.Zero, out var audioClient);
        return (IAudioClient)audioClient;
    }

    public static AudioFormatInfo GetMixFormat(IMMDevice device)
    {
        var client = ActivateAudioClient(device);
        IntPtr format = IntPtr.Zero;
        try
        {
            client.GetMixFormat(out format);
            return AudioFormatInfo.FromPointer(format);
        }
        finally
        {
            if (format != IntPtr.Zero)
            {
                CoTaskMemFree(format);
            }
        }
    }

    private static IMMDeviceEnumerator CreateDeviceEnumerator()
    {
        var type = Type.GetTypeFromCLSID(MMDeviceEnumeratorId, throwOnError: true)
            ?? throw new InvalidOperationException("MMDeviceEnumerator COM class was not found.");
        return (IMMDeviceEnumerator)(Activator.CreateInstance(type)
            ?? throw new InvalidOperationException("MMDeviceEnumerator COM class could not be created."));
    }

    private static string GetFriendlyName(IMMDevice device)
    {
        device.OpenPropertyStore(0, out var store);
        var key = FriendlyNameKey;
        store.GetValue(ref key, out var value);
        try
        {
            return value.Vt == 31 && value.Pointer != IntPtr.Zero
                ? Marshal.PtrToStringUni(value.Pointer) ?? "(unnamed playback device)"
                : "(unnamed playback device)";
        }
        finally
        {
            PropVariantClear(ref value);
        }
    }
}
