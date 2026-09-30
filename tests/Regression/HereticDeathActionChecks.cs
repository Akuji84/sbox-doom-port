// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticDeathActionChecks
{
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    public static void Verify(string root)
    {
        using var content=GameContent.CreateHereticPreview(Path.Combine(root,"Assets/doom/blasphem.wad"));
        var s=new HereticWorldSession(content,1,2);
        foreach(var type in new[]{HereticActorType.MT_IMP,HereticActorType.MT_IMPLEADER,HereticActorType.MT_SORCERER1,HereticActorType.MT_SORCERER2})
        {
            var e=new HereticClinkTestEnemy(s,type);int drops=0;e.DropRequested+=_=>drops++;
            e.Body.Flags|=MobjFlags.Solid;s.World.Random.Clear();
            e.Execute(HereticAction.A_NoBlocking,e.Combatant.Animation);
            Check(drops==0 && s.World.Random.Index==0 && (e.Body.Flags&MobjFlags.Solid)==0,"Non-dropping actor emitted loot or consumed RNG.");
        }
        foreach(var fixture in new[]{
            (HereticActorType.MT_MUMMY,HereticActorType.MT_AMGWNDWIMPY,3),
            (HereticActorType.MT_MUMMYGHOST,HereticActorType.MT_AMGWNDWIMPY,3),
            (HereticActorType.MT_MUMMYLEADER,HereticActorType.MT_AMGWNDWIMPY,3),
            (HereticActorType.MT_MUMMYLEADERGHOST,HereticActorType.MT_AMGWNDWIMPY,3),
            (HereticActorType.MT_CLINK,HereticActorType.MT_AMSKRDWIMPY,20),
            (HereticActorType.MT_SNAKE,HereticActorType.MT_AMPHRDWIMPY,5),
            (HereticActorType.MT_KNIGHT,HereticActorType.MT_AMCBOWWIMPY,5),
            (HereticActorType.MT_KNIGHTGHOST,HereticActorType.MT_AMCBOWWIMPY,5),
            (HereticActorType.MT_BEAST,HereticActorType.MT_AMCBOWWIMPY,10)})
        {
            var e=new HereticClinkTestEnemy(s,fixture.Item1);e.Body.X=s.Body.X;e.Body.Y=s.Body.Y;e.Body.Z=s.Body.Z;
            HereticTestDrop? drop=null;e.DropRequested+=d=>drop=d;
            s.World.Random.Clear();e.Execute(HereticAction.A_NoBlocking,e.Combatant.Animation);
            Check(drop.HasValue && drop.Value.Type==fixture.Item2 && drop.Value.Amount==fixture.Item3,"Native ammo drop type/amount mismatch.");
            drop=null;s.World.Random.Index=1;e.Execute(HereticAction.A_NoBlocking,e.Combatant.Animation);
            Check(!drop.HasValue && s.World.Random.Index==2,"Failed drop roll emitted loot or consumed extra randomness.");
        }
        foreach(var type in new[]{HereticActorType.MT_MINOTAUR,HereticActorType.MT_SORCERER1,HereticActorType.MT_HEAD,HereticActorType.MT_CLINK})
        {
            var e=new HereticClinkTestEnemy(s,type);Mobj origin=null;HereticSoundId? heard=null;
            Action<HereticSoundId,Mobj> listener=(sound,body)=>{heard=sound;origin=body;};
            e.SoundRequested+=listener;s.SoundRequested+=listener;s.World.Random.Clear();
            e.Execute(HereticAction.A_Scream,e.Combatant.Animation);s.SoundRequested-=listener;
            bool full=type is HereticActorType.MT_MINOTAUR or HereticActorType.MT_SORCERER1;
            Check(heard==HereticDefinitions.Actors[(int)type].DeathSound && ReferenceEquals(origin,full?s.Body:e.Body) && s.World.Random.Index==0,"Death sound volume/origin or RNG mismatch.");
        }
        Console.WriteLine("PASS native death actions: explicit ammo families, no-drop actors/RNG, failed rolls and boss versus positional death sounds");
    }
}
