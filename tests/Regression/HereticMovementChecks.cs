using ManagedDoom;
static class HereticMovementChecks
{
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static void Verify(string root)
    {
        var path = Path.Combine(root, "Assets/doom/blasphem.wad");
        using var content = GameContent.CreateHereticPreview(path);
        HereticWorldSession New() => new(content);
        var session = New();
        var x = session.Body.X; var y = session.Body.Y;
        session.Tick(new HereticCommand { Forward = 25 });
        Check(session.Body.X != x || session.Body.Y != y, "Heretic player did not move.");
        for (var i = 0; i < 100; i++) session.Tick(new HereticCommand { Look = 1 });
        Check(session.State.LookDirection == 90, "Look-up limit.");
        session.Tick(new HereticCommand { CenterLook = true });
        for (var i = 0; i < 20; i++) session.Tick(default);
        Check(session.State.LookDirection == 0, "Look centering.");
        for (var i = 0; i < 100; i++) session.Tick(new HereticCommand { Look = -1 });
        Check(session.State.LookDirection == -110, "Look-down limit.");
        session = New();
        session.Tick(new HereticCommand { Fly = 5 });
        Check(!session.State.Flying, "Flight without power.");
        session.GrantFlight(100);
        session.Tick(new HereticCommand { Fly = 5 });
        Check(session.State.Flying && session.Body.Z > session.Body.FloorZ, "Powered flight did not lift player.");
        for (var i = 0; i < 20; i++) session.Tick(new HereticCommand { Fly = 5 });
        Check(session.Body.Z + session.Body.Height <= session.Body.CeilingZ, "Flight crossed ceiling.");
        session.Tick(new HereticCommand { Land = true });
        for (var i = 0; i < 70; i++) session.Tick(default);
        Check(!session.State.Flying && session.Body.Z == session.Body.FloorZ, "Landing/gravity.");
        session = New(); session.GrantFlight(1); session.Tick(new HereticCommand { Fly = 5 });
        Check(!session.State.Flying && session.State.FlightTics == 0, "Flight expiry.");
        session = New();
        session.Body.Subsector.Sector.Special = (SectorSpecial)7;
        session.Tick(default); Check(session.State.Health == 96, "Sludge damage differs from Heretic.");
        session.Body.Subsector.Sector.Special = (SectorSpecial)9;
        session.Tick(default); session.Tick(default); Check(session.State.Secrets == 1, "Secret counted more than once.");
        var normal = New(); var ice = New();
        normal.Body.Subsector.Sector.Special = 0; ice.Body.Subsector.Sector.Special = (SectorSpecial)15;
        normal.Tick(new HereticCommand { Forward = 25 }); ice.Tick(new HereticCommand { Forward = 25 });
        Check(Fixed.Abs(ice.Body.MomX) + Fixed.Abs(ice.Body.MomY) < Fixed.Abs(normal.Body.MomX) + Fixed.Abs(normal.Body.MomY), "Ice thrust reduction.");
        var windy = New(); windy.Body.Subsector.Sector.Special = (SectorSpecial)40; windy.Tick(default);
        Check(windy.Body.MomX > Fixed.Zero, "East wind.");
        var collision = New();
        var solid = collision.World.Map.Lines.First(l => l.BackSector == null);
        var oldX = collision.Body.X; var oldY = collision.Body.Y;
        Check(!collision.World.ThingMovement.TryMove(collision.Body, (solid.Vertex1.X + solid.Vertex2.X) / 2, (solid.Vertex1.Y + solid.Vertex2.Y) / 2), "One-sided wall accepted player.");
        Check(collision.Body.X == oldX && collision.Body.Y == oldY, "Rejected collision moved player.");
        var floor = collision.Body.Subsector.Sector;
        var originalFloor = floor.FloorHeight;
        floor.FloorHeight = originalFloor + Fixed.FromInt(25);
        Check(!collision.World.ThingMovement.TryMove(collision.Body, oldX, oldY), "25-unit step should block.");
        floor.FloorHeight = originalFloor + Fixed.FromInt(24);
        Check(collision.World.ThingMovement.TryMove(collision.Body, oldX, oldY), "24-unit step should fit.");
        CheckDoorsAndPlatforms(content);
        CheckTeleportAndKeys(content);
        // Fixed command streams must be reproducible and safe in every bundled map slot.
        var pixels = new byte[320 * 200 * 4];
        var rendering = New(); var render = new HereticMapPreview(content, rendering);
        render.Render(pixels, 0); var levelView = pixels.ToArray();
        rendering.State.LookDirection = 45; render.Render(pixels, 0);
        Check(!pixels.SequenceEqual(levelView), "Look direction did not change rendered horizon.");
        var output = Environment.GetEnvironmentVariable("HERETIC_NAVIGATION_RGBA");
        if (!string.IsNullOrEmpty(output)) File.WriteAllBytes(output, pixels);
        rendering.State.LookDirection = 0; render.Render(pixels, 0);
        Check(pixels.SequenceEqual(levelView), "Look centering did not restore rendered view.");
        foreach (var lump in content.Wad.LumpInfos.Where(l => l.Name.Length == 4 && l.Name[0] == 'E' && l.Name[2] == 'M'))
        {
            var a = new HereticWorldSession(content, lump.Name[1] - '0', lump.Name[3] - '0');
            var b = new HereticWorldSession(content, lump.Name[1] - '0', lump.Name[3] - '0');
            for (var tic = 0; tic < 210; tic++)
            {
                var cmd = new HereticCommand { Forward = (sbyte)(tic % 60 < 30 ? 25 : -25), Side = 12, Turn = 256, Use = tic % 7 == 0, Look = (sbyte)(tic % 40 < 20 ? 1 : -1) };
                a.Tick(cmd); b.Tick(cmd);
                Check(a.Body.X == b.Body.X && a.Body.Y == b.Body.Y && a.Body.Z == b.Body.Z && a.State.Health == b.State.Health, lump.Name + " nondeterministic navigation.");
            }
            var preview = new HereticMapPreview(content, a);
            a.State.LookDirection = -110; preview.Render(pixels, 0);
            a.State.LookDirection = 90; preview.Render(pixels, 0);
        }
        Console.WriteLine("PASS Heretic navigation: movement, look limits, flight, landing, ice, wind, damage, secrets, keys, doors, lifts, switches, teleport and deterministic/rendered navigation in 48 maps");
    }
    private static void CheckDoorsAndPlatforms(GameContent content)
    {
        foreach (var (code, key) in new[] { (26, HereticKeys.Blue), (27, HereticKeys.Yellow), (28, HereticKeys.Green) })
        {
            var s = new HereticWorldSession(content);
            var line = s.World.Map.Lines.First(l => l.BackSide != null && l.FrontSector != l.BackSector && l.BackSector.SpecialData == null);
            line.Special = (LineSpecial)code;
            var sector = line.BackSector; sector.CeilingHeight = sector.FloorHeight;
            s.UseLine(line);
            Check(sector.SpecialData == null && s.State.Message.Contains(key.ToString().ToLowerInvariant()), "Wrong/missing key accepted.");
            s.State.Keys = key;
            Check(!s.UseLine(line, 1), "Door usable from rear.");
            s.UseLine(line);
            Check(sector.SpecialData is VerticalDoor, "Key did not open door.");
            var height = sector.CeilingHeight; s.Tick(default);
            Check(sector.CeilingHeight > height, "Door did not move.");
        }
        {
            var s = new HereticWorldSession(content);
            var line = s.World.Map.Lines.First(l => l.BackSide != null && l.BackSector != l.FrontSector && l.BackSector.SpecialData == null);
            foreach (var sec in s.World.Map.Sectors) sec.Tag = 0;
            var sector = line.BackSector; sector.Tag = 32000; line.Tag = 32000; line.Special = (LineSpecial)62;
            sector.FloorHeight = line.FrontSector.FloorHeight + Fixed.FromInt(32);
            line.FrontSide.TopTexture = content.Textures.GetNumber("SW1OFF");
            s.UseLine(line); Check(sector.SpecialData is Platform, "Lift did not spawn.");
            Check(line.FrontSide.TopTexture == content.Textures.GetNumber("SW1ON"), "Heretic switch did not change texture.");
            var height = sector.FloorHeight; s.Tick(default); Check(sector.FloorHeight < height, "Lift did not lower.");
            for (var i = 0; i < 36; i++) s.Tick(default);
            Check(line.FrontSide.TopTexture == content.Textures.GetNumber("SW1OFF"), "Repeatable switch did not reset.");
        }
        // Heretic stairs rise at 1 unit/tic for both 8- and 16-unit steps.
        foreach (var (code, step) in new[] { (7, 8), (107, 16) })
        {
            var s = new HereticWorldSession(content);
            var sector = s.Body.Subsector.Sector;
            foreach (var sec in s.World.Map.Sectors) sec.Tag = 0;
            sector.Tag = 32002;
            var line = s.World.Map.Lines[0]; line.Tag = 32002; line.Special = (LineSpecial)code;
            var floor = sector.FloorHeight;
            s.UseLine(line);
            Check(sector.SpecialData is FloorMove move && move.Speed == Fixed.One && move.FloorDestHeight == floor + Fixed.FromInt(step), "Heretic stair speed/height.");
            s.Tick(default);
            Check(sector.FloorHeight == floor + Fixed.One && s.Body.Z == s.Body.FloorZ, "Rider did not follow moving stairs.");
        }
    }
    private static void CheckTeleportAndKeys(GameContent content)
    {
        var s = new HereticWorldSession(content);
        var destination = s.World.Map.Things.First(t => t.Type == 1); destination.Type = 14;
        var destSector = Geometry.PointInSubsector(destination.X, destination.Y, s.World.Map).Sector;
        foreach (var sector in s.World.Map.Sectors) sector.Tag = 0;
        destSector.Tag = 32001;
        var line = s.World.Map.Lines[0]; line.Tag = 32001; line.Special = (LineSpecial)97;
        s.State.LookDirection = 50;
        s.Body.Angle = Angle.Ang90;
        s.CrossLine(line, 0, s.Body);
        Check(s.Body.X == destination.X && s.Body.Y == destination.Y && s.Body.Angle == destination.Angle && s.Body.ReactionTime == 18, "Teleport destination/freeze.");
        var angle = s.Body.Angle; s.Tick(new HereticCommand { Turn = 640 }); Check(s.Body.Angle == angle, "Teleport freeze ignored.");
        Check(s.State.LookDirection == 0, "Grounded teleport must center view.");
        var acquired = HereticKeys.None;
        foreach (var mapName in new[] { "E1M1", "E1M2", "E1M3", "E1M4" })
        {
            s = new HereticWorldSession(content, 1, mapName[3] - '0');
            foreach (var key in s.World.Map.Things.Where(t => t.Type == 73 || t.Type == 79 || t.Type == 80))
            {
                Check(s.World.ThingMovement.TeleportMove(s.Body, key.X, key.Y), "Cannot position key test.");
                s.Body.Z = s.Body.FloorZ; s.Body.MomX = s.Body.MomY = Fixed.Zero;
                s.Tick(default);
                var expected = new Dictionary<int, HereticKeys> { [73] = HereticKeys.Green, [79] = HereticKeys.Blue, [80] = HereticKeys.Yellow }[key.Type];
                Check((s.State.Keys & expected) != 0, "Key pickup did not set Heretic inventory.");
                acquired |= expected;
            }
        }
        Check(acquired == (HereticKeys.Blue | HereticKeys.Green | HereticKeys.Yellow), "Tests did not pick up all three real Heretic key types.");
    }
}
