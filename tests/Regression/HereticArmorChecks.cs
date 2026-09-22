// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticArmorChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        foreach (var type in new[] { 1, 2 })
        {
            var s = new HereticWorldSession(content);
            var thing = s.World.Map.Things.First(t => t.Type == 10 && ((int)t.Flags & 2) != 0 && ((int)t.Flags & 16) == 0);
            thing.Type = (short)(type == 1 ? 85 : 31);
            Check(s.StartClinkTest() != null, "Shield encounter fixture failed.");
            var shield = s.Actors.First(a => a.Type == (type == 1 ? HereticActorType.MT_ITEMSHIELD1 : HereticActorType.MT_ITEMSHIELD2));
            s.World.ThingMovement.UnsetThingPosition(s.Body);
            s.Body.X = shield.Body.X; s.Body.Y = shield.Body.Y; s.Body.Z = shield.Body.Z;
            s.World.ThingMovement.SetThingPosition(s.Body);
            s.Body.FloorZ = shield.Body.FloorZ; s.Body.CeilingZ = shield.Body.CeilingZ;
            s.Tick(default);
            Check(s.State.ArmorType == type && s.State.ArmorPoints == type * 100 && shield.Animation.Removed,
                "Map shield failed to grant armor/remove pickup.");
            Check(!s.GiveArmor(type), "Full armor accepted duplicate.");
            if (type == 2) Check(!s.GiveArmor(1), "Weaker shield replaced full enchanted shield.");
            s.DamageEnvironment(7);
            var saved = type == 1 ? 3 : 4;
            Check(s.State.Health == 100 - 7 + saved && s.Body.Health == s.State.Health && s.State.ArmorPoints == type * 100 - saved,
                "Heretic armor absorption/rounding differs.");
            s.State.ArmorPoints = 2;
            var health = s.State.Health;
            s.DamageEnvironment(10);
            Check(s.State.Health == health - 8 && s.State.ArmorPoints == 0 && s.State.ArmorType == 0,
                "Armor depletion did not clear type or leaked damage.");
            s.GiveArmor(2); s.State.ArmorPoints = 50;
            Check(s.GiveArmor(1) && s.State.ArmorType == 1 && s.State.ArmorPoints == 100, "Low enchanted armor replacement differs.");
            s.DamageEnvironment(10000);
            Check(s.State.Health == 0 && !s.GiveArmor(2), "Lethal damage or dead armor guard failed.");
        }
        Console.WriteLine("PASS Heretic shields: map pickups, caps/replacement, absorption rounding, depletion, health synchronization and death");
    }
}
