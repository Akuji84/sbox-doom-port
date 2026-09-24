// s&Doom modification: 2026-09-24, Disciple three-projectile volleys.
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
// Adapted 2026-09-24: pinned p_enemy.c A_WizAtk3.
namespace ManagedDoom
{
    public sealed partial class HereticWorldSession
    {
        internal void SpawnWizardVolley(HereticClinkTestEnemy enemy)
        {
            var center = SpawnMonsterMissile(enemy, HereticActorType.MT_WIZFX1);
            if (center == null || !center.Flying) return;
            var angle = center.Body.Angle; var momZ = center.Body.MomZ;
            SpawnMonsterMissile(enemy, HereticActorType.MT_WIZFX1, angle - new Angle(0x04000000u), momZ);
            SpawnMonsterMissile(enemy, HereticActorType.MT_WIZFX1, angle + new Angle(0x04000000u), momZ);
        }
    }
}
