// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticSorcererMapChecks
{
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    static HereticClinkTestEnemy Raise(HereticWorldSession s,HereticClinkTestEnemy mount)
    {
        s.DamageTestEnemy(mount.Body,2000,environment:true);
        for(var i=0;i<200 && !s.CombatEnemies.Any(e=>e.Combatant.Type==HereticActorType.MT_SORCERER2);i++)s.Tick(default);
        return s.CombatEnemies.Single(e=>e.Combatant.Type==HereticActorType.MT_SORCERER2);
    }
    public static void Verify(string root)
    {
        using var content=GameContent.CreateHereticPreview(Path.Combine(root,"Assets/doom/blasphem.wad"));
        var encounters=0;var destinations=0;
        foreach(var (episode,map) in new[]{(3,8),(6,1),(6,3)})
        foreach(var skill in new[]{GameSkill.Easy,GameSkill.Medium,GameSkill.Hard})
        {
            var s=new HereticWorldSession(content,episode,map,skill);s.StartMapCombat();
            var mount=s.CombatEnemies.Single(e=>e.Combatant.Type==HereticActorType.MT_SORCERER1);
            var placement=s.World.Map.Things.Single(t=>t.Type==7);
            Check(mount.Body.X==placement.X && mount.Body.Y==placement.Y && mount.Body.Angle==placement.Angle,"Mounted map placement mismatch.");
            var count=s.TestEnemyCount;s.StartMapCombat();Check(count==s.TestEnemyCount,"Repeated map activation duplicated boss.");
            var rider=Raise(s,mount);Check(!s.EpisodeBossTriggered,"Mounted death completed episode prematurely.");
            var successes=0;
            foreach(var spot in s.World.Map.Things.Where(t=>t.Type==56))
            {
                if(!s.TryTeleportSorcerer(rider,spot.X,spot.Y,spot.Angle))continue;
                successes++;destinations++;
                Check(rider.Body.X==spot.X && rider.Body.Y==spot.Y && rider.Body.Z==rider.Body.FloorZ && rider.Body.Angle==spot.Angle,"Map teleport destination mismatch.");
            }
            Check(successes>0,$"E{episode}M{map} has no usable boss teleport spot.");
            var oldX=rider.Body.X;var oldY=rider.Body.Y;rider.Body.Health=1;s.World.Random.Clear();
            s.DecideSorcererTeleport(rider);
            Check((rider.Body.X!=oldX || rider.Body.Y!=oldY) && s.World.Map.Things.Any(t=>t.Type==56 && t.X==rider.Body.X && t.Y==rider.Body.Y),"Native decision failed to choose a distant map boss spot.");
            rider.Body.Health=3500;
            new HereticMapPreview(content,s).Render(new byte[320*200*4],s.World.LevelTime);
            s.DamageTestEnemy(rider.Body,3500,environment:true);
            for(var i=0;i<300 && !s.EpisodeBossTriggered;i++)s.Tick(default);
            Check(s.EpisodeBossTriggered==(episode==3 && map==8),"Map boss death completed wrong episode.");
            encounters++;
        }
        // A rejected duplicate boss must prevent falsely declaring its map complete.
        var blocked=new HereticWorldSession(content,3,8);var things=blocked.World.Map.Things;
        var bossThing=things.Single(t=>t.Type==7);things[^1]=bossThing;blocked.StartMapCombat();
        var survivingMount=blocked.CombatEnemies.Single(e=>e.Combatant.Type==HereticActorType.MT_SORCERER1);
        Check(blocked.BlockedMapEnemies>0,"Duplicate boss fixture was not rejected.");
        var remaining=Raise(blocked,survivingMount);blocked.DamageTestEnemy(remaining.Body,3500,environment:true);
        for(var i=0;i<300;i++)blocked.Tick(default);
        Check(!blocked.EpisodeBossTriggered,"Failed boss placement allowed false completion.");
        Console.WriteLine($"PASS Sorcerer map encounters: {encounters} map/skill cases, {destinations} usable teleport destinations, phase/render/death, duplicate activation and blocked-boss completion guard");
    }
}
