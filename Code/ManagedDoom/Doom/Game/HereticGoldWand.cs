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
    /// <summary>Normal staff, Gold Wand and Dragon Claw controller for the opt-in encounter.</summary>
    public sealed class HereticGoldWand
    {
        private readonly HereticWorldSession session;
        private readonly Hitscan aiming;
        private bool attack;
        private int depth;
        public HereticWeapon ReadyWeapon { get; private set; } = HereticWeapon.wp_goldwand;
        public HereticWeapon? PendingWeapon { get; private set; }
        public bool HasBlaster { get; private set; }
        public int BlasterAmmo { get; private set; }
        public int BlasterShots { get; private set; }
        public event Action<HereticWandShot> BlasterShotFired;
        public void GrantTestBlaster(int ammo = 50)
        {
            if (ammo < 1 || ammo > 200) throw new ArgumentOutOfRangeException(nameof(ammo));
            HasBlaster = true; BlasterAmmo = ammo;
        }
        // Adapted 2026-09-22 from pinned p_inter.c P_GiveAmmo (implemented ammo types).
        internal bool GiveAmmo(bool blaster, int amount, bool bonus)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            var previous = blaster ? BlasterAmmo : Ammo;
            var maximum = blaster ? 200 : 100;
            if (previous >= maximum || session.State.Health <= 0) return false;
            if (bonus) amount += amount >> 1;
            var total = (int)Math.Min(maximum, (long)previous + amount);
            if (blaster) BlasterAmmo = total; else Ammo = total;
            if (previous == 0 && ReadyWeapon == HereticWeapon.wp_staff && (!blaster || HasBlaster))
                PendingWeapon = blaster ? HereticWeapon.wp_blaster : HereticWeapon.wp_goldwand;
            return true;
        }
        // Adapted 2026-09-22 from pinned p_inter.c P_GiveWeapon, single-player only.
        internal bool GiveBlaster(bool bonus)
        {
            if (session.State.Health <= 0) return false;
            var gaveAmmo = GiveAmmo(true, 30, bonus);
            if (HasBlaster) return gaveAmmo;
            HasBlaster = true;
            // Dragon Claw outranks both other currently implemented weapons.
            if (ReadyWeapon != HereticWeapon.wp_blaster) PendingWeapon = HereticWeapon.wp_blaster;
            return true;
        }
        public int StaffSwings { get; private set; }
        private HereticWeaponDefinition Weapon => HereticDefinitions.Weapons1[(int)ReadyWeapon];
        public bool SelectWeapon(HereticWeapon weapon)
        {
            if (session.State.Health <= 0 || !Visible ||
                (weapon != HereticWeapon.wp_staff && weapon != HereticWeapon.wp_goldwand && weapon != HereticWeapon.wp_blaster) ||
                (weapon == HereticWeapon.wp_goldwand && Ammo <= 0) ||
                (weapon == HereticWeapon.wp_blaster && (!HasBlaster || BlasterAmmo <= 0))) return false;
            if (weapon != ReadyWeapon) PendingWeapon = weapon;
            return true;
        }
        private static int WeaponAmmoCost(HereticWeapon weapon) => HereticDefinitions.Weapons1[(int)weapon].AmmoPerShot;
        private bool HasAmmo()
        {
            if (ReadyWeapon == HereticWeapon.wp_staff || (ReadyWeapon == HereticWeapon.wp_blaster ? BlasterAmmo > 0 : Ammo > 0)) return true;
            // P_CheckAmmo uses strictly more than one shot for automatic selection.
            PendingWeapon = HasBlaster && BlasterAmmo > WeaponAmmoCost(HereticWeapon.wp_blaster)
                ? HereticWeapon.wp_blaster : Ammo > WeaponAmmoCost(HereticWeapon.wp_goldwand)
                ? HereticWeapon.wp_goldwand : HereticWeapon.wp_staff;
            SetState(Weapon.Down);
            return false;
        }
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
            if (session.State.Health <= 0 && State != Weapon.Down)
                SetState(Weapon.Down);
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
                            if (Y <= Fixed.FromInt(32)) { Y = Fixed.FromInt(32); SetState(Weapon.Ready); }
                            break;
                        case HereticAction.A_Lower:
                            Y += Fixed.FromInt(6);
                            if (Y >= Fixed.FromInt(128))
                            {
                                Y = Fixed.FromInt(128);
                                if (session.State.Health <= 0) SetState(HereticStateId.S_NULL);
                                else
                                {
                                    ReadyWeapon = PendingWeapon ?? ReadyWeapon;
                                    PendingWeapon = null;
                                    Refire = 0;
                                    SetState(Weapon.Up);
                                }
                            }
                            break;
                        case HereticAction.A_WeaponReady:
                            if (PendingWeapon != null) SetState(Weapon.Down);
                            else if (attack) { if (HasAmmo()) SetState(Weapon.Attack); }
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
                            if (attack && PendingWeapon == null && HasAmmo()) { Refire++; SetState(Weapon.HoldAttack); }
                            else { Refire = 0; if (PendingWeapon == null) HasAmmo(); }
                            break;
                        case HereticAction.A_StaffAttackPL1: SwingStaff(); break;
                        case HereticAction.A_FireGoldWandPL1: Fire(false); break;
                        case HereticAction.A_FireBlasterPL1: Fire(true); break;
                        default: throw new NotSupportedException("Gold Wand action: " + state.Action);
                    }
                    if (!Visible) return;
                    next = Definition.Next;
                } while (Tics == 0);
            }
            finally { depth--; }
        }
        // Adapted 2026-09-22 from the pinned p_pspr.c A_StaffAttackPL1.
        private void SwingStaff()
        {
            if (session.State.Health <= 0) return;
            var random = session.World.Random;
            var damage = 5 + (random.Next() & 15);
            var body = session.Body;
            var angle = body.Angle + new Angle(unchecked((uint)((random.Next() - random.Next()) << 18)));
            var range = Fixed.FromInt(64);
            var slope = aiming.AimLineAttack(body, angle, range);
            var hit = session.TraceWeapon(angle, range, slope, body.Z + (body.Height >> 1) + Fixed.FromInt(8), physicalStaff: true);
            session.SpawnWeaponImpact(hit, angle, slope, ReadyWeapon);
            session.SpawnWeaponBlood(hit, angle, slope);
            if (hit?.Actor != null)
            {
                var target = hit.Value.Actor;
                session.DamageTestEnemy(target, damage);
                body.Angle = Geometry.PointToAngle(body.X, body.Y, target.X, target.Y);
            }
            StaffSwings++;
        }
        private void Fire(bool blaster)
        {
            if ((blaster ? BlasterAmmo : Ammo) <= 0 || session.State.Health <= 0) return;
            if (blaster) { session.RequestSound(HereticSoundId.sfx_gldhit, session.Body); BlasterAmmo--; }
            else Ammo--;
            var body = session.Body;
            var slope = aiming.AimLineAttack(body, body.Angle, Fixed.FromInt(1024));
            if (aiming.LineTarget == null)
            {
                slope = aiming.AimLineAttack(body, body.Angle + new Angle(1u << 26), Fixed.FromInt(1024));
                if (aiming.LineTarget == null) slope = aiming.AimLineAttack(body, body.Angle - new Angle(1u << 26), Fixed.FromInt(1024));
                if (aiming.LineTarget == null) slope = Fixed.FromInt(session.State.LookDirection) / 173;
            }
            var random = session.World.Random;
            var damage = blaster ? ((random.Next() & 7) + 1) * 4 : 7 + (random.Next() & 7);
            var angle = body.Angle;
            if (Refire != 0) angle += new Angle(unchecked((uint)((random.Next() - random.Next()) << 18)));
            var origin = body.Z + (body.Height >> 1) + Fixed.FromInt(8);
            var hit = session.TraceWeapon(angle, Fixed.FromInt(2048), slope, origin);
            session.SpawnWeaponImpact(hit, angle, slope, ReadyWeapon);
            session.SpawnWeaponBlood(hit, angle, slope);
            if (hit?.Actor != null) session.DamageTestEnemy(hit.Value.Actor, damage);
            session.RequestSound(blaster ? HereticSoundId.sfx_blssht : HereticSoundId.sfx_gldhit, body);
            if (blaster) { BlasterShots++; BlasterShotFired?.Invoke(new(damage, angle, slope, hit)); }
            else { ShotsFired++; ShotFired?.Invoke(new(damage, angle, slope, hit)); }
        }
    }
}
