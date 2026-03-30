//
// Copyright (C) 1993-1996 Id Software, Inc.
// Copyright (C) 2019-2020 Nobuaki Tanaka
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//

using System;
using System.Collections.Generic;

namespace ManagedDoom
{
    public sealed class LeaderboardMenu : MenuDef
    {
        private static readonly string[] LoadingLines =
        {
            "LOADING PERSONAL STATS...",
            "",
            "PLEASE WAIT"
        };

        private static readonly string[] ErrorLines =
        {
            "LEADERBOARDS UNAVAILABLE",
            "",
            "PLEASE TRY AGAIN LATER"
        };

        public LeaderboardMenu(DoomMenu menu) : base(menu)
        {
        }

        public override void Open()
        {
            SboxManagedDoomLeaderboardService.QueueRefresh();
        }

        public override bool DoEvent(DoomEvent e)
        {
            if (e.Type != EventType.KeyDown)
            {
                return true;
            }

            if (e.Key == DoomKey.Enter)
            {
                Menu.Close();
                Menu.StartSound(Sfx.PISTOL);
                return true;
            }

            return true;
        }

        public IReadOnlyList<string> Title => new[] { "PERSONAL STATS" };

        public IReadOnlyList<string> Lines
        {
            get
            {
                if (SboxManagedDoomLeaderboardService.IsLoading)
                {
                    return LoadingLines;
                }

                if (!string.IsNullOrEmpty(SboxManagedDoomLeaderboardService.Error))
                {
                    return ErrorLines;
                }

                var stats = SboxManagedDoomLeaderboardService.Stats;
                var profile = SboxManagedDoomLeaderboardService.Profile;

                if (stats == null || profile == null)
                {
                    return LoadingLines;
                }

                return new[]
                {
                    $"NAME      {ToUpper(profile.DisplayName)}",
                    $"KILLS     {stats.Kills}",
                    $"DEATHS    {stats.Deaths}",
                    $"ITEMS     {stats.Items}",
                    $"SECRETS   {stats.Secrets}",
                    $"TIME      {FormatTime(stats.TimePlayedSeconds)}",
                    $"LEVELS    {stats.LevelsCompleted}"
                };
            }
        }

        public IReadOnlyList<string> Hint => new[]
        {
            "SEE LEADERBOARDS AT",
            "LEADERBOARDS.AKUJI.ORG",
            "PRESS ENTER TO CLOSE"
        };

        private static string ToUpper(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "UNKNOWN MARINE" : value.ToUpperInvariant();
        }

        private static string FormatTime(int totalSeconds)
        {
            var span = TimeSpan.FromSeconds(totalSeconds);
            if (span.TotalHours >= 1)
            {
                return $"{(int)span.TotalHours}:{span.Minutes:00}:{span.Seconds:00}";
            }

            return $"{span.Minutes:00}:{span.Seconds:00}";
        }
    }
}
