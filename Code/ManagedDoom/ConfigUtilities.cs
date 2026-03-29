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
    public static class ConfigUtilities
    {
        private static readonly string defaultConfigPath = "managed-doom.cfg";
        private static readonly string[] iwadNames = new string[]
        {
            "DOOM2.WAD",
            "PLUTONIA.WAD",
            "TNT.WAD",
            "DOOM.WAD",
            "DOOM1.WAD",
            "FREEDOOM2.WAD",
            "FREEDOOM1.WAD"
        };

        public static string GetExeDirectory()
        {
            return string.Empty;
        }

        public static string GetConfigPath()
        {
            return defaultConfigPath;
        }

        public static void SetHostWadPaths(params string[] paths)
        {
            SboxManagedDoomFileSystem.SetHostWadPaths(paths);
        }

        public static string GetDefaultIwadPath()
        {
            foreach (var path in SboxManagedDoomFileSystem.HostWadPaths)
            {
                if (IsIwad(path))
                {
                    return path;
                }
            }

            throw new Exception("No IWAD was found!");
        }

        public static bool IsIwad(string path)
        {
            var name = SboxManagedDoomFileSystem.GetFileName(path).ToUpperInvariant();
            return iwadNames.Contains(name);
        }

        public static string[] GetWadPaths(CommandLineArgs args)
        {
            var wadPaths = new List<string>();

            if (args.iwad.Present)
            {
                wadPaths.Add(args.iwad.Value);
            }
            else
            {
                wadPaths.Add(ConfigUtilities.GetDefaultIwadPath());
            }

            if (args.file.Present)
            {
                foreach (var path in args.file.Value)
                {
                    wadPaths.Add(path);
                }
            }

            return wadPaths.ToArray();
        }
    }
}
