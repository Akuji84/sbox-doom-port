// s&Doom modification: 2026-09-24, enable normal and ghost Nitrogolems.
// s&Doom modification: 2026-09-24, supported-monster map spawning and Golem souls.
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

// Adapted 2026-09-24: native supported-monster map spawning and A_MummySoul.
using System.Collections.Generic;
namespace ManagedDoom
{
    public sealed partial class HereticWorldSession
    {
        private bool mapCombatEnabled;
        public int BlockedMapEnemies { get; private set; }
        public IReadOnlyList<HereticClinkTestEnemy> CombatEnemies => testEnemies.AsReadOnly();
        public static bool SupportsMapEnemy(HereticActorType type) => type is
            HereticActorType.MT_CLINK or HereticActorType.MT_MUMMY or HereticActorType.MT_MUMMYGHOST or
            HereticActorType.MT_MUMMYLEADER or HereticActorType.MT_MUMMYLEADERGHOST;
        // Opt-in until the complete roster, campaign and multiplayer are implemented.
        public void StartMapCombat()
        {
            if (mapCombatEnabled) return;
            mapCombatEnabled = true;
            GoldWand ??= new HereticGoldWand(this);
            EnableCombatAmmo();
            foreach (var thing in world.Map.Things)
            {
                var decision = HereticMapSpawns.Decide(thing, skill);
                if (decision.Disposition != HereticSpawnDisposition.Unsupported || !SupportsMapEnemy(decision.Type)) continue;
                var enemy = TrySpawnSupportedEnemy(decision.Type, thing.X, thing.Y);
                if (enemy == null) { BlockedMapEnemies++; continue; }
                enemy.Body.Angle = thing.Angle;
                if (((int)thing.Flags & 8) != 0) enemy.Body.Flags |= MobjFlags.Ambush;
                enemy.Body.UpdateFrameInterpolationInfo();
                UnsupportedMapThings--;
            }
        }
        // Pinned p_enemy.c A_MummySoul: lift a non-solid soul ten units above the corpse.
        internal void SpawnGolemSoul(Mobj source)
        {
            var soul = SpawnLiquidEffect(source, HereticActorType.MT_MUMMYSOUL);
            soul.Body.Z = source.Z + Fixed.FromInt(10);
            soul.Body.MomZ = Fixed.One;
            soul.Body.UpdateFrameInterpolationInfo();
        }
    }
}
