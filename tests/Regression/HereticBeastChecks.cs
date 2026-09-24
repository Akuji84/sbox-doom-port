// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticBeastChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var melee = new HereticWorldSession(content); var beast = melee.StartEnemyTest(HereticActorType.MT_BEAST);
        Check(beast != null && beast.Body.Info == null && beast.Body.Health == 220, "Beast native spawn failed.");
        var direction = Geometry.PointToAngle(melee.Body.X, melee.Body.Y, beast.Body.X, beast.Body.Y);
        melee.World.ThingMovement.UnsetThingPosition(beast.Body);
        beast.Body.X = melee.Body.X + 48 * Trig.Cos(direction); beast.Body.Y = melee.Body.Y + 48 * Trig.Sin(direction);
        melee.World.ThingMovement.SetThingPosition(beast.Body);
        beast.Body.Target = melee.Body; beast.Body.ReactionTime = 0;
        beast.Execute(HereticAction.A_Chase, beast.Combatant.Animation);
        Check(!beast.Combatant.Animation.Removed && beast.Combatant.Animation.State == HereticStateId.S_BEAST_ATK1, "Beast entered nonexistent melee state.");
        for (var i = 0; i < 12; i++) melee.Tick(default);
        Check(melee.State.Health < 100 && melee.State.Health >= 76 && (100 - melee.State.Health) % 3 == 0 && melee.Projectiles.Count == 0, "Beast close attack is not 3-24 melee damage.");
        var ranged = new HereticWorldSession(content); var attacker = ranged.StartEnemyTest(HereticActorType.MT_BEAST);
        attacker.Body.Target = ranged.Body; attacker.Body.ReactionTime = 0;
        attacker.Execute(HereticAction.A_Chase, attacker.Combatant.Animation);
        var fired = false;
        for (var i = 0; i < 100 && ranged.State.Health == 100; i++)
        {
            ranged.Tick(default); fired |= ranged.Projectiles.Any(p => p.Type == HereticActorType.MT_BEASTBALL);
        }
        Check(fired && ranged.State.Health < 100, "Beast ranged encounter did not launch/hit.");
        var missile = ranged.SpawnMonsterMissile(attacker, HereticActorType.MT_BEASTBALL);
        Check(missile.Flying && missile.SeekerTarget == null && missile.Body.Target == attacker.Body, "Beast fireball acquired homing or lost owner.");
        var speed = new Fixed(HereticDefinitions.Actors[(int)HereticActorType.MT_BEASTBALL].Speed);
        Check(missile.Body.MomX == speed * Trig.Cos(missile.Body.Angle) && missile.Body.MomY == speed * Trig.Sin(missile.Body.Angle), "Fireball speed incorrect.");
        var rng = ranged.World.Random.Index;
        Check(missile.Contact(attacker.Body) && ranged.World.Random.Index == rng, "Beast fireball hit owner.");
        var effects = ranged.ImpactEffects.Count;
        ranged.World.Random.Clear(); ranged.SpawnBeastPuff(missile.Body);
        Check(ranged.ImpactEffects.Count == effects && ranged.World.Random.Index == 1, "Beast puff probability/RNG incorrect.");
        ranged.SpawnBeastPuff(missile.Body);
        var puff = ranged.ImpactEffects.Last();
        Check(puff.Type == HereticActorType.MT_PUFFY && ranged.World.Random.Index == 9, "Puff spawn/order incorrect.");
        Check((puff.Body.Flags & (MobjFlags.Solid | MobjFlags.Shootable | MobjFlags.Missile)) == 0, "Cosmetic fire trail blocks or damages actors.");
        var pixels = new byte[320 * 200 * 4];
        ranged.Body.Angle = Geometry.PointToAngle(ranged.Body.X, ranged.Body.Y, attacker.Body.X, attacker.Body.Y);
        new HereticMapPreview(content, ranged).Render(pixels, ranged.World.LevelTime);
        var output = Environment.GetEnvironmentVariable("HERETIC_BEAST_RGBA"); if (!string.IsNullOrEmpty(output)) File.WriteAllBytes(output, pixels);
        Check(attacker.MorphToChicken() && attacker.UpdateChicken(2000) && attacker.Combatant.Type == HereticActorType.MT_BEAST, "Beast chicken restore failed.");
        ranged.DamageTestEnemy(attacker.Body, 10000);
        for (var i = 0; i < 200; i++) ranged.Tick(default);
        Check(ranged.Projectiles.Count == 0 && !ranged.ImpactEffects.Any(e => e.Type == HereticActorType.MT_PUFFY), "Beast fire effects leaked after death.");
        foreach (var gib in new[] { false, true })
        {
            var s = new HereticWorldSession(content); var victim = s.StartEnemyTest(HereticActorType.MT_BEAST);
            var drops = new List<HereticTestDrop>(); victim.DropRequested += drops.Add;
            s.DamageTestEnemy(victim.Body, gib ? 10000 : 220);
            Check(victim.Combatant.Animation.State == (gib ? HereticStateId.S_BEAST_XDIE1 : HereticStateId.S_BEAST_DIE1), "Wrong Beast death chain.");
            s.World.Random.Clear();
            for (var i = 0; i < 100; i++) s.Tick(default);
            Check(victim.Combatant.Animation.Tics == -1 && (victim.Body.Flags & MobjFlags.Solid) == 0 && s.TestKills == 1, "Beast corpse did not settle.");
            Check(drops.Count == 1 && drops[0].Type == HereticActorType.MT_AMCBOWWIMPY && drops[0].Amount == 10, "Beast death drop is not ten crossbow rounds.");
        }
        var pickup = new HereticWorldSession(content); pickup.StartEnemyTest(HereticActorType.MT_BEAST);
        var before = pickup.GoldWand.CrossbowAmmo;
        pickup.SpawnEnemyAmmoDrop(pickup.Body, HereticActorType.MT_AMCBOWWIMPY, 10); pickup.Tick(default);
        Check(pickup.GoldWand.CrossbowAmmo == before + 10, "Dropped arrows used normal five-round amount.");
        Console.WriteLine("PASS Beast: melee/ranged, straight fireballs, trails/RNG/render/cleanup, morph, normal/gib deaths and exact crossbow drops");
    }
}
