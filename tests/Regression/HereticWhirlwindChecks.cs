// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticWhirlwindChecks
{
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    public static void Verify(string root)
    {
        using var content=GameContent.CreateHereticPreview(Path.Combine(root,"Assets/doom/blasphem.wad"));
        var s=new HereticWorldSession(content); var e=s.StartEnemyTest(HereticActorType.MT_IMPLEADER);
        var sounds=0; s.SoundRequested+=(id,_)=>{if(id==HereticSoundId.sfx_hedat3)sounds++;};
        var wind=s.SpawnIronLichWhirlwind(e);
        Check(wind.Flying && wind.Body.Health==700 && wind.SeekerTarget==s.Body && wind.Body.Target==e.Body,"Whirlwind initialization incorrect.");
        Check(wind.Body.MomX==10*Trig.Cos(wind.Body.Angle) && sounds==1,"Whirlwind speed/sound incorrect.");
        var rng=s.World.Random.Index; Check(wind.Contact(e.Body) && s.World.Random.Index==rng,"Whirlwind hit owner.");
        wind.Body.Z=s.Body.Z; var health=s.State.Health;
        wind.Contact(s.Body); Check(s.State.Health==health-3,"Whirlwind periodic damage incorrect.");
        s.State.InvulnerabilityTics=100; health=s.State.Health; wind.Contact(s.Body);
        Check(s.State.Health==health,"Whirlwind ignored invulnerability.");
        s.Body.Flags|=MobjFlags.Shadow; wind.Body.Angle+=Angle.Ang180; var angle=wind.Body.Angle;
        wind.Execute(HereticAction.A_WhirlwindSeek,wind.Animation);
        Check(wind.Body.Angle==angle && wind.Body.Health==697,"Invisible target incorrectly tracked or lifetime stalled.");
        s.Body.Flags&=~MobjFlags.Shadow;
        wind.Execute(HereticAction.A_WhirlwindSeek,wind.Animation);
        Check(wind.Body.Angle!=angle,"Visible target not tracked.");
        s.Body.Flags&=~MobjFlags.Shootable; wind.Execute(HereticAction.A_WhirlwindSeek,wind.Animation);
        Check(wind.SeekerTarget==null,"Dead target retained.");
        s.Body.Flags|=MobjFlags.Shootable;
        for(var i=0;i<240 && wind.Flying;i++)wind.Execute(HereticAction.A_WhirlwindSeek,wind.Animation);
        Check(!wind.Flying && wind.Body.MomX==Fixed.Zero && wind.Body.MomZ==Fixed.Zero && sounds>1,"Whirlwind duration/sound/termination failed.");
        for(var i=0;i<20;i++)wind.Tick(); Check(wind.Animation.Removed,"Whirlwind death animation leaked.");
        var lift=new HereticWorldSession(content);
        for(var i=0;i<16;i++)lift.Tick(default);
        lift.Body.MomZ=Fixed.FromInt(11); lift.World.Random.Clear(); lift.TouchWhirlwind(lift.Body);
        Check(lift.Body.MomZ>Fixed.FromInt(11) && lift.Body.MomZ<=Fixed.FromInt(12),"Whirlwind lift/cap failed.");
        var collision=new HereticWorldSession(content); var caster=collision.StartEnemyTest(HereticActorType.MT_IMPLEADER);
        var blocked=collision.SpawnIronLichWhirlwind(caster);
        blocked.Body.MomX=blocked.Body.MomY=blocked.Body.MomZ=Fixed.Zero;
        blocked.Body.Z=blocked.Body.FloorZ;
        for(var i=0;i<30;i++)blocked.Advance();
        Check(blocked.Flying,"Resting floor contact prematurely killed whirlwind.");
        blocked.Body.MomZ=-Fixed.One;
        blocked.Body.Z=blocked.Body.FloorZ;
        blocked.Advance(); Check(blocked.Flying,"Whirlwind died on first floor contact.");
        for(var i=0;i<20 && blocked.Flying;i++)blocked.Advance();
        Check(!blocked.Flying,"Repeated whirlwind impacts did not terminate.");
        foreach(var far in new[]{false,true})
        foreach(var attack in new[]{0,1,2})
        {
            var a=new HereticWorldSession(content); var enemy=a.StartEnemyTest(HereticActorType.MT_IMPLEADER);
            enemy.Body.Target=a.Body;
            if(far){a.World.ThingMovement.UnsetThingPosition(a.Body);a.Body.X+=Fixed.FromInt(1024);a.World.ThingMovement.SetThingPosition(a.Body);}
            var low=attack==0?0:attack==1?(far?150:50):(far?200:150);
            var high=attack==0?(far?150:50):attack==1?(far?200:150):256;
            for(var seed=0;seed<256;seed++){var r=new DoomRandom(seed).Next();if(r>=low && r<high){a.World.Random.Index=seed;break;}}
            a.AttackIronLich(enemy);
            var expected=attack==0?HereticActorType.MT_HEADFX1:attack==1?HereticActorType.MT_HEADFX3:HereticActorType.MT_WHIRLWIND;
            Check(a.Projectiles.Count>0 && a.Projectiles[0].Type==expected,"Iron Lich close/far attack selection incorrect.");
        }
        Check(!HereticWorldSession.SupportsMapEnemy(HereticActorType.MT_HEAD),"Incomplete Iron Lich roster enabled.");
        Console.WriteLine("PASS whirlwind: ownership, speed, seeking/ghost/dead target, periodic damage/invulnerability, lift cap, lifetime/sounds, repeated impacts and close/far attack selection");
    }
}
