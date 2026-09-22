// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticPoweredWandChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var s = new HereticWorldSession(content); var enemy = s.StartClinkTest(); var w = s.GoldWand;
        w.GrantTestPoweredGoldWand(); w.Ammo = 1;
        for (var i = 0; i < 30; i++) w.Tick(false);
        s.Body.Angle = Geometry.PointToAngle(s.Body.X, s.Body.Y, enemy.Body.X, enemy.Body.Y);
        var angle = s.Body.Angle; var health = enemy.Body.Health;
        var traces = new List<HereticWandShot>(); w.ShotFired += traces.Add;
        for (var i = 0; i < 20 && w.ShotsFired == 0; i++) w.Tick(true);
        Check(w.ShotsFired == 1 && w.Ammo == 0 && traces.Count == 5 && s.Projectiles.Count == 2, "Powered wand volley/ammo differs.");
        const uint spread = 0x20000000u / 8;
        for (var i = 0; i < 5; i++)
            Check(traces[i].Angle == angle - new Angle(spread) + new Angle((uint)i * (spread / 2)) && traces[i].Damage >= 1 && traces[i].Damage <= 8 && traces[i].Slope == traces[0].Slope, "Powered wand fan damage/angles/slope differs.");
        Check(enemy.Body.Health < health, "Powered wand traces did not damage enemy.");
        Check(s.ImpactEffects.Any(e => e.Type == HereticActorType.MT_GOLDWANDPUFF2), "Powered wand impact missing.");
        Check(s.Projectiles[0].Body.Angle == angle - new Angle(spread) && s.Projectiles[1].Body.Angle == angle + new Angle(spread), "Side missiles reacquired independent angles.");
        foreach (var bolt in s.Projectiles)
        {
            Check(bolt.Type == HereticActorType.MT_GOLDWANDFX2 && bolt.Contact(s.Body), "Powered wand bolt type/owner collision wrong.");
            Check(bolt.Body.MomZ == new Fixed(HereticDefinitions.Actors[(int)bolt.Type].Speed) * traces[0].Slope, "Side missile lost shared slope.");
        }
        for (var i = 0; i < 100; i++) s.Tick(default);
        Check(s.Projectiles.Count == 0 && w.ShotsFired == 1 && w.ReadyWeapon == HereticWeapon.wp_staff, "Powered wand cleanup/empty fallback failed.");
        var miss = new HereticWorldSession(content); var mw = new HereticGoldWand(miss); mw.GrantTestPoweredGoldWand();
        miss.State.LookDirection = 30;
        var shots = new List<HereticWandShot>(); mw.ShotFired += shots.Add;
        for (var i = 0; i < 40 && mw.ShotsFired == 0; i++) mw.Tick(true);
        Check(shots.Count == 5 && shots.All(t => t.Slope == Fixed.FromInt(30) / 173), "Powered wand free-look slope failed.");
        miss.DamageEnvironment(1000); for (var i = 0; i < 40; i++) mw.Tick(true);
        Check(!mw.Visible && mw.ShotsFired == 1, "Dead player fired powered wand.");
        Console.WriteLine("PASS powered Gold Wand: five traces/two missiles, shared slope/spread, damage/effects, ammo/fallback, free look, cleanup and death");
    }
}
