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
        var battle=new HereticWorldSession(content,1,2);
        var hunter=battle.StartClinkTest();var victim=battle.StartEnemyTest(HereticActorType.MT_MUMMY);
        Check(hunter!=null && victim!=null,"Missing post-death fixtures.");
        // Co-locate the fixture bodies to guarantee sight; no movement is performed here.
        battle.World.ThingMovement.UnsetThingPosition(victim.Body);
        victim.Body.X=hunter.Body.X;victim.Body.Y=hunter.Body.Y;victim.Body.Z=hunter.Body.Z;
        battle.World.ThingMovement.SetThingPosition(victim.Body);
        battle.DamageEnvironment(10000);hunter.Body.Target=null;
        battle.World.Random.Clear();
        Check(!hunter.LookForPlayer(true) && battle.World.Random.Index==1,"Native low-roll monster skip missing.");
        Check(hunter.LookForPlayer(true) && hunter.Body.Target==victim.Body && battle.World.Random.Index==2,"Dead-player monster acquisition failed.");
        hunter.Body.Target=null;victim.Body.Flags&=~MobjFlags.CountKill;var before=battle.World.Random.Index;
        Check(!hunter.LookForPlayer(true) && battle.World.Random.Index==before,"Non-kill actor considered as monster target.");
        victim.Body.Flags|=MobjFlags.CountKill;victim.Body.Health=0;
        Check(!hunter.LookForPlayer(true) && battle.World.Random.Index==before,"Dead monster considered as target.");
        victim.Body.Health=100;victim.Body.X=hunter.Body.X+Fixed.FromInt(1281);
        Check(!hunter.LookForPlayer(true) && battle.World.Random.Index==before,"Out-of-range monster consumed selection randomness.");
        victim.Body.X=hunter.Body.X;battle.World.Options.NetGame=true;
        Check(!hunter.LookForPlayer(true) && hunter.Body.Target==null && battle.World.Random.Index==before,"Single-player fallback leaked into multiplayer.");
        battle.World.Options.NetGame=false;hunter.Body.Target=battle.Body;battle.World.Random.Index=1;
        hunter.Execute(HereticAction.A_Chase,hunter.Combatant.Animation);
        Check(hunter.Body.Target==victim.Body,"Chase did not switch from dead player to living monster.");
        Console.WriteLine("PASS post-death monster acquisition: random skip, living kill-count targets, range, multiplayer exclusion and chase recovery");
        Console.WriteLine("PASS player acquisition: front/rear view, close exception, all-around recovery, dead targets and invisibility rolls");
    }
}
