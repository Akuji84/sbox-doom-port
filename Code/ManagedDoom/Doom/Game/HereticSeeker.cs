//
// Copyright(C) 1993-1996 Id Software, Inc.
// Copyright(C) 1993-2008 Raven Software
// Copyright(C) 2005-2014 Simon Howard
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//

// Adapted 2026-09-22: P_FaceMobj/P_SeekerMissile from pinned Heretic p_mobj.c.
// Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
namespace ManagedDoom
{
    // Heretic-only steering. The Hellstaff action uses the original ANG1_X
    // constant (0x01000000), which is deliberately not an exact degree.
    internal static class HereticSeeker
    {
        internal static bool SeekHellstaff(Mobj missile, ref Mobj target)
            => Seek(missile, ref target,
                new Fixed(HereticDefinitions.Actors[(int)HereticActorType.MT_HORNRODFX2].Speed),
                10u * 0x01000000u, 30u * 0x01000000u);

        internal static bool Seek(Mobj missile, ref Mobj target, Fixed speed, uint threshold, uint maximumTurn)
        {
            if (target == null) return false;
            if ((target.Flags & MobjFlags.Shootable) == 0) { target = null; return false; }
            var current = missile.Angle.Data;
            var desired = Geometry.PointToAngle(missile.X, missile.Y, target.X, target.Y).Data;
            var increasing = desired > current;
            var delta = increasing ? desired - current : current - desired;
            if (delta > 0x80000000u)
            {
                // Preserve the reference's ANG_MAX subtraction, including its
                // single-unit difference from two's-complement wraparound.
                delta = uint.MaxValue - delta;
                increasing = !increasing;
            }
            if (delta > threshold)
            {
                delta >>= 1;
                if (delta > maximumTurn) delta = maximumTurn;
            }
            missile.Angle = new Angle(increasing ? unchecked(current + delta) : unchecked(current - delta));
            missile.MomX = speed * Trig.Cos(missile.Angle);
            missile.MomY = speed * Trig.Sin(missile.Angle);
            if (missile.Z + missile.Height < target.Z || target.Z + target.Height < missile.Z)
            {
                var distance = Geometry.AproxDistance(target.X - missile.X, target.Y - missile.Y).Data / speed.Data;
                if (distance < 1) distance = 1;
                missile.MomZ = (target.Z - missile.Z) / distance;
            }
            return true;
        }
    }
}
