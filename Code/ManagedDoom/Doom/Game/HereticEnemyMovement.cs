// s&Doom modification: 2026-09-24, native Heretic chase-direction selection.
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
// Adapted from pinned p_enemy.c P_Move/P_TryWalk/P_NewChaseDir.
namespace ManagedDoom
{
    public sealed partial class HereticClinkTestEnemy
    {
        private static readonly int[] ChaseX = {65536,47000,0,-47000,-65536,-47000,0,47000};
        private static readonly int[] ChaseY = {0,47000,65536,47000,0,-47000,-65536,-47000};
        internal bool MoveChaseDirection()
        {
            var direction=(int)Body.MoveDir;
            if(direction<0 || direction>=8)return false;
            var speed=HereticDefinitions.Actors[(int)Combatant.Type].Speed;
            var movement=session.World.ThingMovement;
            if(!movement.TryMove(Body,Body.X+speed*new Fixed(ChaseX[direction]),Body.Y+speed*new Fixed(ChaseY[direction])))
            {
                if((Body.Flags&MobjFlags.Float)!=0 && movement.FloatOk)
                {
                    Body.Z+=Body.Z<movement.CurrentFloorZ?Fixed.FromInt(4):-Fixed.FromInt(4);
                    Body.Flags|=MobjFlags.InFloat;
                    return true;
                }
                // Monster activation of blocked door specials is a separate integration.
                return false;
            }
            Body.Flags&=~MobjFlags.InFloat;
            if((Body.Flags&MobjFlags.Float)==0)
            {
                if(Body.Z>Body.FloorZ)session.HitLiquidFloor(Body);
                Body.Z=Body.FloorZ;
            }
            return true;
        }
        private bool TryChaseDirection(Direction direction)
        {
            Body.MoveDir=direction;
            if(!MoveChaseDirection())return false;
            Body.MoveCount=session.World.Random.Next()&15;
            return true;
        }
        internal void NewChaseDirection()
        {
            if(Body.Target==null){Body.MoveDir=Direction.None;return;}
            var old=Body.MoveDir;
            var turnaround=(int)old<8?(Direction)(((int)old+4)&7):Direction.None;
            var dx=Body.Target.X-Body.X;var dy=Body.Target.Y-Body.Y;
            var horizontal=dx>Fixed.FromInt(10)?Direction.East:dx<-Fixed.FromInt(10)?Direction.west:Direction.None;
            var vertical=dy<-Fixed.FromInt(10)?Direction.South:dy>Fixed.FromInt(10)?Direction.North:Direction.None;
            if(horizontal!=Direction.None && vertical!=Direction.None)
            {
                var diagonal=dy<Fixed.Zero?(dx>Fixed.Zero?Direction.Southeast:Direction.Southwest):(dx>Fixed.Zero?Direction.Northeast:Direction.Northwest);
                if(diagonal!=turnaround && TryChaseDirection(diagonal))return;
            }
            var random=session.World.Random;
            if(random.Next()>200 || Fixed.Abs(dy)>Fixed.Abs(dx))(horizontal,vertical)=(vertical,horizontal);
            if(horizontal==turnaround)horizontal=Direction.None;
            if(vertical==turnaround)vertical=Direction.None;
            if(horizontal!=Direction.None && TryChaseDirection(horizontal))return;
            if(vertical!=Direction.None && TryChaseDirection(vertical))return;
            if(old!=Direction.None && TryChaseDirection(old))return;
            var ascending=(random.Next()&1)!=0;
            for(var i=0;i<8;i++)
            {
                var direction=(Direction)(ascending?i:7-i);
                if(direction!=turnaround && TryChaseDirection(direction))return;
            }
            if(turnaround!=Direction.None && TryChaseDirection(turnaround))return;
            Body.MoveDir=Direction.None;
        }
        private void ChaseStep()
        {
            if(--Body.MoveCount<0 || !MoveChaseDirection())NewChaseDirection();
        }
    }
}
