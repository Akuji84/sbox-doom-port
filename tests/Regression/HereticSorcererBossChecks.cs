// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticSorcererBossChecks
{
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    static HereticClinkTestEnemy Spawn(HereticWorldSession s,HereticActorType type)
    {
        foreach(var thing in s.World.Map.Things){var e=s.TrySpawnSupportedEnemy(type,thing.X,thing.Y,sorcererPreview:true);if(e!=null)return e;}
        throw new Exception("No boss fixture space.");
    }
    static int Seed(HereticWorldSession s,bool escape)
    {
        for(var i=0;i<256;i++){s.World.Random.Index=i;s.World.Random.Next();if((s.World.Random.Next()<96)==escape)return i;}
        throw new Exception("No escape seed.");
    }
    public static void Verify(string root)
    {
        using var content=GameContent.CreateHereticPreview(Path.Combine(root,"Assets/doom/blasphem.wad"));
        foreach(var type in new[]{HereticActorType.MT_SORCERER1,HereticActorType.MT_SORCERER2})
        {
            var s=new HereticWorldSession(content,1,2);var boss=Spawn(s,type);
            var origin=new Mobj(s.World){X=boss.Body.X,Y=boss.Body.Y,Z=boss.Body.Z,Subsector=boss.Body.Subsector};
            Check(s.PhoenixBlastDamage(origin,boss.Body)==0,"Sorcerer splash immunity missing.");
            foreach(var missileType in new[]{HereticActorType.MT_MACEFX4,HereticActorType.MT_RAINPLR3})
            {
                var p=new HereticProjectile(s,missileType,Angle.Ang0,Fixed.Zero);p.Body.Z=boss.Body.Z;
                var health=boss.Body.Health;s.World.Random.Clear();p.Contact(boss.Body);
                Check(boss.Body.Health<health && boss.Body.Health>0 && (missileType!=HereticActorType.MT_RAINPLR3 || health-boss.Body.Health<=8),"Death ball/rain boss resistance failed.");
            }
            var wizard=Spawn(s,HereticActorType.MT_WIZARD);boss.Body.Target=null;boss.Body.Threshold=0;
            s.DamageTestEnemy(boss.Body,1,inflictor:wizard.Body,source:wizard.Body);
            Check((boss.Body.Target==wizard.Body)==(type==HereticActorType.MT_SORCERER1),"Disciple retaliation exclusion applied to wrong phase.");
        }
        foreach(var missileType in new[]{HereticActorType.MT_PHOENIXFX1,HereticActorType.MT_HORNRODFX2})
        foreach(var escape in new[]{false,true})
        {
            var s=new HereticWorldSession(content,1,2);var boss=Spawn(s,HereticActorType.MT_SORCERER2);
            Check(!s.World.Map.Things.Any(t=>t.Type==56),"Expected no-spot fixture.");
            var p=new HereticProjectile(s,missileType,Angle.Ang0,Fixed.Zero);p.Body.Z=boss.Body.Z;
            var seed=Seed(s,escape);s.World.Random.Index=seed;var health=boss.Body.Health;
            Check(!p.Contact(boss.Body),"Evaded projectile should still impact.");
            Check((boss.Body.Health==health)==escape,"Damage escape did not skip hit with absent spots, or rejected roll skipped damage.");
            if(escape)Check(boss.Body.MomX==Fixed.Zero && boss.Body.MomY==Fixed.Zero,"Escaped hit applied thrust.");
        }
        foreach(var (episode,map) in new[]{(3,8),(3,7),(2,8)})
        {
            var s=new HereticWorldSession(content,1,2);s.World.Options.Episode=episode;s.World.Options.Map=map;
            var first=Spawn(s,HereticActorType.MT_SORCERER2);var last=Spawn(s,HereticActorType.MT_SORCERER2);
            var sector=s.World.Map.Sectors.First(t=>t.Lines.Any(l=>l.BackSector!=null));sector.Tag=666;
            // A_Sor2DthInit massacres other monsters, so isolate the last-boss guard explicitly.
            first.Body.Health=0;s.EpisodeBossDeath(first);
            Check(!s.EpisodeBossTriggered,"E3 completion ignored living rider.");
            last.Body.Health=0;s.EpisodeBossDeath(last);
            Check(s.EpisodeBossTriggered==(episode==3 && map==8),"Sorcerer episode/map gate incorrect.");
            if(episode==3 && map==8){Check(sector.SpecialData!=null,"E3M8 floors not scheduled.");var floor=sector.SpecialData;s.EpisodeBossDeath(last);Check(ReferenceEquals(floor,sector.SpecialData),"Repeated completion replaced floor action.");}
        }
        var live=new HereticWorldSession(content,1,2);live.World.Options.Episode=3;live.World.Options.Map=8;
        var rider=Spawn(live,HereticActorType.MT_SORCERER2);var tagged=live.World.Map.Sectors.First(t=>t.Lines.Any(l=>l.BackSector!=null));tagged.Tag=666;
        live.DamageTestEnemy(rider.Body,3500,environment:true);
        Check(!live.EpisodeBossTriggered,"Death triggered floors before animation completed.");
        for(var i=0;i<300 && !live.EpisodeBossTriggered;i++)live.Tick(default);
        Check(live.EpisodeBossTriggered,$"Full rider death did not complete E3M8: {rider.Combatant.Animation.State}.");
        Console.WriteLine("PASS Sorcerer boss rules: splash, rain/death-ball resistance, Disciple retaliation, hit escape, E3M8 guards and animated completion");
    }
}
