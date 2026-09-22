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

        internal void SpawnWeaponImpact(HereticTraceHit? result, Angle angle, Fixed slope, HereticWeapon weapon)
        {
            if (result is not HereticTraceHit hit) return;
            var distance = hit.Distance - Fixed.FromInt(hit.Actor == null ? 4 : 10);
            var z = hit.Z - (hit.Distance - distance) * slope;
            if (hit.Line is { } line && line.FrontSector.CeilingFlat == world.Map.SkyFlatNumber &&
                (z > line.FrontSector.CeilingHeight || line.BackSector?.CeilingFlat == world.Map.SkyFlatNumber)) return;
            var type = weapon == HereticWeapon.wp_staff ? HereticActorType.MT_STAFFPUFF : HereticActorType.MT_GOLDWANDPUFF1;
            var def = HereticDefinitions.Actors[(int)type];
            z += new Fixed((world.Random.Next() - world.Random.Next()) << 10);
            var animation = new HereticActorState(def.SpawnState);
            var body = new Mobj(world)
            {
                X = Body.X + distance * Trig.Cos(angle), Y = Body.Y + distance * Trig.Sin(angle), Z = z,
                Radius = def.Radius, Height = def.Height, Health = def.SpawnHealth,
                LastLook = world.Random.Next() % 4,
                Flags = MobjFlags.NoGravity | MobjFlags.NoBlockMap,
                Sprite = (Sprite)animation.Definition.Sprite, Frame = animation.Definition.Frame,
                MomZ = weapon == HereticWeapon.wp_staff ? Fixed.One : Fixed.Zero
            };
            world.ThingMovement.SetThingPosition(body);
            body.FloorZ = body.Subsector.Sector.FloorHeight;
            body.CeilingZ = body.Subsector.Sector.CeilingHeight;
            body.UpdateFrameInterpolationInfo();
            impactEffects.Add(new HereticMapActor(type, body, animation));
        }

        private void TickImpactEffects()
        {
            for (var i = impactEffects.Count - 1; i >= 0; i--)
            {
                var effect = impactEffects[i];
                var body = effect.Body;
                body.UpdateFrameInterpolationInfo();
                body.Z += body.MomZ;
                if (body.MomZ != Fixed.Zero)
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
