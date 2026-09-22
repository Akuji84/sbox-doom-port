// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticMaceWeaponChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static HereticWorldSession Ready(GameContent content, int ammo)
    {
        var s = new HereticWorldSession(content); Check(s.StartClinkTest() != null, "Mace encounter fixture failed.");
        Check(!s.GoldWand.SelectWeapon(HereticWeapon.wp_mace), "Unowned mace selected.");
        s.GoldWand.GrantTestMace(ammo); s.GoldWand.SelectWeapon(HereticWeapon.wp_mace);
        for (var i = 0; i < 60; i++) s.GoldWand.Tick(false);
        return s;
    }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        foreach (var lob in new[] { false, true })
        {
            var s = Ready(content, 1); var w = s.GoldWand;
            w.Tick(true); for (var i = 0; i < 3; i++) w.Tick(false);
            Check(w.MaceShots == 0, "Mace skipped windup.");
            var seed = Enumerable.Range(0, 256).First(i => (new DoomRandom(i).Next() < 28) == lob);
            s.World.Random.Index = seed;
            var sounded = false; s.SoundRequested += (id, source) => { if (id == HereticSoundId.sfx_lobsht) sounded = true; };
            w.Tick(false);
            Check(w.MaceShots == 1 && w.MaceAmmo == 0 && s.Projectiles.Count == 1 && sounded, "Mace shot/ammo/sound failed.");
            var bolt = s.Projectiles[0];
            Check(bolt.Type == (lob ? HereticActorType.MT_MACEFX2 : HereticActorType.MT_MACEFX1), "Mace random branch differs.");
            Check(bolt.Body.Target == s.Body && (lob ? bolt.LowGravity : bolt.DropTics == 16), "Mace launch configuration differs.");
            if (lob) Check(bolt.Body.MomZ == Fixed.FromInt(2), "Lobbed mace initial upward momentum differs.");
            for (var i = 0; i < 90; i++) w.Tick(false);
            Check(w.MaceShots == 1 && w.MaceAmmo == 0 && w.ReadyWeapon == HereticWeapon.wp_goldwand, "Mace last-ammo/fallback failed.");
        }
        var cadence = Ready(content, 50); var cw = cadence.GoldWand;
        cw.Tick(true);
        for (var i = 1; i <= 13; i++)
        {
            cw.Tick(false);
            Check(cw.MaceShots == (i < 4 ? 0 : 1 + (i - 4) / 3), "Mace three-tick burst/release cadence differs.");
        }
        var a = Ready(content, 50); var b = Ready(content, 50);
        for (var i = 0; i < 140; i++)
        {
            a.Tick(new HereticCommand { TestAttack = true }); b.Tick(new HereticCommand { TestAttack = true });
            Check(a.World.Random.Index == b.World.Random.Index && a.Projectiles.Count == b.Projectiles.Count && a.GoldWand.MaceAmmo == b.GoldWand.MaceAmmo, "Mace replay RNG/ammo diverged.");
            for (var j = 0; j < a.Projectiles.Count; j++)
            {
                var x = a.Projectiles[j]; var y = b.Projectiles[j];
                Check(x.Type == y.Type && x.Body.X == y.Body.X && x.Body.Y == y.Body.Y && x.Body.Z == y.Body.Z && x.Animation.State == y.Animation.State, "Mace replay motion diverged.");
            }
        }
        foreach (var type in new[] { HereticActorType.MT_WMACE, HereticActorType.MT_AMMACEWIMPY, HereticActorType.MT_AMMACEHEFTY })
        {
            var s = new HereticWorldSession(content);
            var thing = s.World.Map.Things.First(t => t.Type == 10 && ((int)t.Flags & 2) != 0 && ((int)t.Flags & 16) == 0);
            thing.Type = HereticDefinitions.Actors[(int)type].MapNumber; s.StartClinkTest();
            var pickup = s.Actors.First(p => p.Type == type);
            s.World.ThingMovement.UnsetThingPosition(s.Body);
            s.Body.X = pickup.Body.X; s.Body.Y = pickup.Body.Y; s.Body.Z = pickup.Body.Z;
            s.World.ThingMovement.SetThingPosition(s.Body); s.Body.FloorZ = pickup.Body.FloorZ; s.Body.CeilingZ = pickup.Body.CeilingZ;
            s.Tick(default);
            var weapon = type == HereticActorType.MT_WMACE;
            Check(pickup.Animation.Removed && s.GoldWand.HasMace == weapon && s.GoldWand.MaceAmmo == (weapon ? 50 : type == HereticActorType.MT_AMMACEWIMPY ? 20 : 100), "Mace map pickup failed.");
        }
        var rank = Ready(content, 50); var r = rank.GoldWand;
        r.GivePhoenix(false); r.GiveSkullRod(false); r.GiveBlaster(false); r.GiveCrossbow(false);
        Check(r.PendingWeapon == null, "Lower-ranked pickup replaced mace.");
        r.GiveMace(true); Check(r.MaceAmmo == 125, "Mace weapon difficulty bonus differs.");
        r.GiveMaceAmmo(100, false);
        Check(r.MaceAmmo == 150 && !r.GiveMace(false), "Mace ammo cap/duplicate handling failed.");
        Console.WriteLine("PASS normal Firemace weapon: ownership, both shot branches, windup/burst, ammo/fallback, deterministic replay, map pickups and ranking");
    }
}
