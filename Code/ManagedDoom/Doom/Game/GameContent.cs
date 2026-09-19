//
// Copyright (C) 1993-1996 Id Software, Inc.
// Copyright (C) 2019-2020 Nobuaki Tanaka
// s&Doom modification: 2026-09-18, profile-controlled initialization and failure cleanup.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//



using System;

namespace ManagedDoom
{
    public sealed class GameContent : IDisposable
    {
        private Wad wad;
        private Palette palette;
        private ColorMap colorMap;
        private ITextureLookup textures;
        private IFlatLookup flats;
        private ISpriteLookup sprites;
        private TextureAnimation animation;

        private GameContent()
        {
        }

        public GameContent(CommandLineArgs args, GameProfile profile = null)
        {
            wad = new Wad(profile, ConfigUtilities.GetWadPaths(args));
            try
            {
                Profile.InitializeDefinitions(args, wad);
                palette = new Palette(wad);
                colorMap = new ColorMap(wad);
                textures = new TextureLookup(wad);
                flats = new FlatLookup(wad);
                sprites = new SpriteLookup(wad);
                animation = new TextureAnimation(textures, flats);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        // Asset-only path: never initializes Doom definitions or enables Heretic gameplay.
        public static GameContent CreateHereticPreview(params string[] wadPaths)
        {
            var gc = new GameContent();
            try
            {
                gc.wad = new Wad(GameProfile.Heretic, wadPaths);
                HereticAssets.Validate(gc.wad);
                gc.palette = new Palette(gc.wad);
                gc.palette.ResetColors(1.0);
                gc.colorMap = new ColorMap(gc.wad);
                gc.textures = new TextureLookup(gc.wad);
                gc.flats = new FlatLookup(gc.wad);
                gc.sprites = new SpriteLookup(gc.wad, HereticAssets.SpriteNames);
                gc.animation = new TextureAnimation(gc.textures, gc.flats, HereticAssets.Animations);
                return gc;
            }
            catch (Exception ex)
            {
                gc.Dispose();
                throw new InvalidOperationException("Heretic preview asset load failed: " + ex.Message, ex);
            }
        }

        public static GameContent CreateDummy(params string[] wadPaths)
        {
            var gc = new GameContent();

            gc.wad = new Wad(wadPaths);
            try
            {
                gc.Profile.EnsureRuntimeSupported();
                gc.palette = new Palette(gc.wad);
                gc.colorMap = new ColorMap(gc.wad);
                gc.textures = new DummyTextureLookup(gc.wad);
                gc.flats = new DummyFlatLookup(gc.wad);
                gc.sprites = new DummySpriteLookup(gc.wad);
                gc.animation = new TextureAnimation(gc.textures, gc.flats);

                return gc;
            }
            catch
            {
                gc.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            if (wad != null)
            {
                wad.Dispose();
                wad = null;
            }
        }

        public Wad Wad => wad;
        public GameProfile Profile => wad.Profile;
        public Palette Palette => palette;
        public ColorMap ColorMap => colorMap;
        public ITextureLookup Textures => textures;
        public IFlatLookup Flats => flats;
        public ISpriteLookup Sprites => sprites;
        public TextureAnimation Animation => animation;
    }
}
