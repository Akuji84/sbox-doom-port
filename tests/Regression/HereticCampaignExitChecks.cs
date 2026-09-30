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
        Console.WriteLine("PASS campaign exit routing: five episodes, secret returns/finales, unknown route guard, all four exit specials, immutable stats and stopped simulation");
    }
}
