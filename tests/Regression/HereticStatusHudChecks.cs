// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
using ManagedDoom.Video;
static class HereticStatusHudChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var session = new HereticWorldSession(content);
        var hud = new HereticStatusHud(content.Wad);
        var screen = new DrawScreen(content.Wad, 320, 200);
        byte[] Draw()
        {
            screen.FillRect(0, 0, 320, 200, 103); hud.Render(session, screen);
            return (byte[])screen.Data.Clone();
        }
        Check(Draw().All(p => p == 103), "Status HUD leaked into geometry preview.");
        session.StartClinkTest(); var weapon = session.GoldWand;
        var normal = Draw(); Check(normal.Any(p => p != 103), "Status HUD missing.");
        Check(Enumerable.Range(0, 320).All(x => Enumerable.Range(157, 43).All(y => normal[x * 200 + y] == 103)), "Status overlaps inventory.");
        foreach (var key in new[] { HereticKeys.Yellow, HereticKeys.Green, HereticKeys.Blue })
        {
            session.State.Keys = key; var keyed = Draw();
            Check(!keyed.SequenceEqual(normal), "Collected key not displayed.");
            var slot = key == HereticKeys.Yellow ? 0 : key == HereticKeys.Green ? 1 : 2;
            for (var i = 0; i < 3; i++)
                Check(Enumerable.Range(5 + 14 * i, 10).Any(x => Enumerable.Range(130, 6).Any(y => keyed[x * 200 + y] != 103)) == (i == slot), "Key displayed in wrong slot.");
        }
        session.State.Keys = HereticKeys.None;
        Check(Draw().SequenceEqual(normal), "Removed keys left stale artwork.");
        session.State.Health = 17; session.State.ArmorPoints = 200;
        Check(!Draw().SequenceEqual(normal), "Health/armor did not update.");
        weapon.GrantTestCrossbow(21); weapon.GrantTestBlaster(31); weapon.GrantTestSkullRod(41);
        weapon.GrantTestPhoenix(11); weapon.GrantTestMace(51); weapon.GrantTestGauntlets();
        var choices = new[] { (HereticWeapon.wp_crossbow, 21), (HereticWeapon.wp_blaster, 31),
            (HereticWeapon.wp_skullrod, 41), (HereticWeapon.wp_phoenixrod, 11), (HereticWeapon.wp_mace, 51),
            (HereticWeapon.wp_goldwand, 50), (HereticWeapon.wp_staff, -1), (HereticWeapon.wp_gauntlets, -1) };
        foreach (var (choice, expected) in choices)
        {
            for (var i = 0; i < 40; i++) weapon.Tick(false);
            Check(weapon.SelectWeapon(choice), "HUD fixture switch rejected.");
            for (var i = 0; i < 80; i++) weapon.Tick(false);
            Check(weapon.ReadyWeapon == choice && (HereticStatusHud.CurrentAmmo(weapon) ?? -1) == expected, "Wrong selected-weapon ammo.");
            Draw();
        }
        session.MorphPlayer(); var chicken = Draw();
        Check(HereticStatusHud.CurrentAmmo(weapon) == null && !chicken.SequenceEqual(normal), "Chicken retained ammo readout.");
        var rng = session.World.Random.Index; var time = session.World.LevelTime;
        Check(Draw().SequenceEqual(chicken) && rng == session.World.Random.Index && time == session.World.LevelTime, "HUD mutates simulation.");
        session.State.Health = -100; var dead = Draw(); session.State.Health = 0;
        Check(Draw().SequenceEqual(dead), "Death health not clamped to zero.");
        Console.WriteLine("PASS status HUD: combat gating, health/armor, all weapon ammo, melee/chicken, death, inventory spacing and read-only rendering");
    }
}
