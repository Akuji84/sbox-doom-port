// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
using ManagedDoom.Video;
static class HereticTranslucencyChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var s = new HereticWorldSession(content); var enemy = s.StartClinkTest();
        Check(enemy != null, "Translucency test requires a visible Clink.");
        s.Body.Angle = Geometry.PointToAngle(s.Body.X, s.Body.Y, enemy.Body.X, enemy.Body.Y);
        s.Body.UpdateFrameInterpolationInfo(); s.Camera.UpdateFrameInterpolationInfo();
        var screen = new DrawScreen(content.Wad, 320, 200);
        var renderer = new ThreeDRenderer(content, screen, 8);
        var tint = content.Wad.ReadLump(content.Wad.GetLumpNumber("TINTTAB"));
        byte[] Frame() { renderer.Render(s.Camera, Fixed.One); return (byte[])screen.Data.Clone(); }
        foreach (var yaw in new[] { 0, 20, -20 })
        {
            var angle = s.Body.Angle; s.Body.Angle += Angle.FromDegree(yaw);
            s.Body.UpdateFrameInterpolationInfo();
            s.World.ThingMovement.UnsetThingPosition(enemy.Body); var background = Frame();
            s.World.ThingMovement.SetThingPosition(enemy.Body);
            enemy.Body.Flags &= ~MobjFlags.Shadow; var opaque = Frame();
            enemy.Body.Flags |= MobjFlags.Shadow; var ghost = Frame(); var repeat = Frame();
            Check(ghost.SequenceEqual(repeat), "Heretic ghost rendering retained animated Doom fuzz.");
            var visible = 0;
            for (var i = 0; i < opaque.Length; i++)
            {
                if (opaque[i] == background[i]) continue;
                visible++;
                Check(ghost[i] == tint[(background[i] << 8) + opaque[i]], "World ghost pixel does not match lit-source TINTTAB blend.");
            }
            Check(visible > 0, "Ghost comparison had no visible sprite pixels.");
            s.Body.Angle = angle;
        }
        var pickupSession = new HereticWorldSession(content);
        var thing = pickupSession.World.Map.Things.First(t => t.Type == 10 && ((int)t.Flags & 2) != 0 && ((int)t.Flags & 16) == 0);
        thing.Type = 75; pickupSession.StartClinkTest();
        var pickup = pickupSession.Actors.First(a => a.Type == HereticActorType.MT_ARTIINVISIBILITY);
        Check((pickup.Body.Flags & MobjFlags.Shadow) != 0, "Shadowsphere pickup lost its native translucent flag.");
        Console.WriteLine("PASS Heretic world translucency: exact palette blending, angled views, repeatable rendering and Shadowsphere pickup flag");
    }
}
