// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticMaulotaurBossChecks
{
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    static HereticClinkTestEnemy Spawn(HereticWorldSession s,HereticActorType type)
    {
        foreach(var thing in s.World.Map.Things){var actor=s.TrySpawnSupportedEnemy(type,thing.X,thing.Y);if(actor!=null)return actor;}
        throw new Exception("Boss fixture has no room.");
    }
    public static void Verify(string root)
    {
        using var content=GameContent.CreateHereticPreview(Path.Combine(root,"Assets/doom/blasphem.wad"));
        var s=new HereticWorldSession(content,1,2);var boss=s.StartEnemyTest(HereticActorType.MT_MINOTAUR);
        Check(boss!=null && s.IsBoss(boss.Body),"Maulotaur boss registration failed.");
        var pixels=new byte[320*200*4];s.Body.Angle=Geometry.PointToAngle(s.Body.X,s.Body.Y,boss.Body.X,boss.Body.Y);
        new HereticMapPreview(content,s).Render(pixels,0);
        var output=Environment.GetEnvironmentVariable("HERETIC_MAULOTAUR_BOSS_RGBA");if(!string.IsNullOrEmpty(output))File.WriteAllBytes(output,pixels);
        var origin=new Mobj(s.World){X=boss.Body.X,Y=boss.Body.Y,Z=boss.Body.Z,Subsector=boss.Body.Subsector};
        Check(s.PhoenixBlastDamage(origin,boss.Body)==0,"Maulotaur lost splash immunity.");
        foreach(var type in new[]{HereticActorType.MT_MACEFX4,HereticActorType.MT_RAINPLR3})
        {
            var missile=new HereticProjectile(s,type,Angle.Ang0,Fixed.Zero);missile.Body.Z=boss.Body.Z;
            var health=boss.Body.Health;s.World.Random.Clear();missile.Contact(boss.Body);
            var damage=health-boss.Body.Health;
            Check(damage>0 && damage<100 && (type!=HereticActorType.MT_RAINPLR3 || damage<=8),"Boss death-ball/rain protection failed.");
        }
        boss.Body.Flags|=MobjFlags.SkullFly;var healthBefore=boss.Body.Health;var x=boss.Body.MomX;var z=boss.Body.MomZ;var rng=s.World.Random.Index;
        s.TouchWhirlwind(boss.Body);s.MaulotaurSlam(s.Body,boss.Body);
        Check(boss.Body.Health==healthBefore && boss.Body.MomX==x && boss.Body.MomZ==z && s.World.Random.Index==rng,"Charging boss accepted whirlwind/slam effects.");
        boss.Body.Flags&=~MobjFlags.SkullFly;
        var other=Spawn(s,HereticActorType.MT_CLINK);other.Body.Target=null;other.Body.Threshold=0;
        s.DamageTestEnemy(other.Body,1,inflictor:boss.Body,source:boss.Body);
        Check(other.Body.Target==null && other.Body.Threshold==0,"Boss damage provoked ordinary retaliation.");
        var drops=new HereticWorldSession(content,1,2);var victim=drops.StartEnemyTest(HereticActorType.MT_MINOTAUR);
        drops.World.Random.Clear();victim.Execute(HereticAction.A_NoBlocking,victim.Combatant.Animation);
        Check(drops.Actors.Any(a=>a.Type==HereticActorType.MT_ARTISUPERHEAL && (a.Body.Flags&MobjFlags.Dropped)!=0),"Mystic Urn drop missing.");
        var ammoSeed=Enumerable.Range(0,256).First(i=>{var r=new DoomRandom(i);return r.Next()>51 && r.Next()<=84;});
        drops.World.Random.Index=ammoSeed;victim.Execute(HereticAction.A_NoBlocking,victim.Combatant.Animation);
        Check(drops.Actors.Any(a=>a.Type==HereticActorType.MT_AMPHRDWIMPY && a.Body.Health==10 && (a.Body.Flags&MobjFlags.Dropped)!=0),"Ten-round Phoenix drop missing.");
        var urns=drops.State.MysticUrns;drops.SpawnEnemyAmmoDrop(drops.Body,HereticActorType.MT_ARTISUPERHEAL,0);drops.Tick(default);
        Check(drops.State.MysticUrns==urns+1,"Dropped Urn cannot be collected.");
        foreach(var episode in new[]{1,2,5})
        {
            var b=new HereticWorldSession(content,1,2);b.World.Options.Map=8;b.World.Options.Episode=episode;
            var first=b.StartEnemyTest(HereticActorType.MT_MINOTAUR);var last=Spawn(b,HereticActorType.MT_MINOTAUR);var survivor=Spawn(b,HereticActorType.MT_CLINK);
            var sector=b.World.Map.Sectors.First(t=>t.Lines.Any(l=>l.BackSector!=null));sector.Tag=666;
            b.DamageTestEnemy(first.Body,10000);for(var i=0;i<100;i++)first.Tick();
            Check(!b.EpisodeBossTriggered,"Maulotaur death triggered before final boss.");
            b.DamageTestEnemy(last.Body,10000);for(var i=0;i<100;i++)last.Tick();
            Check(b.EpisodeBossTriggered==(episode!=1) && (survivor.Body.Health<=0)==(episode!=1),"Maulotaur episode/massacre rules incorrect.");
            if(episode!=1)Check(sector.SpecialData!=null,"Maulotaur death did not schedule tag-666 floors.");
            Check((last.Body.Flags&MobjFlags.Solid)==0 && last.Combatant.Animation.Tics==-1,"Maulotaur corpse failed.");
        }
        var live=new HereticWorldSession(content,1,2);var attacker=live.StartEnemyTest(HereticActorType.MT_MINOTAUR);
        for(var i=0;i<300 && live.State.Health==100;i++)live.Tick(default);
        Check(live.State.Health<100,"Live Maulotaur did not attack.");
        Check(HereticWorldSession.SupportsMapEnemy(HereticActorType.MT_MINOTAUR),"Maulotaur not enabled in map roster.");
        Console.WriteLine("PASS Maulotaur boss: splash/death-ball/rain/charge protections, boss-source retaliation, native drops/pickup, last-boss episode floor/massacre, corpse and live combat");
    }
}
