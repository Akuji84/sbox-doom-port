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
        Check(count == 81, "Encounter sound mapping incomplete.");
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
        var curve = new byte[1600]; Array.Fill(curve, (byte)127); curve[150] = 63;
        var attenuation = new HereticSoundAttenuation(curve);
        curve[0] = 0; // Constructor must retain its validated copy.
        var listener = new Mobj(s.World); var source = new Mobj(s.World);
        Check(attenuation.Gain(null, listener) == 1, "Local sound gain or curve ownership differs.");
        source.X = source.Y = Fixed.FromInt(100);
        Check(Math.Abs(attenuation.Gain(source, listener) - 63f / 127f) < 0.00001f, "Approximate distance lookup differs.");
        source.Y = Fixed.Zero; source.X = Fixed.FromInt(1600);
        Check(attenuation.Gain(source, listener) == 0, "Sound beyond hearing distance remained audible.");
        source.X = new Fixed(int.MaxValue); listener.X = new Fixed(int.MinValue);
        Check(attenuation.Gain(source, listener) == 0, "Extreme coordinates wrapped into hearing range.");
        var invalid = false;
        try { new HereticSoundAttenuation(new byte[1599]); } catch (ArgumentException) { invalid = true; }
        Check(invalid, "Truncated SNDCURVE accepted.");
        var wadCurve = new HereticSoundAttenuation(content.Wad.ReadLump(content.Wad.GetLumpNumber("SNDCURVE")));
        Check(wadCurve.Gain(s.Body, s.Body) > 0, "Bundled sound curve muted local audio.");
        source.X = Fixed.Zero; source.Y = Fixed.FromInt(100); listener.X = listener.Y = Fixed.Zero; listener.Angle = Angle.Ang0;
        var left = HereticSoundAttenuation.Separation(source, listener);
        source.Y = Fixed.FromInt(-100);
        var right = HereticSoundAttenuation.Separation(source, listener);
        Check(left < 128 && right > 128 && HereticSoundAttenuation.Separation(listener, listener) == 128,
            "Sound orientation or local centering differs.");
        listener.Angle = Angle.Ang180;
        Check(HereticSoundAttenuation.Separation(source, listener) < 128, "Listener rotation did not reverse pan.");
        var pcm = new short[] { short.MinValue, 0, short.MaxValue };
        var centered = HereticSoundAttenuation.Stereo(pcm, 128);
        Check(centered.Length == 6 && centered[0] == centered[1] && centered[4] == centered[5], "Centered stereo conversion differs.");
        var hardLeft = HereticSoundAttenuation.Stereo(pcm, 0);
        var hardRight = HereticSoundAttenuation.Stereo(pcm, 256);
        Check(hardLeft[0] == short.MinValue && hardLeft[1] == 0 && hardRight[0] == 0 && hardRight[1] == short.MinValue,
            "Stereo interleaving, side gain or sample limits differ.");
        Console.WriteLine("PASS Heretic stereo: left/right orientation, listener rotation, local centering and interleaved PCM bounds");
        Console.WriteLine("PASS Heretic sound attenuation: curve validation, distance, local gain and extreme-coordinate bounds");
        Console.WriteLine($"PASS Heretic sound: {count} licensed WAD samples, validated DMX decoding and weapon/impact/enemy events");
    }
}
