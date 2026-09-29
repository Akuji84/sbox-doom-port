// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticNoiseChecks
{
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    public static void Verify(string root)
    {
        using var content=GameContent.CreateHereticPreview(Path.Combine(root,"Assets/doom/blasphem.wad"));
        var s=new HereticWorldSession(content,1,2);
        var sectors=Enumerable.Range(0,4).Select(i=>new Sector(i,Fixed.Zero,Fixed.FromInt(128),0,0,160,0,0)).ToArray();
        var links=new List<LineDef>();
        LineDef Link(int a,int b,bool block)
        {
            var sideA=new SideDef(Fixed.Zero,Fixed.Zero,0,0,0,sectors[a]);var sideB=new SideDef(Fixed.Zero,Fixed.Zero,0,0,0,sectors[b]);
            var line=new LineDef(new Vertex(Fixed.Zero,Fixed.Zero),new Vertex(Fixed.FromInt(64),Fixed.Zero),LineFlags.TwoSided|(block?LineFlags.SoundBlock:0),0,0,sideA,sideB);links.Add(line);return line;
        }
        void Refresh(){foreach(var sector in sectors){sector.Lines=links.Where(l=>l.FrontSector==sector || l.BackSector==sector).ToArray();sector.SoundTarget=null;}}
        Link(0,1,true);Link(1,2,false);Link(2,3,true);Refresh();
        var emitter=new Mobj(s.World){Subsector=new Subsector(sectors[0],0,0)};var rng=s.World.Random.Index;
        s.NoiseAlert(s.Body,emitter);
        Check(sectors.Take(3).All(x=>x.SoundTarget==s.Body) && sectors[3].SoundTarget==null,"Sound did not respect one-block boundary allowance.");
        Link(0,2,false);Refresh();s.NoiseAlert(s.Body,emitter);
        Check(sectors.All(x=>x.SoundTarget==s.Body) && sectors[2].SoundTraversed==1,"Alternate lower-cost sound route failed.");
        sectors[2].CeilingHeight=Fixed.Zero;Refresh();s.NoiseAlert(s.Body,emitter);
        Check(sectors[2].SoundTarget==null && sectors[3].SoundTarget==null,"Closed portal leaked sound.");
        Check(s.World.Random.Index==rng,"Sound propagation consumed gameplay RNG.");
        var enemy=s.StartClinkTest();var def=HereticDefinitions.Actors[(int)enemy.Combatant.Type];
        enemy.Body.Angle+=Angle.Ang180;enemy.Body.Subsector.Sector.SoundTarget=s.Body;enemy.Body.Target=null;
        enemy.Execute(HereticAction.A_Look,enemy.Combatant.Animation);
        Check(enemy.Body.Target==s.Body && enemy.Combatant.Animation.State!=def.SpawnState,"Sound failed to wake rear-facing enemy.");
        var hidden=s.World.Map.Things.Select(t=>new Mobj(s.World){X=t.X,Y=t.Y,Health=100,Flags=MobjFlags.Shootable,Height=Fixed.FromInt(56),Subsector=Geometry.PointInSubsector(t.X,t.Y,s.World.Map)}).First(t=>!new VisibilityCheck(s.World).CheckSight(enemy.Body,t));
        enemy.Body.Flags|=MobjFlags.Ambush;enemy.Body.Target=null;enemy.Body.Angle=Geometry.PointToAngle(enemy.Body.X,enemy.Body.Y,s.Body.X,s.Body.Y)+Angle.Ang180;
        enemy.Body.Subsector.Sector.SoundTarget=hidden;enemy.Combatant.Animation.SetState(def.SpawnState,false);
        enemy.Execute(HereticAction.A_Look,enemy.Combatant.Animation);
        Check(enemy.Body.Target==null && enemy.Combatant.Animation.State==def.SpawnState,"Ambush enemy followed unseen sound target.");
        var weaponSession=new HereticWorldSession(content);var wand=new HereticGoldWand(weaponSession);
        for(var i=0;i<20;i++)wand.Tick(true);
        Check(weaponSession.Body.Subsector.Sector.SoundTarget==weaponSession.Body,"Weapon attack did not emit alert.");
        Console.WriteLine("PASS sound alerts: one-block limit, lower-cost routes, closed portals, RNG isolation, rear wake, ambush sight and weapon activation");
    }
}
