// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticPoweredCrossbowChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var s = new HereticWorldSession(content); var enemy = s.StartClinkTest(); var w = s.GoldWand;
        w.GrantTestPoweredCrossbow(1); w.SelectWeapon(HereticWeapon.wp_crossbow);
        for (var i = 0; i < 60; i++) w.Tick(false);
        s.Body.Angle = Geometry.PointToAngle(s.Body.X, s.Body.Y, enemy.Body.X, enemy.Body.Y);
        w.Tick(true);
        Check(w.CrossbowShots == 1 && w.CrossbowAmmo == 0 && s.Projectiles.Count == 5, "Powered crossbow volley/ammo differs.");
        Check(s.Projectiles.Count(p => p.Type == HereticActorType.MT_CRBOWFX2) == 3 && s.Projectiles.Count(p => p.Type == HereticActorType.MT_CRBOWFX3) == 2, "Powered bolt mix differs.");
        foreach (var bolt in s.Projectiles) Check(bolt.Contact(s.Body) && bolt.Body.Info == null && bolt.Body.State == null, "Powered bolt owner/definition isolation failed.");
        var health = enemy.Body.Health;
        for (var i = 0; i < 70; i++) s.Tick(default);
        Check(enemy.Body.Health < health && s.Projectiles.Count == 0, "Powered bolt damage/cleanup failed.");
        Check(w.ReadyWeapon == HereticWeapon.wp_goldwand && w.CrossbowShots == 1, "Empty powered crossbow refired or failed fallback.");
        var sparkSession = new HereticWorldSession(content); var origin = sparkSession.Body;
        origin.Z += Fixed.FromInt(48);
        for (var i = 0; i < 20; i++) sparkSession.SpawnCrossbowSpark(origin);
        Check(sparkSession.ImpactEffects.Count > 0, "Powered bolt sparks never spawned.");
        var spark = sparkSession.ImpactEffects.First(); var z = spark.Body.Z;
        Check(spark.Type == HereticActorType.MT_CRBOWFX4 && spark.Body.Info == null && spark.Body.State == null, "Spark type/isolation failed.");
        sparkSession.Tick(default); sparkSession.Tick(default);
        Check(spark.Body.Z < z, "Spark did not fall with low gravity.");
        for (var i = 0; i < 80; i++) sparkSession.Tick(default);
        Check(sparkSession.ImpactEffects.Count == 0 && spark.Animation.Removed, "Spark effects failed cleanup.");
        var a = new HereticWorldSession(content); var b = new HereticWorldSession(content);
        for (var i = 0; i < 10; i++) { a.SpawnCrossbowSpark(a.Body); b.SpawnCrossbowSpark(b.Body); }
        Check(a.World.Random.Index == b.World.Random.Index && a.ImpactEffects.Select(e => (e.Body.X.Data, e.Body.Y.Data)).SequenceEqual(b.ImpactEffects.Select(e => (e.Body.X.Data, e.Body.Y.Data))), "Spark replay/randomness differs.");
        Console.WriteLine("PASS powered crossbow: five-bolt volley, ammo/fallback, owner exclusion, damage, spark gravity/cleanup and deterministic replay");
    }
}
