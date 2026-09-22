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
    public sealed class HereticProjectile
    {
        private readonly HereticWorldSession session;
        public Mobj Body { get; }
        public HereticActorType Type { get; }
        public HereticActorState Animation { get; }
        public bool Flying { get; private set; } = true;
        internal HereticProjectile(HereticWorldSession session, HereticActorType type, Angle angle, Fixed slope)
        {
            if (type != HereticActorType.MT_CRBOWFX1 && type != HereticActorType.MT_CRBOWFX3 && type != HereticActorType.MT_HORNRODFX1)
                throw new NotSupportedException("Only normal crossbow and Hellstaff projectiles are enabled.");
            this.session = session; Type = type;
            var def = HereticDefinitions.Actors[(int)type];
            Animation = new HereticActorState(def.SpawnState);
            Body = new Mobj(session.World) { X = session.Body.X, Y = session.Body.Y,
                Z = session.Body.Z + Fixed.FromInt(32) + Fixed.FromInt(session.State.LookDirection) / 173,
                Radius = def.Radius, Height = def.Height, Health = def.SpawnHealth,
                Flags = MobjFlags.Missile | MobjFlags.NoGravity | MobjFlags.NoBlockMap | MobjFlags.DropOff,
                Target = session.Body, Angle = angle, LastLook = session.World.Random.Next() % 4,
                MomX = new Fixed(def.Speed) * Trig.Cos(angle), MomY = new Fixed(def.Speed) * Trig.Sin(angle),
                MomZ = new Fixed(def.Speed) * slope };
            Sync();
            session.World.ThingMovement.SetThingPosition(Body);
            Body.FloorZ = Body.Subsector.Sector.FloorHeight; Body.CeilingZ = Body.Subsector.Sector.CeilingHeight;
            Body.UpdateFrameInterpolationInfo();
        }
        internal void SetFlightState(HereticStateId state) { Animation.SetState(state); Sync(); }
        private void Sync() { Body.Sprite = (Sprite)Animation.Definition.Sprite; Body.Frame = Animation.Definition.Frame; }
        internal bool Contact(Mobj target)
        {
            if (!Flying || target == Body.Target || Body.Z > target.Z + target.Height || Body.Z + Body.Height < target.Z) return true;
            var def = HereticDefinitions.Actors[(int)Type];
            if ((def.Flags2 & HereticActorFlags2.MF2_THRUGHOST) != 0 && (target.Flags & MobjFlags.Shadow) != 0) return true;
            if ((target.Flags & MobjFlags.Shootable) == 0) return (target.Flags & MobjFlags.Solid) == 0;
            session.DamageTestEnemy(target, (session.World.Random.Next() % 8 + 1) * def.Damage, inflictor: Body);
            return false;
        }
        internal void Advance(bool half = false)
        {
            if (!Flying) return;
            var dx = half ? Body.MomX / 2 : Body.MomX;
            var dy = half ? Body.MomY / 2 : Body.MomY;
            var dz = half ? Body.MomZ / 2 : Body.MomZ;
            // Bounded substeps prevent a bolt crossing a narrow target between collision checks.
            var magnitude = Math.Max(Math.Abs((long)dx.Data), Math.Max(Math.Abs((long)dy.Data), Math.Abs((long)dz.Data)));
            var steps = Math.Max(1, (int)((magnitude + 8 * Fixed.FracUnit - 1) / (8 * Fixed.FracUnit)));
            var startZ = Body.Z; var startX = Body.X; var startY = Body.Y;
            for (var i = 1; i <= steps; i++)
            {
                Body.Z = startZ + new Fixed((int)((long)dz.Data * i / steps));
                if (!session.World.ThingMovement.TryMove(Body, startX + new Fixed((int)((long)dx.Data * i / steps)), startY + new Fixed((int)((long)dy.Data * i / steps))))
                { Explode(session.World.ThingMovement.HereticMissileHitSky || (Body.Z + Body.Height > Body.Subsector.Sector.CeilingHeight && Body.Subsector.Sector.CeilingFlat == session.World.Map.SkyFlatNumber)); return; }
                if (Body.Z <= Body.FloorZ || Body.Z + Body.Height > Body.CeilingZ)
                { Explode(Body.Z + Body.Height > Body.CeilingZ && Body.Subsector.Sector.CeilingFlat == session.World.Map.SkyFlatNumber); return; }
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
