// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticSkullRodChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var a = new HereticWorldSession(content); var b = new HereticWorldSession(content);
        var ea = a.StartClinkTest(); var eb = b.StartClinkTest();
        Check(ea != null && eb != null, "Hellstaff encounter fixture failed.");
        Check(!a.GoldWand.SelectWeapon(HereticWeapon.wp_skullrod), "Unowned Hellstaff selectable.");
        foreach (var s in new[] { a, b }) { s.GoldWand.GrantTestSkullRod(); s.GoldWand.SelectWeapon(HereticWeapon.wp_skullrod); }
        var sounds = 0; a.SoundRequested += (id, source) => { if (id == HereticSoundId.sfx_hrnsht) sounds++; };
        for (var i = 0; i < 140; i++)
        {
            a.Body.Angle = Geometry.PointToAngle(a.Body.X, a.Body.Y, ea.Body.X, ea.Body.Y);
            b.Body.Angle = Geometry.PointToAngle(b.Body.X, b.Body.Y, eb.Body.X, eb.Body.Y);
            a.Tick(new HereticCommand { TestAttack = true }); b.Tick(new HereticCommand { TestAttack = true });
            Check(a.World.Random.Index == b.World.Random.Index && ea.Body.Health == eb.Body.Health && a.Projectiles.Count == b.Projectiles.Count,
                "Hellstaff replay diverged.");
            for (var j = 0; j < a.Projectiles.Count; j++)
            {
                var x = a.Projectiles[j]; var y = b.Projectiles[j];
                Check(x.Body.X == y.Body.X && x.Body.Y == y.Body.Y && x.Body.Z == y.Body.Z && x.Animation.State == y.Animation.State,
                    "Hellstaff projectile replay diverged.");
                Check(x.Type == HereticActorType.MT_HORNRODFX1 && x.Body.Info == null && x.Body.State == null, "Hellstaff used Doom definitions.");
            }
        }
        Check(a.TestKills == 1 && sounds > 0 && a.GoldWand.SkullRodAmmo == 50 - a.GoldWand.SkullRodShots,
            "Hellstaff encounter damage/sound/ammo failed.");
        new HereticMapPreview(content, a).Render(new byte[320 * 200 * 4], 0);
        for (var i = 0; i < 180; i++) a.Tick(default);
        Check(a.Projectiles.Count == 0, "Hellstaff projectiles failed to expire against map geometry.");
        var cadence = new HereticWorldSession(content); cadence.StartClinkTest();
        var w = cadence.GoldWand; w.GrantTestSkullRod(3); w.SelectWeapon(HereticWeapon.wp_skullrod);
        for (var i = 0; i < 60; i++) w.Tick(false);
        w.Tick(true); Check(w.SkullRodShots == 1, "Hellstaff initial shot delayed.");
        for (var i = 0; i < 3; i++) w.Tick(false);
        Check(w.SkullRodShots == 1, "Hellstaff fired before four ticks.");
        w.Tick(false); Check(w.SkullRodShots == 2, "Hellstaff release cancelled second animation shot.");
        for (var i = 0; i < 4; i++) w.Tick(false);
        w.Tick(true);
        for (var i = 0; i < 65; i++) w.Tick(false);
        Check(w.SkullRodShots == 3 && w.SkullRodAmmo == 0 && w.ReadyWeapon == HereticWeapon.wp_goldwand,
            "Hellstaff last-ammo guard/fallback failed.");
        foreach (var type in new[] { HereticActorType.MT_WSKULLROD, HereticActorType.MT_AMSKRDWIMPY, HereticActorType.MT_AMSKRDHEFTY })
        {
            var s = new HereticWorldSession(content);
            var thing = s.World.Map.Things.First(t => t.Type == 10 && ((int)t.Flags & 2) != 0 && ((int)t.Flags & 16) == 0);
            thing.Type = HereticDefinitions.Actors[(int)type].MapNumber;
            s.StartClinkTest(); var pickup = s.Actors.First(p => p.Type == type);
            s.World.ThingMovement.UnsetThingPosition(s.Body);
            s.Body.X = pickup.Body.X; s.Body.Y = pickup.Body.Y; s.Body.Z = pickup.Body.Z;
            s.World.ThingMovement.SetThingPosition(s.Body);
            s.Body.FloorZ = pickup.Body.FloorZ; s.Body.CeilingZ = pickup.Body.CeilingZ;
            s.Tick(default);
            var weapon = type == HereticActorType.MT_WSKULLROD;
            Check(s.GoldWand.HasSkullRod == weapon && s.GoldWand.SkullRodAmmo == (weapon ? 50 : type == HereticActorType.MT_AMSKRDWIMPY ? 20 : 100)
                && pickup.Animation.Removed, "Hellstaff map pickup failed.");
        }
        var rank = new HereticWorldSession(content); rank.StartClinkTest(); var r = rank.GoldWand;
        r.GiveSkullRod(true); Check(r.SkullRodAmmo == 75, "Hellstaff weapon difficulty bonus differs.");
        for (var i = 0; i < 60; i++) r.Tick(false);
        r.GiveBlaster(false); r.GiveCrossbow(false);
        Check(r.PendingWeapon == null && r.ReadyWeapon == HereticWeapon.wp_skullrod, "Lower-ranked pickup replaced Hellstaff.");
        r.GiveSkullRodAmmo(200, false);
        Check(r.SkullRodAmmo == 200 && !r.GiveSkullRod(false), "Hellstaff cap/duplicate failed.");
        Console.WriteLine("PASS normal Hellstaff: cadence/release, native projectile damage, deterministic replay, cleanup, map pickups, ammo, ranking and fallback");
    }
}
