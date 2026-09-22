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
        Console.WriteLine("PASS Heretic ammo: opt-in map spawns, idempotence, touch/removal, full-cap retention, difficulty bonus and ownership isolation");
    }
}
