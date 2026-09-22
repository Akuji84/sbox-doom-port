// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticPoweredGauntletChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        foreach (var powered in new[] { false, true })
        foreach (var startingHealth in new[] { 50, 99 })
        {
            var s = new HereticWorldSession(content); var enemy = s.StartClinkTest(); var w = s.GoldWand;
            Check(enemy != null, "Gauntlet test needs enemy.");
            s.Body.Angle = Geometry.PointToAngle(s.Body.X, s.Body.Y, enemy.Body.X, enemy.Body.Y);
            if (powered) w.GrantTestPoweredGauntlets(); else w.GrantTestGauntlets();
            w.SelectWeapon(HereticWeapon.wp_gauntlets); for (var i = 0; i < 60; i++) w.Tick(false);
            s.DamageEnvironment(100 - startingHealth);
            var health = enemy.Body.Health; var ammo = w.Ammo; var sound = false;
            s.SoundRequested += (id, source) => { if (id == HereticSoundId.sfx_gntpow) sound = true; };
            for (var i = 0; i < 20 && w.GauntletAttacks == 0; i++) w.Tick(true);
            Check(w.GauntletAttacks == 1 && w.Ammo == ammo, "Gauntlet attack/ammo failed.");
            if (powered)
            {
                var damage = health - enemy.Body.Health;
                Check(damage >= 2 && damage <= 16 && damage % 2 == 0, "Powered gauntlets failed extended-range damage.");
                Check(s.State.Health == Math.Min(100, startingHealth + damage / 2) && s.Body.Health == s.State.Health, "Gauntlet healing/cap/body synchronization failed.");
                Check(sound && s.ImpactEffects.Any(e => e.Type == HereticActorType.MT_GAUNTLETPUFF2), "Powered gauntlet sound/puff missing.");
            }
            else Check(enemy.Body.Health == health && s.State.Health == startingHealth && !sound, "Normal gauntlets gained powered range/healing.");
            w.SelectWeapon(HereticWeapon.wp_goldwand); for (var i = 0; i < 60; i++) w.Tick(false);
            Check(w.State == HereticStateId.S_GOLDWANDREADY, "Powered gauntlets altered wand switching.");
            w.SelectWeapon(HereticWeapon.wp_gauntlets); for (var i = 0; i < 60; i++) w.Tick(false);
            s.DamageEnvironment(1000); for (var i = 0; i < 40; i++) w.Tick(true);
            Check(!w.Visible && s.State.Health == 0, "Dead gauntlet user attacked/healed.");
        }
        Console.WriteLine("PASS powered gauntlets: extended range, normal isolation, damage, capped healing, puff/audio, ammo, switching and death");
    }
}
