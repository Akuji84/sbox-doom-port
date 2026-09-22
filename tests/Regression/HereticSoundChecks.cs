// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticSoundChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var count = 0;
        foreach (HereticSoundId id in Enum.GetValues(typeof(HereticSoundId)))
        {
            var name = HereticSoundData.LumpName(id);
            if (name == null) continue;
            var lump = content.Wad.GetLumpNumber(name);
            Check(lump >= 0, "Missing encounter sound " + name);
            var sound = HereticSoundData.Decode(content.Wad.ReadLump(lump));
            Check(sound.SampleRate > 0 && sound.Samples.Length > 0 && sound.Samples.Any(x => x != 0), "Empty decoded sound " + name);
            count++;
        }
        Check(count == 9, "Encounter sound mapping incomplete.");
        var synthetic = new byte[43]; synthetic[0] = 3; synthetic[2] = 0x11; synthetic[3] = 0x2b; synthetic[4] = 35;
        synthetic[24] = 0; synthetic[25] = 128; synthetic[26] = 255;
        var decoded = HereticSoundData.Decode(synthetic);
        Check(decoded.SampleRate == 11025 && decoded.Samples.SequenceEqual(new short[] { -32768, 0, 32512 }), "PCM conversion or DMX guards differ.");
        foreach (var bad in new[] { Array.Empty<byte>(), new byte[8], synthetic.Take(42).ToArray() })
        {
            var rejected = false;
            try { HereticSoundData.Decode(bad); } catch (ArgumentException) { rejected = true; }
            Check(rejected, "Malformed DMX was accepted.");
        }
        var s = new HereticWorldSession(content);
        var events = new List<HereticSoundId>();
        s.SoundRequested += (id, source) => { Check(source != null, "Sound missing source."); events.Add(id); };
        var enemy = s.StartClinkTest(); Check(enemy != null, "Audio encounter fixture failed.");
        for (var i = 0; i < 30; i++) s.Tick(new HereticCommand { TestAttack = true });
        Check(events.Contains(HereticSoundId.sfx_gldhit) && events.Contains(HereticSoundId.sfx_clksit), "Weapon or enemy sound did not reach session.");
        var shots = s.GoldWand.ShotsFired;
        Check(events.Count(x => x == HereticSoundId.sfx_gldhit) == shots, "Wand playback event count differs from shots.");
        s.SpawnWeaponImpact(new HereticTraceHit(null, null, Fixed.FromInt(32), s.Camera.ViewZ), s.Body.Angle, Fixed.Zero, HereticWeapon.wp_staff);
        Check(events.Contains(HereticSoundId.sfx_stfhit), "Staff impact sound not forwarded.");
        s.DamageTestEnemy(enemy.Body, 10000);
        for (var i = 0; i < 16; i++) s.Tick(default);
        Check(events.Contains(HereticSoundId.sfx_clkdth), "Enemy death sound not forwarded.");
        Console.WriteLine("PASS Heretic sound: nine licensed WAD samples, validated DMX decoding and weapon/impact/enemy events");
    }
}
