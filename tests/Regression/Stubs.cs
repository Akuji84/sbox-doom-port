// Only external engine/network/storage boundaries are stubbed. Tests compile
// the production Doom engine, serializer, configuration and input adapter.
namespace Sandbox
{
    public struct Vector2 { public float x, y; }
    public static class Input
    {
        public static Vector2 MouseDelta;
        public static class Keyboard
        {
            public static readonly HashSet<string> Keys = new();
            public static bool Down(string key) => Keys.Contains(key);
        }
    }
    internal static class SboxManagedDoomShellBridgeService
    {
        internal sealed class ShellLaunchConfig { public ShellControlsPayload Controls {get;set;} public double? ShellVolume {get;set;} }
        internal sealed class ShellControlsPayload { public Dictionary<string,string> Bindings {get;set;} public ShellControlsSettings Settings {get;set;} }
        internal sealed class ShellControlsSettings { public int MouseSensitivity {get;set;} = 5; public bool AlwaysRun {get;set;} = true; public bool ShowMessages {get;set;} = true; public bool Music {get;set;} = true; public bool Sfx {get;set;} = true; }
    }
}
namespace ManagedDoom
{
    public static class SboxManagedDoomFileSystem
    {
        private static readonly Dictionary<string, byte[]> data = new();
        public static string[] HostWadPaths {get; private set;} = Array.Empty<string>();
        public static void SetHostWadPaths(params string[] paths) => HostWadPaths = paths;
        public static byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);
        public static bool DataFileExists(string path) => data.ContainsKey(path);
        public static string ReadAllTextFromData(string path) => System.Text.Encoding.UTF8.GetString(data[path]);
        public static void WriteAllTextToData(string path,string text) => data[path] = System.Text.Encoding.UTF8.GetBytes(text);
        public static byte[] ReadAllBytesFromData(string path) => data[path];
        public static void WriteAllBytesToData(string path,byte[] bytes) => data[path] = bytes;
        public static string GetFileName(string path) => Path.GetFileName(path);
        public static string GetFileNameWithoutExtension(string path) => Path.GetFileNameWithoutExtension(path);
        public static string GetExtension(string path) => Path.GetExtension(path);
    }
    public static class SboxManagedDoomBugReportService
    {
        public static bool TryConsumeResult(out string result) { result = ""; return false; }
        public static void QueueSubmit(params string[] args) {}
    }
    public static class SboxManagedDoomMultiplayerPanelService { public static void Open() {} }
    public static class SboxManagedDoomLeaderboardService
    {
        public static void QueueRefresh() {}
        public static bool IsLoading => false;
        public static string Error => "";
        public static TestStats Stats => new();
        public static TestProfile Profile => new();
    }
    public class TestStats { public int Kills,Deaths,Items,Secrets,TimePlayedSeconds,LevelsCompleted; }
    public class TestProfile { public string DisplayName = ""; }
    public class SboxManagedDoomMultiplayerSessionComponent
    {
        public bool PvpActive,CoopActive;
        public string PvpMap,CoopMap;
        public int PvpLaunchSerial,CoopLaunchSerial,CoopSkill;
    }
}
