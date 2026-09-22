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

// Adapted 2026-09-22: powered Dragon Claw movement/smoke from pinned p_mobj.c.
// Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
namespace ManagedDoom
{
    public sealed partial class HereticProjectile
    {
        private void AdvanceBlaster(bool spawn)
        {
            var dx = Body.MomX >> 3; var dy = Body.MomY >> 3; var dz = Body.MomZ >> 3;
            for (var i = 0; i < (spawn ? 1 : 8); i++)
            {
                // The spawn check advances Z before checking XY; the thinker reverses that order.
                if (spawn) Body.Z += dz;
                if (!session.World.ThingMovement.TryMove(Body, Body.X + dx, Body.Y + dy)) { Explode(false); return; }
                if (!spawn) Body.Z += dz;
                if (Body.Z <= Body.FloorZ)
                {
                    Body.Z = Body.FloorZ; session.HitLiquidFloor(Body); Explode(false); return;
                }
                if (Body.Z + Body.Height > Body.CeilingZ)
                {
                    Body.Z = Body.CeilingZ - Body.Height; Explode(false); return;
                }
                if (!spawn && (dx != Fixed.Zero || dy != Fixed.Zero) && session.World.Random.Next() < 64)
                    session.SpawnBlasterSmoke(Body);
            }
        }
    }
    public sealed partial class HereticWorldSession
    {
        internal void SpawnBlasterRippers(Mobj origin)
        {
            for (var i = 0; i < 8; i++)
            {
                var bolt = new HereticProjectile(this, HereticActorType.MT_RIPPER, new Angle((uint)i * 0x20000000u), Fixed.Zero);
                world.ThingMovement.UnsetThingPosition(bolt.Body);
                bolt.Body.X = origin.X; bolt.Body.Y = origin.Y; bolt.Body.Z = origin.Z; bolt.Body.Target = origin.Target;
                world.ThingMovement.SetThingPosition(bolt.Body);
                bolt.Body.FloorZ = bolt.Body.Subsector.Sector.FloorHeight; bolt.Body.CeilingZ = bolt.Body.Subsector.Sector.CeilingHeight;
                bolt.Body.UpdateFrameInterpolationInfo(); projectiles.Add(bolt); bolt.Advance(true);
            }
        }
        internal void SpawnRipperBlood(Mobj origin)
        {
            var x = origin.X + new Fixed((world.Random.Next() - world.Random.Next()) << 12);
            var y = origin.Y + new Fixed((world.Random.Next() - world.Random.Next()) << 12);
            var z = origin.Z + new Fixed((world.Random.Next() - world.Random.Next()) << 12);
            var look = world.Random.Next() % 4;
            var def = HereticDefinitions.Actors[(int)HereticActorType.MT_BLOOD];
            var animation = new HereticActorState(def.SpawnState);
            animation.ExtendPositiveTics(world.Random.Next() & 3);
            var body = new Mobj(world) { X = x, Y = y, Z = z, Radius = def.Radius, Height = def.Height,
                Flags = MobjFlags.NoBlockMap | MobjFlags.NoGravity, LastLook = look,
                MomX = origin.MomX >> 1, MomY = origin.MomY >> 1,
                Sprite = (Sprite)animation.Definition.Sprite, Frame = animation.Definition.Frame };
            world.ThingMovement.SetThingPosition(body);
            body.FloorZ = body.Subsector.Sector.FloorHeight; body.CeilingZ = body.Subsector.Sector.CeilingHeight;
            body.UpdateFrameInterpolationInfo(); impactEffects.Add(new HereticMapActor(HereticActorType.MT_BLOOD, body, animation));
        }
        internal void SpawnBlasterSmoke(Mobj bolt)
        {
            var def = HereticDefinitions.Actors[(int)HereticActorType.MT_BLASTERSMOKE];
            var animation = new HereticActorState(def.SpawnState);
            var z = bolt.Z - Fixed.FromInt(8); if (z < bolt.FloorZ) z = bolt.FloorZ;
            var body = new Mobj(world) { X = bolt.X, Y = bolt.Y, Z = z,
                Radius = def.Radius, Height = def.Height, Health = def.SpawnHealth,
                Flags = MobjFlags.NoBlockMap | MobjFlags.NoGravity | MobjFlags.Shadow,
                LastLook = world.Random.Next() % 4, Sprite = (Sprite)animation.Definition.Sprite, Frame = animation.Definition.Frame };
            world.ThingMovement.SetThingPosition(body);
            body.FloorZ = body.Subsector.Sector.FloorHeight; body.CeilingZ = body.Subsector.Sector.CeilingHeight;
            body.UpdateFrameInterpolationInfo();
            impactEffects.Add(new HereticMapActor(HereticActorType.MT_BLASTERSMOKE, body, animation));
        }
    }
}
