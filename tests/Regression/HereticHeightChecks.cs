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

}
