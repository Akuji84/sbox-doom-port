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

// Adapted 2026-09-22: powered bolt sparks from pinned p_pspr.c/p_mobj.c.
// Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
namespace ManagedDoom
{
    public sealed partial class HereticWorldSession
    {
        internal void SpawnCrossbowSpark(Mobj bolt)
        {
            if (world.Random.Next() <= 50) return;
            var def = HereticDefinitions.Actors[(int)HereticActorType.MT_CRBOWFX4];
            var animation = new HereticActorState(def.SpawnState);
            var body = new Mobj(world) { X = bolt.X, Y = bolt.Y, Z = bolt.Z,
                Radius = def.Radius, Height = def.Height, Health = def.SpawnHealth,
                Flags = MobjFlags.NoBlockMap, LastLook = world.Random.Next() % 4,
                Sprite = (Sprite)animation.Definition.Sprite, Frame = animation.Definition.Frame };
            body.X += new Fixed((world.Random.Next() - world.Random.Next()) << 10);
            body.Y += new Fixed((world.Random.Next() - world.Random.Next()) << 10);
            world.ThingMovement.SetThingPosition(body);
            body.FloorZ = body.Subsector.Sector.FloorHeight; body.CeilingZ = body.Subsector.Sector.CeilingHeight;
            body.UpdateFrameInterpolationInfo();
            impactEffects.Add(new HereticMapActor(HereticActorType.MT_CRBOWFX4, body, animation));
        }
        private void MoveCrossbowSpark(Mobj body)
        {
            body.Z += body.MomZ;
            body.FloorZ = body.Subsector.Sector.FloorHeight;
            body.CeilingZ = body.Subsector.Sector.CeilingHeight;
            if (body.Z <= body.FloorZ) { body.Z = body.FloorZ; body.MomZ = Fixed.Zero; }
            else body.MomZ = body.MomZ == Fixed.Zero ? -Fixed.One / 4 : body.MomZ - Fixed.One / 8;
            if (body.Z + body.Height > body.CeilingZ) { body.Z = body.CeilingZ - body.Height; if (body.MomZ > Fixed.Zero) body.MomZ = Fixed.Zero; }
        }
    }
}
