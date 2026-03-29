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
using System.Linq;

namespace ManagedDoom
{
    public sealed class BugReportMenu : MenuDef
    {
        private readonly int[] skullY = { 56, 88, 120, 170, 186 };
        private readonly TextBoxMenuItem contact;
        private readonly TextBoxMenuItem map;
        private readonly TextBoxMenuItem details;

        private int index;
        private TextInput textInput;

        public BugReportMenu(DoomMenu menu) : base(menu)
        {
            contact = new TextBoxMenuItem(32, 56, 72, 56);
            map = new TextBoxMenuItem(32, 88, 72, 88);
            details = new TextBoxMenuItem(32, 120, 72, 120, 22, 3);

            index = 0;
        }

        public override void Open()
        {
            index = 0;
            textInput = null;
            map.SetText(GetDefaultMapText());
        }

        public override bool DoEvent(DoomEvent e)
        {
            if (e.Type != EventType.KeyDown)
            {
                return true;
            }

            if (textInput != null)
            {
                var result = textInput.DoEvent(e);
                if (textInput.State == TextInputState.Canceled || textInput.State == TextInputState.Finished)
                {
                    textInput = null;
                }

                return result;
            }

            if (e.Key == DoomKey.Up)
            {
                index--;
                if (index < 0)
                {
                    index = 4;
                }
                Menu.StartSound(Sfx.PSTOP);
            }

            if (e.Key == DoomKey.Down)
            {
                index++;
                if (index > 4)
                {
                    index = 0;
                }
                Menu.StartSound(Sfx.PSTOP);
            }

            if (e.Key == DoomKey.Enter)
            {
                switch (index)
                {
                    case 0:
                        textInput = contact.Edit(() => { });
                        Menu.StartSound(Sfx.PISTOL);
                        return true;
                    case 1:
                        textInput = map.Edit(() => { });
                        Menu.StartSound(Sfx.PISTOL);
                        return true;
                    case 2:
                        textInput = details.Edit(() => { });
                        Menu.StartSound(Sfx.PISTOL);
                        return true;
                    case 3:
                        Menu.SubmitBugReport(ContactText, MapText, DetailsText);
                        Menu.StartSound(Sfx.PISTOL);
                        return true;
                    case 4:
                        Menu.Close();
                        Menu.StartSound(Sfx.SWTCHX);
                        return true;
                }
            }

            if (e.Key == DoomKey.Escape)
            {
                Menu.Close();
                Menu.StartSound(Sfx.SWTCHX);
            }

            return true;
        }

        private string GetDefaultMapText()
        {
            var doom = Menu.Doom;
            var options = doom.Options;

            if (doom.State == DoomState.Game)
            {
                if (options.GameMode == GameMode.Commercial)
                {
                    return $"MAP{options.Map:00}";
                }

                return $"E{options.Episode}M{options.Map}";
            }

            if (doom.State == DoomState.DemoPlayback)
            {
                if (options.GameMode == GameMode.Commercial)
                {
                    return $"DEMO MAP{options.Map:00}";
                }

                return $"DEMO E{options.Episode}M{options.Map}";
            }

            return "TITLE SCREEN";
        }

        public int SkullX => 32;
        public int SkullY => skullY[index];

        public IReadOnlyList<string> Title => new[] { "REPORT BUG" };
        public IReadOnlyList<string> Labels => new[] { "EMAIL/DISCORD", "CURRENT MAP", "DETAILS", "SUBMIT", "CANCEL" };
        public IReadOnlyList<int> LabelX => new[] { 72, 72, 72, 72, 72 };
        public IReadOnlyList<int> LabelY => new[] { 40, 72, 104, 170, 186 };

        public TextBoxMenuItem Contact => contact;
        public TextBoxMenuItem Map => map;
        public TextBoxMenuItem Details => details;

        public string ContactText => contact.Text != null ? new string(contact.Text.ToArray()) : string.Empty;
        public string MapText => map.Text != null ? new string(map.Text.ToArray()) : string.Empty;
        public string DetailsText => details.Text != null ? new string(details.Text.ToArray()) : string.Empty;
    }
}
