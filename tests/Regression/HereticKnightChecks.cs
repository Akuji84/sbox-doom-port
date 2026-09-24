// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticKnightChecks
{
    static void Check(bool ok, string text) { if (!ok) throw new Exception(text); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        foreach (var type in new[] { HereticActorType.MT_KNIGHT, HereticActorType.MT_KNIGHTGHOST })
        {
            var s = new HereticWorldSession(content); var enemy = s.StartEnemyTest(type);
            Check(enemy != null && enemy.Body.Health == 200, "Warrior spawn failed.");
            enemy.Body.Target = s.Body;
            s.State.InvulnerabilityTics = 1000;
            // Execute the authored two-throw attack, holding the actor in its native attack chain.
            enemy.Combatant.Animation.SetState(HereticStateId.S_KNIGHT_ATK1);
            var throws = 0; var previous = 0;
            for (var i = 0; i < 45; i++)
            {
                enemy.Tick();
                var count = s.Projectiles.Count;
                if (count > previous) throws += count - previous;
                previous = count;
            }
            Check(throws == 2, "Warrior attack did not throw two axes.");
            Check(s.Projectiles.All(p => p.Type == HereticActorType.MT_KNIGHTAXE || p.Type == HereticActorType.MT_REDAXE), "Wrong Warrior missile family.");
            if (type == HereticActorType.MT_KNIGHTGHOST) Check(s.Projectiles.All(p => p.Type == HereticActorType.MT_REDAXE), "Ghost Warrior threw green axe.");
            Check(enemy.MorphToChicken() && enemy.UpdateChicken(2000) && enemy.Combatant.Type == type, "Warrior morph restored wrong variant.");
            var drops = new List<HereticTestDrop>(); enemy.DropRequested += drops.Add;
            s.DamageTestEnemy(enemy.Body, 10000); s.World.Random.Clear();
            for (var i = 0; i < 200; i++) s.Tick(default);
            Check(s.TestKills == 1 && (enemy.Body.Flags & MobjFlags.Solid) == 0 && enemy.Combatant.Animation.Tics == -1, "Warrior death/corpse failed.");
            Check(drops.All(d => d.Type == HereticActorType.MT_AMCBOWWIMPY && d.Amount == 5), "Wrong Warrior drop.");
            Check(s.Projectiles.Count == 0, "Axe impacts leaked.");
        }
        var melee = new HereticWorldSession(content); var close = melee.StartEnemyTest(HereticActorType.MT_KNIGHT);
        var direction = Geometry.PointToAngle(melee.Body.X, melee.Body.Y, close.Body.X, close.Body.Y);
        melee.World.ThingMovement.UnsetThingPosition(close.Body);
        close.Body.X = melee.Body.X + 48 * Trig.Cos(direction); close.Body.Y = melee.Body.Y + 48 * Trig.Sin(direction);
        melee.World.ThingMovement.SetThingPosition(close.Body); close.Body.Target = melee.Body;
        close.Execute(HereticAction.A_KnightAttack, close.Combatant.Animation);
        Check(melee.State.Health < 100 && melee.State.Health >= 76 && (100-melee.State.Health)%3 == 0 && melee.Projectiles.Count == 0, "Warrior melee failed.");
        var ranged = new HereticWorldSession(content); var shooter = ranged.StartEnemyTest(HereticActorType.MT_KNIGHT);
        shooter.Body.Target = ranged.Body;
        ranged.World.Random.Clear(); shooter.Execute(HereticAction.A_KnightAttack, shooter.Combatant.Animation);
        Check(ranged.Projectiles.Last().Type == HereticActorType.MT_REDAXE, "Low random roll did not select red axe.");
        ranged.World.Random.Clear(); ranged.World.Random.Next(); shooter.Execute(HereticAction.A_KnightAttack, shooter.Combatant.Animation);
        Check(ranged.Projectiles.Last().Type == HereticActorType.MT_KNIGHTAXE, "High random roll did not select green axe.");
        foreach (var type in new[] { HereticActorType.MT_KNIGHTAXE, HereticActorType.MT_REDAXE })
        {
            var missile = ranged.SpawnMonsterMissile(shooter, type);
            Check(missile.Flying && missile.SeekerTarget == null && missile.Body.Target == shooter.Body, "Axe owner/nonhoming flight incorrect.");
            missile.Body.Z = ranged.Body.Z;
            var before = ranged.State.Health; ranged.World.Random.Clear();
            missile.Contact(ranged.Body);
            Check(before-ranged.State.Health == (type == HereticActorType.MT_REDAXE ? 7 : 2), "Axe damage does not use native multiplier.");
            var ghost = new Mobj(ranged.World) { Z = missile.Body.Z, Height = Fixed.FromInt(56), Flags = MobjFlags.Shadow | MobjFlags.Shootable | MobjFlags.Solid };
            var random = ranged.World.Random.Index;
            Check(missile.Contact(ghost) && random == ranged.World.Random.Index, "Axe did not pass through ghost without damage RNG.");
        }
        var red = ranged.Projectiles.Last(); red.Body.Z = ranged.Body.Z + Fixed.FromInt(32); ranged.World.Random.Clear();
        red.Execute(HereticAction.A_DripBlood, red.Animation);
        var blood = ranged.ImpactEffects.Last(); var z = blood.Body.Z;
        Check(blood.Type == HereticActorType.MT_BLOOD && ranged.World.Random.Index == 9, "Red axe blood random order incorrect.");
        ranged.Tick(default); ranged.Tick(default);
        Check(blood.Body.Z < z && blood.Body.MomZ < Fixed.Zero, "Axe blood did not fall with low gravity.");
        var pixels = new byte[320*200*4]; ranged.Body.Angle = Geometry.PointToAngle(ranged.Body.X, ranged.Body.Y, shooter.Body.X, shooter.Body.Y);
        new HereticMapPreview(content, ranged).Render(pixels, ranged.World.LevelTime);
        var output = Environment.GetEnvironmentVariable("HERETIC_KNIGHT_RGBA"); if (!string.IsNullOrEmpty(output)) File.WriteAllBytes(output, pixels);
        Console.WriteLine("PASS Undead Warriors: normal/ghost, two-throw chain, melee, axe choice/damage/ghost pass-through, low-gravity blood, morph/death/drop and cleanup");
    }
}
