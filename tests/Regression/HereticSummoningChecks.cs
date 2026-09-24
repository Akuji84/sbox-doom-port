// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticSummoningChecks
{
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    public static void Verify(string root)
    {
        using var content=GameContent.CreateHereticPreview(Path.Combine(root,"Assets/doom/blasphem.wad"));
        var pair=new HereticWorldSession(content,1,2);var caster=pair.StartEnemyTest(HereticActorType.MT_WIZARD);caster.Body.Target=pair.Body;
        caster.Body.Angle=Geometry.PointToAngle(caster.Body.X,caster.Body.Y,pair.Body.X,pair.Body.Y);
        pair.SpawnSorcererSummoners(caster);var missiles=pair.Projectiles.ToArray();
        Check(missiles.Length==2,"Summoning pair missing.");
        for(var i=0;i<2;i++)
        {
            var p=missiles[i];var angle=i==0?caster.Body.Angle-Angle.Ang45:caster.Body.Angle+Angle.Ang45;
            Check(p.Type==HereticActorType.MT_SOR2FX2 && p.Body.Target==caster.Body && p.Body.Angle==angle,"Summoning angle/ownership incorrect.");
            if(p.Flying)Check(p.Body.MomX==6*Trig.Cos(angle) && p.Body.MomZ==Fixed.One/2,"Summoning speed/vertical momentum incorrect.");
        }
        var s=new HereticWorldSession(content,1,2);var blocker=s.StartEnemyTest(HereticActorType.MT_WIZARD);blocker.Body.Target=s.Body;
        var summon=s.SpawnMonsterMissile(blocker,HereticActorType.MT_SOR2FX2,Geometry.PointToAngle(blocker.Body.X,blocker.Body.Y,s.Body.X,s.Body.Y),Fixed.One/2);
        Check(summon.Flying,"Summoner fixture impacted at launch.");
        s.World.ThingMovement.UnsetThingPosition(summon.Body);summon.Body.X=blocker.Body.X;summon.Body.Y=blocker.Body.Y;summon.Body.Z=blocker.Body.Z+blocker.Body.Height/2;s.World.ThingMovement.SetThingPosition(summon.Body);
        var initial=summon.Animation.Tics;var count=s.TestEnemyCount;
        for(var i=0;i<initial-1;i++)summon.Animation.Tick();
        Check(s.TestEnemyCount==count && summon.Animation.State==HereticStateId.S_SOR2FX2_1,"Summon happened before native delay.");
        s.World.Random.Clear();summon.Animation.Tick();
        Check(s.TestEnemyCount==count && summon.Flying && s.World.Random.Index==1 && !s.ImpactEffects.Any(e=>e.Type==HereticActorType.MT_TFOG),"Blocked summon leaked actor/effects or stopped missile.");
        blocker.Body.Flags&=~MobjFlags.Solid;
        var savedZ=summon.Body.Z;summon.Body.Z=blocker.Body.FloorZ-Fixed.One;
        Check(!s.TrySummonDisciple(summon.Body),"Below-floor summon accepted.");
        summon.Body.Z=blocker.Body.CeilingZ;
        Check(!s.TrySummonDisciple(summon.Body),"Ceiling-clipped summon accepted.");summon.Body.Z=savedZ;
        for(var i=0;i<9;i++)summon.Animation.Tick();Check(s.TestEnemyCount==count,"Blocked summon retried too early.");
        summon.Animation.Tick();
        Check(s.TestEnemyCount==count+1 && !summon.Flying && summon.Body.MomX==Fixed.Zero && summon.Body.MomZ==Fixed.Zero,"Retry failed to create exactly one Disciple and stop missile.");
        var wizard=s.CombatEnemies.Last();
        Check(wizard.Combatant.Type==HereticActorType.MT_WIZARD && wizard.Body.Z==savedZ-wizard.Body.Height/2 && wizard.Body.Health==HereticDefinitions.Actors[(int)HereticActorType.MT_WIZARD].SpawnHealth,"Summoned Disciple type/height/health incorrect.");
        Check(s.ImpactEffects.Count(e=>e.Type==HereticActorType.MT_TFOG)==1,"Summon fog missing/duplicated.");
        var pixels=new byte[320*200*4];s.Body.Angle=Geometry.PointToAngle(s.Body.X,s.Body.Y,wizard.Body.X,wizard.Body.Y);new HereticMapPreview(content,s).Render(pixels,0);
        var output=Environment.GetEnvironmentVariable("HERETIC_SUMMON_RGBA");if(!string.IsNullOrEmpty(output))File.WriteAllBytes(output,pixels);
        blocker.Body.Flags&=~MobjFlags.Shootable;blocker.Body.Health=0;blocker.Combatant.Animation.SetState(HereticStateId.S_WIZARD_DIE1);
        for(var i=0;i<150 && s.State.Health==100;i++)s.Tick(default);
        Check(s.State.Health<100,"Summoned Disciple failed to enter combat.");
        for(var i=0;i<40;i++)s.Tick(default);
        Check(!s.Projectiles.Contains(summon) && s.TestEnemyCount==count+1,"Summon failed cleanup or repeated after success.");
        Check(!HereticWorldSession.SupportsMapEnemy(HereticActorType.MT_SORCERER2),"Unfinished D'Sparil enabled.");
        Console.WriteLine("PASS Disciple summoning: pair speed/angles, native delay/retry, blocked and height rejection, single successful spawn/fog, live AI and missile cleanup");
    }
}
