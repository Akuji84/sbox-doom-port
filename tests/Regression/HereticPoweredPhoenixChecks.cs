// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticPoweredPhoenixChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var s = new HereticWorldSession(content); var enemy = s.StartClinkTest(); var w = s.GoldWand;
        w.GrantTestPoweredPhoenix(2); w.SelectWeapon(HereticWeapon.wp_phoenixrod);
        for (var i = 0; i < 60; i++) w.Tick(false);
        s.Body.Angle = Geometry.PointToAngle(s.Body.X, s.Body.Y, enemy.Body.X, enemy.Body.Y);
        var sound = false; s.SoundRequested += (id, source) => { if (id == HereticSoundId.sfx_phopow) sound = true; };
        for (var i = 0; i < 10 && w.PhoenixFlames == 0; i++) w.Tick(true);
        Check(w.PhoenixFlames == 1 && w.PhoenixAmmo == 2 && sound, "Flame windup/ammo timing/audio failed.");
        var flame = s.Projectiles.Single();
        Check(flame.Type == HereticActorType.MT_PHOENIXFX2 && flame.Contact(s.Body), "Flame owner/type failed.");
        var health = enemy.Body.Health;
        for (var i = 0; i < 60; i++) s.Tick(default);
        Check(w.PhoenixAmmo == 1 && w.PhoenixFlames == 1 && enemy.Body.Health < health && s.Projectiles.Count == 0, "Flame release/cost/damage/cleanup failed.");
        for (var i = 0; i < 400; i++) w.Tick(true);
        Check(w.PhoenixAmmo == 0 && w.PhoenixFlames == 350, "Maximum burst did not emit 349 flames and charge once.");
        var count = w.PhoenixFlames; for (var i = 0; i < 30; i++) w.Tick(true);
        Check(w.PhoenixFlames == count && w.PhoenixAmmo == 0, "Empty held Phoenix restarted.");
        var d = new HereticWorldSession(content); d.StartClinkTest(); var dw = d.GoldWand;
        dw.GrantTestPoweredPhoenix(2); dw.SelectWeapon(HereticWeapon.wp_phoenixrod);
        for (var i = 0; i < 60; i++) dw.Tick(false);
        for (var i = 0; i < 10 && dw.PhoenixFlames == 0; i++) dw.Tick(true);
        dw.SelectWeapon(HereticWeapon.wp_goldwand); for (var i = 0; i < 60; i++) dw.Tick(false);
        Check(dw.ReadyWeapon == HereticWeapon.wp_goldwand && dw.PhoenixAmmo == 1, "Switching interrupted shutdown charge.");
        dw.SelectWeapon(HereticWeapon.wp_phoenixrod); for (var i = 0; i < 60; i++) dw.Tick(false);
        for (var i = 0; i < 10 && dw.PhoenixFlames == 1; i++) dw.Tick(true);
        count = dw.PhoenixFlames; d.DamageEnvironment(1000);
        for (var i = 0; i < 60; i++) d.Tick(default);
        Check(!dw.Visible && dw.PhoenixFlames == count && d.Projectiles.Count == 0, "Dead Phoenix user kept firing or leaked flames.");
        Console.WriteLine("PASS powered Phoenix: windup, deferred ammo, sustained limit, release/switching, damage/audio, owner exclusion, cleanup and death");
    }
}
