// s&Doom modification: 2026-09-24, Beast fireball trails.
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

// Reference: pinned Heretic p_enemy.c/p_mobj.c.
// Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
// Adapted 2026-09-24: pinned p_enemy.c A_BeastPuff, preserving random draw order.
namespace ManagedDoom
{
    public sealed partial class HereticWorldSession
    {
        internal void SpawnBeastPuff(Mobj source)
        {
            if (world.Random.Next() <= 64) return;
            var dz = new Fixed((world.Random.Next() - world.Random.Next()) << 10);
            var dy = new Fixed((world.Random.Next() - world.Random.Next()) << 10);
            var dx = new Fixed((world.Random.Next() - world.Random.Next()) << 10);
            var effect = SpawnLiquidEffect(source, HereticActorType.MT_PUFFY);
            world.ThingMovement.UnsetThingPosition(effect.Body);
            effect.Body.X = source.X + dx; effect.Body.Y = source.Y + dy; effect.Body.Z = source.Z + dz;
            world.ThingMovement.SetThingPosition(effect.Body);
            effect.Body.FloorZ = effect.Body.Subsector.Sector.FloorHeight;
            effect.Body.CeilingZ = effect.Body.Subsector.Sector.CeilingHeight;
            effect.Body.UpdateFrameInterpolationInfo();
        }
    }
}
