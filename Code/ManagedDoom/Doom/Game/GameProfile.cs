// s&Doom modification: 2026-09-24, recognize renamed Heretic base content.
// Copyright (C) 2026 s&Doom contributors.
// SPDX-License-Identifier: GPL-2.0-or-later

using System;

namespace ManagedDoom
{
    public enum GameFamily
    {
        Doom,
        Heretic
    }

    /// <summary>
    /// Selects game rules independently of Doom edition, mission pack and WAD.
    /// Profiles are immutable and belong to content, never to mutable global state.
    /// </summary>
    public abstract class GameProfile
    {
        public static GameProfile Doom { get; } = new DoomProfile();
        public static GameProfile Heretic { get; } = new HereticProfile();

        private GameProfile() { }

        public abstract GameFamily Family { get; }
        public abstract string Id { get; }
        public abstract bool RuntimeSupported { get; }

        public void EnsureRuntimeSupported()
        {
            if (!RuntimeSupported)
                throw new NotSupportedException("Heretic/Blasphemer gameplay is not implemented yet. This content cannot run with Doom rules.");
        }

        internal abstract void InitializeDefinitions(CommandLineArgs args, Wad wad);

        internal static GameProfile Select(Wad wad, GameProfile requested)
        {
            // Only the base WAD selects the game family. PWAD filenames must
            // not silently switch the rules of an existing Doom game.
            // Full Heretic asset validation belongs to the content loader.
            var name = wad.Names.Count == 0 ? string.Empty : wad.Names[0];
            // Conservative classic-Heretic signature. Names remain a fallback for known files;
            // explicit Heretic selection remains available for unusual compatible content.
            var hereticContent = wad.BaseContainsLump("E1M1") && wad.BaseContainsLump("MUS_E1M1") &&
                wad.BaseContainsLump("M_HTIC") && wad.BaseContainsLump("ARTIBOX") && wad.BaseContainsLump("SPFLY0");
            var knownHeretic = hereticContent || name == "heretic" || name == "heretic1" ||
                name == "blasphem" || name == "blasphemer" || name == "blasphdm";
            if (knownHeretic && requested == Doom)
                throw new ArgumentException("A Heretic base WAD cannot select the Doom profile.", nameof(requested));
            return requested ?? (knownHeretic ? Heretic : Doom);
        }

        private sealed class DoomProfile : GameProfile
        {
            public override GameFamily Family => GameFamily.Doom;
            public override string Id => "doom";
            public override bool RuntimeSupported => true;
            internal override void InitializeDefinitions(CommandLineArgs args, Wad wad)
            {
                DeHackEd.Initialize(args, wad);
            }
        }

        private sealed class HereticProfile : GameProfile
        {
            public override GameFamily Family => GameFamily.Heretic;
            public override string Id => "heretic";
            public override bool RuntimeSupported => false;
            internal override void InitializeDefinitions(CommandLineArgs args, Wad wad)
            {
                // Do not reset or patch DoomInfo while rejecting another family.
                EnsureRuntimeSupported();
            }
        }
    }
}
