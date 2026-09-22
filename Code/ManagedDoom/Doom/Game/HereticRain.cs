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

// Adapted 2026-09-22: powered Hellstaff/rain actions from pinned p_pspr.c.
// Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
using System;
namespace ManagedDoom
{
    public sealed partial class HereticProjectile
    {
        private int rainDrops;
        internal int RainDrops => rainDrops;
        private bool SupportsRain(HereticAction action) =>
            Type == HereticActorType.MT_HORNRODFX2 &&
                (action == HereticAction.A_SkullRodPL2Seek || action == HereticAction.A_AddPlayerRain ||
                 action == HereticAction.A_HideInCeiling || action == HereticAction.A_SkullRodStorm) ||
            Type == HereticActorType.MT_RAINPLR3 && action == HereticAction.A_RainImpact;
        private void ExecuteRain(HereticAction action)
        {
            switch (action)
            {
                case HereticAction.A_SkullRodPL2Seek:
                    var target = SeekerTarget;
                    HereticSeeker.SeekHellstaff(Body, ref target);
                    SeekerTarget = target;
                    break;
                case HereticAction.A_AddPlayerRain:
                    session.AddPlayerRain(this);
                    break;
                case HereticAction.A_HideInCeiling:
                    Body.Z = Body.CeilingZ + Fixed.FromInt(4);
                    break;
                case HereticAction.A_SkullRodStorm:
                    if (Body.Health-- == 0)
                    {
                        Animation.SetState(HereticStateId.S_NULL);
                        session.RemovePlayerRain(this);
                        return;
                    }
                    if (session.World.Random.Next() < 25) return;
                    var x = Body.X + Fixed.FromInt((session.World.Random.Next() & 127) - 64);
                    var y = Body.Y + Fixed.FromInt((session.World.Random.Next() & 127) - 64);
                    session.SpawnRainDrop(Body, x, y);
                    if ((rainDrops & 31) == 0) session.RequestSound(HereticSoundId.sfx_ramrain, Body);
                    rainDrops++;
                    break;
                case HereticAction.A_RainImpact:
                    if (Body.Z > Body.FloorZ) Animation.SetState(HereticStateId.S_RAINAIRXPLR3_1);
                    else if (session.World.Random.Next() < 40) session.HitLiquidFloor(Body);
                    break;
            }
        }
    }
    public sealed partial class HereticWorldSession
    {
        // Single-player preview uses the reference red rain variant. Network
        // player/color assignment remains owned by the future multiplayer layer.
        private HereticProjectile rain1, rain2;
        internal int TrackedRainCount => (rain1 == null ? 0 : 1) + (rain2 == null ? 0 : 1);
        internal void AddPlayerRain(HereticProjectile rain)
        {
            if (State.Health <= 0) return;
            if (rain1 != null && rain2 != null)
            {
                if (rain1.Body.Health < rain2.Body.Health)
                {
                    rain1.Body.Health = Math.Min(rain1.Body.Health, 16);
                    rain1 = null;
                }
                else
                {
                    rain2.Body.Health = Math.Min(rain2.Body.Health, 16);
                    rain2 = null;
                }
            }
            if (rain1 != null) rain2 = rain; else rain1 = rain;
        }
        internal void RemovePlayerRain(HereticProjectile rain)
        {
            // Clear references even after player death; this changes no flight,
            // animation or random-number ordering.
            if (rain1 == rain) rain1 = null;
            if (rain2 == rain) rain2 = null;
        }
        internal HereticProjectile SpawnRainDrop(Mobj storm, Fixed x, Fixed y)
        {
            var drop = new HereticProjectile(this, HereticActorType.MT_RAINPLR3, Angle.Ang0, Fixed.Zero);
            world.ThingMovement.UnsetThingPosition(drop.Body);
            drop.Body.X = x; drop.Body.Y = y;
            world.ThingMovement.SetThingPosition(drop.Body);
            drop.Body.FloorZ = drop.Body.Subsector.Sector.FloorHeight;
            drop.Body.CeilingZ = drop.Body.Subsector.Sector.CeilingHeight;
            drop.Body.Z = drop.Body.CeilingZ - drop.Body.Height;
            drop.Body.Target = storm.Target;
            drop.Body.MomX = new Fixed(1); drop.Body.MomY = Fixed.Zero;
            drop.Body.MomZ = -new Fixed(HereticDefinitions.Actors[(int)drop.Type].Speed);
            projectiles.Add(drop);
            drop.Advance(true);
            drop.Body.UpdateFrameInterpolationInfo();
            return drop;
        }
    }
}
