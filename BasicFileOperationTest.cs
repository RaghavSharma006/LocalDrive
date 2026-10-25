using System.IO;
using System.Security.Cryptography;

namespace LocalDrive;

internal static class BasicFileOperationTest
{
    internal static int Run()
    {
        string root = Path.GetFullPath("T:\\");
        if (!Directory.Exists(root)) throw new IOException("T: is not mounted");
        string testDirectory = Path.Combine(root, "LocalDrive-file-test-" + Guid.NewGuid().ToString("N"));
        string localSource = Path.Combine(Path.GetTempPath(), "LocalDrive-source-" + Guid.NewGuid().ToString("N") + ".bin");
        string localCopy = Path.Combine(Path.GetTempPath(), "LocalDrive-copy-" + Guid.NewGuid().ToString("N") + ".bin");
        bool created = false;
        try
        {
            Directory.CreateDirectory(testDirectory); created = true;
            byte[] expected = RandomNumberGenerator.GetBytes(64 * 1024);
            string cloudPath = Path.Combine(testDirectory, "copy-test.bin");
            File.WriteAllBytes(localSource, expected);
            File.Copy(localSource, cloudPath);
            bool hashMatches = SHA256.HashData(File.ReadAllBytes(cloudPath)).SequenceEqual(SHA256.HashData(expected));
            File.Copy(cloudPath, localCopy);
            bool roundTripMatches = File.ReadAllBytes(localCopy).SequenceEqual(expected);
            File.Delete(cloudPath);
            bool deleteSucceeded = !File.Exists(cloudPath);
            Console.WriteLine($"Cloud copy hash: {(hashMatches ? "PASS" : "FAIL")}; copy back: {(roundTripMatches ? "PASS" : "FAIL")}; delete: {(deleteSucceeded ? "PASS" : "FAIL")}");
            return hashMatches && roundTripMatches && deleteSucceeded ? 0 : 1;
        }
        finally
        {
            if (created && Directory.Exists(testDirectory))
            {
                string marker = Path.Combine(testDirectory, "copy-test.bin");
                if (File.Exists(marker)) File.Delete(marker);
                Directory.Delete(testDirectory, false);
            }
            if (File.Exists(localSource)) File.Delete(localSource);
            if (File.Exists(localCopy)) File.Delete(localCopy);
        }
    }
}
