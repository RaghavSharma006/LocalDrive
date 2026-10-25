using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using System.Security.Cryptography;
using System.IO;

namespace LocalDrive;

internal sealed record TeraBoxSession(string CookieHeader, DateTimeOffset CapturedUtc);

internal sealed class TeraBoxSessionStore(string path)
{
    private const int UiForbidden = 0x1;
    private readonly string filePath = Path.GetFullPath(path);

    internal TeraBoxSession? Load()
    {
        try
        {
            if (!File.Exists(filePath)) return null;
            byte[] protectedBytes = File.ReadAllBytes(filePath);
            byte[] clear = Transform(protectedBytes, protect: false);
            try
            {
                var session = JsonSerializer.Deserialize<TeraBoxSession>(clear);
                return string.IsNullOrWhiteSpace(session?.CookieHeader) ? null : session;
            }
            finally { CryptographicOperations.ZeroMemory(clear); }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or CryptographicException or JsonException or Win32Exception)
        {
            return null;
        }
    }

    internal void Save(TeraBoxSession session)
    {
        ValidateCookie(session.CookieHeader);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        byte[] clear = JsonSerializer.SerializeToUtf8Bytes(session);
        byte[] protectedBytes = Transform(clear, protect: true);
        CryptographicOperations.ZeroMemory(clear);
        string temporary = filePath + ".tmp";
        try
        {
            File.WriteAllBytes(temporary, protectedBytes);
            RestrictFile(temporary);
            File.Move(temporary, filePath, overwrite: true);
            RestrictFile(filePath);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(protectedBytes);
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    internal static void WriteRcloneConfig(TeraBoxSession session, string configPath)
    {
        ValidateCookie(session.CookieHeader);
        string fullPath = Path.GetFullPath(configPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, "[localdrive]\ntype = terabox\ncookie = " + session.CookieHeader + "\n", new UTF8Encoding(false));
        RestrictFile(fullPath);
    }

    internal static void DeleteRcloneConfig(string configPath)
    {
        try { if (File.Exists(configPath)) File.Delete(configPath); } catch { }
    }

    internal static void ValidateCookie(string cookie)
    {
        if (string.IsNullOrWhiteSpace(cookie) || cookie.Contains('\r') || cookie.Contains('\n'))
            throw new InvalidDataException("TeraBox returned an invalid session cookie.");
    }

    private static void RestrictFile(string path)
    {
        string sid = WindowsIdentity.GetCurrent().User?.Value ?? throw new InvalidOperationException("Could not identify the current Windows user.");
        var start = new ProcessStartInfo("icacls.exe") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(path);
        start.ArgumentList.Add("/inheritance:r");
        start.ArgumentList.Add("/grant:r");
        start.ArgumentList.Add("*" + sid + ":(F)");
        using var process = Process.Start(start) ?? throw new IOException("Could not protect the local TeraBox session file.");
        process.WaitForExit();
        if (process.ExitCode != 0) throw new IOException("Could not set private permissions on the local TeraBox session file.");
    }

    private static byte[] Transform(byte[] input, bool protect)
    {
        if (input.Length == 0) throw new InvalidDataException("The saved TeraBox session is empty.");
        IntPtr inputMemory = Marshal.AllocHGlobal(input.Length);
        Marshal.Copy(input, 0, inputMemory, input.Length);
        var inputBlob = new DataBlob { Length = input.Length, Data = inputMemory };
        DataBlob outputBlob = default;
        IntPtr description = IntPtr.Zero;
        try
        {
            bool ok = protect
                ? CryptProtectData(ref inputBlob, "LocalDrive TeraBox session", IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, UiForbidden, out outputBlob)
                : CryptUnprotectData(ref inputBlob, out description, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, UiForbidden, out outputBlob);
            if (!ok) throw new Win32Exception(Marshal.GetLastWin32Error());
            var output = new byte[outputBlob.Length];
            Marshal.Copy(outputBlob.Data, output, 0, output.Length);
            return output;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(input);
            if (inputMemory != IntPtr.Zero) Marshal.FreeHGlobal(inputMemory);
            if (outputBlob.Data != IntPtr.Zero) LocalFree(outputBlob.Data);
            if (description != IntPtr.Zero) LocalFree(description);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob { internal int Length; internal IntPtr Data; }

    [DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptProtectData(ref DataBlob input, string description, IntPtr entropy, IntPtr reserved, IntPtr prompt, int flags, out DataBlob output);

    [DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptUnprotectData(ref DataBlob input, out IntPtr description, IntPtr entropy, IntPtr reserved, IntPtr prompt, int flags, out DataBlob output);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr memory);
}
