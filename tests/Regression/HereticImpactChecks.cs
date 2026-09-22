// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticImpactChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static bool Linked(Mobj body)
    {
        for (var item = body.Subsector.Sector.ThingList; item != null; item = item.SectorNext)
            if (item == body) return true;
        return false;
    }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var s = new HereticWorldSession(content);
        var preview = new HereticMapPreview(content, s);
        var before = new byte[320 * 200 * 4]; preview.Render(before, 0);
        var z = s.Camera.ViewZ;
        var hit = new HereticTraceHit(s.Body, null, Fixed.FromInt(64), z);
        s.World.Random.Clear();
        s.SpawnWeaponImpact(null, s.Body.Angle, Fixed.Zero, HereticWeapon.wp_staff);
        Check(s.ImpactEffects.Count == 0 && s.World.Random.Index == 0, "Miss spawned an effect or consumed random input.");
        s.SpawnWeaponImpact(hit, s.Body.Angle, Fixed.Zero, HereticWeapon.wp_staff);
        var staff = s.ImpactEffects.Single();
        Check(staff.Type == HereticActorType.MT_STAFFPUFF && staff.Body.MomZ == Fixed.One && s.World.Random.Index == 3,
            "Staff puff type, velocity or random use differs.");
        Check(staff.Body.X == s.Body.X + Fixed.FromInt(54) * Trig.Cos(s.Body.Angle) &&
            staff.Body.Y == s.Body.Y + Fixed.FromInt(54) * Trig.Sin(s.Body.Angle), "Actor impact did not back off ten units.");
        Check(Linked(staff.Body) && staff.Body.Info == null && staff.Body.State == null &&
            (staff.Body.Flags & (MobjFlags.Solid | MobjFlags.Shootable)) == 0 && (staff.Body.Flags & MobjFlags.NoBlockMap) != 0,
            "Puff is not isolated from Doom definitions/collision.");
        var after = new byte[before.Length]; preview.Render(after, 0);
        Check(!before.SequenceEqual(after), "Impact sprite not rendered in native preview.");
        var output = Environment.GetEnvironmentVariable("HERETIC_IMPACT_RGBA");
        if (!string.IsNullOrEmpty(output)) File.WriteAllBytes(output, after);
        var initialZ = staff.Body.Z;
        s.Tick(default);
        Check(staff.Body.Z == initialZ + Fixed.One, "Staff puff did not rise.");
        for (var i = 1; i < 16; i++) s.Tick(default);
        Check(s.ImpactEffects.Count == 0 && !Linked(staff.Body), "Staff puff did not expire/unlink after 16 ticks.");
        var wallHit = new HereticTraceHit(null, null, Fixed.FromInt(64), z);
        s.SpawnWeaponImpact(wallHit, s.Body.Angle, Fixed.Zero, HereticWeapon.wp_goldwand);
        var wand = s.ImpactEffects.Single();
        Check(wand.Type == HereticActorType.MT_GOLDWANDPUFF1 && wand.Body.MomZ == Fixed.Zero &&
            wand.Body.X == s.Body.X + Fixed.FromInt(60) * Trig.Cos(s.Body.Angle), "Wand wall puff placement or motion differs.");
        var line = s.World.Map.Lines[0];
        var oldFlat = line.FrontSector.CeilingFlat;
        line.FrontSector.CeilingFlat = s.World.Map.SkyFlatNumber;
        var randomIndex = s.World.Random.Index;
        try
        {
            s.SpawnWeaponImpact(new HereticTraceHit(null, line, Fixed.FromInt(64), line.FrontSector.CeilingHeight + Fixed.One),
                s.Body.Angle, Fixed.Zero, HereticWeapon.wp_goldwand);
            Check(s.ImpactEffects.Count == 1 && s.World.Random.Index == randomIndex, "Sky impact emitted a puff or consumed random input.");
        }
        finally { line.FrontSector.CeilingFlat = oldFlat; }
        s.DamageEnvironment(100);
        for (var i = 0; i < 40; i++) s.Tick(default);
        Check(s.ImpactEffects.Count == 0 && !Linked(wand.Body), "Effects stopped expiring after player death.");
        var bloodSession = new HereticWorldSession(content);
        var bloodHit = new HereticTraceHit(bloodSession.Body, null, Fixed.FromInt(48), bloodSession.Camera.ViewZ);
        bloodSession.World.Random.Clear();
        bloodSession.Body.Flags |= MobjFlags.NoBlood;
        bloodSession.SpawnWeaponBlood(bloodHit, bloodSession.Body.Angle, Fixed.Zero);
        bloodSession.SpawnWeaponBlood(null, bloodSession.Body.Angle, Fixed.Zero);
        Check(bloodSession.ImpactEffects.Count == 0 && bloodSession.World.Random.Index == 0, "Blood spawned for immune target/miss.");
        bloodSession.Body.Flags &= ~MobjFlags.NoBlood;
        bloodSession.SpawnWeaponBlood(bloodHit, bloodSession.Body.Angle, Fixed.Zero);
        var blood = bloodSession.ImpactEffects.Single();
        Check(blood.Type == HereticActorType.MT_BLOODSPLATTER && blood.Body.Target == bloodSession.Body &&
            blood.Body.MomZ == Fixed.FromInt(2) && bloodSession.World.Random.Index == 6,
            "Blood spawn type, target, velocity or random sequence differs.");
        var bloodPreview = new HereticMapPreview(content, bloodSession);
        var withBlood = new byte[320 * 200 * 4]; bloodPreview.Render(withBlood, 0);
        bloodSession.World.ThingMovement.UnsetThingPosition(blood.Body);
        var withoutBlood = new byte[withBlood.Length]; bloodPreview.Render(withoutBlood, 0);
        bloodSession.World.ThingMovement.SetThingPosition(blood.Body);
        Check(!withBlood.SequenceEqual(withoutBlood), "Blood sprite did not render.");
        var bloodZ = blood.Body.Z;
        bloodSession.Tick(default);
        Check(blood.Body.Z == bloodZ + Fixed.FromInt(2) && blood.Body.MomZ == Fixed.FromInt(2) - Fixed.One / 8,
            "Blood low gravity differs.");
        for (var i = 0; i < 40; i++) bloodSession.Tick(default);
        Check(!Linked(blood.Body) && bloodSession.ImpactEffects.Count == 0, "Blood failed to expire.");
        bloodSession.World.Random.Clear();
        bloodSession.SpawnWeaponBlood(bloodHit, bloodSession.Body.Angle, Fixed.Zero);
        var grounded = bloodSession.ImpactEffects.Single();
        grounded.Body.Z = grounded.Body.FloorZ; grounded.Body.MomZ = -Fixed.One;
        grounded.Body.MomX = grounded.Body.MomY = Fixed.Zero;
        bloodSession.Tick(default);
        Check(grounded.Animation.State == HereticStateId.S_BLOODSPLATTERX && grounded.Body.MomZ == Fixed.Zero,
            "Blood floor impact failed to enter its terminal state.");
        for (var i = 0; i < 8; i++) bloodSession.Tick(default);
        Check(!Linked(grounded.Body), "Grounded blood remained linked.");
        Console.WriteLine("PASS Heretic blood: eligibility, random sequence, rendered sprite, low gravity, floor impact and cleanup");
        Console.WriteLine("PASS Heretic impacts: rendered sprites, backoff, RNG, staff rise, collision isolation and cleanup after death");
    }
}
