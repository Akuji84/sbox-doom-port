// s&Doom modification: 2026-09-22, opt-in registered enemy morph dispatch.
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

// Adapted 2026-09-22: Morph Ovum volley from pinned p_user.c/P_SPMAngle.
// Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
using System;
namespace ManagedDoom
{
    public sealed partial class HereticWorldSession
    {
        // Internal foundation only: inventory activation stays gated until the
        // morph handler implements actor replacement, restoration and retries.
        internal event Action<Mobj> EggMorphRequested;
        internal void RequestEggMorph(Mobj target)
        {
            EggMorphRequested?.Invoke(target);
            if (TestEnemyMorphEnabled) MorphTestEnemy(target);
        }
        internal void SpawnEggVolley()
        {
            if (State.Health <= 0) return;
            var angle = Body.Angle;
            SpawnPlayerProjectile(HereticActorType.MT_EGGFX, angle);
            SpawnPlayerProjectile(HereticActorType.MT_EGGFX, angle - Angle.Ang45 / 6);
            SpawnPlayerProjectile(HereticActorType.MT_EGGFX, angle + Angle.Ang45 / 6);
            SpawnPlayerProjectile(HereticActorType.MT_EGGFX, angle - Angle.Ang45 / 3);
            SpawnPlayerProjectile(HereticActorType.MT_EGGFX, angle + Angle.Ang45 / 3);
        }
    }
}
