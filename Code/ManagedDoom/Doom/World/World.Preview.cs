// Copyright (C) 2026 s&Doom contributors
// SPDX-License-Identifier: GPL-2.0-or-later
using System;
namespace ManagedDoom
{
    public sealed partial class World
    {
        public bool IsGeometryPreview { get; private set; }
        private World(GameContent content, GameOptions options)
        {
            if (content.Profile.Family != GameFamily.Heretic)
                throw new ArgumentException("Geometry preview requires Heretic content.");
            IsGeometryPreview = true;
            this.options = options;
            random = options.Random;
            map = new Map(content, this);
            thinkers = new Thinkers(this);
            specials = new Specials(this);
            // Deliberately do not spawn things or interpret Doom line/sector specials.
        }
        internal static World CreateGeometryPreview(GameContent content, int episode, int map)
        {
            return new World(content, new GameOptions { Episode = episode, Map = map });
        }
        internal void SetPreviewTime(int tic)
        {
            if (!IsGeometryPreview) throw new InvalidOperationException("Not a geometry preview.");
            levelTime = Math.Max(0, tic);
            specials.UpdateAnimations(levelTime);
        }
    }
}
