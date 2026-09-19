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

// Adapted 2026-09-18: asset names and animation definitions only.
// Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28, info.c and p_spec.c.
using System;

namespace ManagedDoom
{
    public static class HereticAssets
    {
        public static readonly string[] SpriteNames = { "IMPX", "ACLO", "PTN1", "SHLD", "SHD2", "BAGH", "SPMP", "INVS", "PTN2", "SOAR", "INVU", "PWBK", "EGGC", "EGGM", "FX01", "SPHL", "TRCH", "FBMB", "XPL1", "ATLP", "PPOD", "AMG1", "SPSH", "LVAS", "SLDG", "SKH1", "SKH2", "SKH3", "SKH4", "CHDL", "SRTC", "SMPL", "STGS", "STGL", "STCS", "STCL", "KFR1", "BARL", "BRPL", "MOS1", "MOS2", "WTRH", "HCOR", "KGZ1", "KGZB", "KGZG", "KGZY", "VLCO", "VFBL", "VTFB", "SFFI", "TGLT", "TELE", "STFF", "PUF3", "PUF4", "BEAK", "WGNT", "GAUN", "PUF1", "WBLS", "BLSR", "FX18", "FX17", "WMCE", "MACE", "FX02", "WSKL", "HROD", "FX00", "FX20", "FX21", "FX22", "FX23", "GWND", "PUF2", "WPHX", "PHNX", "FX04", "FX08", "FX09", "WBOW", "CRBW", "FX03", "BLOD", "PLAY", "FDTH", "BSKL", "CHKN", "MUMM", "FX15", "BEAS", "FRB1", "SNKE", "SNFX", "HEAD", "FX05", "FX06", "FX07", "CLNK", "WZRD", "FX11", "FX10", "KNIG", "SPAX", "RAXE", "SRCR", "FX14", "SOR2", "SDTH", "FX16", "MNTR", "FX12", "FX13", "AKYY", "BKYY", "CKYY", "AMG2", "AMM1", "AMM2", "AMC1", "AMC2", "AMS1", "AMS2", "AMP1", "AMP2", "AMB1", "AMB2" };
        public static readonly AnimationDef[] Animations =
        {
            new AnimationDef(false, "FLTWAWA3", "FLTWAWA1", 8),
            new AnimationDef(false, "FLTSLUD3", "FLTSLUD1", 8),
            new AnimationDef(false, "FLTTELE4", "FLTTELE1", 6),
            new AnimationDef(false, "FLTFLWW3", "FLTFLWW1", 9),
            new AnimationDef(false, "FLTLAVA4", "FLTLAVA1", 8),
            new AnimationDef(false, "FLATHUH4", "FLATHUH1", 8),
            new AnimationDef(true, "LAVAFL3", "LAVAFL1", 6),
            new AnimationDef(true, "WATRWAL3", "WATRWAL1", 4),
        };
        public static void Validate(Wad wad)
        {
            foreach (var name in new[] { "PLAYPAL", "COLORMAP", "PNAMES", "TEXTURE1", "F_START", "F_END", "S_START", "S_END", "F_SKY1" })
                if (wad.GetLumpNumber(name) < 0) throw new InvalidOperationException("Heretic assets require lump " + name + ".");
            if (wad.ReadLump("PLAYPAL").Length < 768 || wad.ReadLump("PLAYPAL").Length % 768 != 0)
                throw new InvalidOperationException("Heretic PLAYPAL must contain complete 256-color palettes.");
            if (wad.ReadLump("COLORMAP").Length < 34 * 256)
                throw new InvalidOperationException("Heretic COLORMAP requires 34 lighting tables.");
        }
        public static void ValidateMap(Wad wad, string name)
        {
            var marker = wad.GetLumpNumber(name);
            if (marker < 0) throw new InvalidOperationException("Heretic map " + name + " is missing.");
            var names = new[] { "THINGS", "LINEDEFS", "SIDEDEFS", "VERTEXES", "SEGS", "SSECTORS", "NODES", "SECTORS", "REJECT", "BLOCKMAP" };
            for (var i = 0; i < names.Length; i++)
                if (marker + i + 1 >= wad.LumpInfos.Count || wad.LumpInfos[marker + i + 1].Name != names[i])
                    throw new NotSupportedException(name + ": expected " + names[i] + "; only classic Heretic binary maps are supported (not UDMF or extended nodes).");
            if (marker + 11 < wad.LumpInfos.Count && wad.LumpInfos[marker + 11].Name == "BEHAVIOR")
                throw new NotSupportedException(name + ": Hexen-format BEHAVIOR maps are not supported.");
            var sizes = new[] { 10, 14, 30, 4, 12, 4, 28, 26 };
            for (var i = 0; i < sizes.Length; i++)
                if (wad.GetLumpSize(marker + i + 1) % sizes[i] != 0)
                    throw new InvalidOperationException(name + ": malformed " + names[i] + " records.");
        }
    }
}
