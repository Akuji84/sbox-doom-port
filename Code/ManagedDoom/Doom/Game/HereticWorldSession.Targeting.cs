// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using System;
namespace ManagedDoom
{
    public readonly record struct HereticTraceHit(Mobj Actor, LineDef Line, Fixed Distance, Fixed Z);
    public sealed partial class HereticWorldSession
    {
        /// <summary>Height-aware obstruction query for future attacks; applies no damage or line actions.</summary>
        public HereticTraceHit? TraceAim(Angle angle, Fixed range, Fixed slope)
        {
            if (range <= Fixed.Zero || range > Fixed.FromInt(2048)) throw new ArgumentOutOfRangeException(nameof(range));
            if (slope < Fixed.FromInt(-4) || slope > Fixed.FromInt(4)) throw new ArgumentOutOfRangeException(nameof(slope));
            HereticTraceHit? hit = null;
            var eye = Camera.ViewZ;
            world.PathTraversal.PathTraverse(Body.X, Body.Y, Body.X + range * Trig.Cos(angle), Body.Y + range * Trig.Sin(angle),
                PathTraverseFlags.AddLines | PathTraverseFlags.AddThings, intercept =>
                {
                    var distance = range * intercept.Frac;
                    var z = eye + distance * slope;
                    if (intercept.Line is { } line)
                    {
                        if (line.BackSector != null)
                        {
                            var bottom = new Fixed(Math.Max(line.FrontSector.FloorHeight.Data, line.BackSector.FloorHeight.Data));
                            var top = new Fixed(Math.Min(line.FrontSector.CeilingHeight.Data, line.BackSector.CeilingHeight.Data));
                            if (z > bottom && z < top) return true;
                        }
                        hit = new(null, line, distance, z);
                    }
                    else
                    {
                        var actor = intercept.Thing;
                        if (actor == Body || (actor.Flags & (MobjFlags.Solid | MobjFlags.Shootable)) == 0 || z < actor.Z || z > actor.Z + actor.Height) return true;
                        hit = new(actor, null, distance, z);
                    }
                    return false;
                });
            return hit;
        }
    }
}
