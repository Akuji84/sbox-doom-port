// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticPlayerMorphChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static HereticWorldSession Armed(GameContent content)
    {
        var s = new HereticWorldSession(content); var enemy = s.StartClinkTest(); s.DamageTestEnemy(enemy.Body, 10000); return s;
    }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var s = Armed(content); var old = s.Body; var weapon = s.GoldWand.ReadyWeapon;
        s.State.ArmorPoints = 100; s.State.ArmorType = 1; s.State.InvisibilityTics = 100; s.State.WeaponPowerTics = 100;
        old.Flags |= MobjFlags.Shadow; s.State.Flying = true; s.State.FlightTics = 2000; old.Flags |= MobjFlags.NoGravity;
        Check(s.MorphPlayer(), "Player morph rejected.");
        Check(s.Body != old && old.Player == null && (old.Flags & (MobjFlags.Solid | MobjFlags.Shootable)) == 0 && s.Body.Player == s.Camera, "Player/camera body ownership not replaced.");
        Check(s.State.Health == 30 && s.Body.Health == 30 && s.Body.Height == Fixed.FromInt(24) && s.Body.Radius == Fixed.FromInt(16), "Player chicken dimensions/health differ.");
        Check(s.State.ArmorPoints == 0 && s.State.ArmorType == 0 && s.State.InvisibilityTics == 0 && s.State.WeaponPowerTics == 0 && (s.Body.Flags & MobjFlags.Shadow) == 0, "Morph retained armor/ghost/Tome power.");
        Check(s.State.Flying && (s.Body.Flags & MobjFlags.NoGravity) != 0 && s.GoldWand.ReadyWeapon == HereticWeapon.wp_beak && s.Camera.ViewZ == s.Body.Z + Fixed.FromInt(21), "Morph flight/beak/camera mismatch.");
        Check(!s.MorphPlayer() && s.State.WeaponPowerTics == 0, "Immediate remorph granted power.");
        s.State.ChickenTics = 1364; Check(!s.MorphPlayer() && s.State.WeaponPowerTics == 1400, "Delayed remorph did not create super chicken.");
        s.DamageEnvironment(5); Check(s.GiveHealth(100) && s.State.Health == 30, "Chicken healing exceeded cap.");
        var chicken = s.Body; var ceiling = chicken.Subsector.Sector.CeilingHeight;
        chicken.Subsector.Sector.CeilingHeight = chicken.Z + Fixed.FromInt(32);
        Check(!s.UndoPlayerChicken() && s.Body == chicken && s.State.ChickenTics == 70 && s.State.Health == 30, "Blocked player restoration did not retain body/health and retry.");
        chicken.Subsector.Sector.CeilingHeight = ceiling;
        Check(s.UndoPlayerChicken() && s.State.ChickenTics == 0 && s.State.Health == 100 && s.Body.Height == Fixed.FromInt(56) && s.Body.ReactionTime == 18 && s.GoldWand.ReadyWeapon == weapon, "Player restoration failed.");
        Check(s.State.Flying && s.State.WeaponPowerTics == 0 && s.Camera.Mobj == s.Body, "Restoration lost flight or retained super power.");
        var timer = Armed(content); timer.MorphPlayer();
        for (var i = 0; i < 1399; i++) timer.Tick(default);
        Check(timer.State.ChickenTics == 1 && timer.State.Health > 0, "Player morph duration ended early.");
        timer.Tick(default); Check(timer.State.ChickenTics == 0 && timer.State.Health == 100, "Player morph did not expire at forty seconds.");
        var immune = Armed(content); immune.State.InvulnerabilityTics = 1; var body = immune.Body; var rng = immune.World.Random.Index;
        Check(!immune.MorphPlayer() && immune.Body == body && immune.World.Random.Index == rng, "Invulnerable player morph changed state.");
        var tome = Armed(content); tome.MorphPlayer(); tome.GiveArtifact(HereticArtifact.TomeOfPower);
        Check(tome.UseArtifact(HereticArtifact.TomeOfPower) && tome.State.ChickenTics == 0 && tome.State.TomesOfPower == 0 && tome.State.Health == 100 && tome.State.WeaponPowerTics == 0, "Tome did not reverse morph.");
        var blocked = Armed(content); blocked.MorphPlayer(); blocked.GiveArtifact(HereticArtifact.TomeOfPower);
        blocked.Body.Subsector.Sector.CeilingHeight = blocked.Body.Z + Fixed.FromInt(32);
        Check(blocked.UseArtifact(HereticArtifact.TomeOfPower) && blocked.State.Health == 0 && blocked.State.TomesOfPower == 0 && blocked.State.Message == "You died.", "Blocked Tome reversal did not apply fatal rule.");
        var deadBody = blocked.Body;
        var deadView = blocked.Camera.ViewZ; blocked.Tick(default);
        Check(blocked.Camera.ViewZ <= deadView, "Dead chicken camera jumped to human eye height.");
        for (var i = 0; i < 80; i++) blocked.Tick(default);
        Check(blocked.Body == deadBody && !blocked.GoldWand.Visible && !blocked.MorphPlayer() && !blocked.UndoPlayerChicken(), "Dead chicken restored or kept its beak.");
        Console.WriteLine("PASS player morph: body/camera/flight, health/armor/powers, beak restore, duration, invulnerability, blocked retries and Tome reversal/death");
    }
}
