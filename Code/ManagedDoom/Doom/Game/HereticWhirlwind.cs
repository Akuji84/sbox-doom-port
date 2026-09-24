// s&Doom modification: 2026-09-24, Maulotaur boss rules and map combat.
// s&Doom modification: 2026-09-24, Iron Lich whirlwind lifetime, seeking and touch effects.
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
// Adapted from pinned p_enemy.c A_WhirlwindSeek, p_mobj.c P_ExplodeMissile,
// and p_inter.c P_TouchWhirlwind.
using System;
namespace ManagedDoom
{
    public sealed partial class HereticProjectile
    {
        private int whirlwindSoundTimer;
        internal void InitializeWhirlwind(Mobj target)
        {
            Body.Z -= Fixed.FromInt(32);
            SeekerTarget = target;
            whirlwindSoundTimer = 50;
            Body.Health = 20 * 35;
            Body.UpdateFrameInterpolationInfo();
        }
        private void SeekWhirlwind()
        {
            Body.Health -= 3;
            if (Body.Health < 0)
            {
                // Duration expiry is unconditional, unlike repeated collision attempts.
                whirlwindSoundTimer = 60;
                Explode(false);
                return;
            }
            if ((whirlwindSoundTimer -= 3) < 0)
            {
                whirlwindSoundTimer = 58 + (session.World.Random.Next() & 31);
                session.RequestSound(HereticSoundId.sfx_hedat3, Body);
            }
            if (SeekerTarget != null && (SeekerTarget.Flags & MobjFlags.Shadow) != 0) return;
            var target = SeekerTarget;
            HereticSeeker.Seek(Body, ref target, Fixed.FromInt(10), 10u * 0x01000000u, 30u * 0x01000000u);
            SeekerTarget = target;
        }
        private bool DeferWhirlwindImpact() => Type == HereticActorType.MT_WHIRLWIND && ++whirlwindSoundTimer < 60;
    }
    public sealed partial class HereticWorldSession
    {
        internal HereticProjectile SpawnIronLichWhirlwind(HereticClinkTestEnemy enemy)
        {
            var wind = SpawnMonsterMissile(enemy, HereticActorType.MT_WHIRLWIND);
            if (wind != null && wind.Flying)
            {
                wind.InitializeWhirlwind(Body);
                RequestSound(HereticSoundId.sfx_hedat3, enemy.Body);
            }
            return wind;
        }
        internal void TouchWhirlwind(Mobj target)
        {
            if (IsChargingBoss(target)) return;
            target.Angle += new Angle(unchecked((uint)((world.Random.Next() - world.Random.Next()) << 20)));
            target.MomX += new Fixed((world.Random.Next() - world.Random.Next()) << 10);
            target.MomY += new Fixed((world.Random.Next() - world.Random.Next()) << 10);
            bool boss = false;
            foreach (var enemy in testEnemies)
                if (enemy.Body == target) { boss = (enemy.Combatant.Flags2 & HereticActorFlags2.MF2_BOSS) != 0; break; }
            if ((tic & 16) != 0 && !boss)
                target.MomZ = new Fixed(Math.Min((target.MomZ + new Fixed(Math.Min(world.Random.Next(),160) << 10)).Data, 12 * Fixed.FracUnit));
            if ((tic & 7) == 0)
            {
                if (target == Body) DamageEnvironment(3);
                else DamageTestEnemy(target,3,environment:true);
            }
        }
    }
}
