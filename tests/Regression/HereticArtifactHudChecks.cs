// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
using ManagedDoom.Video;
static class HereticArtifactHudChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var session = new HereticWorldSession(content);
        var hud = new HereticArtifactHud(content.Wad); var screen = new DrawScreen(content.Wad, 320, 200);
        byte[] Draw(bool inventory)
        {
            screen.FillRect(0, 0, 320, 200, 103);
            hud.Render(session, screen, inventory);
            return (byte[])screen.Data.Clone();
        }
        var empty = Draw(false); var zeroCounts = Draw(true);
        Check(!empty.SequenceEqual(zeroCounts), "Artifact inventory did not draw.");
        for (var slot = 0; slot < 9; slot++)
        {
            Check(Enumerable.Range(20 + slot * 31, 30).Any(x => Enumerable.Range(157, 41).Any(y => zeroCounts[x * 200 + y] != 103)), "Missing artifact slot/key artwork.");
        }
        foreach (HereticArtifact artifact in Enum.GetValues<HereticArtifact>()) session.GiveArtifact(artifact);
        var ones = Draw(true); Check(!ones.SequenceEqual(zeroCounts), "One-count inventory matches zero.");
        foreach (HereticArtifact artifact in Enum.GetValues<HereticArtifact>())
            for (var i = 1; i < 16; i++) session.GiveArtifact(artifact);
        var full = Draw(true); Check(!full.SequenceEqual(ones), "Two-digit counts did not render.");
        for (var slot = 0; slot < 9; slot++)
            Check(Enumerable.Range(20 + slot * 31 + 18, 9).Any(x => Enumerable.Range(190, 5).Any(y => full[x * 200 + y] != ones[x * 200 + y])), "Count not updated in artifact slot.");
        var rng = session.World.Random.Index; var time = session.World.LevelTime;
        Check(Draw(true).SequenceEqual(full) && session.World.Random.Index == rng && session.World.LevelTime == time && session.State.TomesOfPower == 16, "Drawing mutated gameplay or was unstable.");
        session.State.WeaponPowerTics = 1400;
        var book0 = Draw(false); Check(!book0.SequenceEqual(empty), "Active Tome icon missing.");
        session.World.SetPreviewTime(3); var book1 = Draw(false);
        Check(!book1.SequenceEqual(book0), "Tome icon did not animate at three-tick cadence.");
        session.State.WeaponPowerTics = 128; Check(!Draw(false).SequenceEqual(empty), "Tome blink visible phase missing.");
        session.State.WeaponPowerTics = 112; Check(Draw(false).SequenceEqual(empty), "Tome blink hidden phase remained visible.");
        session.State.WeaponPowerTics = 0; Check(Draw(false).SequenceEqual(empty), "Expired Tome icon remained visible.");
        session.DamageEnvironment(10000); Check(Draw(true).SequenceEqual(empty), "Artifact HUD remained after death.");

        var playable = new HereticWorldSession(content); playable.StartClinkTest();
        foreach (HereticArtifact artifact in Enum.GetValues<HereticArtifact>()) playable.GiveArtifact(artifact);
        playable.GiveArtifact(HereticArtifact.TomeOfPower); playable.UseArtifact(HereticArtifact.TomeOfPower);
        var view = new HereticMapPreview(content, playable); var pixels = new byte[320 * 200 * 4];
        view.Render(pixels, 0); var closed = (byte[])pixels.Clone();
        view.InventoryVisible = true; view.Render(pixels, 0);
        Check(!pixels.SequenceEqual(closed), "Preview inventory toggle was not connected.");
        var opened = (byte[])pixels.Clone(); view.Render(pixels, 200);
        Check(pixels.SequenceEqual(opened), "HUD animation used render argument instead of simulation time.");
        var output = Environment.GetEnvironmentVariable("HERETIC_HUD_RGBA");
        if (!string.IsNullOrEmpty(output)) File.WriteAllBytes(output, pixels);
        view.InventoryVisible = false; view.Render(pixels, 0);
        Check(pixels.SequenceEqual(closed), "Closing inventory left stale pixels.");
        view.AutomapVisible = true; view.Render(pixels, 0); var map = (byte[])pixels.Clone();
        view.InventoryVisible = true; view.Render(pixels, 0);
        Check(!pixels.SequenceEqual(map), "Inventory did not overlay automap.");
        Console.WriteLine("PASS artifact HUD: licensed artwork, all nine counts/keys, Tome animation/blink, stable read-only rendering, toggling, automap and death");
    }
}
