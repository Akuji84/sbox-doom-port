// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticIronLichChecks
{
    static void Check(bool ok, string text) { if (!ok) throw new Exception(text); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var s = new HereticWorldSession(content); var owner = s.StartEnemyTest(HereticActorType.MT_IMPLEADER);
        var sounds = new List<HereticSoundId>(); s.SoundRequested += (id, _) => sounds.Add(id);
        var ice = s.SpawnIronLichIce(owner);
        Check(ice.Flying && ice.Type == HereticActorType.MT_HEADFX1 && ice.Body.Target == owner.Body, "Ice spawn/ownership failed.");
        Check(ice.Body.MomX == 13 * Trig.Cos(ice.Body.Angle), "Ice speed is not 13.");
        ice.Body.Z = s.Body.Z + Fixed.FromInt(16);
        s.World.Random.Clear(); var rng=s.World.Random.Index;
        Check(ice.Contact(owner.Body) && s.World.Random.Index==rng,"Ice hit owner.");
        s.Body.Flags |= MobjFlags.Shadow;
        Check(ice.Contact(s.Body) && s.State.Health==100 && s.World.Random.Index==rng,"Ice did not pass ghost.");
        s.Body.Flags &= ~MobjFlags.Shadow;
        Check(!ice.Contact(s.Body) && s.State.Health==99,"Ice base damage incorrect.");
        ice.Body.MomX=ice.Body.MomY=Fixed.Zero; ice.Body.MomZ=-Fixed.FromInt(64); ice.Tick();
        var shards=s.Projectiles.Where(p=>p.Type==HereticActorType.MT_HEADFX2).ToArray();
        Check(!ice.Flying && shards.Length==8,"Actual ice impact did not spawn eight shards.");
        for(var i=0;i<8;i++)
        {
            var p=shards[i]; var a=Angle.FromDegree(i*45);
            Check(p.Body.Target==owner.Body && p.MonsterOwnerType==HereticActorType.MT_IMPLEADER && p.Body.Angle==a,"Shard ownership/angle incorrect.");
            if(p.Flying) Check(p.Body.MomX==8*Trig.Cos(a) && p.Body.MomY==8*Trig.Sin(a) && p.Body.MomZ==new Fixed(-39321),"Shard velocity incorrect.");
        }
        ice.Tick(); Check(s.Projectiles.Count(p=>p.Type==HereticActorType.MT_HEADFX2)==8,"Ice impact duplicated shards.");
        Check(sounds.Contains(HereticSoundId.sfx_hedat2),"Ice sound missing.");

        var f=new HereticWorldSession(content); var caster=f.StartEnemyTest(HereticActorType.MT_IMPLEADER);
        var fireSounds=0; f.SoundRequested+=(id,_)=>{if(id==HereticSoundId.sfx_hedat1)fireSounds++;};
        var baseFire=f.SpawnIronLichFire(caster);
        var flames=f.Projectiles.Where(p=>p.Type==HereticActorType.MT_HEADFX3).ToArray();
        Check(flames.Length==6 && baseFire.Flying && baseFire.Animation.State==HereticStateId.S_HEADFX3_4 && fireSounds==1,"Fire column missing base/layers/sound.");
        Check(baseFire.Body.MomX==10*Trig.Cos(baseFire.Body.Angle),"Fire column speed incorrect.");
        for(var i=1;i<6;i++)
        {
            var p=flames[i]; Check(p.Flying && p.Body.Target==caster.Body && p.Body.MomZ==baseFire.Body.MomZ && p.Body.Health==i*2,"Fire layer setup incorrect.");
            var z=p.Body.Z; p.Body.Z=f.Body.Z; f.World.Random.Clear(); p.Contact(f.Body);
            Check(f.State.Health==100,"Growing fire dealt damage."); p.Body.Z=z;
            for(var tick=0;tick<50 && p.Body.Health>0;tick++) p.Animation.Tick();
            Check(p.Body.Z==z+Fixed.FromInt(i*18) && p.Animation.State==HereticStateId.S_HEADFX3_4 && p.ContactDamageOverride==null,"Fire growth height/activation incorrect.");
        }
        var shardTest=f.SpawnMonsterMissile(caster,HereticActorType.MT_HEADFX2);
        shardTest.Body.Z=f.Body.Z; f.Body.Flags |= MobjFlags.Shadow;
        f.World.Random.Clear(); shardTest.Contact(f.Body);
        Check(f.State.Health==97,"Ice shard damage/ghost contact incorrect.");
        f.Body.Flags &= ~MobjFlags.Shadow;
        var active=flames[1]; active.Body.Z=f.Body.Z; f.World.Random.Clear(); active.Contact(f.Body);
        Check(f.State.Health==92,"Mature fire damage incorrect.");
        var pixels=new byte[320*200*4]; f.Body.Angle=Geometry.PointToAngle(f.Body.X,f.Body.Y,caster.Body.X,caster.Body.Y);
        new HereticMapPreview(content,f).Render(pixels,f.World.LevelTime);
        var output=Environment.GetEnvironmentVariable("HERETIC_LICH_RGBA"); if(!string.IsNullOrEmpty(output))File.WriteAllBytes(output,pixels);
        s.DamageTestEnemy(owner.Body,10000); f.DamageTestEnemy(caster.Body,10000);
        for(var i=0;i<500;i++){s.Tick(default);f.Tick(default);}
        Check(s.Projectiles.Count==0 && f.Projectiles.Count==0,"Ice/fire projectiles leaked after impacts.");
        Check(!HereticWorldSession.SupportsMapEnemy(HereticActorType.MT_HEAD),"Iron Lich enabled before full attack/death support.");
        Console.WriteLine("PASS Iron Lich ice/fire: native speeds, owner/ghost handling, impact shards, fire growth/damage, sounds, rendering and cleanup; actor remains gated");
    }
}
