// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticAmmoChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var s = new HereticWorldSession(content);
        Check(!s.Actors.Any(a => a.Type == HereticActorType.MT_AMGWNDWIMPY), "Ammo enabled in navigation-only preview.");
        Check(s.StartClinkTest() != null, "Ammo fixture encounter failed.");
        var ammo = s.Actors.First(a => a.Type == HereticActorType.MT_AMGWNDWIMPY);
        var count = s.Actors.Count; s.StartClinkTest();
        Check(s.Actors.Count == count, "Combat restart duplicated ammo.");
        var weapon = s.GoldWand;
        weapon.Ammo = 100;
        s.World.ThingMovement.UnsetThingPosition(s.Body);
        s.Body.X = ammo.Body.X; s.Body.Y = ammo.Body.Y; s.Body.Z = ammo.Body.Z;
        s.World.ThingMovement.SetThingPosition(s.Body);
        s.Body.FloorZ = s.Body.Subsector.Sector.FloorHeight;
        s.Body.CeilingZ = s.Body.Subsector.Sector.CeilingHeight;
        s.Tick(default);
        Check(s.Actors.Contains(ammo) && weapon.Ammo == 100, "Full ammo consumed pickup.");
        weapon.Ammo = 95;
        var sounds = 0; s.SoundRequested += (id, source) => { if(id == HereticSoundId.sfx_itemup) sounds++; };
        s.Tick(default);
        Check(!s.Actors.Contains(ammo) && ammo.Animation.Removed && weapon.Ammo == 100 && sounds > 0,
            "Touch did not cap ammo, remove pickup and emit sound.");
        weapon.Ammo = 0;
        Check(weapon.GiveAmmo(false, 10, true) && weapon.Ammo == 15, "Difficulty ammo bonus differs.");
        Check(weapon.GiveAmmo(true, 25, true) && weapon.BlasterAmmo == 37 && !weapon.HasBlaster,
            "Ammo pickup incorrectly granted weapon or bonus differs.");
        weapon.GiveAmmo(true, 200, false);
        Check(weapon.BlasterAmmo == 200 && !weapon.GiveAmmo(true, 1, false), "Dragon Claw cap differs.");
        s.DamageEnvironment(100); weapon.Ammo = 0;
        Check(!weapon.GiveAmmo(false, 10, false), "Dead player received ammo.");
        var pickupSession = new HereticWorldSession(content);
        // Replace an unsupported ammo map thing before opt-in spawning with a weapon fixture.
        var thing = pickupSession.World.Map.Things.First(t => t.Type == 10 && ((int)t.Flags & 2) != 0 && ((int)t.Flags & 16) == 0);
        thing.Type = 53;
        Check(pickupSession.StartClinkTest() != null, "Weapon pickup fixture encounter failed.");
        var pickup = pickupSession.Actors.First(a => a.Type == HereticActorType.MT_MISC14);
        Check(!pickupSession.GoldWand.HasBlaster, "Weapon granted before touching pickup.");
        var pickupSound = false;
        pickupSession.SoundRequested += (id, source) => { if (id == HereticSoundId.sfx_wpnup) pickupSound = true; };
        pickupSession.World.ThingMovement.UnsetThingPosition(pickupSession.Body);
        pickupSession.Body.X = pickup.Body.X; pickupSession.Body.Y = pickup.Body.Y; pickupSession.Body.Z = pickup.Body.Z;
        pickupSession.World.ThingMovement.SetThingPosition(pickupSession.Body);
        pickupSession.Body.FloorZ = pickup.Body.FloorZ; pickupSession.Body.CeilingZ = pickup.Body.CeilingZ;
        pickupSession.Tick(default);
        var picked = pickupSession.GoldWand;
        Check(picked.HasBlaster && picked.BlasterAmmo == 30 && picked.PendingWeapon == HereticWeapon.wp_blaster &&
            pickup.Animation.Removed && !pickupSession.Actors.Contains(pickup) && pickupSound,
            "Map weapon pickup failed ownership/ammo/selection/removal/sound.");
        Check(picked.GiveBlaster(true) && picked.BlasterAmmo == 75, "Duplicate weapon difficulty ammo grant differs.");
        picked.GiveAmmo(true, 200, false);
        Check(!picked.GiveBlaster(false), "Owned full-ammo weapon pickup was consumed.");
        var full = new HereticGoldWand(new HereticWorldSession(content));
        full.GiveAmmo(true, 200, false);
        Check(full.GiveBlaster(false) && full.HasBlaster && full.BlasterAmmo == 200,
            "Full ammo prevented acquiring an unowned weapon.");
        var healing = new HereticWorldSession(content);
        var potionThing = healing.World.Map.Things.First(t => t.Type == 10 && ((int)t.Flags & 2) != 0 && ((int)t.Flags & 16) == 0);
        potionThing.Type = 81;
        Check(healing.StartClinkTest() != null, "Healing encounter failed.");
        var potion = healing.Actors.First(a => a.Type == HereticActorType.MT_MISC0);
        potion.Body.Health = 0;
        for (var i = 0; i < 65; i++)
        {
            potion.Tick();
            if (i == 16) Check(potion.Body.Z == potion.Body.FloorZ + new Fixed(524287), "Potion bob peak differs.");
        }
        Check(potion.Body.Z == potion.Body.FloorZ, "Potion bob did not wrap after 64 phases.");
        healing.World.ThingMovement.UnsetThingPosition(healing.Body);
        healing.Body.X = potion.Body.X; healing.Body.Y = potion.Body.Y; healing.Body.Z = potion.Body.Z;
        healing.World.ThingMovement.SetThingPosition(healing.Body);
        healing.Body.FloorZ = potion.Body.FloorZ; healing.Body.CeilingZ = potion.Body.CeilingZ;
        healing.Tick(default);
        Check(healing.Actors.Contains(potion), "Full-health player consumed potion.");
        healing.DamageEnvironment(5);
        healing.Tick(default);
        Check(healing.State.Health == 100 && healing.Body.Health == 100 && potion.Animation.Removed,
            "Potion did not heal/cap/synchronize health and disappear.");
        Check(healing.State.PickupFlash > 0, "Accepted potion did not flash.");
        healing.DamageEnvironment(30);
        Check(healing.GiveHealth(10) && healing.State.Health == 80 && healing.Body.Health == 80, "Health grant differs.");
        healing.DamageEnvironment(100);
        Check(!healing.GiveHealth(10) && healing.Body.Health == 0, "Healing resurrected a dead player.");
        Console.WriteLine("PASS Heretic healing: map collection, full-health retention, capped synchronized health and no resurrection");
        var gloves = new HereticWorldSession(content);
        var glovesThing = gloves.World.Map.Things.First(t => t.Type == 10 && ((int)t.Flags & 2) != 0 && ((int)t.Flags & 16) == 0);
        glovesThing.Type = 2005;
        Check(gloves.StartClinkTest() != null, "Gauntlet pickup encounter failed.");
        var glovePickup = gloves.Actors.First(a => a.Type == HereticActorType.MT_MISC13);
        gloves.World.ThingMovement.UnsetThingPosition(gloves.Body);
        gloves.Body.X = glovePickup.Body.X; gloves.Body.Y = glovePickup.Body.Y; gloves.Body.Z = glovePickup.Body.Z;
        gloves.World.ThingMovement.SetThingPosition(gloves.Body);
        gloves.Body.FloorZ = glovePickup.Body.FloorZ; gloves.Body.CeilingZ = glovePickup.Body.CeilingZ;
        var gloveSound = false;
        gloves.SoundRequested += (id, source) => { if (id == HereticSoundId.sfx_wpnup) gloveSound = true; };
        gloves.Tick(default);
        Check(gloves.GoldWand.HasGauntlets && glovePickup.Animation.Removed && gloveSound &&
            gloves.GoldWand.PendingWeapon == null && gloves.GoldWand.Ammo == 50,
            "Gauntlet map pickup changed ammo or displaced a higher-ranked weapon.");
        Check(!gloves.GoldWand.GiveGauntlets(), "Duplicate gauntlets accepted.");
        var staffSession = new HereticWorldSession(content); var staffWeapon = new HereticGoldWand(staffSession);
        staffWeapon.SelectWeapon(HereticWeapon.wp_staff);
        for (var i = 0; i < 60; i++) staffWeapon.Tick(false);
        Check(staffWeapon.GiveGauntlets() && staffWeapon.PendingWeapon == HereticWeapon.wp_gauntlets,
            "New gauntlets failed to replace staff.");
        var rearmSession = new HereticWorldSession(content);
        var rearm = new HereticGoldWand(rearmSession);
        rearm.GrantTestGauntlets(); rearm.SelectWeapon(HereticWeapon.wp_gauntlets);
        for (var i = 0; i < 60; i++) rearm.Tick(false);
        rearm.Ammo = 0;
        Check(rearm.GiveAmmo(false, 10, false) && rearm.PendingWeapon == HereticWeapon.wp_goldwand,
            "Gauntlets did not reselect newly replenished empty wand.");
        var unownedSession = new HereticWorldSession(content);
        var unowned = new HereticGoldWand(unownedSession);
        unowned.GrantTestGauntlets(); unowned.SelectWeapon(HereticWeapon.wp_gauntlets);
        for (var i = 0; i < 60; i++) unowned.Tick(false);
        Check(unowned.GiveAmmo(true, 10, false) && unowned.PendingWeapon == null && !unowned.HasBlaster,
            "Gauntlets selected an unowned weapon after collecting ammo.");
        Console.WriteLine("PASS Gauntlet pickup: native collection, sound, duplicate rejection, no ammo grant and weapon ranking");
        Console.WriteLine("PASS Dragon Claw pickup: map touch, ownership, ammo, selection, sound and duplicate/full-ammo behavior");
        Console.WriteLine("PASS Heretic ammo: opt-in map spawns, idempotence, touch/removal, full-cap retention, difficulty bonus and ownership isolation");
    }
}
