// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticBagChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static void Fill(HereticGoldWand w)
    {
        w.GiveAmmo(false, 1000, false); w.GiveAmmo(true, 1000, false);
        w.GiveCrossbowAmmo(1000, false); w.GiveSkullRodAmmo(1000, false);
        w.GivePhoenixAmmo(1000, false); w.GiveMaceAmmo(1000, false);
    }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var s = new HereticWorldSession(content); s.StartClinkTest(); var w = s.GoldWand;
        Check(!w.HasBagOfHolding, "New player starts with bag."); Fill(w);
        Check(w.Ammo == 100 && w.BlasterAmmo == 200 && w.CrossbowAmmo == 50 && w.SkullRodAmmo == 200 && w.PhoenixAmmo == 20 && w.MaceAmmo == 150, "Normal ammo caps changed.");
        Check(w.GiveBagOfHolding(false) && w.HasBagOfHolding, "Bag pickup rejected at old ammo caps.");
        Check(w.Ammo == 110 && w.BlasterAmmo == 210 && w.CrossbowAmmo == 55 && w.SkullRodAmmo == 220 && w.PhoenixAmmo == 21 && w.MaceAmmo == 150, "Bag grant amounts differ from reference.");
        Check(!w.HasBlaster && !w.HasCrossbow && !w.HasSkullRod && !w.HasPhoenix && !w.HasMace, "Bag granted weapon ownership.");
        Fill(w); Check(w.Ammo == 200 && w.BlasterAmmo == 400 && w.CrossbowAmmo == 100 && w.SkullRodAmmo == 400 && w.PhoenixAmmo == 40 && w.MaceAmmo == 300, "Doubled ammo caps failed.");
        Check(w.GiveBagOfHolding(false), "Reference consumes repeated bags even at full ammo."); Fill(w);
        Check(w.Ammo == 200 && w.BlasterAmmo == 400 && w.CrossbowAmmo == 100 && w.SkullRodAmmo == 400 && w.PhoenixAmmo == 40 && w.MaceAmmo == 300, "Repeated bag doubled capacities again.");
        s.DamageEnvironment(1000); Check(!w.GiveBagOfHolding(false), "Dead player collected bag.");
        foreach (var skill in new[] { GameSkill.Baby, GameSkill.Medium, GameSkill.Nightmare })
        {
            var p = new HereticWorldSession(content, skill: skill);
            var bit = skill == GameSkill.Baby ? 1 : skill == GameSkill.Medium ? 2 : 4;
            var thing = p.World.Map.Things.First(t => t.Type == 10 && ((int)t.Flags & bit) != 0 && ((int)t.Flags & 16) == 0); thing.Type = 8;
            p.StartClinkTest(); var bag = p.Actors.First(a => a.Type == HereticActorType.MT_MISC1);
            var sounds = 0; p.SoundRequested += (id, source) => { if (id == HereticSoundId.sfx_itemup) sounds++; };
            p.World.ThingMovement.UnsetThingPosition(p.Body);
            p.Body.X = bag.Body.X; p.Body.Y = bag.Body.Y; p.Body.Z = bag.Body.Z;
            p.World.ThingMovement.SetThingPosition(p.Body); p.Body.FloorZ = bag.Body.FloorZ; p.Body.CeilingZ = bag.Body.CeilingZ;
            p.Tick(default);
            var bonus = skill != GameSkill.Medium; var a = p.GoldWand;
            Check(a.HasBagOfHolding && !p.Actors.Contains(bag) && bag.Animation.Removed && sounds == 1 && p.State.PickupFlash > 0, "Bag map pickup/removal/feedback failed.");
            Check(a.Ammo == (bonus ? 65 : 60) && a.BlasterAmmo == (bonus ? 15 : 10) && a.CrossbowAmmo == (bonus ? 7 : 5) && a.SkullRodAmmo == (bonus ? 30 : 20) && a.PhoenixAmmo == 1 && a.MaceAmmo == 0, "Bag difficulty bonus/rounding or mace exclusion failed.");
        }
        Console.WriteLine("PASS Bag of Holding: map collection/audio, all six caps, repeated bags, five ammo grants, difficulty rounding, ownership and death gating");
    }
}
