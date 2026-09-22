// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticBeakChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        foreach (var powered in new[] { false, true })
        {
            var s = new HereticWorldSession(content); var enemy = s.StartClinkTest(); var w = s.GoldWand;
            var direction = Geometry.PointToAngle(s.Body.X, s.Body.Y, enemy.Body.X, enemy.Body.Y);
            s.World.ThingMovement.UnsetThingPosition(enemy.Body);
            enemy.Body.X = s.Body.X + Fixed.FromInt(48) * Trig.Cos(direction);
            enemy.Body.Y = s.Body.Y + Fixed.FromInt(48) * Trig.Sin(direction);
            s.World.ThingMovement.SetThingPosition(enemy.Body);
            s.Body.Angle = direction; enemy.Body.Flags |= MobjFlags.Shadow;
            if (powered) s.State.WeaponPowerTics = 1400;
            Check(!w.SelectWeapon(HereticWeapon.wp_beak), "Beak exposed as an ordinary selectable gun.");
            w.ActivateBeak();
            Check(w.ReadyWeapon == HereticWeapon.wp_beak && w.Y == Fixed.FromInt(32) && !w.SelectWeapon(HereticWeapon.wp_goldwand), "Beak activation/switch lock failed.");
            var ammo = w.Ammo; var health = enemy.Body.Health;
            var sounds = new List<HereticSoundId>(); s.SoundRequested += (id, source) => sounds.Add(id);
            w.Tick(true);
            Check(w.BeakAttacks == 1 && w.Ammo == ammo && enemy.Body.Health == health - w.LastBeakDamage, "Peck failed damage or consumed gun ammo.");
            Check(powered ? w.LastBeakDamage >= 4 && w.LastBeakDamage <= 32 && w.LastBeakDamage % 4 == 0 : w.LastBeakDamage >= 1 && w.LastBeakDamage <= 4, "Beak damage outside native range.");
            Check(powered ? w.Tics >= 9 && w.Tics <= 12 : w.Tics >= 11 && w.Tics <= 18, "Peck animation timing outside native range.");
            Check(w.BeakPeck == 12 && s.Body.Angle == Geometry.PointToAngle(s.Body.X, s.Body.Y, enemy.Body.X, enemy.Body.Y), "Peck counter/target facing failed.");
            Check(sounds.Count(id => id >= HereticSoundId.sfx_chicpk1 && id <= HereticSoundId.sfx_chicpk3) == 1, "Peck audio selection failed.");
            Check(s.ImpactEffects.Any(e => e.Type == HereticActorType.MT_BEAKPUFF && e.Body.MomZ == Fixed.One), "Rising beak impact missing.");
            w.Tick(false); Check(w.Y == Fixed.FromInt(38) && w.BeakPeck == 9, "Peck overlay movement differs.");
            for (var i = 0; i < 30; i++) w.Tick(false);
            Check(w.BeakAttacks == 1 && w.BeakPeck == 0 && w.Y == Fixed.FromInt(32), "Release did not stop pecking/settle overlay.");
            s.State.WeaponPowerTics = 0;
            w.Tick(true); Check(w.State == HereticStateId.S_BEAKATK1_1 && w.LastBeakDamage <= 4, "Expired power retained super-chicken attack.");
            var attacks = w.BeakAttacks;
            s.DamageEnvironment(10000);
            for (var i = 0; i < 40; i++) w.Tick(true);
            Check(!w.Visible && w.BeakAttacks == attacks, "Dead player retained active beak.");
        }
        var miss = new HereticWorldSession(content); var far = miss.StartClinkTest(); var mw = miss.GoldWand;
        miss.Body.Angle = Geometry.PointToAngle(miss.Body.X, miss.Body.Y, far.Body.X, far.Body.Y);
        var originalHealth = far.Body.Health; mw.ActivateBeak(); mw.Tick(true);
        Check(far.Body.Health == originalHealth && mw.BeakAttacks == 1, "Beak damaged a target beyond melee range.");
        Console.WriteLine("PASS beak: activation/switch lock, normal/powered damage, ghost hits/range, native timing, peck movement, sounds/puffs, release and death");
    }
}
