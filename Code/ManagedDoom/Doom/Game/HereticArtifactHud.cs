// s&Doom modification: 2026-09-22, suppress Tome icon for super-chicken power.
// s&Doom modification: 2026-09-22, tenth artifact slot for Morph Ovum.
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

// Adapted 2026-09-22: artifact artwork and Tome animation from pinned sb_bar.c.
// Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
using System;
using System.Linq;
using ManagedDoom.Video;
namespace ManagedDoom
{
    // The preview exposes its ten supported artifacts as fixed shortcut slots.
    // This is an overview, not the reference's scrolling seven-slot selector.
    internal sealed class HereticArtifactHud
    {
        private readonly Patch box;
        private readonly Patch[] icons, digits, keys, books;
        private const string Shortcuts = "QUGITHBJKL";
        private static readonly string[] IconNames =
        {
            "ARTIPTN2", "ARTISPHL", "ARTISOAR", "ARTIINVU", "ARTITRCH",
            "ARTIATLP", "ARTIFBMB", "ARTIINVS", "ARTIPWBK", "ARTIEGGC"
        };
        internal HereticArtifactHud(Wad wad)
        {
            box = Patch.FromWad(wad, "ARTIBOX");
            icons = IconNames.Select(n => Patch.FromWad(wad, n)).ToArray();
            digits = Enumerable.Range(0, 10).Select(i => Patch.FromWad(wad, "SMALLIN" + i)).ToArray();
            books = Enumerable.Range(0, 16).Select(i => Patch.FromWad(wad, "SPINBK" + i)).ToArray();
            keys = Shortcuts.Select(c => Patch.FromWad(wad, "FONTA" + (c - 32).ToString("00"))).ToArray();
        }
        internal void Render(HereticWorldSession session, DrawScreen screen, bool inventoryVisible)
        {
            var state = session.State;
            if (state.Health <= 0) return;
            if (state.ChickenTics == 0 && state.WeaponPowerTics > 0 && (state.WeaponPowerTics > 128 || (state.WeaponPowerTics & 16) == 0))
                screen.DrawPatch(books[(session.World.LevelTime / 3) & 15], 300, 17, 1);
            if (!inventoryVisible) return;
            var counts = new[] { state.QuartzFlasks, state.MysticUrns, state.WingsOfWrath,
                state.RingsOfInvincibility, state.Torches, state.ChaosDevices, state.TimeBombs,
                state.Shadowspheres, state.TomesOfPower, state.MorphOvums };
            for (var i = 0; i < icons.Length; i++)
            {
                var x = 5 + 31 * i;
                screen.DrawPatch(keys[i], x + 11, 157, 1);
                screen.DrawPatch(box, x, 168, 1);
                screen.DrawPatch(icons[i], x, 168, 1);
                // Show even zero/one counts so fixed empty slots are unambiguous.
                var count = Math.Clamp(counts[i], 0, 16);
                if (count >= 10) screen.DrawPatch(digits[count / 10], x + 19, 190, 1);
                screen.DrawPatch(digits[count % 10], x + 23, 190, 1);
            }
        }
    }
}
