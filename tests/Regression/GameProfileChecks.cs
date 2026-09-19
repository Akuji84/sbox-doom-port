using ManagedDoom;
using System.Text;

static class GameProfileChecks
{
    public static void Verify()
    {
        var directory = Path.Combine(Path.GetTempPath(), "sdoom-profiles-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var paths = new List<string>();
        var health = DoomInfo.MobjInfos[0].SpawnHealth;
        var oldPaths = SboxManagedDoomFileSystem.HostWadPaths;
        try
        {
            string Fixture(string name)
            {
                var path = Path.Combine(directory, name + ".wad");
                using var writer = new BinaryWriter(File.Create(path));
                writer.Write(Encoding.ASCII.GetBytes("IWAD"));
                writer.Write(0); writer.Write(12);
                paths.Add(path);
                return path;
            }
            void Reject(Action action)
            {
                try { action(); }
                catch (NotSupportedException e) when (e.Message.Contains("Heretic/Blasphemer")) { return; }
                throw new Exception("Heretic content reached Doom initialization or an unrelated failure.");
            }
            DoomInfo.MobjInfos[0].SpawnHealth = 12345;
            foreach (var name in new[] { "heretic", "heretic1", "blasphem", "blasphemer", "blasphdm" })
            {
                var path = Fixture(name);
                using var wad = new Wad(path);
                if (wad.Profile != GameProfile.Heretic || wad.Profile.RuntimeSupported)
                    throw new Exception("Heretic profile detection/availability is incorrect.");
                SboxManagedDoomFileSystem.SetHostWadPaths(path);
                var args = new CommandLineArgs(new[] { "-iwad", path });
                Reject(() => { using var content = new GameContent(args); });
                Reject(() => { using var content = GameContent.CreateDummy(path); });
                if (DoomInfo.MobjInfos[0].SpawnHealth != 12345)
                    throw new Exception("Rejected content changed Doom definitions.");
            }
            var renamed = Fixture("renamed");
            SboxManagedDoomFileSystem.SetHostWadPaths(renamed);
            Reject(() => { using var content = new GameContent(new CommandLineArgs(new[] { "-iwad", renamed }), GameProfile.Heretic); });
            using var combined = new Wad(renamed, paths[0]);
            if (combined.Profile != GameProfile.Doom)
                throw new Exception("An add-on filename changed the base game's family.");
            var rejected = false;
            try { using var invalid = new Wad(GameProfile.Doom, paths[0]); }
            catch (ArgumentException) { rejected = true; }
            if (!rejected) throw new Exception("Explicit Doom profile accepted a known Heretic base WAD.");
            if (DoomInfo.MobjInfos[0].SpawnHealth != 12345)
                throw new Exception("Profile selection changed Doom definitions.");
            Console.WriteLine("PASS Heretic selection, explicit override, early rejection, add-on isolation and unchanged Doom definitions");
        }
        finally
        {
            DoomInfo.MobjInfos[0].SpawnHealth = health;
            SboxManagedDoomFileSystem.SetHostWadPaths(oldPaths);
            foreach (var path in paths) File.Delete(path);
            Directory.Delete(directory);
        }
    }
}
