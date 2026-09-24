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
