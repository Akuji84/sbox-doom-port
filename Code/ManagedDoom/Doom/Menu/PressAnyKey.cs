// s&Doom modification notice (added 2026-09-16).
// This file has been modified from Managed Doom for the s&Doom port.
// Recorded project revision dates: 2026-03-28, 2026-03-29.
// Original copyright and GPL terms below remain unchanged.

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



using System;
using System.Collections.Generic;

namespace ManagedDoom
{
    public sealed class PressAnyKey : MenuDef
    {
        private string[] text;
        private Action action;

        public PressAnyKey(DoomMenu menu, string text, Action action) : base(menu)
        {
            this.text = text.Split('\n');
            this.action = action;
        }

        public void SetText(string value)
        {
            text = (value ?? string.Empty).Split('\n');
        }

        public override bool DoEvent(DoomEvent e)
        {
            if (e.Type == EventType.KeyDown)
            {
                if (action != null)
                {
                    action();
                }

                Menu.Close();
                Menu.StartSound(Sfx.SWTCHX);

                return true;
            }

            return true;
        }

        public IReadOnlyList<string> Text => text;
    }
}
