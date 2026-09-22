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
        // Adapted 2026-09-22: P_AutoUseHealth, single-player Baby difficulty.
        // Correct the reference mixed-inventory overdraw/wrong-slot bug: consume
        // only owned quantities, and remove urns from their own inventory count.
        private void AutoUseHealingArtifacts(int damage)
        {
            if (skill != GameSkill.Baby || State.Health <= 0 || damage < State.Health) return;
            var needed = (long)damage - State.Health + 1;
            var flaskHealing = State.QuartzFlasks * 25;
            var urnHealing = State.MysticUrns * 100;
            int flasks = 0, urns = 0;
            if (flaskHealing >= needed) flasks = (int)((needed + 24) / 25);
            else if (urnHealing >= needed) urns = (int)((needed + 99) / 100);
            else if (flaskHealing + urnHealing >= needed)
            {
                flasks = State.QuartzFlasks;
                urns = (int)((needed - flaskHealing + 99) / 100);
            }
            else return; // Keep inventory unchanged if it cannot prevent death.
            State.QuartzFlasks -= flasks; State.MysticUrns -= urns;
            State.Health += flasks * 25 + urns * 100;
            Body.Health = State.Health;
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
