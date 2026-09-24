// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticIronLichCombatChecks
{
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    static HereticClinkTestEnemy SpawnElsewhere(HereticWorldSession session,HereticActorType type)
    {
        foreach(var thing in session.World.Map.Things)
        {
            var enemy=session.TrySpawnSupportedEnemy(type,thing.X,thing.Y);
            if(enemy!=null)return enemy;
        }
        return null;
    }
    public static void Verify(string root)
    {
        using var content=GameContent.CreateHereticPreview(Path.Combine(root,"Assets/doom/blasphem.wad"));
        var s=new HereticWorldSession(content,1,2);var lich=s.StartEnemyTest(HereticActorType.MT_HEAD);
        Check(lich!=null && lich.Body.Health==700 && lich.Body.Info==null,"Iron Lich spawn failed.");
        var rng=s.World.Random.Index;Check(!lich.MorphToChicken() && s.World.Random.Index==rng,"Iron Lich allowed morph or consumed random draws.");
        foreach(var type in new[]{HereticActorType.MT_MACEFX4,HereticActorType.MT_BLASTERFX1,HereticActorType.MT_RIPPER})
        {
            var p=new HereticProjectile(s,type,Angle.Ang0,Fixed.Zero);p.Body.Z=lich.Body.Z;
            var health=lich.Body.Health;s.World.Random.Clear();p.Contact(lich.Body);
            var damage=health-lich.Body.Health;
            Check(type==HereticActorType.MT_MACEFX4?damage>0 && damage<100:damage>=0 && damage<=1,"Iron Lich weapon resistance failed.");
        }
        lich.Body.Target=s.Body;s.World.Random.Clear();lich.Execute(HereticAction.A_HeadAttack,lich.Combatant.Animation);
        Check(s.Projectiles.Any(p=>p.Type==HereticActorType.MT_HEADFX1),"Lich actor did not dispatch attack.");
        var pixels=new byte[320*200*4];s.Body.Angle=Geometry.PointToAngle(s.Body.X,s.Body.Y,lich.Body.X,lich.Body.Y);
        new HereticMapPreview(content,s).Render(pixels,s.World.LevelTime);
        var output=Environment.GetEnvironmentVariable("HERETIC_LICH_COMBAT_RGBA");if(!string.IsNullOrEmpty(output))File.WriteAllBytes(output,pixels);
        var d=new HereticWorldSession(content,1,2);var dropper=d.StartEnemyTest(HereticActorType.MT_HEAD);
        var seed=Enumerable.Range(0,256).First(i=>{var r=new DoomRandom(i);return r.Next()>84 && r.Next()<=51;});
        d.World.Random.Index=seed;dropper.Execute(HereticAction.A_NoBlocking,dropper.Combatant.Animation);
        Check(d.Actors.Any(a=>a.Type==HereticActorType.MT_ARTIEGG && (a.Body.Flags&MobjFlags.Dropped)!=0),"Lich Ovum drop missing.");
        foreach(var episode in new[]{1,2,4})
        {
            var boss=new HereticWorldSession(content,1,2);boss.World.Options.Map=8;boss.World.Options.Episode=episode;
            var first=boss.StartEnemyTest(HereticActorType.MT_HEAD);var last=SpawnElsewhere(boss,HereticActorType.MT_HEAD);
            Check(first!=null && last!=null,"Multiple Lich fixture failed.");
            var other=SpawnElsewhere(boss,HereticActorType.MT_CLINK);Check(other!=null,"Massacre fixture failed.");
            var sector=boss.World.Map.Sectors.First(x=>x.Lines.Any(l=>l.BackSector!=null));sector.Tag=666;
            boss.DamageTestEnemy(first.Body,10000);
            for(var i=0;i<50;i++)first.Tick();
            Check(!boss.EpisodeBossTriggered,"Boss trigger fired before final Lich death.");
            boss.DamageTestEnemy(last.Body,10000);
            for(var i=0;i<50;i++)last.Tick();
            Check(boss.EpisodeBossTriggered==(episode!=2),"Boss episode gate incorrect.");
            Check((other.Body.Health<=0)==(episode==4),"Episode 4 massacre incorrect.");
            if(episode!=2)Check(sector.SpecialData!=null,"Boss floor lowering not scheduled.");
            Check(last.Combatant.Animation.State==HereticStateId.S_HEAD_DIE7 && (last.Body.Flags&MobjFlags.Solid)==0,"Lich corpse failed to settle.");
        }
        var live=new HereticWorldSession(content,1,2);var attacker=live.StartEnemyTest(HereticActorType.MT_HEAD);
        for(var i=0;i<300 && live.State.Health==100;i++)live.Tick(default);
        Check(live.State.Health<100,"Live Iron Lich never attacked.");
        Console.WriteLine("PASS Iron Lich combat: spawn/render, attacks, morph/death-ball/Claw resistance, Ovum drop, last-boss episode floor trigger, massacre and live combat");
    }
}
