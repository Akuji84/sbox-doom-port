// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticCampaignExitChecks
{
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    public static void Verify(string root)
    {
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
        foreach(var code in new[]{11,51,52,105})
        {
            var s=new HereticWorldSession(content,1,2);s.Tick(default);s.State.Secrets=3;
            Check(s.Completion==null,"Completion exists before exit.");
            var line=s.World.Map.Lines[0];line.Special=(LineSpecial)code;
            if(code==11 || code==51)s.UseLine(line);else s.CrossLine(line,0,s.Body);
            bool secret=code==51 || code==105;
            Check(s.ExitRequested && s.SecretExitRequested==secret && s.Completion.HasValue,"Exit action did not publish completion.");
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
        Console.WriteLine("PASS death restart: fresh health/loadout/inventory, restored map roster, skill, idempotence, live/network guards and campaign continuation");
        Console.WriteLine("PASS campaign carry: immutable health/armor/weapons/ammo, native artifact cleanup, fresh map combat, chicken restore and idempotent transition");
        Console.WriteLine("PASS campaign exit routing: five episodes, secret returns/finales, unknown route guard, all four exit specials, immutable stats and stopped simulation");
    }
}
