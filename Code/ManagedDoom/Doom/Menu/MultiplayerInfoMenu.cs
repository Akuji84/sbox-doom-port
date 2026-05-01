//
// Copyright (C) 1993-1996 Id Software, Inc.
// Copyright (C) 2019-2020 Nobuaki Tanaka
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

using System.Collections.Generic;

namespace ManagedDoom
{
    public sealed class MultiplayerInfoMenu : MenuDef
    {
        private readonly string title;
        private readonly string[] lines;
        private readonly MenuDef previous;

        public MultiplayerInfoMenu(DoomMenu menu, string title, MenuDef previous, params string[] lines)
            : base(menu)
        {
            this.title = title;
            this.previous = previous;
            this.lines = lines;
        }

        public override bool DoEvent(DoomEvent e)
        {
            if (e.Type != EventType.KeyDown)
            {
                return true;
            }

            if (e.Key == DoomKey.Enter || e.Key == DoomKey.Escape)
            {
                Menu.SetCurrent(previous);
                Menu.StartSound(Sfx.SWTCHX);
                return true;
            }

            return true;
        }

        public IReadOnlyList<string> Title => new[] { title };

        public IReadOnlyList<string> Lines => lines;
    }
}
