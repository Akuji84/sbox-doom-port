// s&Doom modification: 2026-09-22, shared feather particle landing and gravity.
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

// Adapted 2026-09-22: liquid terrain and floor effects from pinned p_spec.c/p_mobj.c.
// Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
namespace ManagedDoom
{
    public enum HereticFloorType { Solid, Water, Lava, Sludge }
    public sealed partial class HereticWorldSession
    {
        // Adapted 2026-09-22: pinned p_spec.c terrain table and p_mobj.c P_HitFloor.
        internal HereticFloorType FloorType(Mobj body) => world.Map.Flats[body.Subsector.Sector.FloorFlat].Name switch
        {
            "FLTWAWA1" or "FLTFLWW1" => HereticFloorType.Water,
            "FLTLAVA1" or "FLATHUH1" => HereticFloorType.Lava,
            "FLTSLUD1" => HereticFloorType.Sludge,
            _ => HereticFloorType.Solid
        };
        internal HereticFloorType HitLiquidFloor(Mobj source)
        {
            if (source.FloorZ != source.Subsector.Sector.FloorHeight) return HereticFloorType.Solid;
            var terrain = FloorType(source);
            if (terrain == HereticFloorType.Solid) return terrain;
            SpawnLiquidEffect(source, terrain == HereticFloorType.Water ? HereticActorType.MT_SPLASHBASE :
                terrain == HereticFloorType.Lava ? HereticActorType.MT_LAVASPLASH : HereticActorType.MT_SLUDGESPLASH);
            var effect = SpawnLiquidEffect(source, terrain == HereticFloorType.Water ? HereticActorType.MT_SPLASH :
                terrain == HereticFloorType.Lava ? HereticActorType.MT_LAVASMOKE : HereticActorType.MT_SLUDGECHUNK);
            if (terrain == HereticFloorType.Lava)
            {
                effect.Body.MomZ = Fixed.One + new Fixed(world.Random.Next() << 7);
                RequestSound(HereticSoundId.sfx_burn, effect.Body);
            }
            else
            {
                effect.Body.Target = source;
                effect.Body.MomX = new Fixed((world.Random.Next() - world.Random.Next()) << 8);
                effect.Body.MomY = new Fixed((world.Random.Next() - world.Random.Next()) << 8);
                effect.Body.MomZ = Fixed.FromInt(terrain == HereticFloorType.Water ? 2 : 1) + new Fixed(world.Random.Next() << 8);
                if (terrain == HereticFloorType.Water) RequestSound(HereticSoundId.sfx_gloop, effect.Body);
            }
            return terrain;
        }
        private HereticMapActor SpawnLiquidEffect(Mobj source, HereticActorType type)
        {
            var def = HereticDefinitions.Actors[(int)type];
            var state = new HereticActorState(def.SpawnState);
            var body = new Mobj(world) { X = source.X, Y = source.Y, Z = source.Subsector.Sector.FloorHeight,
                Radius = def.Radius, Height = def.Height, Health = def.SpawnHealth,
                LastLook = world.Random.Next() % 4, Flags = MobjFlags.NoBlockMap | MobjFlags.NoGravity,
                Sprite = (Sprite)state.Definition.Sprite, Frame = state.Definition.Frame };
            world.ThingMovement.SetThingPosition(body);
            body.FloorZ = body.Subsector.Sector.FloorHeight; body.CeilingZ = body.Subsector.Sector.CeilingHeight;
            body.UpdateFrameInterpolationInfo();
            var effect = new HereticMapActor(type, body, state); impactEffects.Add(effect); return effect;
        }
        private static bool IsLiquidChunk(HereticActorType type) => type == HereticActorType.MT_SPLASH || type == HereticActorType.MT_SLUDGECHUNK || type == HereticActorType.MT_FEATHER;
        private void MoveLiquidChunk(HereticMapActor effect)
        {
            var body = effect.Body;
            var death = HereticDefinitions.Actors[(int)effect.Type].DeathState;
            if (effect.Animation.State == death) return;
            MovePhoenixTrail(body); // Shared cosmetic wall clipping; no gameplay contacts.
            body.FloorZ = body.Subsector.Sector.FloorHeight; body.CeilingZ = body.Subsector.Sector.CeilingHeight;
            body.Z += body.MomZ;
            if (body.Z <= body.FloorZ)
            {
                body.Z = body.FloorZ; body.MomX = body.MomY = body.MomZ = Fixed.Zero;
                effect.Animation.SetState(death);
            }
            else
            {
                if (body.Z + body.Height > body.CeilingZ) { body.Z = body.CeilingZ - body.Height; if (body.MomZ > Fixed.Zero) body.MomZ = Fixed.Zero; }
                body.MomZ = body.MomZ == Fixed.Zero ? -Fixed.One / 4 : body.MomZ - Fixed.One / 8;
            }
        }
    }
}
