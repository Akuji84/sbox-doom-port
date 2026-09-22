// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticAutomapChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var s = new HereticWorldSession(content);
        var view = new HereticMapPreview(content, s); var pixels = new byte[320 * 200 * 4];
        view.Render(pixels, 0);
        Check(s.World.Map.Lines.Any(l => (l.Flags & LineFlags.Mapped) != 0), "Normal view failed to discover map walls.");
        var line = s.World.Map.Lines.First(l => l.BackSector == null);
        var saved = line.Flags;
        line.Flags &= ~(LineFlags.Mapped | LineFlags.DontDraw);
        Check(HereticAutomap.WallColor(line, false) == null && HereticAutomap.WallColor(line, true) == 43, "Scroll reveal/discovery distinction failed.");
        line.Flags |= LineFlags.Mapped | LineFlags.Secret;
        Check(HereticAutomap.WallColor(line, false) == 96, "Mapped secret boundary failed.");
        line.Flags |= LineFlags.DontDraw;
        Check(HereticAutomap.WallColor(line, true) == null, "Scroll exposed a never-see line."); line.Flags = saved;
        var flags = s.World.Map.Lines.Select(l => l.Flags).ToArray(); var rng = s.World.Random.Index;
        view.AutomapVisible = true; view.Render(pixels, 0); var discovered = (byte[])pixels.Clone();
        s.State.HasMapScroll = true; view.Render(pixels, 0);
        Check(!pixels.SequenceEqual(discovered), "Scroll did not visibly reveal unexplored geometry.");
        var reveal = (byte[])pixels.Clone(); view.Render(pixels, 0);
        Check(pixels.SequenceEqual(reveal) && flags.SequenceEqual(s.World.Map.Lines.Select(l => l.Flags)) && rng == s.World.Random.Index, "Automap rendering changed discovery/randomness or was unstable.");
        view.ZoomAutomap(true); view.Render(pixels, 0); Check(!pixels.SequenceEqual(reveal), "Automap zoom had no effect.");
        for (var i = 0; i < 200; i++) view.ZoomAutomap(true); view.Render(pixels, 0);
        for (var i = 0; i < 200; i++) view.ZoomAutomap(false); view.Render(pixels, 0);
        view.AutomapVisible = false; view.Render(pixels, 0);
        Check(!pixels.SequenceEqual(reveal), "Closing map did not restore 3D view.");
        var p = new HereticWorldSession(content);
        foreach (var thing in p.World.Map.Things.Where(t => t.Type == 10 && ((int)t.Flags & 2) != 0 && ((int)t.Flags & 16) == 0).Take(2)) thing.Type = 35;
        p.StartClinkTest(); var scrolls = p.Actors.Where(a => a.Type == HereticActorType.MT_MISC2).Take(2).ToArray();
        Check(scrolls.Length == 2, "Map scroll test needs two pickups.");
        void Touch(HereticMapActor scroll)
        {
            p.World.ThingMovement.UnsetThingPosition(p.Body); p.Body.X = scroll.Body.X; p.Body.Y = scroll.Body.Y; p.Body.Z = scroll.Body.Z;
            p.World.ThingMovement.SetThingPosition(p.Body); p.Body.FloorZ = scroll.Body.FloorZ; p.Body.CeilingZ = scroll.Body.CeilingZ;
            p.Tick(default);
        }
        Touch(scrolls[0]); Check(p.State.HasMapScroll && scrolls[0].Animation.Removed, "Map scroll did not grant/remove.");
        Touch(scrolls[1]); Check(p.Actors.Contains(scrolls[1]), "Duplicate map scroll was consumed.");
        Console.WriteLine("PASS Heretic automap: discovery, scroll reveal, never-see boundaries, stable rendering, zoom bounds, return to 3D and scroll pickups");
    }
}
