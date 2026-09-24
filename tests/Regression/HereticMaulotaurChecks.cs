// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticMaulotaurChecks
{
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    public static void Verify(string root)
    {
        using var content=GameContent.CreateHereticPreview(Path.Combine(root,"Assets/doom/blasphem.wad"));
        var s=new HereticWorldSession(content);var caster=s.StartEnemyTest(HereticActorType.MT_IMPLEADER);
        caster.Body.Target=s.Body;s.SpawnMaulotaurSpread(caster);
        var volley=s.Projectiles.ToArray();Check(volley.Length==5,"Maulotaur spread missing shots.");
        var offsets=new[]{0,-0x04000000,0x04000000,-0x02000000,0x02000000};
        for(var i=0;i<5;i++)
        {
            var p=volley[i];Check(p.Type==HereticActorType.MT_MNTRFX1 && p.Body.Target==caster.Body && p.Body.Angle==volley[0].Body.Angle+new Angle(unchecked((uint)offsets[i])),"Spread ownership/angles incorrect.");
            Check(p.Body.MomZ==volley[0].Body.MomZ && p.Body.MomX==20*Trig.Cos(p.Body.Angle),"Spread slope/speed incorrect.");
            Check(p.Body.Z==caster.Body.Z+Fixed.FromInt(40),"Spread launch height incorrect.");
        }
        var shot=volley[0];s.World.Random.Clear();Check(shot.Contact(caster.Body),"Spread hit its owner.");
        shot.Body.Z=s.Body.Z;shot.Contact(s.Body);Check(s.State.Health==97,"Spread damage incorrect.");
        var fireSession=new HereticWorldSession(content);var owner=fireSession.StartEnemyTest(HereticActorType.MT_IMPLEADER);owner.Body.Target=fireSession.Body;
        var fire=fireSession.SpawnMaulotaurFloorFire(owner);
        Check(fire.Flying && fire.Body.Z==fire.Body.FloorZ && fire.Body.MomX==14*Trig.Cos(fire.Body.Angle),"Floor fire launch incorrect.");
        fire.Body.MomX=fire.Body.MomY=fire.Body.MomZ=Fixed.Zero;
        var sector=fire.Body.Subsector.Sector;sector.FloorHeight+=Fixed.FromInt(8);
        fire.Advance();Check(fire.Flying && fire.Body.Z==sector.FloorHeight,"Floor fire did not climb a small step.");
        fireSession.World.Random.Clear();fire.Execute(HereticAction.A_MntrFloorFire,fire.Animation);
        var patch=fireSession.Projectiles.Single(p=>p.Type==HereticActorType.MT_MNTRFX3);
        Check(patch.Body.Target==owner.Body && patch.MonsterOwnerType==HereticActorType.MT_IMPLEADER && patch.Body.MomX==new Fixed(1),"Floor flame ownership/contact motion incorrect.");
        Check(patch.Flying && patch.Body.Z==patch.Body.FloorZ,"Floor flame exploded on resting floor contact.");
        var pixels=new byte[320*200*4];fireSession.Body.Angle=Geometry.PointToAngle(fireSession.Body.X,fireSession.Body.Y,fire.Body.X,fire.Body.Y);
        new HereticMapPreview(content,fireSession).Render(pixels,0);
        var output=Environment.GetEnvironmentVariable("HERETIC_MAULOTAUR_RGBA");if(!string.IsNullOrEmpty(output))File.WriteAllBytes(output,pixels);
        for(var i=0;i<45;i++)patch.Tick();Check(patch.Animation.Removed,"Floor flame did not expire.");
        var origin=new Mobj(fireSession.World){X=fireSession.Body.X,Y=fireSession.Body.Y,Z=fireSession.Body.Z,Subsector=fireSession.Body.Subsector};
        Check(fireSession.PhoenixBlastDamage(origin,fireSession.Body,24)==24 && fireSession.PhoenixBlastDamage(origin,fireSession.Body)==128,"Explosion radius damage leaked between projectile families.");
        var f=new HereticWorldSession(content);var attacker=f.StartEnemyTest(HereticActorType.MT_IMPLEADER);attacker.Body.Target=f.Body;
        var moving=f.SpawnMaulotaurFloorFire(attacker);var spawned=false;
        for(var i=0;i<100;i++){moving.Tick();spawned|=f.Projectiles.Any(p=>p.Type==HereticActorType.MT_MNTRFX3);if(moving.Animation.Removed)break;}
        Check(spawned,"Traveling floor fire did not generate its native trail.");
        f.DamageTestEnemy(attacker.Body,10000);for(var i=0;i<200;i++)f.Tick(default);
        Check(f.Projectiles.Count==0,"Maulotaur fire leaked after collision/expiry.");
        Check(HereticWorldSession.SupportsMapEnemy(HereticActorType.MT_MINOTAUR),"Maulotaur roster missing.");
        Console.WriteLine("PASS Maulotaur projectiles: five-shot spread, height/speed/slope/damage, ownership, floor launch/step/trail, flame expiry, separate splash radii and cleanup");
    }
}
