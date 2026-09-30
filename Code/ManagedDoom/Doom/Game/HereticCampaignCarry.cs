// s&Doom modification: 2026-09-29, single-player death restart with fresh loadout.
// s&Doom modification: 2026-09-29, native single-player campaign inventory carry.
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

// Carry rules adapted from pinned Heretic g_game.c G_PlayerFinishLevel.
// Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
namespace ManagedDoom
{
    internal sealed class HereticCampaignCarry
    {
        private readonly HereticPlayerState player;
        private readonly HereticWeaponCarry weapons;
        internal HereticCampaignCarry(HereticWorldSession source, HereticWeapon restoredWeapon)
        {
            var state = source.State;
            player = new HereticPlayerState
            {
                Health = state.Health,
                ArmorType = state.ArmorType,
                ArmorPoints = state.ArmorPoints,
                QuartzFlasks = System.Math.Min(1, state.QuartzFlasks),
                MysticUrns = System.Math.Min(1, state.MysticUrns),
                RingsOfInvincibility = System.Math.Min(1, state.RingsOfInvincibility),
                Torches = System.Math.Min(1, state.Torches),
                ChaosDevices = System.Math.Min(1, state.ChaosDevices),
                TimeBombs = System.Math.Min(1, state.TimeBombs),
                Shadowspheres = System.Math.Min(1, state.Shadowspheres),
                TomesOfPower = System.Math.Min(1, state.TomesOfPower),
                MorphOvums = System.Math.Min(1, state.MorphOvums),
            };
            weapons = source.GoldWand?.CaptureCampaignWeapons(restoredWeapon);
        }
        internal void Apply(HereticWorldSession destination)
        {
            destination.State.Health = player.Health;
            destination.State.ArmorType = player.ArmorType;
            destination.State.ArmorPoints = player.ArmorPoints;
            destination.State.QuartzFlasks = player.QuartzFlasks;
            destination.State.MysticUrns = player.MysticUrns;
            destination.State.RingsOfInvincibility = player.RingsOfInvincibility;
            destination.State.Torches = player.Torches;
            destination.State.ChaosDevices = player.ChaosDevices;
            destination.State.TimeBombs = player.TimeBombs;
            destination.State.Shadowspheres = player.Shadowspheres;
            destination.State.TomesOfPower = player.TomesOfPower;
            destination.State.MorphOvums = player.MorphOvums;
            destination.Body.Health = player.Health;
            if (weapons != null) destination.RestoreCampaignWeapons(weapons);
        }
    }
    internal sealed record HereticWeaponCarry(
        bool HasMace,
        bool HasPhoenix,
        bool HasSkullRod,
        bool HasCrossbow,
        bool HasGauntlets,
        bool HasBlaster,
        bool HasBagOfHolding,
        int MaceAmmo,
        int PhoenixAmmo,
        int SkullRodAmmo,
        int CrossbowAmmo,
        int BlasterAmmo,
        int Ammo,
        HereticWeapon ReadyWeapon);
    public sealed partial class HereticGoldWand
    {
        internal HereticWeaponCarry CaptureCampaignWeapons(HereticWeapon ready) => new(
            HasMace, HasPhoenix, HasSkullRod, HasCrossbow, HasGauntlets, HasBlaster, HasBagOfHolding, MaceAmmo, PhoenixAmmo, SkullRodAmmo, CrossbowAmmo, BlasterAmmo, Ammo, ready);
        internal void ApplyCampaignWeapons(HereticWeaponCarry carry)
        {
            HasMace = carry.HasMace;
            HasPhoenix = carry.HasPhoenix;
            HasSkullRod = carry.HasSkullRod;
            HasCrossbow = carry.HasCrossbow;
            HasGauntlets = carry.HasGauntlets;
            HasBlaster = carry.HasBlaster;
            HasBagOfHolding = carry.HasBagOfHolding;
            MaceAmmo = carry.MaceAmmo;
            PhoenixAmmo = carry.PhoenixAmmo;
            SkullRodAmmo = carry.SkullRodAmmo;
            CrossbowAmmo = carry.CrossbowAmmo;
            BlasterAmmo = carry.BlasterAmmo;
            Ammo = carry.Ammo;
            RestoreAfterChicken(carry.ReadyWeapon);
        }
    }
    public sealed partial class HereticWorldSession
    {
        private HereticCampaignCarry campaignCarry;
        private readonly GameContent campaignContent;
        private HereticWorldSession nextCampaignSession;
        private HereticWorldSession restartedSession;
        public HereticWorldSession RestartAfterDeath()
        {
            if (World.Options.NetGame)
                throw new System.NotSupportedException("Heretic multiplayer respawn is not implemented.");
            if (State.Health > 0 || ExitRequested)
                throw new System.InvalidOperationException("Only an unfinished dead-player session can restart.");
            if (restartedSession != null) return restartedSession;
            var next = new HereticWorldSession(campaignContent, campaignEpisode, campaignMap, skill);
            if (mapCombatEnabled) next.StartMapCombat();
            else if (GoldWand != null)
            {
                next.GoldWand = new HereticGoldWand(next);
                next.EnableCombatAmmo();
            }
            restartedSession = next;
            return next;
        }
        internal void RestoreCampaignWeapons(HereticWeaponCarry carry)
        {
            GoldWand = new HereticGoldWand(this);
            GoldWand.ApplyCampaignWeapons(carry);
            EnableCombatAmmo();
        }
        public HereticWorldSession CreateNextCampaignSession()
        {
            if (!ExitRequested || Completion?.Kind != HereticExitKind.NextMap || campaignCarry == null)
                throw new System.InvalidOperationException("No playable campaign destination is pending.");
            if (World.Options.NetGame)
                throw new System.NotSupportedException("Heretic campaign multiplayer is not implemented.");
            if (nextCampaignSession != null) return nextCampaignSession;
            // Construct and restore fully before publishing; load failures leave this session intact.
            var next = new HereticWorldSession(campaignContent, campaignEpisode, Completion.Value.NextMap.Value, skill);
            campaignCarry.Apply(next);
            if (mapCombatEnabled) next.StartMapCombat();
            nextCampaignSession = next;
            return next;
        }
    }
}
