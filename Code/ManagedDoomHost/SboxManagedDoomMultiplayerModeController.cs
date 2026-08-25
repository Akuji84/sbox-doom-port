using System;
using Sandbox;

namespace ManagedDoom
{
    public enum SboxManagedDoomMultiplayerModeKind
    {
        None = 0,
        Pvp = 1,
        Coop = 2
    }

    public static class SboxManagedDoomMultiplayerModeCatalog
    {
        public const string DefaultPvpMapName = "E1M1";
        public const string DefaultCoopMapName = "E1M1";
        public const string DefaultCoopCommercialMapName = "MAP01";
        public const string SelectModeStatus = "SELECT A MODE";
        public const string CoopLabel = "CO-OP";
    }

    public readonly struct SboxManagedDoomMultiplayerPvpLaunchPlan
    {
        public SboxManagedDoomMultiplayerPvpLaunchPlan( string mapName, int episode, int map, int consolePlayer )
        {
            MapName = mapName;
            Episode = episode;
            Map = map;
            ConsolePlayer = consolePlayer;
        }

        public string MapName { get; }
        public int Episode { get; }
        public int Map { get; }
        public int ConsolePlayer { get; }
    }

    public readonly struct SboxManagedDoomMultiplayerCoopLaunchPlan
    {
        public SboxManagedDoomMultiplayerCoopLaunchPlan( string mapName, int episode, int map, int skill, int consolePlayer )
        {
            MapName = mapName;
            Episode = episode;
            Map = map;
            Skill = skill;
            ConsolePlayer = consolePlayer;
        }

        public string MapName { get; }
        public int Episode { get; }
        public int Map { get; }
        public int Skill { get; }
        public int ConsolePlayer { get; }
    }

    public sealed class SboxManagedDoomMultiplayerModeController
    {
        private bool multiplayerOverridesApplied;
        private bool savedPrePvpNetGame;
        private int savedPrePvpDeathmatch;
        private bool savedPrePvpNoMonsters;
        private bool savedPrePvpFastMonsters;
        private bool savedPrePvpRespawnMonsters;
        private int savedPrePvpConsolePlayer;
        private readonly bool[] savedPrePvpPlayersInGame = new bool[Player.MaxPlayerCount];

        public SboxManagedDoomMultiplayerModeKind ActiveMode { get; private set; }
        public bool PvpMatchLoaded { get; private set; }
        public bool PvpLaunchPending { get; private set; }
        public string ActivePvpMap { get; private set; } = string.Empty;
        public int HandledPvpLaunchSerial { get; private set; }
        public bool CoopMatchLoaded { get; private set; }
        public bool CoopLaunchPending { get; private set; }
        public string ActiveCoopMap { get; private set; } = string.Empty;
        public int HandledCoopLaunchSerial { get; private set; }

        public void ShowModeMenu()
        {
            ActiveMode = SboxManagedDoomMultiplayerModeKind.None;
        }

        public void SelectPvpMode()
        {
            ActiveMode = SboxManagedDoomMultiplayerModeKind.Pvp;
        }

        public void SelectCoopMode()
        {
            ActiveMode = SboxManagedDoomMultiplayerModeKind.Coop;
        }

        public bool TryCreatePvpLaunchPlan(
            Doom doom,
            global::Sandbox.SboxManagedDoomInput input,
            SboxManagedDoomMultiplayerSessionComponent session,
            bool isHost,
            out SboxManagedDoomMultiplayerPvpLaunchPlan launchPlan )
        {
            launchPlan = default;

            if ( doom is null || input is null || session?.PvpActive != true )
            {
                return false;
            }

            var mapName = string.IsNullOrWhiteSpace( session.PvpMap )
                ? SboxManagedDoomMultiplayerModeCatalog.DefaultPvpMapName
                : session.PvpMap.Trim().ToUpperInvariant();
            var sameMap = string.Equals( ActivePvpMap, mapName, StringComparison.OrdinalIgnoreCase );
            var fullyInLoadedPvpMap =
                PvpMatchLoaded &&
                sameMap &&
                doom.State == DoomState.Game &&
                doom.Game is not null &&
                doom.Game.State == GameState.Level &&
                doom.Game.World is not null;

            if ( fullyInLoadedPvpMap || (PvpLaunchPending && sameMap) )
            {
                return false;
            }

            if ( !TryParseDoomMapName( mapName, out var episode, out var map ) )
            {
                episode = 1;
                map = 1;
                mapName = SboxManagedDoomMultiplayerModeCatalog.DefaultPvpMapName;
            }

            launchPlan = new SboxManagedDoomMultiplayerPvpLaunchPlan(
                mapName,
                episode,
                map,
                isHost ? 0 : 1 );
            return true;
        }

        public bool TryCreateCoopLaunchPlan(
            Doom doom,
            global::Sandbox.SboxManagedDoomInput input,
            SboxManagedDoomMultiplayerSessionComponent session,
            bool isHost,
            out SboxManagedDoomMultiplayerCoopLaunchPlan launchPlan )
        {
            launchPlan = default;

            if ( doom is null || input is null || session?.CoopActive != true )
            {
                return false;
            }

            var mapName = string.IsNullOrWhiteSpace( session.CoopMap )
                ? SboxManagedDoomMultiplayerModeCatalog.DefaultCoopMapName
                : session.CoopMap.Trim().ToUpperInvariant();
            var sameMap = string.Equals( ActiveCoopMap, mapName, StringComparison.OrdinalIgnoreCase );

            // Once a co-op match is loaded it advances levels on its own, so
            // never relaunch it (even mid-intermission when GameState != Level).
            if ( (CoopMatchLoaded || CoopLaunchPending) && sameMap )
            {
                return false;
            }

            if ( !TryParseDoomMapName( mapName, out var episode, out var map ) )
            {
                episode = 1;
                map = 1;
                mapName = SboxManagedDoomMultiplayerModeCatalog.DefaultCoopMapName;
            }

            launchPlan = new SboxManagedDoomMultiplayerCoopLaunchPlan(
                mapName,
                episode,
                map,
                session.CoopSkill,
                isHost ? 0 : 1 );
            return true;
        }

        public void BeginCoopLaunch( SboxManagedDoomMultiplayerSessionComponent session, string mapName )
        {
            ActiveMode = SboxManagedDoomMultiplayerModeKind.Coop;
            HandledCoopLaunchSerial = session?.CoopLaunchSerial ?? HandledCoopLaunchSerial;
            ActiveCoopMap = mapName ?? string.Empty;
            CoopLaunchPending = true;
            CoopMatchLoaded = false;
        }

        public void UpdateCoopLoadedState( Doom doom, SboxManagedDoomMultiplayerSessionComponent session )
        {
            if ( session?.CoopActive != true )
            {
                return;
            }

            var isFullyLoaded =
                doom is not null &&
                doom.State == DoomState.Game &&
                doom.Game is not null &&
                doom.Game.State == GameState.Level &&
                doom.Game.World is not null;

            if ( !isFullyLoaded )
            {
                return;
            }

            ActiveMode = SboxManagedDoomMultiplayerModeKind.Coop;
            CoopMatchLoaded = true;
            CoopLaunchPending = false;
        }

        public void BeginPvpLaunch( SboxManagedDoomMultiplayerSessionComponent session, string mapName )
        {
            ActiveMode = SboxManagedDoomMultiplayerModeKind.Pvp;
            HandledPvpLaunchSerial = session?.PvpLaunchSerial ?? HandledPvpLaunchSerial;
            ActivePvpMap = mapName ?? string.Empty;
            PvpLaunchPending = true;
            PvpMatchLoaded = false;
        }

        public void UpdatePvpLoadedState( Doom doom, SboxManagedDoomMultiplayerSessionComponent session, Action onLoaded )
        {
            if ( session?.PvpActive != true )
            {
                return;
            }

            var isFullyLoaded =
                doom is not null &&
                doom.State == DoomState.Game &&
                doom.Game is not null &&
                doom.Game.State == GameState.Level &&
                doom.Game.World is not null;

            if ( !isFullyLoaded )
            {
                return;
            }

            onLoaded?.Invoke();
            ActiveMode = SboxManagedDoomMultiplayerModeKind.Pvp;
            PvpMatchLoaded = true;
            PvpLaunchPending = false;
            ActivePvpMap = string.IsNullOrWhiteSpace( session.PvpMap )
                ? SboxManagedDoomMultiplayerModeCatalog.DefaultPvpMapName
                : session.PvpMap.Trim().ToUpperInvariant();
        }

        public void ResetRuntimeState()
        {
            ActiveMode = SboxManagedDoomMultiplayerModeKind.None;
            PvpMatchLoaded = false;
            PvpLaunchPending = false;
            ActivePvpMap = string.Empty;
            CoopMatchLoaded = false;
            CoopLaunchPending = false;
            ActiveCoopMap = string.Empty;
        }

        public void ApplyPvpGameplayOverrides( GameOptions options )
        {
            if ( options is null || multiplayerOverridesApplied )
            {
                return;
            }

            savedPrePvpNetGame = options.NetGame;
            savedPrePvpDeathmatch = options.Deathmatch;
            savedPrePvpNoMonsters = options.NoMonsters;
            savedPrePvpFastMonsters = options.FastMonsters;
            savedPrePvpRespawnMonsters = options.RespawnMonsters;
            savedPrePvpConsolePlayer = options.ConsolePlayer;

            for ( var i = 0; i < Player.MaxPlayerCount; i++ )
            {
                savedPrePvpPlayersInGame[i] = options.Players[i].InGame;
            }

            multiplayerOverridesApplied = true;
        }

        public void RestoreGameplayOverrides( Doom doom )
        {
            if ( doom is not null )
            {
                doom.ExternalTicCmdBuilder = null;

                if ( multiplayerOverridesApplied )
                {
                    var options = doom.Options;
                    options.NetGame = savedPrePvpNetGame;
                    options.Deathmatch = savedPrePvpDeathmatch;
                    options.NoMonsters = savedPrePvpNoMonsters;
                    options.FastMonsters = savedPrePvpFastMonsters;
                    options.RespawnMonsters = savedPrePvpRespawnMonsters;
                    options.ConsolePlayer = savedPrePvpConsolePlayer;

                    for ( var i = 0; i < Player.MaxPlayerCount; i++ )
                    {
                        options.Players[i].InGame = savedPrePvpPlayersInGame[i];
                    }
                }
            }

            multiplayerOverridesApplied = false;
        }

        private static bool TryParseDoomMapName( string mapName, out int episode, out int map )
        {
            episode = 1;
            map = 1;

            if ( string.IsNullOrWhiteSpace( mapName ) )
            {
                return false;
            }

            var normalized = mapName.Trim().ToUpperInvariant();
            if ( normalized.Length == 4 && normalized[0] == 'E' && normalized[2] == 'M'
                && char.IsDigit( normalized[1] ) && char.IsDigit( normalized[3] ) )
            {
                episode = normalized[1] - '0';
                map = normalized[3] - '0';
                return true;
            }

            if ( normalized.Length == 5 && normalized.StartsWith( "MAP", StringComparison.Ordinal )
                && int.TryParse( normalized[3..], out var commercialMap ) )
            {
                episode = 1;
                map = commercialMap;
                return true;
            }

            return false;
        }
    }
}
