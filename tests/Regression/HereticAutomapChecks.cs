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
        flags = s.World.Map.Lines.Select(l => l.Flags).ToArray();
        var nav = view.Automap;
        Check(nav.Follow && nav.CenterX == s.Body.X.ToFloat(), "Automap did not follow player initially.");
        nav.ToggleFollow(); var cx = nav.CenterX; var cy = nav.CenterY;
        nav.Pan(1, 0, 0.01f); Check(nav.CenterX > cx && nav.CenterY == cy, "Free panning failed.");
        var a = new HereticAutomapView(s); var b = new HereticAutomapView(s); a.ToggleFollow(); b.ToggleFollow();
        for (var i = 0; i < 30; i++) a.Pan(1, 1, 1f / 30);
        for (var i = 0; i < 120; i++) b.Pan(1, 1, 1f / 120);
        Check(Math.Abs(a.CenterX - b.CenterX) < 0.1f && Math.Abs(a.CenterY - b.CenterY) < 0.1f, "Map pan speed depends on frame rate.");
        nav.Pan(1, 1, 10000);
        Check(nav.CenterX == s.World.Map.Vertices.Max(v => v.X.ToFloat()) && nav.CenterY == s.World.Map.Vertices.Max(v => v.Y.ToFloat()), "Map exceeded upper bounds.");
        nav.Pan(-1, -1, 10000);
        Check(nav.CenterX == s.World.Map.Vertices.Min(v => v.X.ToFloat()) && nav.CenterY == s.World.Map.Vertices.Min(v => v.Y.ToFloat()), "Map exceeded lower bounds.");
        nav.ToggleFollow(); nav.Pan(1, 1, 1);
        Check(nav.CenterX == s.Body.X.ToFloat() && nav.CenterY == s.Body.Y.ToFloat(), "Follow did not recenter or ignored pan gating.");
        for (var i = 0; i < 10; i++) nav.AddMark();
        nav.ToggleFollow(); nav.Pan(1, 0, 0.01f); nav.AddMark();
        Check(nav.Marks.Count == 10 && nav.Marks[0].X == nav.CenterX && nav.Marks[1].X == s.Body.X.ToFloat(), "Marker ring did not replace oldest slot.");
        view.AutomapVisible = true; view.Render(pixels, 0); var marked = (byte[])pixels.Clone();
        nav.ClearMarks(); view.Render(pixels, 0);
        Check(nav.Marks.Count == 0 && !marked.SequenceEqual(pixels), "Marker clear did not update rendered map.");
        nav.AddMark(); Check(nav.Marks.Count == 1, "Marker reset retained stale slots.");
        Check(flags.SequenceEqual(s.World.Map.Lines.Select(l => l.Flags)), "Navigation altered map discovery.");
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
