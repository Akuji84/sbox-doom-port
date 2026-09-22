// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticPoweredStaffChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        foreach (var powered in new[] { false, true })
        {
            var s = new HereticWorldSession(content); var enemy = s.StartClinkTest(); var w = s.GoldWand;
            Check(enemy != null, "Powered staff fixture needs enemy.");
            var angle = Geometry.PointToAngle(enemy.Body.X, enemy.Body.Y, s.Body.X, s.Body.Y);
            s.World.ThingMovement.UnsetThingPosition(s.Body);
            s.Body.X = enemy.Body.X + Fixed.FromInt(40) * Trig.Cos(angle);
            s.Body.Y = enemy.Body.Y + Fixed.FromInt(40) * Trig.Sin(angle);
            s.World.ThingMovement.SetThingPosition(s.Body);
            s.Body.Angle = Geometry.PointToAngle(s.Body.X, s.Body.Y, enemy.Body.X, enemy.Body.Y);
            enemy.Body.Flags |= MobjFlags.Shadow;
            if (powered) w.GrantTestPoweredStaff();
            w.SelectWeapon(HereticWeapon.wp_staff);
            for (var i = 0; i < 50; i++) w.Tick(false);
            Check(w.ReadyWeapon == HereticWeapon.wp_staff, "Staff failed selection.");
            var sound = false; s.SoundRequested += (id, source) => { if (id == HereticSoundId.sfx_stfpow) sound = true; };
            var health = enemy.Body.Health; var ammo = w.Ammo;
            for (var i = 0; i < 30 && w.StaffSwings == 0; i++) w.Tick(true);
            Check(w.StaffSwings == 1 && w.Ammo == ammo, "Staff attack did not fire or consumed ammo.");
            if (powered)
            {
                Check(health - enemy.Body.Health >= 18 && health - enemy.Body.Health <= 81, "Powered staff failed damage/ghost hit.");
                Check(enemy.Body.MomZ == Fixed.FromInt(5) && (enemy.Body.MomX != Fixed.Zero || enemy.Body.MomY != Fixed.Zero), "Powered staff failed special thrust.");
                var puff = s.ImpactEffects.Single(e => e.Type == HereticActorType.MT_STAFFPUFF2);
                Check(puff.Body.MomZ == Fixed.Zero && sound, "Powered puff motion/sound differs.");
                for (var i = 0; i < 40; i++) w.Tick(false);
                Check(w.State >= HereticStateId.S_STAFFREADY2_1 && w.State <= HereticStateId.S_STAFFREADY2_3, "Release did not return to powered ready animation.");
                w.SelectWeapon(HereticWeapon.wp_goldwand); for (var i = 0; i < 60; i++) w.Tick(false);
                Check(w.State == HereticStateId.S_GOLDWANDREADY, "Powered staff option changed normal wand states.");
            }
            else Check(enemy.Body.Health == health, "Unpowered staff hit a ghost.");
            s.DamageEnvironment(1000); for (var i = 0; i < 40; i++) w.Tick(true);
            Check(!w.Visible, "Dead player's staff did not lower.");
        }
        Console.WriteLine("PASS powered staff: selection/animation, ghost damage, special thrust, puff/audio, ammo independence, switching and death");
    }
}
