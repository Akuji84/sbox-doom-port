// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticAcquisitionChecks
{
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    public static void Verify(string root)
    {
        using var content=GameContent.CreateHereticPreview(Path.Combine(root,"Assets/doom/blasphem.wad"));
        var s=new HereticWorldSession(content,1,2);var e=s.StartClinkTest();Check(e!=null,"No acquisition fixture.");
        e.Body.Target=null;Check(e.LookForPlayer(false) && e.Body.Target==s.Body,"Visible front player not acquired.");
        e.Body.Angle+=Angle.Ang180;e.Body.Target=null;
        Check(!e.LookForPlayer(false) && e.Body.Target==null,"Distant rear player acquired.");
        Check(e.LookForPlayer(true),"All-around reacquisition failed.");
        e.Body.Target=new Mobj(s.World){Health=0};e.Body.ReactionTime=100;
        e.Execute(HereticAction.A_Chase,e.Combatant.Animation);
        Check(e.Body.Target==s.Body,"Dead target not replaced.");
        s.Body.Flags|=MobjFlags.Shadow;s.World.Random.Clear();e.Body.Target=null;
        Check(!e.LookForPlayer(true),"Invisible player bypassed failed detection roll.");
        for(var i=0;i<256;i++){s.World.Random.Index=i;if(s.World.Random.Next()>=225){s.World.Random.Index=i;break;}}
        Check(e.LookForPlayer(true),"Nearby invisible player cannot be detected on successful roll.");
        s.Body.Flags&=~MobjFlags.Shadow;
        s.World.ThingMovement.UnsetThingPosition(e.Body);e.Body.X=s.Body.X;e.Body.Y=s.Body.Y;e.Body.Z=s.Body.Z;s.World.ThingMovement.SetThingPosition(e.Body);
        e.Body.Angle=Angle.Ang180;Check(e.LookForPlayer(false),"Close player failed rear detection exception.");
        s.DamageEnvironment(10000);e.Body.Target=s.Body;e.Execute(HereticAction.A_Chase,e.Combatant.Animation);
        Check(e.Body.Target==null && e.Combatant.Animation.State==HereticDefinitions.Actors[(int)e.Combatant.Type].SpawnState,"Dead player retained as target.");
        Console.WriteLine("PASS player acquisition: front/rear view, close exception, all-around recovery, dead targets and invisibility rolls");
    }
}
