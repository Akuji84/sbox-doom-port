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

// Adapted 2026-09-22: pinned Heretic s_sound.c distance and SNDCURVE lookup.
// Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
using System;
namespace ManagedDoom
{
    public sealed class HereticSoundAttenuation
    {
        private readonly byte[] curve;
        public HereticSoundAttenuation(byte[] data)
        {
            if (data == null || data.Length < 1600) throw new ArgumentException("Heretic SNDCURVE needs 1600 entries.");
            curve = new byte[1600]; Array.Copy(data, curve, curve.Length);
        }
        // Panning angle from pinned s_sound.c; PCM gains use an equal-power stereo mix.
        public static int Separation(Mobj source, Mobj listener)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            if (source == null || source == listener || (source.X == listener.X && source.Y == listener.Y)) return 128;
            var angle = Geometry.PointToAngle(listener.X, listener.Y, source.X, source.Y);
            var relative = unchecked(angle.Data - listener.Angle.Data) >> 24;
            var separation = (int)relative * 2 - 128;
            if (separation < 64) separation = -separation;
            if (separation > 192) separation = 512 - separation;
            return Math.Clamp(separation, 0, 256);
        }
        public static short[] Stereo(short[] mono, int separation)
        {
            if (mono == null) throw new ArgumentNullException(nameof(mono));
            if (separation < 0 || separation > 256) throw new ArgumentOutOfRangeException(nameof(separation));
            var left = Math.Cos(separation * Math.PI / 512);
            var right = Math.Sin(separation * Math.PI / 512);
            var stereo = new short[checked(mono.Length * 2)];
            for (var i = 0; i < mono.Length; i++)
            {
                stereo[2 * i] = (short)Math.Round(mono[i] * left);
                stereo[2 * i + 1] = (short)Math.Round(mono[i] * right);
            }
            return stereo;
        }
        public float Gain(Mobj source, Mobj listener)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            source ??= listener;
            // Widen coordinate differences to avoid wrapping across extreme map coordinates.
            var dx = Math.Abs((long)source.X.Data - listener.X.Data);
            var dy = Math.Abs((long)source.Y.Data - listener.Y.Data);
            var distance = (dx + dy - (Math.Min(dx, dy) >> 1)) >> Fixed.FracBits;
            return distance >= curve.Length ? 0 : Math.Clamp(curve[(int)distance] / 127f, 0, 1);
        }
    }
}
