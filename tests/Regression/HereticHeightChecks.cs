// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticHeightChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var s = new HereticWorldSession(content);
        var obstacle = s.Actors.First(a => (a.Body.Flags & MobjFlags.Solid) != 0).Body;
        // Isolate one real map actor at the start's otherwise traversable position.
        foreach (var actor in s.Actors) actor.Body.Flags &= ~MobjFlags.Solid;
        var movement = s.World.ThingMovement;
        movement.UnsetThingPosition(obstacle);
        obstacle.X = s.Body.X; obstacle.Y = s.Body.Y;
        obstacle.Height = Fixed.FromInt(16); obstacle.Radius = Fixed.FromInt(16);
        obstacle.Z = s.Body.FloorZ;
        obstacle.Flags |= MobjFlags.Solid;
        movement.SetThingPosition(obstacle);
        s.Body.Subsector.Sector.Special = 0;
        var floor = s.Body.FloorZ; var top = obstacle.Z + obstacle.Height;
        s.Body.Z = floor;
        Check(!movement.CheckPosition(s.Body, s.Body.X, s.Body.Y), "Overlapping actor volumes accepted.");
        s.Body.Z = top;
        Check(movement.CheckPosition(s.Body, s.Body.X, s.Body.Y), "Player cannot move along supporting surface.");
        s.Body.Z = top + Fixed.One;
        Check(movement.CheckPosition(s.Body, s.Body.X, s.Body.Y), "Over-actor movement blocked.");
        s.Body.Z = top + Fixed.FromInt(25); s.Body.MomZ = Fixed.FromInt(-50);
        s.Tick(default);
        Check(s.Body.Z == top && s.Body.MomZ == Fixed.Zero && s.StandingOnActor, "Fast fall tunneled through scenery.");
        for (var tic = 0; tic < 5; tic++) s.Tick(default);
        Check(s.Body.Z == top, "Standing player fell through support.");
        var x = s.Body.X; var y = s.Body.Y;
        s.Tick(new HereticCommand { Forward = 25 });
        Check(s.Body.X != x || s.Body.Y != y, "Standing player cannot move.");
        // Moving away removes support immediately; gravity then resumes.
        movement.UnsetThingPosition(obstacle);
        obstacle.X += Fixed.FromInt(128); movement.SetThingPosition(obstacle);
        s.Body.MomX = s.Body.MomY = Fixed.Zero;
        s.Tick(default); s.Tick(default);
        Check(!s.StandingOnActor && s.Body.Z < top, "Leaving support did not resume gravity.");
        // Restore the obstacle overhead and hit its underside at high speed.
        movement.UnsetThingPosition(obstacle);
        obstacle.X = s.Body.X; obstacle.Y = s.Body.Y; obstacle.Z = floor + Fixed.FromInt(72);
        movement.SetThingPosition(obstacle);
        s.Body.Z = floor; s.Body.MomZ = Fixed.Zero;
        Check(movement.CheckPosition(s.Body, s.Body.X, s.Body.Y), "Under-actor movement blocked.");
        s.GrantFlight(10); s.State.Flying = true; s.State.FlyHeight = 40;
        s.Tick(default);
        Check(s.Body.Z + s.Body.Height == obstacle.Z && s.Body.MomZ == Fixed.Zero, "Flight tunneled through underside.");
        Check(s.Body.FloorZ == floor, "Actor support corrupted map floor height.");
        CheckTargeting(content);
        CheckMovingSupport(content);
        CheckDeath(content);
        Console.WriteLine("PASS Heretic actor height collision: side blocking, above/below clearance, swept landing, support movement, gravity and underside collision");
    }
    static void CheckTargeting(GameContent content)
    {
        var s = new HereticWorldSession(content);
        var obstacle = s.Actors.First(a => (a.Body.Flags & MobjFlags.Solid) != 0).Body;
        foreach (var actor in s.Actors) actor.Body.Flags &= ~MobjFlags.Solid;
        var movement = s.World.ThingMovement;
        movement.UnsetThingPosition(obstacle);
        obstacle.X = s.Body.X + Fixed.FromInt(24); obstacle.Y = s.Body.Y;
        obstacle.Z = s.Body.Z; obstacle.Height = Fixed.FromInt(64); obstacle.Radius = Fixed.FromInt(4);
        obstacle.Flags |= MobjFlags.Solid; movement.SetThingPosition(obstacle);
        var hit = s.TraceAim(Angle.Ang0, Fixed.FromInt(48), Fixed.Zero);
        Check(hit?.Actor == obstacle && hit.Value.Distance > Fixed.Zero, "Aim did not find the first actor.");
        var health = obstacle.Health;
        obstacle.Height = Fixed.FromInt(8);
        Check(s.TraceAim(Angle.Ang0, Fixed.FromInt(48), Fixed.Zero)?.Actor != obstacle, "Aim hit actor below the ray.");
        Check(obstacle.Health == health, "Targeting applied damage.");
        obstacle.Flags &= ~MobjFlags.Solid;
        var wallHits = 0;
        for (var angle = 0; angle < 360; angle += 45)
            if (s.TraceAim(Angle.FromDegree(angle), Fixed.FromInt(2048), Fixed.Zero)?.Line != null) wallHits++;
        Check(wallHits > 0, "Aim passed through all enclosing walls.");
        try { s.TraceAim(Angle.Ang0, Fixed.Zero, Fixed.Zero); throw new Exception("Invalid aim range accepted."); }
        catch (ArgumentOutOfRangeException) { }
    }

    static void CheckMovingSupport(GameContent content)
    {
        var s = new HereticWorldSession(content);
        var obstacle = s.Actors.First(a => (a.Body.Flags & MobjFlags.Solid) != 0).Body;
        foreach (var actor in s.Actors) actor.Body.Flags &= ~MobjFlags.Solid;
        var movement = s.World.ThingMovement;
        movement.UnsetThingPosition(obstacle);
        obstacle.X = s.Body.X; obstacle.Y = s.Body.Y;
        obstacle.Flags |= MobjFlags.Solid;
        obstacle.Flags &= ~MobjFlags.SpawnCeiling;
        obstacle.Height = Fixed.FromInt(16);
        movement.SetThingPosition(obstacle);
        var sector = obstacle.Subsector.Sector;
        sector.Special = 0;
        var floor = sector.FloorHeight;
        sector.CeilingHeight = floor + Fixed.FromInt(80);
        obstacle.FloorZ = obstacle.Z = floor;
        obstacle.CeilingZ = sector.CeilingHeight;
        s.Body.FloorZ = floor; s.Body.CeilingZ = sector.CeilingHeight;
        s.Body.Z = floor + obstacle.Height;
        var action = s.World.SectorAction;
        action.MovePlane(sector, Fixed.One, floor + Fixed.FromInt(4), false, 0, 1);
        Check(s.Body.Z == floor + Fixed.FromInt(17) && s.StandingOnActor, "Lift left rider behind on rising scenery.");
        action.MovePlane(sector, Fixed.One, floor, false, 0, -1);
        Check(s.Body.Z == floor + Fixed.FromInt(16) && s.StandingOnActor, "Lowering scenery lost rider.");
        var result = action.MovePlane(sector, Fixed.FromInt(16), floor + Fixed.FromInt(32), false, 0, 1);
        Check(result == SectorActionResult.Crushed && sector.FloorHeight == floor && obstacle.Z == floor && s.Body.Z == floor + Fixed.FromInt(16), "Blocked rider move failed to roll back floor/actor/player.");
        Check(s.State.Health == 100, "Non-crushing lift damaged rider.");
        result = action.MovePlane(sector, Fixed.FromInt(16), floor + Fixed.FromInt(32), true, 0, 1);
        Check(result == SectorActionResult.Crushed && s.State.Health == 90 && s.Body.Health == 90, "Rider crushing did not apply one Heretic damage event.");
        Check(s.StandingOnActor, "Crushing lost support needed for subsequent ticks.");
        Console.WriteLine("PASS Heretic sector riders: rising/lowering scenery, headroom rollback and crushing damage");
    }

    static void CheckDeath(GameContent content)
    {
        var s = new HereticWorldSession(content);
        s.GrantFlight(100);
        s.Tick(new HereticCommand { Fly = 5 });
        var angle = s.Body.Angle;
        s.DamageEnvironment(100);
        Check(s.State.Health == 0 && s.Body.Health == 0 && !s.State.Flying && s.State.FlightTics == 0, "Lethal damage failed to clear flight/health.");
        var height = s.Body.Height;
        s.DamageEnvironment(100);
        Check(s.Body.Height == height && height == Fixed.FromInt(14), "Death initialization repeated.");
        Check((s.Body.Flags & (MobjFlags.Solid | MobjFlags.Shootable)) == 0, "Dead player remains solid/shootable.");
        var time = s.World.LevelTime;
        for (var i = 0; i < 50; i++) s.Tick(new HereticCommand { Forward = 50, Turn = 640, Fly = 5, Use = true });
        Check(s.World.LevelTime > time && s.Camera.ViewHeight == Fixed.FromInt(6), "Death froze the world or camera did not settle.");
        Check(s.Body.Angle == angle && s.State.Health == 0, "Dead player accepted live controls.");
        new HereticMapPreview(content, s).Render(new byte[320 * 200 * 4], s.World.LevelTime);
        Console.WriteLine("PASS Heretic environmental death: one-time corpse physics, flight cancellation, ignored input, continued world ticks and lowered camera");
    }

}
