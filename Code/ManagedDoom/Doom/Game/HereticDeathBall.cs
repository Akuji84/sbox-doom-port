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

// Adapted 2026-09-22: death-ball seeking and impacts from pinned p_pspr.c.
// Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
namespace ManagedDoom
{
    public sealed partial class HereticProjectile
    {
        public Mobj SeekerTarget { get; internal set; }
        private void DeathBallImpact()
        {
            if (Body.Z <= Body.FloorZ && session.HitLiquidFloor(Body) != HereticFloorType.Solid)
            { Flying = false; Animation.SetState(HereticStateId.S_NULL); return; }
            if (Body.Z <= Body.FloorZ && Body.MomZ != Fixed.Zero)
            {
                Mobj target = null;
                if (SeekerTarget != null)
                {
                    if ((SeekerTarget.Flags & MobjFlags.Shootable) == 0) SeekerTarget = null;
                    else target = SeekerTarget;
                }
                else
                {
                    var aim = new Hitscan(session.World);
                    for (var i = 0; i < 16; i++)
                    {
                        aim.AimLineAttack(Body, new Angle((uint)i * 0x10000000u), Fixed.FromInt(640));
                        if (aim.LineTarget != null && aim.LineTarget != Body.Target) { target = SeekerTarget = aim.LineTarget; break; }
                    }
                }
                if (target != null)
                {
                    Body.Angle = Geometry.PointToAngle(Body.X, Body.Y, target.X, target.Y);
                    var speed = new Fixed(HereticDefinitions.Actors[(int)Type].Speed);
                    Body.MomX = speed * Trig.Cos(Body.Angle); Body.MomY = speed * Trig.Sin(Body.Angle);
                }
                Animation.SetState(HereticDefinitions.Actors[(int)Type].SpawnState);
                session.RequestSound(HereticSoundId.sfx_pstop, Body);
            }
            else
            {
                Flying = false; LowGravity = false; floorBounce = false;
                Body.Flags |= MobjFlags.NoGravity; Body.Flags &= ~MobjFlags.Missile;
                Body.MomX = Body.MomY = Body.MomZ = Fixed.Zero;
                session.RequestSound(HereticSoundId.sfx_phohit, Body);
            }
        }
    }
}
