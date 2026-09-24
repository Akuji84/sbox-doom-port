// s&Doom modification: 2026-09-24, guard failed Sorcerer map placements.
// s&Doom modification: 2026-09-24, Sorcerer damage reactions and E3M8 completion.
// s&Doom modification: 2026-09-24, native supported-boss rules and episode death triggers.
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
// Adapted 2026-09-24: pinned p_enemy.c A_BossDeath/P_Massacre and p_inter.c boss flags.
namespace ManagedDoom
{
    public sealed partial class HereticWorldSession
    {
        internal bool IsBoss(Mobj body)
        {
            foreach(var enemy in testEnemies)
                if(enemy.Body==body)return (enemy.Combatant.Flags2 & HereticActorFlags2.MF2_BOSS)!=0;
            return false;
        }
        internal bool IsChargingBoss(Mobj body) => IsBoss(body) && (body.Flags & MobjFlags.SkullFly)!=0;
        internal bool EpisodeBossTriggered { get; private set; }
        internal void EpisodeBossDeath(HereticClinkTestEnemy source)
        {
            HereticActorType? type = world.Options.Episode switch
            {
                1 or 4 => HereticActorType.MT_HEAD,
                2 or 5 => HereticActorType.MT_MINOTAUR,
                3 => HereticActorType.MT_SORCERER2,
                _ => null
            };
            if (source.Combatant.Type != type || source.Body.Health > 0 || EpisodeBossTriggered || world.Options.Map != 8) return;
            if ((type == HereticActorType.MT_HEAD && blockedIronLiches != 0) || (type == HereticActorType.MT_MINOTAUR && blockedMaulotaurs != 0) || (type == HereticActorType.MT_SORCERER2 && blockedSorcerers != 0)) return;
            foreach (var enemy in testEnemies)
                if (enemy.Combatant.Type == type && enemy.Body.Health > 0) return;
            EpisodeBossTriggered = true;
            if (world.Options.Episode > 1)
                foreach (var enemy in testEnemies)
                    if (enemy.Body.Health > 0) DamageTestEnemy(enemy.Body,10000,environment:true);
            var template = world.Map.Lines[0];
            var trigger = new LineDef(template.Vertex1,template.Vertex2,0,0,666,template.FrontSide,template.BackSide);
            world.SectorAction.DoFloor(trigger,FloorMoveType.LowerFloor);
        }
    }
}
