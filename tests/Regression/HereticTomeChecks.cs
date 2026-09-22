// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticTomeChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static HereticWorldSession Armed(GameContent content)
    {
        var s = new HereticWorldSession(content); var enemy = s.StartClinkTest();
        s.DamageTestEnemy(enemy.Body, 10000);
        for (var i = 0; i < 60; i++) s.GoldWand.Tick(false);
        return s;
    }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var s = Armed(content); var w = s.GoldWand;
        Check(!s.UseArtifact(HereticArtifact.TomeOfPower), "Unowned Tome used.");
        s.GiveArtifact(HereticArtifact.TomeOfPower); s.GiveArtifact(HereticArtifact.TomeOfPower);
        var used = 0; s.SoundRequested += (id, source) => { if (id == HereticSoundId.sfx_artiuse) used++; };
        Check(s.UseArtifact(HereticArtifact.TomeOfPower) && s.State.WeaponPowerTics == 1400 && s.State.TomesOfPower == 1 && used == 1, "Tome activation/inventory/sound failed.");
        for (var weapon = 0; weapon < 8; weapon++) Check(w.IsPowered((HereticWeapon)weapon), "Tome missed a weapon family.");
        Check(!w.HasMace && !w.HasPhoenix && !w.HasSkullRod, "Tome granted unowned weapons.");
        w.GrantTestBlaster(4); Check(!w.SelectWeapon(HereticWeapon.wp_blaster), "Tome ignored powered ammo cost.");
        Check(!s.UseArtifact(HereticArtifact.TomeOfPower) && s.State.TomesOfPower == 1, "Early Tome refresh consumed inventory.");
        s.State.WeaponPowerTics = 129; Check(!s.UseArtifact(HereticArtifact.TomeOfPower), "Tome refreshed above threshold.");
        s.State.WeaponPowerTics = 128; Check(s.UseArtifact(HereticArtifact.TomeOfPower) && s.State.WeaponPowerTics == 1400, "Tome refresh boundary failed.");
        // Real wand attack uses the timed power without any test-power flag.
        for (var i = 0; i < 10 && w.ShotsFired == 0; i++) w.Tick(true);
        Check(s.Projectiles.Count(p => p.Type == HereticActorType.MT_GOLDWANDFX2) == 2, "Tome did not enable powered Gold Wand volley.");
        s.State.WeaponPowerTics = 1; s.Tick(default);
        Check(s.State.WeaponPowerTics == 0 && !w.IsPowered(HereticWeapon.wp_goldwand) && w.SelectWeapon(HereticWeapon.wp_blaster), "Expiry did not restore normal cost/state table.");
        Check(s.Projectiles.Any(p => p.Type == HereticActorType.MT_GOLDWANDFX2), "Expiry removed already-fired powered missiles.");

        var timer = new HereticWorldSession(content); timer.GiveArtifact(HereticArtifact.TomeOfPower); timer.UseArtifact(HereticArtifact.TomeOfPower);
        for (var i = 0; i < 1399; i++) timer.Tick(default);
        Check(timer.State.WeaponPowerTics == 1, "Tome duration off by one.");
        timer.Tick(default); Check(timer.State.WeaponPowerTics == 0, "Tome timer failed to expire.");
        foreach (var weapon in new[] { HereticWeapon.wp_staff, HereticWeapon.wp_gauntlets })
        {
            var melee = Armed(content); var mw = melee.GoldWand; mw.GrantTestGauntlets(); mw.SelectWeapon(weapon);
            for (var i = 0; i < 60; i++) mw.Tick(false);
            melee.GiveArtifact(HereticArtifact.TomeOfPower); melee.UseArtifact(HereticArtifact.TomeOfPower);
            Check(mw.State == HereticDefinitions.Weapons2[(int)weapon].Ready, "Tome did not immediately change melee ready animation.");
            melee.State.WeaponPowerTics = 1; melee.Tick(default);
            Check(mw.PendingWeapon == weapon, "Melee expiry did not schedule re-raise.");
            for (var i = 0; i < 60; i++) mw.Tick(false);
            Check(mw.ReadyWeapon == weapon && mw.State == HereticDefinitions.Weapons1[(int)weapon].Ready, "Melee did not return to normal animation.");
        }
        var phoenix = Armed(content); var pw = phoenix.GoldWand; pw.GrantTestPhoenix(3); pw.SelectWeapon(HereticWeapon.wp_phoenixrod);
        for (var i = 0; i < 60; i++) pw.Tick(false);
        phoenix.GiveArtifact(HereticArtifact.TomeOfPower); phoenix.UseArtifact(HereticArtifact.TomeOfPower);
        for (var i = 0; i < 15 && pw.PhoenixFlames == 0; i++) pw.Tick(true);
        Check(pw.PhoenixFlames > 0 && pw.PhoenixAmmo == 3, "Tome Phoenix burst did not defer its ammo charge.");
        phoenix.State.WeaponPowerTics = 1; phoenix.Tick(new HereticCommand { TestAttack = true });
        Check(pw.State == HereticStateId.S_PHOENIXREADY && pw.PhoenixAmmo == 2 && pw.Refire == 0, "Phoenix expiry did not stop/charge/reset burst.");
        var flames = pw.PhoenixFlames;
        for (var i = 0; i < 30; i++) pw.Tick(false);
        Check(pw.PhoenixAmmo == 2 && pw.PhoenixFlames == flames, "Phoenix expiry double charged or continued flames.");
        phoenix.GiveArtifact(HereticArtifact.TomeOfPower); phoenix.UseArtifact(HereticArtifact.TomeOfPower);
        phoenix.State.WeaponPowerTics = 1; phoenix.Tick(default);
        Check(pw.PhoenixAmmo == 2, "Idle Phoenix expiry consumed ammo.");
        phoenix.GiveArtifact(HereticArtifact.TomeOfPower); phoenix.UseArtifact(HereticArtifact.TomeOfPower); phoenix.DamageEnvironment(10000);
        Check(phoenix.State.WeaponPowerTics == 0 && !phoenix.UseArtifact(HereticArtifact.TomeOfPower) && !phoenix.GiveArtifact(HereticArtifact.TomeOfPower), "Death retained Tome power or allowed artifacts.");

        var pickupSession = new HereticWorldSession(content);
        var thing = pickupSession.World.Map.Things.First(t => t.Type == 10 && ((int)t.Flags & 2) != 0 && ((int)t.Flags & 16) == 0);
        thing.Type = 86; pickupSession.StartClinkTest();
        var pickup = pickupSession.Actors.First(a => a.Type == HereticActorType.MT_ARTITOMEOFPOWER);
        for (var i = 0; i < 16; i++) pickupSession.GiveArtifact(HereticArtifact.TomeOfPower);
        Check(!pickupSession.GiveArtifact(HereticArtifact.TomeOfPower), "Tome inventory exceeded cap.");
        pickupSession.World.ThingMovement.UnsetThingPosition(pickupSession.Body);
        pickupSession.Body.X = pickup.Body.X; pickupSession.Body.Y = pickup.Body.Y; pickupSession.Body.Z = pickup.Body.Z;
        pickupSession.World.ThingMovement.SetThingPosition(pickupSession.Body); pickupSession.Body.FloorZ = pickup.Body.FloorZ; pickupSession.Body.CeilingZ = pickup.Body.CeilingZ;
        pickupSession.Tick(default); Check(pickupSession.Actors.Contains(pickup), "Full Tome inventory consumed pickup.");
        pickupSession.UseArtifact(HereticArtifact.TomeOfPower); pickupSession.Tick(default);
        Check(pickupSession.State.TomesOfPower == 16 && !pickupSession.Actors.Contains(pickup) && pickupSession.ImpactEffects.Contains(pickup), "Tome pickup did not store/animate.");
        Console.WriteLine("PASS Tome: pickup/cap, activation/refresh/duration, powered tables/ammo, melee transitions, Phoenix expiry, missiles and death");
    }
}
