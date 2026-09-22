// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticShootChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        foreach (var code in new[] { 24, 46, 47 })
        {
            var s = new HereticWorldSession(content);
            var hit = s.TraceAim(s.Body.Angle, Fixed.FromInt(2048), Fixed.Zero);
            Check(hit?.Line != null, "Shoot switch ray fixture did not find wall.");
            var line = hit.Value.Line;
            var sector = s.Body.Subsector.Sector;
            foreach (var item in s.World.Map.Sectors) item.Tag = 0;
            sector.Tag = line.Tag = 31000;
            line.Special = (LineSpecial)code;
            var off = s.World.Map.Textures.GetNumber("SW1OFF");
            var on = s.World.Map.Textures.GetNumber("SW1ON");
            Check(off >= 0 && on >= 0, "Switch textures missing.");
            line.FrontSide.TopTexture = off;
            s.TraceAim(s.Body.Angle, Fixed.FromInt(2048), Fixed.Zero);
            Check(sector.SpecialData == null && (int)line.Special == code && line.FrontSide.TopTexture == off,
                "Read-only aim activated a switch.");
            s.TraceWeapon(s.Body.Angle, Fixed.FromInt(2048), Fixed.Zero, s.Camera.ViewZ);
            Check(sector.SpecialData != null, "Impact failed to start tagged sector action: " + code);
            Check(line.FrontSide.TopTexture == on && (int)line.Special == (code == 46 ? 46 : 0),
                "Impact switch texture or repeat behavior differs: " + code);
            var mover = sector.SpecialData;
            s.TraceWeapon(s.Body.Angle, Fixed.FromInt(2048), Fixed.Zero, s.Camera.ViewZ);
            Check(ReferenceEquals(mover, sector.SpecialData), "Repeated shot replaced active mover.");
            if (code == 46)
            {
                // The synthetic shot precedes tick zero; process ticks zero through 35.
                for (var i = 0; i <= 35; i++) s.Tick(default);
                Check(line.FrontSide.TopTexture == off && (int)line.Special == 46, "Repeat impact switch did not reset.");
            }
        }
        var firing = new HereticWorldSession(content);
        var wand = new HereticGoldWand(firing);
        var wall = firing.TraceAim(firing.Body.Angle, Fixed.FromInt(2048), Fixed.Zero).Value.Line;
        wall.Special = (LineSpecial)24;
        wall.Tag = 31999; // No target: reference still consumes a one-shot switch.
        for (var i = 0; i < 19; i++) wand.Tick(true);
        Check(wand.ShotsFired == 1 && wall.Special == 0, "Actual wand fire did not activate one-shot line.");
        wall.Special = (LineSpecial)11;
        firing.TraceWeapon(firing.Body.Angle, Fixed.FromInt(2048), Fixed.Zero, firing.Camera.ViewZ);
        Check((int)wall.Special == 11 && !firing.ExitRequested, "Weapon activated a use-only exit.");
        Console.WriteLine("PASS Heretic shoot switches: floor/door/platform activation, read-only aim, reset, one-shot consumption and real wand fire");
    }
}
