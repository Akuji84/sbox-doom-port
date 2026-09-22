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

// Adapted 2026-09-22: artifact teleport and fog from pinned p_user.c/p_telept.c.
// Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
using System.Linq;
namespace ManagedDoom
{
    public sealed partial class HereticWorldSession
    {
        // Adapted 2026-09-22: pinned P_ArtiTele/P_Teleport, single-player only.
        private bool UseChaosTeleport()
        {
            var start = world.Map.Things.FirstOrDefault(t => t.Type == 1);
            return start != null && TryTeleportPlayer(start.X, start.Y, start.Angle);
        }
        internal bool TryTeleportPlayer(Fixed x, Fixed y, Angle angle)
        {
            if (State.Health <= 0) return false;
            var sector = Geometry.PointInSubsector(x, y, world.Map).Sector;
            // Validate headroom before moving or telefragging an occupant.
            if (sector.CeilingHeight - sector.FloorHeight < Body.Height) return false;
            var oldX = Body.X; var oldY = Body.Y; var oldZ = Body.Z;
            var aboveFloor = Body.Z - Body.FloorZ;
            if (!world.ThingMovement.TeleportMove(Body, x, y)) return false;
            Body.Z = State.FlightTics > 0 && aboveFloor > Fixed.Zero ? Fixed.Min(Body.FloorZ + aboveFloor, Body.CeilingZ - Body.Height) : Body.FloorZ;
            if (Body.Z == Body.FloorZ) { State.LookDirection = 0; State.Centering = false; }
            Body.Angle = angle; Body.MomX = Body.MomY = Body.MomZ = Fixed.Zero; Body.ReactionTime = 18;
            SpawnTeleportFog(oldX, oldY, oldZ + Fixed.FromInt(32));
            SpawnTeleportFog(x + Fixed.FromInt(20) * Trig.Cos(angle), y + Fixed.FromInt(20) * Trig.Sin(angle), Body.Z + Fixed.FromInt(32));
            UpdateView(); Body.UpdateFrameInterpolationInfo(); Camera.UpdateFrameInterpolationInfo(); return true;
        }
        private void SpawnTeleportFog(Fixed x, Fixed y, Fixed z)
        {
            var type = HereticActorType.MT_TFOG; var def = HereticDefinitions.Actors[(int)type];
            var animation = new HereticActorState(def.SpawnState);
            var body = new Mobj(world) { X = x, Y = y, Z = z, Radius = def.Radius, Height = def.Height,
                Flags = MobjFlags.NoGravity | MobjFlags.NoBlockMap, LastLook = world.Random.Next() % 4,
                Sprite = (Sprite)animation.Definition.Sprite, Frame = animation.Definition.Frame };
            world.ThingMovement.SetThingPosition(body);
            body.FloorZ = body.Subsector.Sector.FloorHeight; body.CeilingZ = body.Subsector.Sector.CeilingHeight;
            body.UpdateFrameInterpolationInfo(); impactEffects.Add(new HereticMapActor(type, body, animation));
            RequestSound(HereticSoundId.sfx_telept, body);
        }
    }
}
