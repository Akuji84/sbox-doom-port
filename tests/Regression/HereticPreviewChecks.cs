using ManagedDoom;
using System.Text;
static class HereticPreviewChecks
{
    public static void Verify(string root)
    {
        var path = Path.Combine(root, "Assets/doom/blasphem.wad");
        SboxManagedDoomFileSystem.SetHostWadPaths(path);
        using var content = GameContent.CreateHereticPreview(path);
        if (content.Profile.RuntimeSupported) throw new Exception("Preview enabled gameplay.");
        if (content.Animation.Animations.Length != 8) throw new Exception("Missing Heretic animation cycles.");
        var health = DoomInfo.MobjInfos[0].SpawnHealth;
        var world = World.CreateGeometryPreview(content, 1, 1);
        foreach (var anim in content.Animation.Animations)
        {
            world.SetPreviewTime(0);
            var translation = anim.IsTexture ? world.Specials.TextureTranslation : world.Specials.FlatTranslation;
            var first = translation[anim.BasePic];
            world.SetPreviewTime(anim.Speed);
            if (translation[anim.BasePic] == first) throw new Exception("Animation did not advance.");
            world.SetPreviewTime(anim.Speed * anim.NumPics);
            if (translation[anim.BasePic] != first) throw new Exception("Animation did not wrap.");
        }
        try { world.Update(); throw new Exception("Preview simulation was allowed."); }
        catch (InvalidOperationException ex) when (ex.Message.Contains("cannot simulate")) { }
        for (var episode = 1; episode <= 6; episode++)
        {
            var skyWorld = World.CreateGeometryPreview(content, episode, 1);
            var expectedSky = episode == 3 || episode == 5 ? "SKY3" : episode == 2 ? "SKY2" : "SKY1";
            if (skyWorld.Map.SkyTexture != content.Textures[expectedSky]) throw new Exception("Wrong Heretic episode sky.");
        }
        for (var i = 0; i < HereticAssets.SpriteNames.Length; i++)
            if (content.Sprites[(Sprite)i].Frames.Length == 0) throw new Exception("Missing Heretic sprite " + HereticAssets.SpriteNames[i]);
        CheckFailures(path);
        var pixels = new byte[HereticMapPreview.Width * HereticMapPreview.Height * 4];
        var count = 0;
        foreach (var lump in content.Wad.LumpInfos)
        {
            var name = lump.Name;
            if (name.Length != 4 || name[0] != 'E' || name[2] != 'M' || !char.IsDigit(name[1]) || !char.IsDigit(name[3])) continue;
            try
            {
                var preview = new HereticMapPreview(content, name[1] - '0', name[3] - '0');
                foreach (var yaw in new[] { 0, 90, 180, 270 }) preview.Render(pixels, 35, yaw);
                if (pixels.Distinct().Count() < 16) throw new Exception("Empty preview.");
                if (name == "E1M1")
                {
                    preview.Render(pixels, 0);
                    var output = Environment.GetEnvironmentVariable("HERETIC_PREVIEW_RGBA");
                    if (!string.IsNullOrEmpty(output)) File.WriteAllBytes(output, pixels);
                }
                count++;
            }
            catch (Exception ex) { throw new Exception(name + " failed: " + ex.Message, ex); }
        }
        try { new DoomGame(content, new GameOptions()); throw new Exception("Gameplay guard bypassed."); }
        catch (NotSupportedException) { }
        if (DoomInfo.MobjInfos[0].SpawnHealth != health) throw new Exception("Heretic preview changed Doom definitions.");
        if (count != 48) throw new Exception("Unexpected Blasphemer map count: " + count);
        Console.WriteLine("PASS Blasphemer 0.1.8 assets, eight animation cycles and 48 maps rendered in four directions; gameplay remains blocked");
    }
    private static void CheckFailures(string basePath)
    {
        var temp = Path.Combine(Path.GetTempPath(), "heretic-preview-" + Guid.NewGuid().ToString("N") + ".wad");
        try
        {
            void Patch(params (string name, byte[] bytes)[] lumps)
            {
                using var writer = new BinaryWriter(File.Create(temp));
                writer.Write(Encoding.ASCII.GetBytes("PWAD"));
                writer.Write(lumps.Length);
                writer.Write(12 + lumps.Sum(l => l.bytes.Length));
                foreach (var lump in lumps) writer.Write(lump.bytes);
                var offset = 12;
                foreach (var lump in lumps)
                {
                    writer.Write(offset); writer.Write(lump.bytes.Length);
                    writer.Write(Encoding.ASCII.GetBytes(lump.name.PadRight(8, '\0')));
                    offset += lump.bytes.Length;
                }
            }
            void Expect(Action action, string message)
            {
                try { action(); }
                catch (Exception ex) when (ex.Message.Contains(message)) { return; }
                throw new Exception("Expected diagnostic: " + message);
            }
            Patch(("PLAYPAL", new byte[1]));
            Expect(() => { using var c = GameContent.CreateHereticPreview(basePath, temp); }, "PLAYPAL");
            Patch(("COLORMAP", new byte[1]));
            Expect(() => { using var c = GameContent.CreateHereticPreview(basePath, temp); }, "COLORMAP");
            Patch(("E1M1", Array.Empty<byte>()), ("TEXTMAP", Encoding.ASCII.GetBytes("namespace=heretic;")));
            using (var wad = new Wad(GameProfile.Heretic, basePath, temp))
                Expect(() => HereticAssets.ValidateMap(wad, "E1M1"), "only classic Heretic binary maps");
            Patch(("E1M1", Array.Empty<byte>()), ("THINGS", new byte[1]),
                ("LINEDEFS", Array.Empty<byte>()), ("SIDEDEFS", Array.Empty<byte>()),
                ("VERTEXES", Array.Empty<byte>()), ("SEGS", Array.Empty<byte>()),
                ("SSECTORS", Array.Empty<byte>()), ("NODES", Array.Empty<byte>()),
                ("SECTORS", Array.Empty<byte>()), ("REJECT", Array.Empty<byte>()), ("BLOCKMAP", Array.Empty<byte>()));
            using (var wad = new Wad(GameProfile.Heretic, basePath, temp))
                Expect(() => HereticAssets.ValidateMap(wad, "E1M1"), "malformed THINGS");
            using (var wad = new Wad(GameProfile.Heretic, basePath))
                Expect(() => HereticAssets.ValidateMap(wad, "E9M9"), "E9M9 is missing");
            Console.WriteLine("PASS Heretic preview diagnostics reject malformed palettes, lighting, map records and UDMF; animations advance and wrap");
        }
        finally { File.Delete(temp); }
    }
}
