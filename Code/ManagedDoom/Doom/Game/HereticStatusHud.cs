// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using System;
using System.Collections.Generic;
using ManagedDoom.Video;
namespace ManagedDoom
{
    // Compact preview readout using the bundled IWAD font. Not the original status bar.
    internal sealed class HereticStatusHud
    {
        private readonly Dictionary<char, Patch> font = new();
        internal HereticStatusHud(Wad wad)
        {
            foreach (var c in "HEALTHRMOBEAK0123456789-")
                if (!font.ContainsKey(c)) font.Add(c, Patch.FromWad(wad, "FONTA" + (c - 32).ToString("00")));
        }
        internal static int? CurrentAmmo(HereticGoldWand weapon) => weapon.ReadyWeapon switch
        {
            HereticWeapon.wp_goldwand => weapon.Ammo,
            HereticWeapon.wp_crossbow => weapon.CrossbowAmmo,
            HereticWeapon.wp_blaster => weapon.BlasterAmmo,
            HereticWeapon.wp_skullrod => weapon.SkullRodAmmo,
            HereticWeapon.wp_phoenixrod => weapon.PhoenixAmmo,
            HereticWeapon.wp_mace => weapon.MaceAmmo,
            _ => null
        };
        internal void Render(HereticWorldSession session, DrawScreen screen)
        {
            if (session.GoldWand == null) return;
            Draw(screen, "HEALTH " + Math.Clamp(session.State.Health, 0, 999), 5);
            Draw(screen, "ARMOR " + Math.Clamp(session.State.ArmorPoints, 0, 999), 112);
            var ammo = CurrentAmmo(session.GoldWand);
            Draw(screen, session.State.ChickenTics > 0 ? "BEAK" : "AMMO " + (ammo.HasValue ? Math.Clamp(ammo.Value, 0, 999).ToString() : "-"), 222);
        }
        private void Draw(DrawScreen screen, string text, int x)
        {
            foreach (var c in text)
            {
                if (c == ' ') { x += 4; continue; }
                var patch = font[c];
                screen.DrawPatch(patch, x + patch.LeftOffset, 142 + patch.TopOffset, 1);
                x += patch.Width + 1;
            }
        }
    }
}
