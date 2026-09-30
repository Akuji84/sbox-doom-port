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

// s&Doom modification: 2026-09-29, track aliases adapted from Heretic sounds.c.
// Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
namespace ManagedDoom
{
    // Track aliases from pinned Heretic sounds.c; independent of Doom's D_ names.
    public static class HereticMusic
    {
        private static readonly string[] maps = { "MUS_E1M1", "MUS_E1M2", "MUS_E1M3", "MUS_E1M4", "MUS_E1M5", "MUS_E1M6", "MUS_E1M7", "MUS_E1M8", "MUS_E1M9", "MUS_E2M1", "MUS_E2M2", "MUS_E2M3", "MUS_E2M4", "MUS_E1M4", "MUS_E2M6", "MUS_E2M7", "MUS_E2M8", "MUS_E2M9", "MUS_E1M1", "MUS_E3M2", "MUS_E3M3", "MUS_E1M6", "MUS_E1M3", "MUS_E1M2", "MUS_E1M5", "MUS_E1M9", "MUS_E2M6", "MUS_E1M6", "MUS_E1M2", "MUS_E1M3", "MUS_E1M4", "MUS_E1M5", "MUS_E1M1", "MUS_E1M7", "MUS_E1M8", "MUS_E1M9", "MUS_E2M1", "MUS_E2M2", "MUS_E2M3", "MUS_E2M4", "MUS_E1M4", "MUS_E2M6", "MUS_E2M7", "MUS_E2M8", "MUS_E2M9", "MUS_E3M2", "MUS_E3M3", "MUS_E1M6" };
        public static string MapTrack(int episode, int map)
        {
            if (episode < 1 || episode > 6 || map < 1 || map > (episode == 6 ? 3 : 9)) return null;
            return maps[(episode-1)*9+map-1];
        }
        public static string TrackFor(HereticWorldSession session) => session.Completion is HereticCampaignExit exit
            ? exit.Kind == HereticExitKind.EpisodeComplete ? "MUS_CPTD" : "MUS_INTR"
            : MapTrack(session.World.Options.Episode, session.World.Options.Map);
    }
}
