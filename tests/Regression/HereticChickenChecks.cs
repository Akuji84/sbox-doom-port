// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticChickenChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var s = new HereticWorldSession(content); var enemy = s.StartClinkTest(); var old = enemy.Body;
        old.Flags |= MobjFlags.Shadow; old.Target = s.Body;
        var angle = old.Angle; var x = old.X; var y = old.Y; var z = old.Z;
        var audio = new List<HereticSoundId>(); s.SoundRequested += (id, source) => audio.Add(id);
        Check(!s.MorphTestEnemy(s.Body), "Player accepted by ordinary enemy morph.");
        Check(s.MorphTestEnemy(old) && enemy.IsChicken && enemy.Body != old, "Clink did not become a replacement chicken.");
        Check(enemy.Body.Health == 10 && enemy.Body.Radius == Fixed.FromInt(9) && enemy.Body.Height == Fixed.FromInt(22), "Chicken health/dimensions differ.");
        Check(enemy.Body.X == x && enemy.Body.Y == y && enemy.Body.Z == z && enemy.Body.Angle == angle && enemy.Body.Target == s.Body && (enemy.Body.Flags & MobjFlags.Shadow) != 0, "Morph lost location/angle/target/ghost state.");
        Check((old.Flags & (MobjFlags.Shootable | MobjFlags.Solid)) == 0 && !s.IsTestEnemy(old) && s.IsTestEnemy(enemy.Body) && s.TestKills == 0, "Old actor retained collision/damage registration or morph counted a kill.");
        Check(enemy.ChickenTics >= 1400 && enemy.ChickenTics <= 1655 && audio.Contains(HereticSoundId.sfx_telept), "Morph duration/fog audio differs.");
        var remaining = enemy.ChickenTics;
        Check(!s.MorphTestEnemy(enemy.Body) && enemy.ChickenTics == remaining, "Chicken remorph extended lifetime.");
        var chicken = enemy.Body;
        var ceiling = chicken.Subsector.Sector.CeilingHeight;
        chicken.Subsector.Sector.CeilingHeight = chicken.Z + Fixed.FromInt(24);
        Check(!enemy.UpdateChicken(remaining) && enemy.IsChicken && enemy.Body == chicken && enemy.ChickenTics == 175 && chicken.Health == 10 && (chicken.Flags & MobjFlags.Solid) != 0, "Blocked restoration failed to retain chicken and retry timer.");
        chicken.Subsector.Sector.CeilingHeight = ceiling;
        Check(!enemy.UpdateChicken(174) && enemy.ChickenTics == 1, "Retry restored early.");
        Check(enemy.UpdateChicken(1) && !enemy.IsChicken && enemy.Body.Health == HereticDefinitions.Actors[(int)HereticActorType.MT_CLINK].SpawnHealth, "Retry did not restore Clink at full spawn health.");
        Check(enemy.Body.Target == s.Body && enemy.Body.Angle == angle && s.TestKills == 0 && (chicken.Flags & MobjFlags.Shootable) == 0, "Restoration lost target or left stale body active.");
        Check((enemy.Body.Flags & MobjFlags.Shadow) == 0, "Restored Clink incorrectly retained temporary ghost flag.");

        // Egg dispatch stays opt-in, but performs replacement when enabled.
        var eggSession = new HereticWorldSession(content); var eggEnemy = eggSession.StartClinkTest();
        eggSession.TestEnemyMorphEnabled = true;
        var egg = eggSession.SpawnAimedProjectile(HereticActorType.MT_EGGFX, eggSession.Body.Angle, Fixed.Zero);
        Check(!egg.Contact(eggEnemy.Body) && eggEnemy.IsChicken && eggSession.TestKills == 0, "Enabled egg did not morph its target.");
        var before = eggEnemy.ChickenTics;
        for (var i = 0; i < 12; i++) eggSession.Tick(default);
        Check(eggEnemy.IsChicken && eggEnemy.ChickenTics < before, "Chicken state actions did not advance duration.");
        var feathersBefore = eggSession.ImpactEffects.Count(a => a.Type == HereticActorType.MT_FEATHER);
        eggSession.SpawnChickenFeathers(eggEnemy.Body);
        var feathers = eggSession.ImpactEffects.Count(a => a.Type == HereticActorType.MT_FEATHER) - feathersBefore;
        Check(feathers >= 1 && feathers <= 2, "Live chicken feather count differs.");
        Check(eggSession.DamageTestEnemy(eggEnemy.Body, 10000) == HereticDamageResult.Killed && eggSession.TestKills == 1, "Chicken death did not count exactly one kill.");
        Check(!eggEnemy.UpdateChicken(10000), "Dead chicken restored itself.");
        var dead = eggEnemy.Body;
        for (var i = 0; i < 160; i++) eggSession.Tick(default);
        Check(eggEnemy.Body == dead && eggSession.TestKills == 1 && (dead.Flags & MobjFlags.Solid) == 0, "Chicken death retained blocking or restored a monster.");
        Check(!eggSession.ImpactEffects.Any(a => a.Type == HereticActorType.MT_FEATHER), "Feather particles did not land/expire.");
        Check(!eggSession.Actors.Any(a => a.Type == HereticActorType.MT_AMSKRDWIMPY && a.Body.Target == dead), "Chicken used Clink ammo-drop behavior.");
        Check(!eggSession.MorphTestEnemy(dead), "Corpse remorphed.");
        Console.WriteLine("PASS chicken lifecycle: replacement/ownership, timer, blocked restore/retry, egg dispatch, state ticking, death accounting and feather cleanup");
    }
}
