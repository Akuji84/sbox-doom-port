// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticWandChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var s = new HereticWorldSession(content); var wand = new HereticGoldWand(s);
        var shots = new List<(int tic, HereticWandShot shot)>(); var now = 0;
        wand.ShotFired += shot => shots.Add((now, shot));
        for (now = 1; now <= 50; now++) wand.Tick(true);
        Check(shots.Count == 3 && shots[0].tic == 18 && shots[1].tic == 29 && shots[2].tic == 40, "Gold Wand raise/attack/refire timing differs.");
        Check(wand.Ammo == 47 && shots.All(x => x.shot.Damage >= 7 && x.shot.Damage <= 14), "Gold Wand ammo or damage differs.");
        Check(shots[0].shot.Angle == s.Body.Angle && wand.Refire > 0, "First shot spread/refire state wrong.");
        for (now = 51; now <= 70; now++) wand.Tick(false);
        var count = shots.Count;
        Check(wand.Refire == 0 && wand.State == HereticStateId.S_GOLDWANDREADY, "Release did not return wand to ready state.");
        wand.Ammo = 0;
        for (var i = 0; i < 30; i++) wand.Tick(true);
        Check(shots.Count == count && wand.Ammo == 0, "Empty wand fired or ammo went negative.");
        wand.Ammo = 1;
        for (var i = 0; i < 30; i++) wand.Tick(true);
        Check(shots.Count == count + 1 && wand.Ammo == 0, "Last round did not fire exactly once.");
        s.DamageEnvironment(100);
        for (var i = 0; i < 30; i++) wand.Tick(true);
        Check(!wand.Visible && shots.Count == count + 1, "Dead player fired or weapon failed to lower.");
        var renderSession = new HereticWorldSession(content);
        Check(renderSession.StartClinkTest() != null, "Wand rendering fixture spawn failed.");
        var preview = new HereticMapPreview(content, renderSession);
        var pixels = new byte[320 * 200 * 4];
        preview.Render(pixels, 0); var lowered = pixels.ToArray();
        for (var i = 0; i < 16; i++) renderSession.GoldWand.Tick(false);
        preview.Render(pixels, 0); var ready = pixels.ToArray();
        Check(!ready.SequenceEqual(lowered), "Raised Gold Wand not visible in shared preview.");
        for (var i = 0; i < 4; i++) renderSession.GoldWand.Tick(true);
        preview.Render(pixels, 0);
        Check(!pixels.SequenceEqual(ready), "Firing frame did not change weapon rendering.");
        var output = Environment.GetEnvironmentVariable("HERETIC_WAND_RGBA");
        if (!string.IsNullOrEmpty(output)) File.WriteAllBytes(output, pixels);
        var look = new HereticWorldSession(content); look.State.LookDirection = 45;
        var aimed = new HereticGoldWand(look); HereticWandShot? shotResult = null;
        aimed.ShotFired += shot => shotResult = shot;
        for (var i = 0; i < 19; i++) aimed.Tick(true);
        Check(shotResult?.Slope == Fixed.FromInt(45) / 173, "No-target wand aim ignored view pitch.");
        Console.WriteLine("PASS normal Gold Wand: reference cadence, 7-14 damage, ammo, refire/release, empty weapon, death lowering, pitch fallback and sprite overlay");
    }
}
