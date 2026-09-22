// s&Doom modification: 2026-09-22, artifact overview and Tome HUD indicator.
// s&Doom modification: 2026-09-22, sector lighting and full-bright Heretic weapon frames.
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
        private readonly byte[] tintTable;
        public const int Width = 320;
        public const int Height = 200;
        public string Report { get; }
        public bool AutomapVisible { get; set; }
        public HereticAutomapView Automap { get; }
        private readonly Patch[] markDigits;
        private readonly HereticArtifactHud artifactHud;
        public bool InventoryVisible { get; set; }
        public void ZoomAutomap(bool closer) => Automap?.ChangeZoom(closer);
        public HereticMapPreview(GameContent content, int episode = 1, int map = 1)
            : this(content, World.CreateGeometryPreview(content, episode, map), null) { }
        public HereticMapPreview(GameContent content, HereticWorldSession session)
            : this(content, session.World, session.Camera) { }
        private HereticMapPreview(GameContent content, World previewWorld, Player player)
        {
            this.content = content;
            world = previewWorld;
            if (world.HereticSession != null)
            {
                Automap = new HereticAutomapView(world.HereticSession);
                artifactHud = new HereticArtifactHud(content.Wad);
                markDigits = Enumerable.Range(0, 10).Select(i => Patch.FromWad(content.Wad, "IN" + i)).ToArray();
            }
            tintTable = content.Wad.ReadLump(content.Wad.GetLumpNumber("TINTTAB"));
            if (tintTable.Length != 65536) throw new InvalidOperationException("Heretic TINTTAB must contain 65536 bytes.");
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
            if (AutomapVisible && world.HereticSession != null)
                HereticAutomap.Render(world.HereticSession, screen, Automap, markDigits);
            else
            {
                var original = camera.Mobj.Angle;
                camera.Mobj.Angle = original + Angle.FromDegree(yawDegrees);
                try { renderer.Render(camera, Fixed.One, world.HereticSession?.State.LookDirection ?? 0); }
                finally { camera.Mobj.Angle = original; }
                var wand = world.HereticSession?.GoldWand;
                if (wand?.Visible == true)
                {
                    var state = wand.Definition;
                    var frame = content.Sprites[(Sprite)state.Sprite].Frames[state.Frame & 0x7fff];
                    var invisible = world.HereticSession.State.InvisibilityTics;
                    var tint = invisible > 128 || (invisible & 8) != 0 ? tintTable : null;
                    var map = renderer.GetWeaponColorMap(camera.Mobj.Subsector.Sector.LightLevel, (state.Frame & 0x8000) != 0, tint != null);
                    if (frame.Flip[0]) screen.DrawPatchFlip(frame.Patches[0], wand.X.ToIntFloor(), wand.Y.ToIntFloor(), 1, map, tint);
                    else screen.DrawPatch(frame.Patches[0], wand.X.ToIntFloor(), wand.Y.ToIntFloor(), 1, map, tint);
                }
            }
            if (world.HereticSession?.GoldWand != null) artifactHud.Render(world.HereticSession, screen, InventoryVisible);
            var palette = content.Palette[world.HereticSession?.State.PaletteIndex ?? 0];
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
