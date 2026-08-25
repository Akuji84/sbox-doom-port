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
    public static class SaveAndLoad
    {
        public static readonly int DescriptionSize = 24;
        public static bool IsSupported => true;
        private const string SaveGameDirectory = "savegames";

        private static readonly byte[] VersionTag = new byte[]
        {
            (byte)'S', (byte)'B', (byte)'O', (byte)'X',
            (byte)'0', (byte)'0', (byte)'1', 0
        };

        public static string GetSlotPath(int slotNumber)
        {
            return $"{SaveGameDirectory}/slot{slotNumber}.dsg";
        }

        public static bool SlotExists(int slotNumber)
        {
            return SboxManagedDoomFileSystem.DataFileExists(GetSlotPath(slotNumber));
        }

        // Thinker type discriminators.
        private const byte ThinkerEnd = 0;
        private const byte ThinkerMobj = 1;
        private const byte ThinkerVerticalDoor = 2;
        private const byte ThinkerPlatform = 3;
        private const byte ThinkerFloorMove = 4;
        private const byte ThinkerCeilingMove = 5;
        private const byte ThinkerLightFlash = 6;
        private const byte ThinkerStrobeFlash = 7;
        private const byte ThinkerFireFlicker = 8;
        private const byte ThinkerGlowingLight = 9;

        public static void Save(DoomGame game, string description, string path)
        {
            SboxManagedDoomFileSystem.WriteAllBytesToData(path, SaveToMemory(game, description));
        }

        // Serializes the full world state to bytes without touching the
        // filesystem; used for savegames and for co-op state resync.
        public static byte[] SaveToMemory(DoomGame game, string description)
        {
            var w = new SaveWriter();
            var world = game.World;
            var options = game.Options;
            var map = world.Map;

            // Header.
            w.WriteString(description, DescriptionSize);
            w.WriteBytes(VersionTag);
            w.WriteByte((byte)options.Skill);
            w.WriteByte((byte)options.Episode);
            w.WriteByte((byte)options.Map);
            w.WriteInt32(game.GameTic);
            w.WriteByte((byte)options.Random.Index);

            // World state.
            w.WriteInt32(world.LevelTime);
            w.WriteInt32(world.TotalKills);
            w.WriteInt32(world.TotalItems);
            w.WriteInt32(world.TotalSecrets);

            // Assign mobj indices for cross-reference resolution.
            var mobjTable = new Dictionary<Mobj, int>();
            var mobjIndex = 0;
            foreach (var thinker in world.Thinkers)
            {
                if (thinker is Mobj mobj)
                {
                    mobjTable[mobj] = mobjIndex++;
                }
            }

            // Players.
            for (var i = 0; i < Player.MaxPlayerCount; i++)
            {
                var player = options.Players[i];
                w.WriteBool(player.InGame);
                if (!player.InGame) continue;

                w.WriteByte((byte)player.PlayerState);
                w.WriteInt32(player.ViewZ.Data);
                w.WriteInt32(player.ViewHeight.Data);
                w.WriteInt32(player.DeltaViewHeight.Data);
                w.WriteInt32(player.Bob.Data);
                w.WriteInt32(player.Health);
                w.WriteInt32(player.ArmorPoints);
                w.WriteInt32(player.ArmorType);

                for (var p = 0; p < (int)PowerType.Count; p++)
                    w.WriteInt32(player.Powers[p]);
                for (var c = 0; c < (int)CardType.Count; c++)
                    w.WriteBool(player.Cards[c]);
                w.WriteBool(player.Backpack);

                for (var f = 0; f < Player.MaxPlayerCount; f++)
                    w.WriteInt32(player.Frags[f]);

                w.WriteInt32((int)player.ReadyWeapon);
                w.WriteInt32((int)player.PendingWeapon);

                for (var wp = 0; wp < (int)WeaponType.Count; wp++)
                    w.WriteBool(player.WeaponOwned[wp]);
                for (var a = 0; a < (int)AmmoType.Count; a++)
                    w.WriteInt32(player.Ammo[a]);
                for (var a = 0; a < (int)AmmoType.Count; a++)
                    w.WriteInt32(player.MaxAmmo[a]);

                w.WriteBool(player.AttackDown);
                w.WriteBool(player.UseDown);
                w.WriteInt32((int)player.Cheats);
                w.WriteInt32(player.Refire);
                w.WriteInt32(player.KillCount);
                w.WriteInt32(player.ItemCount);
                w.WriteInt32(player.SecretCount);
                w.WriteInt32(player.DamageCount);
                w.WriteInt32(player.BonusCount);
                w.WriteInt32(player.ExtraLight);
                w.WriteInt32(player.FixedColorMap);
                w.WriteInt32(player.ColorMap);

                for (var s = 0; s < (int)PlayerSprite.Count; s++)
                {
                    var psp = player.PlayerSprites[s];
                    if (psp.State != null)
                        w.WriteInt32(psp.State.Number);
                    else
                        w.WriteInt32(-1);
                    w.WriteInt32(psp.Tics);
                    w.WriteInt32(psp.Sx.Data);
                    w.WriteInt32(psp.Sy.Data);
                }

                w.WriteBool(player.DidSecret);

                // Player mobj/attacker references (indices into mobj table).
                w.WriteInt32(WriteMobjRef(mobjTable, player.Mobj));
                w.WriteInt32(WriteMobjRef(mobjTable, player.Attacker));
            }

            // Sectors.
            var sectors = map.Sectors;
            w.WriteInt32(sectors.Length);
            for (var i = 0; i < sectors.Length; i++)
            {
                var sector = sectors[i];
                w.WriteInt32(sector.FloorHeight.Data);
                w.WriteInt32(sector.CeilingHeight.Data);
                w.WriteInt32(sector.FloorFlat);
                w.WriteInt32(sector.CeilingFlat);
                w.WriteInt16((short)sector.LightLevel);
                w.WriteInt16((short)sector.Special);
            }

            // LineDefs.
            var lines = map.Lines;
            w.WriteInt32(lines.Length);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                w.WriteInt16((short)line.Flags);
                w.WriteInt16((short)line.Special);
                w.WriteInt16(line.Tag);
            }

            // Thinkers.
            foreach (var thinker in world.Thinkers)
            {
                if (thinker is Mobj mobj)
                {
                    w.WriteByte(ThinkerMobj);
                    WriteMobj(w, mobj, mobjTable);
                }
                else if (thinker is VerticalDoor door)
                {
                    w.WriteByte(ThinkerVerticalDoor);
                    WriteVerticalDoor(w, door);
                }
                else if (thinker is Platform plat)
                {
                    w.WriteByte(ThinkerPlatform);
                    WritePlatform(w, plat);
                }
                else if (thinker is FloorMove floor)
                {
                    w.WriteByte(ThinkerFloorMove);
                    WriteFloorMove(w, floor);
                }
                else if (thinker is CeilingMove ceiling)
                {
                    w.WriteByte(ThinkerCeilingMove);
                    WriteCeilingMove(w, ceiling);
                }
                else if (thinker is LightFlash flash)
                {
                    w.WriteByte(ThinkerLightFlash);
                    WriteLightFlash(w, flash);
                }
                else if (thinker is StrobeFlash strobe)
                {
                    w.WriteByte(ThinkerStrobeFlash);
                    WriteStrobeFlash(w, strobe);
                }
                else if (thinker is FireFlicker flicker)
                {
                    w.WriteByte(ThinkerFireFlicker);
                    WriteFireFlicker(w, flicker);
                }
                else if (thinker is GlowingLight glow)
                {
                    w.WriteByte(ThinkerGlowingLight);
                    WriteGlowingLight(w, glow);
                }
            }
            w.WriteByte(ThinkerEnd);

            return w.ToArray();
        }

        public static void Load(DoomGame game, string path)
        {
            LoadFromMemory(game, SboxManagedDoomFileSystem.ReadAllBytesFromData(path));
        }

        // Synchronously rebuilds the world from serialized bytes; used for
        // savegames and for co-op state resync (both peers load the same
        // bytes so the simulations come out bit-identical).
        public static void LoadFromMemory(DoomGame game, byte[] data)
        {
            var r = new SaveReader(data);
            var options = game.Options;

            // Header.
            r.ReadString(DescriptionSize); // description (not needed on load)

            var version = r.ReadBytes(8);
            for (var i = 0; i < VersionTag.Length; i++)
            {
                if (version[i] != VersionTag[i])
                {
                    throw new Exception("Save game version mismatch.");
                }
            }

            var skill = (GameSkill)r.ReadByte();
            var episode = r.ReadByte();
            var map = r.ReadByte();
            var gameTic = r.ReadInt32();
            var rngIndex = r.ReadByte();

            // World state.
            var levelTime = r.ReadInt32();
            var totalKills = r.ReadInt32();
            var totalItems = r.ReadInt32();
            var totalSecrets = r.ReadInt32();

            // Rebuild the world from WAD for this map.
            options.Skill = skill;
            options.Episode = episode;
            options.Map = map;
            game.DoLoadLevel_Internal();

            var world = game.World;
            var mapData = world.Map;

            // Apply world state.
            world.LevelTime = levelTime;
            world.TotalKills = totalKills;
            world.TotalItems = totalItems;
            world.TotalSecrets = totalSecrets;
            options.Random.Index = rngIndex;

            // Clear existing thinkers and sector/blockmap links before restoring.
            ClearWorldLinks(world);

            // Deferred mobj references for second-pass resolution.
            var mobjList = new List<Mobj>();
            var targetRefs = new List<int>();
            var tracerRefs = new List<int>();

            // Players.
            var playerMobjRefs = new int[Player.MaxPlayerCount];
            var playerAttackerRefs = new int[Player.MaxPlayerCount];
            for (var i = 0; i < Player.MaxPlayerCount; i++)
            {
                playerMobjRefs[i] = -1;
                playerAttackerRefs[i] = -1;
            }

            for (var i = 0; i < Player.MaxPlayerCount; i++)
            {
                var player = options.Players[i];
                var inGame = r.ReadBool();
                player.InGame = inGame;
                if (!inGame) continue;

                player.PlayerState = (PlayerState)r.ReadByte();
                player.ViewZ = new Fixed(r.ReadInt32());
                player.ViewHeight = new Fixed(r.ReadInt32());
                player.DeltaViewHeight = new Fixed(r.ReadInt32());
                player.Bob = new Fixed(r.ReadInt32());
                player.Health = r.ReadInt32();
                player.ArmorPoints = r.ReadInt32();
                player.ArmorType = r.ReadInt32();

                for (var p = 0; p < (int)PowerType.Count; p++)
                    player.Powers[p] = r.ReadInt32();
                for (var c = 0; c < (int)CardType.Count; c++)
                    player.Cards[c] = r.ReadBool();
                player.Backpack = r.ReadBool();

                for (var f = 0; f < Player.MaxPlayerCount; f++)
                    player.Frags[f] = r.ReadInt32();

                player.ReadyWeapon = (WeaponType)r.ReadInt32();
                player.PendingWeapon = (WeaponType)r.ReadInt32();

                for (var wp = 0; wp < (int)WeaponType.Count; wp++)
                    player.WeaponOwned[wp] = r.ReadBool();
                for (var a = 0; a < (int)AmmoType.Count; a++)
                    player.Ammo[a] = r.ReadInt32();
                for (var a = 0; a < (int)AmmoType.Count; a++)
                    player.MaxAmmo[a] = r.ReadInt32();

                player.AttackDown = r.ReadBool();
                player.UseDown = r.ReadBool();
                player.Cheats = (CheatFlags)r.ReadInt32();
                player.Refire = r.ReadInt32();
                player.KillCount = r.ReadInt32();
                player.ItemCount = r.ReadInt32();
                player.SecretCount = r.ReadInt32();
                player.DamageCount = r.ReadInt32();
                player.BonusCount = r.ReadInt32();
                player.ExtraLight = r.ReadInt32();
                player.FixedColorMap = r.ReadInt32();
                player.ColorMap = r.ReadInt32();

                for (var s = 0; s < (int)PlayerSprite.Count; s++)
                {
                    var psp = player.PlayerSprites[s];
                    var stateNum = r.ReadInt32();
                    psp.State = stateNum >= 0 ? DoomInfo.States[stateNum] : null;
                    psp.Tics = r.ReadInt32();
                    psp.Sx = new Fixed(r.ReadInt32());
                    psp.Sy = new Fixed(r.ReadInt32());
                }

                player.DidSecret = r.ReadBool();

                playerMobjRefs[i] = r.ReadInt32();
                playerAttackerRefs[i] = r.ReadInt32();
            }

            // Sectors.
            var sectorCount = r.ReadInt32();
            var sectors = mapData.Sectors;
            for (var i = 0; i < sectorCount && i < sectors.Length; i++)
            {
                sectors[i].FloorHeight = new Fixed(r.ReadInt32());
                sectors[i].CeilingHeight = new Fixed(r.ReadInt32());
                sectors[i].FloorFlat = r.ReadInt32();
                sectors[i].CeilingFlat = r.ReadInt32();
                sectors[i].LightLevel = r.ReadInt16();
                sectors[i].Special = (SectorSpecial)r.ReadInt16();
            }

            // LineDefs.
            var lineCount = r.ReadInt32();
            var lines = mapData.Lines;
            for (var i = 0; i < lineCount && i < lines.Length; i++)
            {
                lines[i].Flags = (LineFlags)r.ReadInt16();
                lines[i].Special = (LineSpecial)r.ReadInt16();
                lines[i].Tag = r.ReadInt16();
            }

            // Thinkers.
            while (true)
            {
                var thinkerType = r.ReadByte();
                if (thinkerType == ThinkerEnd) break;

                switch (thinkerType)
                {
                    case ThinkerMobj:
                        {
                            var mobj = ReadMobj(r, world, mobjList.Count, targetRefs, tracerRefs);
                            mobjList.Add(mobj);

                            world.Thinkers.Add(mobj);
                            world.ThingMovement.SetThingPosition(mobj);
                        }
                        break;

                    case ThinkerVerticalDoor:
                        {
                            var door = ReadVerticalDoor(r, world);
                            world.Thinkers.Add(door);
                            door.Sector.SpecialData = door;
                        }
                        break;

                    case ThinkerPlatform:
                        {
                            var plat = ReadPlatform(r, world);
                            world.Thinkers.Add(plat);
                            plat.Sector.SpecialData = plat;
                        }
                        break;

                    case ThinkerFloorMove:
                        {
                            var floor = ReadFloorMove(r, world);
                            world.Thinkers.Add(floor);
                            floor.Sector.SpecialData = floor;
                        }
                        break;

                    case ThinkerCeilingMove:
                        {
                            var ceiling = ReadCeilingMove(r, world);
                            world.Thinkers.Add(ceiling);
                            ceiling.Sector.SpecialData = ceiling;
                            world.SectorAction.AddActiveCeiling(ceiling);
                        }
                        break;

                    case ThinkerLightFlash:
                        {
                            var flash = ReadLightFlash(r, world);
                            world.Thinkers.Add(flash);
                        }
                        break;

                    case ThinkerStrobeFlash:
                        {
                            var strobe = ReadStrobeFlash(r, world);
                            world.Thinkers.Add(strobe);
                        }
                        break;

                    case ThinkerFireFlicker:
                        {
                            var flicker = ReadFireFlicker(r, world);
                            world.Thinkers.Add(flicker);
                        }
                        break;

                    case ThinkerGlowingLight:
                        {
                            var glow = ReadGlowingLight(r, world);
                            world.Thinkers.Add(glow);
                        }
                        break;

                    default:
                        throw new Exception("Unknown thinker type in save: " + thinkerType);
                }
            }

            // Second pass: resolve Mobj cross-references.
            for (var i = 0; i < mobjList.Count; i++)
            {
                var mobj = mobjList[i];
                mobj.Target = ResolveMobjRef(mobjList, targetRefs[i]);
                mobj.Tracer = ResolveMobjRef(mobjList, tracerRefs[i]);
            }

            // Restore player-to-mobj links.
            for (var i = 0; i < Player.MaxPlayerCount; i++)
            {
                var player = options.Players[i];
                if (!player.InGame) continue;

                player.Mobj = ResolveMobjRef(mobjList, playerMobjRefs[i]);
                player.Attacker = ResolveMobjRef(mobjList, playerAttackerRefs[i]);

                if (player.Mobj != null)
                {
                    player.Mobj.Player = player;
                }
            }

            // Set game state.
            game.RestoreAfterLoad(gameTic);
        }

        ////////////////////////////////////////////////////////////////
        // Clear world links before loading saved thinkers.
        ////////////////////////////////////////////////////////////////

        private static void ClearWorldLinks(World world)
        {
            // Remove all thinkers from the linked list.
            world.Thinkers.Reset();

            // Clear sector thing lists and specialData.
            var sectors = world.Map.Sectors;
            for (var i = 0; i < sectors.Length; i++)
            {
                sectors[i].ThingList = null;
                sectors[i].SpecialData = null;
            }

            // Clear blockmap thing lists.
            var thingLists = world.Map.BlockMap.ThingLists;
            for (var i = 0; i < thingLists.Length; i++)
            {
                thingLists[i] = null;
            }

            // Clear linedef specialData.
            var lines = world.Map.Lines;
            for (var i = 0; i < lines.Length; i++)
            {
                lines[i].SpecialData = null;
            }
        }

        ////////////////////////////////////////////////////////////////
        // Mobj reference helpers.
        ////////////////////////////////////////////////////////////////

        private static int WriteMobjRef(Dictionary<Mobj, int> table, Mobj mobj)
        {
            if (mobj == null) return -1;
            if (table.TryGetValue(mobj, out var idx)) return idx;
            return -1;
        }

        private static Mobj ResolveMobjRef(List<Mobj> mobjList, int index)
        {
            if (index < 0 || index >= mobjList.Count) return null;
            return mobjList[index];
        }

        ////////////////////////////////////////////////////////////////
        // Write individual thinker types.
        ////////////////////////////////////////////////////////////////

        private static void WriteMobj(SaveWriter w, Mobj mobj, Dictionary<Mobj, int> mobjTable)
        {
            w.WriteInt32(mobj.X.Data);
            w.WriteInt32(mobj.Y.Data);
            w.WriteInt32(mobj.Z.Data);
            w.WriteUInt32(mobj.Angle.Data);
            w.WriteInt32((int)mobj.Sprite);
            w.WriteInt32(mobj.Frame);
            w.WriteInt32(mobj.FloorZ.Data);
            w.WriteInt32(mobj.CeilingZ.Data);
            w.WriteInt32(mobj.Radius.Data);
            w.WriteInt32(mobj.Height.Data);
            w.WriteInt32(mobj.MomX.Data);
            w.WriteInt32(mobj.MomY.Data);
            w.WriteInt32(mobj.MomZ.Data);
            w.WriteInt32((int)mobj.Type);
            w.WriteInt32(mobj.Tics);
            w.WriteInt32(mobj.State.Number);
            w.WriteInt32((int)mobj.Flags);
            w.WriteInt32(mobj.Health);
            w.WriteInt32((int)mobj.MoveDir);
            w.WriteInt32(mobj.MoveCount);
            w.WriteInt32(WriteMobjRef(mobjTable, mobj.Target));
            w.WriteInt32(mobj.ReactionTime);
            w.WriteInt32(mobj.Threshold);

            // Player number (-1 if not a player body).
            if (mobj.Player != null)
                w.WriteInt32(mobj.Player.Number);
            else
                w.WriteInt32(-1);

            w.WriteInt32(mobj.LastLook);

            // SpawnPoint.
            if (mobj.SpawnPoint != null)
            {
                w.WriteBool(true);
                w.WriteInt32(mobj.SpawnPoint.X.Data);
                w.WriteInt32(mobj.SpawnPoint.Y.Data);
                w.WriteUInt32(mobj.SpawnPoint.Angle.Data);
                w.WriteInt32(mobj.SpawnPoint.Type);
                w.WriteInt32((int)mobj.SpawnPoint.Flags);
            }
            else
            {
                w.WriteBool(false);
            }

            w.WriteInt32(WriteMobjRef(mobjTable, mobj.Tracer));
            w.WriteByte((byte)mobj.ThinkerState);
        }

        private static void WriteVerticalDoor(SaveWriter w, VerticalDoor door)
        {
            w.WriteInt32((int)door.Type);
            w.WriteInt32(door.Sector.Number);
            w.WriteInt32(door.TopHeight.Data);
            w.WriteInt32(door.Speed.Data);
            w.WriteInt32(door.Direction);
            w.WriteInt32(door.TopWait);
            w.WriteInt32(door.TopCountDown);
            w.WriteByte((byte)door.ThinkerState);
        }

        private static void WritePlatform(SaveWriter w, Platform plat)
        {
            w.WriteInt32(plat.Sector.Number);
            w.WriteInt32(plat.Speed.Data);
            w.WriteInt32(plat.Low.Data);
            w.WriteInt32(plat.High.Data);
            w.WriteInt32(plat.Wait);
            w.WriteInt32(plat.Count);
            w.WriteInt32((int)plat.Status);
            w.WriteInt32((int)plat.OldStatus);
            w.WriteBool(plat.Crush);
            w.WriteInt32(plat.Tag);
            w.WriteInt32((int)plat.Type);
            w.WriteByte((byte)plat.ThinkerState);
        }

        private static void WriteFloorMove(SaveWriter w, FloorMove floor)
        {
            w.WriteInt32((int)floor.Type);
            w.WriteBool(floor.Crush);
            w.WriteInt32(floor.Sector.Number);
            w.WriteInt32(floor.Direction);
            w.WriteInt32((int)floor.NewSpecial);
            w.WriteInt32(floor.Texture);
            w.WriteInt32(floor.FloorDestHeight.Data);
            w.WriteInt32(floor.Speed.Data);
            w.WriteByte((byte)floor.ThinkerState);
        }

        private static void WriteCeilingMove(SaveWriter w, CeilingMove ceiling)
        {
            w.WriteInt32((int)ceiling.Type);
            w.WriteInt32(ceiling.Sector.Number);
            w.WriteInt32(ceiling.BottomHeight.Data);
            w.WriteInt32(ceiling.TopHeight.Data);
            w.WriteInt32(ceiling.Speed.Data);
            w.WriteBool(ceiling.Crush);
            w.WriteInt32(ceiling.Direction);
            w.WriteInt32(ceiling.Tag);
            w.WriteInt32(ceiling.OldDirection);
            w.WriteByte((byte)ceiling.ThinkerState);
        }

        private static void WriteLightFlash(SaveWriter w, LightFlash flash)
        {
            w.WriteInt32(flash.Sector.Number);
            w.WriteInt32(flash.Count);
            w.WriteInt32(flash.MaxLight);
            w.WriteInt32(flash.MinLight);
            w.WriteInt32(flash.MaxTime);
            w.WriteInt32(flash.MinTime);
            w.WriteByte((byte)flash.ThinkerState);
        }

        private static void WriteStrobeFlash(SaveWriter w, StrobeFlash strobe)
        {
            w.WriteInt32(strobe.Sector.Number);
            w.WriteInt32(strobe.Count);
            w.WriteInt32(strobe.MinLight);
            w.WriteInt32(strobe.MaxLight);
            w.WriteInt32(strobe.DarkTime);
            w.WriteInt32(strobe.BrightTime);
            w.WriteByte((byte)strobe.ThinkerState);
        }

        private static void WriteFireFlicker(SaveWriter w, FireFlicker flicker)
        {
            w.WriteInt32(flicker.Sector.Number);
            w.WriteInt32(flicker.Count);
            w.WriteInt32(flicker.MaxLight);
            w.WriteInt32(flicker.MinLight);
            w.WriteByte((byte)flicker.ThinkerState);
        }

        private static void WriteGlowingLight(SaveWriter w, GlowingLight glow)
        {
            w.WriteInt32(glow.Sector.Number);
            w.WriteInt32(glow.MinLight);
            w.WriteInt32(glow.MaxLight);
            w.WriteInt32(glow.Direction);
            w.WriteByte((byte)glow.ThinkerState);
        }

        ////////////////////////////////////////////////////////////////
        // Read individual thinker types.
        ////////////////////////////////////////////////////////////////

        private static Mobj ReadMobj(
            SaveReader r, World world,
            int mobjIndex, List<int> targetRefs, List<int> tracerRefs)
        {
            var mobj = new Mobj(world);

            mobj.X = new Fixed(r.ReadInt32());
            mobj.Y = new Fixed(r.ReadInt32());
            mobj.Z = new Fixed(r.ReadInt32());
            mobj.Angle = new Angle(r.ReadUInt32());
            mobj.Sprite = (Sprite)r.ReadInt32();
            mobj.Frame = r.ReadInt32();
            mobj.FloorZ = new Fixed(r.ReadInt32());
            mobj.CeilingZ = new Fixed(r.ReadInt32());
            mobj.Radius = new Fixed(r.ReadInt32());
            mobj.Height = new Fixed(r.ReadInt32());
            mobj.MomX = new Fixed(r.ReadInt32());
            mobj.MomY = new Fixed(r.ReadInt32());
            mobj.MomZ = new Fixed(r.ReadInt32());
            mobj.Type = (MobjType)r.ReadInt32();
            mobj.Info = DoomInfo.MobjInfos[(int)mobj.Type];
            mobj.Tics = r.ReadInt32();
            mobj.State = DoomInfo.States[r.ReadInt32()];
            mobj.Flags = (MobjFlags)r.ReadInt32();
            mobj.Health = r.ReadInt32();
            mobj.MoveDir = (Direction)r.ReadInt32();
            mobj.MoveCount = r.ReadInt32();

            // Target deferred.
            targetRefs.Add(r.ReadInt32());

            mobj.ReactionTime = r.ReadInt32();
            mobj.Threshold = r.ReadInt32();

            // Player number (resolved later).
            var playerNum = r.ReadInt32();
            // Player link is set after all thinkers are loaded.

            mobj.LastLook = r.ReadInt32();

            // SpawnPoint.
            var hasSpawnPoint = r.ReadBool();
            if (hasSpawnPoint)
            {
                mobj.SpawnPoint = new MapThing(
                    new Fixed(r.ReadInt32()),
                    new Fixed(r.ReadInt32()),
                    new Angle(r.ReadUInt32()),
                    r.ReadInt32(),
                    (ThingFlags)r.ReadInt32());
            }

            // Tracer deferred.
            tracerRefs.Add(r.ReadInt32());

            mobj.ThinkerState = (ThinkerState)r.ReadByte();

            return mobj;
        }

        private static VerticalDoor ReadVerticalDoor(SaveReader r, World world)
        {
            var door = new VerticalDoor(world);
            door.Type = (VerticalDoorType)r.ReadInt32();
            door.Sector = world.Map.Sectors[r.ReadInt32()];
            door.TopHeight = new Fixed(r.ReadInt32());
            door.Speed = new Fixed(r.ReadInt32());
            door.Direction = r.ReadInt32();
            door.TopWait = r.ReadInt32();
            door.TopCountDown = r.ReadInt32();
            door.ThinkerState = (ThinkerState)r.ReadByte();
            return door;
        }

        private static Platform ReadPlatform(SaveReader r, World world)
        {
            var plat = new Platform(world);
            plat.Sector = world.Map.Sectors[r.ReadInt32()];
            plat.Speed = new Fixed(r.ReadInt32());
            plat.Low = new Fixed(r.ReadInt32());
            plat.High = new Fixed(r.ReadInt32());
            plat.Wait = r.ReadInt32();
            plat.Count = r.ReadInt32();
            plat.Status = (PlatformState)r.ReadInt32();
            plat.OldStatus = (PlatformState)r.ReadInt32();
            plat.Crush = r.ReadBool();
            plat.Tag = r.ReadInt32();
            plat.Type = (PlatformType)r.ReadInt32();
            plat.ThinkerState = (ThinkerState)r.ReadByte();
            return plat;
        }

        private static FloorMove ReadFloorMove(SaveReader r, World world)
        {
            var floor = new FloorMove(world);
            floor.Type = (FloorMoveType)r.ReadInt32();
            floor.Crush = r.ReadBool();
            floor.Sector = world.Map.Sectors[r.ReadInt32()];
            floor.Direction = r.ReadInt32();
            floor.NewSpecial = (SectorSpecial)r.ReadInt32();
            floor.Texture = r.ReadInt32();
            floor.FloorDestHeight = new Fixed(r.ReadInt32());
            floor.Speed = new Fixed(r.ReadInt32());
            floor.ThinkerState = (ThinkerState)r.ReadByte();
            return floor;
        }

        private static CeilingMove ReadCeilingMove(SaveReader r, World world)
        {
            var ceiling = new CeilingMove(world);
            ceiling.Type = (CeilingMoveType)r.ReadInt32();
            ceiling.Sector = world.Map.Sectors[r.ReadInt32()];
            ceiling.BottomHeight = new Fixed(r.ReadInt32());
            ceiling.TopHeight = new Fixed(r.ReadInt32());
            ceiling.Speed = new Fixed(r.ReadInt32());
            ceiling.Crush = r.ReadBool();
            ceiling.Direction = r.ReadInt32();
            ceiling.Tag = r.ReadInt32();
            ceiling.OldDirection = r.ReadInt32();
            ceiling.ThinkerState = (ThinkerState)r.ReadByte();
            return ceiling;
        }

        private static LightFlash ReadLightFlash(SaveReader r, World world)
        {
            var flash = new LightFlash(world);
            flash.Sector = world.Map.Sectors[r.ReadInt32()];
            flash.Count = r.ReadInt32();
            flash.MaxLight = r.ReadInt32();
            flash.MinLight = r.ReadInt32();
            flash.MaxTime = r.ReadInt32();
            flash.MinTime = r.ReadInt32();
            flash.ThinkerState = (ThinkerState)r.ReadByte();
            return flash;
        }

        private static StrobeFlash ReadStrobeFlash(SaveReader r, World world)
        {
            var strobe = new StrobeFlash(world);
            strobe.Sector = world.Map.Sectors[r.ReadInt32()];
            strobe.Count = r.ReadInt32();
            strobe.MinLight = r.ReadInt32();
            strobe.MaxLight = r.ReadInt32();
            strobe.DarkTime = r.ReadInt32();
            strobe.BrightTime = r.ReadInt32();
            strobe.ThinkerState = (ThinkerState)r.ReadByte();
            return strobe;
        }

        private static FireFlicker ReadFireFlicker(SaveReader r, World world)
        {
            var flicker = new FireFlicker(world);
            flicker.Sector = world.Map.Sectors[r.ReadInt32()];
            flicker.Count = r.ReadInt32();
            flicker.MaxLight = r.ReadInt32();
            flicker.MinLight = r.ReadInt32();
            flicker.ThinkerState = (ThinkerState)r.ReadByte();
            return flicker;
        }

        private static GlowingLight ReadGlowingLight(SaveReader r, World world)
        {
            var glow = new GlowingLight(world);
            glow.Sector = world.Map.Sectors[r.ReadInt32()];
            glow.MinLight = r.ReadInt32();
            glow.MaxLight = r.ReadInt32();
            glow.Direction = r.ReadInt32();
            glow.ThinkerState = (ThinkerState)r.ReadByte();
            return glow;
        }

        ////////////////////////////////////////////////////////////////
        // Binary writer / reader helpers.
        ////////////////////////////////////////////////////////////////

        private sealed class SaveWriter
        {
            private List<byte> buffer;

            public SaveWriter()
            {
                buffer = new List<byte>(65536);
            }

            public void WriteByte(byte value)
            {
                buffer.Add(value);
            }

            public void WriteBool(bool value)
            {
                buffer.Add(value ? (byte)1 : (byte)0);
            }

            public void WriteInt16(short value)
            {
                buffer.Add((byte)(value & 0xFF));
                buffer.Add((byte)((value >> 8) & 0xFF));
            }

            public void WriteInt32(int value)
            {
                buffer.Add((byte)(value & 0xFF));
                buffer.Add((byte)((value >> 8) & 0xFF));
                buffer.Add((byte)((value >> 16) & 0xFF));
                buffer.Add((byte)((value >> 24) & 0xFF));
            }

            public void WriteUInt32(uint value)
            {
                buffer.Add((byte)(value & 0xFF));
                buffer.Add((byte)((value >> 8) & 0xFF));
                buffer.Add((byte)((value >> 16) & 0xFF));
                buffer.Add((byte)((value >> 24) & 0xFF));
            }

            public void WriteString(string value, int length)
            {
                for (var i = 0; i < length; i++)
                {
                    if (value != null && i < value.Length)
                        buffer.Add((byte)value[i]);
                    else
                        buffer.Add(0);
                }
            }

            public void WriteBytes(byte[] data)
            {
                for (var i = 0; i < data.Length; i++)
                    buffer.Add(data[i]);
            }

            public byte[] ToArray()
            {
                return buffer.ToArray();
            }
        }

        private sealed class SaveReader
        {
            private byte[] data;
            private int pos;

            public SaveReader(byte[] data)
            {
                this.data = data;
                pos = 0;
            }

            public byte ReadByte()
            {
                return data[pos++];
            }

            public bool ReadBool()
            {
                return data[pos++] != 0;
            }

            public short ReadInt16()
            {
                var value = (short)(data[pos] | (data[pos + 1] << 8));
                pos += 2;
                return value;
            }

            public int ReadInt32()
            {
                var value = data[pos]
                    | (data[pos + 1] << 8)
                    | (data[pos + 2] << 16)
                    | (data[pos + 3] << 24);
                pos += 4;
                return value;
            }

            public uint ReadUInt32()
            {
                var value = (uint)(data[pos]
                    | (data[pos + 1] << 8)
                    | (data[pos + 2] << 16)
                    | (data[pos + 3] << 24));
                pos += 4;
                return value;
            }

            public string ReadString(int length)
            {
                var chars = new char[length];
                var realLen = 0;
                for (var i = 0; i < length; i++)
                {
                    var b = data[pos++];
                    if (b != 0 && realLen == i)
                    {
                        chars[i] = (char)b;
                        realLen = i + 1;
                    }
                }
                return new string(chars, 0, realLen);
            }

            public byte[] ReadBytes(int length)
            {
                var result = new byte[length];
                Array.Copy(data, pos, result, 0, length);
                pos += length;
                return result;
            }
        }
    }
}
