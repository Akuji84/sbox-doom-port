// s&Doom modification: 2026-09-24, normal Gargoyle melee, charge and native collision recovery.
// s&Doom modification: 2026-09-24, Fire Gargoyle combat and crash debris.
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
// Adapted 2026-09-24: pinned p_enemy.c A_ImpMsAttack2, A_ImpDeath, A_ImpXDeath and A_ImpExplode.
using System;
namespace ManagedDoom
{
    public sealed partial class HereticClinkTestEnemy
    {
        private bool gargoyleCrashed, gargoyleExtremeDeath;
        private bool SupportsGargoyle(HereticAction action) => action is HereticAction.A_ImpMeAttack or HereticAction.A_ImpMsAttack or HereticAction.A_ImpMsAttack2 or
            HereticAction.A_ImpDeath or HereticAction.A_ImpXDeath1 or HereticAction.A_ImpXDeath2 or HereticAction.A_ImpExplode;
        internal bool GargoyleCharging => Combatant.Type == HereticActorType.MT_IMP && (Body.Flags & MobjFlags.SkullFly) != 0;
        internal void StopCharge()
        {
            Body.Flags &= ~MobjFlags.SkullFly;
            Body.MomX = Body.MomY = Body.MomZ = Fixed.Zero;
            if (Body.Health > 0) Combatant.Animation.SetState(HereticDefinitions.Actors[(int)Combatant.Type].SeeState);
        }
        internal void GargoyleChargeContact(Mobj target)
        {
            // Native MT_IMP has zero impact damage; melee delivers its damage.
            var damage = (session.World.Random.Next() % 8 + 1) * HereticDefinitions.Actors[(int)Combatant.Type].Damage;
            if (damage > 0)
            {
                if (target == session.Body) session.DamageEnvironment(damage);
                else session.DamageTestEnemy(target, damage, inflictor: Body, source: Body);
            }
            StopCharge();
        }
        private void BeginGargoyleCrash()
        {
            if (gargoyleCrashed) return;
            gargoyleCrashed = true;
            Combatant.Animation.SetState(HereticStateId.S_IMP_CRASH1);
        }
        private void CheckGargoyleLanding()
        {
            if ((Combatant.Type is HereticActorType.MT_IMP or HereticActorType.MT_IMPLEADER) && Body.Health <= 0 && Body.Z <= Body.FloorZ &&
                (Body.Flags & MobjFlags.NoGravity) == 0) BeginGargoyleCrash();
        }
        private void ExecuteGargoyle(HereticAction action, HereticActorState state)
        {
            switch (action)
            {
                case HereticAction.A_ImpMsAttack:
                    if (Body.Target == null || session.World.Random.Next() > 64)
                    { state.SetState(HereticDefinitions.Actors[(int)Combatant.Type].SeeState); return; }
                    Body.Flags |= MobjFlags.SkullFly;
                    Sound(HereticDefinitions.Actors[(int)Combatant.Type].AttackSound);
                    Face();
                    Body.MomX = 12 * Trig.Cos(Body.Angle); Body.MomY = 12 * Trig.Sin(Body.Angle);
                    var distance = Math.Max(1, Geometry.AproxDistance(Body.Target.X - Body.X, Body.Target.Y - Body.Y).ToIntFloor() / 12);
                    Body.MomZ = (Body.Target.Z + Body.Target.Height / 2 - Body.Z) / distance;
                    break;
                case HereticAction.A_ImpMeAttack:
                    if (Body.Target == null) return;
                    Sound(HereticDefinitions.Actors[(int)Combatant.Type].AttackSound);
                    if (MeleeRange) session.DamageEnvironment(5 + (session.World.Random.Next() & 7));
                    break;
                case HereticAction.A_ImpMsAttack2:
                    if (Body.Target == null) return;
                    Sound(HereticDefinitions.Actors[(int)Combatant.Type].AttackSound);
                    if (MeleeRange) session.DamageEnvironment(5 + (session.World.Random.Next() & 7));
                    else session.SpawnMonsterMissile(this, HereticActorType.MT_IMPBALL);
                    break;
                case HereticAction.A_ImpDeath:
                    Body.Flags &= ~MobjFlags.Solid; Combatant.EnableFootClipping();
                    if (Body.Z <= Body.FloorZ) BeginGargoyleCrash();
                    break;
                case HereticAction.A_ImpXDeath1:
                    Body.Flags &= ~MobjFlags.Solid; Body.Flags |= MobjFlags.NoGravity;
                    Combatant.EnableFootClipping(); gargoyleExtremeDeath = true;
                    break;
                case HereticAction.A_ImpXDeath2:
                    Body.Flags &= ~MobjFlags.NoGravity;
                    if (Body.Z <= Body.FloorZ) BeginGargoyleCrash();
                    break;
                case HereticAction.A_ImpExplode:
                    session.SpawnGargoyleChunks(Body);
                    if (gargoyleExtremeDeath) state.SetState(HereticStateId.S_IMP_XCRASH1);
                    break;
            }
        }
    }
    public sealed partial class HereticWorldSession
    {
        internal void SpawnGargoyleChunks(Mobj source)
        {
            foreach (var type in new[] { HereticActorType.MT_IMPCHUNK1, HereticActorType.MT_IMPCHUNK2 })
            {
                var effect = SpawnLiquidEffect(source, type); var body = effect.Body;
                body.Z = source.Z; body.Flags = MobjFlags.NoBlockMap;
                body.MomX = new Fixed((world.Random.Next() - world.Random.Next()) << 10);
                body.MomY = new Fixed((world.Random.Next() - world.Random.Next()) << 10);
                body.MomZ = Fixed.FromInt(9); body.UpdateFrameInterpolationInfo();
            }
        }
        private void MoveGargoyleChunk(Mobj body)
        {
            MovePhoenixTrail(body);
            body.FloorZ = body.Subsector.Sector.FloorHeight; body.CeilingZ = body.Subsector.Sector.CeilingHeight;
            body.Z += body.MomZ;
            if (body.Z <= body.FloorZ)
            {
                body.Z = body.FloorZ; body.MomZ = Fixed.Zero;
                body.MomX *= new Fixed(0xe800); body.MomY *= new Fixed(0xe800);
            }
            else body.MomZ = body.MomZ == Fixed.Zero ? -Fixed.FromInt(2) : body.MomZ - Fixed.One;
        }
    }
}
