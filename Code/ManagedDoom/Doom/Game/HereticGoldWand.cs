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

// Adapted 2026-09-22: normal Gold Wand/psprite rules from p_pspr.c and g_game.c.
// Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
using System;
namespace ManagedDoom
{
    public readonly record struct HereticWandShot(int Damage, Angle Angle, Fixed Slope, HereticTraceHit? Hit);
    /// <summary>Normal Gold Wand in the opt-in encounter; other weapons/powers are not enabled.</summary>
    public sealed class HereticGoldWand
    {
        private readonly HereticWorldSession session;
        private readonly Hitscan aiming;
        private bool attack;
        private int depth;
        public int Ammo { get; internal set; } = 50;
        public int Refire { get; private set; }
        public int ShotsFired { get; private set; }
        public HereticStateId State { get; private set; }
        public int Tics { get; private set; }
        public Fixed X { get; private set; } = Fixed.One;
        public Fixed Y { get; private set; } = Fixed.FromInt(128);
        public HereticStateDefinition Definition => HereticDefinitions.States[(int)State];
        public bool Visible => State != HereticStateId.S_NULL;
        public event Action<HereticWandShot> ShotFired;
        internal HereticGoldWand(HereticWorldSession session)
        {
            this.session = session;
            aiming = new Hitscan(session.World);
            SetState(HereticStateId.S_GOLDWANDUP);
        }
        public void Tick(bool attackHeld)
        {
            attack = attackHeld && session.State.Health > 0;
            if (!Visible) return;
            if (session.State.Health <= 0 && State != HereticStateId.S_GOLDWANDDOWN)
                SetState(HereticStateId.S_GOLDWANDDOWN);
            if (Tics != -1 && --Tics == 0) SetState(Definition.Next);
        }
        private void SetState(HereticStateId next)
        {
            if (++depth > 32) { depth--; throw new InvalidOperationException("Recursive wand state action."); }
            try
            {
                var steps = 0;
                do
                {
                    if (++steps > 64) throw new InvalidOperationException("Zero-duration wand state cycle.");
                    State = next;
                    if (!Visible) return;
                    var state = Definition;
                    Tics = state.Tics;
                    if (state.Misc1 != 0) { X = Fixed.FromInt(state.Misc1); Y = Fixed.FromInt(state.Misc2); }
                    switch (state.Action)
                    {
                        case HereticAction.None: break;
                        case HereticAction.A_Raise:
                            Y -= Fixed.FromInt(6);
                            if (Y <= Fixed.FromInt(32)) { Y = Fixed.FromInt(32); SetState(HereticStateId.S_GOLDWANDREADY); }
                            break;
                        case HereticAction.A_Lower:
                            Y += Fixed.FromInt(6);
                            if (Y >= Fixed.FromInt(128)) { Y = Fixed.FromInt(128); SetState(HereticStateId.S_NULL); }
                            break;
                        case HereticAction.A_WeaponReady:
                            if (attack && Ammo > 0) SetState(HereticStateId.S_GOLDWANDATK1_1);
                            else
                            {
                                var body = session.Body;
                                var bob = (body.MomX * body.MomX + body.MomY * body.MomY) / 4;
                                if (bob > Fixed.FromInt(16)) bob = Fixed.FromInt(16);
                                var phase = (128 * session.World.LevelTime) & 8191;
                                X = Fixed.One + bob * Trig.Cos(new Angle((uint)(phase << 19)));
                                Y = Fixed.FromInt(32) + bob * Trig.Sin(new Angle((uint)((phase & 4095) << 19)));
                            }
                            break;
                        case HereticAction.A_ReFire:
                            if (attack && Ammo > 0) { Refire++; SetState(HereticStateId.S_GOLDWANDATK1_1); }
                            else Refire = 0;
                            break;
                        case HereticAction.A_FireGoldWandPL1: Fire(); break;
                        default: throw new NotSupportedException("Gold Wand action: " + state.Action);
                    }
                    if (!Visible) return;
                    next = Definition.Next;
                } while (Tics == 0);
            }
            finally { depth--; }
        }
        private void Fire()
        {
            if (Ammo <= 0 || session.State.Health <= 0) return;
            Ammo--;
            var body = session.Body;
            var slope = aiming.AimLineAttack(body, body.Angle, Fixed.FromInt(1024));
            if (aiming.LineTarget == null)
            {
                slope = aiming.AimLineAttack(body, body.Angle + new Angle(1u << 26), Fixed.FromInt(1024));
                if (aiming.LineTarget == null) slope = aiming.AimLineAttack(body, body.Angle - new Angle(1u << 26), Fixed.FromInt(1024));
                if (aiming.LineTarget == null) slope = Fixed.FromInt(session.State.LookDirection) / 173;
            }
            var random = session.World.Random;
            var damage = 7 + (random.Next() & 7);
            var angle = body.Angle;
            if (Refire != 0) angle += new Angle(unchecked((uint)((random.Next() - random.Next()) << 18)));
            var origin = body.Z + (body.Height >> 1) + Fixed.FromInt(8);
            var hit = session.TraceAim(angle, Fixed.FromInt(2048), slope, origin, shootableOnly: true);
            if (hit?.Actor != null) session.DamageTestEnemy(hit.Value.Actor, damage);
            ShotsFired++;
            ShotFired?.Invoke(new(damage, angle, slope, hit));
        }
    }
}
