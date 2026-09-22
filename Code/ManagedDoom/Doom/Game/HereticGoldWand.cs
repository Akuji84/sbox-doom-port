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

// Adapted 2026-09-22: normal weapon/psprite and pickup rules from p_pspr.c, p_inter.c and g_game.c.
// Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
using System;
namespace ManagedDoom
{
    public readonly record struct HereticWandShot(int Damage, Angle Angle, Fixed Slope, HereticTraceHit? Hit);
    /// <summary>Normal staff, Gold Wand, Crossbow, Dragon Claw, Hellstaff, Phoenix Rod, Firemace and Gauntlets controller for the opt-in encounter.</summary>
    public sealed class HereticGoldWand
    {
        private readonly HereticWorldSession session;
        private readonly Hitscan aiming;
        private bool attack;
        private bool attackDown;
        private int depth;
        public HereticWeapon ReadyWeapon { get; private set; } = HereticWeapon.wp_goldwand;
        public HereticWeapon? PendingWeapon { get; private set; }
        public bool HasMace { get; private set; }
        public int MaceAmmo { get; private set; }
        public int MaceShots { get; private set; }
        public void GrantTestMace(int ammo = 50)
        {
            if (ammo < 1 || ammo > 150) throw new ArgumentOutOfRangeException(nameof(ammo));
            HasMace = true; MaceAmmo = ammo;
        }
        internal bool GiveMaceAmmo(int amount, bool bonus)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (session.State.Health <= 0 || MaceAmmo >= 150) return false;
            var empty = MaceAmmo == 0;
            MaceAmmo = (int)Math.Min(150L, MaceAmmo + (long)amount + (bonus ? amount / 2 : 0));
            if (empty && HasMace && (ReadyWeapon == HereticWeapon.wp_staff || ReadyWeapon == HereticWeapon.wp_gauntlets)) PendingWeapon = HereticWeapon.wp_mace;
            return true;
        }
        internal bool GiveMace(bool bonus)
        {
            if (session.State.Health <= 0) return false;
            var ammo = GiveMaceAmmo(50, bonus);
            if (HasMace) return ammo;
            HasMace = true;
            if (ReadyWeapon != HereticWeapon.wp_mace) PendingWeapon = HereticWeapon.wp_mace;
            return true;
        }
        public bool HasPhoenix { get; private set; }
        public int PhoenixAmmo { get; private set; }
        public int PhoenixShots { get; private set; }
        public void GrantTestPhoenix(int ammo = 2)
        {
            if (ammo < 1 || ammo > 20) throw new ArgumentOutOfRangeException(nameof(ammo));
            HasPhoenix = true; PhoenixAmmo = ammo;
        }
        internal bool GivePhoenixAmmo(int amount, bool bonus)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (session.State.Health <= 0 || PhoenixAmmo >= 20) return false;
            var empty = PhoenixAmmo == 0;
            PhoenixAmmo = (int)Math.Min(20L, PhoenixAmmo + (long)amount + (bonus ? amount / 2 : 0));
            if (empty && HasPhoenix && (ReadyWeapon == HereticWeapon.wp_staff || ReadyWeapon == HereticWeapon.wp_gauntlets)) PendingWeapon = HereticWeapon.wp_phoenixrod;
            return true;
        }
        internal bool GivePhoenix(bool bonus)
        {
            if (session.State.Health <= 0) return false;
            var ammo = GivePhoenixAmmo(2, bonus);
            if (HasPhoenix) return ammo;
            HasPhoenix = true;
            if (ReadyWeapon != HereticWeapon.wp_mace && ReadyWeapon != HereticWeapon.wp_phoenixrod) PendingWeapon = HereticWeapon.wp_phoenixrod;
            return true;
        }
        public bool HasSkullRod { get; private set; }
        public int SkullRodAmmo { get; private set; }
        public int SkullRodShots { get; private set; }
        public void GrantTestSkullRod(int ammo = 50)
        {
            if (ammo < 1 || ammo > 200) throw new ArgumentOutOfRangeException(nameof(ammo));
            HasSkullRod = true; SkullRodAmmo = ammo;
        }
        internal bool GiveSkullRodAmmo(int amount, bool bonus)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (session.State.Health <= 0 || SkullRodAmmo >= 200) return false;
            var empty = SkullRodAmmo == 0;
            SkullRodAmmo = (int)Math.Min(200L, SkullRodAmmo + (long)amount + (bonus ? amount / 2 : 0));
            if (empty && HasSkullRod && (ReadyWeapon == HereticWeapon.wp_staff || ReadyWeapon == HereticWeapon.wp_gauntlets)) PendingWeapon = HereticWeapon.wp_skullrod;
            return true;
        }
        internal bool GiveSkullRod(bool bonus)
        {
            if (session.State.Health <= 0) return false;
            var ammo = GiveSkullRodAmmo(50, bonus);
            if (HasSkullRod) return ammo;
            HasSkullRod = true;
            if (ReadyWeapon != HereticWeapon.wp_mace && ReadyWeapon != HereticWeapon.wp_phoenixrod && ReadyWeapon != HereticWeapon.wp_skullrod) PendingWeapon = HereticWeapon.wp_skullrod;
            return true;
        }
        public bool HasCrossbow { get; private set; }
        public int CrossbowAmmo { get; private set; }
        public int CrossbowShots { get; private set; }
        public void GrantTestCrossbow(int ammo = 20)
        {
            if (ammo < 1 || ammo > 50) throw new ArgumentOutOfRangeException(nameof(ammo));
            HasCrossbow = true; CrossbowAmmo = ammo;
        }
        internal bool GiveCrossbowAmmo(int amount, bool bonus)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (session.State.Health <= 0 || CrossbowAmmo >= 50) return false;
            var empty = CrossbowAmmo == 0;
            CrossbowAmmo = (int)Math.Min(50L, CrossbowAmmo + (long)amount + (bonus ? amount / 2 : 0));
            if (empty && HasCrossbow && (ReadyWeapon == HereticWeapon.wp_staff || ReadyWeapon == HereticWeapon.wp_gauntlets)) PendingWeapon = HereticWeapon.wp_crossbow;
            return true;
        }
        internal bool GiveCrossbow(bool bonus)
        {
            if (session.State.Health <= 0) return false;
            var ammo = GiveCrossbowAmmo(10, bonus);
            if (HasCrossbow) return ammo;
            HasCrossbow = true;
            if (ReadyWeapon != HereticWeapon.wp_mace && ReadyWeapon != HereticWeapon.wp_phoenixrod && ReadyWeapon != HereticWeapon.wp_skullrod && ReadyWeapon != HereticWeapon.wp_blaster && ReadyWeapon != HereticWeapon.wp_crossbow) PendingWeapon = HereticWeapon.wp_crossbow;
            return true;
        }
        public bool HasGauntlets { get; private set; }
        public int GauntletAttacks { get; private set; }
        public void GrantTestGauntlets() => HasGauntlets = true;
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
            if (previous == 0 && (ReadyWeapon == HereticWeapon.wp_staff || ReadyWeapon == HereticWeapon.wp_gauntlets) && (!blaster || HasBlaster))
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
            // Dragon Claw outranks the wand, crossbow and melee weapons.
            if (ReadyWeapon != HereticWeapon.wp_mace && ReadyWeapon != HereticWeapon.wp_phoenixrod && ReadyWeapon != HereticWeapon.wp_skullrod && ReadyWeapon != HereticWeapon.wp_blaster) PendingWeapon = HereticWeapon.wp_blaster;
            return true;
        }
        // Adapted 2026-09-22: single-player P_GiveWeapon/WeaponValue for gauntlets.
        internal bool GiveGauntlets()
        {
            if (session.State.Health <= 0 || HasGauntlets) return false;
            HasGauntlets = true;
            if (ReadyWeapon == HereticWeapon.wp_staff) PendingWeapon = HereticWeapon.wp_gauntlets;
            return true;
        }
        public int StaffSwings { get; private set; }
        private HereticWeaponDefinition Weapon => HereticDefinitions.Weapons1[(int)ReadyWeapon];
        public bool SelectWeapon(HereticWeapon weapon)
        {
            if (session.State.Health <= 0 || !Visible ||
                (weapon != HereticWeapon.wp_staff && weapon != HereticWeapon.wp_goldwand && weapon != HereticWeapon.wp_blaster && weapon != HereticWeapon.wp_gauntlets && weapon != HereticWeapon.wp_crossbow && weapon != HereticWeapon.wp_skullrod && weapon != HereticWeapon.wp_phoenixrod && weapon != HereticWeapon.wp_mace) ||
                (weapon == HereticWeapon.wp_goldwand && Ammo <= 0) ||
                (weapon == HereticWeapon.wp_blaster && (!HasBlaster || BlasterAmmo <= 0)) ||
                (weapon == HereticWeapon.wp_gauntlets && !HasGauntlets) ||
                (weapon == HereticWeapon.wp_mace && (!HasMace || MaceAmmo <= 0)) ||
                (weapon == HereticWeapon.wp_phoenixrod && (!HasPhoenix || PhoenixAmmo <= 0)) ||
                (weapon == HereticWeapon.wp_skullrod && (!HasSkullRod || SkullRodAmmo <= 0)) ||
                (weapon == HereticWeapon.wp_crossbow && (!HasCrossbow || CrossbowAmmo <= 0))) return false;
            if (weapon != ReadyWeapon) PendingWeapon = weapon;
            return true;
        }
        private static int WeaponAmmoCost(HereticWeapon weapon) => HereticDefinitions.Weapons1[(int)weapon].AmmoPerShot;
        private bool HasAmmo()
        {
            if ((ReadyWeapon == HereticWeapon.wp_staff || ReadyWeapon == HereticWeapon.wp_gauntlets) || (ReadyWeapon == HereticWeapon.wp_mace ? MaceAmmo > 0 : ReadyWeapon == HereticWeapon.wp_phoenixrod ? PhoenixAmmo > 0 : ReadyWeapon == HereticWeapon.wp_skullrod ? SkullRodAmmo > 0 : ReadyWeapon == HereticWeapon.wp_crossbow ? CrossbowAmmo > 0 : ReadyWeapon == HereticWeapon.wp_blaster ? BlasterAmmo > 0 : Ammo > 0)) return true;
            // P_CheckAmmo uses strictly more than one shot for automatic selection.
            PendingWeapon = HasSkullRod && SkullRodAmmo > 1 ? HereticWeapon.wp_skullrod : HasBlaster && BlasterAmmo > WeaponAmmoCost(HereticWeapon.wp_blaster)
                ? HereticWeapon.wp_blaster : HasCrossbow && CrossbowAmmo > 1 ? HereticWeapon.wp_crossbow : HasMace && MaceAmmo > 1 ? HereticWeapon.wp_mace : Ammo > WeaponAmmoCost(HereticWeapon.wp_goldwand)
                ? HereticWeapon.wp_goldwand : HasGauntlets ? HereticWeapon.wp_gauntlets : HasPhoenix && PhoenixAmmo > 1 ? HereticWeapon.wp_phoenixrod : HereticWeapon.wp_staff;
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
        private void BeginAttack(bool held)
        {
            SetState(held ? Weapon.HoldAttack : Weapon.Attack);
            if (ReadyWeapon == HereticWeapon.wp_gauntlets) session.RequestSound(HereticSoundId.sfx_gntuse, session.Body);
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
                                    if (ReadyWeapon == HereticWeapon.wp_gauntlets) session.RequestSound(HereticSoundId.sfx_gntact, session.Body);
                                    SetState(Weapon.Up);
                                }
                            }
                            break;
                        case HereticAction.A_WeaponReady:
                            if (PendingWeapon != null) SetState(Weapon.Down);
                            else if (attack)
                            {
                                if ((!attackDown || ReadyWeapon != HereticWeapon.wp_phoenixrod) && HasAmmo())
                                { attackDown = true; BeginAttack(false); }
                            }
                            else
                            {
                                attackDown = false;
                                var body = session.Body;
                                var bob = (body.MomX * body.MomX + body.MomY * body.MomY) / 4;
                                if (bob > Fixed.FromInt(16)) bob = Fixed.FromInt(16);
                                var phase = (128 * session.World.LevelTime) & 8191;
                                X = Fixed.One + bob * Trig.Cos(new Angle((uint)(phase << 19)));
                                Y = Fixed.FromInt(32) + bob * Trig.Sin(new Angle((uint)((phase & 4095) << 19)));
                            }
                            break;
                        case HereticAction.A_ReFire:
                            if (attack && PendingWeapon == null && HasAmmo()) { Refire++; BeginAttack(true); }
                            else { Refire = 0; if (PendingWeapon == null) HasAmmo(); }
                            break;
                        case HereticAction.A_FireMacePL1: FireMace(); break;
                        case HereticAction.A_FirePhoenixPL1: FirePhoenix(); break;
                        case HereticAction.A_FireSkullRodPL1: FireSkullRod(); break;
                        case HereticAction.A_FireCrossbowPL1: FireCrossbow(); break;
                        case HereticAction.A_GauntletAttack: AttackGauntlets(); break;
                        case HereticAction.A_Light0: session.Camera.ExtraLight = 0; break;
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
        // Adapted 2026-09-22: pinned A_FireMacePL1/A_FireMacePL1B.
        private void FireMace()
        {
            var random = session.World.Random;
            var lob = random.Next() < 28;
            if (MaceAmmo <= 0 || session.State.Health <= 0) return;
            MaceAmmo--; MaceShots++;
            var body = session.Body;
            if (lob)
            {
                var speed = new Fixed(HereticDefinitions.Actors[(int)HereticActorType.MT_MACEFX2].Speed);
                var bolt = session.SpawnMaceProjectile(HereticActorType.MT_MACEFX2, body.X, body.Y,
                    body.Z + Fixed.FromInt(28) + Fixed.FromInt(session.State.LookDirection) / 16, body.Angle,
                    (body.MomX >> 1) + speed * Trig.Cos(body.Angle), (body.MomY >> 1) + speed * Trig.Sin(body.Angle),
                    Fixed.FromInt(2) + Fixed.FromInt(session.State.LookDirection) / 32);
                session.RequestSound(HereticSoundId.sfx_lobsht, bolt.Body);
                bolt.Advance(true);
            }
            else
            {
                X = Fixed.FromInt((random.Next() & 3) - 2);
                Y = Fixed.FromInt(32 + (random.Next() & 3));
                var angle = body.Angle + new Angle(unchecked((uint)(((random.Next() & 7) - 4) << 24)));
                var bolt = session.SpawnPlayerProjectile(HereticActorType.MT_MACEFX1, angle);
                if (bolt.Flying) bolt.DropTics = 16;
            }
        }
        // Adapted 2026-09-22 from pinned p_pspr.c A_FirePhoenixPL1.
        private void FirePhoenix()
        {
            if (PhoenixAmmo <= 0 || session.State.Health <= 0) return;
            PhoenixAmmo--; PhoenixShots++;
            session.SpawnPlayerProjectile(HereticActorType.MT_PHOENIXFX1, session.Body.Angle);
            var recoil = session.Body.Angle + Angle.Ang180;
            session.Body.MomX += Fixed.FromInt(4) * Trig.Cos(recoil);
            session.Body.MomY += Fixed.FromInt(4) * Trig.Sin(recoil);
        }
        // Adapted 2026-09-22 from pinned p_pspr.c A_FireSkullRodPL1.
        private void FireSkullRod()
        {
            if (SkullRodAmmo <= 0 || session.State.Health <= 0) return;
            SkullRodAmmo--; SkullRodShots++;
            var bolt = session.SpawnPlayerProjectile(HereticActorType.MT_HORNRODFX1, session.Body.Angle);
            // P_SpawnPlayerMissile returns null after a spawn collision: no extra RNG then.
            if (bolt.Flying && session.World.Random.Next() > 128) bolt.SetFlightState(HereticStateId.S_HRODFX1_2);
        }
        // Adapted 2026-09-22 from pinned p_pspr.c A_FireCrossbowPL1.
        private void FireCrossbow()
        {
            if (CrossbowAmmo <= 0 || session.State.Health <= 0) return;
            CrossbowAmmo--; CrossbowShots++;
            var angle = session.Body.Angle;
            session.SpawnCrossbowBolt(HereticActorType.MT_CRBOWFX1, angle);
            session.SpawnCrossbowBolt(HereticActorType.MT_CRBOWFX3, angle - new Angle(0x20000000u / 10));
            session.SpawnCrossbowBolt(HereticActorType.MT_CRBOWFX3, angle + new Angle(0x20000000u / 10));
        }
        // Adapted 2026-09-22 from pinned p_pspr.c A_GauntletAttack, unpowered only.
        private void AttackGauntlets()
        {
            if (session.State.Health <= 0) return;
            var random = session.World.Random;
            X = Fixed.FromInt((random.Next() & 3) - 2);
            Y = Fixed.FromInt(32 + (random.Next() & 3));
            var body = session.Body;
            var damage = ((random.Next() & 7) + 1) * 2;
            var angle = body.Angle + new Angle(unchecked((uint)((random.Next() - random.Next()) << 18)));
            var range = Fixed.FromInt(65);
            var slope = aiming.AimLineAttack(body, angle, range);
            var hit = session.TraceWeapon(angle, range, slope, body.Z + (body.Height >> 1) + Fixed.FromInt(8));
            session.SpawnWeaponImpact(hit, angle, slope, ReadyWeapon);
            session.SpawnWeaponBlood(hit, angle, slope);
            GauntletAttacks++;
            if (hit?.Actor == null)
            {
                if (random.Next() > 64) session.Camera.ExtraLight = session.Camera.ExtraLight == 0 ? 1 : 0;
                session.RequestSound(HereticSoundId.sfx_gntful, body);
                return;
            }
            var target = hit.Value.Actor;
            session.DamageTestEnemy(target, damage);
            var light = random.Next(); session.Camera.ExtraLight = light < 64 ? 0 : light < 160 ? 1 : 2;
            session.RequestSound(HereticSoundId.sfx_gnthit, body);
            angle = Geometry.PointToAngle(body.X, body.Y, target.X, target.Y);
            var delta = unchecked(angle.Data - body.Angle.Data);
            const uint turn = 0x40000000u / 20;
            const uint offset = 0x40000000u / 21;
            if (delta > 0x80000000u)
                body.Angle = unchecked((int)delta) < -(int)turn ? angle + new Angle(offset) : body.Angle - new Angle(turn);
            else body.Angle = delta > turn ? angle - new Angle(offset) : body.Angle + new Angle(turn);
            body.Flags |= MobjFlags.JustAttacked;
        }
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
