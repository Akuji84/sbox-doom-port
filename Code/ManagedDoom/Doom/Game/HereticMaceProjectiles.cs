// s&Doom modification: 2026-09-22, powered Firemace death-ball behavior.
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

// Adapted 2026-09-22: normal crossbow and Hellstaff missiles from pinned p_mobj.c/p_map.c.
// Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
using System;
namespace ManagedDoom
{
    public sealed partial class HereticProjectile
    {
        // Adapted 2026-09-22: pinned p_pspr.c normal mace checks/impacts,
        // and p_mobj.c floor-bounce/low-gravity ordering.
        private bool floorBounce = true;
        private bool bouncedThisTick;
        private bool hasBounced;
        public bool LowGravity { get; private set; }
        public int DropTics { get; internal set; }
        internal static bool IsMaceType(HereticActorType type) => type == HereticActorType.MT_MACEFX1 ||
            type == HereticActorType.MT_MACEFX2 || type == HereticActorType.MT_MACEFX3 || type == HereticActorType.MT_MACEFX4;
        private bool SupportsMace(HereticAction action) => IsMaceType(Type) &&
            (action == HereticAction.A_DeathBallImpact || action == HereticAction.A_MacePL1Check || action == HereticAction.A_MaceBallImpact || action == HereticAction.A_MaceBallImpact2);
        private void ExecuteMace(HereticAction action)
        {
            if (action == HereticAction.A_DeathBallImpact) { DeathBallImpact(); return; }
            if (action == HereticAction.A_MacePL1Check)
            {
                if (DropTics == 0) return;
                DropTics = Math.Max(0, DropTics - 4);
                if (DropTics != 0) return;
                LowGravity = true;
                Body.MomX = Fixed.FromInt(7) * Trig.Cos(Body.Angle);
                Body.MomY = Fixed.FromInt(7) * Trig.Sin(Body.Angle);
                Body.MomZ -= Body.MomZ >> 1;
                return;
            }
            if (Body.Z <= Body.FloorZ && session.HitLiquidFloor(Body) != HereticFloorType.Solid)
            { Flying = false; Animation.SetState(HereticStateId.S_NULL); return; }
            var split = action == HereticAction.A_MaceBallImpact2;
            var bounce = split ? Body.Z == Body.FloorZ && Body.MomZ >= Fixed.FromInt(2) :
                !hasBounced && Body.Z <= Body.FloorZ && Body.MomZ != Fixed.Zero;
            if (bounce)
            {
                Body.MomZ = new Fixed((int)(((long)Body.MomZ.Data * 192) >> 8));
                if (!split) { hasBounced = true; floorBounce = false; }
                Animation.SetState(HereticDefinitions.Actors[(int)Type].SpawnState);
                if (split)
                {
                    foreach (var angle in new[] { Body.Angle + Angle.Ang90, Body.Angle - Angle.Ang90 })
                        session.SpawnMaceFragment(Body, angle);
                }
                else session.RequestSound(HereticSoundId.sfx_bounce, Body);
            }
            else
            {
                Flying = false; LowGravity = false; floorBounce = false;
                Body.MomX = Body.MomY = Body.MomZ = Fixed.Zero; Body.Flags &= ~MobjFlags.Missile;
                if (!split) session.RequestSound(HereticSoundId.sfx_lobhit, Body);
            }
        }
    }
    public sealed partial class HereticWorldSession
    {
        // Shared explicit-velocity spawn for lobbed shots, fragments and regression fixtures.
        internal HereticProjectile SpawnMaceProjectile(HereticActorType type, Fixed x, Fixed y, Fixed z,
            Angle angle, Fixed momX, Fixed momY, Fixed momZ)
        {
            if (!HereticProjectile.IsMaceType(type)) throw new ArgumentOutOfRangeException(nameof(type));
            var bolt = new HereticProjectile(this, type, angle, Fixed.Zero);
            world.ThingMovement.UnsetThingPosition(bolt.Body);
            bolt.Body.X = x; bolt.Body.Y = y; bolt.Body.Z = z;
            world.ThingMovement.SetThingPosition(bolt.Body);
            bolt.Body.FloorZ = bolt.Body.Subsector.Sector.FloorHeight; bolt.Body.CeilingZ = bolt.Body.Subsector.Sector.CeilingHeight;
            bolt.Body.MomX = momX; bolt.Body.MomY = momY; bolt.Body.MomZ = momZ;
            bolt.Body.UpdateFrameInterpolationInfo(); projectiles.Add(bolt); return bolt;
        }
        internal void SpawnMaceFragment(Mobj parent, Angle angle)
        {
            var bolt = SpawnMaceProjectile(HereticActorType.MT_MACEFX3, parent.X, parent.Y, parent.Z, angle,
                (parent.MomX >> 1) + (parent.MomZ - Fixed.One) * Trig.Cos(angle),
                (parent.MomY >> 1) + (parent.MomZ - Fixed.One) * Trig.Sin(angle), parent.MomZ);
            bolt.Body.Target = parent.Target;
            bolt.Advance(true);
        }
    }
}
