// s&Doom modification: 2026-09-24, D'Sparil fireballs, blue bolts and sparks.
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
// Adapted from pinned p_enemy.c A_Srcr1Attack/A_BlueSpark/A_Explode.
namespace ManagedDoom
{
    public sealed partial class HereticProjectile
    {
        private bool SupportsSorcerer(HereticAction action) => Type == HereticActorType.MT_SOR2FX1 &&
            (action == HereticAction.A_BlueSpark || action == HereticAction.A_Explode);
        private void ExecuteSorcerer(HereticAction action)
        {
            if(action==HereticAction.A_BlueSpark)session.SpawnSorcererSparks(Body);
            else
            {
                session.PhoenixRadiusAttack(Body,80+(session.World.Random.Next()&31));
                session.HitLiquidFloor(Body);
            }
        }
    }
    public sealed partial class HereticWorldSession
    {
        // The actor's health/attack-state controller will select single or triple fire.
        internal void SpawnSorcererFire(HereticClinkTestEnemy caster,bool spread)
        {
            if(caster.Body.Target==null)return;
            RequestSound(HereticSoundId.sfx_sbtatk,caster.Body);
            var center=SpawnMonsterMissile(caster,HereticActorType.MT_SRCRFX1);
            if(!spread || center==null || !center.Flying)return;
            var angle=center.Body.Angle;var momZ=center.Body.MomZ;
            SpawnMonsterMissile(caster,HereticActorType.MT_SRCRFX1,angle-new Angle(3u*0x01000000u),momZ);
            SpawnMonsterMissile(caster,HereticActorType.MT_SRCRFX1,angle+new Angle(3u*0x01000000u),momZ);
        }
        internal HereticProjectile SpawnSorcererBlueBolt(HereticClinkTestEnemy caster)
        {
            if(caster.Body.Target==null)return null;
            RequestSound(HereticSoundId.sfx_soratk,Body); // Reference attack sound is local/full volume.
            return SpawnMonsterMissile(caster,HereticActorType.MT_SOR2FX1);
        }
        internal void SpawnSorcererSparks(Mobj source)
        {
            for(var i=0;i<2;i++)
            {
                var spark=SpawnLiquidEffect(source,HereticActorType.MT_SOR2FXSPARK);
                spark.Body.Z=source.Z;
                spark.Body.MomX=new Fixed((world.Random.Next()-world.Random.Next())<<9);
                spark.Body.MomY=new Fixed((world.Random.Next()-world.Random.Next())<<9);
                spark.Body.MomZ=Fixed.One+new Fixed(world.Random.Next()<<8);
                spark.Body.UpdateFrameInterpolationInfo();
            }
        }
    }
}
