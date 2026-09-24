// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticGargoyleChecks
{
    static void Check(bool ok, string text) { if (!ok) throw new Exception(text); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var s = new HereticWorldSession(content); var enemy = s.StartEnemyTest(HereticActorType.MT_IMPLEADER);
        Check(enemy != null && enemy.Body.Health == 80 && enemy.Body.Info == null, "Fire Gargoyle spawn failed.");
        enemy.Body.Target = s.Body;
        enemy.Execute(HereticAction.A_ImpMsAttack2, enemy.Combatant.Animation);
        var missile = s.Projectiles.Single();
        Check(missile.Type == HereticActorType.MT_IMPBALL && missile.Body.Target == enemy.Body && missile.SeekerTarget == null, "Fire Gargoyle projectile ownership/type incorrect.");
        var speed = Fixed.FromInt(10);
        Check(missile.Body.MomX == speed * Trig.Cos(missile.Body.Angle), "Fire Gargoyle missile speed incorrect.");
        missile.Body.Z = s.Body.Z; s.World.Random.Clear(); missile.Contact(s.Body);
        Check(s.State.Health == 99, "Fire Gargoyle missile did not use 1-8 damage.");
        var rng = s.World.Random.Index;
        Check(missile.Contact(enemy.Body) && rng == s.World.Random.Index, "Fire Gargoyle missile hit owner.");
        var pixels = new byte[320*200*4]; s.Body.Angle = Geometry.PointToAngle(s.Body.X, s.Body.Y, enemy.Body.X, enemy.Body.Y);
        new HereticMapPreview(content, s).Render(pixels, s.World.LevelTime);
        var output = Environment.GetEnvironmentVariable("HERETIC_GARGOYLE_RGBA"); if (!string.IsNullOrEmpty(output)) File.WriteAllBytes(output, pixels);
        Check(enemy.MorphToChicken() && enemy.UpdateChicken(2000) && enemy.Combatant.Type == HereticActorType.MT_IMPLEADER && (enemy.Body.Flags & MobjFlags.Float) != 0, "Fire Gargoyle morph restoration failed.");
        var melee = new HereticWorldSession(content); var close = melee.StartEnemyTest(HereticActorType.MT_IMPLEADER);
        var direction = Geometry.PointToAngle(melee.Body.X, melee.Body.Y, close.Body.X, close.Body.Y);
        melee.World.ThingMovement.UnsetThingPosition(close.Body);
        close.Body.X = melee.Body.X + 48 * Trig.Cos(direction); close.Body.Y = melee.Body.Y + 48 * Trig.Sin(direction);
        melee.World.ThingMovement.SetThingPosition(close.Body); close.Body.Target = melee.Body;
        melee.World.Random.Clear(); close.Execute(HereticAction.A_ImpMsAttack2, close.Combatant.Animation);
        Check(melee.State.Health == 95 && melee.Projectiles.Count == 0, "Fire Gargoyle melee branch incorrect.");
        foreach (var extreme in new[] { false, true })
        foreach (var airborne in new[] { false, true })
        {
            var death = new HereticWorldSession(content); var victim = death.StartEnemyTest(HereticActorType.MT_IMPLEADER);
            if (airborne) victim.Body.Z = victim.Body.FloorZ + Fixed.FromInt(48);
            var drops = 0; victim.DropRequested += _ => drops++;
            death.DamageTestEnemy(victim.Body, extreme ? 10000 : 80);
            Check((victim.Body.Flags & MobjFlags.Solid) == 0, "Gargoyle death retained collision.");
            if (extreme) Check((victim.Body.Flags & MobjFlags.NoGravity) != 0, "Extreme death omitted initial suspended phase.");
            for (var i = 0; i < 100; i++) death.Tick(default);
            Check(victim.Body.Z == victim.Body.FloorZ && victim.Combatant.Animation.State == (extreme ? HereticStateId.S_IMP_XCRASH3 : HereticStateId.S_IMP_CRASH4), "Gargoyle did not complete correct landing crash.");
            Check(victim.Combatant.Animation.Tics == -1 && death.TestKills == 1 && drops == 0, "Crash state/kill/drop accounting incorrect.");
            Check(death.ImpactEffects.Count(e => e.Type == HereticActorType.MT_IMPCHUNK1 || e.Type == HereticActorType.MT_IMPCHUNK2) == 2, "Crash did not create exactly two debris chunks.");
            var debris = death.ImpactEffects.Where(e => e.Type == HereticActorType.MT_IMPCHUNK1 || e.Type == HereticActorType.MT_IMPCHUNK2).ToArray();
            Check(debris.All(e => e.Body.Z == e.Body.FloorZ && (e.Body.Flags & (MobjFlags.Solid | MobjFlags.Shootable)) == 0), "Debris did not land safely.");
            for (var i = 0; i < 1500; i++) death.Tick(default);
            Check(!death.ImpactEffects.Any(e => e.Type == HereticActorType.MT_IMPCHUNK1 || e.Type == HereticActorType.MT_IMPCHUNK2), "Native debris lifetime leaked or crash repeated.");
        }
        var live = new HereticWorldSession(content); var attacker = live.StartEnemyTest(HereticActorType.MT_IMPLEADER);
        for (var i = 0; i < 250 && live.State.Health == 100; i++) live.Tick(default);
        Check(live.State.Health < 100, "Live Fire Gargoyle did not attack.");
        Check(!HereticWorldSession.SupportsMapEnemy(HereticActorType.MT_IMP), "Normal Gargoyle enabled before charge support.");
        Console.WriteLine("PASS Fire Gargoyle: flight/melee/fireballs, owner exclusion, morph, normal/extreme airborne/ground crashes, exactly two debris chunks and native expiry");
    }
}
