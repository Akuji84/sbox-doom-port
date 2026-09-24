// s&Doom modification: 2026-09-24, Maulotaur attack decisions, melee, timed charge and slam.
// s&Doom modification: 2026-09-24, Maulotaur spread and floor-fire projectile systems.
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
// Adapted from pinned p_enemy.c A_MinotaurAtk2/A_MntrFloorFire/A_Explode.
namespace ManagedDoom
{
    public sealed partial class HereticClinkTestEnemy
    {
        private int maulotaurChargeTics;
        private bool maulotaurRepeatedFire;
        internal bool MaulotaurCharging => Combatant.Type == HereticActorType.MT_MINOTAUR && (Body.Flags & MobjFlags.SkullFly) != 0;
        private bool SupportsMaulotaurActor(HereticAction action) => action is HereticAction.A_MinotaurAtk1 or HereticAction.A_MinotaurAtk2 or HereticAction.A_MinotaurAtk3 or HereticAction.A_MinotaurDecide or HereticAction.A_MinotaurCharge;
        private void ExecuteMaulotaurActor(HereticAction action,HereticActorState state)
        {
            if (action == HereticAction.A_MinotaurCharge)
            {
                if(maulotaurChargeTics>0){session.SpawnMaulotaurChargePuff(Body);maulotaurChargeTics--;}
                else{Body.Flags &= ~MobjFlags.SkullFly;state.SetState(HereticStateId.S_MNTR_WALK1);}
                return;
            }
            if(Body.Target==null)return;
            var random=session.World.Random;
            if(action==HereticAction.A_MinotaurDecide)
            {
                Sound(HereticSoundId.sfx_minsit);
                var target=Body.Target;var distance=Geometry.AproxDistance(target.X-Body.X,target.Y-Body.Y);
                if(target.Z+target.Height>Body.Z && target.Z+target.Height<Body.Z+Body.Height && distance<Fixed.FromInt(512) && distance>Fixed.FromInt(64) && random.Next()<150)
                {
                    state.SetState(HereticStateId.S_MNTR_ATK4_1,false);Body.Flags|=MobjFlags.SkullFly;Face();
                    Body.MomX=13*Trig.Cos(Body.Angle);Body.MomY=13*Trig.Sin(Body.Angle);maulotaurChargeTics=17;
                }
                else if(target.Z==target.FloorZ && distance<Fixed.FromInt(576) && random.Next()<220)
                {state.SetState(HereticStateId.S_MNTR_ATK3_1);maulotaurRepeatedFire=false;}
                else Face();
                return;
            }
            if(action==HereticAction.A_MinotaurAtk1)Sound(HereticSoundId.sfx_stfpow);
            if(MeleeRange)
            {
                session.DamageEnvironment((random.Next()%8+1)*(action==HereticAction.A_MinotaurAtk1?4:5));
                if(action!=HereticAction.A_MinotaurAtk2)session.Camera.DeltaViewHeight=-Fixed.FromInt(16);
                else Sound(HereticSoundId.sfx_minat2);
            }
            else if(action==HereticAction.A_MinotaurAtk2)session.SpawnMaulotaurSpread(this);
            else if(action==HereticAction.A_MinotaurAtk3)session.SpawnMaulotaurFloorFire(this);
            if(action==HereticAction.A_MinotaurAtk3 && random.Next()<192 && !maulotaurRepeatedFire)
            {state.SetState(HereticStateId.S_MNTR_ATK3_4);maulotaurRepeatedFire=true;}
        }
        internal void MaulotaurChargeContact(Mobj target)
        {
            session.World.Random.Next(); // Ordinary skull impact roll precedes the special slam.
            if((target.Flags&MobjFlags.Shootable)!=0 && target.Health>0)session.MaulotaurSlam(Body,target);
            StopCharge(); // Shared native momentum reset and SeeState recovery.
        }
    }
    public sealed partial class HereticProjectile
    {
        private bool SupportsMaulotaur(HereticAction action) =>
            (Type == HereticActorType.MT_MNTRFX2 && action == HereticAction.A_MntrFloorFire) ||
            ((Type == HereticActorType.MT_MNTRFX2 || Type == HereticActorType.MT_MNTRFX3) && action == HereticAction.A_Explode);
        private void ExecuteMaulotaur(HereticAction action)
        {
            if (action == HereticAction.A_MntrFloorFire) session.SpawnMaulotaurFloorFlame(this);
            else
            {
                session.PhoenixRadiusAttack(Body, Type == HereticActorType.MT_MNTRFX2 ? 24 : 128);
                session.HitLiquidFloor(Body);
            }
        }
    }
    public sealed partial class HereticWorldSession
    {
        internal void SpawnMaulotaurChargePuff(Mobj source)
        {
            var puff=SpawnLiquidEffect(source,HereticActorType.MT_PHOENIXPUFF);
            puff.Body.Z=source.Z;puff.Body.MomZ=Fixed.FromInt(2);puff.Body.UpdateFrameInterpolationInfo();
        }
        internal void MaulotaurSlam(Mobj source,Mobj target)
        {
            var angle=Geometry.PointToAngle(source.X,source.Y,target.X,target.Y);
            var thrust=Fixed.FromInt(16)+new Fixed(world.Random.Next()<<10);
            target.MomX+=thrust*Trig.Cos(angle);target.MomY+=thrust*Trig.Sin(angle);
            var damage=(world.Random.Next()%8+1)*6;
            if(target==Body){DamageEnvironment(damage);target.ReactionTime=14+(world.Random.Next()&7);}
            else DamageTestEnemy(target,damage,environment:true);
        }
        internal void SpawnMaulotaurSpread(HereticClinkTestEnemy enemy)
        {
            if (enemy.Body.Target == null) return;
            RequestSound(HereticSoundId.sfx_minat2, enemy.Body);
            var shot=SpawnMonsterMissile(enemy,HereticActorType.MT_MNTRFX1);
            if (shot == null || !shot.Flying) return;
            RequestSound(HereticSoundId.sfx_minat2,shot.Body);
            var angle=shot.Body.Angle; var momZ=shot.Body.MomZ;
            foreach(var offset in new[]{-0x04000000,0x04000000,-0x02000000,0x02000000})
                SpawnMonsterMissile(enemy,HereticActorType.MT_MNTRFX1,angle+new Angle(unchecked((uint)offset)),momZ);
        }
        internal HereticProjectile SpawnMaulotaurFloorFire(HereticClinkTestEnemy enemy)
        {
            if (enemy.Body.Target == null) return null;
            var shot=SpawnMonsterMissile(enemy,HereticActorType.MT_MNTRFX2);
            if (shot != null && shot.Flying) RequestSound(HereticSoundId.sfx_minat1,shot.Body);
            return shot;
        }
        internal void SpawnMaulotaurFloorFlame(HereticProjectile source)
        {
            var dy=new Fixed((world.Random.Next()-world.Random.Next())<<10);
            var dx=new Fixed((world.Random.Next()-world.Random.Next())<<10);
            source.Body.Z=source.Body.FloorZ;
            var fire=new HereticProjectile(this,HereticActorType.MT_MNTRFX3,Angle.Ang0,Fixed.Zero,source.Body.Target);
            fire.MonsterOwnerType=source.MonsterOwnerType;
            world.ThingMovement.UnsetThingPosition(fire.Body);
            fire.Body.X=source.Body.X+dx;fire.Body.Y=source.Body.Y+dy;
            world.ThingMovement.SetThingPosition(fire.Body);
            fire.Body.Z=fire.Body.FloorZ=fire.Body.Subsector.Sector.FloorHeight;
            fire.Body.CeilingZ=fire.Body.Subsector.Sector.CeilingHeight;
            fire.Body.MomX=new Fixed(1); // Native minimal momentum forces contact checks.
            projectiles.Add(fire);
            fire.Animation.ShortenPositiveTics(world.Random.Next()&3);
            fire.Advance(true);fire.Body.UpdateFrameInterpolationInfo();
        }
    }
}
