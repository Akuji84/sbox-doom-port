// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticClinkChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var s = new HereticWorldSession(content);
        Check(s.TestEnemyCount == 0, "Test encounter enabled by default.");
        var enemy = s.StartClinkTest();
        Check(enemy != null, "Clink test placement failed.");
        var sounds = new List<HereticSoundId>(); var drops = new List<HereticTestDrop>();
        enemy.SoundRequested += (sound, body) => sounds.Add(sound);
        enemy.DropRequested += drops.Add;
        Check(enemy.Body.Info == null && enemy.Body.State == null, "Clink bound to Doom states.");
        for (var i = 0; i < 150 && s.State.Health == 100; i++) s.Tick(default);
        Check(s.State.Health < 100 && s.State.Health >= 91, "Clink did not detect/approach/melee player for reference damage.");
        Check(sounds.Count > 0, "Clink sound actions not emitted.");
        new HereticMapPreview(content, s).Render(new byte[320 * 200 * 4], s.World.LevelTime);
        Check(s.DamageTestEnemy(enemy.Body, 1) != HereticDamageResult.Ignored, "Clink ignored damage.");
        Check(s.DamageTestEnemy(enemy.Body, 10000) == HereticDamageResult.Killed && s.TestKills == 1, "Clink death/kill accounting failed.");
        Check(s.DamageTestEnemy(enemy.Body, 10000) == HereticDamageResult.Ignored && s.TestKills == 1, "Duplicate kill counted.");
        s.World.Random.Clear();
        for (var i = 0; i < 100; i++) s.Tick(default);
        Check(enemy.Combatant.Animation.Tics == -1 && (enemy.Body.Flags & MobjFlags.Solid) == 0, "Clink corpse did not finish animation/release collision.");
        Check(drops.Count == 1 && drops[0].Amount == 20 && drops[0].Type == HereticActorType.MT_AMSKRDWIMPY, "Clink drop request differs from pinned rules.");
        var teleport = new HereticWorldSession(content);
        var victim = teleport.StartClinkTest();
        Check(victim != null && teleport.World.ThingMovement.TeleportMove(teleport.Body, victim.Body.X, victim.Body.Y), "Heretic telefrag invoked an invalid path.");
        Check(victim.Body.Health <= 0 && teleport.TestKills == 1, "Heretic telefrag failed.");
        var shooting = new HereticWorldSession(content);
        var target = shooting.StartClinkTest();
        Check(target != null, "Test ray target placement failed.");
        shooting.Body.Angle = Geometry.PointToAngle(shooting.Body.X, shooting.Body.Y, target.Body.X, target.Body.Y);
        for (var i = 0; i < 40 && shooting.GoldWand.ShotsFired == 0; i++)
        {
            shooting.Body.Angle = Geometry.PointToAngle(shooting.Body.X, shooting.Body.Y, target.Body.X, target.Body.Y);
            shooting.Tick(new HereticCommand { TestAttack = true });
        }
        Check(shooting.ImpactEffects.Any(x => x.Type == HereticActorType.MT_GOLDWANDPUFF1), "Wand attack did not spawn an impact.");
        var dealt = HereticDefinitions.Actors[(int)HereticActorType.MT_CLINK].SpawnHealth - target.Body.Health;
        Check(dealt >= 7 && dealt <= 14 && shooting.GoldWand.Ammo == 49, "Gold Wand input did not damage linked target/use ammo.");
        var stepper = new HereticFrameStepper(); var attacks = 0;
        stepper.Advance(0.001, new HereticCommand { TestAttack = true }, c => { if(c.TestAttack) attacks++; });
        stepper.Advance(0.03, default, c => { if(c.TestAttack) attacks++; });
        Check(attacks == 1, "Brief test attack was lost between ticks.");
        var crusher = new HereticWorldSession(content);
        var crushed = crusher.StartClinkTest();
        Check(crushed != null, "Crusher target placement failed.");
        var sector = crushed.Body.Subsector.Sector;
        var health = crushed.Body.Health;
        crusher.World.SectorAction.MovePlane(sector, Fixed.FromInt(256), sector.FloorHeight + Fixed.FromInt(8), true, 1, -1);
        Check(crushed.Body.Health < health && crushed.Body.Info == null, "Crusher did not use Heretic enemy damage.");
        var a = new HereticWorldSession(content); var b = new HereticWorldSession(content);
        var ea = a.StartClinkTest(); var eb = b.StartClinkTest();
        Check(ea != null && eb != null, "Replay placement failed.");
        for (var i = 0; i < 180; i++)
        {
            a.Body.Angle = Geometry.PointToAngle(a.Body.X, a.Body.Y, ea.Body.X, ea.Body.Y);
            b.Body.Angle = Geometry.PointToAngle(b.Body.X, b.Body.Y, eb.Body.X, eb.Body.Y);
            var cmd = new HereticCommand { TestAttack = true };
            a.Tick(cmd); b.Tick(cmd);
            Check(a.State.Health == b.State.Health && ea.Body.Health == eb.Body.Health && ea.Body.X == eb.Body.X && ea.Body.Y == eb.Body.Y && ea.Combatant.Animation.State == eb.Combatant.Animation.State && a.World.Random.Index == b.World.Random.Index, "Clink encounter replay diverged.");
        }
        Check(a.TestKills == 1 && b.TestKills == 1, "Repeated test shots did not complete encounter.");
        Console.WriteLine("PASS native Clink test encounter: linked/rendered actor, detection, melee, damage, death, sound/drop requests, telefrag and frame-latched test attack");
    }
}
