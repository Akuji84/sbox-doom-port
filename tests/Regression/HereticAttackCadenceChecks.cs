// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticAttackCadenceChecks
{
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    public static void Verify(string root)
    {
        using var content=GameContent.CreateHereticPreview(Path.Combine(root,"Assets/doom/blasphem.wad"));
        foreach(var skill in new[]{GameSkill.Medium,GameSkill.Nightmare})
        {
            var s=new HereticWorldSession(content,1,2,skill);var e=s.StartEnemyTest(HereticActorType.MT_BEAST);
            Check(e!=null,"Missing cadence fixture.");e.Body.Target=s.Body;e.Body.ReactionTime=0;
            var state=e.Combatant.Animation;var def=HereticDefinitions.Actors[(int)e.Combatant.Type];
            state.SetState(def.SeeState,false);e.Body.MoveCount=5;e.Body.Flags|=MobjFlags.JustHit;
            e.Execute(HereticAction.A_Chase,state);
            Check((state.State==def.MissileState)==(skill==GameSkill.Nightmare),$"Cadence {skill}: {state.State} expected {def.MissileState}, world {s.World.Options.Skill}.");
            state.SetState(def.SeeState,false);e.Body.MoveCount=0;e.Body.Flags&=~MobjFlags.JustAttacked;e.Body.Flags|=MobjFlags.JustHit;
            e.Execute(HereticAction.A_Chase,state);
            Check(state.State==def.MissileState && (e.Body.Flags&MobjFlags.JustAttacked)!=0,"Ready missile attack did not set recovery flag.");
            state.SetState(def.SeeState,false);var x=e.Body.X;var y=e.Body.Y;var random=s.World.Random.Index;
            e.Execute(HereticAction.A_Chase,state);
            Check(state.State==def.SeeState && (e.Body.Flags&MobjFlags.JustAttacked)==0,"Recovery immediately attacked again.");
            if(skill==GameSkill.Nightmare)Check(e.Body.X==x && e.Body.Y==y && s.World.Random.Index==random,"Nightmare recovery moved or selected a chase direction.");
        }
        var melee=new HereticWorldSession(content,1,2);var clink=melee.StartClinkTest();
        melee.World.ThingMovement.UnsetThingPosition(clink.Body);clink.Body.X=melee.Body.X;clink.Body.Y=melee.Body.Y;clink.Body.Z=melee.Body.Z;melee.World.ThingMovement.SetThingPosition(clink.Body);
        clink.Body.Target=melee.Body;clink.Body.ReactionTime=100;
        clink.Execute(HereticAction.A_Chase,clink.Combatant.Animation);
        Check(clink.Combatant.Animation.State==HereticDefinitions.Actors[(int)HereticActorType.MT_CLINK].MeleeState,"Reaction time incorrectly blocked native melee.");
        Console.WriteLine("PASS chase attack cadence: walking gate, Nightmare exemption, recovery, ready attack and melee during reaction time");
    }
}
