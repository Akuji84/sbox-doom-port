// s&Doom modification: 2026-09-29, native episode exit routing and immutable completion.
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

// Routing adapted from pinned Heretic g_game.c G_DoCompleted.
// Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
namespace ManagedDoom
{
    public enum HereticExitKind { NextMap, EpisodeComplete, UnsupportedRoute }
    public readonly record struct HereticCampaignExit(int Episode, int Map, bool Secret,
        HereticExitKind Kind, int? NextMap, int ElapsedTics, int Secrets);
    public static class HereticCampaignRoutes
    {
        public static (HereticExitKind Kind, int? NextMap) Resolve(int episode, int map, bool secret)
        {
            // Bonus/custom episodes need an explicit route definition; do not index
            // the vanilla five-episode secret-return table with arbitrary WAD data.
            if (episode < 1 || episode > 5 || map < 1 || map > 9)
                return (HereticExitKind.UnsupportedRoute, null);
            if (secret) return (HereticExitKind.NextMap, 9);
            if (map == 9)
                return (HereticExitKind.NextMap, episode switch { 1 => 7, 5 => 4, _ => 5 });
            if (map == 8) return (HereticExitKind.EpisodeComplete, null);
            return (HereticExitKind.NextMap, map + 1);
        }
    }
    public sealed partial class HereticWorldSession
    {
        private readonly int campaignEpisode, campaignMap;
        public HereticCampaignExit? Completion { get; private set; }
        private void RequestExit(bool secret)
        {
            if (ExitRequested) return;
            var route = HereticCampaignRoutes.Resolve(campaignEpisode, campaignMap, secret);
            Completion = new HereticCampaignExit(campaignEpisode, campaignMap, secret,
                route.Kind, route.NextMap, tic, State.Secrets);
            ExitRequested = true;
            SecretExitRequested = secret;
            State.Message = route.Kind switch
            {
                HereticExitKind.EpisodeComplete => "Episode complete.",
                HereticExitKind.NextMap => $"Level complete. Next: E{campaignEpisode}M{route.NextMap}.",
                _ => "Level complete. No campaign route is defined for this map."
            };
        }
    }
}
