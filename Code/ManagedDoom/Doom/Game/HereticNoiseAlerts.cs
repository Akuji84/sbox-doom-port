// s&Doom modification: 2026-09-29, sector sound alerts.
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

// Reference: pinned Heretic p_enemy.c/p_mobj.c.
// Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
// Adapted from pinned p_enemy.c P_RecursiveSound/P_NoiseAlert and A_Look.
using System.Collections.Generic;
namespace ManagedDoom
{
    public sealed partial class HereticWorldSession
    {
        internal void NoiseAlert(Mobj target,Mobj emitter)
        {
            if(target==null || emitter?.Subsector==null)return;
            var best=new Dictionary<Sector,int>();
            var pending=new Queue<(Sector sector,int blocks)>();
            pending.Enqueue((emitter.Subsector.Sector,0));
            while(pending.Count>0)
            {
                var (sector,blocks)=pending.Dequeue();
                if(best.TryGetValue(sector,out var previous) && previous<=blocks)continue;
                best[sector]=blocks;sector.SoundTarget=target;sector.SoundTraversed=blocks+1;
                foreach(var line in sector.Lines)
                {
                    if((line.Flags&LineFlags.TwoSided)==0 || line.BackSector==null)continue;
                    var other=line.FrontSector==sector?line.BackSector:line.FrontSector;
                    if(Fixed.Min(sector.CeilingHeight,other.CeilingHeight)<=Fixed.Max(sector.FloorHeight,other.FloorHeight))continue;
                    var next=blocks+((line.Flags&LineFlags.SoundBlock)!=0?1:0);
                    if(next<=1)pending.Enqueue((other,next));
                }
            }
        }
    }
    public sealed partial class HereticClinkTestEnemy
    {
        private bool LookForSoundTarget()
        {
            var target=Body.Subsector.Sector.SoundTarget;
            if(target==null || (target.Flags&MobjFlags.Shootable)==0)return false;
            if((Body.Flags&MobjFlags.Ambush)!=0 && !visibility.CheckSight(Body,target))return false;
            Body.Target=target;return true;
        }
    }
}
