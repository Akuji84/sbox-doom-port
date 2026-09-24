// s&Doom modification: 2026-09-24, Undead Warrior combat.
// s&Doom modification: 2026-09-24, Beast attacks and per-family drops.
// s&Doom modification: 2026-09-24, Nitrogolem ranged attacks.
// s&Doom modification: 2026-09-24, Golem/ghost combat and shared supported-enemy spawning.
// s&Doom modification: 2026-09-22, reversible chicken test-enemy lifecycle.
// s&Doom modification: 2026-09-22, opt-in powered staff attack, thrust and effects.
// s&Doom modification: 2026-09-22, normal Gold Wand replaces the encounter test ray.
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

// Adapted 2026-09-22: Clink attack/death actions from pinned p_enemy.c.
// Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
// Pursuit is a limited preview implementation, not the complete vanilla A_Chase.
using System;
using System.Collections.Generic;
namespace ManagedDoom
{
    public readonly record struct HereticTestDrop(HereticActorType Type, int Amount, Fixed X, Fixed Y, Fixed Z, Fixed MomX, Fixed MomY, Fixed MomZ);
    public sealed partial class HereticClinkTestEnemy : IHereticActorActions
    {
        private readonly HereticWorldSession session;
        private readonly VisibilityCheck visibility;
        public HereticCombatant Combatant { get; private set; }
        internal HereticActorType OriginalType { get; }
        public Mobj Body => Combatant.Body;
        public event Action<HereticSoundId, Mobj> SoundRequested;
        public event Action<HereticTestDrop> DropRequested;
        internal HereticClinkTestEnemy(HereticWorldSession session, HereticActorType type = HereticActorType.MT_CLINK)
        {
            this.session = session;
            visibility = new VisibilityCheck(session.World);
            OriginalType = type;
            Combatant = new HereticCombatant(session.World, type, this);
        }
        public bool Supports(HereticAction action) => action is HereticAction.A_KnightAttack or HereticAction.A_BeastAttack or HereticAction.A_MummyAttack2 or HereticAction.A_MummyAttack or HereticAction.A_MummySoul or HereticAction.A_ChicLook or HereticAction.A_ChicChase or HereticAction.A_ChicAttack or HereticAction.A_ChicPain or HereticAction.A_Feathers or HereticAction.A_Look or HereticAction.A_Chase or
            HereticAction.A_FaceTarget or HereticAction.A_ClinkAttack or HereticAction.A_Pain or HereticAction.A_Scream or HereticAction.A_NoBlocking;
        private bool CanSee => session.State.Health > 0 && visibility.CheckSight(Body, session.Body);
        private bool MeleeRange
        {
            get
            {
                var dx = Fixed.Abs(Body.X - session.Body.X); var dy = Fixed.Abs(Body.Y - session.Body.Y);
                var distance = dx + dy - new Fixed(Math.Min(dx.Data, dy.Data)) / 2;
                return distance < Fixed.FromInt(64) && Body.Z <= session.Body.Z + session.Body.Height &&
                    session.Body.Z <= Body.Z + Body.Height && CanSee;
            }
        }
        private bool CheckMissileRange()
        {
            if (!CanSee) return false;
            if ((Body.Flags & MobjFlags.JustHit) != 0) { Body.Flags &= ~MobjFlags.JustHit; return true; }
            if (Body.ReactionTime != 0) return false;
            var distance = Geometry.AproxDistance(Body.X - session.Body.X, Body.Y - session.Body.Y).ToIntFloor() - 64;
            if (HereticDefinitions.Actors[(int)Combatant.Type].MeleeState == HereticStateId.S_NULL) distance -= 128;
            return session.World.Random.Next() >= Math.Min(distance, 200);
        }
        private void Face() => Body.Angle = Geometry.PointToAngle(Body.X, Body.Y, session.Body.X, session.Body.Y);
        private void Sound(HereticSoundId sound) => SoundRequested?.Invoke(sound, Body);
        public void Execute(HereticAction action, HereticActorState state)
        {
            var elapsed = action == HereticAction.A_ChicLook || action == HereticAction.A_ChicPain ? 10 : action == HereticAction.A_ChicChase ? 3 : action == HereticAction.A_ChicAttack ? 18 : 0;
            if (elapsed != 0 && UpdateChicken(elapsed)) return;
            var def = HereticDefinitions.Actors[(int)Combatant.Type];
            switch (action)
            {
                case HereticAction.A_ChicLook:
                case HereticAction.A_Look:
                    if (CanSee) { Body.Target = session.Body; Sound(def.SeeSound); state.SetState(def.SeeState); }
                    break;
                case HereticAction.A_ChicChase:
                case HereticAction.A_Chase:
                    if (session.State.Health <= 0) { Body.Target = null; state.SetState(def.SpawnState); break; }
                    if (Body.ReactionTime > 0) Body.ReactionTime--;
                    if (Body.Threshold > 0) Body.Threshold--;
                    Face();
                    var recovering = def.MissileState != HereticStateId.S_NULL && (Body.Flags & MobjFlags.JustAttacked) != 0;
                    if (recovering) Body.Flags &= ~MobjFlags.JustAttacked;
                    if (!recovering && def.MeleeState != HereticStateId.S_NULL && Body.ReactionTime == 0 && MeleeRange) { state.SetState(def.MeleeState); break; }
                    if (!recovering && def.MissileState != HereticStateId.S_NULL && CheckMissileRange())
                    {
                        state.SetState(def.MissileState); Body.Flags |= MobjFlags.JustAttacked; break;
                    }
                    foreach (var offset in new[] { 0, 45, -45, 90, -90 })
                    {
                        var angle = Body.Angle + Angle.FromDegree(offset);
                        if (session.World.ThingMovement.TryMove(Body, Body.X + def.Speed * Trig.Cos(angle), Body.Y + def.Speed * Trig.Sin(angle))) break;
                    }
                    break;
                case HereticAction.A_FaceTarget: Face(); break;
                case HereticAction.A_ClinkAttack:
                    Sound(def.AttackSound);
                    if (MeleeRange) session.DamageEnvironment(session.World.Random.Next() % 7 + 3);
                    break;
                case HereticAction.A_MummyAttack:
                    if (Body.Target == null) break;
                    Sound(def.AttackSound);
                    if (MeleeRange)
                    {
                        session.DamageEnvironment(((session.World.Random.Next() & 7) + 1) * 2);
                        Sound(HereticSoundId.sfx_mumat2);
                    }
                    else Sound(HereticSoundId.sfx_mumat1);
                    break;
                case HereticAction.A_KnightAttack:
                    if (Body.Target == null) break;
                    if (MeleeRange)
                    {
                        session.DamageEnvironment(((session.World.Random.Next() & 7) + 1) * 3);
                        Sound(HereticSoundId.sfx_kgtat2);
                    }
                    else
                    {
                        Sound(def.AttackSound);
                        var red = Combatant.Type == HereticActorType.MT_KNIGHTGHOST || session.World.Random.Next() < 40;
                        session.SpawnMonsterMissile(this, red ? HereticActorType.MT_REDAXE : HereticActorType.MT_KNIGHTAXE);
                    }
                    break;
                case HereticAction.A_BeastAttack:
                    if (Body.Target == null) break;
                    Sound(def.AttackSound);
                    if (MeleeRange) session.DamageEnvironment(((session.World.Random.Next() & 7) + 1) * 3);
                    else session.SpawnMonsterMissile(this, HereticActorType.MT_BEASTBALL);
                    break;
                case HereticAction.A_MummyAttack2:
                    if (Body.Target == null) break;
                    if (MeleeRange) session.DamageEnvironment(((session.World.Random.Next() & 7) + 1) * 2);
                    else session.SpawnNitrogolemMissile(this);
                    break;
                case HereticAction.A_MummySoul: session.SpawnGolemSoul(Body); break;
                case HereticAction.A_ChicAttack:
                    if (Body.Target != null && MeleeRange) session.DamageEnvironment(1 + (session.World.Random.Next() & 1));
                    break;
                case HereticAction.A_Feathers: session.SpawnChickenFeathers(Body); break;
                case HereticAction.A_ChicPain:
                case HereticAction.A_Pain: Sound(def.PainSound); break;
                case HereticAction.A_Scream: Sound(def.DeathSound); break;
                case HereticAction.A_NoBlocking:
                    Body.Flags &= ~MobjFlags.Solid;
                    if (IsChicken) break;
                    var random = session.World.Random;
                    if (random.Next() <= 84)
                    {
                        var drop = OriginalType switch
                        {
                            HereticActorType.MT_CLINK => session.SpawnClinkAmmoDrop(Body),
                            HereticActorType.MT_KNIGHT or HereticActorType.MT_KNIGHTGHOST => session.SpawnEnemyAmmoDrop(Body, HereticActorType.MT_AMCBOWWIMPY, 5),
                            HereticActorType.MT_BEAST => session.SpawnEnemyAmmoDrop(Body, HereticActorType.MT_AMCBOWWIMPY, 10),
                            _ => session.SpawnEnemyAmmoDrop(Body, HereticActorType.MT_AMGWNDWIMPY, 3)
                        };
                        DropRequested?.Invoke(drop);
                    }
                    break;
                default: throw new NotSupportedException("Clink test action: " + action);
            }
        }
        internal void Tick()
        {
            Body.UpdateFrameInterpolationInfo();
            if (Body.MomX != Fixed.Zero || Body.MomY != Fixed.Zero)
            {
                var dx = new Fixed(Math.Clamp(Body.MomX.Data, -15 * Fixed.FracUnit, 15 * Fixed.FracUnit));
                var dy = new Fixed(Math.Clamp(Body.MomY.Data, -15 * Fixed.FracUnit, 15 * Fixed.FracUnit));
                if (!session.World.ThingMovement.TryMove(Body, Body.X + dx, Body.Y + dy)) Body.MomX = Body.MomY = Fixed.Zero;
                Body.MomX *= new Fixed(0xe800); Body.MomY *= new Fixed(0xe800);
            }
            Body.Z += Body.MomZ;
            if (Body.Z <= Body.FloorZ) { Body.Z = Body.FloorZ; Body.MomZ = Fixed.Zero; }
            else Body.MomZ -= Fixed.One;
            if (Body.Z + Body.Height > Body.CeilingZ) { Body.Z = Body.CeilingZ - Body.Height; Body.MomZ = Fixed.Zero; }
            Combatant.TickState();
            if (Body.Z < Body.FloorZ) Body.Z = Body.FloorZ;
        }
    }
    public sealed partial class HereticWorldSession
    {
        private readonly List<HereticClinkTestEnemy> testEnemies = new();
        public int TestEnemyCount => testEnemies.Count;
        public int TestKills { get; private set; }
        public HereticGoldWand GoldWand { get; private set; }
        public HereticClinkTestEnemy TrySpawnClinkTest(Fixed x, Fixed y) => TrySpawnSupportedEnemy(HereticActorType.MT_CLINK, x, y, true);
        internal HereticClinkTestEnemy TrySpawnSupportedEnemy(HereticActorType type, Fixed x, Fixed y, bool requireSight = false)
        {
            if (!SupportsMapEnemy(type)) throw new ArgumentException("Unsupported map enemy: " + type);
            var enemy = new HereticClinkTestEnemy(this, type);
            var body = enemy.Body; body.X = x; body.Y = y;
            body.Subsector = Geometry.PointInSubsector(x, y, world.Map);
            var sector = body.Subsector.Sector;
            body.Z = sector.FloorHeight;
            if (!world.ThingMovement.CheckPosition(body, x, y) || world.ThingMovement.CurrentCeilingZ - world.ThingMovement.CurrentFloorZ < body.Height) return null;
            if (requireSight && !new VisibilityCheck(world).CheckSight(body, Body)) return null;
            world.ThingMovement.SetThingPosition(body);
            body.Z = body.FloorZ = world.ThingMovement.CurrentFloorZ; body.CeilingZ = world.ThingMovement.CurrentCeilingZ;
            enemy.SoundRequested += RequestSound;
            body.UpdateFrameInterpolationInfo(); testEnemies.Add(enemy); return enemy;
        }
        public HereticClinkTestEnemy StartClinkTest() => StartEnemyTest(HereticActorType.MT_CLINK);
        public HereticClinkTestEnemy StartEnemyTest(HereticActorType type)
        {
            foreach (var distance in new[] { 96, 160, 64 })
            for (var offset = 0; offset < 360; offset += 45)
            {
                var angle = Body.Angle + Angle.FromDegree(offset);
                var enemy = TrySpawnSupportedEnemy(type, Body.X + distance * Trig.Cos(angle), Body.Y + distance * Trig.Sin(angle), true);
                if (enemy != null) { GoldWand ??= new HereticGoldWand(this); EnableCombatAmmo(); return enemy; }
            }
            return null;
        }
        public HereticDamageResult DamageTestEnemy(Mobj body, int damage, bool environment = false, Mobj inflictor = null, HereticDamageThrust thrust = HereticDamageThrust.Normal, Mobj source = null)
        {
            foreach (var enemy in testEnemies)
                if (enemy.Body == body)
                {
                    var result = enemy.Combatant.ApplyOrdinaryDamage(damage, environment ? null : inflictor ?? Body, environment ? null : source ?? Body, thrust);
                    if (result == HereticDamageResult.Killed) TestKills++;
                    return result;
                }
            return HereticDamageResult.Ignored;
        }
        internal bool IsTestEnemy(Mobj body)
        {
            foreach (var enemy in testEnemies) if (enemy.Body == body) return true;
            return false;
        }
        public event Action<HereticSoundId, Mobj> SoundRequested;
        internal void RequestSound(HereticSoundId sound, Mobj source)
        {
            if ((int)sound != 0) SoundRequested?.Invoke(sound, source);
        }
        private void TickClinkTest(bool shoot)
        {
            TickImpactEffects();
            TickProjectiles();
            GoldWand?.Tick(shoot);
            foreach (var enemy in testEnemies) enemy.Tick();
        }
    }
}
