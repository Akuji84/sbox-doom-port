// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticMaulotaurChargeChecks
{
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    public static void Verify(string root)
    {
        using var content=GameContent.CreateHereticPreview(Path.Combine(root,"Assets/doom/blasphem.wad"));
        var s=new HereticWorldSession(content,1,2);var m=s.StartEnemyTest(HereticActorType.MT_MINOTAUR);
        Check(m!=null && m.Body.Health==3000,"Maulotaur test spawn failed.");
        m.Body.Target=s.Body;s.World.Random.Clear();m.Execute(HereticAction.A_MinotaurDecide,m.Combatant.Animation);
        Check(m.MaulotaurCharging && m.Body.MomX==13*Trig.Cos(m.Body.Angle),"Charge selection/speed incorrect.");
        var rng=s.World.Random.Index;Check(s.DamageTestEnemy(m.Body,10000)==HereticDamageResult.Ignored && m.Body.Health==3000 && s.World.Random.Index==rng,"Charge immunity consumed damage/randomness.");
        Check(!m.MorphToChicken(),"Maulotaur morphed.");
        var mx=m.Body.MomX;var my=m.Body.MomY;m.Tick();
        Check(m.Body.MomX==mx && m.Body.MomY==my,"Charge lost momentum to friction.");
        for(var i=0;i<40 && m.MaulotaurCharging;i++)m.Tick();
        Check(!m.MaulotaurCharging && s.State.Health<100 && s.Body.ReactionTime>=14 && s.Body.ReactionTime<=21,"Charge did not slam/recover/stun player.");
        var d=new HereticWorldSession(content,1,2);var actor=d.StartEnemyTest(HereticActorType.MT_MINOTAUR);actor.Body.Target=d.Body;
        d.World.Random.Clear();actor.Execute(HereticAction.A_MinotaurDecide,actor.Combatant.Animation);
        for(var i=0;i<17;i++)actor.Execute(HereticAction.A_MinotaurCharge,actor.Combatant.Animation);
        Check(actor.MaulotaurCharging && d.ImpactEffects.Count==17,"Charge duration/puff count incorrect.");
        actor.Execute(HereticAction.A_MinotaurCharge,actor.Combatant.Animation);
        Check(!actor.MaulotaurCharging,"Timed charge failed to end.");
        var puff=d.ImpactEffects[0];var z=puff.Body.Z;actor.Body.Target=null;
        d.Tick(default);Check(puff.Body.Z>z,"Charge puff did not rise.");
        var melee=new HereticWorldSession(content,1,2);var close=melee.StartEnemyTest(HereticActorType.MT_MINOTAUR);
        var angle=Geometry.PointToAngle(melee.Body.X,melee.Body.Y,close.Body.X,close.Body.Y);
        melee.World.ThingMovement.UnsetThingPosition(close.Body);close.Body.X=melee.Body.X+48*Trig.Cos(angle);close.Body.Y=melee.Body.Y+48*Trig.Sin(angle);melee.World.ThingMovement.SetThingPosition(close.Body);
        close.Body.Target=melee.Body;melee.World.Random.Clear();close.Execute(HereticAction.A_MinotaurAtk1,close.Combatant.Animation);
        Check(melee.State.Health==96 && melee.Camera.DeltaViewHeight==-Fixed.FromInt(16),"Hammer melee damage/view squish incorrect.");
        var ranged=new HereticWorldSession(content,1,2);var shooter=ranged.StartEnemyTest(HereticActorType.MT_MINOTAUR);shooter.Body.Target=ranged.Body;
        shooter.Execute(HereticAction.A_MinotaurAtk2,shooter.Combatant.Animation);
        Check(ranged.Projectiles.Count==5,"Maulotaur actor did not fire spread.");
        var floor=new HereticWorldSession(content,1,2);var stomper=floor.StartEnemyTest(HereticActorType.MT_MINOTAUR);stomper.Body.Target=floor.Body;
        var seed=Enumerable.Range(0,256).First(i=>{var r=new DoomRandom(i);return r.Next()>=150 && r.Next()<220;});
        floor.World.Random.Index=seed;stomper.Execute(HereticAction.A_MinotaurDecide,stomper.Combatant.Animation);
        Check(stomper.Combatant.Animation.State==HereticStateId.S_MNTR_ATK3_1 && !stomper.MaulotaurCharging,"Floor-fire decision failed.");
        floor.World.Random.Index=3;stomper.Execute(HereticAction.A_MinotaurAtk3,stomper.Combatant.Animation);
        Check(stomper.Combatant.Animation.State==HereticStateId.S_MNTR_ATK3_4,"Floor-fire follow-up missing.");
        stomper.Combatant.Animation.SetState(HereticStateId.S_MNTR_ATK3_3,false);
        floor.World.Random.Index=3;stomper.Execute(HereticAction.A_MinotaurAtk3,stomper.Combatant.Animation);
        Check(stomper.Combatant.Animation.State==HereticStateId.S_MNTR_ATK3_3,"Floor-fire repeated more than once.");
        var wall=new HereticWorldSession(content,1,2);var charger=wall.StartEnemyTest(HereticActorType.MT_MINOTAUR);
        charger.Body.Target=null;charger.Body.Flags|=MobjFlags.SkullFly;charger.Body.MomX=Fixed.FromInt(13);charger.Body.MomY=Fixed.Zero;
        charger.Combatant.Animation.SetState(HereticStateId.S_MNTR_ATK4_1,false);
        for(var i=0;i<100 && charger.MaulotaurCharging;i++)charger.Tick();
        Check(!charger.MaulotaurCharging,"Blocked charge never recovered.");
        Check(!HereticWorldSession.SupportsMapEnemy(HereticActorType.MT_MINOTAUR),"Maulotaur map gate removed prematurely.");
        Console.WriteLine("PASS Maulotaur charge/melee: selection, speed/friction, immunity/morph, slam/stun/recovery, timed puffs, melee view impact and spread dispatch");
    }
}
