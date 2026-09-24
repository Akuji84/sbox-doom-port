// s&Doom modification: 2026-09-24, mounted Sorcerer attack and pain controller.
// s&Doom modification: 2026-09-24, native Disciple summoning and blocked retries.
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
    public sealed partial class HereticClinkTestEnemy
    {
        // Native special1 is shared by pain acceleration and the repeat limiter.
        private int sorcererSpecial1;
        private bool SupportsMountedSorcerer(HereticAction action) => action is
            HereticAction.A_Sor1Pain or HereticAction.A_Sor1Chase or HereticAction.A_Srcr1Attack;
        private void ExecuteMountedSorcerer(HereticAction action, HereticActorState state)
        {
            var def = HereticDefinitions.Actors[(int)HereticActorType.MT_SORCERER1];
            if (action == HereticAction.A_Sor1Pain)
            {
                sorcererSpecial1 = 20;
                Sound(def.PainSound);
                return;
            }
            if (action == HereticAction.A_Sor1Chase)
            {
                if (sorcererSpecial1 != 0) { sorcererSpecial1--; state.ShortenPositiveTics(3); }
                Execute(HereticAction.A_Chase, state);
                return;
            }
            if (Body.Target == null) return;
            if (MeleeRange)
            {
                Sound(def.AttackSound);
                session.DamageEnvironment((session.World.Random.Next() % 8 + 1) * 8);
                return;
            }
            session.SpawnSorcererFire(this, Body.Health <= (def.SpawnHealth / 3) * 2);
            if (Body.Health < def.SpawnHealth / 3)
            {
                if (sorcererSpecial1 != 0) sorcererSpecial1 = 0;
                else { sorcererSpecial1 = 1; state.SetState(HereticStateId.S_SRCR1_ATK4); }
            }
        }
    }
    public sealed partial class HereticProjectile
    {
        private bool SupportsSorcerer(HereticAction action) => (Type == HereticActorType.MT_SOR2FX2 && action == HereticAction.A_GenWizard) || Type == HereticActorType.MT_SOR2FX1 &&
            (action == HereticAction.A_BlueSpark || action == HereticAction.A_Explode);
        private void ExecuteSorcerer(HereticAction action)
        {
            if(action==HereticAction.A_GenWizard)
            {
                if(Flying && session.TrySummonDisciple(Body)) Explode(false);
                return;
            }
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
        internal void SpawnSorcererSummoners(HereticClinkTestEnemy caster)
        {
            if(caster.Body.Target==null)return;
            RequestSound(HereticSoundId.sfx_soratk,Body);
            SpawnMonsterMissile(caster,HereticActorType.MT_SOR2FX2,caster.Body.Angle-Angle.Ang45,Fixed.One/2);
            SpawnMonsterMissile(caster,HereticActorType.MT_SOR2FX2,caster.Body.Angle+Angle.Ang45,Fixed.One/2);
        }
        internal bool TrySummonDisciple(Mobj missile)
        {
            // P_SpawnMobj consumes LastLook even when P_TestMobjLocation rejects it.
            var lastLook=world.Random.Next()%4;
            var height=HereticDefinitions.Actors[(int)HereticActorType.MT_WIZARD].Height;
            var wizard=TrySpawnSupportedEnemy(HereticActorType.MT_WIZARD,missile.X,missile.Y,spawnZ:missile.Z-height/2);
            if(wizard==null)return false;
            wizard.Body.LastLook=lastLook;
            SpawnTeleportFog(missile.X,missile.Y,missile.Z);
            return true;
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
