// s&Doom modification: 2026-09-24, register Iron Lich ice/fire projectile families.
// s&Doom modification: 2026-09-24, Fire Gargoyle missiles.
// s&Doom modification: 2026-09-24, Disciple missile registration.
// s&Doom modification: 2026-09-24, register both Ophidian missile types.
// s&Doom modification: 2026-09-24, Warrior axe trails.
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
// Adapted 2026-09-24: pinned p_enemy.c A_DripBlood, preserving random draw order.
namespace ManagedDoom
{
    public sealed partial class HereticProjectile
    {
        internal static bool IsMonsterMissile(HereticActorType type) => type is HereticActorType.MT_HEADFX1 or HereticActorType.MT_HEADFX2 or HereticActorType.MT_HEADFX3 or HereticActorType.MT_MUMMYFX1 or
            HereticActorType.MT_IMPBALL or HereticActorType.MT_WIZFX1 or HereticActorType.MT_SNAKEPRO_A or HereticActorType.MT_SNAKEPRO_B or HereticActorType.MT_BEASTBALL or HereticActorType.MT_KNIGHTAXE or HereticActorType.MT_REDAXE;
        private bool SupportsKnight(HereticAction action) =>
            (Type == HereticActorType.MT_KNIGHTAXE && action == HereticAction.A_ContMobjSound) ||
            (Type == HereticActorType.MT_REDAXE && action == HereticAction.A_DripBlood);
        private void ExecuteKnight(HereticAction action)
        {
            if (action == HereticAction.A_ContMobjSound) session.RequestSound(HereticSoundId.sfx_kgtatk, Body);
            else session.SpawnAxeBlood(Body);
        }
    }
    public sealed partial class HereticWorldSession
    {
        internal void SpawnAxeBlood(Mobj source)
        {
            var dy = new Fixed((world.Random.Next() - world.Random.Next()) << 11);
            var dx = new Fixed((world.Random.Next() - world.Random.Next()) << 11);
            var effect = SpawnLiquidEffect(source, HereticActorType.MT_BLOOD);
            var body = effect.Body;
            world.ThingMovement.UnsetThingPosition(body);
            body.X = source.X + dx; body.Y = source.Y + dy; body.Z = source.Z;
            body.Flags = MobjFlags.NoBlockMap;
            body.MomX = new Fixed((world.Random.Next() - world.Random.Next()) << 10);
            body.MomY = new Fixed((world.Random.Next() - world.Random.Next()) << 10);
            world.ThingMovement.SetThingPosition(body);
            body.FloorZ = body.Subsector.Sector.FloorHeight; body.CeilingZ = body.Subsector.Sector.CeilingHeight;
            body.UpdateFrameInterpolationInfo();
        }
    }
}
