// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticFeedbackChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var s = new HereticWorldSession(content);
        var preview = new HereticMapPreview(content, s);
        var pixels = new byte[320 * 200 * 4];
        preview.Render(pixels, 0); var normal = pixels.ToArray();
        s.State.PickupFlash = int.MaxValue;
        Check(s.State.PaletteIndex == 12, "Large pickup counter overflowed palette selection.");
        s.State.PickupFlash = 6;
        Check(s.State.PaletteIndex == 10, "Pickup palette differs.");
        preview.Render(pixels, 0); var bonus = pixels.ToArray();
        Check(!bonus.SequenceEqual(normal), "Pickup palette not rendered.");
        s.DamageEnvironment(8);
        Check(s.State.PaletteIndex == 2, "Damage did not override pickup palette.");
        preview.Render(pixels, 0);
        Check(!pixels.SequenceEqual(normal) && !pixels.SequenceEqual(bonus), "Damage palette not rendered.");
        preview.Render(pixels, 0);
        Check(s.State.DamageFlash == 8 && s.State.PickupFlash == 6, "Rendering advanced feedback timers.");
        for (var i = 0; i < 8; i++) s.Tick(default);
        Check(s.State.PaletteIndex == 0, "Feedback failed to decay at simulation rate.");
        s.DamageEnvironment(10000);
        Check(s.State.DamageFlash == 100 && s.State.PaletteIndex == 8, "Lethal flash not capped.");
        for (var i = 0; i < 100; i++) s.Tick(default);
        Check(s.State.PaletteIndex == 0, "Death left a permanent damage flash.");
        Console.WriteLine("PASS Heretic feedback: visible palettes, damage priority, render independence, tick decay and death cap");
    }
}
