// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticEggChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var s = new HereticWorldSession(content);
        s.SpawnEggVolley();
        Check(s.Projectiles.Count == 5 && s.Projectiles.All(p => p.Type == HereticActorType.MT_EGGFX), "Egg volley count/type mismatch.");
        var angles = new[] { s.Body.Angle, s.Body.Angle - Angle.Ang45 / 6, s.Body.Angle + Angle.Ang45 / 6, s.Body.Angle - Angle.Ang45 / 3, s.Body.Angle + Angle.Ang45 / 3 };
        for (var i = 0; i < 5; i++)
        {
            var egg = s.Projectiles[i];
            Check(egg.Body.Angle == angles[i] && egg.Body.Target == s.Body && egg.Contact(s.Body), "Egg spread or owner exclusion differs.");
            Check(egg.Body.MomX == Fixed.FromInt(18) * Trig.Cos(angles[i]) && egg.Body.MomY == Fixed.FromInt(18) * Trig.Sin(angles[i]), "Egg speed differs.");
        }
        var targetSession = new HereticWorldSession(content); var enemy = targetSession.StartClinkTest();
        var hits = 0; Mobj hitTarget = null;
        targetSession.EggMorphRequested += body => { hits++; hitTarget = body; };
        var projectile = targetSession.SpawnAimedProjectile(HereticActorType.MT_EGGFX, targetSession.Body.Angle, Fixed.Zero);
        var health = enemy.Body.Health; var momX = enemy.Body.MomX; var momY = enemy.Body.MomY;
        var rng = targetSession.World.Random.Index;
        Check(!projectile.Contact(enemy.Body) && hits == 1 && hitTarget == enemy.Body, "Egg did not dispatch morph contact.");
        Check(enemy.Body.Health == health && enemy.Body.MomX == momX && enemy.Body.MomY == momY && targetSession.TestKills == 0, "Egg applied ordinary damage/thrust/kill.");
        Check(targetSession.World.Random.Index == ((rng + 1) & 255), "Egg damage roll ordering differs.");
        enemy.Body.Flags |= MobjFlags.Shadow;
        Check(!projectile.Contact(enemy.Body) && hits == 2, "Egg incorrectly passed through a ghost.");
        projectile.Body.Z = enemy.Body.Z + enemy.Body.Height + Fixed.One;
        Check(projectile.Contact(enemy.Body) && hits == 2, "Egg contacted target outside its height.");
        // Actual collision must stop the egg and run its finite impact chain.
        projectile.Body.Z = projectile.Body.FloorZ + Fixed.One;
        projectile.Body.MomX = projectile.Body.MomY = Fixed.Zero; projectile.Body.MomZ = -Fixed.FromInt(2);
        projectile.Advance();
        Check(!projectile.Flying && projectile.Animation.State == HereticStateId.S_EGGFXI1_1, "Egg floor impact did not start animation.");
        for (var i = 0; i < 12; i++) projectile.Tick();
        Check(projectile.Animation.Removed, "Egg impact did not expire.");
        var aimed = new HereticWorldSession(content); var aimTarget = aimed.StartClinkTest();
        aimed.Body.Angle = Geometry.PointToAngle(aimed.Body.X, aimed.Body.Y, aimTarget.Body.X, aimTarget.Body.Y);
        aimed.SpawnEggVolley();
        Check(aimed.Projectiles.Any(p => p.Body.MomZ != Fixed.Zero), "Egg volley did not autoaim vertically.");
        var look = new HereticWorldSession(content); look.State.LookDirection = 30; look.SpawnEggVolley();
        Check(look.Projectiles.All(p => p.Body.MomZ == Fixed.FromInt(18) * (Fixed.FromInt(30) / 173)), "Egg free-look fallback differs.");
        for (var i = 0; i < 180; i++) s.Tick(default);
        Check(s.Projectiles.Count == 0, "Egg volley leaked after impacts.");
        s.DamageEnvironment(10000); s.SpawnEggVolley(); Check(s.Projectiles.Count == 0, "Dead player spawned eggs.");
        Check(!Enum.GetNames<HereticArtifact>().Any(n => n.Contains("Egg") || n.Contains("Ovum")), "Incomplete morph artifact exposed in inventory.");
        Console.WriteLine("PASS egg foundation: five-shot spread, speed/aim, owner/ghost/height contact, no ordinary damage, impact cleanup and inventory gate");
    }
}
