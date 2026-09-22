// s&Doom modification: 2026-09-22, opt-in native Clink test encounter integration.
//
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

// P_user.c

// Adapted 2026-09-18 for the shared ManagedDoom engine.
// Reference: Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
using System;
namespace ManagedDoom
{
    [Flags] public enum HereticKeys { None = 0, Yellow = 1, Green = 2, Blue = 4 }
    public enum HereticArtifact { QuartzFlask, MysticUrn, WingsOfWrath, RingOfInvincibility, Torch, ChaosDevice, TimeBomb }
    public struct HereticCommand
    {
        public sbyte Forward, Side, Look, Fly;
        public short Turn;
        public bool Use, CenterLook, Land, TestAttack;
        public HereticWeapon? SelectWeapon;
        public HereticArtifact? UseArtifact;
    }
    public sealed class HereticPlayerState
    {
        public int TimeBombs { get; internal set; }
        public int ChaosDevices { get; internal set; }
        public int Torches { get; internal set; }
        public int TorchTics { get; internal set; }
        public int RingsOfInvincibility { get; internal set; }
        public int InvulnerabilityTics { get; internal set; }
        public int WingsOfWrath { get; internal set; }
        public int QuartzFlasks { get; internal set; }
        public int MysticUrns { get; internal set; }
        public HereticKeys Keys { get; internal set; }
        public int LookDirection { get; internal set; }
        public bool Centering { get; internal set; }
        public bool Flying { get; internal set; }
        public int FlightTics { get; internal set; }
        public int FlyHeight { get; internal set; }
        public int DamageFlash { get; internal set; }
        public int PickupFlash { get; internal set; }
        // Adapted 2026-09-22: pinned sb_bar.c SB_PaletteFlash palette selection.
        public int PaletteIndex => DamageFlash > 0 ? 1 + Math.Min(7, (DamageFlash + 7) >> 3)
            : PickupFlash > 0 ? 9 + (int)Math.Min(3L, ((long)PickupFlash + 7) >> 3) : 0;
        public int ArmorType { get; internal set; }
        public int ArmorPoints { get; internal set; }
        public int Health { get; internal set; } = 100;
        public int Secrets { get; internal set; }
        public string Message { get; internal set; } = "";
    }
}
