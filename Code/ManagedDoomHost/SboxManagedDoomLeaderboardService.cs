using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.Services;

namespace ManagedDoom
{
    public static class SboxManagedDoomLeaderboardService
    {
        private const string BaseUrl = "https://leaderboards.akuji.org";
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
        private static readonly object Sync = new();

        private static bool pendingRefresh;
        private static bool inFlight;
        private static PersonalStatsResponse latest = new();
        private static string error;

        public static void QueueRefresh()
        {
            lock (Sync)
            {
                pendingRefresh = true;
                error = null;
            }
        }

        public static bool TryBeginRefresh()
        {
            lock (Sync)
            {
                if (inFlight || !pendingRefresh)
                {
                    return false;
                }

                pendingRefresh = false;
                inFlight = true;
                error = null;
                return true;
            }
        }

        public static async Task RefreshAsync()
        {
            try
            {
                var personal = await GetPersonalStatsAsync();
                lock (Sync)
                {
                    latest = personal ?? new PersonalStatsResponse();
                    error = null;
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[ManagedDoomHost] Leaderboard refresh failed: {ex}");
                lock (Sync)
                {
                    error = "leaderboards unavailable right now";
                }
            }
            finally
            {
                lock (Sync)
                {
                    inFlight = false;
                }
            }
        }

        private static async Task<PersonalStatsResponse> GetPersonalStatsAsync()
        {
            var json = await SendRequestAsync("/api/player/me", "GET");

            return JsonSerializer.Deserialize<PersonalStatsResponse>(json, JsonOptions) ?? new PersonalStatsResponse();
        }

        public static async Task SyncProfileAsync(string displayName)
        {
            var payload = new
            {
                displayName = string.IsNullOrWhiteSpace(displayName) ? "Steam " + Game.SteamId : displayName.Trim()
            };

            await SendJsonAsync("/api/player/sync", payload);
        }

        public static async Task SubmitStatsAsync(StatsUpdateRequest request)
        {
            await SendJsonAsync("/api/stats/update", request);
        }

        private static async Task SendJsonAsync(string relativeUrl, object payload)
        {
            var content = Http.CreateJsonContent(payload);
            await SendRequestAsync(relativeUrl, "POST", content);
        }

        private static async Task<string> SendRequestAsync(string relativeUrl, string method, HttpContent content = null)
        {
            var token = await Auth.GetToken("sbox-doom-bug-server", CancellationToken.None);
            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException("steam auth unavailable");
            }

            var headers = new Dictionary<string, string>
            {
                ["Authorization"] = $"Steam {token}",
                ["X-Steam-Id"] = Game.SteamId.ToString()
            };

            return await Http.RequestStringAsync(
                $"{BaseUrl}{relativeUrl}",
                method,
                content,
                headers,
                CancellationToken.None);
        }

        public static bool IsLoading
        {
            get
            {
                lock (Sync)
                {
                    return inFlight;
                }
            }
        }

        public static string Error
        {
            get
            {
                lock (Sync)
                {
                    return error;
                }
            }
        }

        public static PlayerProfile Profile
        {
            get
            {
                lock (Sync)
                {
                    return latest.Profile;
                }
            }
        }

        public static PlayerStats Stats
        {
            get
            {
                lock (Sync)
                {
                    return latest.Stats;
                }
            }
        }
    }

    public sealed class PersonalStatsResponse
    {
        public bool Ok { get; set; }
        public PlayerProfile Profile { get; set; } = new();
        public PlayerStats Stats { get; set; } = new();
    }

    public sealed class PlayerProfile
    {
        public string SteamId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    public sealed class PlayerStats
    {
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Items { get; set; }
        public int Secrets { get; set; }
        public int TimePlayedSeconds { get; set; }
        public int LevelsCompleted { get; set; }
    }

    public sealed class StatsUpdateRequest
    {
        public int Kills { get; set; }
        public int KillsDelta { get; set; }
        public int Deaths { get; set; }
        public int DeathsDelta { get; set; }
        public int Items { get; set; }
        public int ItemsDelta { get; set; }
        public int Secrets { get; set; }
        public int SecretsDelta { get; set; }
        public int TimePlayedSeconds { get; set; }
        public int TimePlayedSecondsDelta { get; set; }
        public int LevelsCompleted { get; set; }
        public int LevelsCompletedDelta { get; set; }
        public Dictionary<string, int> BestTimes { get; set; } = new();
    }

}
