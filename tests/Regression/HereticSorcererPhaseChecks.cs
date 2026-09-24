// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticSorcererPhaseChecks
{
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    static HereticClinkTestEnemy Spawn(HereticWorldSession s)
    {
        foreach(var thing in s.World.Map.Things)
        {
            var e=s.TrySpawnSupportedEnemy(HereticActorType.MT_SORCERER1,thing.X,thing.Y,sorcererPreview:true);
            if(e!=null)return e;
        }
        throw new Exception("No mounted boss fixture space.");
    }
    public static void Verify(string root)
    {
        using var content=GameContent.CreateHereticPreview(Path.Combine(root,"Assets/doom/blasphem.wad"));
        foreach(var (health,roll,summon) in new[]{(3500,47,true),(3500,48,false),(1750,60,false),(1749,60,true),(1749,96,false)})
        {
            var attack=new HereticWorldSession(content,1,2);var caster=Spawn(attack);
            var boss=attack.RaiseSorcerer(caster);boss.Body.Target=attack.Body;boss.Body.Health=health;
            // Native probability boundaries; find a deterministic index with the desired draw.
            var random=attack.World.Random;var found=false;
            for(var i=0;i<256;i++){random.Index=i;if(random.Next()==roll){random.Index=i;found=true;break;}}
            Check(found,"Missing random boundary fixture.");
            boss.Execute(HereticAction.A_Srcr2Attack,boss.Combatant.Animation);
            Check(attack.Projectiles.Count==(summon?2:1) && attack.Projectiles.All(p=>p.Type==(summon?HereticActorType.MT_SOR2FX2:HereticActorType.MT_SOR2FX1)),"Rider attack probability boundary incorrect.");
            var count=attack.Projectiles.Count;var index=random.Index;boss.Body.Target=null;
            boss.Execute(HereticAction.A_Srcr2Attack,boss.Combatant.Animation);
            Check(attack.Projectiles.Count==count && random.Index==index,"Targetless rider fired or consumed randomness.");
        }
        var tele=new HereticWorldSession(content,1,2);var teleMount=Spawn(tele);var teleBoss=tele.RaiseSorcerer(teleMount);
        var things=tele.World.Map.Things;
        for(var i=0;i<things.Length;i++) if(things[i].Type==56) things[i]=MapThing.Empty;
        var before=tele.World.Random.Index;
        tele.DecideSorcererTeleport(teleBoss);
        Check(tele.World.Random.Index==before,"No-spot decision consumed RNG.");
        things[0]=new MapThing(teleBoss.Body.X,teleBoss.Body.Y,Angle.Ang0,56,0);
        teleBoss.Body.Health=1;tele.World.Random.Clear();
        tele.DecideSorcererTeleport(teleBoss);
        Check(!tele.ImpactEffects.Any(e=>e.Type==HereticActorType.MT_SOR2TELEFADE),"Too-close-only spot did not terminate without teleport.");
        teleBoss.Body.Health=3500;tele.World.Random.Clear();before=tele.World.Random.Index;
        tele.DecideSorcererTeleport(teleBoss);
        Check(tele.World.Random.Index==before+1,"Full-health teleport consumed a destination draw.");
        var s=new HereticWorldSession(content,1,2);var mount=Spawn(s);
        mount.Body.Target=s.Body;mount.Body.Angle=Angle.Ang90;
        var sounds=new List<HereticSoundId>();s.SoundRequested+=(id,_)=>sounds.Add(id);
        Check(!mount.MorphToChicken(),"Mounted boss morphed.");
        Check(s.DamageTestEnemy(mount.Body,2000,environment:true)==HereticDamageResult.Killed,"Mounted boss did not die.");
        var elapsed=0;
        while(s.TestEnemyCount==1 && elapsed++<200)s.Tick(default);
        Check(s.TestEnemyCount==2 && elapsed>100,"Mounted death did not produce exactly one delayed rider.");
        var rider=s.CombatEnemies.Single(e=>e.Combatant.Type==HereticActorType.MT_SORCERER2);
        Check(rider.Body.Health==3500 && rider.Body.X==mount.Body.X && rider.Body.Y==mount.Body.Y && rider.Body.Z==mount.Body.Z && rider.Body.Angle==Angle.Ang90 && rider.Body.Target==s.Body,"Rise lost native position, angle, target or health.");
        Check(rider.Combatant.Animation.State==HereticStateId.S_SOR2_RISE1 && rider.Body.Sprite==(Sprite)HereticSpriteId.SPR_SOR2 && (mount.Body.Flags&MobjFlags.Solid)==0,"Rise state, initial rendering or corpse solidity incorrect.");
        var pixels=new byte[320*200*4];s.Body.Angle=Geometry.PointToAngle(s.Body.X,s.Body.Y,rider.Body.X,rider.Body.Y);
        new HereticMapPreview(content,s).Render(pixels,0);
        Check(pixels.Any(v=>v!=0),"Phase rendering empty.");
        Check(sounds.Count(x=>x==HereticSoundId.sfx_sorzap)==2 && s.TestKills==1,"Mounted zap cadence or kill count incorrect.");
        mount.Execute(HereticAction.A_SorcererRise,mount.Combatant.Animation);
        Check(s.TestEnemyCount==2,"Rise spawned duplicate rider.");
        Check(!rider.MorphToChicken(),"Rider morphed.");
        rider.Body.ReactionTime=1000;
        for(var i=0;i<36;i++)s.Tick(default);
        Check(sounds.Contains(HereticSoundId.sfx_sorrise) && sounds.Contains(HereticSoundId.sfx_sorsit) && rider.Combatant.Animation.State==HereticStateId.S_SOR2_WALK1,"Rider rise failed to enter pursuit.");
        // A monster cannot telefrag the player; a failed move must preserve actor/effects.
        var x=rider.Body.X;var y=rider.Body.Y;var effects=s.ImpactEffects.Count;
        Check(!s.TryTeleportSorcerer(rider,s.Body.X,s.Body.Y,Angle.Ang0) && rider.Body.X==x && rider.Body.Y==y && s.ImpactEffects.Count==effects,"Blocked teleport moved boss or emitted effects.");
        Check(s.TryTeleportSorcerer(rider,x,y,Angle.Ang180) && rider.Combatant.Animation.State==HereticStateId.S_SOR2_TELE1 && rider.Body.Angle==Angle.Ang180 && rider.Body.MomX==Fixed.Zero && rider.Body.MomZ==Fixed.Zero,"Teleport animation/momentum reset failed.");
        Check(s.ImpactEffects.Any(e=>e.Type==HereticActorType.MT_SOR2TELEFADE),"Teleport fade missing.");
        var minion=s.StartEnemyTest(HereticActorType.MT_IMPLEADER);Check(minion!=null,"Minion fixture failed.");
        s.DamageTestEnemy(rider.Body,3500,environment:true);
        Check(minion.Body.Health<=0,"Second-form death did not massacre minions immediately.");
        var loops=0;var previous=rider.Combatant.Animation.State;
        for(var i=0;i<300;i++)
        {
            s.Tick(default);var current=rider.Combatant.Animation.State;
            if(current==HereticStateId.S_SOR2_DIE4 && previous!=current)loops++;
            previous=current;
        }
        Check(loops==7 && rider.Combatant.Animation.State==HereticStateId.S_SOR2_DIE15 && (rider.Body.Flags&MobjFlags.Solid)==0,"Rider death loop count/corpse incorrect.");
        Check(sounds.Contains(HereticSoundId.sfx_sordsph) && sounds.Contains(HereticSoundId.sfx_sordexp) && sounds.Contains(HereticSoundId.sfx_sordbon),"Final death sounds missing.");
        Check(!s.ImpactEffects.Any(e=>e.Type==HereticActorType.MT_SOR2TELEFADE),"Teleport fade leaked.");
        Check(HereticWorldSession.SupportsMapEnemy(HereticActorType.MT_SORCERER1),"Mounted boss missing from preview roster.");
        Console.WriteLine("PASS Sorcerer phase: real mounted death, unique rider, rise timing/audio, pursuit, blocked teleport, fade, massacre and seven-loop death");
    }
}
