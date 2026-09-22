// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticCrossbowChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var s = new HereticWorldSession(content); var enemy = s.StartClinkTest();
        Check(enemy != null, "Crossbow encounter fixture failed.");
        var weapon = s.GoldWand;
        Check(!weapon.SelectWeapon(HereticWeapon.wp_crossbow), "Unowned crossbow selectable.");
        weapon.GrantTestCrossbow(2); weapon.SelectWeapon(HereticWeapon.wp_crossbow);
        for (var i = 0; i < 60; i++) weapon.Tick(false);
        s.Body.Angle = Geometry.PointToAngle(s.Body.X, s.Body.Y, enemy.Body.X, enemy.Body.Y);
        weapon.Tick(true);
        Check(weapon.CrossbowShots == 1 && weapon.CrossbowAmmo == 1 && s.Projectiles.Count == 3,
            "Crossbow did not emit three bolts for one ammo.");
        Check(s.Projectiles.Count(p => p.Type == HereticActorType.MT_CRBOWFX1) == 1 &&
            s.Projectiles.Count(p => p.Type == HereticActorType.MT_CRBOWFX3) == 2, "Normal bolt types differ.");
        foreach (var bolt in s.Projectiles)
            Check(bolt.Body.Info == null && bolt.Body.State == null && bolt.Contact(s.Body), "Bolt invoked Doom definitions or hit owner.");
        var before = enemy.Body.Health;
        var pixels = new byte[320 * 200 * 4]; new HereticMapPreview(content, s).Render(pixels, 0);
        for (var i = 0; i < 50; i++) s.Tick(default);
        Check(enemy.Body.Health < before, "Native moving bolts failed to damage Clink.");
        Check(s.Projectiles.Count == 0, "Crossbow impact actors failed to expire.");
        weapon.Tick(true);
        for (var i = 0; i < 60; i++) weapon.Tick(false);
        Check(weapon.CrossbowAmmo == 0 && weapon.ReadyWeapon == HereticWeapon.wp_goldwand, "Last bolt ammo/fallback failed.");
        var pickupSession = new HereticWorldSession(content);
        var thing = pickupSession.World.Map.Things.First(t => t.Type == 10 && ((int)t.Flags & 2) != 0 && ((int)t.Flags & 16) == 0);
        thing.Type = HereticDefinitions.Actors[(int)HereticActorType.MT_MISC15].MapNumber;
        Check(pickupSession.StartClinkTest() != null, "Crossbow pickup encounter failed.");
        var pickup = pickupSession.Actors.First(a => a.Type == HereticActorType.MT_MISC15);
        var sound = false;
        pickupSession.SoundRequested += (id, source) => { if (id == HereticSoundId.sfx_wpnup) sound = true; };
        pickupSession.World.ThingMovement.UnsetThingPosition(pickupSession.Body);
        pickupSession.Body.X = pickup.Body.X; pickupSession.Body.Y = pickup.Body.Y; pickupSession.Body.Z = pickup.Body.Z;
        pickupSession.World.ThingMovement.SetThingPosition(pickupSession.Body);
        pickupSession.Body.FloorZ = pickup.Body.FloorZ; pickupSession.Body.CeilingZ = pickup.Body.CeilingZ;
        pickupSession.Tick(default);
        var picked = pickupSession.GoldWand;
        Check(picked.HasCrossbow && picked.CrossbowAmmo == 10 && picked.PendingWeapon == HereticWeapon.wp_crossbow &&
            pickup.Animation.Removed && !pickupSession.Actors.Contains(pickup) && sound, "Crossbow map pickup failed.");
        Check(picked.GiveCrossbow(true) && picked.CrossbowAmmo == 25, "Duplicate crossbow bonus differs.");
        picked.GiveCrossbowAmmo(50, false);
        Check(picked.CrossbowAmmo == 50 && !picked.GiveCrossbow(false), "Full crossbow consumed duplicate.");
        var unowned = new HereticGoldWand(new HereticWorldSession(content));
        Check(unowned.GiveCrossbowAmmo(5, true) && unowned.CrossbowAmmo == 7 && !unowned.HasCrossbow,
            "Crossbow ammo incorrectly grants ownership or rounds bonus.");
        unowned.GiveCrossbowAmmo(50, false);
        Check(unowned.GiveCrossbow(false) && unowned.HasCrossbow, "Full ammo prevents crossbow acquisition.");
        var ghostSession = new HereticWorldSession(content);
        var ghost = new Mobj(ghostSession.World) { X = ghostSession.Body.X, Y = ghostSession.Body.Y, Z = ghostSession.Body.Z,
            Height = Fixed.FromInt(128), Flags = MobjFlags.Shootable | MobjFlags.Shadow };
        var small = ghostSession.SpawnCrossbowBolt(HereticActorType.MT_CRBOWFX3, ghostSession.Body.Angle);
        var large = ghostSession.SpawnCrossbowBolt(HereticActorType.MT_CRBOWFX1, ghostSession.Body.Angle);
        Check(small.Contact(ghost) && !large.Contact(ghost), "Normal crossbow ghost rules differ.");
        var overhead = new Mobj(ghostSession.World) { Z = Fixed.FromInt(30000), Height = Fixed.FromInt(16), Flags = MobjFlags.Shootable };
        Check(large.Contact(overhead), "Bolt hit vertically separated actor.");
        var sky = new HereticWorldSession(content);
        var skyBolt = sky.SpawnCrossbowBolt(HereticActorType.MT_CRBOWFX1, sky.Body.Angle);
        skyBolt.Body.Subsector.Sector.CeilingFlat = sky.World.Map.SkyFlatNumber;
        skyBolt.Body.Z = skyBolt.Body.Subsector.Sector.CeilingHeight;
        skyBolt.Advance();
        Check(skyBolt.Animation.Removed, "Sky ceiling generated a bolt explosion.");
        sky.Tick(default); Check(sky.Projectiles.Count == 0, "Sky bolt remained linked.");
        var floor = new HereticWorldSession(content);
        var floorBolt = floor.SpawnCrossbowBolt(HereticActorType.MT_CRBOWFX1, floor.Body.Angle);
        floorBolt.Body.Z = floorBolt.Body.FloorZ; floorBolt.Body.MomZ = -Fixed.One;
        floorBolt.Advance(); Check(!floorBolt.Flying && !floorBolt.Animation.Removed, "Floor impact did not enter explosion.");
        for (var i = 0; i < 30; i++) floor.Tick(default);
        Check(floor.Projectiles.Count == 0, "Floor impact failed cleanup.");
        var a = new HereticWorldSession(content); var b = new HereticWorldSession(content);
        var ea = a.StartClinkTest(); var eb = b.StartClinkTest();
        foreach (var session in new[] { a, b }) { session.GoldWand.GrantTestCrossbow(); session.GoldWand.SelectWeapon(HereticWeapon.wp_crossbow); }
        for (var i = 0; i < 160; i++)
        {
            a.Body.Angle = Geometry.PointToAngle(a.Body.X, a.Body.Y, ea.Body.X, ea.Body.Y);
            b.Body.Angle = Geometry.PointToAngle(b.Body.X, b.Body.Y, eb.Body.X, eb.Body.Y);
            a.Tick(new HereticCommand { TestAttack = true }); b.Tick(new HereticCommand { TestAttack = true });
            Check(a.World.Random.Index == b.World.Random.Index && ea.Body.Health == eb.Body.Health && a.Projectiles.Count == b.Projectiles.Count,
                "Crossbow replay diverged.");
            for (var j = 0; j < a.Projectiles.Count; j++)
                Check(a.Projectiles[j].Body.X == b.Projectiles[j].Body.X && a.Projectiles[j].Body.Y == b.Projectiles[j].Body.Y &&
                    a.Projectiles[j].Body.Z == b.Projectiles[j].Body.Z && a.Projectiles[j].Animation.State == b.Projectiles[j].Animation.State,
                    "Projectile replay position/state diverged.");
        }
        Check(a.TestKills == 1 && b.TestKills == 1, "Crossbow replay did not complete encounter.");
        Console.WriteLine("PASS normal Crossbow: native flight/damage/rendering, three bolts, ammo/fallback, owner/ghost/height rules, sky/floor cleanup and replay");
    }
}
