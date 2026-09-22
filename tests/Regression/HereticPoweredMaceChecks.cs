// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticPoweredMaceChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var s = new HereticWorldSession(content); var enemy = s.StartClinkTest(); var w = s.GoldWand;
        w.GrantTestPoweredMace(4); Check(!w.SelectWeapon(HereticWeapon.wp_mace), "Powered mace selected without five ammo.");
        w.GrantTestPoweredMace(9); w.SelectWeapon(HereticWeapon.wp_mace);
        for (var i = 0; i < 60; i++) w.Tick(false);
        s.Body.Angle = Geometry.PointToAngle(s.Body.X, s.Body.Y, enemy.Body.X, enemy.Body.Y);
        for (var i = 0; i < 20 && w.MaceShots == 0; i++) w.Tick(true);
        Check(w.MaceShots == 1 && w.MaceAmmo == 4 && s.Projectiles.Count == 1, "Death ball ammo/spawn failed.");
        var ball = s.Projectiles.Single();
        Check(ball.Type == HereticActorType.MT_MACEFX4 && ball.LowGravity && ball.SeekerTarget == enemy.Body && ball.Contact(s.Body), "Death ball target/owner/gravity failed.");
        Check(!ball.Contact(enemy.Body) && enemy.Body.Health <= 0 && s.TestKills == 1, "Death ball did not kill registered ordinary enemy.");
        for (var i = 0; i < 80; i++) w.Tick(false);
        Check(w.ReadyWeapon != HereticWeapon.wp_mace && w.MaceAmmo == 4, "Mace remainder fallback failed.");
        var bounce = new HereticWorldSession(content); var target = bounce.StartClinkTest();
        var b = bounce.SpawnMaceProjectile(HereticActorType.MT_MACEFX4, bounce.Body.X, bounce.Body.Y,
            bounce.Body.FloorZ + Fixed.One, Angle.Ang0, Fixed.Zero, Fixed.Zero, -Fixed.FromInt(2));
        b.SeekerTarget = target.Body; var sound = false;
        bounce.SoundRequested += (id, source) => { if (id == HereticSoundId.sfx_pstop) sound = true; };
        b.Advance();
        Check(b.Flying && b.Body.MomZ > Fixed.Zero && sound && b.Body.Angle == Geometry.PointToAngle(b.Body.X, b.Body.Y, target.Body.X, target.Body.Y), "Death ball bounce/seek/sound failed.");
        target.Body.Flags &= ~MobjFlags.Shootable;
        b.Body.Z = b.Body.FloorZ; b.Body.MomZ = Fixed.One;
        b.Animation.SetState(HereticDefinitions.Actors[(int)b.Type].DeathState);
        Check(b.SeekerTarget == null && b.Flying, "Dead seeking target retained.");
        foreach (var flat in new[] { "FLTWAWA1", "FLTLAVA1", "FLTSLUD1" })
        {
            var liquid = new HereticWorldSession(content);
            liquid.Body.Subsector.Sector.FloorFlat = liquid.World.Map.Flats.GetNumber(flat);
            var sink = liquid.SpawnMaceProjectile(HereticActorType.MT_MACEFX4, liquid.Body.X, liquid.Body.Y,
                liquid.Body.FloorZ + Fixed.One, Angle.Ang0, Fixed.Zero, Fixed.Zero, -Fixed.FromInt(2));
            sink.Advance(); Check(sink.Animation.Removed && !sink.Flying, "Death ball bounced out of liquid.");
        }
        Console.WriteLine("PASS powered Firemace: ammo gate/cost/fallback, owner and target, ordinary lethal damage, bounce/seek/dead target and liquids");
    }
}
