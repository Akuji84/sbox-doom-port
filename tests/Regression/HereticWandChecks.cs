// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticWandChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var s = new HereticWorldSession(content); var wand = new HereticGoldWand(s);
        var shots = new List<(int tic, HereticWandShot shot)>(); var now = 0;
        wand.ShotFired += shot => shots.Add((now, shot));
        for (now = 1; now <= 50; now++) wand.Tick(true);
        Check(shots.Count == 3 && shots[0].tic == 18 && shots[1].tic == 29 && shots[2].tic == 40, "Gold Wand raise/attack/refire timing differs.");
        Check(wand.Ammo == 47 && shots.All(x => x.shot.Damage >= 7 && x.shot.Damage <= 14), "Gold Wand ammo or damage differs.");
        Check(shots[0].shot.Angle == s.Body.Angle && wand.Refire > 0, "First shot spread/refire state wrong.");
        for (now = 51; now <= 70; now++) wand.Tick(false);
        var count = shots.Count;
        Check(wand.Refire == 0 && wand.State == HereticStateId.S_GOLDWANDREADY, "Release did not return wand to ready state.");
        wand.Ammo = 0;
        for (var i = 0; i < 30; i++) wand.Tick(true);
        Check(shots.Count == count && wand.Ammo == 0, "Empty wand fired or ammo went negative.");
        for (var i = 0; i < 60; i++) wand.Tick(true);
        Check(wand.ReadyWeapon == HereticWeapon.wp_staff && wand.StaffSwings > 0, "Empty wand did not switch to usable staff.");
        Check(!wand.SelectWeapon(HereticWeapon.wp_goldwand), "Selected empty wand.");
        Check(!wand.SelectWeapon(HereticWeapon.wp_blaster), "Selected unsupported weapon.");
        wand.Ammo = 1;
        Check(wand.SelectWeapon(HereticWeapon.wp_goldwand), "Could not switch back to loaded wand.");
        for (var i = 0; i < 90; i++) wand.Tick(true);
        Check(shots.Count == count + 1 && wand.Ammo == 0, "Last round did not fire exactly once.");
        s.DamageEnvironment(100);
        for (var i = 0; i < 30; i++) wand.Tick(true);
        Check(!wand.Visible && shots.Count == count + 1, "Dead player fired or weapon failed to lower.");
        var melee = new HereticWorldSession(content);
        var enemy = melee.StartClinkTest();
        Check(enemy != null, "Staff target fixture failed.");
        melee.GoldWand.SelectWeapon(HereticWeapon.wp_staff);
        for (var i = 0; i < 100 && melee.State.Health == 100; i++) melee.Tick(default);
        Check(melee.State.Health < 100, "Staff target did not approach melee range.");
        for (var i = 0; i < 40; i++) melee.GoldWand.Tick(false);
        Check(melee.GoldWand.State == HereticStateId.S_STAFFREADY, "Staff failed to finish raising.");
        melee.Body.Angle = Geometry.PointToAngle(melee.Body.X, melee.Body.Y, enemy.Body.X, enemy.Body.Y);
        var hp = enemy.Body.Health;
        for (var i = 0; i < 7; i++) melee.GoldWand.Tick(true);
        Check(melee.GoldWand.StaffSwings == 1 && hp - enemy.Body.Health >= 5 && hp - enemy.Body.Health <= 20,
            $"Staff failed to hit linked melee target: swings={melee.GoldWand.StaffSwings}, damage={hp - enemy.Body.Health}, state={melee.GoldWand.State}.");
        Check(melee.ImpactEffects.Any(x => x.Type == HereticActorType.MT_STAFFPUFF), "Staff attack did not spawn an impact.");
        Check(melee.GoldWand.Ammo == 50, "Staff consumed wand ammo.");
        var stepper = new HereticFrameStepper(); var selections = new List<HereticWeapon?>();
        stepper.Advance(0.001, new HereticCommand { SelectWeapon = HereticWeapon.wp_staff }, c => selections.Add(c.SelectWeapon));
        stepper.Advance(0.06, default, c => selections.Add(c.SelectWeapon));
        Check(selections.Count == 2 && selections[0] == HereticWeapon.wp_staff && selections[1] == null,
            "Short weapon selection was lost or repeated across ticks.");
        var renderSession = new HereticWorldSession(content);
        Check(renderSession.StartClinkTest() != null, "Wand rendering fixture spawn failed.");
        var preview = new HereticMapPreview(content, renderSession);
        var pixels = new byte[320 * 200 * 4];
        preview.Render(pixels, 0); var lowered = pixels.ToArray();
        for (var i = 0; i < 16; i++) renderSession.GoldWand.Tick(false);
        preview.Render(pixels, 0); var ready = pixels.ToArray();
        Check(!ready.SequenceEqual(lowered), "Raised Gold Wand not visible in shared preview.");
        for (var i = 0; i < 4; i++) renderSession.GoldWand.Tick(true);
        preview.Render(pixels, 0);
        Check(!pixels.SequenceEqual(ready), "Firing frame did not change weapon rendering.");
        var output = Environment.GetEnvironmentVariable("HERETIC_WAND_RGBA");
        if (!string.IsNullOrEmpty(output)) File.WriteAllBytes(output, pixels);
        var screen = new ManagedDoom.Video.DrawScreen(content.Wad, 320, 200);
        var lighting = new ManagedDoom.Video.ThreeDRenderer(content, screen, 8);
        lighting.Render(renderSession.Camera, Fixed.One);
        var darkMap = lighting.GetWeaponColorMap(0, false);
        var brightMap = lighting.GetWeaponColorMap(255, false);
        Check(!darkMap.SequenceEqual(brightMap), "Weapon sector light has no effect.");
        Check(lighting.GetWeaponColorMap(0, true).SequenceEqual(content.ColorMap.FullBright), "Full-bright weapon frame darkened.");
        var patch = content.Sprites[(Sprite)HereticSpriteId.SPR_GWND].Frames[0].Patches[0];
        foreach (var flip in new[] { false, true })
        {
            Array.Fill(screen.Data, (byte)247);
            if (flip) screen.DrawPatchFlip(patch, 1, 32, 1); else screen.DrawPatch(patch, 1, 32, 1);
            var plain = screen.Data.ToArray();
            Array.Fill(screen.Data, (byte)247);
            if (flip) screen.DrawPatchFlip(patch, 1, 32, 1, darkMap); else screen.DrawPatch(patch, 1, 32, 1, darkMap);
            Check(!plain.SequenceEqual(screen.Data), "Weapon patch ignored palette mapping.");
            for (var i = 0; i < plain.Length; i++)
                if (plain[i] != 247) Check(screen.Data[i] == darkMap[plain[i]], "Weapon patch mapped a pixel incorrectly.");
            Check(screen.Data[0] == 247, "Weapon overlay overwrote transparent background.");
        }
        Console.WriteLine("PASS Heretic weapon lighting: sector darkness, full-bright override, mapped normal/flipped patches and transparency");
        var clawSession = new HereticWorldSession(content);
        var claw = new HereticGoldWand(clawSession);
        Check(!claw.SelectWeapon(HereticWeapon.wp_blaster), "Ungiven Dragon Claw selectable.");
        claw.GrantTestBlaster(3); claw.SelectWeapon(HereticWeapon.wp_blaster);
        for (var i = 0; i < 60; i++) claw.Tick(false);
        Check(claw.State == HereticStateId.S_BLASTERREADY, "Dragon Claw failed to raise.");
        var clawShots = new List<int>(); var clawDamages = new List<int>(); var clawTick = 0;
        claw.BlasterShotFired += shot => { clawShots.Add(clawTick); clawDamages.Add(shot.Damage); };
        for (clawTick = 0; clawTick < 90; clawTick++) claw.Tick(true);
        Check(clawShots.Count == 3 && clawShots[1] - clawShots[0] == 6 && clawShots[2] - clawShots[1] == 6,
            "Dragon Claw hold-attack cadence differs.");
        Check(clawDamages.All(x => x >= 4 && x <= 32 && x % 4 == 0) && claw.BlasterAmmo == 0,
            "Dragon Claw damage/ammo differs.");
        Check(claw.ReadyWeapon == HereticWeapon.wp_goldwand, "Empty Dragon Claw did not fall back to wand.");
        var impact = new HereticTraceHit(clawSession.Body, null, Fixed.FromInt(40), clawSession.Camera.ViewZ);
        clawSession.SpawnWeaponImpact(impact, clawSession.Body.Angle, Fixed.Zero, HereticWeapon.wp_blaster);
        Check(clawSession.ImpactEffects.Any(x => x.Type == HereticActorType.MT_BLASTERPUFF2), "Claw actor impact missing.");
        clawSession.SpawnWeaponImpact(new HereticTraceHit(null, null, Fixed.FromInt(40), clawSession.Camera.ViewZ),
            clawSession.Body.Angle, Fixed.Zero, HereticWeapon.wp_blaster);
        Check(clawSession.ImpactEffects.Any(x => x.Type == HereticActorType.MT_BLASTERPUFF1), "Claw wall impact missing.");
        Console.WriteLine("PASS normal Dragon Claw: explicit grant, raise, held cadence, damage/ammo, fallback and distinct impacts");
        foreach (var reserve in new[] { 1, 2 })
        {
            var fallbackSession = new HereticWorldSession(content);
            var fallback = new HereticGoldWand(fallbackSession);
            fallback.GrantTestBlaster(reserve); fallback.Ammo = 0;
            for (var i = 0; i < 17; i++) fallback.Tick(false);
            fallback.Tick(true);
            for (var i = 0; i < 40; i++) fallback.Tick(false);
            Check(fallback.ReadyWeapon == (reserve == 2 ? HereticWeapon.wp_blaster : HereticWeapon.wp_staff),
                "Automatic fallback violated the reference reserve threshold.");
            if (reserve == 1) Check(fallback.SelectWeapon(HereticWeapon.wp_blaster), "Manual selection rejected the last claw round.");
        }
        var ghostSession = new HereticWorldSession(content);
        var ghost = ghostSession.StartClinkTest(); Check(ghost != null, "Ghost trace fixture failed.");
        ghost.Body.Flags |= MobjFlags.Shadow;
        var ghostAngle = Geometry.PointToAngle(ghostSession.Body.X, ghostSession.Body.Y, ghost.Body.X, ghost.Body.Y);
        var ghostZ = ghost.Body.Z + ghost.Body.Height / 2;
        var ordinary = ghostSession.TraceWeapon(ghostAngle, Fixed.FromInt(2048), Fixed.Zero, ghostZ);
        var physical = ghostSession.TraceWeapon(ghostAngle, Fixed.FromInt(2048), Fixed.Zero, ghostZ, physicalStaff: true);
        Check(ordinary?.Actor == ghost.Body && physical?.Actor != ghost.Body, "Staff ghost immunity incorrectly affects ranged attacks.");
        Check(ghostSession.TraceAim(ghostAngle, Fixed.FromInt(2048), Fixed.Zero, ghostZ, true)?.Actor == ghost.Body,
            "Ghost filter leaked into general aiming.");
        Console.WriteLine("PASS Heretic weapon edge cases: reference fallback priority/reserve, manual last-round selection and staff ghost pass-through");
        var look = new HereticWorldSession(content); look.State.LookDirection = 45;
        var aimed = new HereticGoldWand(look); HereticWandShot? shotResult = null;
        aimed.ShotFired += shot => shotResult = shot;
        for (var i = 0; i < 19; i++) aimed.Tick(true);
        Check(shotResult?.Slope == Fixed.FromInt(45) / 173, "No-target wand aim ignored view pitch.");
        Console.WriteLine("PASS normal Gold Wand: reference cadence, 7-14 damage, ammo, refire/release, empty weapon, death lowering, pitch fallback and sprite overlay");
    }
}
