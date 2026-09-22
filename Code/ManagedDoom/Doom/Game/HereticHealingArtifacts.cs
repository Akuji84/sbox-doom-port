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

// Adapted 2026-09-22: healing artifacts from pinned p_inter.c/p_user.c.
// Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
using System;
namespace ManagedDoom
{
    public sealed partial class HereticWorldSession
    {
        // Adapted 2026-09-22: P_GiveArtifact/P_UseArtifact, healing subset only.
        private static bool HealingArtifactPickup(HereticActorType type) => type == HereticActorType.MT_MISC3 || type == HereticActorType.MT_ARTISUPERHEAL;
        internal bool GiveHealingArtifact(HereticHealingArtifact type)
        {
            if ((uint)type > (uint)HereticHealingArtifact.MysticUrn) throw new ArgumentOutOfRangeException(nameof(type));
            if (State.Health <= 0) return false;
            if (type == HereticHealingArtifact.QuartzFlask)
            { if (State.QuartzFlasks >= 16) return false; State.QuartzFlasks++; }
            else { if (State.MysticUrns >= 16) return false; State.MysticUrns++; }
            return true;
        }
        internal bool UseHealingArtifact(HereticHealingArtifact type)
        {
            if ((uint)type > (uint)HereticHealingArtifact.MysticUrn) throw new ArgumentOutOfRangeException(nameof(type));
            var flask = type == HereticHealingArtifact.QuartzFlask;
            if ((flask ? State.QuartzFlasks : State.MysticUrns) <= 0 || !GiveHealth(flask ? 25 : 100)) return false;
            if (flask) State.QuartzFlasks--; else State.MysticUrns--;
            State.Message = flask ? "Used Quartz Flask" : "Used Mystic Urn";
            RequestSound(HereticSoundId.sfx_artiuse, Body);
            return true;
        }
    }
}
