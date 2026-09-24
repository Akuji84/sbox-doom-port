// s&Doom modification: 2026-09-24, owned Nitrogolem missiles and player collision.
// s&Doom modification: 2026-09-22, gated Morph Ovum projectile foundation.
// s&Doom modification: 2026-09-22, powered Hellstaff and rain lifecycle.
// s&Doom modification: 2026-09-22, powered Firemace death-ball behavior.
// s&Doom modification: 2026-09-22, powered Phoenix Rod flame cycle and effects.
// s&Doom modification: 2026-09-22, powered Dragon Claw and radial rippers.
// s&Doom modification: 2026-09-22, native powered Gold Wand attack and effects.
// s&Doom modification: 2026-09-22, powered Crossbow and native bolt sparks.
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

// Adapted 2026-09-22: normal crossbow and Hellstaff missiles from pinned p_mobj.c/p_map.c.
// Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
using System;
using System.Collections.Generic;
namespace ManagedDoom
{
    public sealed partial class HereticProjectile : IHereticActorActions
    {
        private readonly HereticWorldSession session;
        public Mobj Body { get; }
        public HereticActorType Type { get; }
        public HereticActorState Animation { get; }
        public bool Flying { get; private set; } = true;
        internal HereticProjectile(HereticWorldSession session, HereticActorType type, Angle angle, Fixed slope, Mobj owner = null)
        {
            if (type != HereticActorType.MT_MUMMYFX1 && type != HereticActorType.MT_EGGFX && type != HereticActorType.MT_HORNRODFX2 && type != HereticActorType.MT_RAINPLR3 && type != HereticActorType.MT_PHOENIXFX2 && type != HereticActorType.MT_RIPPER && type != HereticActorType.MT_BLASTERFX1 && type != HereticActorType.MT_GOLDWANDFX2 && type != HereticActorType.MT_CRBOWFX2 && type != HereticActorType.MT_CRBOWFX1 && type != HereticActorType.MT_CRBOWFX3 && type != HereticActorType.MT_HORNRODFX1 && type != HereticActorType.MT_PHOENIXFX1 && !IsMaceType(type))
                throw new NotSupportedException("Projectile family is not enabled.");
            this.session = session; Type = type;
            owner ??= session.Body;
            var def = HereticDefinitions.Actors[(int)type];
            Animation = new HereticActorState(def.SpawnState, this);
            Body = new Mobj(session.World) { X = owner.X, Y = owner.Y,
                Z = owner.Z + Fixed.FromInt(32) + (owner != session.Body || type == HereticActorType.MT_GOLDWANDFX2 ? Fixed.Zero : Fixed.FromInt(session.State.LookDirection) / 173),
                Radius = def.Radius, Height = def.Height, Health = def.SpawnHealth,
                Flags = MobjFlags.Missile | MobjFlags.NoGravity | MobjFlags.NoBlockMap | MobjFlags.DropOff,
                Target = owner, Angle = angle, LastLook = session.World.Random.Next() % 4,
                MomX = new Fixed(def.Speed) * Trig.Cos(angle), MomY = new Fixed(def.Speed) * Trig.Sin(angle),
                MomZ = new Fixed(def.Speed) * slope };
            LowGravity = type == HereticActorType.MT_MACEFX4 || type == HereticActorType.MT_MACEFX2 || type == HereticActorType.MT_MACEFX3;
            Sync();
            session.World.ThingMovement.SetThingPosition(Body);
            Body.FloorZ = Body.Subsector.Sector.FloorHeight; Body.CeilingZ = Body.Subsector.Sector.CeilingHeight;
            Body.UpdateFrameInterpolationInfo();
        }
        public bool Supports(HereticAction action) => SupportsNitrogolem(action) || SupportsRain(action) || (Type == HereticActorType.MT_PHOENIXFX2 && (action == HereticAction.A_FlameEnd || action == HereticAction.A_FloatPuff)) || (Type == HereticActorType.MT_BLASTERFX1 && action == HereticAction.A_SpawnRippers) || (Type == HereticActorType.MT_CRBOWFX2 && action == HereticAction.A_BoltSpark) || SupportsMace(action) || Type == HereticActorType.MT_PHOENIXFX1 &&
            (action == HereticAction.A_PhoenixPuff || action == HereticAction.A_Explode);
        public void Execute(HereticAction action, HereticActorState actor)
        {
            if (!Supports(action)) throw new NotSupportedException("Projectile action: " + action);
            if (SupportsNitrogolem(action)) { ExecuteNitrogolem(action); return; }
            if (SupportsRain(action)) { ExecuteRain(action); return; }
            if (action == HereticAction.A_FlameEnd) { Body.MomZ += new Fixed(98304); return; }
            if (action == HereticAction.A_FloatPuff) { Body.MomZ += new Fixed(117964); return; }
            if (action == HereticAction.A_SpawnRippers) { session.SpawnBlasterRippers(Body); return; }
            if (action == HereticAction.A_BoltSpark) { session.SpawnCrossbowSpark(Body); return; }
            if (SupportsMace(action)) { ExecuteMace(action); return; }
            if (action == HereticAction.A_PhoenixPuff) session.SpawnPhoenixTrail(Body);
            else { session.PhoenixRadiusAttack(Body); session.HitLiquidFloor(Body); }
        }
        internal void SetFlightState(HereticStateId state) { Animation.SetState(state); Sync(); }
        private void Sync() { Body.Sprite = (Sprite)Animation.Definition.Sprite; Body.Frame = Animation.Definition.Frame; }
        internal bool Contact(Mobj target)
        {
            if (!Flying || target == Body.Target || Body.Z > target.Z + target.Height || Body.Z + Body.Height < target.Z) return true;
            var def = HereticDefinitions.Actors[(int)Type];
            if ((def.Flags2 & HereticActorFlags2.MF2_THRUGHOST) != 0 && (target.Flags & MobjFlags.Shadow) != 0) return true;
            if ((target.Flags & MobjFlags.Shootable) == 0) return (target.Flags & MobjFlags.Solid) == 0;
            if (Type == HereticActorType.MT_RIPPER)
            {
                if ((target.Flags & MobjFlags.NoBlood) == 0) session.SpawnRipperBlood(Body);
                session.RequestSound(HereticSoundId.sfx_ripslop, Body);
                session.DamageTestEnemy(target, ((session.World.Random.Next() & 3) + 2) * def.Damage, inflictor: Body);
                return true;
            }
            if (Type == HereticActorType.MT_MUMMYFX1 && session.SameMonsterType(target, MonsterOwnerType)) return false;
            var damage = (session.World.Random.Next() % 8 + 1) * def.Damage;
            if (Type == HereticActorType.MT_MUMMYFX1)
            {
                session.DamageMonsterMissile(target, damage, Body);
                return false;
            }
            // The reference consumes the missile damage roll, then always
            // returns from egg handling without ordinary damage or thrust.
            if (Type == HereticActorType.MT_EGGFX) { session.RequestEggMorph(target); return false; }
            if (Type == HereticActorType.MT_MACEFX4 && session.IsTestEnemy(target)) damage = 10000;
            session.DamageTestEnemy(target, damage, inflictor: Body);
            return false;
        }
        internal void Advance(bool half = false)
        {
            if (!Flying) return;
            if (Type == HereticActorType.MT_BLASTERFX1) { AdvanceBlaster(half); return; }
            bouncedThisTick = false;
            var dx = half ? Body.MomX / 2 : Body.MomX;
            var dy = half ? Body.MomY / 2 : Body.MomY;
            var dz = half ? Body.MomZ / 2 : Body.MomZ;
            // Bounded substeps prevent a bolt crossing a narrow target between collision checks.
            var magnitude = Math.Max(Math.Abs((long)dx.Data), Math.Max(Math.Abs((long)dy.Data), Math.Abs((long)dz.Data)));
            var steps = Type == HereticActorType.MT_RIPPER ? 1 : Math.Max(1, (int)((magnitude + 8 * Fixed.FracUnit - 1) / (8 * Fixed.FracUnit)));
            var startZ = Body.Z; var startX = Body.X; var startY = Body.Y;
            for (var i = 1; i <= steps; i++)
            {
                Body.Z = startZ + new Fixed((int)((long)dz.Data * i / steps));
                if (!session.World.ThingMovement.TryMove(Body, startX + new Fixed((int)((long)dx.Data * i / steps)), startY + new Fixed((int)((long)dy.Data * i / steps))))
                { Explode(session.World.ThingMovement.HereticMissileHitSky || (Body.Z + Body.Height > Body.Subsector.Sector.CeilingHeight && Body.Subsector.Sector.CeilingFlat == session.World.Map.SkyFlatNumber)); return; }
                if (Body.Z <= Body.FloorZ && IsMaceType(Type) && floorBounce)
                {
                    Body.Z = Body.FloorZ; Body.MomZ = -Body.MomZ; bouncedThisTick = true;
                    Animation.SetState(HereticDefinitions.Actors[(int)Type].DeathState); if (!Animation.Removed) Sync(); return;
                }
                if (Body.Z <= Body.FloorZ || Body.Z + Body.Height > Body.CeilingZ)
                {
                    // Rain impacts choose floor versus airborne animation by Z.
                    if (Type == HereticActorType.MT_RAINPLR3 && Body.Z <= Body.FloorZ) Body.Z = Body.FloorZ;
                    Explode(Body.Z + Body.Height > Body.CeilingZ && Body.Subsector.Sector.CeilingFlat == session.World.Map.SkyFlatNumber); return;
                }
            }
        }
        private void Explode(bool sky)
        {
            Flying = false; Body.MomX = Body.MomY = Body.MomZ = Fixed.Zero; Body.Flags &= ~MobjFlags.Missile;
            var def = HereticDefinitions.Actors[(int)Type];
            Animation.SetState(sky ? HereticStateId.S_NULL : def.DeathState);
            if (!sky) { Sync(); session.RequestSound(def.DeathSound, Body); }
        }
        internal void Tick()
        {
            Body.UpdateFrameInterpolationInfo();
            if (Flying) Advance();
            else if (Type == HereticActorType.MT_PHOENIXFX2)
            {
                Body.Z += Body.MomZ;
                if (Body.Z + Body.Height > Body.CeilingZ) { Body.Z = Body.CeilingZ - Body.Height; Body.MomZ = Fixed.Zero; }
            }
            if (Flying && LowGravity && !bouncedThisTick)
                Body.MomZ = Body.MomZ == Fixed.Zero ? -Fixed.One / 4 : Body.MomZ - Fixed.One / 8;
            if (!Animation.Removed) { Animation.Tick(); if (!Animation.Removed) Sync(); }
        }
    }
    public sealed partial class HereticWorldSession
    {
        private readonly List<HereticProjectile> projectiles = new();
        public IReadOnlyList<HereticProjectile> Projectiles => projectiles.AsReadOnly();
        internal HereticProjectile FindProjectile(Mobj body) => projectiles.Find(p => p.Body == body);
        internal HereticProjectile SpawnCrossbowBolt(HereticActorType type, Angle angle) => SpawnPlayerProjectile(type, angle);
        internal HereticProjectile SpawnPlayerProjectile(HereticActorType type, Angle angle)
        {
            var aiming = new Hitscan(world); var original = angle;
            var slope = aiming.AimLineAttack(Body, angle, Fixed.FromInt(1024));
            if (aiming.LineTarget == null) { angle += new Angle(1u << 26); slope = aiming.AimLineAttack(Body, angle, Fixed.FromInt(1024)); }
            if (aiming.LineTarget == null) { angle -= new Angle(2u << 26); slope = aiming.AimLineAttack(Body, angle, Fixed.FromInt(1024)); }
            if (aiming.LineTarget == null) { angle = original; slope = Fixed.FromInt(State.LookDirection) / 173; }
            var bolt = SpawnAimedProjectile(type, angle, slope);
            if ((type == HereticActorType.MT_MACEFX4 && bolt.Flying) || type == HereticActorType.MT_HORNRODFX2) bolt.SeekerTarget = aiming.LineTarget;
            return bolt;
        }
        internal HereticProjectile SpawnAimedProjectile(HereticActorType type, Angle angle, Fixed slope)
        {
            var bolt = new HereticProjectile(this, type, angle, slope);
            projectiles.Add(bolt);
            RequestSound(HereticDefinitions.Actors[(int)type].SeeSound, bolt.Body);
            bolt.Advance(true);
            return bolt;
        }
        private void TickProjectiles()
        {
            for (var i = projectiles.Count - 1; i >= 0; i--)
            {
                var bolt = projectiles[i]; bolt.Tick();
                if (bolt.Animation.Removed) { world.ThingMovement.UnsetThingPosition(bolt.Body); projectiles.RemoveAt(i); }
            }
        }
    }
}
