// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticOphidianChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var s = new HereticWorldSession(content); var enemy = s.StartEnemyTest(HereticActorType.MT_SNAKE);
        Check(enemy != null && enemy.Body.Health == 280 && enemy.Body.Info == null, "Ophidian native spawn failed.");
        enemy.Body.Target = s.Body;
        var attacks = new List<(int tick, HereticActorType type)>(); var sounds = new List<HereticSoundId>();
        enemy.SoundRequested += (id, _) => sounds.Add(id);
        enemy.Combatant.Animation.SetState(HereticStateId.S_SNAKE_ATK1);
        for (var tick = 1; tick <= 37; tick++)
        {
            var count = s.Projectiles.Count; enemy.Tick();
            if (s.Projectiles.Count > count) attacks.Add((tick, s.Projectiles.Last().Type));
        }
        Check(attacks.SequenceEqual(new[] { (10, HereticActorType.MT_SNAKEPRO_A), (14, HereticActorType.MT_SNAKEPRO_A), (18, HereticActorType.MT_SNAKEPRO_A), (37, HereticActorType.MT_SNAKEPRO_B) }), "Ophidian burst timing/types differ from native sequence.");
        Check(sounds.Count(x => x == HereticSoundId.sfx_snkatk) == 4, "Ophidian attack audio missing.");
        foreach (var missile in s.Projectiles)
        {
            Check(missile.Body.Target == enemy.Body && missile.SeekerTarget == null, "Ophidian projectile owner/homing mismatch.");
            var speed = Fixed.FromInt(14);
            Check(missile.Body.MomX == speed * Trig.Cos(missile.Body.Angle) && missile.Body.MomY == speed * Trig.Sin(missile.Body.Angle), "Ophidian missile speed incorrect.");
            var random = s.World.Random.Index;
            Check(missile.Contact(enemy.Body) && s.World.Random.Index == random, "Ophidian projectile damaged owner.");
        }
        foreach (var type in new[] { HereticActorType.MT_SNAKEPRO_A, HereticActorType.MT_SNAKEPRO_B })
        {
            var missile = s.SpawnMonsterMissile(enemy, type); missile.Body.Z = s.Body.Z;
            var health = s.State.Health; s.World.Random.Clear(); missile.Contact(s.Body);
            Check(health - s.State.Health == (type == HereticActorType.MT_SNAKEPRO_A ? 1 : 3), "Wrong Ophidian projectile damage multiplier.");
        }
        enemy.Body.Target = null; var before = s.Projectiles.Count;
        enemy.Execute(HereticAction.A_SnakeAttack2, enemy.Combatant.Animation);
        Check(s.Projectiles.Count == before && !enemy.Combatant.Animation.Removed, "Missing target spawned projectile or removed Ophidian.");
        var pixels = new byte[320*200*4]; s.Body.Angle = Geometry.PointToAngle(s.Body.X, s.Body.Y, enemy.Body.X, enemy.Body.Y);
        new HereticMapPreview(content, s).Render(pixels, s.World.LevelTime);
        var output = Environment.GetEnvironmentVariable("HERETIC_OPHIDIAN_RGBA"); if (!string.IsNullOrEmpty(output)) File.WriteAllBytes(output, pixels);
        Check(enemy.MorphToChicken() && enemy.UpdateChicken(2000) && enemy.Combatant.Type == HereticActorType.MT_SNAKE, "Ophidian morph restoration failed.");
        s.DamageTestEnemy(enemy.Body, 10000);
        for (var i = 0; i < 200; i++) s.Tick(default);
        Check(s.Projectiles.Count == 0 && s.TestKills == 1 && (enemy.Body.Flags & MobjFlags.Solid) == 0 && enemy.Combatant.Animation.Tics == -1, "Ophidian death/projectile cleanup failed.");
        var death = new HereticWorldSession(content); var victim = death.StartEnemyTest(HereticActorType.MT_SNAKE);
        var drops = new List<HereticTestDrop>(); victim.DropRequested += drops.Add;
        death.DamageTestEnemy(victim.Body, 10000); death.World.Random.Clear();
        for (var i = 0; i < 100; i++) death.Tick(default);
        Check(drops.Count == 1 && drops[0].Type == HereticActorType.MT_AMPHRDWIMPY && drops[0].Amount == 5, "Ophidian death did not drop five Phoenix rounds.");
        var pickup = new HereticWorldSession(content); pickup.StartEnemyTest(HereticActorType.MT_SNAKE);
        var ammo = pickup.GoldWand.PhoenixAmmo;
        pickup.SpawnEnemyAmmoDrop(pickup.Body, HereticActorType.MT_AMPHRDWIMPY, 5); pickup.Tick(default);
        Check(pickup.GoldWand.PhoenixAmmo == ammo + 5, "Ophidian drop granted normal one-round pickup.");
        var live = new HereticWorldSession(content); var attacker = live.StartEnemyTest(HereticActorType.MT_SNAKE);
        var fired = false;
        for (var i = 0; i < 200 && live.State.Health == 100; i++) { live.Tick(default); fired |= live.Projectiles.Count > 0; }
        Check(fired && live.State.Health < 100, "Live Ophidian encounter did not detect/fire/hit player.");
        Console.WriteLine("PASS Ophidian: native four-shot timing, projectiles/speed/damage, owner exclusion, missing target, render, morph/death/cleanup, five-round drop and live combat");
    }
}
