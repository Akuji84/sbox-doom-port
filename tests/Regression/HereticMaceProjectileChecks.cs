// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticMaceProjectileChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static HereticProjectile Spawn(HereticWorldSession s, HereticActorType type, int height, int vz) =>
        s.SpawnMaceProjectile(type, s.Body.X, s.Body.Y, s.Body.FloorZ + Fixed.FromInt(height),
            s.Body.Angle, Fixed.Zero, Fixed.Zero, Fixed.FromInt(vz));
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var s = new HereticWorldSession(content);
        var fast = Spawn(s, HereticActorType.MT_MACEFX1, 32, 0); fast.DropTics = 16;
        for (var i = 0; i < 15; i++) fast.Tick();
        Check(!fast.LowGravity && fast.DropTics == 4, "Fast mace dropped before sixteen ticks.");
        fast.Tick();
        Check(fast.LowGravity && fast.DropTics == 0 && fast.Body.MomX == Fixed.FromInt(7) * Trig.Cos(fast.Body.Angle), "Mace drop speed/gravity transition failed.");
        var bounceSession = new HereticWorldSession(content);
        var bounce = Spawn(bounceSession, HereticActorType.MT_MACEFX1, 1, -4);
        bounce.Advance();
        Check(bounce.Flying && bounce.Body.MomZ == Fixed.FromInt(3), "Mace first bounce restitution differs.");
        bounce.Body.Z = bounce.Body.FloorZ + Fixed.One; bounce.Body.MomZ = -Fixed.FromInt(4); bounce.Advance();
        Check(!bounce.Flying, "Small mace bounced twice.");
        for (var i = 0; i < 30; i++) bounceSession.Tick(default);
        Check(bounceSession.Projectiles.Count == 0, "Small mace explosion leaked.");
        var splitSession = new HereticWorldSession(content);
        var ball = Spawn(splitSession, HereticActorType.MT_MACEFX2, 1, -4); ball.Advance();
        Check(ball.Flying && ball.Body.MomZ == Fixed.FromInt(3) && splitSession.Projectiles.Count == 3, "Lobbed mace failed to bounce/split.");
        var fragments = splitSession.Projectiles.Where(p => p.Type == HereticActorType.MT_MACEFX3).ToArray();
        Check(fragments.Length == 2 && fragments.All(p => p.Body.Target == splitSession.Body && p.LowGravity), "Fragments lost ownership/gravity.");
        Check(fragments[0].Body.Angle == ball.Body.Angle + Angle.Ang90 && fragments[1].Body.Angle == ball.Body.Angle - Angle.Ang90, "Fragment angles differ.");
        ball.Body.Z = ball.Body.FloorZ + Fixed.One; ball.Body.MomZ = -Fixed.One; ball.Advance();
        Check(!ball.Flying && splitSession.Projectiles.Count == 3, "Slow lobbed mace kept splitting.");
        for (var i = 0; i < 160; i++) splitSession.Tick(default);
        Check(splitSession.Projectiles.Count == 0, "Mace split family failed cleanup.");
        foreach (var flat in new[] { "FLTWAWA1", "FLTLAVA1", "FLTSLUD1" })
        foreach (var type in new[] { HereticActorType.MT_MACEFX1, HereticActorType.MT_MACEFX2, HereticActorType.MT_MACEFX3 })
        {
            var liquid = new HereticWorldSession(content);
            liquid.Body.Subsector.Sector.FloorFlat = liquid.World.Map.Flats.GetNumber(flat);
            var bolt = Spawn(liquid, type, 1, -4); bolt.Advance();
            Check(bolt.Animation.Removed && liquid.Projectiles.Count == 1 && liquid.ImpactEffects.Count == 2, "Mace liquid impact bounced/split or omitted splash.");
            liquid.Tick(default); Check(liquid.Projectiles.Count == 0, "Sunken mace stayed linked.");
        }
        var hit = new HereticWorldSession(content); var enemy = hit.StartClinkTest();
        Check(enemy != null, "Mace damage encounter failed.");
        var shot = hit.SpawnPlayerProjectile(HereticActorType.MT_MACEFX1,
            Geometry.PointToAngle(hit.Body.X, hit.Body.Y, enemy.Body.X, enemy.Body.Y)); shot.DropTics = 16;
        var health = enemy.Body.Health;
        for (var i = 0; i < 40; i++) hit.Tick(default);
        Check(enemy.Body.Health < health, "Moving mace failed actual enemy damage.");
        Console.WriteLine("PASS native mace projectiles: delayed drop, low gravity, bounce limits, splitting, owner/angles, liquid sinking, damage and cleanup");
    }
}
