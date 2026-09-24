// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticMonsterDoorChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Verify(string root)
    {
        using var content=GameContent.CreateHereticPreview(Path.Combine(root,"Assets/doom/blasphem.wad"));
        foreach(var code in new[]{1,7,11,26,27,28,31,32,33,34,62,97})
        foreach(var secret in new[]{false,true})
        {
            var s=new HereticWorldSession(content);
            var line=s.World.Map.Lines.First(l=>l.BackSide!=null && l.FrontSector!=l.BackSector && l.BackSector.SpecialData==null);
            line.Special=(LineSpecial)code;line.Flags=secret?line.Flags|LineFlags.Secret:line.Flags&~LineFlags.Secret;
            s.State.Keys=HereticKeys.Blue|HereticKeys.Yellow|HereticKeys.Green;
            var message=s.State.Message;var count=0;s.SoundRequested+=(id,_)=>{if(id==HereticSoundId.sfx_doropn)count++;};
            Check(s.UseMonsterLine(line)==(!secret && code is 1 or 32 or 33 or 34),"Monster activation acceptance differs from native rules.");
            Check((line.BackSector.SpecialData is VerticalDoor)==(!secret && code==1),"Monster used locked, secret, or player-only action.");
            Check(s.State.Message==message && count==(!secret && code==1?1:0) && (int)line.Special==code,"Monster changed player message, door special or unexpected sound.");
            if(!secret && code==1)
            {
                var door=(VerticalDoor)line.BackSector.SpecialData;
                s.UseMonsterLine(line);Check(door.Direction==1,"Monster closed opening door.");
                door.Direction=0;s.UseMonsterLine(line);Check(door.Direction==0,"Monster closed waiting door.");
                door.Direction=-1;s.UseMonsterLine(line);Check(door.Direction==1,"Monster failed to reopen closing door.");
            }
        }
        // Exercise the collision-to-special path on actual map geometry.
        var live=new HereticWorldSession(content);HereticClinkTestEnemy actor=null;LineDef doorLine=null;
        foreach(var line in live.World.Map.Lines.Where(l=>(int)l.Special==1 && l.BackSide!=null && l.FrontSector!=l.BackSector))
        {
            var dx=line.Vertex2.X-line.Vertex1.X;var dy=line.Vertex2.Y-line.Vertex1.Y;
            if(dx!=Fixed.Zero && dy!=Fixed.Zero)continue;
            var nx=dy>Fixed.Zero?1:dy<Fixed.Zero?-1:0;var ny=dx>Fixed.Zero?-1:dx<Fixed.Zero?1:0;
            var distance=HereticDefinitions.Actors[(int)HereticActorType.MT_CLINK].Radius+Fixed.FromInt(4);
            var x=(line.Vertex1.X+line.Vertex2.X)/2+nx*distance;var y=(line.Vertex1.Y+line.Vertex2.Y)/2+ny*distance;
            var ceiling=line.BackSector.CeilingHeight;line.BackSector.CeilingHeight=line.FrontSector.CeilingHeight;
            var e=live.TrySpawnSupportedEnemy(HereticActorType.MT_CLINK,x,y);
            line.BackSector.CeilingHeight=ceiling;
            if(e==null)continue;
            actor=e;doorLine=line;actor.Body.MoveDir=nx>0?Direction.west:nx<0?Direction.East:ny>0?Direction.South:Direction.North;break;
        }
        Check(actor!=null,"No live door fixture.");
        doorLine.Flags&=~LineFlags.Secret;doorLine.BackSector.CeilingHeight=doorLine.BackSector.FloorHeight;
        var approach=actor.Body.MoveDir;var oldX=actor.Body.X;var oldY=actor.Body.Y;
        Check(actor.MoveChaseDirection() && actor.Body.MoveDir==Direction.None,"Blocked chase failed to activate door/reset direction.");
        Check(actor.Body.X==oldX && actor.Body.Y==oldY && doorLine.BackSector.SpecialData is VerticalDoor,"Actor crossed closed door or failed to start it.");
        var height=doorLine.BackSector.CeilingHeight;live.Tick(default);
        Check(doorLine.BackSector.CeilingHeight>height,"Monster-opened door did not rise.");
        var opened=(VerticalDoor)doorLine.BackSector.SpecialData;
        var originalSide=Geometry.PointOnLineSide(oldX,oldY,doorLine);
        for(var i=0;i<100 && Geometry.PointOnLineSide(actor.Body.X,actor.Body.Y,doorLine)==originalSide;i++)
        {
            opened.Run();actor.Body.MoveDir=approach;actor.MoveChaseDirection();
        }
        Check(Geometry.PointOnLineSide(actor.Body.X,actor.Body.Y,doorLine)!=originalSide,"Enemy could not pass through its opened door.");
        Console.WriteLine("PASS monster doors: native eligible/locked/secret rules, no player-key borrowing, no close, reopen, audio and real blocked-chase activation");
    }
}
