// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticChaosChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static void Place(HereticWorldSession s, Mobj item)
    {
        s.World.ThingMovement.UnsetThingPosition(s.Body);
        s.Body.X = item.X; s.Body.Y = item.Y; s.Body.Z = item.Z;
        s.World.ThingMovement.SetThingPosition(s.Body); s.Body.FloorZ = item.FloorZ; s.Body.CeilingZ = item.CeilingZ;
    }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var s = new HereticWorldSession(content); var start = s.World.Map.Things.First(t => t.Type == 1);
        Check(!s.UseArtifact(HereticArtifact.ChaosDevice), "Unowned Chaos Device activated.");
        s.GiveArtifact(HereticArtifact.ChaosDevice);
        start.Type = 0;
        Check(!s.UseArtifact(HereticArtifact.ChaosDevice) && s.State.ChaosDevices == 1, "Missing start consumed Chaos Device."); start.Type = 1;
        var sector = Geometry.PointInSubsector(start.X, start.Y, s.World.Map).Sector; var ceiling = sector.CeilingHeight;
        sector.CeilingHeight = sector.FloorHeight + s.Body.Height - Fixed.One;
        var rng = s.World.Random.Index;
        Check(!s.UseArtifact(HereticArtifact.ChaosDevice) && s.State.ChaosDevices == 1 && s.ImpactEffects.Count == 0 && s.World.Random.Index == rng,
            "Cramped destination consumed inventory or emitted effects."); sector.CeilingHeight = ceiling;
        s.World.ThingMovement.TeleportMove(s.Body, start.X + Fixed.FromInt(8), start.Y);
        s.Body.MomX = Fixed.One; s.Body.MomY = Fixed.One; s.Body.MomZ = Fixed.One; s.State.LookDirection = 30;
        var sounds = 0; s.SoundRequested += (id, source) => { if (id == HereticSoundId.sfx_telept) sounds++; };
        Check(s.UseArtifact(HereticArtifact.ChaosDevice), "Chaos Device failed valid start.");
        Check(s.State.ChaosDevices == 0 && s.Body.X == start.X && s.Body.Y == start.Y && s.Body.Angle == start.Angle && s.Body.Z == s.Body.FloorZ &&
            s.Body.MomX == Fixed.Zero && s.Body.MomY == Fixed.Zero && s.Body.MomZ == Fixed.Zero && s.Body.ReactionTime == 18 && s.State.LookDirection == 0,
            "Chaos Device player reset differs.");
        Check(sounds == 2 && s.ImpactEffects.Count == 2 && s.ImpactEffects.All(e => e.Type == HereticActorType.MT_TFOG), "Teleport fog/sounds missing.");
        new HereticMapPreview(content, s).Render(new byte[320 * 200 * 4], 0);
        for (var i = 0; i < 120; i++) s.Tick(default);
        Check(s.ImpactEffects.Count == 0, "Teleport fog failed cleanup.");
        s.GiveArtifact(HereticArtifact.ChaosDevice); s.GrantFlight(200); s.Body.Z = s.Body.FloorZ + Fixed.FromInt(24);
        s.State.LookDirection = 20;
        Check(s.UseArtifact(HereticArtifact.ChaosDevice) && s.Body.Z == s.Body.FloorZ + Fixed.FromInt(24) && s.State.LookDirection == 20, "Chaos Device lost flight height/pitch.");
        var stomp = new HereticWorldSession(content); var enemy = stomp.StartClinkTest();
        Check(enemy != null, "Teleport occupant fixture failed.");
        var enemyCeiling = enemy.Body.Subsector.Sector.CeilingHeight;
        enemy.Body.Subsector.Sector.CeilingHeight = enemy.Body.Subsector.Sector.FloorHeight + Fixed.One;
        Check(!stomp.TryTeleportPlayer(enemy.Body.X, enemy.Body.Y, enemy.Body.Angle) && enemy.Body.Health > 0, "Rejected teleport killed occupant.");
        enemy.Body.Subsector.Sector.CeilingHeight = enemyCeiling;
        Check(stomp.TryTeleportPlayer(enemy.Body.X, enemy.Body.Y, enemy.Body.Angle) && enemy.Body.Health <= 0, "Teleport did not telefrag registered enemy.");
        var pickupSession = new HereticWorldSession(content);
        var thing = pickupSession.World.Map.Things.First(t => t.Type == 10 && ((int)t.Flags & 2) != 0 && ((int)t.Flags & 16) == 0);
        thing.Type = 36; pickupSession.StartClinkTest();
        var pickup = pickupSession.Actors.First(a => a.Type == HereticActorType.MT_ARTITELEPORT);
        for (var i = 0; i < 16; i++) pickupSession.GiveArtifact(HereticArtifact.ChaosDevice);
        Check(!pickupSession.GiveArtifact(HereticArtifact.ChaosDevice), "Chaos inventory cap failed.");
        Place(pickupSession, pickup.Body); pickupSession.Tick(default); Check(pickupSession.Actors.Contains(pickup), "Full Chaos inventory consumed pickup.");
        pickupSession.UseArtifact(HereticArtifact.ChaosDevice); Place(pickupSession, pickup.Body); pickupSession.Tick(default);
        Check(pickupSession.State.ChaosDevices == 16 && !pickupSession.Actors.Contains(pickup), "Chaos map collection failed.");
        Console.WriteLine("PASS Chaos Device: map pickup/cap, start teleport, failure retention, fog/audio, flight, telefrag and cleanup");
    }
}
