using System.Collections.Generic;

namespace ManagedDoom
{
    public static class SboxManagedDoomMultiplayerPanelService
    {
        public enum MultiplayerPanelView
        {
            Main,
            HostInfo,
            JoinInfo,
            JoinLobby,
            MatchMenu
        }

        private static int version;
        private static readonly List<string> hostedPlayerNames = new();
        private static readonly List<string> joinedPlayerNames = new();
        private static readonly List<MultiplayerLobbyListEntry> availableLobbies = new();

        public static bool IsOpen { get; private set; }

        public static MultiplayerPanelView View { get; private set; } = MultiplayerPanelView.Main;

        public static int Version => version;
        public static string HostStatus { get; private set; } = "CREATE A SESSION TO START MULTIPLAYER.";
        public static string JoinStatus { get; private set; } = "USE S&BOX TO JOIN OR CREATE A SESSION.";
        public static string JoinedLobbyName { get; private set; } = "JOINED LOBBY";
        public static string MatchStatus { get; private set; } = "SELECT A MODE";
        public static IReadOnlyList<string> HostedPlayerNames => hostedPlayerNames;
        public static IReadOnlyList<string> JoinedPlayerNames => joinedPlayerNames;
        public static IReadOnlyList<MultiplayerLobbyListEntry> AvailableLobbies => availableLobbies;

        public static void Open()
        {
            IsOpen = true;
            View = MultiplayerPanelView.Main;
            version++;
        }

        public static void Close()
        {
            IsOpen = false;
            version++;
        }

        public static void Show(MultiplayerPanelView view)
        {
            IsOpen = true;
            View = view;
            version++;
        }

        public static void Reset()
        {
            IsOpen = false;
            View = MultiplayerPanelView.Main;
            HostStatus = "CREATE A SESSION TO START MULTIPLAYER.";
            JoinStatus = "USE S&BOX TO JOIN OR CREATE A SESSION.";
            JoinedLobbyName = "JOINED LOBBY";
            MatchStatus = "SELECT A MODE";
            hostedPlayerNames.Clear();
            joinedPlayerNames.Clear();
            availableLobbies.Clear();
            version++;
        }

        public static void BeginHosting(string hostName)
        {
            IsOpen = true;
            View = MultiplayerPanelView.HostInfo;
            HostStatus = "CREATING LOBBY...";
            hostedPlayerNames.Clear();

            if (!string.IsNullOrWhiteSpace(hostName))
            {
                hostedPlayerNames.Add(hostName.Trim());
            }

            version++;
        }

        public static void SetHostStatus(string status)
        {
            HostStatus = status;
            version++;
        }

        public static void AddHostedPlayer(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName))
            {
                return;
            }

            var trimmed = playerName.Trim();
            if (hostedPlayerNames.Contains(trimmed))
            {
                return;
            }

            hostedPlayerNames.Add(trimmed);
            version++;
        }

        public static void RemoveHostedPlayer(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName))
            {
                return;
            }

            var trimmed = playerName.Trim();
            if (hostedPlayerNames.Remove(trimmed))
            {
                version++;
            }
        }

        public static void SetHostedPlayers(IEnumerable<string> playerNames)
        {
            hostedPlayerNames.Clear();

            if (playerNames is not null)
            {
                foreach (var playerName in playerNames)
                {
                    if (string.IsNullOrWhiteSpace(playerName))
                    {
                        continue;
                    }

                    var trimmed = playerName.Trim();
                    if (!hostedPlayerNames.Contains(trimmed))
                    {
                        hostedPlayerNames.Add(trimmed);
                    }
                }
            }

            version++;
        }

        public static void BeginJoinBrowser()
        {
            IsOpen = true;
            View = MultiplayerPanelView.JoinInfo;
            JoinStatus = "CHECKING SESSION STATUS...";
            availableLobbies.Clear();
            version++;
        }

        public static void SetAvailableLobbies(List<MultiplayerLobbyListEntry> lobbies)
        {
            availableLobbies.Clear();
            if (lobbies is not null)
            {
                availableLobbies.AddRange(lobbies);
            }

            JoinStatus = availableLobbies.Count > 0 ? "CONNECTED SESSION AVAILABLE." : "USE S&BOX TO JOIN OR CREATE A SESSION.";
            version++;
        }

        public static void SetJoinStatus(string status)
        {
            JoinStatus = string.IsNullOrWhiteSpace(status) ? "USE S&BOX TO JOIN OR CREATE A SESSION." : status.Trim();
            version++;
        }

        public static void ShowJoinedLobby(string lobbyName, IEnumerable<string> playerNames)
        {
            IsOpen = true;
            View = MultiplayerPanelView.JoinLobby;
            JoinedLobbyName = string.IsNullOrWhiteSpace(lobbyName) ? "JOINED LOBBY" : lobbyName.Trim();
            SetJoinedPlayers(playerNames);
        }

        public static void SetJoinedPlayers(IEnumerable<string> playerNames)
        {
            joinedPlayerNames.Clear();

            if (playerNames is not null)
            {
                foreach (var playerName in playerNames)
                {
                    if (string.IsNullOrWhiteSpace(playerName))
                    {
                        continue;
                    }

                    var trimmed = playerName.Trim();
                    if (!joinedPlayerNames.Contains(trimmed))
                    {
                        joinedPlayerNames.Add(trimmed);
                    }
                }
            }

            version++;
        }

        public static void ShowMatchMenu(string status = null)
        {
            IsOpen = true;
            View = MultiplayerPanelView.MatchMenu;

            if (!string.IsNullOrWhiteSpace(status))
            {
                MatchStatus = status.Trim();
            }

            version++;
        }

        public static void SetMatchStatus(string status)
        {
            MatchStatus = string.IsNullOrWhiteSpace(status) ? "SELECT A MODE" : status.Trim();
            version++;
        }
    }

    public sealed class MultiplayerLobbyListEntry
    {
        public ulong Id { get; set; }
        public string Address { get; set; }
        public string Name { get; set; }
        public List<string> PlayerNames { get; set; } = new();
        public int MemberCount { get; set; }
        public int MaxPlayers { get; set; }
        public bool IsFull { get; set; }
        public bool PvpStarted { get; set; }
    }
}
