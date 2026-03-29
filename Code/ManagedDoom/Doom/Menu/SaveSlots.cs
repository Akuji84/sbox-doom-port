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
using System.Text;

namespace ManagedDoom
{
    public sealed class SaveSlots
    {
        private static readonly int slotCount = 6;
        private const string SlotDataPath = "save-slots.txt";

        private string[] slots;

        private void ReadSlots()
        {
            slots = new string[slotCount];

            try
            {
                if (!SboxManagedDoomFileSystem.DataFileExists(SlotDataPath))
                {
                    return;
                }

                var lines = SboxManagedDoomFileSystem.ReadAllTextFromData(SlotDataPath)
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    var separator = line.IndexOf('=');
                    if (separator <= 0)
                    {
                        continue;
                    }

                    if (!int.TryParse(line[..separator], out var index))
                    {
                        continue;
                    }

                    if (index < 0 || index >= slotCount)
                    {
                        continue;
                    }

                    var value = line[(separator + 1)..];
                    slots[index] = string.IsNullOrWhiteSpace(value) ? null : value;
                }
            }
            catch
            {
            }
        }

        private void SaveSlotsToData()
        {
            try
            {
                var sb = new StringBuilder();
                for (var i = 0; i < slotCount; i++)
                {
                    if (!string.IsNullOrWhiteSpace(slots[i]))
                    {
                        sb.Append(i);
                        sb.Append('=');
                        sb.AppendLine(slots[i]);
                    }
                }

                SboxManagedDoomFileSystem.WriteAllTextToData(SlotDataPath, sb.ToString());
            }
            catch
            {
            }
        }

        public string this[int number]
        {
            get
            {
                if (slots == null)
                {
                    ReadSlots();
                }

                return slots[number];
            }

            set
            {
                if (slots == null)
                {
                    ReadSlots();
                }

                slots[number] = value;
                SaveSlotsToData();
            }
        }

        public int Count => slots.Length;
    }
}
