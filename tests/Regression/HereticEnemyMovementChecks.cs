// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticEnemyMovementChecks
{
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    static HereticClinkTestEnemy OpenFixture(HereticWorldSession s,Direction direction)
    {
        foreach(var thing in s.World.Map.Things)
        {
            var e=s.TrySpawnSupportedEnemy(HereticActorType.MT_CLINK,thing.X,thing.Y);
            if(e==null)continue;
            e.Body.MoveDir=direction;var x=e.Body.X;var y=e.Body.Y;
            if(e.MoveChaseDirection())
            {
                s.World.ThingMovement.UnsetThingPosition(e.Body);e.Body.X=x;e.Body.Y=y;s.World.ThingMovement.SetThingPosition(e.Body);
                e.Body.Z=e.Body.FloorZ=e.Body.Subsector.Sector.FloorHeight;
                return e;
            }
            s.World.ThingMovement.UnsetThingPosition(e.Body);e.Body.Flags&=~MobjFlags.Solid;
        }
        throw new Exception("No open movement fixture.");
    }
    public static void Verify(string root)
    {
        using var content=GameContent.CreateHereticPreview(Path.Combine(root,"Assets/doom/blasphem.wad"));
        var xs=new[]{65536,47000,0,-47000,-65536,-47000,0,47000};
        var ys=new[]{0,47000,65536,47000,0,-47000,-65536,-47000};
        for(var i=0;i<8;i++)
        {
            var s=new HereticWorldSession(content,1,2);var e=OpenFixture(s,(Direction)i);var x=e.Body.X;var y=e.Body.Y;
            var speed=HereticDefinitions.Actors[(int)HereticActorType.MT_CLINK].Speed;
            Check(e.MoveChaseDirection() && e.Body.X-x==speed*new Fixed(xs[i]) && e.Body.Y-y==speed*new Fixed(ys[i]),"Native cardinal/diagonal step incorrect.");
            Check(e.Body.Z==e.Body.FloorZ,"Ground enemy did not follow floor.");
        }
        var chase=new HereticWorldSession(content,1,2);var actor=OpenFixture(chase,Direction.Northeast);
        actor.Body.Target=new Mobj(chase.World){X=actor.Body.X+Fixed.FromInt(128),Y=actor.Body.Y+Fixed.FromInt(128)};
        actor.Body.MoveDir=Direction.None;chase.World.Random.Clear();actor.NewChaseDirection();
        Check(actor.Body.MoveDir==Direction.Northeast && actor.Body.MoveCount==8 && chase.World.Random.Index==1,"Direct diagonal choice or walk RNG/counter incorrect.");
        actor.Body.MoveDir=Direction.Southwest;actor.NewChaseDirection();
        Check(actor.Body.MoveDir!=Direction.Northeast && actor.Body.MoveDir!=Direction.None,"Chase unnecessarily reversed instead of choosing an alternative.");
        var blocked=new Mobj(chase.World){X=actor.Body.X,Y=actor.Body.Y,Z=actor.Body.Z,Radius=Fixed.FromInt(64),Height=Fixed.FromInt(128),Flags=MobjFlags.Solid};
        chase.World.ThingMovement.SetThingPosition(blocked);var bx=actor.Body.X;var by=actor.Body.Y;
        actor.NewChaseDirection();
        Check(actor.Body.MoveDir==Direction.None && actor.Body.X==bx && actor.Body.Y==by,"Surrounded actor moved or retained an invalid direction.");
        var index=chase.World.Random.Index;Check(!actor.MoveChaseDirection() && chase.World.Random.Index==index,"No-direction move consumed RNG.");
        chase.World.ThingMovement.UnsetThingPosition(blocked);actor.NewChaseDirection();
        Check(actor.Body.MoveDir!=Direction.None,"Actor failed to resume after blocker removal.");
        actor.Body.Target=null;actor.NewChaseDirection();Check(actor.Body.MoveDir==Direction.None,"Missing target retained movement direction.");
        Console.WriteLine("PASS Heretic chase movement: eight native step vectors, floor following, diagonal priority, walk RNG/counter, reversal avoidance, blocked and recovered movement");
    }
}
