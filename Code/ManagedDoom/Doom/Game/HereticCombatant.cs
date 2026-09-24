// s&Doom modification: 2026-09-24, Gargoyle corpse foot-clipping flag.
// s&Doom modification: 2026-09-22, opt-in native Clink test encounter integration.
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

// Adapted 2026-09-22: ordinary non-player damage/kill rules from p_inter.c.
// Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
using System;
using System.Collections.Generic;
namespace ManagedDoom
{
    public enum HereticDamageResult { Ignored, Hurt, Pain, Killed }
    public enum HereticDamageThrust { Normal, None, PoweredStaff }

    /// <summary>Family-owned non-player damage state. The owner supplies real state actions,
    /// movement, spatial linking, kill accounting, drops and audio; this does not enable map AI.</summary>
    public sealed class HereticCombatant
    {
        public HereticActorType Type { get; }
        public Mobj Body { get; }
        public HereticActorState Animation { get; }
        public HereticActorFlags2 Flags2 { get; private set; }
        public int LastDamage { get; private set; }
        internal void EnableFootClipping() => Flags2 |= HereticActorFlags2.MF2_FOOTCLIP;
        public Mobj Killer { get; private set; }
        private HereticActorDefinition Definition => HereticDefinitions.Actors[(int)Type];

        public HereticCombatant(World world, HereticActorType type, IHereticActorActions actions)
        {
            if (world?.HereticSession == null) throw new ArgumentException("Combatant requires a Heretic session.", nameof(world));
            if ((uint)type >= HereticDefinitions.Actors.Count) throw new ArgumentOutOfRangeException(nameof(type));
            if (actions == null) throw new ArgumentNullException(nameof(actions));
            var def = HereticDefinitions.Actors[(int)type];
            if ((def.Flags & (HereticActorFlags.MF_SHOOTABLE | HereticActorFlags.MF_COUNTKILL)) != (HereticActorFlags.MF_SHOOTABLE | HereticActorFlags.MF_COUNTKILL) || (def.Flags2 & HereticActorFlags2.MF2_BOSS) != 0 || def.Mass <= 0)
                throw new ArgumentException("Ordinary combatant requires a non-boss monster; players, bosses and destructible props need specialized damage handlers.", nameof(type));
            // Validate complete reachable chains before allocating an actor or advancing randomness.
            var visited = new HashSet<HereticStateId>();
            foreach (var entry in new[] { def.SpawnState, def.SeeState, def.PainState, def.MeleeState, def.MissileState, def.CrashState, def.DeathState, def.ExtremeDeathState })
            {
                var state = entry;
                while (state != HereticStateId.S_NULL && visited.Add(state))
                {
                    var frame = HereticDefinitions.States[(int)state];
                    if (frame.Action != HereticAction.None && !actions.Supports(frame.Action))
                        throw new NotSupportedException("Heretic combatant requires action: " + frame.Action);
                    if (frame.Tics == -1) break;
                    state = frame.Next;
                }
            }
            Type = type;
            Flags2 = def.Flags2;
            Body = new Mobj(world) { Health = def.SpawnHealth, Radius = def.Radius, Height = def.Height,
                Flags = (MobjFlags)(uint)def.Flags, ReactionTime = def.ReactionTime };
            Animation = new HereticActorState(def.SpawnState, actions);
            SyncFrame();
        }
        public void TickState()
        {
            Animation.Tick();
            SyncFrame();
        }
        private void SyncFrame()
        {
            Body.Sprite = (Sprite)Animation.Definition.Sprite;
            Body.Frame = Animation.Definition.Frame;
        }
        /// <summary>Call after attack-specific effects have been resolved. Does not implement
        /// morphing, death balls, rain scaling or player armor/inventory protections.</summary>
        public HereticDamageResult ApplyOrdinaryDamage(int damage, Mobj inflictor = null, Mobj source = null,
            HereticDamageThrust thrustMode = HereticDamageThrust.Normal, bool sourceIsBoss = false)
        {
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
            if ((uint)thrustMode > (uint)HereticDamageThrust.PoweredStaff) throw new ArgumentOutOfRangeException(nameof(thrustMode));
            if ((inflictor != null && inflictor.World != Body.World) || (source != null && source.World != Body.World))
                throw new ArgumentException("Damage sources must belong to the target's session.");
            if (damage == 0 || Animation.Removed || Body.Health <= 0 || (Body.Flags & MobjFlags.Shootable) == 0)
                return HereticDamageResult.Ignored;
            var random = Body.World.Random;
            if ((Body.Flags & MobjFlags.SkullFly) != 0)
            {
                Body.MomX = Body.MomY = Body.MomZ = Fixed.Zero;
            }
            if (inflictor != null && thrustMode != HereticDamageThrust.None)
            {
                var angle = Geometry.PointToAngle(inflictor.X, inflictor.Y, Body.X, Body.Y);
                // Preserve the reference's explicit 32-bit wrap before signed division.
                var thrust = new Fixed(unchecked((int)((uint)damage * (Fixed.FracUnit >> 3) * 150u)) / Definition.Mass);
                if (damage < 40 && damage > Body.Health && Body.Z - inflictor.Z > Fixed.FromInt(64) && (random.Next() & 1) != 0)
                { angle += Angle.Ang180; thrust *= 4; }
                if (thrustMode == HereticDamageThrust.PoweredStaff)
                {
                    thrust = Fixed.FromInt(10);
                    if ((Body.Flags & MobjFlags.NoGravity) == 0) Body.MomZ += Fixed.FromInt(5);
                }
                Body.MomX += thrust * Trig.Cos(angle);
                Body.MomY += thrust * Trig.Sin(angle);
            }
            Body.Health = (int)Math.Max(int.MinValue, (long)Body.Health - damage);
            LastDamage = damage;
            if (Body.Health <= 0)
            {
                Killer = source;
                Body.Flags &= ~(MobjFlags.Shootable | MobjFlags.Float | MobjFlags.SkullFly | MobjFlags.NoGravity);
                Body.Flags |= MobjFlags.Corpse | MobjFlags.DropOff;
                Flags2 &= ~HereticActorFlags2.MF2_PASSMOBJ;
                Body.Height >>= 2;
                var death = Body.Health < -(Definition.SpawnHealth >> 1) && Definition.ExtremeDeathState != HereticStateId.S_NULL
                    ? Definition.ExtremeDeathState : Definition.DeathState;
                Animation.SetState(death);
                Animation.ShortenPositiveTics(random.Next() & 3);
                SyncFrame();
                return HereticDamageResult.Killed;
            }
            var result = HereticDamageResult.Hurt;
            if (random.Next() < Definition.PainChance && (Body.Flags & MobjFlags.SkullFly) == 0)
            {
                Body.Flags |= MobjFlags.JustHit;
                Animation.SetState(Definition.PainState);
                result = HereticDamageResult.Pain;
            }
            Body.ReactionTime = 0;
            if (Body.Threshold == 0 && source != null && !sourceIsBoss)
            {
                Body.Target = source;
                Body.Threshold = 100;
                if (Animation.State == Definition.SpawnState && Definition.SeeState != HereticStateId.S_NULL)
                    Animation.SetState(Definition.SeeState);
            }
            SyncFrame();
            return result;
        }
    }
}
