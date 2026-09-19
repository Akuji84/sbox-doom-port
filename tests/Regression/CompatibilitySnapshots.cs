using ManagedDoom;
using System.Security.Cryptography;
using System.Text.Json;

static class CompatibilitySnapshots
{
    // Captured before introducing game profiles. Deliberate behavior changes
    // require reviewing these baselines, not automatically regenerating them.
    public static void Verify(string root, bool record)
    {
        var actual = new SortedDictionary<string, string>();
        foreach (var name in new[] { "freedoom1", "freedoom2", "freedm", "fsfc1", "fssc1" })
        {
            var path = Path.Combine(root, "Assets/doom/" + name + ".wad");
            SboxManagedDoomFileSystem.SetHostWadPaths(path);
            var args = new CommandLineArgs(new[] { "-iwad", path });
            using var content = new GameContent(args);
            if (content.Profile != GameProfile.Doom || content.Profile.Family != GameFamily.Doom)
                throw new Exception(name + " selected incorrect game rules.");
            var config = new Config();
            var renderer = new ManagedDoom.Video.Renderer(config, content);
            var doom = new Doom(args, config, content, null, null, null, null);
            doom.NewGame(GameSkill.Medium, 1, 1);
            for (var tic = 0; tic < 100; tic++) doom.Update();
            // Menu wipe duration has a wall-clock seed. Reset the simulation
            // after it finishes so snapshots always cover the same game ticks.
            doom.Game.InitNew(GameSkill.Medium, 1, 1);
            doom.Game.RestoreAfterLoad(0);
            var commands = Enumerable.Range(0, 4).Select(_ => new TicCmd()).ToArray();
            for (var tic = 0; tic < 210; tic++)
            {
                commands[0].ForwardMove = (sbyte)(tic % 40 < 20 ? 25 : -25);
                commands[0].AngleTurn = (short)(tic % 2 == 0 ? 128 : -128);
                commands[0].Buttons = (byte)(tic % 3 == 0 ? TicCmdButtons.Attack : 0);
                doom.Game.Update(commands);
            }
            actual[name + "/state"] = Convert.ToHexString(SHA256.HashData(SaveAndLoad.SaveToMemory(doom.Game, "compatibility")));
            var pixels = new byte[renderer.Width * renderer.Height * 4];
            renderer.Render(doom, pixels, Fixed.Zero);
            actual[name + "/frame"] = Convert.ToHexString(SHA256.HashData(pixels));
        }
        var file = Path.Combine(root, "tests/Regression/doom-compatibility.json");
        if (record)
        {
            File.WriteAllText(file, JsonSerializer.Serialize(actual, new JsonSerializerOptions { WriteIndented = true }) + "\n");
            Console.WriteLine("Recorded pre-profile Doom compatibility baselines.");
            return;
        }
        var expected = JsonSerializer.Deserialize<SortedDictionary<string, string>>(File.ReadAllText(file));
        if (!actual.SequenceEqual(expected)) throw new Exception("Doom simulation/rendering changed from pre-profile baselines: " +
            string.Join(", ", actual.Where(pair => !expected.TryGetValue(pair.Key, out var value) || value != pair.Value).Select(pair => pair.Key)));
        Console.WriteLine("PASS all five bundled WADs match pre-profile simulation and rendered-frame hashes");
    }
}
