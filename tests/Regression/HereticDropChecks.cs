// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticDropChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static void Touch(HereticWorldSession s, Mobj item)
    {
        s.World.ThingMovement.UnsetThingPosition(s.Body);
        s.Body.X = item.X; s.Body.Y = item.Y; s.Body.Z = item.Z;
        s.World.ThingMovement.SetThingPosition(s.Body); s.Body.FloorZ = item.FloorZ; s.Body.CeilingZ = item.CeilingZ;
        s.Tick(default);
    }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var s = new HereticWorldSession(content); var enemy = s.StartClinkTest();
        Check(enemy != null, "Drop encounter failed.");
        s.GoldWand.GiveSkullRodAmmo(200, false);
        s.DamageTestEnemy(enemy.Body, 10000, environment: true); s.World.Random.Clear();
        var events = 0; enemy.DropRequested += drop => events++;
        for (var i = 0; i < 100; i++) s.Tick(default);
        var drops = s.Actors.Where(a => (a.Body.Flags & MobjFlags.Dropped) != 0).ToArray();
        Check(events == 1 && drops.Length == 1 && drops[0].Type == HereticActorType.MT_AMSKRDWIMPY, "Clink death did not create exactly one collectible drop.");
        var item = drops[0];
        Check(item.Body.Info == null && item.Body.State == null && item.Body.Health == 20 && item.Body.Z == item.Body.FloorZ && item.Body.MomZ == Fixed.Zero,
            "Drop definition, amount or landing failed.");
        Touch(s, item.Body);
        Check(s.Actors.Contains(item) && s.GoldWand.SkullRodAmmo == 200, "Full ammo consumed drop.");
        s.GoldWand.GrantTestSkullRod(190);
        var pickedSound = false; s.SoundRequested += (id, source) => { if (id == HereticSoundId.sfx_itemup) pickedSound = true; };
        Touch(s, item.Body);
        Check(!s.Actors.Contains(item) && item.Animation.Removed && s.GoldWand.SkullRodAmmo == 200 && pickedSound, "Drop collection/cap/removal/sound failed.");
        var a = new HereticWorldSession(content); var b = new HereticWorldSession(content);
        a.StartClinkTest(); b.StartClinkTest();
        a.GoldWand.GiveSkullRodAmmo(200, false); b.GoldWand.GiveSkullRodAmmo(200, false);
        a.World.Random.Clear(); b.World.Random.Clear();
        a.SpawnClinkAmmoDrop(a.Body); b.SpawnClinkAmmoDrop(b.Body);
        Check(a.World.Random.Index == 6, "Dropped item RNG consumption differs from spawn plus velocity.");
        var da = a.Actors.Last(); var db = b.Actors.Last();
        var initialZ = da.Body.Z;
        for (var i = 0; i < 60; i++)
        {
            a.Tick(default); b.Tick(default);
            Check(da.Body.X == db.Body.X && da.Body.Y == db.Body.Y && da.Body.Z == db.Body.Z && a.World.Random.Index == b.World.Random.Index, "Drop replay diverged.");
            if (i == 0) Check(da.Body.Z > initialZ, "Drop failed initial upward toss.");
        }
        Check(!a.GoldWand.HasSkullRod, "Dropped ammo granted weapon ownership.");
        new HereticMapPreview(content, a).Render(new byte[320 * 200 * 4], 0);
        Console.WriteLine("PASS Clink ammo drops: actual death spawn, toss/landing, RNG/replay, full-cap retention, collection/removal and sound");
    }
}
