// s&Doom modification: 2026-09-24, shared launch path for Beast fireballs.
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

// Adapted 2026-09-22: P_FaceMobj/P_SeekerMissile from pinned Heretic p_mobj.c.
// Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
// Adapted 2026-09-24: A_MummyFX1Seek, A_ContMobjSound, P_SpawnMissile and missile/player damage.
using System;
namespace ManagedDoom
{
    public sealed partial class HereticProjectile
    {
        internal HereticActorType? MonsterOwnerType { get; set; }
        private bool SupportsNitrogolem(HereticAction action) => Type == HereticActorType.MT_MUMMYFX1 &&
            (action == HereticAction.A_MummyFX1Seek || action == HereticAction.A_ContMobjSound);
        private void ExecuteNitrogolem(HereticAction action)
        {
            if (action == HereticAction.A_ContMobjSound) session.RequestSound(HereticSoundId.sfx_mumhed, Body);
            else
            {
                var target = SeekerTarget;
                HereticSeeker.Seek(Body, ref target, new Fixed(HereticDefinitions.Actors[(int)Type].Speed),
                    10u * 0x01000000u, 20u * 0x01000000u);
                SeekerTarget = target;
            }
        }
    }
    public sealed partial class HereticWorldSession
    {
        internal bool SameMonsterType(Mobj target, HereticActorType? type)
        {
            if (!type.HasValue) return false;
            foreach (var enemy in testEnemies)
                if (enemy.Body == target) return enemy.Combatant.Type == type.Value;
            return false;
        }
        internal HereticProjectile SpawnNitrogolemMissile(HereticClinkTestEnemy enemy) => SpawnMonsterMissile(enemy, HereticActorType.MT_MUMMYFX1);
        internal HereticProjectile SpawnMonsterMissile(HereticClinkTestEnemy enemy, HereticActorType type)
        {
            if (type != HereticActorType.MT_MUMMYFX1 && type != HereticActorType.MT_BEASTBALL)
                throw new ArgumentException("Unsupported monster projectile.", nameof(type));
            var source = enemy.Body;
            if (source.Health <= 0 || State.Health <= 0) return null;
            var angle = Geometry.PointToAngle(source.X, source.Y, Body.X, Body.Y);
            var missile = new HereticProjectile(this, type, angle, Fixed.Zero, source);
            missile.MonsterOwnerType = enemy.Combatant.Type;
            // Feet clipping applies only while standing on a liquid floor.
            if (source.Z == source.FloorZ && source.FloorZ == source.Subsector.Sector.FloorHeight && FloorType(source) != HereticFloorType.Solid)
                missile.Body.Z -= Fixed.FromInt(10);
            if ((Body.Flags & MobjFlags.Shadow) != 0)
                angle += new Angle(unchecked((uint)((world.Random.Next() - world.Random.Next()) << 21)));
            missile.Body.Angle = angle;
            var speed = new Fixed(HereticDefinitions.Actors[(int)type].Speed);
            missile.Body.MomX = speed * Trig.Cos(angle); missile.Body.MomY = speed * Trig.Sin(angle);
            var distance = Math.Max(1, Geometry.AproxDistance(Body.X - source.X, Body.Y - source.Y).Data / speed.Data);
            missile.Body.MomZ = (Body.Z - source.Z) / distance;
            projectiles.Add(missile);
            missile.Animation.ShortenPositiveTics(world.Random.Next() & 3);
            missile.Advance(true);
            if (missile.Flying && type == HereticActorType.MT_MUMMYFX1) missile.SeekerTarget = Body;
            missile.Body.UpdateFrameInterpolationInfo();
            return missile;
        }
        internal void DamageMonsterMissile(Mobj target, int damage, Mobj missile)
        {
            if (target != Body)
            {
                DamageTestEnemy(target, damage, inflictor: missile, source: missile.Target);
                return;
            }
            if (State.Health <= 0 || (Body.Flags & MobjFlags.Shootable) == 0) return;
            var thrustDamage = skill == GameSkill.Baby ? damage >> 1 : damage;
            var angle = Geometry.PointToAngle(missile.X, missile.Y, Body.X, Body.Y);
            var thrust = new Fixed(unchecked((int)((uint)thrustDamage * (Fixed.FracUnit >> 3) * 150u)) / 100);
            if (thrustDamage < 40 && thrustDamage > State.Health && Body.Z - missile.Z > Fixed.FromInt(64) && (world.Random.Next() & 1) != 0)
            { angle += Angle.Ang180; thrust *= 4; }
            Body.MomX += thrust * Trig.Cos(angle); Body.MomY += thrust * Trig.Sin(angle);
            DamageEnvironment(damage);
        }
    }
}
