// s&Doom modification: 2026-09-22, powered Dragon Claw and radial rippers.
// s&Doom modification: 2026-09-22, native powered Gold Wand attack and effects.
// s&Doom modification: 2026-09-22, powered Crossbow and native bolt sparks.
// s&Doom modification: 2026-09-22, opt-in powered staff attack, thrust and effects.
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

// Adapted 2026-09-22: normal weapon puffs from pinned Heretic p_map.c/p_mobj.c.
// Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
using System.Collections.Generic;
namespace ManagedDoom
{
    public sealed partial class HereticWorldSession
    {
        private readonly List<HereticMapActor> impactEffects = new();
        public IReadOnlyList<HereticMapActor> ImpactEffects => impactEffects.AsReadOnly();

        internal void SpawnWeaponImpact(HereticTraceHit? result, Angle angle, Fixed slope, HereticWeapon weapon, bool poweredStaff = false, bool poweredGauntlets = false, bool poweredGoldWand = false)
        {
            if (result is not HereticTraceHit hit) return;
            var distance = hit.Distance - Fixed.FromInt(hit.Actor == null ? 4 : 10);
            var z = hit.Z - (hit.Distance - distance) * slope;
            if (hit.Line is { } line && line.FrontSector.CeilingFlat == world.Map.SkyFlatNumber &&
                (z > line.FrontSector.CeilingHeight || line.BackSector?.CeilingFlat == world.Map.SkyFlatNumber)) return;
            var blasterActor = weapon == HereticWeapon.wp_blaster && hit.Actor != null;
            var type = weapon == HereticWeapon.wp_gauntlets ? (poweredGauntlets ? HereticActorType.MT_GAUNTLETPUFF2 : HereticActorType.MT_GAUNTLETPUFF1) : weapon == HereticWeapon.wp_blaster ? (blasterActor ? HereticActorType.MT_BLASTERPUFF2 : HereticActorType.MT_BLASTERPUFF1) : weapon == HereticWeapon.wp_staff ? (poweredStaff ? HereticActorType.MT_STAFFPUFF2 : HereticActorType.MT_STAFFPUFF) : poweredGoldWand ? HereticActorType.MT_GOLDWANDPUFF2 : HereticActorType.MT_GOLDWANDPUFF1;
            var def = HereticDefinitions.Actors[(int)type];
            if (!blasterActor) z += new Fixed((world.Random.Next() - world.Random.Next()) << 10);
            var animation = new HereticActorState(def.SpawnState);
            var body = new Mobj(world)
            {
                X = Body.X + distance * Trig.Cos(angle), Y = Body.Y + distance * Trig.Sin(angle), Z = z,
                Radius = def.Radius, Height = def.Height, Health = def.SpawnHealth,
                LastLook = world.Random.Next() % 4,
                Flags = MobjFlags.NoGravity | MobjFlags.NoBlockMap | (weapon == HereticWeapon.wp_gauntlets ? MobjFlags.Shadow : 0),
                Sprite = (Sprite)animation.Definition.Sprite, Frame = animation.Definition.Frame,
                MomZ = weapon == HereticWeapon.wp_staff && !poweredStaff ? Fixed.One : weapon == HereticWeapon.wp_gauntlets ? new Fixed(52428) : Fixed.Zero
            };
            world.ThingMovement.SetThingPosition(body);
            body.FloorZ = body.Subsector.Sector.FloorHeight;
            body.CeilingZ = body.Subsector.Sector.CeilingHeight;
            body.UpdateFrameInterpolationInfo();
            impactEffects.Add(new HereticMapActor(type, body, animation));
            RequestSound(blasterActor ? HereticSoundId.sfx_blshit : def.AttackSound, body);
        }

        // Adapted 2026-09-22: P_BloodSplatter and the hitscan blood chance.
        internal void SpawnWeaponBlood(HereticTraceHit? result, Angle angle, Fixed slope)
        {
            if (result is not HereticTraceHit hit || hit.Actor == null ||
                (hit.Actor.Flags & MobjFlags.NoBlood) != 0 || world.Random.Next() >= 192) return;
            var def = HereticDefinitions.Actors[(int)HereticActorType.MT_BLOODSPLATTER];
            var animation = new HereticActorState(def.SpawnState);
            var distance = hit.Distance - Fixed.FromInt(10);
            var body = new Mobj(world)
            {
                X = Body.X + distance * Trig.Cos(angle), Y = Body.Y + distance * Trig.Sin(angle),
                Z = hit.Z - Fixed.FromInt(10) * slope,
                Radius = def.Radius, Height = def.Height, Health = def.SpawnHealth,
                LastLook = world.Random.Next() % 4, Target = hit.Actor,
                Flags = MobjFlags.NoBlockMap,
                Sprite = (Sprite)animation.Definition.Sprite, Frame = animation.Definition.Frame,
                MomX = new Fixed((world.Random.Next() - world.Random.Next()) << 9),
                MomY = new Fixed((world.Random.Next() - world.Random.Next()) << 9), MomZ = Fixed.FromInt(2)
            };
            world.ThingMovement.SetThingPosition(body);
            body.FloorZ = body.Subsector.Sector.FloorHeight;
            body.CeilingZ = body.Subsector.Sector.CeilingHeight;
            body.UpdateFrameInterpolationInfo();
            impactEffects.Add(new HereticMapActor(HereticActorType.MT_BLOODSPLATTER, body, animation));
        }

        private void MoveBlood(HereticMapActor effect)
        {
            var body = effect.Body;
            if (effect.Animation.State == HereticStateId.S_BLOODSPLATTERX) return;
            // Cosmetic center-line wall clipping; no actor damage, pickups or line activation.
            var clear = world.PathTraversal.PathTraverse(body.X, body.Y, body.X + body.MomX, body.Y + body.MomY,
                PathTraverseFlags.AddLines, intercept =>
                {
                    var line = intercept.Line;
                    return line.BackSector != null &&
                        body.Z > line.FrontSector.FloorHeight && body.Z > line.BackSector.FloorHeight &&
                        body.Z + body.Height < line.FrontSector.CeilingHeight && body.Z + body.Height < line.BackSector.CeilingHeight;
                });
            if (clear)
            {
                world.ThingMovement.UnsetThingPosition(body);
                body.X += body.MomX; body.Y += body.MomY;
                world.ThingMovement.SetThingPosition(body);
                body.FloorZ = body.Subsector.Sector.FloorHeight;
                body.CeilingZ = body.Subsector.Sector.CeilingHeight;
                body.Z += body.MomZ;
            }
            if (!clear || body.Z <= body.FloorZ || body.Z + body.Height >= body.CeilingZ)
            {
                if (body.Z < body.FloorZ) body.Z = body.FloorZ;
                if (body.Z + body.Height > body.CeilingZ) body.Z = body.CeilingZ - body.Height;
                body.MomX = body.MomY = body.MomZ = Fixed.Zero;
                effect.Animation.SetState(HereticStateId.S_BLOODSPLATTERX);
            }
            else body.MomZ = body.MomZ == Fixed.Zero ? -Fixed.One / 4 : body.MomZ - Fixed.One / 8;
        }

        private void TickImpactEffects()
        {
            for (var i = impactEffects.Count - 1; i >= 0; i--)
            {
                var effect = impactEffects[i];
                var body = effect.Body;
                body.UpdateFrameInterpolationInfo();
                if (effect.Type == HereticActorType.MT_CRBOWFX4) MoveCrossbowSpark(body);
                else if (IsLiquidChunk(effect.Type)) MoveLiquidChunk(effect);
                else if (effect.Type == HereticActorType.MT_BLOOD) MovePhoenixTrail(body);
                else if (effect.Type == HereticActorType.MT_PHOENIXPUFF) MovePhoenixTrail(body);
                else if (effect.Type == HereticActorType.MT_BLOODSPLATTER) MoveBlood(effect);
                else body.Z += body.MomZ;
                if (!IsLiquidChunk(effect.Type) && effect.Type != HereticActorType.MT_BLOODSPLATTER && body.MomZ != Fixed.Zero)
                {
                    body.FloorZ = body.Subsector.Sector.FloorHeight;
                    body.CeilingZ = body.Subsector.Sector.CeilingHeight;
                    if (body.Z <= body.FloorZ) { body.Z = body.FloorZ; body.MomZ = Fixed.Zero; }
                    if (body.Z + body.Height > body.CeilingZ) { body.Z = body.CeilingZ - body.Height; body.MomZ = Fixed.Zero; }
                }
                effect.Animation.Tick();
                if (effect.Animation.Removed)
                {
                    world.ThingMovement.UnsetThingPosition(body);
                    impactEffects.RemoveAt(i);
                }
                else
                {
                    body.Sprite = (Sprite)effect.Animation.Definition.Sprite;
                    body.Frame = effect.Animation.Definition.Frame;
                }
            }
        }
    }
}
