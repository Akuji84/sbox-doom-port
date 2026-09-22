// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticRingChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var s = new HereticWorldSession(content);
        Check(!s.UseArtifact(HereticArtifact.RingOfInvincibility), "Unowned Ring activated.");
        var view = new HereticMapPreview(content, s); var plain = new byte[320 * 200 * 4]; var protectedFrame = new byte[plain.Length];
        view.Render(plain, 0);
        s.GiveArtifact(HereticArtifact.RingOfInvincibility); s.GiveArtifact(HereticArtifact.RingOfInvincibility);
        Check(s.UseArtifact(HereticArtifact.RingOfInvincibility) && s.State.InvulnerabilityTics == 1050 && s.State.RingsOfInvincibility == 1,
            "Ring duration/consumption differs.");
        view.Render(protectedFrame, 0);
        Check(!plain.SequenceEqual(protectedFrame) && s.Camera.FixedColorMap == ColorMap.Inverse, "Ring inverse view did not render.");
        s.GiveArmor(1); s.GiveArtifact(HereticArtifact.QuartzFlask);
        s.DamageEnvironment(999);
        Check(s.State.Health == 100 && s.Body.Health == 100 && s.State.ArmorPoints == 100 && s.State.QuartzFlasks == 1 && s.State.DamageFlash == 0,
            "Invulnerability allowed ordinary damage or consumed armor/healing.");
        Check(!s.UseArtifact(HereticArtifact.RingOfInvincibility) && s.State.RingsOfInvincibility == 1, "Ring refresh consumed early.");
        s.State.InvulnerabilityTics = 128;
        Check(s.UseArtifact(HereticArtifact.RingOfInvincibility) && s.State.InvulnerabilityTics == 1050 && s.State.RingsOfInvincibility == 0, "Ring refresh threshold differs.");
        for (var i = 0; i < 1049; i++) s.Tick(default);
        Check(s.State.InvulnerabilityTics == 1, "Ring expired early.");
        s.Tick(default); Check(s.State.InvulnerabilityTics == 0 && s.Camera.FixedColorMap == 0, "Ring failed expiration/view restoration.");
        s.DamageEnvironment(20); Check(s.State.Health == 90, "Expired Ring still blocks damage.");
        var blink = new HereticWorldSession(content); blink.State.InvulnerabilityTics = 9;
        blink.Tick(default); Check(blink.Camera.FixedColorMap == ColorMap.Inverse, "Ring blink on phase differs.");
        blink.Tick(default); Check(blink.Camera.FixedColorMap == 0 && blink.State.InvulnerabilityTics > 0, "Ring blink off phase differs.");
        blink.DamageEnvironment(50); Check(blink.State.Health == 100, "Blink off removed damage protection.");
        foreach (var skill in new[] { GameSkill.Medium, GameSkill.Baby })
        {
            var bypass = new HereticWorldSession(content, 1, 1, skill);
            bypass.GiveArtifact(HereticArtifact.RingOfInvincibility); bypass.UseArtifact(HereticArtifact.RingOfInvincibility);
            bypass.DamageEnvironment(skill == GameSkill.Baby ? 1998 : 999);
            Check(bypass.State.Health == 100, "Bypass threshold precedes difficulty halving.");
            bypass.DamageEnvironment(skill == GameSkill.Baby ? 2000 : 1000);
            Check(bypass.State.Health == 0 && bypass.State.InvulnerabilityTics == 0 && bypass.Camera.FixedColorMap == 0,
                "High-damage bypass or death power cleanup failed.");
        }
        var pickupSession = new HereticWorldSession(content);
        var thing = pickupSession.World.Map.Things.First(t => t.Type == 10 && ((int)t.Flags & 2) != 0 && ((int)t.Flags & 16) == 0);
        thing.Type = 84; pickupSession.StartClinkTest();
        var pickup = pickupSession.Actors.First(a => a.Type == HereticActorType.MT_ARTIINVULNERABILITY);
        for (var i = 0; i < 16; i++) pickupSession.GiveArtifact(HereticArtifact.RingOfInvincibility);
        Check(!pickupSession.GiveArtifact(HereticArtifact.RingOfInvincibility), "Ring inventory cap failed.");
        pickupSession.World.ThingMovement.UnsetThingPosition(pickupSession.Body);
        pickupSession.Body.X = pickup.Body.X; pickupSession.Body.Y = pickup.Body.Y; pickupSession.Body.Z = pickup.Body.Z;
        pickupSession.World.ThingMovement.SetThingPosition(pickupSession.Body); pickupSession.Body.FloorZ = pickup.Body.FloorZ; pickupSession.Body.CeilingZ = pickup.Body.CeilingZ;
        pickupSession.Tick(default); Check(pickupSession.Actors.Contains(pickup), "Full Ring inventory consumed pickup.");
        pickupSession.UseArtifact(HereticArtifact.RingOfInvincibility); pickupSession.Tick(default);
        Check(pickupSession.State.RingsOfInvincibility == 16 && !pickupSession.Actors.Contains(pickup) && pickupSession.ImpactEffects.Contains(pickup), "Ring pickup/storage/animation failed.");
        Console.WriteLine("PASS Ring: inventory/pickup, timed protection, inverse rendering/blink, refresh, armor ordering, damage bypass and death cleanup");
    }
}
