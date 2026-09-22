// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticOvumChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var s = new HereticWorldSession(content); var enemy = s.StartClinkTest();
        Check(!s.UseArtifact(HereticArtifact.MorphOvum), "Unowned Ovum fired.");
        s.GiveArtifact(HereticArtifact.MorphOvum);
        s.Body.Angle = Geometry.PointToAngle(s.Body.X, s.Body.Y, enemy.Body.X, enemy.Body.Y);
        var audio = 0; s.SoundRequested += (id, source) => { if (id == HereticSoundId.sfx_artiuse) audio++; };
        var old = enemy.Body;
        s.Tick(new HereticCommand { UseArtifact = HereticArtifact.MorphOvum });
        Check(s.State.MorphOvums == 0 && s.Projectiles.Count == 5 && audio == 1, "Ovum did not consume one item and create a five-egg volley.");
        for (var i = 0; i < 30 && !enemy.IsChicken; i++) s.Tick(default);
        Check(enemy.IsChicken && enemy.Body != old && s.TestKills == 0, "Real egg movement did not transform Clink.");
        Check(!s.UseArtifact(HereticArtifact.MorphOvum), "Empty inventory fired again.");
        Check(enemy.UpdateChicken(enemy.ChickenTics) && !enemy.IsChicken, "Ovum target failed to restore.");
        s.DamageEnvironment(10000);
        Check(!s.GiveArtifact(HereticArtifact.MorphOvum) && !s.UseArtifact(HereticArtifact.MorphOvum), "Dead player could acquire/use Ovum.");
        var navigation = new HereticWorldSession(content); navigation.GiveArtifact(HereticArtifact.MorphOvum);
        Check(!navigation.UseArtifact(HereticArtifact.MorphOvum) && navigation.State.MorphOvums == 1 && navigation.Projectiles.Count == 0, "Navigation-only session activated combat artifact.");

        var pickupSession = new HereticWorldSession(content);
        var thing = pickupSession.World.Map.Things.First(t => t.Type == 10 && ((int)t.Flags & 2) != 0 && ((int)t.Flags & 16) == 0);
        thing.Type = 30; pickupSession.StartClinkTest();
        var pickup = pickupSession.Actors.First(a => a.Type == HereticActorType.MT_ARTIEGG);
        for (var i = 0; i < 16; i++) pickupSession.GiveArtifact(HereticArtifact.MorphOvum);
        Check(!pickupSession.GiveArtifact(HereticArtifact.MorphOvum), "Ovum inventory exceeded cap.");
        pickupSession.World.ThingMovement.UnsetThingPosition(pickupSession.Body);
        pickupSession.Body.X = pickup.Body.X; pickupSession.Body.Y = pickup.Body.Y; pickupSession.Body.Z = pickup.Body.Z;
        pickupSession.World.ThingMovement.SetThingPosition(pickupSession.Body); pickupSession.Body.FloorZ = pickup.Body.FloorZ; pickupSession.Body.CeilingZ = pickup.Body.CeilingZ;
        pickupSession.Tick(default); Check(pickupSession.Actors.Contains(pickup), "Full inventory consumed Ovum pickup.");
        pickupSession.UseArtifact(HereticArtifact.MorphOvum); pickupSession.Tick(default);
        Check(pickupSession.State.MorphOvums == 16 && !pickupSession.Actors.Contains(pickup) && pickupSession.ImpactEffects.Contains(pickup), "Ovum pickup did not replenish/animate.");
        Console.WriteLine("PASS Morph Ovum: real pickup/use/volley/morph/restore, full-cap retention, audio, empty/dead rejection and navigation gate");
    }
}
