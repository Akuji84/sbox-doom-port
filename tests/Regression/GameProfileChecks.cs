using ManagedDoom;
using System.Text;

static class GameProfileChecks
{
    public static void Verify(string root)
    {
        var directory = Path.Combine(Path.GetTempPath(), "sdoom-profiles-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var paths = new List<string>();
        var health = DoomInfo.MobjInfos[0].SpawnHealth;
        var oldPaths = SboxManagedDoomFileSystem.HostWadPaths;
        try
        {
            string Fixture(string name, params string[] lumps)
            {
                var path = Path.Combine(directory, name + ".wad");
                using var writer = new BinaryWriter(File.Create(path));
                writer.Write(Encoding.ASCII.GetBytes("IWAD"));
                writer.Write(lumps.Length); writer.Write(12);
                foreach (var lump in lumps)
                {
                    writer.Write(12); writer.Write(0);
                    var encoded = new byte[8]; Encoding.ASCII.GetBytes(lump).CopyTo(encoded, 0); writer.Write(encoded);
                }
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
            var signature = new[] { "E1M1", "MUS_E1M1", "M_HTIC", "ARTIBOX", "SPFLY0" };
            var detected = Fixture("custom-heretic", signature);
            using (var automatic = new Wad(detected))
                if (automatic.Profile != GameProfile.Heretic) throw new Exception("Renamed Heretic base not detected.");
            Reject(() => { using var content = GameContent.CreateDummy(detected); });
            using (var addon = new Wad(renamed, detected))
                if (addon.Profile != GameProfile.Doom) throw new Exception("Add-on signature changed empty base profile.");
            var partial = Fixture("partial", signature.Take(4).ToArray());
            var tail = Fixture("tail", signature.Skip(4).ToArray());
            using (var split = new Wad(partial, tail))
                if (split.Profile != GameProfile.Doom) throw new Exception("Signature borrowed lumps from add-on.");
            foreach (var missing in signature)
            {
                using var incomplete = new Wad(Fixture("missing-" + missing, signature.Where(n => n != missing).ToArray()));
                if (incomplete.Profile != GameProfile.Doom) throw new Exception("Partial signature misclassified Doom content.");
            }
            var realRenamed = Path.Combine(directory, "renamed-release.wad"); paths.Add(realRenamed);
            File.Copy(Path.Combine(root, "Assets/doom/blasphem.wad"), realRenamed);
            using (var real = new Wad(realRenamed))
                if (real.Profile != GameProfile.Heretic) throw new Exception("Real renamed IWAD not recognized.");
            using (var doomWithAddon = new Wad(Path.Combine(root, "Assets/doom/freedoom1.wad"), detected))
                if (doomWithAddon.Profile != GameProfile.Doom) throw new Exception("Heretic add-on markers changed real Doom base.");
            using (var preview = GameContent.CreateHereticPreview(realRenamed, tail))
                if (preview.Wad.Profile != GameProfile.Heretic || preview.Wad.Names.Count != 2)
                    throw new Exception("Renamed Heretic preview/add-on loading failed.");
            var detectedRejected = false;
            try { using var invalid = new Wad(GameProfile.Doom, detected); }
            catch (ArgumentException) { detectedRejected = true; }
            if (!detectedRejected) throw new Exception("Doom override accepted detected Heretic content.");
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
