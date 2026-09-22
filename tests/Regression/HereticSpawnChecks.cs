// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticSpawnChecks
{
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static MapThing Thing(int number, int flags) => new(Fixed.Zero, Fixed.Zero, Angle.Ang0, number, (ThingFlags)flags);
    public static void Verify(string root)
    {
        foreach (var skill in Enum.GetValues<GameSkill>())
        {
            var expected = skill <= GameSkill.Easy ? 1 : skill == GameSkill.Medium ? 2 : 4;
            foreach (var mask in new[] { 0, 1, 2, 4, 7 })
                Check((HereticMapSpawns.Decide(Thing(73, mask), skill).Disposition == HereticSpawnDisposition.Spawn) == ((mask & expected) != 0), "Heretic skill filter mismatch.");
        }
        Check(HereticMapSpawns.Decide(Thing(73, 23)).Disposition == HereticSpawnDisposition.Filtered, "Multiplayer-only key spawned solo.");
        Check(HereticMapSpawns.Decide(Thing(73, 23), network: true).Disposition == HereticSpawnDisposition.Spawn, "Network spawn flag rejected.");
        Check(HereticMapSpawns.Decide(Thing(73, 7), network: true, deathmatch: true).Disposition == HereticSpawnDisposition.Filtered, "Deathmatch key spawned.");
        Check(HereticMapSpawns.Decide(Thing(66, 7), noMonsters: true).Disposition == HereticSpawnDisposition.Filtered, "No-monsters filter ignored.");
        Check(HereticMapSpawns.Decide(Thing(66, 7)).Disposition == HereticSpawnDisposition.Unsupported, "Enemy activated before AI exists.");
        Check(HereticMapSpawns.Decide(Thing(81, 7)).Disposition == HereticSpawnDisposition.Unsupported, "Unimplemented potion spawned.");
        foreach (var number in new[] { 1, 4, 11, 56, 1200, 1299 })
            Check(HereticMapSpawns.Decide(Thing(number, 0)).Disposition == HereticSpawnDisposition.Marker, "Special map marker treated as actor.");
        Check(HereticMapSpawns.Decide(Thing(32000, 7)).Disposition == HereticSpawnDisposition.Unknown, "Unknown map thing hidden.");
        Check(!HereticMapSpawns.Supports(HereticActorType.MT_KEYGIZMOBLUE), "Later state action was silently skipped.");
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var total = 0; var solidCount = 0; var hangingCount = 0;
        foreach (var lump in content.Wad.LumpInfos.Where(l => l.Name.Length == 4 && l.Name[0] == 'E' && l.Name[2] == 'M'))
        {
            var a = new HereticWorldSession(content, lump.Name[1] - '0', lump.Name[3] - '0');
            var b = new HereticWorldSession(content, lump.Name[1] - '0', lump.Name[3] - '0');
            Check(a.Actors.Count == b.Actors.Count, "Nondeterministic actor count.");
            for (var i = 0; i < a.Actors.Count; i++)
            {
                var actor = a.Actors[i]; var body = actor.Body;
                Check(actor.Type == b.Actors[i].Type && actor.Animation.Tics == b.Actors[i].Animation.Tics, "Nondeterministic spawn phase.");
                Check(body.Info == null && body.State == null, "Map actor borrowed Doom state.");
                Check((body.Flags & MobjFlags.Shootable) == 0, "Unimplemented damage target activated.");
                var ceiling = (body.Flags & MobjFlags.SpawnCeiling) != 0;
                Check(body.Z == (ceiling ? body.CeilingZ - body.Height : body.FloorZ), "Wrong actor placement height.");
                if (ceiling) hangingCount++;
                if ((body.Flags & MobjFlags.Solid) != 0) solidCount++;
                total++;
            }
            for (var tic = 0; tic < 140; tic++) { a.Tick(default); b.Tick(default); }
            Check(a.Actors.Count == b.Actors.Count, "Nondeterministic actor removal.");
            for (var i = 0; i < a.Actors.Count; i++)
                Check(a.Actors[i].Animation.State == b.Actors[i].Animation.State && a.Actors[i].Animation.Tics == b.Actors[i].Animation.Tics, "Nondeterministic actor animation.");
            new HereticMapPreview(content, a).Render(new byte[320 * 200 * 4], 140);
        }
        Check(total > 100 && solidCount > 0 && hangingCount > 0, "Map actor coverage incomplete.");
        var session = new HereticWorldSession(content);
        var obstacle = session.Actors.First(a => (a.Body.Flags & MobjFlags.Solid) != 0).Body;
        var movement = session.World.ThingMovement;
        movement.UnsetThingPosition(obstacle);
        obstacle.X = session.Body.X; obstacle.Y = session.Body.Y;
        obstacle.Z = session.Body.Z;
        movement.SetThingPosition(obstacle);
        var oldX = session.Body.X; var oldY = session.Body.Y;
        Check(!movement.TryMove(session.Body, oldX, oldY), "Solid Heretic scenery does not block movement.");
        Check(session.Body.X == oldX && session.Body.Y == oldY, "Blocked movement changed coordinates.");
        obstacle.Flags &= ~MobjFlags.Solid;
        Check(movement.TryMove(session.Body, oldX, oldY), "Collision test blocked by something other than scenery.");
        Console.WriteLine($"PASS Heretic map spawning: skill/network filters, explicit unsupported things, deterministic scenery in 48 maps ({total} actors, {solidCount} solid, {hangingCount} hanging), rendering and collision");
    }
}
