// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticWizardChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var s = new HereticWorldSession(content); var enemy = s.StartEnemyTest(HereticActorType.MT_WIZARD);
        Check(enemy != null && enemy.Body.Health == 180 && (enemy.Body.Flags & (MobjFlags.Float | MobjFlags.NoGravity)) == (MobjFlags.Float | MobjFlags.NoGravity), "Disciple float/native spawn failed.");
        enemy.Body.Target = s.Body; enemy.Combatant.Animation.SetState(HereticStateId.S_WIZARD_ATK1);
        for (var tick = 1; tick <= 32; tick++)
        {
            enemy.Tick();
            if (tick % 4 == 0 && tick < 32)
                Check(((enemy.Body.Flags & MobjFlags.Shadow) != 0) == ((tick / 4) % 2 == 1), "Disciple ghost phase mismatch.");
        }
        Check((enemy.Body.Flags & MobjFlags.Shadow) == 0 && s.Projectiles.Count == 3, "Disciple did not become visible and fire three missiles.");
        var center = s.Projectiles[0]; var left = s.Projectiles[1]; var right = s.Projectiles[2];
        Check(left.Body.Angle == center.Body.Angle - new Angle(0x04000000u) && right.Body.Angle == center.Body.Angle + new Angle(0x04000000u), "Disciple fan spread incorrect.");
        Check(s.Projectiles.All(p => p.Type == HereticActorType.MT_WIZFX1 && p.Body.Target == enemy.Body && p.SeekerTarget == null && p.Body.MomZ == center.Body.MomZ), "Disciple volley lost owner/shared slope.");
        var speed = Fixed.FromInt(18);
        Check(center.Body.MomX == speed * Trig.Cos(center.Body.Angle), "Disciple missile speed incorrect.");
        center.Body.Z = s.Body.Z; var health = s.State.Health; s.World.Random.Clear(); center.Contact(s.Body);
        Check(health - s.State.Health == 3, "Disciple missile damage multiplier incorrect.");
        var pixels = new byte[320*200*4]; s.Body.Angle = Geometry.PointToAngle(s.Body.X, s.Body.Y, enemy.Body.X, enemy.Body.Y);
        new HereticMapPreview(content, s).Render(pixels, s.World.LevelTime);
        var output = Environment.GetEnvironmentVariable("HERETIC_WIZARD_RGBA"); if (!string.IsNullOrEmpty(output)) File.WriteAllBytes(output, pixels);
        var flight = new HereticWorldSession(content); var flyer = flight.StartEnemyTest(HereticActorType.MT_WIZARD);
        flyer.Body.Z = flyer.Body.FloorZ + Fixed.FromInt(24); flyer.Body.Target = null;
        var z = flyer.Body.Z; flyer.Tick(); flyer.Tick();
        Check(flyer.Body.Z == z && flyer.Body.MomZ == Fixed.Zero, "Disciple fell despite no-gravity flag.");
        flyer.Body.Target = flight.Body; flight.Body.Z += Fixed.FromInt(40); flyer.Tick();
        Check(flyer.Body.Z > z, "Disciple did not float toward raised target.");
        flight.Body.Z -= Fixed.FromInt(40);
        Check(flyer.MorphToChicken() && flyer.UpdateChicken(2000) && flyer.Combatant.Type == HereticActorType.MT_WIZARD && (flyer.Body.Flags & MobjFlags.NoGravity) != 0, "Disciple morph failed to restore flight.");
        flyer.Body.Flags |= MobjFlags.Shadow;
        flight.DamageTestEnemy(flyer.Body, 10000);
        Check((flyer.Body.Flags & (MobjFlags.Shadow | MobjFlags.Float | MobjFlags.NoGravity)) == 0, "Disciple death retained ghost/flight.");
        for (var i = 0; i < 100; i++) flight.Tick(default);
        Check(flyer.Body.Z == flyer.Body.FloorZ && (flyer.Body.Flags & MobjFlags.Solid) == 0 && flight.TestKills == 1, "Disciple corpse did not fall/settle.");
        // Find a reproducible rare-drop roll with the ammo roll failing, without subscribing to drop events.
        var index = -1;
        for (var i = 0; i < 256; i++)
        {
            var rng = new DoomRandom(); for (var j = 0; j < i; j++) rng.Next();
            if (rng.Next() > 84 && rng.Next() <= 4) { index = i; break; }
        }
        Check(index >= 0, "Rare Tome fixture seed missing.");
        var drops = new HereticWorldSession(content); var victim = drops.StartEnemyTest(HereticActorType.MT_WIZARD);
        drops.World.Random.Clear(); for (var i = 0; i < index; i++) drops.World.Random.Next();
        victim.Execute(HereticAction.A_NoBlocking, victim.Combatant.Animation);
        Check(drops.Actors.Any(a => a.Type == HereticActorType.MT_ARTITOMEOFPOWER && (a.Body.Flags & MobjFlags.Dropped) != 0), "Rare Tome drop depends on observers or is missing.");
        var before = drops.GoldWand.BlasterAmmo;
        drops.SpawnEnemyAmmoDrop(drops.Body, HereticActorType.MT_AMBLSRWIMPY, 10); drops.Tick(default);
        Check(drops.GoldWand.BlasterAmmo == before + 10, "Disciple ammo drop quantity wrong.");
        var tomeBefore = drops.State.TomesOfPower;
        drops.SpawnEnemyAmmoDrop(drops.Body, HereticActorType.MT_ARTITOMEOFPOWER, 0); drops.Tick(default);
        Check(drops.State.TomesOfPower == tomeBefore + 1, "Dropped Tome cannot be collected.");
        var melee = new HereticWorldSession(content); var close = melee.StartEnemyTest(HereticActorType.MT_WIZARD);
        var direction = Geometry.PointToAngle(melee.Body.X, melee.Body.Y, close.Body.X, close.Body.Y);
        melee.World.ThingMovement.UnsetThingPosition(close.Body);
        close.Body.X = melee.Body.X + 48 * Trig.Cos(direction); close.Body.Y = melee.Body.Y + 48 * Trig.Sin(direction);
        melee.World.ThingMovement.SetThingPosition(close.Body); close.Body.Target = melee.Body;
        melee.World.Random.Clear(); close.Execute(HereticAction.A_WizAtk3, close.Combatant.Animation);
        Check(melee.State.Health == 96 && melee.Projectiles.Count == 0, "Disciple melee damage/branch failed.");
        var live = new HereticWorldSession(content); var attacker = live.StartEnemyTest(HereticActorType.MT_WIZARD); var fired = false;
        for (var i = 0; i < 200 && live.State.Health == 100; i++) { live.Tick(default); fired |= live.Projectiles.Count > 0; }
        Check(live.State.Health < 100, $"Live Disciple failed to attack player (health {live.State.Health}, fired {fired}).");
        live.DamageTestEnemy(attacker.Body, 10000);
        for (var i = 0; i < 200; i++) live.Tick(default);
        Check(live.Projectiles.Count == 0, "Disciple missiles leaked after death.");
        Console.WriteLine("PASS Disciple: floating, ghost phases, three-shot spread/slope, missile damage, morph, falling corpse, rare Tome/ammo drops, live combat and cleanup");
    }
}
