// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticHealingArtifactChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static void Touch(HereticWorldSession s, Mobj item)
    {
        s.World.ThingMovement.UnsetThingPosition(s.Body); s.Body.X = item.X; s.Body.Y = item.Y; s.Body.Z = item.Z;
        s.World.ThingMovement.SetThingPosition(s.Body); s.Body.FloorZ = item.FloorZ; s.Body.CeilingZ = item.CeilingZ;
        s.Tick(default);
    }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        foreach (var type in new[] { HereticActorType.MT_MISC3, HereticActorType.MT_ARTISUPERHEAL })
        {
            var s = new HereticWorldSession(content);
            Check(!s.Actors.Any(a => a.Type == type), "Healing artifacts enabled outside combat preview.");
            var thing = s.World.Map.Things.First(t => t.Type == 10 && ((int)t.Flags & 2) != 0 && ((int)t.Flags & 16) == 0);
            thing.Type = HereticDefinitions.Actors[(int)type].MapNumber; s.StartClinkTest();
            var pickup = s.Actors.First(a => a.Type == type);
            var artifact = type == HereticActorType.MT_MISC3 ? HereticHealingArtifact.QuartzFlask : HereticHealingArtifact.MysticUrn;
            for (var i = 0; i < 16; i++) Check(s.GiveHealingArtifact(artifact), "Inventory filled too early.");
            Check(!s.GiveHealingArtifact(artifact), "Healing artifact exceeded sixteen.");
            Touch(s, pickup.Body); Check(s.Actors.Contains(pickup), "Full inventory consumed map artifact.");
            Check(!s.UseHealingArtifact(artifact), "Artifact consumed at full health.");
            s.DamageEnvironment(30);
            Check(s.UseHealingArtifact(artifact) && s.State.Health == (artifact == HereticHealingArtifact.QuartzFlask ? 95 : 100)
                && s.State.Health == s.Body.Health, "Healing artifact effect/health sync differs.");
            var picked = false; s.SoundRequested += (id, source) => { if (id == HereticSoundId.sfx_artiup) picked = true; };
            Touch(s, pickup.Body);
            Check(!s.Actors.Contains(pickup) && s.ImpactEffects.Contains(pickup) && !pickup.Animation.Removed && picked,
                "Artifact pickup did not start non-respawning animation and sound.");
            Check((artifact == HereticHealingArtifact.QuartzFlask ? s.State.QuartzFlasks : s.State.MysticUrns) == 16, "Artifact stored count differs.");
            for (var i = 0; i < 40; i++) s.Tick(default);
            Check(pickup.Animation.Removed && !s.ImpactEffects.Contains(pickup), "Artifact pickup animation leaked.");
        }
        var commands = new List<HereticCommand>(); var stepper = new HereticFrameStepper();
        stepper.Advance(0.001, new HereticCommand { UseArtifact = HereticHealingArtifact.QuartzFlask }, commands.Add);
        stepper.Advance(0.1, default, commands.Add);
        Check(commands.Count(c => c.UseArtifact != null) == 1, "Artifact short tap was lost/repeated during catch-up ticks.");
        var heal = new HereticWorldSession(content); heal.GiveHealingArtifact(HereticHealingArtifact.QuartzFlask); heal.GiveHealingArtifact(HereticHealingArtifact.QuartzFlask);
        heal.DamageEnvironment(80);
        foreach (var command in commands) heal.Tick(command);
        Check(heal.State.Health == 45 && heal.State.QuartzFlasks == 1, "One-shot artifact command healed more than once.");
        heal.GiveHealingArtifact(HereticHealingArtifact.MysticUrn);
        heal.Tick(new HereticCommand { UseArtifact = HereticHealingArtifact.MysticUrn });
        Check(heal.State.Health == 100 && heal.State.MysticUrns == 0, "Urn did not heal/cap/consume.");
        heal.DamageEnvironment(100);
        Check(!heal.UseHealingArtifact(HereticHealingArtifact.QuartzFlask) && heal.State.QuartzFlasks == 1 && !heal.GiveHealingArtifact(HereticHealingArtifact.MysticUrn), "Healing inventory resurrected or granted to dead player.");
        Console.WriteLine("PASS healing inventory: Flask/Urn pickups, sixteen-item caps, pickup animation, health/use rules, one-shot input and dead-player gating");
    }
}
