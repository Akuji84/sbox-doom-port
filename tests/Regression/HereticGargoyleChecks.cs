// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticGargoyleChecks
{
    static void Check(bool ok, string text) { if (!ok) throw new Exception(text); }
    static void VerifyCharge(GameContent content)
    {
        var s = new HereticWorldSession(content); var e = s.StartEnemyTest(HereticActorType.MT_IMP);
        Check(e != null && e.Body.Health == 40 && e.Body.Info == null, "Normal Gargoyle spawn failed.");
        e.Body.Target = s.Body; s.World.Random.Clear();
        e.Combatant.Animation.SetState(HereticStateId.S_IMP_MSATK1_2);
        Check(e.GargoyleCharging && e.Body.MomX == 12 * Trig.Cos(e.Body.Angle), "Charge did not start at native speed.");
        var mx=e.Body.MomX; var my=e.Body.MomY;
        e.Tick(); Check(e.Body.MomX == mx && e.Body.MomY == my, "Charge lost momentum to friction.");
        for (var i=0;i<30 && e.GargoyleCharging;i++) e.Tick();
        Check(!e.GargoyleCharging && s.State.Health == 100 && e.Body.MomZ == Fixed.Zero, "Charge contact/recovery or native zero impact damage incorrect.");
        e.Body.Target=null; e.Execute(HereticAction.A_ImpMsAttack, e.Combatant.Animation);
        Check(!e.GargoyleCharging, "Missing target started charge.");
        e.Body.Target=s.Body; s.World.Random.Clear(); s.World.Random.Next();
        e.Execute(HereticAction.A_ImpMsAttack,e.Combatant.Animation);
        Check(!e.GargoyleCharging, "Charge ignored random rejection.");
        var wall = new HereticWorldSession(content); var w = wall.StartEnemyTest(HereticActorType.MT_IMP);
        w.Body.Target = null; w.Combatant.Animation.SetState(HereticStateId.S_IMP_MSATK1_3, false);
        w.Body.Flags |= MobjFlags.SkullFly; w.Body.MomX = Fixed.FromInt(12); w.Body.MomY = Fixed.Zero;
        for (var i=0; i<500 && w.GargoyleCharging; i++) w.Tick();
        Check(!w.GargoyleCharging && w.Body.MomX == Fixed.Zero && w.Body.MomY == Fixed.Zero && w.Body.MomZ == Fixed.Zero, "Wall collision failed to end charge.");
        var melee = new HereticWorldSession(content); var m = melee.StartEnemyTest(HereticActorType.MT_IMP);
        var angle = Geometry.PointToAngle(melee.Body.X, melee.Body.Y, m.Body.X, m.Body.Y);
        melee.World.ThingMovement.UnsetThingPosition(m.Body);
        m.Body.X = melee.Body.X + 48 * Trig.Cos(angle); m.Body.Y = melee.Body.Y + 48 * Trig.Sin(angle);
        melee.World.ThingMovement.SetThingPosition(m.Body); m.Body.Target = melee.Body;
        melee.World.Random.Clear(); m.Execute(HereticAction.A_ImpMeAttack, m.Combatant.Animation);
        Check(melee.State.Health == 95 && melee.Projectiles.Count == 0, "Normal Gargoyle melee damage incorrect.");
        Check(e.MorphToChicken() && e.UpdateChicken(2000) && e.Combatant.Type == HereticActorType.MT_IMP, "Normal Gargoyle morph restore failed.");
        s.DamageTestEnemy(e.Body,10000);
        for(var i=0;i<100;i++) s.Tick(default);
        Check(e.Combatant.Animation.State == HereticStateId.S_IMP_XCRASH3 && s.TestKills==1,"Normal Gargoyle crash failed.");
        Console.WriteLine("PASS normal Gargoyle: native charge speed/friction/contact/recovery, random/missing-target rejection, morph and crash");
    }
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
        VerifyCharge(content);
        Console.WriteLine("PASS Fire Gargoyle: flight/melee/fireballs, owner exclusion, morph, normal/extreme airborne/ground crashes, exactly two debris chunks and native expiry");
    }
}
