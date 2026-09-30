// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticChaseSoundChecks
{
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    public static void Verify(string root)
    {
        using var content=GameContent.CreateHereticPreview(Path.Combine(root,"Assets/doom/blasphem.wad"));
        foreach(var type in new[]{HereticActorType.MT_IMP,HereticActorType.MT_IMPLEADER})
        {
            var s=new HereticWorldSession(content,1,2);var e=s.StartEnemyTest(type);
            Check(e!=null,"Missing range fixture.");e.Body.Target=s.Body;e.Body.ReactionTime=0;
            var def=HereticDefinitions.Actors[(int)type];
            var distance=Geometry.AproxDistance(e.Body.X-s.Body.X,e.Body.Y-s.Body.Y).ToIntFloor()-64;
            if(def.MeleeState==HereticStateId.S_NULL)distance-=128;
            if(type==HereticActorType.MT_IMP)distance>>=1;
            for(int i=0;i<256;i++)
            {
                s.World.Random.Index=i;var roll=s.World.Random.Next();var end=s.World.Random.Index;
                s.World.Random.Index=i;
                Check(e.CheckMissileRange()==(roll>=Math.Min(distance,200)) && s.World.Random.Index==end,"Gargoyle attack probability/RNG mismatch.");
            }
            e.Body.Flags|=MobjFlags.JustHit;e.Body.ReactionTime=5;s.World.Random.Clear();
            Check(e.CheckMissileRange() && s.World.Random.Index==0 && (e.Body.Flags&MobjFlags.JustHit)==0,"Retaliation must bypass reaction and random range gate.");
            Check(!e.CheckMissileRange() && s.World.Random.Index==0,"Reaction delay consumed RNG.");
        }
        foreach(var type in new[]{HereticActorType.MT_CLINK,HereticActorType.MT_WIZARD,HereticActorType.MT_SORCERER2})
        {
            var s=new HereticWorldSession(content,1,2);var e=new HereticClinkTestEnemy(s,type);
            var def=HereticDefinitions.Actors[(int)type];
            HereticSoundId? heard=null;Mobj origin=null;
            e.SoundRequested+=(sound,body)=>{heard=sound;origin=body;};
            s.SoundRequested+=(sound,body)=>{heard=sound;origin=body;};
            int emitted=0;
            for(int i=0;i<256;i++)
            {
                s.World.Random.Index=i;
                bool emit=(int)def.ActiveSound!=0 && s.World.Random.Next()<3;
                var expected=def.ActiveSound;
                if(emit && type==HereticActorType.MT_WIZARD && s.World.Random.Next()<128)expected=def.SeeSound;
                var end=s.World.Random.Index;
                s.World.Random.Index=i;heard=null;origin=null;e.PlayChaseSound();
                Check(s.World.Random.Index==end && heard.HasValue==emit,"Chase sound probability/RNG mismatch.");
                if(emit){emitted++;Check(heard==expected && ReferenceEquals(origin,type==HereticActorType.MT_SORCERER2?s.Body:e.Body),"Chase sound selection/origin mismatch.");}
            }
            Check(emitted>0,"Sound fixture never emitted.");
        }
        Console.WriteLine("PASS native Gargoyle range bias, retaliation/reaction guards and chase sound probability, Disciple variants and Sorcerer full-volume origin");
    }
}
