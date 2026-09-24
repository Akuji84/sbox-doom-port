// s&Doom modification: 2026-09-24, shared Golem and Clink ammo drops.
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

// Adapted 2026-09-22: dropped items from pinned p_enemy.c/p_mobj.c.
// Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
namespace ManagedDoom
{
    public sealed partial class HereticWorldSession
    {
        // Adapted 2026-09-22: pinned p_enemy.c P_DropItem, p_mobj.c movement.
        internal HereticTestDrop SpawnClinkAmmoDrop(Mobj source) => SpawnEnemyAmmoDrop(source, HereticActorType.MT_AMSKRDWIMPY, 20);
        internal HereticTestDrop SpawnEnemyAmmoDrop(Mobj source, HereticActorType type, int amount)
        {
            var def = HereticDefinitions.Actors[(int)type];
            var animation = new HereticActorState(def.SpawnState);
            var body = new Mobj(world) { X = source.X, Y = source.Y, Z = source.Z + source.Height / 2,
                Radius = def.Radius, Height = def.Height, Health = amount,
                Flags = MobjFlags.Special | MobjFlags.Dropped, LastLook = world.Random.Next() % 4,
                Sprite = (Sprite)animation.Definition.Sprite, Frame = animation.Definition.Frame,
                MomX = new Fixed((world.Random.Next() - world.Random.Next()) << 8),
                MomY = new Fixed((world.Random.Next() - world.Random.Next()) << 8),
                MomZ = Fixed.FromInt(5) + new Fixed(world.Random.Next() << 10) };
            world.ThingMovement.SetThingPosition(body);
            body.FloorZ = body.Subsector.Sector.FloorHeight; body.CeilingZ = body.Subsector.Sector.CeilingHeight;
            body.UpdateFrameInterpolationInfo(); actors.Add(new HereticMapActor(type, body, animation));
            return new HereticTestDrop(type, body.Health, body.X, body.Y, body.Z, body.MomX, body.MomY, body.MomZ);
        }
        private void TickDroppedItem(HereticMapActor actor)
        {
            var body = actor.Body;
            if ((body.Flags & MobjFlags.Dropped) == 0) return;
            body.UpdateFrameInterpolationInfo();
            if (body.MomX != Fixed.Zero || body.MomY != Fixed.Zero)
            {
                if (!world.ThingMovement.TryMove(body, body.X + body.MomX, body.Y + body.MomY)) body.MomX = body.MomY = Fixed.Zero;
                if (body.Z <= body.FloorZ)
                {
                    body.MomX *= new Fixed(0xe800); body.MomY *= new Fixed(0xe800);
                    if (Fixed.Abs(body.MomX) < new Fixed(0x1000) && Fixed.Abs(body.MomY) < new Fixed(0x1000)) body.MomX = body.MomY = Fixed.Zero;
                }
            }
            body.FloorZ = body.Subsector.Sector.FloorHeight; body.CeilingZ = body.Subsector.Sector.CeilingHeight;
            var oldZ = body.Z; body.Z += body.MomZ;
            if (body.Z <= body.FloorZ)
            {
                if (oldZ > body.FloorZ) HitLiquidFloor(body);
                body.Z = body.FloorZ; if (body.MomZ < Fixed.Zero) body.MomZ = Fixed.Zero;
            }
            else body.MomZ = body.MomZ == Fixed.Zero ? -Fixed.FromInt(2) : body.MomZ - Fixed.One;
            if (body.Z + body.Height > body.CeilingZ)
            { body.Z = body.CeilingZ - body.Height; if (body.MomZ > Fixed.Zero) body.MomZ = Fixed.Zero; }
        }
    }
}
