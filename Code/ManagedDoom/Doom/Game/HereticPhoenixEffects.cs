// s&Doom modification: 2026-09-24, Sorcerer damage reactions and E3M8 completion.
// s&Doom modification: 2026-09-24, Maulotaur boss rules and map combat.
// s&Doom modification: 2026-09-24, Maulotaur projectile integration.
// s&Doom modification: 2026-09-22, powered Phoenix Rod flame cycle and effects.
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

// Adapted 2026-09-22: Phoenix trails and radius damage from pinned p_pspr.c/p_map.c/p_inter.c.
// Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
using System;
namespace ManagedDoom
{
    public sealed partial class HereticWorldSession
    {
        // Adapted 2026-09-22: A_FirePhoenixPL2 spawn offsets and inherited movement.
        internal HereticProjectile SpawnPhoenixFlame()
        {
            var x = Body.X + new Fixed((world.Random.Next() - world.Random.Next()) << 9);
            var y = Body.Y + new Fixed((world.Random.Next() - world.Random.Next()) << 9);
            var slope = Fixed.FromInt(State.LookDirection) / 173;
            var flame = new HereticProjectile(this, HereticActorType.MT_PHOENIXFX2, Body.Angle, slope + Fixed.One / 10);
            world.ThingMovement.UnsetThingPosition(flame.Body);
            flame.Body.X = x; flame.Body.Y = y; flame.Body.Z = Body.Z + Fixed.FromInt(26) + slope;
            flame.Body.MomX += Body.MomX; flame.Body.MomY += Body.MomY;
            world.ThingMovement.SetThingPosition(flame.Body);
            flame.Body.FloorZ = flame.Body.Subsector.Sector.FloorHeight; flame.Body.CeilingZ = flame.Body.Subsector.Sector.CeilingHeight;
            flame.Body.UpdateFrameInterpolationInfo(); projectiles.Add(flame); flame.Advance(true);
            return flame;
        }
        // Adapted 2026-09-22 from pinned PIT_RadiusAttack/A_Explode.
        // Only the player and registered Clink encounter actors are enabled here.
        internal int PhoenixBlastDamage(Mobj origin, Mobj target, int radiusDamage = 128)
        {
            if ((SameMonsterType(target,HereticActorType.MT_MINOTAUR) || SameMonsterType(target,HereticActorType.MT_SORCERER1) || SameMonsterType(target,HereticActorType.MT_SORCERER2)) || (target.Flags & MobjFlags.Shootable) == 0) return 0;
            var dx = Math.Abs((long)target.X.Data - origin.X.Data);
            var dy = Math.Abs((long)target.Y.Data - origin.Y.Data);
            var distance = Math.Max(0, (Math.Max(dx, dy) - target.Radius.Data) >> Fixed.FracBits);
            if (distance >= radiusDamage || !new VisibilityCheck(world).CheckSight(target, origin)) return 0;
            return radiusDamage - (int)distance;
        }
        internal void PhoenixRadiusAttack(Mobj origin, int radiusDamage = 128)
        {
            var damage = PhoenixBlastDamage(origin, Body, radiusDamage);
            if (damage > 0 && State.Health > 0)
            {
                var forceDamage = skill == GameSkill.Baby ? damage >> 1 : damage;
                var angle = Geometry.PointToAngle(origin.X, origin.Y, Body.X, Body.Y);
                var force = new Fixed(forceDamage * (Fixed.FracUnit >> 3) * 150 / HereticDefinitions.Actors[(int)HereticActorType.MT_PLAYER].Mass);
                if (forceDamage < 40 && forceDamage > State.Health && Body.Z - origin.Z > Fixed.FromInt(64) && (world.Random.Next() & 1) != 0)
                { angle += Angle.Ang180; force *= 4; }
                Body.MomX += force * Trig.Cos(angle); Body.MomY += force * Trig.Sin(angle);
                DamageEnvironment(damage);
            }
            foreach (var enemy in testEnemies)
            {
                damage = PhoenixBlastDamage(origin, enemy.Body, radiusDamage);
                if (damage > 0) DamageTestEnemy(enemy.Body, damage, inflictor: origin, source: origin.Target);
            }
        }
        // A normal player Phoenix missile has no seeker target. Its puff action
        // emits two lateral trails; powered flames and monster homing remain gated.
        internal void SpawnPhoenixTrail(Mobj missile)
        {
            foreach (var angle in new[] { missile.Angle + Angle.Ang90, missile.Angle - Angle.Ang90 })
            {
                var def = HereticDefinitions.Actors[(int)HereticActorType.MT_PHOENIXPUFF];
                var animation = new HereticActorState(def.SpawnState);
                var body = new Mobj(world) {
                    X = missile.X, Y = missile.Y, Z = missile.Z, Radius = def.Radius, Height = def.Height,
                    Health = def.SpawnHealth, LastLook = world.Random.Next() % 4,
                    Flags = MobjFlags.NoBlockMap | MobjFlags.NoGravity,
                    MomX = new Fixed(85196) * Trig.Cos(angle), MomY = new Fixed(85196) * Trig.Sin(angle),
                    Sprite = (Sprite)animation.Definition.Sprite, Frame = animation.Definition.Frame
                };
                world.ThingMovement.SetThingPosition(body);
                body.FloorZ = body.Subsector.Sector.FloorHeight; body.CeilingZ = body.Subsector.Sector.CeilingHeight;
                body.UpdateFrameInterpolationInfo();
                impactEffects.Add(new HereticMapActor(HereticActorType.MT_PHOENIXPUFF, body, animation));
            }
        }
        private void MovePhoenixTrail(Mobj body)
        {
            // Cosmetic trails are clipped against world geometry without activating lines.
            var clear = world.PathTraversal.PathTraverse(body.X, body.Y, body.X + body.MomX, body.Y + body.MomY,
                PathTraverseFlags.AddLines, hit => hit.Line.BackSector != null &&
                    body.Z >= hit.Line.BackSector.FloorHeight && body.Z + body.Height <= hit.Line.BackSector.CeilingHeight);
            if (!clear) { body.MomX = body.MomY = Fixed.Zero; return; }
            world.ThingMovement.UnsetThingPosition(body);
            body.X += body.MomX; body.Y += body.MomY;
            world.ThingMovement.SetThingPosition(body);
        }
    }
}
