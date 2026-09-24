// s&Doom modification: 2026-09-24, isolated Sorcerer phase lifecycle.
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
// Adapted from pinned p_enemy.c A_SorcererRise, A_Srcr2Attack/Decide, P_DSparilTeleport and death actions.
using System;
using System.Linq;
namespace ManagedDoom
{
    public sealed partial class HereticClinkTestEnemy
    {
        private bool sorcererRose;
        private int sorcererDeathLoops;
        private bool SupportsSorcererLifecycle(HereticAction action) => action is
            HereticAction.A_SorcererRise or HereticAction.A_SorZap or HereticAction.A_SorRise or
            HereticAction.A_SorSightSnd or HereticAction.A_Srcr2Attack or HereticAction.A_Srcr2Decide or
            HereticAction.A_Sor2DthInit or HereticAction.A_Sor2DthLoop or HereticAction.A_SorDSph or
            HereticAction.A_SorDExp or HereticAction.A_SorDBon;
        private void ExecuteSorcererLifecycle(HereticAction action, HereticActorState state)
        {
            switch(action)
            {
                case HereticAction.A_SorcererRise:
                    if (!sorcererRose) { session.RaiseSorcerer(this); sorcererRose = true; }
                    return;
                case HereticAction.A_Srcr2Decide: session.DecideSorcererTeleport(this); return;
                case HereticAction.A_Srcr2Attack:
                    if (Body.Target == null) return;
                    if (MeleeRange)
                    {
                        session.RequestSound(HereticSoundId.sfx_soratk,session.Body);
                        session.DamageEnvironment((session.World.Random.Next()%8+1)*20);
                    }
                    else if (session.World.Random.Next() < (Body.Health < 1750 ? 96 : 48)) session.SpawnSorcererSummoners(this);
                    else session.SpawnSorcererBlueBolt(this);
                    return;
                case HereticAction.A_Sor2DthInit:
                    sorcererDeathLoops = 7;
                    session.SorcererMassacre();
                    return;
                case HereticAction.A_Sor2DthLoop:
                    if (--sorcererDeathLoops > 0) state.SetState(HereticStateId.S_SOR2_DIE4);
                    return;
            }
            var sound = action switch
            {
                HereticAction.A_SorZap => HereticSoundId.sfx_sorzap,
                HereticAction.A_SorRise => HereticSoundId.sfx_sorrise,
                HereticAction.A_SorSightSnd => HereticSoundId.sfx_sorsit,
                HereticAction.A_SorDSph => HereticSoundId.sfx_sordsph,
                HereticAction.A_SorDExp => HereticSoundId.sfx_sordexp,
                HereticAction.A_SorDBon => HereticSoundId.sfx_sordbon,
                _ => throw new NotSupportedException(action.ToString())
            };
            session.RequestSound(sound,session.Body); // Native NULL source: full-volume local sound.
        }
    }
    public sealed partial class HereticWorldSession
    {
        internal HereticClinkTestEnemy RaiseSorcerer(HereticClinkTestEnemy mount)
        {
            // Native rise spawns at the corpse, without a location rejection or telefrag.
            var rider = new HereticClinkTestEnemy(this,HereticActorType.MT_SORCERER2);
            mount.Body.Flags &= ~MobjFlags.Solid;
            var body = rider.Body;
            body.X=mount.Body.X;body.Y=mount.Body.Y;body.Z=mount.Body.Z;
            body.Angle=mount.Body.Angle;body.Target=mount.Body.Target;
            body.LastLook=world.Random.Next()%4;
            world.ThingMovement.SetThingPosition(body);
            body.FloorZ=body.Subsector.Sector.FloorHeight;body.CeilingZ=body.Subsector.Sector.CeilingHeight;
            rider.SoundRequested+=RequestSound;
            rider.Combatant.Animation.SetState(HereticStateId.S_SOR2_RISE1);
            body.Sprite=(Sprite)rider.Combatant.Animation.Definition.Sprite;body.Frame=rider.Combatant.Animation.Definition.Frame;
            body.UpdateFrameInterpolationInfo();testEnemies.Add(rider);
            return rider;
        }
        internal void SorcererMassacre()
        {
            foreach(var enemy in testEnemies)
                if(enemy.Body.Health>0) DamageTestEnemy(enemy.Body,10000,environment:true);
        }
        internal void DecideSorcererTeleport(HereticClinkTestEnemy enemy)
        {
            var spots=world.Map.Things.Where(t=>(int)t.Type==56).ToArray();
            if(spots.Length==0)return;
            var chances=new[]{192,120,120,120,64,64,32,16,0};
            if(world.Random.Next()>=chances[Math.Clamp(enemy.Body.Health/(3500/8),0,8)])return;
            var index=world.Random.Next();
            // Bound malformed maps with no sufficiently distant spot instead of looping forever.
            for(var attempt=0;attempt<spots.Length;attempt++)
            {
                var spot=spots[(++index)%spots.Length];
                if(Geometry.AproxDistance(enemy.Body.X-spot.X,enemy.Body.Y-spot.Y)<Fixed.FromInt(128))continue;
                TryTeleportSorcerer(enemy,spot.X,spot.Y,spot.Angle);
                return;
            }
        }
        internal bool TryTeleportSorcerer(HereticClinkTestEnemy enemy,Fixed x,Fixed y,Angle angle)
        {
            var body=enemy.Body;
            var sector=Geometry.PointInSubsector(x,y,world.Map).Sector;
            if(sector.CeilingHeight-sector.FloorHeight<body.Height)return false;
            var oldX=body.X;var oldY=body.Y;var oldZ=body.Z;
            if(!world.ThingMovement.TeleportMove(body,x,y))return false;
            var fade=SpawnLiquidEffect(body,HereticActorType.MT_SOR2TELEFADE);
            world.ThingMovement.UnsetThingPosition(fade.Body);
            fade.Body.X=oldX;fade.Body.Y=oldY;fade.Body.Z=oldZ;
            world.ThingMovement.SetThingPosition(fade.Body);
            fade.Body.FloorZ=fade.Body.Subsector.Sector.FloorHeight;fade.Body.CeilingZ=fade.Body.Subsector.Sector.CeilingHeight;
            fade.Body.UpdateFrameInterpolationInfo();RequestSound(HereticSoundId.sfx_telept,fade.Body);
            enemy.Combatant.Animation.SetState(HereticStateId.S_SOR2_TELE1);
            body.Sprite=(Sprite)enemy.Combatant.Animation.Definition.Sprite;body.Frame=enemy.Combatant.Animation.Definition.Frame;
            RequestSound(HereticSoundId.sfx_telept,body);
            body.Z=body.FloorZ;body.Angle=angle;body.MomX=body.MomY=body.MomZ=Fixed.Zero;
            body.UpdateFrameInterpolationInfo();return true;
        }
    }
}
