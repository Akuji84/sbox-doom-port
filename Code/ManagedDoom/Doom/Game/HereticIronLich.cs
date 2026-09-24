// s&Doom modification: 2026-09-24, final-Lich episode floor trigger and massacre.
// s&Doom modification: 2026-09-24, native whirlwind dispatch and distance-based attack selection.
// s&Doom modification: 2026-09-24, Iron Lich ice bursts and growing fire columns.
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
// Adapted 2026-09-24: pinned p_enemy.c A_HeadIceImpact, A_HeadFireGrow and the fire-column branch of A_HeadAttack.
namespace ManagedDoom
{
    public sealed partial class HereticProjectile
    {
        internal int? ContactDamageOverride { get; private set; }
        private bool SupportsIronLich(HereticAction action) =>
            (Type == HereticActorType.MT_WHIRLWIND && action == HereticAction.A_WhirlwindSeek) ||
            (Type == HereticActorType.MT_HEADFX1 && action == HereticAction.A_HeadIceImpact) ||
            (Type == HereticActorType.MT_HEADFX3 && action == HereticAction.A_HeadFireGrow);
        internal void BeginFireGrowth(int steps)
        {
            Body.Health = steps;
            ContactDamageOverride = 0;
        }
        private void ExecuteIronLich(HereticAction action)
        {
            if (action == HereticAction.A_WhirlwindSeek) { SeekWhirlwind(); return; }
            if (action == HereticAction.A_HeadIceImpact) session.SpawnIronLichShards(this);
            else
            {
                Body.Health--;
                Body.Z += Fixed.FromInt(9);
                if (Body.Health == 0)
                {
                    ContactDamageOverride = null;
                    Animation.SetState(HereticStateId.S_HEADFX3_4);
                }
            }
        }
    }
    public sealed partial class HereticWorldSession
    {
        internal void AttackIronLich(HereticClinkTestEnemy enemy)
        {
            if (enemy.Body.Target == null) return;
            var body = enemy.Body;
            body.Angle = Geometry.PointToAngle(body.X, body.Y, Body.X, Body.Y);
            var distance = Geometry.AproxDistance(body.X - Body.X, body.Y - Body.Y);
            if (distance < Fixed.FromInt(64) && body.Z <= Body.Z + Body.Height && Body.Z <= body.Z + body.Height &&
                new VisibilityCheck(world).CheckSight(body, Body))
            { DamageEnvironment((world.Random.Next() % 8 + 1) * 6); return; }
            var far = distance > Fixed.FromInt(512);
            var roll = world.Random.Next();
            if (roll < (far ? 150 : 50)) SpawnIronLichIce(enemy);
            else if (roll < (far ? 200 : 150)) SpawnIronLichFire(enemy);
            else SpawnIronLichWhirlwind(enemy);
        }
        internal bool IronLichBossTriggered { get; private set; }
        internal void IronLichBossDeath(HereticClinkTestEnemy source)
        {
            if (source.Combatant.Type != HereticActorType.MT_HEAD || source.Body.Health > 0 || IronLichBossTriggered ||
                world.Options.Map != 8 || (world.Options.Episode != 1 && world.Options.Episode != 4) || blockedIronLiches != 0) return;
            foreach (var enemy in testEnemies)
                if (enemy.Combatant.Type == HereticActorType.MT_HEAD && enemy.Body.Health > 0) return;
            IronLichBossTriggered = true;
            if (world.Options.Episode > 1)
                foreach (var enemy in testEnemies)
                    if (enemy.Body.Health > 0) DamageTestEnemy(enemy.Body,10000,environment:true);
            var template = world.Map.Lines[0];
            var trigger = new LineDef(template.Vertex1,template.Vertex2,0,0,666,template.FrontSide,template.BackSide);
            world.SectorAction.DoFloor(trigger,FloorMoveType.LowerFloor);
        }
        internal HereticProjectile SpawnIronLichIce(HereticClinkTestEnemy enemy)
        {
            var ice = SpawnMonsterMissile(enemy, HereticActorType.MT_HEADFX1);
            RequestSound(HereticSoundId.sfx_hedat2, enemy.Body);
            return ice;
        }
        internal HereticProjectile SpawnIronLichFire(HereticClinkTestEnemy enemy)
        {
            var baseFire = SpawnMonsterMissile(enemy, HereticActorType.MT_HEADFX3);
            if (baseFire == null || !baseFire.Flying) return baseFire;
            baseFire.SetFlightState(HereticStateId.S_HEADFX3_4);
            for (var i = 0; i < 5; i++)
            {
                var fire = CreateIronLichChild(baseFire, HereticActorType.MT_HEADFX3, baseFire.Body.Angle);
                if (i == 0) RequestSound(HereticSoundId.sfx_hedat1, enemy.Body);
                fire.Body.MomX = baseFire.Body.MomX; fire.Body.MomY = baseFire.Body.MomY; fire.Body.MomZ = baseFire.Body.MomZ;
                fire.BeginFireGrowth((i + 1) * 2);
                FinishIronLichChild(fire);
            }
            return baseFire;
        }
        internal void SpawnIronLichShards(HereticProjectile ice)
        {
            for (var i = 0; i < 8; i++)
            {
                var shard = CreateIronLichChild(ice, HereticActorType.MT_HEADFX2, Angle.FromDegree(i * 45));
                shard.Body.MomZ = new Fixed(-39321); // Native truncation of -0.6 * FRACUNIT.
                FinishIronLichChild(shard);
            }
        }
        private HereticProjectile CreateIronLichChild(HereticProjectile parent, HereticActorType type, Angle angle)
        {
            var child = new HereticProjectile(this, type, angle, Fixed.Zero, parent.Body.Target);
            child.MonsterOwnerType = parent.MonsterOwnerType;
            world.ThingMovement.UnsetThingPosition(child.Body);
            child.Body.X = parent.Body.X; child.Body.Y = parent.Body.Y; child.Body.Z = parent.Body.Z;
            world.ThingMovement.SetThingPosition(child.Body);
            child.Body.FloorZ = child.Body.Subsector.Sector.FloorHeight;
            child.Body.CeilingZ = child.Body.Subsector.Sector.CeilingHeight;
            projectiles.Add(child);
            return child;
        }
        private void FinishIronLichChild(HereticProjectile child)
        {
            child.Animation.ShortenPositiveTics(world.Random.Next() & 3);
            child.Advance(true);
            child.Body.UpdateFrameInterpolationInfo();
        }
    }
}
