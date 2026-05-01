using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.Services;

namespace ManagedDoom
{
    public sealed class SboxManagedDoomAnalyticsService : IAnalyticsListener
    {
        private const string AnalyticsEndpoint = "https://bugs.akuji.org/api/analytics/event";
        private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(30);

        private readonly DateTime _sessionStartUtc = DateTime.UtcNow;
        private readonly object _leaderboardSync = new();
        private readonly Dictionary<string, int> _sessionBestTimes = new(StringComparer.OrdinalIgnoreCase);
        private int _sessionLevelsCompleted;
        private DateTime _lastHeartbeatUtc = DateTime.MinValue;

        public void OnNewGame(int episode, int map, GameSkill skill)
        {
            _ = SendEventAsync("new_game", new
            {
                episode,
                map,
                skill = skill.ToString()
            });
        }

        public void OnSaveGame(int slotNumber)
        {
            _ = SendEventAsync("save_game", new { slotNumber });
        }

        public void OnLoadGame(int slotNumber)
        {
            _ = SendEventAsync("load_game", new { slotNumber });
        }

        public void OnSessionEnd()
        {
            var duration = DateTime.UtcNow - _sessionStartUtc;
            _ = SendEventAsync("session_end", new
            {
                playedSeconds = (int)duration.TotalSeconds
            });
        }

        public void OnSessionStart(string map, string currentState)
        {
            _ = SendEventAsync("session_start", new
            {
                map = map?.Trim() ?? string.Empty,
                currentState = currentState?.Trim() ?? string.Empty
            });

            _lastHeartbeatUtc = DateTime.UtcNow;
        }

        public void PumpSessionHeartbeat(string map, string currentState)
        {
            var nowUtc = DateTime.UtcNow;
            if (_lastHeartbeatUtc != DateTime.MinValue
                && nowUtc - _lastHeartbeatUtc < HeartbeatInterval)
            {
                return;
            }

            _lastHeartbeatUtc = nowUtc;
            _ = SendEventAsync("session_heartbeat", new
            {
                map = map?.Trim() ?? string.Empty,
                currentState = currentState?.Trim() ?? string.Empty
            });
        }

        public void OnHostedLobbyStarted(string lobbyName)
        {
            _ = SendEventAsync("pvp_host_started", new
            {
                lobbyName = lobbyName?.Trim() ?? string.Empty
            });
        }

        public void OnJoinedHostedLobby(string lobbyName, string hostName)
        {
            _ = SendEventAsync("pvp_player_joined", new
            {
                lobbyName = lobbyName?.Trim() ?? string.Empty,
                hostName = hostName?.Trim() ?? string.Empty
            });
        }

        public void OnPvpMatchWon(string winnerName, int winnerKills, string loserName, int loserKills, string mapName)
        {
            _ = SendEventAsync("pvp_match_won", new
            {
                winnerName = winnerName?.Trim() ?? string.Empty,
                winnerKills = Math.Max(0, winnerKills),
                loserName = loserName?.Trim() ?? string.Empty,
                loserKills = Math.Max(0, loserKills),
                map = mapName?.Trim() ?? string.Empty
            });
        }

        public void OnLevelCompleted(int episode, int map, int levelTimeTics)
        {
            var mapKey = BuildMapKey(episode, map);
            var levelSeconds = Math.Max(0, levelTimeTics / GameConst.TicRate);

            lock (_leaderboardSync)
            {
                _sessionLevelsCompleted++;

                if (!_sessionBestTimes.TryGetValue(mapKey, out var existing) || levelSeconds < existing)
                {
                    _sessionBestTimes[mapKey] = levelSeconds;
                }
            }

            _ = SendEventAsync("level_completed", new
            {
                episode,
                map,
                mapKey,
                levelSeconds
            });
        }

        public int GetCompletedLevelsTotal()
        {
            lock (_leaderboardSync)
            {
                return _sessionLevelsCompleted;
            }
        }

        public IReadOnlyDictionary<string, int> GetBestTimesSnapshot()
        {
            lock (_leaderboardSync)
            {
                return new Dictionary<string, int>(_sessionBestTimes, StringComparer.OrdinalIgnoreCase);
            }
        }

        private static async Task SendEventAsync(string eventType, object details)
        {
            try
            {
                var token = await Auth.GetToken("sbox-doom-bug-server", CancellationToken.None);

                if (string.IsNullOrEmpty(token))
                {
                    Log.Warning("[ManagedDoomHost] Failed to obtain Steam auth token for analytics");
                    return;
                }

                var payload = new
                {
                    type = eventType,
                    details,
                    gameVersion = "sbox-doom-port",
                    build = "public"
                };

                var content = Http.CreateJsonContent(payload);
                var headers = new Dictionary<string, string>
                {
                    ["Authorization"] = $"Steam {token}",
                    ["X-Steam-Id"] = Game.SteamId.ToString()
                };

                await Http.RequestAsync(
                    AnalyticsEndpoint,
                    "POST",
                    content,
                    headers);
            }
            catch (Exception ex)
            {
                Log.Warning($"[ManagedDoomHost] Analytics event '{eventType}' failed: {ex}");
            }
        }

        private static string BuildMapKey(int episode, int map)
        {
            if (episode <= 0)
            {
                return $"map{map:00}";
            }

            return $"e{episode}m{map}";
        }
    }
}
