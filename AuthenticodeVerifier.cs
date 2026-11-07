using System.Runtime.InteropServices;

namespace CompressionGarmentOrder;

internal static class AuthenticodeVerifier
{
    private static readonly Guid GenericVerifyV2 = new("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");
    private const uint WtdUiNone = 2;
    private const uint WtdRevokeWholeChain = 1;
    private const uint WtdChoiceFile = 1;
    private const uint WtdStateActionVerify = 1;
    private const uint WtdStateActionClose = 2;
    private const uint WtdCacheOnlyUrlRetrieval = 0x1000;

    public static bool IsTrusted(string path)
    {
        var file = new WinTrustFileInfo(path);
        var data = new WinTrustData(file);
        try
        {
            int result = WinVerifyTrust(IntPtr.Zero, GenericVerifyV2, data);
            return result == 0;
        }
        finally
        {
            data.StateAction = WtdStateActionClose;
            _ = WinVerifyTrust(IntPtr.Zero, GenericVerifyV2, data);
            data.Dispose();
            file.Dispose();
        }
    }

    [DllImport("wintrust.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int WinVerifyTrust(IntPtr hwnd, [MarshalAs(UnmanagedType.LPStruct)] Guid actionId,
        [In, Out] WinTrustData data);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private sealed class WinTrustFileInfo : IDisposable
    {
        private readonly IntPtr _pathPointer;
        public uint StructSize = (uint)Marshal.SizeOf<WinTrustFileInfo>();
        public IntPtr FilePath;
        public IntPtr FileHandle = IntPtr.Zero;
        public IntPtr KnownSubject = IntPtr.Zero;

        public WinTrustFileInfo(string path)
        {
            _pathPointer = Marshal.StringToCoTaskMemUni(path);
            FilePath = _pathPointer;
        }

        public void Dispose() => Marshal.FreeCoTaskMem(_pathPointer);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private sealed class WinTrustData : IDisposable
    {
        private readonly IntPtr _fileInfoPointer;
        public uint StructSize = (uint)Marshal.SizeOf<WinTrustData>();
        public IntPtr PolicyCallbackData = IntPtr.Zero;
        public IntPtr SipClientData = IntPtr.Zero;
        public uint UiChoice = WtdUiNone;
        public uint RevocationChecks = WtdRevokeWholeChain;
        public uint UnionChoice = WtdChoiceFile;
        public IntPtr FileInfo;
        public uint StateAction = WtdStateActionVerify;
        public IntPtr StateData = IntPtr.Zero;
        public IntPtr UrlReference = IntPtr.Zero;
        public uint ProviderFlags = WtdCacheOnlyUrlRetrieval;
        public uint UiContext = 0;

        public WinTrustData(WinTrustFileInfo file)
        {
            _fileInfoPointer = Marshal.AllocCoTaskMem(Marshal.SizeOf<WinTrustFileInfo>());
            Marshal.StructureToPtr(file, _fileInfoPointer, false);
            FileInfo = _fileInfoPointer;
        }

        public void Dispose()
        {
            Marshal.DestroyStructure<WinTrustFileInfo>(_fileInfoPointer);
            Marshal.FreeCoTaskMem(_fileInfoPointer);
        }
    }
}
