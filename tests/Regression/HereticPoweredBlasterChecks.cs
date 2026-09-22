// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticPoweredBlasterChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        foreach (var ammo in new[] { 4, 5, 9 })
        {
            var s = new HereticWorldSession(content); var enemy = s.StartClinkTest(); var w = s.GoldWand;
            w.GrantTestPoweredBlaster(ammo);
            if (ammo == 4) { Check(!w.SelectWeapon(HereticWeapon.wp_blaster), "Selected powered Claw without five ammo."); continue; }
            Check(w.SelectWeapon(HereticWeapon.wp_blaster), "Powered Claw selection failed.");
            for (var i = 0; i < 60; i++) w.Tick(false);
            s.Body.Angle = Geometry.PointToAngle(s.Body.X, s.Body.Y, enemy.Body.X, enemy.Body.Y);
            var health = enemy.Body.Health;
            for (var i = 0; i < 20 && w.BlasterShots == 0; i++) w.Tick(true);
            Check(w.BlasterShots == 1 && w.BlasterAmmo == ammo - 5 && s.Projectiles.Count == 1, "Powered Claw shot cost/projectile failed.");
            var bolt = s.Projectiles.Single();
            Check(bolt.Type == HereticActorType.MT_BLASTERFX1 && bolt.Contact(s.Body) && bolt.Body.Info == null && bolt.Body.State == null, "Blaster projectile owner/type isolation failed.");
            for (var i = 0; i < 100; i++) s.Tick(default);
            Check(enemy.Body.Health < health && s.Projectiles.Count == 0, "Fast projectile tunneled through target or failed cleanup.");
            Check(w.BlasterShots == 1 && w.BlasterAmmo == ammo - 5 && w.ReadyWeapon != HereticWeapon.wp_blaster, "Insufficient remainder fired or remained selected.");
        }
        var rip = new HereticWorldSession(content); var victim = rip.StartClinkTest();
        var origin = new Mobj(rip.World) { X = rip.Body.X, Y = rip.Body.Y, Z = rip.Body.Z + Fixed.FromInt(32), Target = rip.Body };
        rip.SpawnBlasterRippers(origin);
        Check(rip.Projectiles.Count == 8 && rip.Projectiles.All(p => p.Type == HereticActorType.MT_RIPPER && p.Body.Target == rip.Body), "Explosion did not spawn eight owned rippers.");
        victim.Body.Flags &= ~MobjFlags.NoBlood; // Exercise the bleeding-target path; native Clinks suppress blood.
        var fragment = rip.Projectiles.First(); var victimHealth = victim.Body.Health;
        Check(fragment.Contact(victim.Body) && victim.Body.Health < victimHealth && fragment.Flying, "Ripper did not damage and pass through target.");
        Check(rip.ImpactEffects.Any(e => e.Type == HereticActorType.MT_BLOOD), "Ripper blood missing.");
        victim.Body.Flags |= MobjFlags.Shadow; victimHealth = victim.Body.Health;
        Check(fragment.Contact(victim.Body) && victim.Body.Health < victimHealth, "Ripper failed native damage against ghosts.");
        var fx = new HereticWorldSession(content); fx.SpawnBlasterSmoke(fx.Body);
        var smoke = fx.ImpactEffects.Single();
        Check(smoke.Type == HereticActorType.MT_BLASTERSMOKE && smoke.Body.Z == fx.Body.FloorZ && (smoke.Body.Flags & MobjFlags.Shadow) != 0, "Smoke floor clamp/translucency failed.");
        for (var i = 0; i < 100; i++) fx.Tick(default);
        Check(smoke.Animation.Removed && fx.ImpactEffects.Count == 0, "Blaster smoke failed cleanup.");
        var normal = new HereticWorldSession(content); normal.StartClinkTest(); normal.GoldWand.GrantTestBlaster(1);
        Check(normal.GoldWand.SelectWeapon(HereticWeapon.wp_blaster), "Normal Claw no longer accepts one ammo.");
        Console.WriteLine("PASS powered Dragon Claw: five-ammo gate/cost, fast projectile damage, owner exclusion, remainder fallback, smoke and normal isolation");
    }
}
