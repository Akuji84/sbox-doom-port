// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticTimeBombChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var s = new HereticWorldSession(content);
        Check(!s.UseArtifact(HereticArtifact.TimeBomb), "Unowned bomb placed.");
        s.GiveArtifact(HereticArtifact.TimeBomb); var sounds = 0;
        s.SoundRequested += (id, source) => { if (id == HereticSoundId.sfx_phohit) sounds++; };
        Check(s.UseArtifact(HereticArtifact.TimeBomb) && s.State.TimeBombs == 0, "Bomb placement did not consume inventory.");
        var bomb = s.ImpactEffects.Single(e => e.Type == HereticActorType.MT_FIREBOMB); var initialZ = bomb.Body.Z;
        Check(bomb.Body.X == s.Body.X + Fixed.FromInt(24) * Trig.Cos(s.Body.Angle) && bomb.Body.Y == s.Body.Y + Fixed.FromInt(24) * Trig.Sin(s.Body.Angle) &&
            bomb.Body.Target == s.Body && bomb.Body.Info == null && bomb.Body.State == null, "Bomb placement/ownership/isolation failed.");
        for (var i = 0; i < 39; i++) s.Tick(default);
        Check(s.State.Health == 100 && sounds == 0, "Bomb fired before fuse.");
        s.Tick(default); Check(sounds == 1 && s.State.Health == 100, "Bomb warning/explosion sound timing differs.");
        for (var i = 0; i < 5; i++) s.Tick(default);
        Check(s.State.Health == 100, "Bomb damage started before tick forty-six.");
        s.Tick(default);
        Check(s.State.Health == 0 && bomb.Body.Z == initialZ + Fixed.FromInt(32) && (bomb.Body.Flags & MobjFlags.Shadow) == 0,
            "Bomb explosion failed self damage, vertical shift or visibility change.");
        for (var i = 0; i < 30; i++) s.Tick(default);
        Check(bomb.Animation.Removed && !s.ImpactEffects.Contains(bomb), "Bomb remained linked after explosion/death.");
        var fight = new HereticWorldSession(content); var enemy = fight.StartClinkTest();
        Check(enemy != null, "Bomb encounter failed."); enemy.Body.ReactionTime = 10000;
        fight.Body.Angle = Geometry.PointToAngle(fight.Body.X, fight.Body.Y, enemy.Body.X, enemy.Body.Y);
        fight.GiveArtifact(HereticArtifact.TimeBomb); fight.UseArtifact(HereticArtifact.TimeBomb);
        var health = enemy.Body.Health;
        for (var i = 0; i < 46; i++) fight.Tick(default);
        Check(enemy.Body.Health < health, "Bomb failed damage against registered enemy.");
        var dead = new HereticWorldSession(content); dead.GiveArtifact(HereticArtifact.TimeBomb); dead.UseArtifact(HereticArtifact.TimeBomb);
        var armed = dead.ImpactEffects.Single(); dead.DamageEnvironment(100);
        for (var i = 0; i < 80; i++) dead.Tick(default);
        Check(armed.Animation.Removed && dead.ImpactEffects.Count == 0, "Armed bomb stopped ticking after owner death.");
        var pickupSession = new HereticWorldSession(content);
        var thing = pickupSession.World.Map.Things.First(t => t.Type == 10 && ((int)t.Flags & 2) != 0 && ((int)t.Flags & 16) == 0);
        thing.Type = 34; pickupSession.StartClinkTest();
        var pickup = pickupSession.Actors.First(a => a.Type == HereticActorType.MT_MISC5);
        for (var i = 0; i < 16; i++) pickupSession.GiveArtifact(HereticArtifact.TimeBomb);
        Check(!pickupSession.GiveArtifact(HereticArtifact.TimeBomb), "Bomb inventory exceeded cap.");
        pickupSession.World.ThingMovement.UnsetThingPosition(pickupSession.Body);
        pickupSession.Body.X = pickup.Body.X; pickupSession.Body.Y = pickup.Body.Y; pickupSession.Body.Z = pickup.Body.Z;
        pickupSession.World.ThingMovement.SetThingPosition(pickupSession.Body); pickupSession.Body.FloorZ = pickup.Body.FloorZ; pickupSession.Body.CeilingZ = pickup.Body.CeilingZ;
        pickupSession.Tick(default); Check(pickupSession.Actors.Contains(pickup), "Full bomb inventory consumed pickup.");
        pickupSession.UseArtifact(HereticArtifact.TimeBomb); pickupSession.Tick(default);
        Check(pickupSession.State.TimeBombs == 16 && !pickupSession.Actors.Contains(pickup), "Bomb map collection failed.");
        new HereticMapPreview(content, pickupSession).Render(new byte[320 * 200 * 4], 0);
        Console.WriteLine("PASS Time Bomb: inventory/pickup, placement, fuse/sound, self/enemy blast damage, owner death and cleanup");
    }
}
