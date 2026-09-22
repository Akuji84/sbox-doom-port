// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticTerrainChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        foreach (var pair in new[] { ("FLTWAWA1", HereticFloorType.Water), ("FLTFLWW1", HereticFloorType.Water),
            ("FLTLAVA1", HereticFloorType.Lava), ("FLATHUH1", HereticFloorType.Lava), ("FLTSLUD1", HereticFloorType.Sludge) })
        {
            var s = new HereticWorldSession(content); var sector = s.Body.Subsector.Sector;
            sector.FloorFlat = s.World.Map.Flats.GetNumber(pair.Item1);
            Check(s.FloorType(s.Body) == pair.Item2, "Liquid terrain classification differs.");
            s.Body.FloorZ = sector.FloorHeight + Fixed.FromInt(8);
            var rng = s.World.Random.Index;
            Check(s.HitLiquidFloor(s.Body) == HereticFloorType.Solid && s.ImpactEffects.Count == 0 && s.World.Random.Index == rng,
                "Raised edge incorrectly splashed or consumed RNG.");
            s.Body.FloorZ = sector.FloorHeight;
            var sounds = new List<HereticSoundId>(); s.SoundRequested += (id, source) => sounds.Add(id);
            Check(s.HitLiquidFloor(s.Body) == pair.Item2 && s.ImpactEffects.Count == 2, "Liquid effect pair missing.");
            Check(s.World.Random.Index == ((rng + (pair.Item2 == HereticFloorType.Lava ? 3 : 7)) & 255), "Liquid RNG sequence differs.");
            var particle = s.ImpactEffects[1];
            Check(particle.Body.Info == null && particle.Body.State == null && particle.Body.Z == sector.FloorHeight,
                "Liquid effect invoked Doom definitions or wrong height.");
            Check(sounds.Count == (pair.Item2 == HereticFloorType.Sludge ? 0 : 1), "Liquid sound count differs.");
            if (pair.Item2 != HereticFloorType.Lava)
            {
                Check(particle.Body.Target == s.Body && particle.Body.MomZ >= Fixed.One, "Liquid particle source/launch differs.");
                particle.Body.Z = particle.Body.FloorZ; particle.Body.MomZ = -Fixed.One;
                s.Tick(default);
                Check(particle.Animation.State == HereticDefinitions.Actors[(int)particle.Type].DeathState,
                    "Liquid chunk landing did not enter impact state.");
            }
            new HereticMapPreview(content, s).Render(new byte[320 * 200 * 4], 0);
            for (var i = 0; i < 60; i++) s.Tick(default);
            Check(s.ImpactEffects.Count == 0, "Liquid effects were not unlinked.");
        }
        var phoenix = new HereticWorldSession(content);
        phoenix.Body.Subsector.Sector.FloorFlat = phoenix.World.Map.Flats.GetNumber("FLTWAWA1");
        var bolt = phoenix.SpawnPlayerProjectile(HereticActorType.MT_PHOENIXFX1, phoenix.Body.Angle);
        bolt.Body.Subsector.Sector.FloorFlat = phoenix.World.Map.Flats.GetNumber("FLTWAWA1");
        bolt.Body.FloorZ = bolt.Body.Subsector.Sector.FloorHeight;
        bolt.Body.Z = bolt.Body.FloorZ; bolt.Body.MomX = bolt.Body.MomY = Fixed.Zero; bolt.Body.MomZ = -Fixed.One;
        bolt.Advance();
        Check(!bolt.Flying && phoenix.ImpactEffects.Any(e => e.Type == HereticActorType.MT_SPLASHBASE), "Phoenix explosion did not splash.");
        for (var i = 0; i < 80; i++) phoenix.Tick(default);
        Check(phoenix.ImpactEffects.Count == 0 && phoenix.Projectiles.Count == 0, "Phoenix liquid effects leaked after impact/death.");
        Console.WriteLine("PASS Heretic terrain: five liquid flats, raised-edge exclusion, RNG, particle motion/landing, sounds, cleanup and Phoenix integration");
    }
}
