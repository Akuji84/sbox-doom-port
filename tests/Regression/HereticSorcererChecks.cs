// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticSorcererChecks
{
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    public static void Verify(string root)
    {
        using var content=GameContent.CreateHereticPreview(Path.Combine(root,"Assets/doom/blasphem.wad"));
        foreach(var spread in new[]{false,true})
        {
            var s=new HereticWorldSession(content);var e=s.StartEnemyTest(HereticActorType.MT_IMPLEADER);e.Body.Target=s.Body;
            var sounds=new List<HereticSoundId>();s.SoundRequested+=(id,_)=>sounds.Add(id);
            s.SpawnSorcererFire(e,spread);var shots=s.Projectiles.ToArray();
            Check(shots.Length==(spread?3:1) && sounds.Contains(HereticSoundId.sfx_sbtatk),"Mounted fire count/sound incorrect.");
            var offsets=new[]{0,-0x03000000,0x03000000};
            for(var i=0;i<shots.Length;i++)
            {
                var p=shots[i];Check(p.Type==HereticActorType.MT_SRCRFX1 && p.Body.Target==e.Body && p.Body.Z==e.Body.Z+Fixed.FromInt(48),"Mounted fire type/owner/height incorrect.");
                Check(p.Body.Angle==shots[0].Body.Angle+new Angle(unchecked((uint)offsets[i])) && p.Body.MomZ==shots[0].Body.MomZ && p.Body.MomX==20*Trig.Cos(p.Body.Angle),"Mounted fire spread/slope/speed incorrect.");
            }
            var shot=shots[0];s.World.Random.Clear();var rng=s.World.Random.Index;
            Check(shot.Contact(e.Body) && s.World.Random.Index==rng,"Mounted fire hit owner.");
            shot.Body.Z=s.Body.Z;shot.Contact(s.Body);Check(s.State.Health==90,"Mounted fire native damage incorrect.");
        }
        var blue=new HereticWorldSession(content);var caster=blue.StartEnemyTest(HereticActorType.MT_IMPLEADER);caster.Body.Target=blue.Body;
        var bolt=blue.SpawnSorcererBlueBolt(caster);
        Check(bolt.Flying && bolt.Body.MomX==20*Trig.Cos(bolt.Body.Angle),"Blue bolt speed incorrect.");
        bolt.Body.Z=blue.Body.Z;blue.World.Random.Clear();bolt.Contact(blue.Body);
        Check(blue.State.Health==99,"Blue bolt direct damage incorrect.");
        bolt.Execute(HereticAction.A_BlueSpark,bolt.Animation);
        var sparks=blue.ImpactEffects.Where(a=>a.Type==HereticActorType.MT_SOR2FXSPARK).ToArray();
        Check(sparks.Length==2 && sparks.All(a=>a.Body.Z==bolt.Body.Z && a.Body.MomZ>=Fixed.One && a.Body.MomZ<Fixed.FromInt(2)),"Blue spark count/height/velocity incorrect.");
        var pixels=new byte[320*200*4];blue.Body.Angle=Geometry.PointToAngle(blue.Body.X,blue.Body.Y,bolt.Body.X,bolt.Body.Y);
        new HereticMapPreview(content,blue).Render(pixels,0);
        var output=Environment.GetEnvironmentVariable("HERETIC_SORCERER_RGBA");if(!string.IsNullOrEmpty(output))File.WriteAllBytes(output,pixels);
        var blast=new HereticWorldSession(content);var owner=blast.StartEnemyTest(HereticActorType.MT_IMPLEADER);owner.Body.Target=blast.Body;
        var exploding=blast.SpawnSorcererBlueBolt(owner);
        blast.World.ThingMovement.UnsetThingPosition(exploding.Body);exploding.Body.X=blast.Body.X;exploding.Body.Y=blast.Body.Y;exploding.Body.Z=blast.Body.Z+Fixed.One;blast.World.ThingMovement.SetThingPosition(exploding.Body);
        exploding.Body.MomX=exploding.Body.MomY=Fixed.Zero;exploding.Body.MomZ=-Fixed.FromInt(64);
        blast.World.Random.Clear();exploding.Tick();
        Check(!exploding.Flying && blast.State.Health==12,"Blue bolt impact did not use native 80+(random&31) splash.");
        blast.DamageTestEnemy(owner.Body,10000);for(var i=0;i<150;i++)blast.Tick(default);
        Check(blast.Projectiles.Count==0,"Blue explosion did not clean up.");
        var effects=new HereticWorldSession(content);effects.SpawnSorcererSparks(effects.Body);var spark=effects.ImpactEffects[0];var z=spark.Body.Z;
        effects.Tick(default);Check(spark.Body.Z>z,"Blue spark did not rise.");
        for(var i=0;i<40;i++)effects.Tick(default);
        Check(!effects.ImpactEffects.Any(a=>a.Type==HereticActorType.MT_SOR2FXSPARK),"Blue sparks leaked.");
        Check(!HereticWorldSession.SupportsMapEnemy(HereticActorType.MT_SORCERER1) && !HereticWorldSession.SupportsMapEnemy(HereticActorType.MT_SORCERER2),"Unfinished D'Sparil enabled.");
        Console.WriteLine("PASS D'Sparil projectiles: mounted single/triple fire, native height/angles/speed/damage, ownership, blue bolt sparks/splash, rendering and cleanup");
    }
}
