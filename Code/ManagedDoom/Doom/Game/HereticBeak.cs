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

// Adapted 2026-09-22: beak activation and attacks from pinned p_pspr.c.
// Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
namespace ManagedDoom
{
    public sealed partial class HereticGoldWand
    {
        internal int BeakPeck { get; private set; }
        internal int BeakAttacks { get; private set; }
        internal int LastBeakDamage { get; private set; }
        // Called by the forthcoming player-morph lifecycle, not a selectable gun.
        internal void ActivateBeak()
        {
            if (session.State.Health <= 0) return;
            PendingWeapon = null; ReadyWeapon = HereticWeapon.wp_beak;
            Y = Fixed.FromInt(32); BeakPeck = 0;
            SetState(HereticStateId.S_BEAKREADY);
        }
        private void AttackBeak(bool powered)
        {
            if (session.State.Health <= 0) return;
            var random = session.World.Random;
            var damage = powered ? 4 * ((random.Next() & 7) + 1) : 1 + (random.Next() & 3);
            var body = session.Body; var angle = body.Angle; var range = Fixed.FromInt(64);
            var slope = aiming.AimLineAttack(body, angle, range);
            var hit = session.TraceWeapon(angle, range, slope, body.Z + (body.Height >> 1) + Fixed.FromInt(8));
            session.SpawnWeaponImpact(hit, angle, slope, HereticWeapon.wp_beak);
            session.SpawnWeaponBlood(hit, angle, slope);
            if (hit?.Actor is Mobj target)
            {
                session.DamageTestEnemy(target, damage);
                body.Angle = Geometry.PointToAngle(body.X, body.Y, target.X, target.Y);
            }
            session.RequestSound((HereticSoundId)((int)HereticSoundId.sfx_chicpk1 + random.Next() % 3), body);
            BeakPeck = 12; BeakAttacks++; LastBeakDamage = damage;
            Tics -= random.Next() & (powered ? 3 : 7);
        }
    }
}
