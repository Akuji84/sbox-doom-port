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
using System.Text;

namespace ManagedDoom
{
    public sealed class Config
    {
        public KeyBinding key_forward;
        public KeyBinding key_backward;
        public KeyBinding key_strafeleft;
        public KeyBinding key_straferight;
        public KeyBinding key_turnleft;
        public KeyBinding key_turnright;
        public KeyBinding key_fire;
        public KeyBinding key_use;
        public KeyBinding key_run;
        public KeyBinding key_strafe;
        public KeyBinding key_weapon1;
        public KeyBinding key_weapon2;
        public KeyBinding key_weapon3;
        public KeyBinding key_weapon4;
        public KeyBinding key_weapon5;
        public KeyBinding key_weapon6;
        public KeyBinding key_weapon7;

        public int mouse_sensitivity;
        public bool mouse_disableyaxis;

        public bool game_alwaysrun;

        public int video_screenwidth;
        public int video_screenheight;
        public bool video_fullscreen;
        public bool video_highresolution;
        public bool video_displaymessage;
        public int video_gamescreensize;
        public int video_gammacorrection;
        public int video_fpsscale;

        public int audio_soundvolume;
        public int audio_musicvolume;
        public bool audio_randompitch;
        public string audio_soundfont;
        public bool audio_musiceffect;

        private bool isRestoredFromFile;

        // Default settings.
        public Config()
        {
            key_forward = new KeyBinding(
                new DoomKey[]
                {
                    DoomKey.Up,
                    DoomKey.W
                });
            key_backward = new KeyBinding(
                new DoomKey[]
                {
                    DoomKey.Down,
                    DoomKey.S
                });
            key_strafeleft = new KeyBinding(
                new DoomKey[]
                {
                    DoomKey.A
                });
            key_straferight = new KeyBinding(
                new DoomKey[]
                {
                    DoomKey.D
                });
            key_turnleft = new KeyBinding(
                new DoomKey[]
                {
                    DoomKey.Left
                });
            key_turnright = new KeyBinding(
                new DoomKey[]
                {
                    DoomKey.Right
                });
            key_fire = new KeyBinding(
                new DoomKey[]
                {
                    DoomKey.LControl,
                    DoomKey.RControl
                },
                new DoomMouseButton[]
                {
                    DoomMouseButton.Mouse1
                });
            key_use = new KeyBinding(
                new DoomKey[]
                {
                    DoomKey.Space
                },
                new DoomMouseButton[]
                {
                    DoomMouseButton.Mouse2
                });
            key_run = new KeyBinding(
                new DoomKey[]
                {
                    DoomKey.LShift,
                    DoomKey.RShift
                });
            key_strafe = new KeyBinding(
                new DoomKey[]
                {
                    DoomKey.LAlt,
                    DoomKey.RAlt
                });

            key_weapon1 = new KeyBinding(new DoomKey[] { DoomKey.Num1 });
            key_weapon2 = new KeyBinding(new DoomKey[] { DoomKey.Num2 });
            key_weapon3 = new KeyBinding(new DoomKey[] { DoomKey.Num3 });
            key_weapon4 = new KeyBinding(new DoomKey[] { DoomKey.Num4 });
            key_weapon5 = new KeyBinding(new DoomKey[] { DoomKey.Num5 });
            key_weapon6 = new KeyBinding(new DoomKey[] { DoomKey.Num6 });
            key_weapon7 = new KeyBinding(new DoomKey[] { DoomKey.Num7 });

            mouse_sensitivity = 8;
            mouse_disableyaxis = false;

            game_alwaysrun = true;

            video_screenwidth = 640;
            video_screenheight = 400;
            video_fullscreen = false;
            video_highresolution = true;
            video_gamescreensize = 7;
            video_displaymessage = true;
            video_gammacorrection = 2;
            video_fpsscale = 2;

            audio_soundvolume = 6;
            audio_musicvolume = 12;
            audio_randompitch = true;
            audio_soundfont = "TimGM6mb.sf2";
            audio_musiceffect = true;

            isRestoredFromFile = false;
        }

        public Config(string path) : this()
        {
            try
            {
                if (!SboxManagedDoomFileSystem.DataFileExists(path))
                {
                    return;
                }

                var dic = ParseConfigText(SboxManagedDoomFileSystem.ReadAllTextFromData(path));

                key_forward = GetKeyBinding(dic, nameof(key_forward), key_forward);
                key_backward = GetKeyBinding(dic, nameof(key_backward), key_backward);
                key_strafeleft = GetKeyBinding(dic, nameof(key_strafeleft), key_strafeleft);
                key_straferight = GetKeyBinding(dic, nameof(key_straferight), key_straferight);
                key_turnleft = GetKeyBinding(dic, nameof(key_turnleft), key_turnleft);
                key_turnright = GetKeyBinding(dic, nameof(key_turnright), key_turnright);
                key_fire = GetKeyBinding(dic, nameof(key_fire), key_fire);
                key_use = GetKeyBinding(dic, nameof(key_use), key_use);
                key_run = GetKeyBinding(dic, nameof(key_run), key_run);
                key_strafe = GetKeyBinding(dic, nameof(key_strafe), key_strafe);
                key_weapon1 = GetKeyBinding(dic, nameof(key_weapon1), key_weapon1);
                key_weapon2 = GetKeyBinding(dic, nameof(key_weapon2), key_weapon2);
                key_weapon3 = GetKeyBinding(dic, nameof(key_weapon3), key_weapon3);
                key_weapon4 = GetKeyBinding(dic, nameof(key_weapon4), key_weapon4);
                key_weapon5 = GetKeyBinding(dic, nameof(key_weapon5), key_weapon5);
                key_weapon6 = GetKeyBinding(dic, nameof(key_weapon6), key_weapon6);
                key_weapon7 = GetKeyBinding(dic, nameof(key_weapon7), key_weapon7);

                mouse_sensitivity = GetInt(dic, nameof(mouse_sensitivity), mouse_sensitivity);
                mouse_disableyaxis = GetBool(dic, nameof(mouse_disableyaxis), mouse_disableyaxis);

                game_alwaysrun = GetBool(dic, nameof(game_alwaysrun), game_alwaysrun);

                video_screenwidth = GetInt(dic, nameof(video_screenwidth), video_screenwidth);
                video_screenheight = GetInt(dic, nameof(video_screenheight), video_screenheight);
                video_fullscreen = GetBool(dic, nameof(video_fullscreen), video_fullscreen);
                video_highresolution = GetBool(dic, nameof(video_highresolution), video_highresolution);
                video_displaymessage = GetBool(dic, nameof(video_displaymessage), video_displaymessage);
                video_gamescreensize = GetInt(dic, nameof(video_gamescreensize), video_gamescreensize);
                video_gammacorrection = GetInt(dic, nameof(video_gammacorrection), video_gammacorrection);
                video_fpsscale = GetInt(dic, nameof(video_fpsscale), video_fpsscale);

                audio_musicvolume = GetInt(dic, nameof(audio_musicvolume), audio_musicvolume);
                audio_randompitch = GetBool(dic, nameof(audio_randompitch), audio_randompitch);
                audio_soundfont = GetString(dic, nameof(audio_soundfont), audio_soundfont);
                audio_musiceffect = GetBool(dic, nameof(audio_musiceffect), audio_musiceffect);

                isRestoredFromFile = true;
            }
            catch
            {
            }
        }

        public void Save(string path)
        {
            try
            {
                var sb = new StringBuilder();
                Append(sb, nameof(key_forward), key_forward.ToString());
                Append(sb, nameof(key_backward), key_backward.ToString());
                Append(sb, nameof(key_strafeleft), key_strafeleft.ToString());
                Append(sb, nameof(key_straferight), key_straferight.ToString());
                Append(sb, nameof(key_turnleft), key_turnleft.ToString());
                Append(sb, nameof(key_turnright), key_turnright.ToString());
                Append(sb, nameof(key_fire), key_fire.ToString());
                Append(sb, nameof(key_use), key_use.ToString());
                Append(sb, nameof(key_run), key_run.ToString());
                Append(sb, nameof(key_strafe), key_strafe.ToString());
                Append(sb, nameof(key_weapon1), key_weapon1.ToString());
                Append(sb, nameof(key_weapon2), key_weapon2.ToString());
                Append(sb, nameof(key_weapon3), key_weapon3.ToString());
                Append(sb, nameof(key_weapon4), key_weapon4.ToString());
                Append(sb, nameof(key_weapon5), key_weapon5.ToString());
                Append(sb, nameof(key_weapon6), key_weapon6.ToString());
                Append(sb, nameof(key_weapon7), key_weapon7.ToString());

                Append(sb, nameof(mouse_sensitivity), mouse_sensitivity.ToString());
                Append(sb, nameof(mouse_disableyaxis), BoolToString(mouse_disableyaxis));
                Append(sb, nameof(game_alwaysrun), BoolToString(game_alwaysrun));

                Append(sb, nameof(video_screenwidth), video_screenwidth.ToString());
                Append(sb, nameof(video_screenheight), video_screenheight.ToString());
                Append(sb, nameof(video_fullscreen), BoolToString(video_fullscreen));
                Append(sb, nameof(video_highresolution), BoolToString(video_highresolution));
                Append(sb, nameof(video_displaymessage), BoolToString(video_displaymessage));
                Append(sb, nameof(video_gamescreensize), video_gamescreensize.ToString());
                Append(sb, nameof(video_gammacorrection), video_gammacorrection.ToString());
                Append(sb, nameof(video_fpsscale), video_fpsscale.ToString());

                Append(sb, nameof(audio_musicvolume), audio_musicvolume.ToString());
                Append(sb, nameof(audio_randompitch), BoolToString(audio_randompitch));
                Append(sb, nameof(audio_soundfont), audio_soundfont ?? string.Empty);
                Append(sb, nameof(audio_musiceffect), BoolToString(audio_musiceffect));

                SboxManagedDoomFileSystem.WriteAllTextToData(path, sb.ToString());
            }
            catch
            {
            }
        }

        private static Dictionary<string, string> ParseConfigText(string text)
        {
            var dic = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(text))
            {
                return dic;
            }

            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#"))
                {
                    continue;
                }

                var separator = line.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                var key = line[..separator].Trim();
                var value = line[(separator + 1)..].Trim();
                dic[key] = value;
            }

            return dic;
        }

        private static void Append(StringBuilder sb, string key, string value)
        {
            sb.Append(key);
            sb.Append('=');
            sb.AppendLine(value ?? string.Empty);
        }

        private static int GetInt(Dictionary<string, string> dic, string name, int defaultValue)
        {
            string stringValue;
            if (dic.TryGetValue(name, out stringValue))
            {
                int value;
                if (int.TryParse(stringValue, out value))
                {
                    return value;
                }
            }

            return defaultValue;
        }

        private static string GetString(Dictionary<string, string> dic, string name, string defaultValue)
        {
            string stringValue;
            if (dic.TryGetValue(name, out stringValue))
            {
                return stringValue;
            }

            return defaultValue;
        }

        private static bool GetBool(Dictionary<string, string> dic, string name, bool defaultValue)
        {
            string stringValue;
            if (dic.TryGetValue(name, out stringValue))
            {
                if (stringValue == "true")
                {
                    return true;
                }
                else if (stringValue == "false")
                {
                    return false;
                }
            }

            return defaultValue;
        }

        private static KeyBinding GetKeyBinding(Dictionary<string, string> dic, string name, KeyBinding defaultValue)
        {
            string stringValue;
            if (dic.TryGetValue(name, out stringValue))
            {
                return KeyBinding.Parse(stringValue);
            }

            return defaultValue;
        }

        private static string BoolToString(bool value)
        {
            return value ? "true" : "false";
        }

        public bool IsRestoredFromFile => isRestoredFromFile;
    }
}
