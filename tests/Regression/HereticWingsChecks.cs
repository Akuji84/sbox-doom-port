// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticWingsChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var s = new HereticWorldSession(content);
        Check(!s.UseArtifact(HereticArtifact.WingsOfWrath), "Unowned Wings activated.");
        s.GiveArtifact(HereticArtifact.WingsOfWrath); s.GiveArtifact(HereticArtifact.WingsOfWrath);
        var floor = s.Body.FloorZ;
        s.Tick(new HereticCommand { UseArtifact = HereticArtifact.WingsOfWrath });
        Check(s.State.WingsOfWrath == 1 && s.State.FlightTics == 2099 && s.State.Flying && s.Body.Z > floor && (s.Body.Flags & MobjFlags.NoGravity) != 0,
            "Wings did not consume, activate sixty-second flight and take off.");
        Check(!s.UseArtifact(HereticArtifact.WingsOfWrath) && s.State.WingsOfWrath == 1, "Active flight consumed replacement too early.");
        s.Tick(new HereticCommand { Land = true });
        Check(!s.State.Flying && s.State.FlightTics > 0 && (s.Body.Flags & MobjFlags.NoGravity) == 0, "Landing failed or removed remaining power.");
        s.Tick(new HereticCommand { Fly = 1 });
        Check(s.State.Flying && s.State.WingsOfWrath == 1, "Resuming existing flight consumed another Wings.");
        s.State.FlightTics = 128;
        Check(s.UseArtifact(HereticArtifact.WingsOfWrath) && s.State.FlightTics == 2100 && s.State.WingsOfWrath == 0, "Flight refresh threshold differs.");
        for (var i = 0; i < 2099; i++) s.Tick(default);
        Check(s.State.Flying && s.State.FlightTics == 1, "Flight expired early.");
        s.Tick(default);
        Check(!s.State.Flying && s.State.FlightTics == 0 && (s.Body.Flags & MobjFlags.NoGravity) == 0, "Flight failed to expire and restore gravity.");
        var auto = new HereticWorldSession(content); auto.GiveArtifact(HereticArtifact.WingsOfWrath);
        auto.Tick(new HereticCommand { Fly = 1 });
        Check(auto.State.Flying && auto.State.WingsOfWrath == 0, "Fly-up did not auto-use stored Wings.");
        auto.DamageEnvironment(100);
        Check(!auto.State.Flying && auto.State.FlightTics == 0 && !auto.UseArtifact(HereticArtifact.WingsOfWrath), "Death retained Wings power.");
        var pickupSession = new HereticWorldSession(content);
        Check(!pickupSession.Actors.Any(a => a.Type == HereticActorType.MT_ARTIFLY), "Wings enabled outside combat preview.");
        var thing = pickupSession.World.Map.Things.First(t => t.Type == 10 && ((int)t.Flags & 2) != 0 && ((int)t.Flags & 16) == 0);
        thing.Type = 83; pickupSession.StartClinkTest();
        var pickup = pickupSession.Actors.First(a => a.Type == HereticActorType.MT_ARTIFLY);
        for (var i = 0; i < 16; i++) pickupSession.GiveArtifact(HereticArtifact.WingsOfWrath);
        Check(!pickupSession.GiveArtifact(HereticArtifact.WingsOfWrath), "Wings exceeded inventory cap.");
        pickupSession.World.ThingMovement.UnsetThingPosition(pickupSession.Body);
        pickupSession.Body.X = pickup.Body.X; pickupSession.Body.Y = pickup.Body.Y; pickupSession.Body.Z = pickup.Body.Z;
        pickupSession.World.ThingMovement.SetThingPosition(pickupSession.Body);
        pickupSession.Body.FloorZ = pickup.Body.FloorZ; pickupSession.Body.CeilingZ = pickup.Body.CeilingZ;
        pickupSession.Tick(default); Check(pickupSession.Actors.Contains(pickup), "Full Wings inventory consumed pickup.");
        pickupSession.UseArtifact(HereticArtifact.WingsOfWrath);
        pickupSession.Tick(new HereticCommand { Land = true });
        Check(!pickupSession.Actors.Contains(pickup) && pickupSession.State.WingsOfWrath == 16 && pickupSession.ImpactEffects.Contains(pickup), "Wings pickup did not store/animate.");
        var stepper = new HereticFrameStepper(); var uses = 0;
        stepper.Advance(0.001, new HereticCommand { UseArtifact = HereticArtifact.WingsOfWrath }, c => uses += c.UseArtifact != null ? 1 : 0);
        stepper.Advance(0.1, default, c => uses += c.UseArtifact != null ? 1 : 0);
        Check(uses == 1, "Wings input was lost or repeated across catch-up ticks.");
        Console.WriteLine("PASS Wings: pickup/cap, takeoff, manual/automatic use, duration/refresh, landing/resume, gravity restoration and death");
    }
}
