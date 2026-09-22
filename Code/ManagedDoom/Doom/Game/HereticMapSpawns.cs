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

// Adapted 2026-09-18: map spawn filtering from p_mobj.c.
// Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
using System;
using System.Collections.Generic;
namespace ManagedDoom
{
    public enum HereticSpawnDisposition { Spawn, Filtered, Marker, Unsupported, Unknown }
    public readonly record struct HereticSpawnDecision(HereticSpawnDisposition Disposition, HereticActorType Type);
    public static class HereticMapSpawns
    {
        private static readonly Dictionary<int, HereticActorType> byNumber = BuildIndex();
        private static Dictionary<int, HereticActorType> BuildIndex()
        {
            var result = new Dictionary<int, HereticActorType>();
            for (var i = 0; i < HereticDefinitions.Actors.Count; i++)
                if (HereticDefinitions.Actors[i].MapNumber >= 0) result.Add(HereticDefinitions.Actors[i].MapNumber, (HereticActorType)i);
            return result;
        }
        public static HereticKeys KeyFor(HereticActorType type) => type switch
        {
            HereticActorType.MT_AKYY => HereticKeys.Green,
            HereticActorType.MT_BKYY => HereticKeys.Blue,
            HereticActorType.MT_CKEY => HereticKeys.Yellow,
            _ => HereticKeys.None
        };
        public static HereticSpawnDecision Decide(MapThing thing, GameSkill skill = GameSkill.Medium, bool network = false, bool deathmatch = false, bool noMonsters = false)
        {
            if ((uint)skill > (uint)GameSkill.Nightmare) throw new ArgumentOutOfRangeException(nameof(skill));
            if (thing == null) throw new ArgumentNullException(nameof(thing));
            if ((thing.Type >= 1 && thing.Type <= 4) || thing.Type == 11 || thing.Type == 56 || (thing.Type >= 1200 && thing.Type < 1300))
                return new(HereticSpawnDisposition.Marker, default);
            var bit = skill <= GameSkill.Easy ? 1 : skill == GameSkill.Medium ? 2 : 4;
            if ((!network && ((int)thing.Flags & 16) != 0) || ((int)thing.Flags & bit) == 0)
                return new(HereticSpawnDisposition.Filtered, default);
            if (!byNumber.TryGetValue(thing.Type, out var type)) return new(HereticSpawnDisposition.Unknown, default);
            var def = HereticDefinitions.Actors[(int)type];
            if ((deathmatch && (def.Flags & HereticActorFlags.MF_NOTDMATCH) != 0) ||
                (noMonsters && (def.Flags & HereticActorFlags.MF_COUNTKILL) != 0))
                return new(HereticSpawnDisposition.Filtered, type);
            return new(Supports(type) ? HereticSpawnDisposition.Spawn : HereticSpawnDisposition.Unsupported, type);
        }
        public static bool Supports(HereticActorType type)
        {
            if ((uint)type >= HereticDefinitions.Actors.Count) return false;
            if (KeyFor(type) != HereticKeys.None) return true;
            var def = HereticDefinitions.Actors[(int)type];
            const HereticActorFlags allowed = HereticActorFlags.MF_SOLID | HereticActorFlags.MF_NOGRAVITY |
                HereticActorFlags.MF_SPAWNCEILING | HereticActorFlags.MF_NOBLOCKMAP | HereticActorFlags.MF_NOSECTOR;
            if ((def.Flags & ~allowed) != 0 || def.Flags2 != 0 || def.MapNumber < 0) return false;
            // Inspect the entire animation loop: a quiet first frame is not sufficient.
            var visited = new HashSet<HereticStateId>();
            var state = def.SpawnState;
            while (state != HereticStateId.S_NULL && visited.Add(state))
            {
                var frame = HereticDefinitions.States[(int)state];
                if (frame.Action != HereticAction.None) return false;
                if (frame.Tics == -1) return true;
                if (frame.Tics <= 0) return false;
                state = frame.Next;
            }
            return state != HereticStateId.S_NULL;
        }
    }
    public sealed class HereticMapActor
    {
        // Adapted 2026-09-22: exact fixed-point item bob offsets from pinned p_mobj.c.
        private static readonly int[] floatBobOffsets = { 0, 51389, 102283, 152192,
    200636, 247147, 291278, 332604,
    370727, 405280, 435929, 462380,
    484378, 501712, 514213, 521763,
    524287, 521763, 514213, 501712,
    484378, 462380, 435929, 405280,
    370727, 332604, 291278, 247147,
    200636, 152192, 102283, 51389,
    -1, -51390, -102284, -152193,
    -200637, -247148, -291279, -332605,
    -370728, -405281, -435930, -462381,
    -484380, -501713, -514215, -521764,
    -524288, -521764, -514214, -501713,
    -484379, -462381, -435930, -405280,
    -370728, -332605, -291279, -247148,
    -200637, -152193, -102284, -51389 };
        public HereticActorType Type { get; }
        public Mobj Body { get; }
        public HereticActorState Animation { get; }
        public HereticKeys Key => HereticMapSpawns.KeyFor(Type);
        internal HereticMapActor(HereticActorType type, Mobj body, HereticActorState animation)
        { Type = type; Body = body; Animation = animation; }
        internal void Tick()
        {
            if ((Body.Flags & MobjFlags.Dropped) == 0) Body.UpdateFrameInterpolationInfo();
            if (Type == HereticActorType.MT_MISC0 || Type == HereticActorType.MT_ITEMSHIELD1 || Type == HereticActorType.MT_ITEMSHIELD2 || Type == HereticActorType.MT_MISC3 || Type == HereticActorType.MT_ARTISUPERHEAL || Type == HereticActorType.MT_ARTIFLY || Type == HereticActorType.MT_ARTIINVULNERABILITY || Type == HereticActorType.MT_MISC4 || Type == HereticActorType.MT_ARTITELEPORT || Type == HereticActorType.MT_MISC5 || Type == HereticActorType.MT_ARTIINVISIBILITY)
            {
                Body.FloorZ = Body.Subsector.Sector.FloorHeight;
                Body.Z = Body.FloorZ + new Fixed(floatBobOffsets[Body.Health++ & 63]);
            }
            Animation.Tick();
            Body.Sprite = (Sprite)Animation.Definition.Sprite;
            Body.Frame = Animation.Definition.Frame;
        }
    }
}
