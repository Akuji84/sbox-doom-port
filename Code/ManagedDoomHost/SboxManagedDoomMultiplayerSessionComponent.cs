using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ManagedDoom
{
    public sealed class SboxManagedDoomMultiplayerSessionComponent : Sandbox.Component
    {
        public enum MultiplayerCombatEventType
        {
            None = 0,
            Damage = 1,
            Death = 2,
            Respawn = 3
        }

        public enum MultiplayerWeaponEventType
        {
            None = 0,
            PickupWeapon = 1,
            PickupAmmo = 2,
            SwitchWeapon = 3,
            RespawnLoadout = 4
        }

        public enum MultiplayerEffectEventType
        {
            None = 0,
            PlayLocalSfx = 1,
            BarrelExploded = 2
        }

        public static SboxManagedDoomMultiplayerSessionComponent Current { get; private set; }

        [Sync]
        public bool MatchStarted { get; set; }

        [Sync]
        public string LobbyName { get; set; } = "DOOM PORT";

        [Sync]
        public string InactivePlayers { get; set; } = string.Empty;

        [Sync]
        public string HostWadPath { get; set; } = string.Empty;

        [Sync]
        public bool PvpActive { get; set; }

        [Sync]
        public string PvpMap { get; set; } = string.Empty;

        [Sync]
        public int PvpLaunchSerial { get; set; }

        [Sync]
        public string RemovedPickupThingIndexes { get; set; } = string.Empty;

        [Sync]
        public string DestroyedWorldThingIndexes { get; set; } = string.Empty;

        [Sync]
        public string PvpSectorState { get; set; } = string.Empty;

        [Sync]
        public string PvpLineState { get; set; } = string.Empty;

        [Sync]
        public string PvpProjectileState { get; set; } = string.Empty;

        [Sync]
        public bool PvpRoundEnded { get; set; }

        [Sync]
        public int PvpWinnerSerial { get; set; }

        [Sync]
        public string PvpWinnerName { get; set; } = string.Empty;

        [Sync]
        public int HitscanEventSerial { get; set; }

        [Sync]
        public string HitscanEventQueue { get; set; } = string.Empty;

        [Sync]
        public int EffectEventSerial { get; set; }

        [Sync]
        public string EffectEventQueue { get; set; } = string.Empty;

        [Sync]
        public int Player0WeaponEventSerial { get; set; }

        [Sync]
        public string Player0WeaponEventQueue { get; set; } = string.Empty;

        [Sync]
        public int Player1WeaponEventSerial { get; set; }

        [Sync]
        public string Player1WeaponEventQueue { get; set; } = string.Empty;

        [Sync]
        public int Player0CombatEventSerial { get; set; }

        [Sync]
        public string Player0CombatEventQueue { get; set; } = string.Empty;

        [Sync]
        public int Player1CombatEventSerial { get; set; }

        [Sync]
        public string Player1CombatEventQueue { get; set; } = string.Empty;

        [Sync]
        public sbyte Player0ForwardMove { get; set; }

        [Sync]
        public sbyte Player0SideMove { get; set; }

        [Sync]
        public short Player0AngleTurn { get; set; }

        [Sync]
        public byte Player0Buttons { get; set; }

        [Sync]
        public sbyte Player1ForwardMove { get; set; }

        [Sync]
        public sbyte Player1SideMove { get; set; }

        [Sync]
        public short Player1AngleTurn { get; set; }

        [Sync]
        public byte Player1Buttons { get; set; }

        [Sync]
        public string Player0CommandQueue { get; set; } = string.Empty;

        [Sync]
        public string Player1CommandQueue { get; set; } = string.Empty;

        [Sync]
        public bool Player0PvpReady { get; set; }

        [Sync]
        public bool Player1PvpReady { get; set; }

        [Sync]
        public bool SharedSimulationPvpStarted { get; set; }

        [Sync]
        public int SharedSimulationPvpStartSerial { get; set; }

        [Sync]
        public int Player0PvpChecksumLevelTime { get; set; }

        [Sync]
        public string Player0PvpChecksum { get; set; } = string.Empty;

        [Sync]
        public int Player1PvpChecksumLevelTime { get; set; }

        [Sync]
        public string Player1PvpChecksum { get; set; } = string.Empty;

        [Sync]
        public bool Player0StateActive { get; set; }

        [Sync]
        public int Player0StateX { get; set; }

        [Sync]
        public int Player0StateY { get; set; }

        [Sync]
        public int Player0StateZ { get; set; }

        [Sync]
        public int Player0StateMomX { get; set; }

        [Sync]
        public int Player0StateMomY { get; set; }

        [Sync]
        public int Player0StateMomZ { get; set; }

        [Sync]
        public int Player0StateAngle { get; set; }

        [Sync]
        public int Player0StateHealth { get; set; }

        [Sync]
        public int Player0StatePlayerState { get; set; }

        [Sync]
        public int Player0StateArmorPoints { get; set; }

        [Sync]
        public int Player0StateArmorType { get; set; }

        [Sync]
        public int Player0StateReadyWeapon { get; set; }

        [Sync]
        public int Player0StatePendingWeapon { get; set; }

        [Sync]
        public int Player0StateAmmoClip { get; set; }

        [Sync]
        public int Player0StateAmmoShell { get; set; }

        [Sync]
        public int Player0StateAmmoCell { get; set; }

        [Sync]
        public int Player0StateAmmoMissile { get; set; }

        [Sync]
        public bool Player0StateBackpack { get; set; }

        [Sync]
        public bool Player0StateCardBlueCard { get; set; }

        [Sync]
        public bool Player0StateCardYellowCard { get; set; }

        [Sync]
        public bool Player0StateCardRedCard { get; set; }

        [Sync]
        public bool Player0StateCardBlueSkull { get; set; }

        [Sync]
        public bool Player0StateCardYellowSkull { get; set; }

        [Sync]
        public bool Player0StateCardRedSkull { get; set; }

        [Sync]
        public int Player0StatePowerInvulnerability { get; set; }

        [Sync]
        public int Player0StatePowerStrength { get; set; }

        [Sync]
        public int Player0StatePowerInvisibility { get; set; }

        [Sync]
        public int Player0StatePowerIronFeet { get; set; }

        [Sync]
        public int Player0StatePowerAllMap { get; set; }

        [Sync]
        public int Player0StatePowerInfrared { get; set; }

        [Sync]
        public int Player0StateMaxAmmoClip { get; set; }

        [Sync]
        public int Player0StateMaxAmmoShell { get; set; }

        [Sync]
        public int Player0StateMaxAmmoCell { get; set; }

        [Sync]
        public int Player0StateMaxAmmoMissile { get; set; }

        [Sync]
        public bool Player0StateWeaponOwnedFist { get; set; }

        [Sync]
        public bool Player0StateWeaponOwnedPistol { get; set; }

        [Sync]
        public bool Player0StateWeaponOwnedShotgun { get; set; }

        [Sync]
        public bool Player0StateWeaponOwnedChaingun { get; set; }

        [Sync]
        public bool Player0StateWeaponOwnedMissile { get; set; }

        [Sync]
        public bool Player0StateWeaponOwnedPlasma { get; set; }

        [Sync]
        public bool Player0StateWeaponOwnedBfg { get; set; }

        [Sync]
        public bool Player0StateWeaponOwnedChainsaw { get; set; }

        [Sync]
        public bool Player0StateWeaponOwnedSuperShotgun { get; set; }

        [Sync]
        public int Player0StateDamageCount { get; set; }

        [Sync]
        public int Player0StateBonusCount { get; set; }

        [Sync]
        public int Player0StateFrag0 { get; set; }

        [Sync]
        public int Player0StateFrag1 { get; set; }

        [Sync]
        public int Player0StateFrag2 { get; set; }

        [Sync]
        public int Player0StateFrag3 { get; set; }

        [Sync]
        public int Player0StateMobjState { get; set; }

        [Sync]
        public int Player0StateMobjSprite { get; set; }

        [Sync]
        public int Player0StateMobjFrame { get; set; }

        [Sync]
        public int Player0StateMobjTics { get; set; }

        [Sync]
        public bool Player1StateActive { get; set; }

        [Sync]
        public int Player1StateX { get; set; }

        [Sync]
        public int Player1StateY { get; set; }

        [Sync]
        public int Player1StateZ { get; set; }

        [Sync]
        public int Player1StateMomX { get; set; }

        [Sync]
        public int Player1StateMomY { get; set; }

        [Sync]
        public int Player1StateMomZ { get; set; }

        [Sync]
        public int Player1StateAngle { get; set; }

        [Sync]
        public int Player1StateHealth { get; set; }

        [Sync]
        public int Player1StatePlayerState { get; set; }

        [Sync]
        public int Player1StateArmorPoints { get; set; }

        [Sync]
        public int Player1StateArmorType { get; set; }

        [Sync]
        public int Player1StateReadyWeapon { get; set; }

        [Sync]
        public int Player1StatePendingWeapon { get; set; }

        [Sync]
        public int Player1StateAmmoClip { get; set; }

        [Sync]
        public int Player1StateAmmoShell { get; set; }

        [Sync]
        public int Player1StateAmmoCell { get; set; }

        [Sync]
        public int Player1StateAmmoMissile { get; set; }

        [Sync]
        public bool Player1StateBackpack { get; set; }

        [Sync]
        public bool Player1StateCardBlueCard { get; set; }

        [Sync]
        public bool Player1StateCardYellowCard { get; set; }

        [Sync]
        public bool Player1StateCardRedCard { get; set; }

        [Sync]
        public bool Player1StateCardBlueSkull { get; set; }

        [Sync]
        public bool Player1StateCardYellowSkull { get; set; }

        [Sync]
        public bool Player1StateCardRedSkull { get; set; }

        [Sync]
        public int Player1StatePowerInvulnerability { get; set; }

        [Sync]
        public int Player1StatePowerStrength { get; set; }

        [Sync]
        public int Player1StatePowerInvisibility { get; set; }

        [Sync]
        public int Player1StatePowerIronFeet { get; set; }

        [Sync]
        public int Player1StatePowerAllMap { get; set; }

        [Sync]
        public int Player1StatePowerInfrared { get; set; }

        [Sync]
        public int Player1StateMaxAmmoClip { get; set; }

        [Sync]
        public int Player1StateMaxAmmoShell { get; set; }

        [Sync]
        public int Player1StateMaxAmmoCell { get; set; }

        [Sync]
        public int Player1StateMaxAmmoMissile { get; set; }

        [Sync]
        public bool Player1StateWeaponOwnedFist { get; set; }

        [Sync]
        public bool Player1StateWeaponOwnedPistol { get; set; }

        [Sync]
        public bool Player1StateWeaponOwnedShotgun { get; set; }

        [Sync]
        public bool Player1StateWeaponOwnedChaingun { get; set; }

        [Sync]
        public bool Player1StateWeaponOwnedMissile { get; set; }

        [Sync]
        public bool Player1StateWeaponOwnedPlasma { get; set; }

        [Sync]
        public bool Player1StateWeaponOwnedBfg { get; set; }

        [Sync]
        public bool Player1StateWeaponOwnedChainsaw { get; set; }

        [Sync]
        public bool Player1StateWeaponOwnedSuperShotgun { get; set; }

        [Sync]
        public int Player1StateDamageCount { get; set; }

        [Sync]
        public int Player1StateBonusCount { get; set; }

        [Sync]
        public int Player1StateFrag0 { get; set; }

        [Sync]
        public int Player1StateFrag1 { get; set; }

        [Sync]
        public int Player1StateFrag2 { get; set; }

        [Sync]
        public int Player1StateFrag3 { get; set; }

        [Sync]
        public int Player1StateMobjState { get; set; }

        [Sync]
        public int Player1StateMobjSprite { get; set; }

        [Sync]
        public int Player1StateMobjFrame { get; set; }

        [Sync]
        public int Player1StateMobjTics { get; set; }

        [Rpc.Broadcast]
        public void OpenMatchMenuRpc()
        {
            MatchStarted = true;
        }

        [Rpc.Broadcast]
        public void CloseMatchMenuRpc()
        {
            MatchStarted = false;
        }

        [Rpc.Broadcast]
        public void StartPvpRpc( string mapName )
        {
            MatchStarted = false;
            PvpMap = string.IsNullOrWhiteSpace( mapName ) ? "E1M1" : mapName.Trim().ToUpperInvariant();
            PvpActive = true;
            PvpRoundEnded = false;
            PvpWinnerSerial = 0;
            PvpWinnerName = string.Empty;
            Player0PvpReady = false;
            Player1PvpReady = false;
            SharedSimulationPvpStarted = false;
            SharedSimulationPvpStartSerial = 0;
            Player0PvpChecksumLevelTime = 0;
            Player1PvpChecksumLevelTime = 0;
            Player0PvpChecksum = string.Empty;
            Player1PvpChecksum = string.Empty;
            ClearPlayerCommands();
            PvpLaunchSerial++;
        }

        [Rpc.Broadcast]
        public void StopPvpRpc()
        {
            PvpActive = false;
            PvpMap = string.Empty;
            PvpRoundEnded = false;
            PvpWinnerSerial = 0;
            PvpWinnerName = string.Empty;
            RemovedPickupThingIndexes = string.Empty;
            DestroyedWorldThingIndexes = string.Empty;
            PvpSectorState = string.Empty;
            PvpLineState = string.Empty;
            PvpProjectileState = string.Empty;
            HitscanEventSerial = 0;
            HitscanEventQueue = string.Empty;
            EffectEventSerial = 0;
            EffectEventQueue = string.Empty;
            Player0WeaponEventSerial = 0;
            Player0WeaponEventQueue = string.Empty;
            Player1WeaponEventSerial = 0;
            Player1WeaponEventQueue = string.Empty;
            Player0CombatEventSerial = 0;
            Player0CombatEventQueue = string.Empty;
            Player1CombatEventSerial = 0;
            Player1CombatEventQueue = string.Empty;
            Player0PvpReady = false;
            Player1PvpReady = false;
            SharedSimulationPvpStarted = false;
            SharedSimulationPvpStartSerial = 0;
            Player0PvpChecksumLevelTime = 0;
            Player1PvpChecksumLevelTime = 0;
            Player0PvpChecksum = string.Empty;
            Player1PvpChecksum = string.Empty;
            ClearPlayerCommands();
        }

        [Rpc.Broadcast]
        public void AnnouncePvpWinnerRpc( string winnerName )
        {
            PvpRoundEnded = true;
            PvpWinnerName = string.IsNullOrWhiteSpace( winnerName ) ? "PLAYER" : winnerName.Trim().ToUpperInvariant();
            PvpWinnerSerial++;
        }

        [Rpc.Broadcast]
        public void SetPvpReadyRpc( int playerIndex, bool ready )
        {
            if ( playerIndex == 0 )
            {
                Player0PvpReady = ready;
                return;
            }

            if ( playerIndex == 1 )
            {
                Player1PvpReady = ready;
            }
        }

        [Rpc.Broadcast]
        public void BeginSharedSimulationPvpRpc()
        {
            SharedSimulationPvpStarted = true;
            SharedSimulationPvpStartSerial++;
            Player0PvpChecksumLevelTime = 0;
            Player1PvpChecksumLevelTime = 0;
            Player0PvpChecksum = string.Empty;
            Player1PvpChecksum = string.Empty;
            ClearPlayerCommands();
        }

        [Rpc.Broadcast]
        public void SetPlayerCommandRpc( int playerIndex, sbyte forwardMove, sbyte sideMove, short angleTurn, byte buttons )
        {
            if ( playerIndex == 0 )
            {
                Player0ForwardMove = forwardMove;
                Player0SideMove = sideMove;
                Player0AngleTurn = angleTurn;
                Player0Buttons = buttons;
                return;
            }

            if ( playerIndex == 1 )
            {
                Player1ForwardMove = forwardMove;
                Player1SideMove = sideMove;
                Player1AngleTurn = angleTurn;
                Player1Buttons = buttons;
            }
        }

        [Rpc.Broadcast]
        public void QueuePlayerCommandRpc( int playerIndex, int tic, sbyte forwardMove, sbyte sideMove, short angleTurn, byte buttons )
        {
            var entry = $"{tic},{forwardMove},{sideMove},{angleTurn},{buttons}";

            if ( playerIndex == 0 )
            {
                Player0CommandQueue = AppendCommandQueueEntry( Player0CommandQueue, entry );
                return;
            }

            if ( playerIndex == 1 )
            {
                Player1CommandQueue = AppendCommandQueueEntry( Player1CommandQueue, entry );
            }
        }

        [Rpc.Broadcast]
        public void MarkPlayerInactiveRpc( string playerName )
        {
            if ( string.IsNullOrWhiteSpace( playerName ) )
            {
                return;
            }

            var players = GetInactivePlayerSet();
            players.Add( playerName.Trim() );
            InactivePlayers = string.Join( "|", players );
        }

        [Rpc.Broadcast]
        public void MarkPlayerActiveRpc( string playerName )
        {
            if ( string.IsNullOrWhiteSpace( playerName ) )
            {
                return;
            }

            var players = GetInactivePlayerSet();
            players.Remove( playerName.Trim() );
            InactivePlayers = string.Join( "|", players );
        }

        public bool IsPlayerInactive( string playerName )
        {
            if ( string.IsNullOrWhiteSpace( playerName ) )
            {
                return false;
            }

            return GetInactivePlayerSet().Contains( playerName.Trim() );
        }

        private HashSet<string> GetInactivePlayerSet()
        {
            return new HashSet<string>(
                (InactivePlayers ?? string.Empty)
                    .Split( '|', StringSplitOptions.RemoveEmptyEntries )
                    .Select( name => name.Trim() )
                    .Where( name => !string.IsNullOrWhiteSpace( name ) ),
                StringComparer.OrdinalIgnoreCase );
        }

        public void CopyPlayerCommandTo( int playerIndex, TicCmd cmd )
        {
            if ( cmd is null )
            {
                return;
            }

            if ( playerIndex == 0 )
            {
                cmd.ForwardMove = Player0ForwardMove;
                cmd.SideMove = Player0SideMove;
                cmd.AngleTurn = Player0AngleTurn;
                cmd.Buttons = Player0Buttons;
                return;
            }

            if ( playerIndex == 1 )
            {
                cmd.ForwardMove = Player1ForwardMove;
                cmd.SideMove = Player1SideMove;
                cmd.AngleTurn = Player1AngleTurn;
                cmd.Buttons = Player1Buttons;
            }
        }

        public bool TryCopyQueuedPlayerCommandTo( int playerIndex, int tic, TicCmd cmd )
        {
            if ( cmd is null )
            {
                return false;
            }

            var queue = playerIndex == 0 ? Player0CommandQueue : Player1CommandQueue;
            if ( string.IsNullOrWhiteSpace( queue ) )
            {
                return false;
            }

            var entries = queue.Split( '|', StringSplitOptions.RemoveEmptyEntries );
            for ( var i = entries.Length - 1; i >= 0; i-- )
            {
                var parts = entries[i].Split( ',', StringSplitOptions.None );
                if ( parts.Length != 5
                    || !int.TryParse( parts[0], out var entryTic )
                    || entryTic != tic
                    || !sbyte.TryParse( parts[1], out var forwardMove )
                    || !sbyte.TryParse( parts[2], out var sideMove )
                    || !short.TryParse( parts[3], out var angleTurn )
                    || !byte.TryParse( parts[4], out var buttons ) )
                {
                    continue;
                }

                cmd.ForwardMove = forwardMove;
                cmd.SideMove = sideMove;
                cmd.AngleTurn = angleTurn;
                cmd.Buttons = buttons;
                return true;
            }

            return false;
        }

        private void ClearPlayerCommands()
        {
            Player0ForwardMove = 0;
            Player0SideMove = 0;
            Player0AngleTurn = 0;
            Player0Buttons = 0;
            Player1ForwardMove = 0;
            Player1SideMove = 0;
            Player1AngleTurn = 0;
            Player1Buttons = 0;
            Player0CommandQueue = string.Empty;
            Player1CommandQueue = string.Empty;
        }

        [Rpc.Broadcast]
        public void SetPvpChecksumRpc( int playerIndex, int levelTime, string checksum )
        {
            checksum ??= string.Empty;

            if ( playerIndex == 0 )
            {
                Player0PvpChecksumLevelTime = levelTime;
                Player0PvpChecksum = checksum;
                return;
            }

            if ( playerIndex == 1 )
            {
                Player1PvpChecksumLevelTime = levelTime;
                Player1PvpChecksum = checksum;
            }
        }

        public void QueueWeaponEvent(
            int playerIndex,
            MultiplayerWeaponEventType eventType,
            int weapon,
            int ammoType,
            int ammoAmount,
            int selectedWeapon,
            int ownedWeaponMask,
            int ammoClip,
            int ammoShell,
            int ammoCell,
            int ammoMissile )
        {
            var serial = playerIndex == 0 ? ++Player0WeaponEventSerial : ++Player1WeaponEventSerial;
            var entry =
                $"{serial},{(int)eventType},{weapon},{ammoType},{ammoAmount},{selectedWeapon},{ownedWeaponMask},{ammoClip},{ammoShell},{ammoCell},{ammoMissile}";

            if ( playerIndex == 0 )
            {
                Player0WeaponEventQueue = AppendWeaponEvent( Player0WeaponEventQueue, entry );
                return;
            }

            if ( playerIndex == 1 )
            {
                Player1WeaponEventQueue = AppendWeaponEvent( Player1WeaponEventQueue, entry );
            }
        }

        public void QueueCombatEvent(
            int playerIndex,
            MultiplayerCombatEventType eventType,
            int health,
            int damageCount,
            int playerState,
            int selectedWeapon,
            int ownedWeaponMask,
            int ammoClip,
            int ammoShell,
            int ammoCell,
            int ammoMissile,
            int frag0,
            int frag1,
            int frag2,
            int frag3 )
        {
            var serial = playerIndex == 0 ? ++Player0CombatEventSerial : ++Player1CombatEventSerial;
            var entry =
                $"{serial},{(int)eventType},{health},{damageCount},{playerState},{selectedWeapon},{ownedWeaponMask},{ammoClip},{ammoShell},{ammoCell},{ammoMissile},{frag0},{frag1},{frag2},{frag3}";

            if ( playerIndex == 0 )
            {
                Player0CombatEventQueue = AppendEventQueueEntry( Player0CombatEventQueue, entry );
                return;
            }

            if ( playerIndex == 1 )
            {
                Player1CombatEventQueue = AppendEventQueueEntry( Player1CombatEventQueue, entry );
            }
        }

        public void QueueHitscanEvent( bool blood, int x, int y, int z, int damage )
        {
            var serial = ++HitscanEventSerial;
            var entry = $"{serial},{(blood ? 1 : 0)},{x},{y},{z},{damage}";
            HitscanEventQueue = AppendEventQueueEntry( HitscanEventQueue, entry );
        }

        public void QueueEffectEvent( MultiplayerEffectEventType eventType, int playerIndex, int value )
        {
            var serial = ++EffectEventSerial;
            var entry = $"{serial},{(int)eventType},{playerIndex},{value}";
            EffectEventQueue = AppendEventQueueEntry( EffectEventQueue, entry );
        }

        private static string AppendWeaponEvent( string currentQueue, string entry )
        {
            return AppendEventQueueEntry( currentQueue, entry );
        }

        private static string AppendCommandQueueEntry( string currentQueue, string entry )
        {
            const int maxEntries = 64;
            return AppendQueueEntry( currentQueue, entry, maxEntries );
        }

        private static string AppendEventQueueEntry( string currentQueue, string entry )
        {
            const int maxEntries = 16;
            return AppendQueueEntry( currentQueue, entry, maxEntries );
        }

        private static string AppendQueueEntry( string currentQueue, string entry, int maxEntries )
        {
            var items = new List<string>();
            if ( !string.IsNullOrWhiteSpace( currentQueue ) )
            {
                items.AddRange( currentQueue.Split( '|', StringSplitOptions.RemoveEmptyEntries ) );
            }

            items.Add( entry );

            if ( items.Count > maxEntries )
            {
                items.RemoveRange( 0, items.Count - maxEntries );
            }

            return string.Join( "|", items );
        }

        public void PublishAuthoritativePlayerState( int playerIndex, Player player, bool publishMovementAndVisuals = true )
        {
            var active = player?.InGame == true && player.Mobj is not null;
            var x = active ? player.Mobj.X.Data : 0;
            var y = active ? player.Mobj.Y.Data : 0;
            var z = active ? player.Mobj.Z.Data : 0;
            var momX = active ? player.Mobj.MomX.Data : 0;
            var momY = active ? player.Mobj.MomY.Data : 0;
            var momZ = active ? player.Mobj.MomZ.Data : 0;
            var angle = active ? unchecked( (int)player.Mobj.Angle.Data ) : 0;
            var health = active ? player.Mobj.Health : 0;
            var playerState = player is not null ? (int)player.PlayerState : 0;
            var armorPoints = player?.ArmorPoints ?? 0;
            var armorType = player?.ArmorType ?? 0;
            var readyWeapon = player is not null ? (int)player.ReadyWeapon : 0;
            var pendingWeapon = player is not null ? (int)player.PendingWeapon : 0;
            var ammoClip = player?.Ammo[(int)AmmoType.Clip] ?? 0;
            var ammoShell = player?.Ammo[(int)AmmoType.Shell] ?? 0;
            var ammoCell = player?.Ammo[(int)AmmoType.Cell] ?? 0;
            var ammoMissile = player?.Ammo[(int)AmmoType.Missile] ?? 0;
            var backpack = player?.Backpack ?? false;
            var cardBlueCard = player?.Cards[(int)CardType.BlueCard] ?? false;
            var cardYellowCard = player?.Cards[(int)CardType.YellowCard] ?? false;
            var cardRedCard = player?.Cards[(int)CardType.RedCard] ?? false;
            var cardBlueSkull = player?.Cards[(int)CardType.BlueSkull] ?? false;
            var cardYellowSkull = player?.Cards[(int)CardType.YellowSkull] ?? false;
            var cardRedSkull = player?.Cards[(int)CardType.RedSkull] ?? false;
            var powerInvulnerability = player?.Powers[(int)PowerType.Invulnerability] ?? 0;
            var powerStrength = player?.Powers[(int)PowerType.Strength] ?? 0;
            var powerInvisibility = player?.Powers[(int)PowerType.Invisibility] ?? 0;
            var powerIronFeet = player?.Powers[(int)PowerType.IronFeet] ?? 0;
            var powerAllMap = player?.Powers[(int)PowerType.AllMap] ?? 0;
            var powerInfrared = player?.Powers[(int)PowerType.Infrared] ?? 0;
            var maxAmmoClip = player?.MaxAmmo[(int)AmmoType.Clip] ?? 0;
            var maxAmmoShell = player?.MaxAmmo[(int)AmmoType.Shell] ?? 0;
            var maxAmmoCell = player?.MaxAmmo[(int)AmmoType.Cell] ?? 0;
            var maxAmmoMissile = player?.MaxAmmo[(int)AmmoType.Missile] ?? 0;
            var ownedFist = player?.WeaponOwned[(int)WeaponType.Fist] ?? false;
            var ownedPistol = player?.WeaponOwned[(int)WeaponType.Pistol] ?? false;
            var ownedShotgun = player?.WeaponOwned[(int)WeaponType.Shotgun] ?? false;
            var ownedChaingun = player?.WeaponOwned[(int)WeaponType.Chaingun] ?? false;
            var ownedMissile = player?.WeaponOwned[(int)WeaponType.Missile] ?? false;
            var ownedPlasma = player?.WeaponOwned[(int)WeaponType.Plasma] ?? false;
            var ownedBfg = player?.WeaponOwned[(int)WeaponType.Bfg] ?? false;
            var ownedChainsaw = player?.WeaponOwned[(int)WeaponType.Chainsaw] ?? false;
            var ownedSuperShotgun = player?.WeaponOwned[(int)WeaponType.SuperShotgun] ?? false;
            var damageCount = player?.DamageCount ?? 0;
            var bonusCount = player?.BonusCount ?? 0;
            var frag0 = player?.Frags[0] ?? 0;
            var frag1 = player?.Frags[1] ?? 0;
            var frag2 = player?.Frags[2] ?? 0;
            var frag3 = player?.Frags[3] ?? 0;
            var mobjState = active ? player.Mobj.State.Number : 0;
            var mobjSprite = active ? (int)player.Mobj.Sprite : 0;
            var mobjFrame = active ? player.Mobj.Frame : 0;
            var mobjTics = active ? player.Mobj.Tics : 0;

            if ( playerIndex == 0 )
            {
                Player0StateActive = active;
                if ( publishMovementAndVisuals )
                {
                    Player0StateX = x;
                    Player0StateY = y;
                    Player0StateZ = z;
                    Player0StateMomX = momX;
                    Player0StateMomY = momY;
                    Player0StateMomZ = momZ;
                    Player0StateAngle = angle;
                }
                Player0StateHealth = health;
                Player0StatePlayerState = playerState;
                Player0StateArmorPoints = armorPoints;
                Player0StateArmorType = armorType;
                Player0StateReadyWeapon = readyWeapon;
                Player0StatePendingWeapon = pendingWeapon;
                Player0StateAmmoClip = ammoClip;
                Player0StateAmmoShell = ammoShell;
                Player0StateAmmoCell = ammoCell;
                Player0StateAmmoMissile = ammoMissile;
                Player0StateBackpack = backpack;
                Player0StateCardBlueCard = cardBlueCard;
                Player0StateCardYellowCard = cardYellowCard;
                Player0StateCardRedCard = cardRedCard;
                Player0StateCardBlueSkull = cardBlueSkull;
                Player0StateCardYellowSkull = cardYellowSkull;
                Player0StateCardRedSkull = cardRedSkull;
                Player0StatePowerInvulnerability = powerInvulnerability;
                Player0StatePowerStrength = powerStrength;
                Player0StatePowerInvisibility = powerInvisibility;
                Player0StatePowerIronFeet = powerIronFeet;
                Player0StatePowerAllMap = powerAllMap;
                Player0StatePowerInfrared = powerInfrared;
                Player0StateMaxAmmoClip = maxAmmoClip;
                Player0StateMaxAmmoShell = maxAmmoShell;
                Player0StateMaxAmmoCell = maxAmmoCell;
                Player0StateMaxAmmoMissile = maxAmmoMissile;
                Player0StateWeaponOwnedFist = ownedFist;
                Player0StateWeaponOwnedPistol = ownedPistol;
                Player0StateWeaponOwnedShotgun = ownedShotgun;
                Player0StateWeaponOwnedChaingun = ownedChaingun;
                Player0StateWeaponOwnedMissile = ownedMissile;
                Player0StateWeaponOwnedPlasma = ownedPlasma;
                Player0StateWeaponOwnedBfg = ownedBfg;
                Player0StateWeaponOwnedChainsaw = ownedChainsaw;
                Player0StateWeaponOwnedSuperShotgun = ownedSuperShotgun;
                Player0StateDamageCount = damageCount;
                Player0StateBonusCount = bonusCount;
                Player0StateFrag0 = frag0;
                Player0StateFrag1 = frag1;
                Player0StateFrag2 = frag2;
                Player0StateFrag3 = frag3;
                if ( publishMovementAndVisuals )
                {
                    Player0StateMobjState = mobjState;
                    Player0StateMobjSprite = mobjSprite;
                    Player0StateMobjFrame = mobjFrame;
                    Player0StateMobjTics = mobjTics;
                }
                return;
            }

            if ( playerIndex == 1 )
            {
                Player1StateActive = active;
                if ( publishMovementAndVisuals )
                {
                    Player1StateX = x;
                    Player1StateY = y;
                    Player1StateZ = z;
                    Player1StateMomX = momX;
                    Player1StateMomY = momY;
                    Player1StateMomZ = momZ;
                    Player1StateAngle = angle;
                }
                Player1StateHealth = health;
                Player1StatePlayerState = playerState;
                Player1StateArmorPoints = armorPoints;
                Player1StateArmorType = armorType;
                Player1StateReadyWeapon = readyWeapon;
                Player1StatePendingWeapon = pendingWeapon;
                Player1StateAmmoClip = ammoClip;
                Player1StateAmmoShell = ammoShell;
                Player1StateAmmoCell = ammoCell;
                Player1StateAmmoMissile = ammoMissile;
                Player1StateBackpack = backpack;
                Player1StateCardBlueCard = cardBlueCard;
                Player1StateCardYellowCard = cardYellowCard;
                Player1StateCardRedCard = cardRedCard;
                Player1StateCardBlueSkull = cardBlueSkull;
                Player1StateCardYellowSkull = cardYellowSkull;
                Player1StateCardRedSkull = cardRedSkull;
                Player1StatePowerInvulnerability = powerInvulnerability;
                Player1StatePowerStrength = powerStrength;
                Player1StatePowerInvisibility = powerInvisibility;
                Player1StatePowerIronFeet = powerIronFeet;
                Player1StatePowerAllMap = powerAllMap;
                Player1StatePowerInfrared = powerInfrared;
                Player1StateMaxAmmoClip = maxAmmoClip;
                Player1StateMaxAmmoShell = maxAmmoShell;
                Player1StateMaxAmmoCell = maxAmmoCell;
                Player1StateMaxAmmoMissile = maxAmmoMissile;
                Player1StateWeaponOwnedFist = ownedFist;
                Player1StateWeaponOwnedPistol = ownedPistol;
                Player1StateWeaponOwnedShotgun = ownedShotgun;
                Player1StateWeaponOwnedChaingun = ownedChaingun;
                Player1StateWeaponOwnedMissile = ownedMissile;
                Player1StateWeaponOwnedPlasma = ownedPlasma;
                Player1StateWeaponOwnedBfg = ownedBfg;
                Player1StateWeaponOwnedChainsaw = ownedChainsaw;
                Player1StateWeaponOwnedSuperShotgun = ownedSuperShotgun;
                Player1StateDamageCount = damageCount;
                Player1StateBonusCount = bonusCount;
                Player1StateFrag0 = frag0;
                Player1StateFrag1 = frag1;
                Player1StateFrag2 = frag2;
                Player1StateFrag3 = frag3;
                if ( publishMovementAndVisuals )
                {
                    Player1StateMobjState = mobjState;
                    Player1StateMobjSprite = mobjSprite;
                    Player1StateMobjFrame = mobjFrame;
                    Player1StateMobjTics = mobjTics;
                }
            }
        }

        [Rpc.Broadcast]
        public void PublishOwnedPlayerMovementStateRpc(
            int playerIndex,
            bool active,
            int x,
            int y,
            int z,
            int momX,
            int momY,
            int momZ,
            int angle,
            int mobjState,
            int mobjSprite,
            int mobjFrame,
            int mobjTics )
        {
            if ( playerIndex == 0 )
            {
                Player0StateActive = active;
                Player0StateX = x;
                Player0StateY = y;
                Player0StateZ = z;
                Player0StateMomX = momX;
                Player0StateMomY = momY;
                Player0StateMomZ = momZ;
                Player0StateAngle = angle;
                return;
            }

            if ( playerIndex == 1 )
            {
                Player1StateActive = active;
                Player1StateX = x;
                Player1StateY = y;
                Player1StateZ = z;
                Player1StateMomX = momX;
                Player1StateMomY = momY;
                Player1StateMomZ = momZ;
                Player1StateAngle = angle;
            }
        }

        protected override void OnStart()
        {
            Current = this;
        }

        protected override void OnDestroy()
        {
            if (Current == this)
            {
                Current = null;
            }
        }
    }
}
