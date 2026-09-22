// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticSeekerChecks
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var session = new HereticWorldSession(content);
        var missile = new Mobj(session.World) { Height = Fixed.FromInt(8), MomZ = Fixed.One };
        Mobj target = new Mobj(session.World) { X = Fixed.FromInt(160), Height = Fixed.FromInt(16), Flags = MobjFlags.Shootable };
        var speed = new Fixed(HereticDefinitions.Actors[(int)HereticActorType.MT_HORNRODFX2].Speed);
        var facing = Geometry.PointToAngle(missile.X, missile.Y, target.X, target.Y).Data;
        // Both turn directions: small offsets snap, medium offsets halve,
        // large offsets cap. Expected turns use fixed reference constants.
        foreach (var offset in new uint[] { 0x05000000, 0x14000000, 0x40000000 })
        foreach (var sign in new[] { -1, 1 })
        {
            var start = unchecked(facing + (uint)(sign * (long)offset));
            missile.Angle = new Angle(start);
            var wrap = sign < 0;
            var magnitude = wrap ? offset - 1 : offset;
            var step = magnitude <= 0x0a000000 ? magnitude : Math.Min(magnitude >> 1, 0x1e000000u);
            var expected = sign < 0 ? unchecked(start + step) : unchecked(start - step);
            Check(HereticSeeker.SeekHellstaff(missile, ref target) && missile.Angle.Data == expected, "Seeker turn limit/wrap mismatch.");
            Check(missile.MomX == speed * Trig.Cos(missile.Angle) && missile.MomY == speed * Trig.Sin(missile.Angle), "Seeker did not restore native speed.");
            Check(missile.MomZ == Fixed.One, "Overlapping target changed vertical momentum.");
        }
        target.Z = Fixed.FromInt(80);
        HereticSeeker.SeekHellstaff(missile, ref target);
        var ticks = Math.Max(1, Fixed.FromInt(160).Data / speed.Data);
        Check(missile.MomZ == Fixed.FromInt(80) / ticks, "Seeker vertical interception failed.");
        target.X = Fixed.Zero; target.Z = -Fixed.FromInt(80);
        HereticSeeker.SeekHellstaff(missile, ref target);
        Check(missile.MomZ == -Fixed.FromInt(80), "Close target must use one travel tick.");
        var angle = missile.Angle; var momX = missile.MomX; var momZ = missile.MomZ;
        target.Flags &= ~MobjFlags.Shootable;
        Check(!HereticSeeker.SeekHellstaff(missile, ref target) && target == null, "Dead target retained.");
        Check(!HereticSeeker.SeekHellstaff(missile, ref target) && missile.Angle == angle && missile.MomX == momX && missile.MomZ == momZ, "Missing target changed flight.");
        Console.WriteLine("PASS Heretic seeker: bounded turns, wraparound, native speed, vertical interception and dead/missing targets");
    }
}
