// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticCampaignExitChecks
{
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    public static void Verify(string root)
    {
        Check(HereticMusic.MapTrack(2,5)=="MUS_E1M4" && HereticMusic.MapTrack(3,8)=="MUS_E1M9" && HereticMusic.MapTrack(4,1)=="MUS_E1M6" && HereticMusic.MapTrack(5,9)=="MUS_E2M9" && HereticMusic.MapTrack(6,3)=="MUS_E1M6","Native music aliases changed.");
        Check(HereticMusic.MapTrack(0,1)==null && HereticMusic.MapTrack(6,4)==null && HereticMusic.MapTrack(1,10)==null,"Invalid music map accepted.");
        int[] returns={7,5,5,5,4};
        for(int episode=1;episode<=5;episode++)
        for(int map=1;map<=9;map++)
        {
            var normal=HereticCampaignRoutes.Resolve(episode,map,false);
            Check(normal.Kind==(map==8?HereticExitKind.EpisodeComplete:HereticExitKind.NextMap),"Wrong episode finale route.");
            Check(normal.NextMap==(map==8?(int?)null:map==9?returns[episode-1]:map+1),"Wrong normal/secret-return destination.");
            var secret=HereticCampaignRoutes.Resolve(episode,map,true);
            Check(secret.Kind==HereticExitKind.NextMap && secret.NextMap==9,"Secret exit must take precedence over finale/return.");
        }
        foreach(var pair in new[]{(0,1),(6,1),(1,0),(1,10)})
            Check(HereticCampaignRoutes.Resolve(pair.Item1,pair.Item2,false)==(HereticExitKind.UnsupportedRoute,(int?)null),"Unknown campaign route guessed.");
        using var content=GameContent.CreateHereticPreview(Path.Combine(root,"Assets/doom/blasphem.wad"));
        for(int ep=1;ep<=6;ep++)for(int map=1;map<=(ep==6?3:9);map++)
        {
            var name=HereticMusic.MapTrack(ep,map);var data=content.Wad.ReadLump(name);
            Check(data.Length>=16 && ((data[0]=='M' && data[1]=='U' && data[2]=='S' && data[3]==26) || (data[0]=='M' && data[1]=='T' && data[2]=='h' && data[3]=='d')),"Missing/unsupported mapped music: "+name);
        }
        foreach(var track in new[]{"MUS_INTR","MUS_CPTD"})Check(content.Wad.ReadLump(track).Length>=16,"Missing transition music.");
        foreach(var code in new[]{11,51,52,105})
        {
            var s=new HereticWorldSession(content,1,2);s.Tick(default);s.State.Secrets=3;
            Check(s.Completion==null,"Completion exists before exit.");
            Check(HereticMusic.TrackFor(s)=="MUS_E1M2","Live session music mismatch.");
            var line=s.World.Map.Lines[0];line.Special=(LineSpecial)code;
            if(code==11 || code==51)s.UseLine(line);else s.CrossLine(line,0,s.Body);
            bool secret=code==51 || code==105;
            Check(s.ExitRequested && s.SecretExitRequested==secret && s.Completion.HasValue,"Exit action did not publish completion.");
            Check(HereticMusic.TrackFor(s)=="MUS_INTR","Completion music did not switch.");
            var result=s.Completion.Value;
            Check(result.Episode==1 && result.Map==2 && result.Secret==secret && result.NextMap==(secret?9:3) && result.ElapsedTics==1 && result.Secrets==3,"Completion snapshot lost route/stats.");
            var x=s.Body.X;var y=s.Body.Y;
            s.Tick(new HereticCommand{Forward=50});s.State.Secrets=99;
            line.Special=(LineSpecial)(secret?52:105);s.CrossLine(line,0,s.Body);
            Check(s.Completion==result && s.SecretExitRequested==secret && s.Body.X==x && s.Body.Y==y,"Repeated exit changed completion or resumed simulation.");
        }
        var source=new HereticWorldSession(content,1,1);source.StartMapCombat();
        source.State.Health=73;source.Body.Health=73;source.State.ArmorType=2;source.State.ArmorPoints=87;
        source.State.Keys=HereticKeys.Blue;source.State.QuartzFlasks=9;source.State.MysticUrns=4;source.State.WingsOfWrath=3;
        source.State.TorchTics=100;source.State.FlightTics=100;source.State.WeaponPowerTics=100;
        source.State.HasMapScroll=true;source.State.DamageFlash=20;source.State.Secrets=5;
        source.GoldWand.GrantTestCrossbow(17);source.GoldWand.GrantTestBlaster(31);source.GoldWand.GiveBagOfHolding(false);
        var ammo=source.GoldWand.CrossbowAmmo;var gold=source.GoldWand.Ammo;
        var exit=source.World.Map.Lines[0];exit.Special=(LineSpecial)52;source.CrossLine(exit,0,source.Body);
        source.State.Health=1;source.GoldWand.Ammo=1; // Frozen carry must not alias old mutable state.
        var next=source.CreateNextCampaignSession();
        Check(next.State.Health==73 && next.Body.Health==73 && next.State.ArmorPoints==87 && next.State.ArmorType==2,"Health/armor carry lost.");
        Check(next.State.QuartzFlasks==1 && next.State.MysticUrns==1 && next.State.WingsOfWrath==0,"Native artifact carry rules failed.");
        Check(next.State.Keys==HereticKeys.None && next.State.FlightTics==0 && next.State.WeaponPowerTics==0 && next.State.TorchTics==0 && !next.State.HasMapScroll && next.State.Secrets==0 && next.State.DamageFlash==0,"Level-local state leaked.");
        Check(next.GoldWand.HasCrossbow && next.GoldWand.HasBlaster && next.GoldWand.HasBagOfHolding && next.GoldWand.CrossbowAmmo==ammo && next.GoldWand.Ammo==gold,"Weapons/ammo/bag lost or aliased.");
        Check(next.Completion==null && !next.ExitRequested && next.TestEnemyCount>0 && ReferenceEquals(next,source.CreateNextCampaignSession()),"Next session not playable/idempotent.");
        var nextView=new HereticMapPreview(content,next);
        var pixels=new byte[HereticMapPreview.Width*HereticMapPreview.Height*4];
        nextView.Render(pixels,0,0);
        Check(pixels.Any(value=>value!=0),"Transition renderer produced an empty frame.");
        var stepper=new HereticFrameStepper();stepper.Advance(1.0/35,default,next.Tick);
        var secondExit=next.World.Map.Lines[0];secondExit.Special=(LineSpecial)52;next.CrossLine(secondExit,0,next.Body);
        Check(next.Completion.Value.Map==2 && next.Completion.Value.NextMap==3 && next.Completion.Value.ElapsedTics==1,"Second-map completion retained old identity/time.");
        var third=next.CreateNextCampaignSession();
        Check(third!=next && third.Completion==null && third.GoldWand.HasCrossbow,"Chained host transition failed.");
        var chicken=new HereticWorldSession(content);chicken.StartMapCombat();chicken.MorphPlayer();
        var chickenHealth=chicken.State.Health;var chickenExit=chicken.World.Map.Lines[0];chickenExit.Special=(LineSpecial)52;chicken.CrossLine(chickenExit,0,chicken.Body);
        var restored=chicken.CreateNextCampaignSession();
        Check(restored.State.ChickenTics==0 && restored.Body.Height==Fixed.FromInt(56) && restored.GoldWand.ReadyWeapon==HereticWeapon.wp_goldwand && restored.State.Health==chickenHealth,"Chicken exit did not restore native body/weapon carry.");
        bool rejected=false;try { restored.CreateNextCampaignSession(); }catch(InvalidOperationException){rejected=true;}
        Check(rejected,"Transition without exit accepted.");
        var doomed=new HereticWorldSession(content,1,2,GameSkill.Hard);doomed.StartMapCombat();
        var originalEnemies=doomed.TestEnemyCount;
        doomed.GoldWand.GrantTestCrossbow(40);doomed.GoldWand.GiveBagOfHolding(false);
        doomed.State.MysticUrns=4;doomed.State.Keys=HereticKeys.Blue;doomed.State.Secrets=3;
        doomed.DamageEnvironment(10000);
        var reborn=doomed.RestartAfterDeath();
        Check(reborn.State.Health==100 && reborn.Body.Health==100 && reborn.State.ArmorPoints==0 && reborn.State.Keys==HereticKeys.None && reborn.State.MysticUrns==0 && reborn.State.Secrets==0,"Restart retained inventory or did not revive.");
        Check(reborn.GoldWand.Ammo==50 && reborn.GoldWand.ReadyWeapon==HereticWeapon.wp_goldwand && !reborn.GoldWand.HasCrossbow && !reborn.GoldWand.HasBagOfHolding,"Restart did not restore starting loadout.");
        Check(reborn.World.Options.Skill==GameSkill.Hard && reborn.TestEnemyCount==originalEnemies && reborn.TestKills==0 && !reborn.ExitRequested && ReferenceEquals(reborn,doomed.RestartAfterDeath()),"Restart lost difficulty/roster or repeated reload.");
        rejected=false;try { reborn.RestartAfterDeath(); }catch(InvalidOperationException){rejected=true;}
        Check(rejected,"Living-player restart accepted.");
        doomed.World.Options.NetGame=true;rejected=false;try { doomed.RestartAfterDeath(); }catch(NotSupportedException){rejected=true;}
        Check(rejected,"Network respawn leaked into single-player restart.");
        reborn.Tick(default);var rebornExit=reborn.World.Map.Lines[0];rebornExit.Special=(LineSpecial)52;reborn.CrossLine(rebornExit,0,reborn.Body);
        Check(reborn.Completion.Value.Map==2 && reborn.Completion.Value.ElapsedTics==1 && reborn.CreateNextCampaignSession().State.Health==100,"Restarted map cannot continue campaign.");
        var stats=new HereticWorldSession(content,1,1);var secrets=stats.World.Map.Sectors.Count(sector=>(int)sector.Special==9);
        stats.StartMapCombat();var totalKills=stats.TotalKills;var totalItems=stats.TotalItems;
        Check(totalKills==stats.CombatEnemies.Count(enemy=>(enemy.Body.Flags&MobjFlags.CountKill)!=0) && stats.TotalSecrets==secrets,"Map totals do not match initial world.");
        Check(totalItems==stats.Actors.Count(actor=>(HereticDefinitions.Actors[(int)actor.Type].Flags&HereticActorFlags.MF_COUNTITEM)!=0),"Counted item total mismatch.");
        stats.StartMapCombat();Check(stats.TotalKills==totalKills && stats.TotalItems==totalItems,"Repeated combat initialization duplicated totals.");
        var victim=stats.CombatEnemies.First();stats.DamageTestEnemy(victim.Body,10000,environment:true);stats.DamageTestEnemy(victim.Body,10000,environment:true);
        Check(stats.TestKills==1,"Repeated damage counted corpse twice.");
        stats.SpawnEnemyAmmoDrop(stats.Body,HereticActorType.MT_MISC3,0);
        stats.State.QuartzFlasks=16;stats.Tick(default);Check(stats.ItemsCollected==0,"Full inventory pickup counted.");
        stats.State.QuartzFlasks=0;stats.Tick(default);Check(stats.ItemsCollected==1,"Accepted artifact pickup not counted.");
        stats.Tick(default);Check(stats.ItemsCollected==1 && stats.TotalItems==totalItems,"Artifact effect recounted or drop changed map total.");
        var statsExit=stats.World.Map.Lines[0];statsExit.Special=(LineSpecial)52;stats.CrossLine(statsExit,0,stats.Body);
        var snapshot=stats.Completion.Value;
        Check(snapshot.Kills==1 && snapshot.TotalKills==totalKills && snapshot.Items==1 && snapshot.TotalItems==totalItems && snapshot.TotalSecrets==secrets,"Completion lost map statistics.");
        stats.DamageTestEnemy(stats.CombatEnemies.First(enemy=>enemy.Body.Health>0).Body,10000,environment:true);
        Check(stats.Completion.Value==snapshot && stats.CreateNextCampaignSession().ItemsCollected==0,"Stats snapshot mutated or leaked across maps.");
        Console.WriteLine("PASS intermission statistics: map totals, idempotent setup, single corpse count, rejected/accepted artifact pickup and frozen exit stats");
        Console.WriteLine("PASS death restart: fresh health/loadout/inventory, restored map roster, skill, idempotence, live/network guards and campaign continuation");
        Console.WriteLine("PASS campaign carry: immutable health/armor/weapons/ammo, native artifact cleanup, fresh map combat, chicken restore and idempotent transition");
        Console.WriteLine("PASS campaign exit routing: five episodes, secret returns/finales, unknown route guard, all four exit specials, immutable stats and stopped simulation");
    }
}
