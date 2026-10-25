using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace LocalDrive;

internal sealed record ExecutableProbe(bool ReadOpenSucceeded, int? ReadOpenError, bool ImageSectionSucceeded, int? ImageSectionError);

internal static class ExecutableProbeRunner
{
    private const uint GenericRead = 0x80000000;
    private const uint ShareAll = 0x1 | 0x2 | 0x4;
    private const uint OpenExisting = 3;
    private const uint PageReadOnly = 0x02;
    private const uint SecImage = 0x01000000;
    private const int ErrorInvalidHandle = 6;

    internal static ExecutableProbe Probe(string path)
    {
        using SafeFileHandle file = CreateFile(path, GenericRead, ShareAll, IntPtr.Zero, OpenExisting, 0, IntPtr.Zero);
        if (file.IsInvalid)
        {
            int error = Marshal.GetLastWin32Error();
            return new(false, error, false, error);
        }

        using SafeFileMappingHandle image = CreateFileMapping(file, IntPtr.Zero, PageReadOnly | SecImage, 0, 0, null);
        if (image.IsInvalid)
        {
            int error = Marshal.GetLastWin32Error();
            return new(true, null, false, error == ErrorInvalidHandle ? ErrorInvalidHandle : error);
        }
        return new(true, null, true, null);
    }

    [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(string fileName, uint desiredAccess, uint shareMode,
        IntPtr securityAttributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern SafeFileMappingHandle CreateFileMapping(SafeFileHandle file, IntPtr attributes,
        uint protect, uint maximumSizeHigh, uint maximumSizeLow, string? name);

    private sealed class SafeFileMappingHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeFileMappingHandle() : base(true) { }
        protected override bool ReleaseHandle() => CloseHandle(handle);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);
}
