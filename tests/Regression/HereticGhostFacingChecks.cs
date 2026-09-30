// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticGhostFacingChecks
{
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    static HereticClinkTestEnemy Spawn(HereticWorldSession s,HereticActorType type)
    {
        foreach(var t in s.World.Map.Things){var e=s.TrySpawnSupportedEnemy(type,t.X,t.Y);if(e!=null)return e;}
        throw new Exception("No flyer fixture space.");
    }
    public static void Verify(string root)
    {
        using var content=GameContent.CreateHereticPreview(Path.Combine(root,"Assets/doom/blasphem.wad"));
        var s=new HereticWorldSession(content,1,2);var e=s.StartClinkTest();e.Body.Target=s.Body;e.Body.Flags|=MobjFlags.Ambush;
        var aim=Geometry.PointToAngle(e.Body.X,e.Body.Y,s.Body.X,s.Body.Y);var random=s.World.Random;
        random.Clear();s.Body.Flags|=MobjFlags.Shadow;e.Execute(HereticAction.A_FaceTarget,e.Combatant.Animation);
        Check(e.Body.Angle==aim+new Angle(unchecked((uint)((8-109)<<21))) && random.Index==2 && (e.Body.Flags&MobjFlags.Ambush)==0,"Ghost facing lost native random spread or ambush clearing.");
        s.Body.Flags&=~MobjFlags.Shadow;var index=random.Index;e.Execute(HereticAction.A_FaceTarget,e.Combatant.Animation);
        Check(e.Body.Angle==aim && random.Index==index,"Visible target consumed aim randomness.");
        e.Body.Target=null;e.Body.Flags|=MobjFlags.Ambush;e.Execute(HereticAction.A_FaceTarget,e.Combatant.Animation);
        Check(e.Body.Angle==aim && random.Index==index && (e.Body.Flags&MobjFlags.Ambush)!=0,"Missing target altered facing/flags/randomness.");
        foreach(var moverType in new[]{HereticActorType.MT_IMP,HereticActorType.MT_WIZARD,HereticActorType.MT_IMPLEADER})
        foreach(var otherType in new[]{HereticActorType.MT_IMP,HereticActorType.MT_WIZARD})
        {
            var world=new HereticWorldSession(content,1,2);var mover=Spawn(world,moverType);var other=Spawn(world,otherType);
            world.World.ThingMovement.UnsetThingPosition(other.Body);other.Body.X=mover.Body.X;other.Body.Y=mover.Body.Y;other.Body.Z=mover.Body.Z+Fixed.FromInt(128);world.World.ThingMovement.SetThingPosition(other.Body);
            var restricted=moverType!=HereticActorType.MT_IMPLEADER;
            Check(world.World.ThingMovement.CheckPosition(mover.Body,mover.Body.X,mover.Body.Y)==!restricted,"Flying species vertical overlap rule incorrect.");
            world.World.ThingMovement.UnsetThingPosition(other.Body);other.Body.Z=mover.Body.Z-Fixed.FromInt(128);world.World.ThingMovement.SetThingPosition(other.Body);
            Check(world.World.ThingMovement.CheckPosition(mover.Body,mover.Body.X,mover.Body.Y)==!restricted,"Flying species underside rule incorrect.");
        }
        Console.WriteLine("PASS ghost facing and flyer blocking: native aim RNG, ambush clearing, absent targets, Gargoyle/Disciple over-under collision and Fire Gargoyle exclusion");
    }
}
