// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticPoweredSkullRodChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static HereticProjectile Impact(HereticWorldSession s)
    {
        var p = s.SpawnAimedProjectile(HereticActorType.MT_HORNRODFX2, s.Body.Angle, Fixed.Zero);
        Check(p.Flying, "Storm fixture did not spawn in open space.");
        p.Body.Z = p.Body.FloorZ; p.Body.MomX = p.Body.MomY = p.Body.MomZ = Fixed.Zero;
        p.Advance();
        Check(!p.Flying && p.Animation.State == HereticStateId.S_HRODFXI2_1, "Hellstaff impact did not start storm animation.");
        return p;
    }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var s = new HereticWorldSession(content); var enemy = s.StartClinkTest(); var w = s.GoldWand;
        var sounds = new List<HereticSoundId>(); s.SoundRequested += (id, source) => sounds.Add(id);
        w.GrantTestPoweredSkullRod(4); Check(!w.SelectWeapon(HereticWeapon.wp_skullrod), "Powered Hellstaff selected with insufficient ammo.");
        w.GrantTestPoweredSkullRod(9); w.SelectWeapon(HereticWeapon.wp_skullrod);
        for (var i = 0; i < 60; i++) w.Tick(false);
        s.Body.Angle = Geometry.PointToAngle(s.Body.X, s.Body.Y, enemy.Body.X, enemy.Body.Y);
        for (var i = 0; i < 40 && w.SkullRodShots == 0; i++) w.Tick(true);
        Check(w.SkullRodShots == 1 && w.SkullRodAmmo == 4, "Powered Hellstaff shot/ammo mismatch.");
        var missile = s.Projectiles.Single();
        Check(missile.Type == HereticActorType.MT_HORNRODFX2 && missile.SeekerTarget == enemy.Body && missile.Contact(s.Body), "Hellstaff target or owner mismatch.");
        Check(sounds.Contains(HereticSoundId.sfx_hrnpow), "Missing powered Hellstaff firing sound.");
        enemy.Body.Flags &= ~MobjFlags.Shootable;
        missile.Animation.SetState(HereticStateId.S_HRODFX2_2);
        Check(missile.SeekerTarget == null, "Seeking action retained dead target.");
        for (var i = 0; i < 80; i++) w.Tick(false);
        Check(w.ReadyWeapon != HereticWeapon.wp_skullrod && w.SkullRodAmmo == 4, "Powered Hellstaff fallback consumed remainder.");

        var storms = new HereticWorldSession(content); storms.StartClinkTest();
        var events = new List<HereticSoundId>(); storms.SoundRequested += (id, source) => events.Add(id);
        var first = Impact(storms); first.Body.Health = 90;
        var second = Impact(storms); second.Body.Health = 110;
        var third = Impact(storms);
        Check(storms.TrackedRainCount == 2 && first.Body.Health == 16 && second.Body.Health == 110 && third.Body.Health == 140, "Third storm did not retire older rain.");
        var fourth = Impact(storms);
        Check(second.Body.Health == 16 && storms.TrackedRainCount == 2, "Repeated storm replacement failed.");
        // Transition through the real explosion frames and ceiling hide action.
        for (var i = 0; i < 24; i++) third.Tick();
        Check(third.Body.Z == third.Body.CeilingZ + Fixed.FromInt(4) && third.RainDrops <= 1, "Storm did not hide at ceiling after explosion.");
        Check(events.Contains(HereticSoundId.sfx_ramphit), "Missing storm impact sound.");
        for (var i = 0; i < 145 && !third.Animation.Removed; i++) third.Tick();
        Check(third.Animation.Removed && third.RainDrops > 0 && third.RainDrops <= 140 && storms.TrackedRainCount == 1, "Storm lifetime/drop count/reference cleanup failed.");
        Check(events.Count(x => x == HereticSoundId.sfx_ramrain) == (third.RainDrops + 31) / 32, "Rain sound cadence differs.");
        Check(storms.Projectiles.Any(p => p.Type == HereticActorType.MT_RAINPLR3), "Storm spawned no red rain.");
        storms.DamageEnvironment(10000);
        for (var i = 0; i < 240; i++) storms.Tick(default);
        Check(storms.Projectiles.Count == 0 && storms.TrackedRainCount == 0, "Rain did not finish and unlink after player death.");

        var hits = new HereticWorldSession(content); var victim = hits.StartClinkTest();
        var source = new Mobj(hits.World) { Target = hits.Body };
        var drop = hits.SpawnRainDrop(source, victim.Body.X, victim.Body.Y);
        Check(drop.Body.Target == hits.Body && drop.Body.MomZ == -Fixed.FromInt(12) && drop.Contact(hits.Body), "Rain speed or owner mismatch.");
        var before = victim.Body.Health;
        for (var i = 0; i < 60 && drop.Flying; i++) drop.Tick();
        Check(victim.Body.Health < before && !drop.Flying, "Falling rain did not hit ordinary enemy.");
        Check(drop.Animation.State >= HereticStateId.S_RAINAIRXPLR3_1, "Airborne rain impact used floor animation.");
        var floor = hits.SpawnRainDrop(source, hits.Body.X, hits.Body.Y);
        floor.Body.Z = floor.Body.FloorZ + Fixed.One;
        floor.Advance();
        Check(!floor.Flying && floor.Body.Z == floor.Body.FloorZ && floor.Animation.State == HereticStateId.S_RAINPLR3X_1, "Rain floor impact mismatch.");
        for (var i = 0; i < 25; i++) floor.Tick();
        Check(floor.Animation.Removed, "Rain impact did not expire.");
        // Repeat a full storm under the session scheduler, including random
        // drops and impacts, to catch nondeterministic ordering/cleanup.
        string Replay()
        {
            var replay = new HereticWorldSession(content); replay.StartClinkTest();
            var cloud = Impact(replay); var frames = new List<string>();
            for (var i = 0; i < 190; i++)
            {
                replay.Tick(default);
                frames.Add(string.Join(";", replay.Projectiles.Select(p => $"{p.Type}:{p.Body.X.Data},{p.Body.Y.Data},{p.Body.Z.Data}:{p.Animation.State}:{p.Body.Health}")));
            }
            return string.Join("|", frames);
        }
        Check(Replay() == Replay(), "Rain replay diverged.");
        foreach (var flat in new[] { "FLTWAWA1", "FLTLAVA1", "FLTSLUD1" })
        {
            var liquid = new HereticWorldSession(content);
            liquid.Body.Subsector.Sector.FloorFlat = liquid.World.Map.Flats.GetNumber(flat);
            var splashed = false;
            liquid.SoundRequested += (id, sourceBody) => { if (id == HereticSoundId.sfx_gloop || id == HereticSoundId.sfx_burn) splashed = true; };
            for (var i = 0; i < 48; i++)
            {
                var rain = liquid.SpawnRainDrop(new Mobj(liquid.World) { Target = liquid.Body }, liquid.Body.X, liquid.Body.Y);
                rain.Body.Z = rain.Body.FloorZ + Fixed.One; rain.Advance();
            }
            var effectType = flat == "FLTWAWA1" ? HereticActorType.MT_SPLASHBASE : flat == "FLTLAVA1" ? HereticActorType.MT_LAVASPLASH : HereticActorType.MT_SLUDGESPLASH;
            Check(liquid.ImpactEffects.Any(e => e.Type == effectType), "Rain did not produce liquid impact effects for " + flat);
            Check(flat == "FLTSLUD1" ? !splashed : splashed, "Rain liquid impact sound mismatch for " + flat);
        }
        Console.WriteLine("PASS powered Hellstaff: ammo/target/fallback, storm limits/lifetime, rain damage/impacts, sounds and cleanup after death");
    }
}
