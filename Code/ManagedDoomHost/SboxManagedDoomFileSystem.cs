using System;
using System.IO;
using System.Text;
using Sandbox;

namespace ManagedDoom
{
    public static class SboxManagedDoomFileSystem
    {
        private static string[] hostWadPaths = Array.Empty<string>();
        private const string DataRoot = "managed-doom";

        public static void SetHostWadPaths(params string[] paths)
        {
            hostWadPaths = paths ?? Array.Empty<string>();
        }

        public static string[] HostWadPaths => hostWadPaths;

        public static byte[] ReadAllBytes(string path)
        {
            var normalized = Normalize(path);

            if (TryReadMountedBytes(normalized, out var bytes))
            {
                return bytes;
            }

            var assetPrefixed = Normalize($"Assets/{normalized}");
            if (TryReadMountedBytes(assetPrefixed, out bytes))
            {
                return bytes;
            }

            throw new FileNotFoundException($"Could not find file '/{normalized}'.");
        }

        public static bool DataFileExists(string path)
        {
            return FileSystem.Data.FileExists(GetDataPath(path));
        }

        public static string ReadAllTextFromData(string path)
        {
            return FileSystem.Data.ReadAllText(GetDataPath(path));
        }

        public static void WriteAllTextToData(string path, string text)
        {
            FileSystem.Data.WriteAllText(GetDataPath(path), text ?? string.Empty);
        }

        public static void WriteAllBytesToData(string path, byte[] data)
        {
            var base64 = System.Convert.ToBase64String(data);
            FileSystem.Data.WriteAllText(GetDataPath(path), base64);
        }

        public static byte[] ReadAllBytesFromData(string path)
        {
            var base64 = FileSystem.Data.ReadAllText(GetDataPath(path));
            return System.Convert.FromBase64String(base64);
        }

        public static string GetDataPath(string path)
        {
            var normalized = Normalize(path);
            if (string.IsNullOrEmpty(normalized))
            {
                return DataRoot;
            }

            return $"{DataRoot}/{normalized}";
        }

        public static string Normalize(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            return path.Replace('\\', '/').TrimStart('/');
        }

        public static string GetFileName(string path)
        {
            var normalized = Normalize(path);
            var slash = normalized.LastIndexOf('/');
            return slash >= 0 ? normalized[(slash + 1)..] : normalized;
        }

        public static string GetFileNameWithoutExtension(string path)
        {
            var fileName = GetFileName(path);
            var dot = fileName.LastIndexOf('.');
            return dot > 0 ? fileName[..dot] : fileName;
        }

        public static string GetExtension(string path)
        {
            var fileName = GetFileName(path);
            var dot = fileName.LastIndexOf('.');
            return dot >= 0 ? fileName[dot..] : string.Empty;
        }

        private static bool TryReadMountedBytes(string path, out byte[] bytes)
        {
            try
            {
                if (FileSystem.Mounted.FileExists(path))
                {
                    bytes = FileSystem.Mounted.ReadAllBytes(path).ToArray();
                    return true;
                }
            }
            catch
            {
            }

            bytes = null;
            return false;
        }
    }
}
