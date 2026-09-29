// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticInfightingChecks
{
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    static HereticClinkTestEnemy Spawn(HereticWorldSession s,HereticActorType type)
    {
        foreach(var thing in s.World.Map.Things){var e=s.TrySpawnSupportedEnemy(type,thing.X,thing.Y,sorcererPreview:true);if(e!=null)return e;}
        throw new Exception("No target fixture space.");
    }
    public static void Verify(string root)
    {
        using var content=GameContent.CreateHereticPreview(Path.Combine(root,"Assets/doom/blasphem.wad"));
        foreach(var (type,action) in new[]{(HereticActorType.MT_CLINK,HereticAction.A_ClinkAttack),(HereticActorType.MT_MUMMY,HereticAction.A_MummyAttack),(HereticActorType.MT_KNIGHT,HereticAction.A_KnightAttack),(HereticActorType.MT_BEAST,HereticAction.A_BeastAttack),(HereticActorType.MT_WIZARD,HereticAction.A_WizAtk3),(HereticActorType.MT_HEAD,HereticAction.A_HeadAttack),(HereticActorType.MT_MINOTAUR,HereticAction.A_MinotaurAtk1),(HereticActorType.MT_SORCERER1,HereticAction.A_Srcr1Attack),(HereticActorType.MT_SORCERER2,HereticAction.A_Srcr2Attack)})
        {
            var s=new HereticWorldSession(content,1,2);var attacker=Spawn(s,type);var victim=Spawn(s,HereticActorType.MT_CLINK);
            s.World.ThingMovement.UnsetThingPosition(victim.Body);victim.Body.X=attacker.Body.X;victim.Body.Y=attacker.Body.Y;victim.Body.Z=attacker.Body.Z;s.World.ThingMovement.SetThingPosition(victim.Body);
            victim.Body.Health=10000;attacker.Body.Target=victim.Body;victim.Body.Target=null;victim.Body.Threshold=0;
            var playerHealth=s.State.Health;var camera=s.Camera.DeltaViewHeight;
            attacker.Execute(action,attacker.Combatant.Animation);
            Check(victim.Body.Health<10000 && s.State.Health==playerHealth && s.Camera.DeltaViewHeight==camera,"Melee hit player instead of selected monster: "+type);
            Check((victim.Body.Target==attacker.Body)==!s.IsBoss(attacker.Body),"Melee lost damage source or boss retaliation exclusion.");
        }
        var ranged=new HereticWorldSession(content,1,2);var shooter=ranged.StartEnemyTest(HereticActorType.MT_MUMMYLEADER);var target=Spawn(ranged,HereticActorType.MT_BEAST);
        shooter.Body.Target=target.Body;
        shooter.Execute(HereticAction.A_FaceTarget,shooter.Combatant.Animation);
        var expected=Geometry.PointToAngle(shooter.Body.X,shooter.Body.Y,target.Body.X,target.Body.Y);
        Check(shooter.Body.Angle==expected,"FaceTarget still points at player.");
        ranged.DamageEnvironment(10000);var missile=ranged.SpawnNitrogolemMissile(shooter);
        Check(missile!=null && missile.Body.Angle==expected && missile.Body.Target==shooter.Body,"Monster missile failed to aim/fire after player death.");
        if(missile.Flying)Check(missile.SeekerTarget==target.Body,"Homing skull tracks player instead of monster.");
        var health=target.Body.Health;target.Body.Threshold=0;target.Body.Target=null;missile.Body.Z=target.Body.Z;
        // Direct contact uses the same damage routing as flight collision.
        ranged.DamageMonsterMissile(target.Body,5,missile.Body);
        Check(target.Body.Health==health-5 && target.Body.Target==shooter.Body,"Projectile damage lost infighting attribution.");
        var before=shooter.Body.Health;target.Execute(HereticAction.A_FaceTarget,target.Combatant.Animation);
        Check(target.Body.Angle==Geometry.PointToAngle(target.Body.X,target.Body.Y,shooter.Body.X,shooter.Body.Y),"Retaliating monster faces dead player.");
        Console.WriteLine("PASS target-aware attacks: nine melee families, player/camera isolation, source attribution, boss exclusions, ranged aim and retaliation after player death");
    }
}
