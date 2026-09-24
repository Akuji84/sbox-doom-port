// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticMountedSorcererChecks
{
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    public static void Verify(string root)
    {
        using var content=GameContent.CreateHereticPreview(Path.Combine(root,"Assets/doom/blasphem.wad"));
        // Use a supported body to exercise the controller while boss death remains gated.
        foreach(var health in new[]{2000,1333,1332,666,665})
        {
            var s=new HereticWorldSession(content);var e=s.StartEnemyTest(HereticActorType.MT_IMPLEADER);
            e.Body.Target=s.Body;e.Body.Health=health;
            var state=new HereticActorState(HereticStateId.S_SRCR1_ATK3,e);
            e.Execute(HereticAction.A_Srcr1Attack,state);
            Check(s.Projectiles.Count==(health>1332?1:3),"Mounted health threshold selected wrong volley.");
            Check((state.State==HereticStateId.S_SRCR1_ATK4)==(health<666),"Mounted repeat threshold incorrect.");
            if(health<666)
            {
                var initial=s.Projectiles.Count;
                for(var i=0;i<30 && state.State!=HereticStateId.S_SRCR1_ATK7;i++)state.Tick();
                Check(state.State==HereticStateId.S_SRCR1_ATK7 && s.Projectiles.Count==initial+3,"Native repeat animation failed or repeated recursively.");
                e.Body.Target=null;var count=s.Projectiles.Count;var rng=s.World.Random.Index;
                e.Execute(HereticAction.A_Srcr1Attack,state);
                Check(s.Projectiles.Count==count && s.World.Random.Index==rng,"Missing target consumed attack randomness.");
            }
        }
        var pain=new HereticWorldSession(content);var actor=pain.StartEnemyTest(HereticActorType.MT_IMPLEADER);
        actor.Body.Target=pain.Body;actor.Body.Health=665;
        var animation=new HereticActorState(HereticStateId.S_SRCR1_PAIN1,actor);
        actor.Execute(HereticAction.A_Sor1Pain,animation);
        // Native shared special1 suppresses the first low-health repeat after pain.
        animation.SetState(HereticStateId.S_SRCR1_ATK3);
        Check(animation.State==HereticStateId.S_SRCR1_ATK3,"Pain counter did not suppress repeat.");
        actor.Execute(HereticAction.A_Sor1Pain,animation);
        actor.Body.ReactionTime=1000; // Keep generic pursuit from selecting an attack.
        for(var i=0;i<21;i++)
        {
            animation.SetState(HereticStateId.S_SRCR1_WALK1);
            Check(animation.Tics==(i<20?2:5),"Pain acceleration duration/timing incorrect.");
        }
        var melee=new HereticWorldSession(content);var attacker=melee.StartEnemyTest(HereticActorType.MT_IMPLEADER);
        melee.World.ThingMovement.UnsetThingPosition(attacker.Body);
        attacker.Body.X=melee.Body.X;attacker.Body.Y=melee.Body.Y;attacker.Body.Z=melee.Body.Z;
        melee.World.ThingMovement.SetThingPosition(attacker.Body);attacker.Body.Target=melee.Body;
        melee.World.Random.Clear();attacker.Execute(HereticAction.A_Srcr1Attack,new HereticActorState(HereticStateId.S_SRCR1_ATK3,attacker));
        Check(melee.State.Health==92 && melee.Projectiles.Count==0,$"Mounted melee damage or projectile exclusion incorrect: {melee.State.Health}, {melee.Projectiles.Count}.");
        Check(!HereticWorldSession.SupportsMapEnemy(HereticActorType.MT_SORCERER1),"Incomplete mounted boss enabled.");
        Console.WriteLine("PASS mounted Sorcerer controller: health thresholds, bounded animated repeat, missing target, pain acceleration and melee");
    }
}
