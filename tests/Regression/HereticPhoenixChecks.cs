// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticPhoenixChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static void Place(HereticWorldSession s, Mobj body, Fixed x, Fixed y, Fixed z)
    {
        s.World.ThingMovement.UnsetThingPosition(body); body.X = x; body.Y = y; body.Z = z;
        s.World.ThingMovement.SetThingPosition(body);
        body.FloorZ = body.Subsector.Sector.FloorHeight; body.CeilingZ = body.Subsector.Sector.CeilingHeight;
    }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var s = new HereticWorldSession(content); var e = s.StartClinkTest();
        Check(e != null && !s.GoldWand.SelectWeapon(HereticWeapon.wp_phoenixrod), "Phoenix ownership fixture failed.");
        var w = s.GoldWand; w.GrantTestPhoenix(1); w.SelectWeapon(HereticWeapon.wp_phoenixrod);
        for (var i = 0; i < 60; i++) w.Tick(false);
        s.Body.Angle = Geometry.PointToAngle(s.Body.X, s.Body.Y, e.Body.X, e.Body.Y);
        w.Tick(true);
        for (var i = 0; i < 4; i++) w.Tick(false);
        Check(w.PhoenixShots == 0, "Phoenix windup was skipped.");
        w.Tick(false);
        Check(w.PhoenixShots == 1 && w.PhoenixAmmo == 0 && s.Projectiles.Count == 1, "Phoenix last ammo did not emit one fireball.");
        var recoil = s.Body.Angle + Angle.Ang180;
        Check(s.Body.MomX == Fixed.FromInt(4) * Trig.Cos(recoil) && s.Body.MomY == Fixed.FromInt(4) * Trig.Sin(recoil), "Phoenix recoil differs.");
        var health = e.Body.Health;
        for (var i = 0; i < 180; i++) s.Tick(default);
        Check(e.Body.Health < health && s.Projectiles.Count == 0 && s.ImpactEffects.Count == 0, "Phoenix damage or effect cleanup failed.");
        Check(w.PhoenixShots == 1, "Empty Phoenix fired again.");
        var trail = new HereticWorldSession(content);
        trail.SpawnPhoenixTrail(trail.Body);
        Check(trail.ImpactEffects.Count == 2, "Phoenix trail pair missing.");
        var first = trail.ImpactEffects[0].Body; var x0 = first.X; var y0 = first.Y;
        trail.Tick(default); Check(first.X != x0 || first.Y != y0, "Phoenix trails did not move laterally.");
        for (var i = 0; i < 25; i++) trail.Tick(default);
        Check(trail.ImpactEffects.Count == 0, "Phoenix trail lifetime did not end.");
        var blast = new HereticWorldSession(content); var enemy = blast.StartClinkTest();
        var origin = new Mobj(blast.World) { X = blast.Body.X, Y = blast.Body.Y, Z = blast.Body.Z + Fixed.FromInt(32), Height = Fixed.FromInt(8) };
        blast.World.ThingMovement.SetThingPosition(origin);
        Check(blast.PhoenixBlastDamage(origin, blast.Body) == 128, "Phoenix self blast is immune or wrong at center.");
        var savedHealth = blast.State.Health;
        blast.GiveArmor(1); blast.PhoenixRadiusAttack(origin);
        Check(blast.State.Health == savedHealth - 64 && blast.Body.Health == blast.State.Health && blast.State.ArmorPoints == 36, "Phoenix self splash ignored armor/health.");
        // Find a nearby pair separated by actual map geometry to exercise sight occlusion.
        var blocked = false;
        foreach (var thing in blast.World.Map.Things)
        {
            var px = thing.X; var py = thing.Y;
            foreach (var angle in new[] { Angle.Ang0, Angle.Ang90, Angle.Ang180, Angle.Ang270 })
            {
                Place(blast, origin, px, py, Fixed.Zero); origin.Z = origin.FloorZ + Fixed.FromInt(32);
                Place(blast, blast.Body, px + Fixed.FromInt(64) * Trig.Cos(angle), py + Fixed.FromInt(64) * Trig.Sin(angle), Fixed.Zero);
                blast.Body.Z = blast.Body.FloorZ;
                if (!new VisibilityCheck(blast.World).CheckSight(blast.Body, origin))
                { Check(blast.PhoenixBlastDamage(origin, blast.Body) == 0, "Splash passed through blocking geometry."); blocked = true; break; }
            }
            if (blocked) break;
        }
        Check(blocked, "No wall-occlusion fixture found.");
        foreach (var type in new[] { HereticActorType.MT_WPHOENIXROD, HereticActorType.MT_AMPHRDWIMPY, HereticActorType.MT_AMPHRDHEFTY })
        {
            var pickupSession = new HereticWorldSession(content);
            var thing = pickupSession.World.Map.Things.First(t => t.Type == 10 && ((int)t.Flags & 2) != 0 && ((int)t.Flags & 16) == 0);
            thing.Type = HereticDefinitions.Actors[(int)type].MapNumber; pickupSession.StartClinkTest();
            var pickup = pickupSession.Actors.First(p => p.Type == type);
            Place(pickupSession, pickupSession.Body, pickup.Body.X, pickup.Body.Y, pickup.Body.Z); pickupSession.Tick(default);
            var weapon = type == HereticActorType.MT_WPHOENIXROD;
            Check(pickup.Animation.Removed && pickupSession.GoldWand.HasPhoenix == weapon &&
                pickupSession.GoldWand.PhoenixAmmo == (weapon ? 2 : type == HereticActorType.MT_AMPHRDWIMPY ? 1 : 10), "Phoenix map pickup failed.");
        }
        Console.WriteLine("PASS normal Phoenix: windup, last ammo, recoil, native damage, trails/cleanup, self splash/armor, wall occlusion and map pickups");
    }
}
