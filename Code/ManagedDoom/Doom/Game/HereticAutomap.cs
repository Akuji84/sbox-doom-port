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

// Adapted 2026-09-22: automap visibility and palette colors from pinned am_map.c/am_map.h.
// Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
// Adapted 2026-09-22: am_map.c wall visibility/colors; shared line rasterizer.
using System;
using ManagedDoom.Video;
namespace ManagedDoom
{
    internal static class HereticAutomap
    {
        internal static int? WallColor(LineDef line, bool reveal)
        {
            if ((line.Flags & LineFlags.DontDraw) != 0) return null;
            if ((line.Flags & LineFlags.Mapped) == 0) return reveal ? 43 : null;
            if (line.BackSector == null) return 96;
            if ((int)line.Special == 39 || (line.Flags & LineFlags.Secret) != 0) return 96;
            var special = (int)line.Special;
            if (special > 25 && special < 35)
                return special switch { 26 or 32 => 197, 27 or 34 => 144, 28 or 33 => 220, _ => null };
            if (line.BackSector.FloorHeight != line.FrontSector.FloorHeight) return 112;
            if (line.BackSector.CeilingHeight != line.FrontSector.CeilingHeight) return 80;
            return null;
        }
        internal static void Render(HereticWorldSession session, DrawScreen screen, float zoom)
        {
            screen.FillRect(0, 0, screen.Width, screen.Height, 103);
            var body = session.Body;
            var scale = 0.2f * zoom;
            float X(Fixed x) => screen.Width / 2f + (x.ToFloat() - body.X.ToFloat()) * scale;
            float Y(Fixed y) => screen.Height / 2f - (y.ToFloat() - body.Y.ToFloat()) * scale;
            foreach (var line in session.World.Map.Lines)
            {
                var color = WallColor(line, session.State.HasMapScroll);
                if (color.HasValue) screen.DrawLine(X(line.Vertex1.X), Y(line.Vertex1.Y), X(line.Vertex2.X), Y(line.Vertex2.Y), color.Value);
            }
            // A compact directional marker; the full reference sword/HUD is deferred.
            var cos = Trig.Cos(body.Angle).ToFloat(); var sin = Trig.Sin(body.Angle).ToFloat();
            void Stroke(float x1, float y1, float x2, float y2) => screen.DrawLine(
                screen.Width / 2f + (x1 * cos - y1 * sin) * scale,
                screen.Height / 2f - (x1 * sin + y1 * cos) * scale,
                screen.Width / 2f + (x2 * cos - y2 * sin) * scale,
                screen.Height / 2f - (x2 * sin + y2 * cos) * scale, 32);
            Stroke(-16, 0, 16, 0); Stroke(16, 0, 4, 8); Stroke(16, 0, 4, -8);
        }
    }
}
