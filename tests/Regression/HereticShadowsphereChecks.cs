// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticShadowsphereChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var s = new HereticWorldSession(content); s.StartClinkTest();
        for (var i = 0; i < 20; i++) s.Tick(default);
        var startingHealth = s.State.Health;
        Check(!s.UseArtifact(HereticArtifact.Shadowsphere), "Unowned Shadowsphere activated.");
        s.GiveArtifact(HereticArtifact.Shadowsphere); s.GiveArtifact(HereticArtifact.Shadowsphere);
        var view = new HereticMapPreview(content, s);
        var before = new byte[320 * 200 * 4]; var after = new byte[before.Length];
        view.Render(before, 0);
        Check(s.UseArtifact(HereticArtifact.Shadowsphere) && s.State.InvisibilityTics == 2100 && s.State.Shadowspheres == 1 && (s.Body.Flags & MobjFlags.Shadow) != 0, "Shadowsphere activation failed.");
        view.Render(after, 0); Check(!before.SequenceEqual(after), "Ghost weapon did not render translucently.");
        Check(!s.UseArtifact(HereticArtifact.Shadowsphere), "Early refresh consumed Shadowsphere.");
        s.DamageEnvironment(10); Check(s.State.Health == startingHealth - 10, "Ghost status incorrectly prevented ordinary damage.");
        s.State.InvisibilityTics = 128;
        Check(s.UseArtifact(HereticArtifact.Shadowsphere) && s.State.InvisibilityTics == 2100 && s.State.Shadowspheres == 0, "Refresh boundary failed.");
        // Isolate timer from the opt-in enemy encounter.
        var timer = new HereticWorldSession(content); timer.GiveArtifact(HereticArtifact.Shadowsphere); timer.UseArtifact(HereticArtifact.Shadowsphere);
        for (var i = 0; i < 2099; i++) timer.Tick(default);
        Check(timer.State.InvisibilityTics == 1 && (timer.Body.Flags & MobjFlags.Shadow) != 0, "Ghost expired early.");
        timer.Tick(default); Check(timer.State.InvisibilityTics == 0 && (timer.Body.Flags & MobjFlags.Shadow) == 0, "Ghost expiration failed.");
        s.State.InvisibilityTics = 7; view.Render(before, 0);
        s.State.InvisibilityTics = 8; view.Render(after, 0);
        Check(!before.SequenceEqual(after), "Final ghost blink did not change weapon rendering.");
        s.DamageEnvironment(1000);
        Check(s.State.InvisibilityTics == 0 && (s.Body.Flags & MobjFlags.Shadow) == 0 && !s.UseArtifact(HereticArtifact.Shadowsphere), "Death retained ghost status.");
        var pickupSession = new HereticWorldSession(content);
        var thing = pickupSession.World.Map.Things.First(t => t.Type == 10 && ((int)t.Flags & 2) != 0 && ((int)t.Flags & 16) == 0);
        thing.Type = 75; pickupSession.StartClinkTest();
        var pickup = pickupSession.Actors.First(a => a.Type == HereticActorType.MT_ARTIINVISIBILITY);
        for (var i = 0; i < 16; i++) pickupSession.GiveArtifact(HereticArtifact.Shadowsphere);
        Check(!pickupSession.GiveArtifact(HereticArtifact.Shadowsphere), "Shadowsphere inventory exceeded cap.");
        pickupSession.World.ThingMovement.UnsetThingPosition(pickupSession.Body);
        pickupSession.Body.X = pickup.Body.X; pickupSession.Body.Y = pickup.Body.Y; pickupSession.Body.Z = pickup.Body.Z;
        pickupSession.World.ThingMovement.SetThingPosition(pickupSession.Body); pickupSession.Body.FloorZ = pickup.Body.FloorZ; pickupSession.Body.CeilingZ = pickup.Body.CeilingZ;
        pickupSession.Tick(default); Check(pickupSession.Actors.Contains(pickup), "Full Shadowsphere inventory consumed pickup.");
        pickupSession.UseArtifact(HereticArtifact.Shadowsphere); pickupSession.Tick(default);
        Check(pickupSession.State.Shadowspheres == 16 && !pickupSession.Actors.Contains(pickup) && pickupSession.ImpactEffects.Contains(pickup), "Shadowsphere pickup did not store/animate.");
        Console.WriteLine("PASS Shadowsphere: pickup/cap, activation/refresh, translucent weapon/blink, duration, damage and death cleanup");
    }
}

