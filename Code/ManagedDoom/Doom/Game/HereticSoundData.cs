// s&Doom modification: 2026-09-22, powered Dragon Claw and radial rippers.
// s&Doom modification: 2026-09-22, opt-in powered staff attack, thrust and effects.
// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using System;
namespace ManagedDoom
{
    public readonly record struct HereticSoundData(int SampleRate, short[] Samples)
    {
        // Names for the currently enabled encounter, checked against pinned Heretic sounds.c.
        public static string LumpName(HereticSoundId sound) => sound switch
        {
            HereticSoundId.sfx_bowsht => "BOWSHT",
            HereticSoundId.sfx_telept => "TELEPT",
            HereticSoundId.sfx_artiup => "ARTIUP",
            HereticSoundId.sfx_artiuse => "ARTIUSE",
            HereticSoundId.sfx_lobsht => "LOBSHT",
            HereticSoundId.sfx_bounce => "BOUNCE",
            HereticSoundId.sfx_lobhit => "LOBHIT",
            HereticSoundId.sfx_gloop => "GLOOP",
            HereticSoundId.sfx_burn => "BURN",
            HereticSoundId.sfx_phosht => "PHOSHT",
            HereticSoundId.sfx_phohit => "PHOHIT",
            HereticSoundId.sfx_hrnsht => "HRNSHT",
            HereticSoundId.sfx_hrnhit => "HRNHIT",
            HereticSoundId.sfx_gntact => "GNTACT",
            HereticSoundId.sfx_gntuse => "GNTUSE",
            HereticSoundId.sfx_gntful => "GNTFUL",
            HereticSoundId.sfx_gnthit => "GNTHIT",
            HereticSoundId.sfx_wpnup => "WPNUP",
            HereticSoundId.sfx_itemup => "ITEMUP",
            HereticSoundId.sfx_gldhit => "GLDHIT",
            HereticSoundId.sfx_blssht => "BLSSHT",
            HereticSoundId.sfx_blshit => "BLSHIT",
            HereticSoundId.sfx_stfhit => "STFHIT",
            HereticSoundId.sfx_ripslop => "RIPSLOP",
            HereticSoundId.sfx_gntpow => "GNTPOW",
            HereticSoundId.sfx_stfpow => "STFPOW",
            HereticSoundId.sfx_stfcrk => "STFCRK",
            HereticSoundId.sfx_clksit => "CLKSIT",
            HereticSoundId.sfx_clkatk => "CLKATK",
            HereticSoundId.sfx_clkdth => "CLKDTH",
            HereticSoundId.sfx_clkact => "CLKACT",
            HereticSoundId.sfx_clkpai => "CLKPAI",
            _ => null
        };
        public static HereticSoundData Decode(byte[] data)
        {
            if (data == null || data.Length < 8 || data[0] != 3 || data[1] != 0)
                throw new ArgumentException("Invalid DMX sound header.");
            var rate = data[2] | data[3] << 8;
            var count = (uint)(data[4] | data[5] << 8 | data[6] << 16 | data[7] << 24);
            if (rate == 0 || count <= 32 || count > (uint)(data.Length - 8))
                throw new ArgumentException("Invalid DMX sound rate or sample count.");
            // DMX stores sixteen guard samples at each end.
            var samples = new short[(int)count - 32];
            for (var i = 0; i < samples.Length; i++) samples[i] = (short)((data[i + 24] - 128) << 8);
            return new(rate, samples);
        }
    }
}
