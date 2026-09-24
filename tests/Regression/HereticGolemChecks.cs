// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticGolemChecks
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        foreach (var type in new[] { HereticActorType.MT_MUMMY, HereticActorType.MT_MUMMYGHOST })
        {
            var s = new HereticWorldSession(content); var enemy = s.StartEnemyTest(type);
            Check(enemy != null && enemy.Combatant.Type == type && enemy.Body.Info == null, "Golem spawn/native definition failed.");
            Check(((enemy.Body.Flags & MobjFlags.Shadow) != 0) == (type == HereticActorType.MT_MUMMYGHOST), "Golem ghost flag incorrect.");
            var sounds = new List<HereticSoundId>(); enemy.SoundRequested += (id, _) => sounds.Add(id);
            for (var i = 0; i < 200 && s.State.Health == 100; i++) s.Tick(default);
            Check(s.State.Health < 100 && s.State.Health >= 84 && (100 - s.State.Health) % 2 == 0, "Golem did not approach/melee for 2-16 damage.");
            Check(sounds.Contains(HereticSoundId.sfx_mumat2), "Golem hit audio missing.");
            var pixels = new byte[320 * 200 * 4];
            s.Body.Angle = Geometry.PointToAngle(s.Body.X, s.Body.Y, enemy.Body.X, enemy.Body.Y);
            new HereticMapPreview(content, s).Render(pixels, s.World.LevelTime);
            var output = Environment.GetEnvironmentVariable("HERETIC_GOLEM_RGBA");
            if (type == HereticActorType.MT_MUMMY && !string.IsNullOrEmpty(output)) File.WriteAllBytes(output, pixels);
            Check(enemy.MorphToChicken() && enemy.IsChicken, "Golem egg morph failed.");
            Check(enemy.UpdateChicken(2000) && enemy.Combatant.Type == type, "Chicken did not restore original Golem variant.");
            Check(((enemy.Body.Flags & MobjFlags.Shadow) != 0) == (type == HereticActorType.MT_MUMMYGHOST), "Restored ghost lost flags.");
            Check(s.DamageTestEnemy(enemy.Body, 10000) == HereticDamageResult.Killed && s.TestKills == 1, "Golem death count failed.");
            var sawSoul = false;
            for (var i = 0; i < 100; i++)
            {
                s.Tick(default);
                sawSoul |= s.ImpactEffects.Any(e => e.Type == HereticActorType.MT_MUMMYSOUL);
            }
            Check(sawSoul && !s.ImpactEffects.Any(e => e.Type == HereticActorType.MT_MUMMYSOUL), "Death soul missing or leaked.");
            Check((enemy.Body.Flags & MobjFlags.Solid) == 0 && enemy.Combatant.Animation.Tics == -1, "Golem corpse remained solid/unfinished.");
            Check(s.DamageTestEnemy(enemy.Body, 1000) == HereticDamageResult.Ignored && s.TestKills == 1, "Duplicate Golem kill.");
        }
        var shooting = new HereticWorldSession(content); var target = shooting.StartEnemyTest(HereticActorType.MT_MUMMY);
        shooting.State.InvulnerabilityTics = 1000;
        for (var i = 0; i < 300 && target.Body.Health > 0; i++)
        {
            shooting.Body.Angle = Geometry.PointToAngle(shooting.Body.X, shooting.Body.Y, target.Body.X, target.Body.Y);
            shooting.Tick(new HereticCommand { TestAttack = true });
        }
        Check(target.Body.Health <= 0 && shooting.TestKills == 1 && shooting.GoldWand.Ammo < 50, "Player weapon did not kill Golem through native combat.");
        var pickup = new HereticWorldSession(content); pickup.StartEnemyTest(HereticActorType.MT_MUMMY);
        var before = pickup.GoldWand.Ammo;
        var drop = pickup.SpawnEnemyAmmoDrop(pickup.Body, HereticActorType.MT_AMGWNDWIMPY, 3);
        pickup.Tick(default);
        Check(drop.Amount == 3 && pickup.GoldWand.Ammo == before + 3, "Golem drop granted normal ten-round pickup instead of three.");
        foreach (var skill in new[] { GameSkill.Easy, GameSkill.Medium, GameSkill.Hard })
        {
            var map = new HereticWorldSession(content, 1, 1, skill);
            Check(map.TestEnemyCount == 0 && map.GoldWand == null, "Map combat enabled by default.");
            var expected = map.World.Map.Things.Count(t => {
                var d = HereticMapSpawns.Decide(t, skill);
                return d.Disposition == HereticSpawnDisposition.Unsupported && HereticWorldSession.SupportsMapEnemy(d.Type);
            });
            map.StartMapCombat();
            Check(expected > 0 && map.TestEnemyCount + map.BlockedMapEnemies == expected, "Map monster filtering/accounting failed.");
            var count = map.TestEnemyCount; map.StartMapCombat();
            Check(map.TestEnemyCount == count && map.GoldWand != null, "Repeated map combat spawned duplicates.");
            Check(map.CombatEnemies.All(e => HereticWorldSession.SupportsMapEnemy(e.Combatant.Type)), "Unsupported monster escaped gate.");
            for (var i = 0; i < 70; i++) map.Tick(default);
            new HereticMapPreview(content, map).Render(new byte[320 * 200 * 4], map.World.LevelTime);
        }
        var mapCount = 0; var spawned = 0; var blocked = 0;
        foreach (var lump in content.Wad.LumpInfos.Where(l => l.Name.Length == 4 && l.Name[0] == 'E' && l.Name[2] == 'M'))
        {
            var map = new HereticWorldSession(content, lump.Name[1] - '0', lump.Name[3] - '0');
            map.StartMapCombat(); spawned += map.TestEnemyCount; blocked += map.BlockedMapEnemies;
            for (var i = 0; i < 10; i++) map.Tick(default);
            new HereticMapPreview(content, map).Render(new byte[320 * 200 * 4], map.World.LevelTime);
            mapCount++;
        }
        Check(mapCount == 48 && spawned > 0, "Map combat smoke coverage incomplete.");
        Console.WriteLine($"PASS map combat smoke: {mapCount} maps, {spawned} native enemies, {blocked} blocked placements reported");
        var a = new HereticWorldSession(content); var b = new HereticWorldSession(content);
        a.StartMapCombat(); b.StartMapCombat();
        for (var i = 0; i < 200; i++)
        {
            a.Tick(new HereticCommand { TestAttack = true }); b.Tick(new HereticCommand { TestAttack = true });
            Check(a.State.Health == b.State.Health && a.World.Random.Index == b.World.Random.Index &&
                a.CombatEnemies.Select(e => (e.Body.X.Data, e.Body.Y.Data, e.Body.Health, e.Combatant.Animation.State)).SequenceEqual(
                b.CombatEnemies.Select(e => (e.Body.X.Data, e.Body.Y.Data, e.Body.Health, e.Combatant.Animation.State))), "Map combat replay diverged.");
        }
        Console.WriteLine("PASS native Golems: normal/ghost melee, sounds, morph restoration, corpse/soul cleanup, exact ammo drops, skill-filtered map combat and deterministic replay");
    }
}
