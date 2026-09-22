// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticAutoHealChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        HereticWorldSession Make(GameSkill skill, int flasks, int urns)
        {
            var s = new HereticWorldSession(content, 1, 1, skill);
            for (var i = 0; i < flasks; i++) s.GiveArtifact(HereticArtifact.QuartzFlask);
            for (var i = 0; i < urns; i++) s.GiveArtifact(HereticArtifact.MysticUrn);
            return s;
        }
        var flask = Make(GameSkill.Baby, 3, 2);
        var sounds = 0; flask.SoundRequested += (id, source) => sounds++;
        flask.DamageEnvironment(200);
        Check(flask.State.Health == 25 && flask.Body.Health == 25 && flask.State.QuartzFlasks == 2 && flask.State.MysticUrns == 2,
            "Baby lethal hit did not prefer minimum flasks.");
        Check(sounds == 0 && flask.State.DamageFlash == 100 && (flask.Body.Flags & MobjFlags.Corpse) == 0,
            "Auto healing used manual-use sound, lost feedback, or left a corpse.");
        var urn = Make(GameSkill.Baby, 1, 2); urn.DamageEnvironment(300);
        Check(urn.State.Health == 50 && urn.State.QuartzFlasks == 1 && urn.State.MysticUrns == 1, "Urn-only sufficient healing consumed flasks.");
        var mixed = Make(GameSkill.Baby, 2, 1); mixed.DamageEnvironment(450);
        Check(mixed.State.Health == 25 && mixed.Body.Health == 25 && mixed.State.QuartzFlasks == 0 && mixed.State.MysticUrns == 0,
            "Mixed automatic healing overdraws inventory or fails survival.");
        var insufficient = Make(GameSkill.Baby, 1, 1); insufficient.DamageEnvironment(500);
        Check(insufficient.State.Health == 0 && insufficient.State.QuartzFlasks == 1 && insufficient.State.MysticUrns == 1,
            "Insufficient healing consumed inventory or prevented death.");
        var exact = Make(GameSkill.Baby, 1, 0); exact.DamageEnvironment(248);
        Check(exact.State.Health == 1 && exact.State.QuartzFlasks == 0, "Exact survival threshold differs.");
        var tooLittle = Make(GameSkill.Baby, 1, 0); tooLittle.DamageEnvironment(250);
        Check(tooLittle.State.Health == 0 && tooLittle.State.QuartzFlasks == 1, "Zero-health result was treated as survival.");
        var armor = Make(GameSkill.Baby, 2, 0); armor.GiveArmor(1); armor.DamageEnvironment(300);
        Check(armor.State.Health == 25 && armor.State.QuartzFlasks == 2 && armor.State.ArmorPoints == 25,
            "Auto-healing ran before Baby damage halving and armor.");
        foreach (var skill in new[] { GameSkill.Easy, GameSkill.Medium, GameSkill.Hard, GameSkill.Nightmare })
        {
            var s = Make(skill, 2, 2); s.DamageEnvironment(100);
            Check(s.State.Health == 0 && s.State.QuartzFlasks == 2 && s.State.MysticUrns == 2, "Single-player auto healing enabled outside Baby.");
        }
        var huge = Make(GameSkill.Baby, 16, 16); huge.DamageEnvironment(int.MaxValue);
        Check(huge.State.Health == 0 && huge.State.QuartzFlasks == 16 && huge.State.MysticUrns == 16, "Extreme damage overflowed healing arithmetic.");
        Console.WriteLine("PASS emergency healing: Baby/armor ordering, flask/urn priority, bounded mixed use, exact survival, insufficient inventory and other skills");
    }
}
