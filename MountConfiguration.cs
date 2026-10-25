namespace LocalDrive;

internal static class MountConfiguration
{
    internal const string FilePermissions = "0755";
    internal const string DirectoryPermissions = "0755";

    internal static string[] CommonArguments(string cacheDirectory, int cacheSizeGb = 2) =>
    [
        "--file-perms", FilePermissions,
        "--dir-perms", DirectoryPermissions,
        "--vfs-cache-mode", "full",
        "--cache-dir", cacheDirectory,
        "--vfs-cache-max-size", Math.Clamp(cacheSizeGb, 1, 4096).ToString(System.Globalization.CultureInfo.InvariantCulture) + "G"
    ];
}
