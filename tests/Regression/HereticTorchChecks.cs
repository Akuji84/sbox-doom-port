// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticTorchChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var s = new HereticWorldSession(content); var baseline = new HereticWorldSession(content);
        Check(!s.UseArtifact(HereticArtifact.Torch), "Unowned Torch activated.");
        s.GiveArtifact(HereticArtifact.Torch); s.GiveArtifact(HereticArtifact.Torch);
        var view = new HereticMapPreview(content, s); var before = new byte[320 * 200 * 4]; var after = new byte[before.Length];
        view.Render(before, 0); s.UseArtifact(HereticArtifact.Torch); view.Render(after, 0);
        Check(s.State.TorchTics == 4200 && s.State.Torches == 1 && !before.SequenceEqual(after), "Torch duration/storage or visible lighting failed.");
        Check(!s.UseArtifact(HereticArtifact.Torch), "Active Torch consumed a replacement early.");
        var maps = new HashSet<int>();
        for (var i = 0; i < 128; i++)
        {
            s.Tick(default); baseline.Tick(default); maps.Add(s.Camera.FixedColorMap);
            Check(s.Camera.FixedColorMap >= 1 && s.Camera.FixedColorMap <= 7 && s.World.Random.Index == baseline.World.Random.Index,
                "Torch lighting escaped bounds or changed gameplay randomness.");
        }
        Check(maps.Count > 1, "Torch did not flicker.");
        var map = s.Camera.FixedColorMap; var timer = s.State.TorchTics; var rng = s.World.Random.Index;
        for (var i = 0; i < 20; i++) view.Render(after, 0);
        Check(s.Camera.FixedColorMap == map && s.State.TorchTics == timer && s.World.Random.Index == rng, "Rendering advanced Torch state.");
        s.GiveArtifact(HereticArtifact.RingOfInvincibility); s.UseArtifact(HereticArtifact.RingOfInvincibility);
        s.Tick(default); Check(s.Camera.FixedColorMap == ColorMap.Inverse, "Torch overrode active Ring.");
        s.State.InvulnerabilityTics = 8; s.Tick(default);
        Check(s.Camera.FixedColorMap == 0 && s.State.TorchTics > 0, "Torch overrode Ring's dark blink.");
        s.State.InvulnerabilityTics = 1; s.Tick(default);
        Check(s.Camera.FixedColorMap >= 1 && s.Camera.FixedColorMap <= 7, "Torch did not resume after Ring.");
        s.State.TorchTics = 128;
        Check(s.UseArtifact(HereticArtifact.Torch) && s.State.TorchTics == 4200 && s.State.Torches == 0, "Torch refresh threshold differs.");
        for (var i = 0; i < 4199; i++) s.Tick(default);
        Check(s.State.TorchTics == 1, "Torch expired early.");
        s.Tick(default); Check(s.State.TorchTics == 0 && s.Camera.FixedColorMap == 0, "Torch failed expiration.");
        s.State.TorchTics = 9; s.Tick(default); Check(s.Camera.FixedColorMap == 0, "Torch final blink dark phase differs.");
        s.Tick(default); Check(s.Camera.FixedColorMap == 1, "Torch final blink light phase differs.");
        s.DamageEnvironment(100); Check(s.State.TorchTics == 0 && s.Camera.FixedColorMap == 0, "Death retained Torch light.");
        var pickupSession = new HereticWorldSession(content);
        var thing = pickupSession.World.Map.Things.First(t => t.Type == 10 && ((int)t.Flags & 2) != 0 && ((int)t.Flags & 16) == 0);
        thing.Type = 33; pickupSession.StartClinkTest();
        var pickup = pickupSession.Actors.First(a => a.Type == HereticActorType.MT_MISC4);
        for (var i = 0; i < 16; i++) pickupSession.GiveArtifact(HereticArtifact.Torch);
        Check(!pickupSession.GiveArtifact(HereticArtifact.Torch), "Torch inventory exceeded cap.");
        pickupSession.World.ThingMovement.UnsetThingPosition(pickupSession.Body);
        pickupSession.Body.X = pickup.Body.X; pickupSession.Body.Y = pickup.Body.Y; pickupSession.Body.Z = pickup.Body.Z;
        pickupSession.World.ThingMovement.SetThingPosition(pickupSession.Body); pickupSession.Body.FloorZ = pickup.Body.FloorZ; pickupSession.Body.CeilingZ = pickup.Body.CeilingZ;
        pickupSession.Tick(default); Check(pickupSession.Actors.Contains(pickup), "Full Torch inventory consumed pickup.");
        pickupSession.UseArtifact(HereticArtifact.Torch); pickupSession.Tick(default);
        Check(pickupSession.State.Torches == 16 && !pickupSession.Actors.Contains(pickup) && pickupSession.ImpactEffects.Contains(pickup), "Torch pickup did not store/animate.");
        Console.WriteLine("PASS Torch: inventory, rendered flicker, gameplay RNG isolation, render independence, Ring priority, duration/refresh/blink and death");
    }
}
