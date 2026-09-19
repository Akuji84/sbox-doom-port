// Copyright (C) 2026 s&Doom contributors
// SPDX-License-Identifier: GPL-2.0-or-later
using System;
using System.Linq;
using ManagedDoom.Video;
namespace ManagedDoom
{
    /// <summary>Shared-renderer geometry inspection, not a playable Heretic session.</summary>
    public sealed class HereticMapPreview
    {
        private readonly GameContent content;
        private readonly World world;
        private readonly Player camera;
        private readonly DrawScreen screen;
        private readonly ThreeDRenderer renderer;
        public const int Width = 320;
        public const int Height = 200;
        public string Report { get; }
        public HereticMapPreview(GameContent content, int episode = 1, int map = 1)
            : this(content, World.CreateGeometryPreview(content, episode, map), null) { }
        public HereticMapPreview(GameContent content, HereticWorldSession session)
            : this(content, session.World, session.Camera) { }
        private HereticMapPreview(GameContent content, World previewWorld, Player player)
        {
            this.content = content;
            world = previewWorld;
            var start = world.Map.Things.FirstOrDefault(t => t.Type == 1);
            if (start == null) throw new InvalidOperationException("Heretic preview requires a player-one start.");
            var body = new Mobj(world) { X = start.X, Y = start.Y, Angle = start.Angle };
            body.Subsector = Geometry.PointInSubsector(body.X, body.Y, world.Map);
            body.Z = body.Subsector.Sector.FloorHeight;
            camera = player ?? new Player(0) { Mobj = body, ViewZ = body.Z + Fixed.FromInt(41) };
            screen = new DrawScreen(content.Wad, Width, Height);
            renderer = new ThreeDRenderer(content, screen, 8);
            Report = player != null ? world.Map.Title + ": Heretic navigation and scenery checkpoint. Combat, enemies, audio, inventory, saves and multiplayer are not active." : world.Map.Title + ": geometry preview. " + world.Map.Things.Length + " things omitted; " +
                world.Map.Lines.Count(l => l.Special != 0) + " line specials and " +
                world.Map.Sectors.Count(s => s.Special != 0) +
                " sector specials inactive. Combat, movement, sounds, inventory, saves and multiplayer are not implemented.";
        }
        public void Render(byte[] rgba, int tic, double yawDegrees = 0)
        {
            if (rgba == null || rgba.Length != Width * Height * 4)
                throw new ArgumentException("Preview requires a 320x200 RGBA buffer.");
            if (world.HereticSession == null) world.SetPreviewTime(tic);
            var original = camera.Mobj.Angle;
            camera.Mobj.Angle = original + Angle.FromDegree(yawDegrees);
            try { renderer.Render(camera, Fixed.One, world.HereticSession?.State.LookDirection ?? 0); }
            finally { camera.Mobj.Angle = original; }
            var palette = content.Palette[0];
            for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
            {
                var color = palette[screen.Data[x * Height + y]];
                var offset = (y * Width + x) * 4;
                rgba[offset] = (byte)color;
                rgba[offset + 1] = (byte)(color >> 8);
                rgba[offset + 2] = (byte)(color >> 16);
                rgba[offset + 3] = 255;
            }
        }
    }
}
