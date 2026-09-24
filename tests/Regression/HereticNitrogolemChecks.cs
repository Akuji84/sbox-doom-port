// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticNitrogolemChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        foreach (var type in new[] { HereticActorType.MT_MUMMYLEADER, HereticActorType.MT_MUMMYLEADERGHOST })
        {
            var s = new HereticWorldSession(content); var enemy = s.StartEnemyTest(type);
            Check(enemy != null && enemy.Combatant.Type == type, "Nitrogolem spawn failed.");
            enemy.Body.Target = s.Body; enemy.Body.ReactionTime = 0;
            s.World.Random.Clear(); s.World.Random.Next();
            enemy.Execute(HereticAction.A_Chase, enemy.Combatant.Animation);
            Check(enemy.Combatant.Animation.State == HereticDefinitions.Actors[(int)type].MissileState, "Ranged chase did not select missile attack.");
            var sawMissile = false;
            for (var i = 0; i < 100 && s.State.Health == 100; i++)
            {
                s.Tick(default); sawMissile |= s.Projectiles.Any(p => p.Type == HereticActorType.MT_MUMMYFX1);
            }
            Check(sawMissile && s.State.Health < 100, "Native ranged encounter did not fire/hit player.");
            Check(enemy.MorphToChicken() && enemy.UpdateChicken(2000) && enemy.Combatant.Type == type, "Nitrogolem morph restored wrong variant.");
            s.DamageTestEnemy(enemy.Body, 10000);
            for (var i = 0; i < 200; i++) s.Tick(default);
            Check(s.Projectiles.Count == 0 && s.TestKills == 1, "Missiles/death did not clean up.");
        }
        var session = new HereticWorldSession(content); var shooter = session.StartEnemyTest(HereticActorType.MT_MUMMYLEADER);
        var missile = session.SpawnNitrogolemMissile(shooter);
        Check(missile.Flying && missile.Body.Target == shooter.Body && missile.SeekerTarget == session.Body, "Missile owner/target incorrect.");
        var random = session.World.Random.Index;
        Check(missile.Contact(shooter.Body) && random == session.World.Random.Index, "Missile damaged owner or consumed hit RNG.");
        missile.Body.Z = session.Body.Z + Fixed.FromInt(20);
        session.State.ArmorType = 1; session.State.ArmorPoints = 100;
        session.World.Random.Clear();
        Check(!missile.Contact(session.Body) && session.State.Health == 98 && session.State.ArmorPoints == 98, "Player missile/armor damage incorrect.");
        Check(session.Body.MomX != Fixed.Zero || session.Body.MomY != Fixed.Zero, "Missile thrust missing.");
        session.State.InvulnerabilityTics = 100; var health = session.State.Health; var armor = session.State.ArmorPoints;
        missile.Contact(session.Body);
        Check(session.State.Health == health && session.State.ArmorPoints == armor, "Missile bypassed invulnerability.");
        var peer = session.StartEnemyTest(HereticActorType.MT_MUMMYLEADER);
        Check(peer != null, "Same-species target fixture failed.");
        missile.Body.Z = peer.Body.Z; random = session.World.Random.Index;
        Check(!missile.Contact(peer.Body) && peer.Body.Health == 100 && session.World.Random.Index == random, "Same-species collision damage/RNG incorrect.");
        // Exercise the actual Nitrogolem seek action and its tighter turn cap.
        missile.Body.Angle = new Angle(0); missile.Body.X = Fixed.Zero; missile.Body.Y = Fixed.Zero;
        var target = new Mobj(session.World) { X = Fixed.Zero, Y = Fixed.FromInt(160), Height = Fixed.FromInt(56), Flags = MobjFlags.Shootable };
        missile.SeekerTarget = target;
        missile.Execute(HereticAction.A_MummyFX1Seek, missile.Animation);
        Check(missile.Body.Angle.Data == 20u * 0x01000000u, "Nitrogolem seeker used wrong turn limit.");
        target.Flags = 0; missile.Execute(HereticAction.A_MummyFX1Seek, missile.Animation);
        Check(missile.SeekerTarget == null, "Dead target retained by skull.");
        var sounds = new List<HereticSoundId>(); session.SoundRequested += (sound, _) => sounds.Add(sound);
        missile.Execute(HereticAction.A_ContMobjSound, missile.Animation);
        Check(sounds.Contains(HereticSoundId.sfx_mumhed), "Skull flight sound missing.");
        var baby = new HereticWorldSession(content, 1, 1, GameSkill.Baby); var babyEnemy = baby.StartEnemyTest(HereticActorType.MT_MUMMYLEADER);
        var babyMissile = baby.SpawnNitrogolemMissile(babyEnemy); babyMissile.Body.Z = baby.Body.Z;
        baby.World.Random.Clear(); babyMissile.Contact(baby.Body);
        Check(baby.State.Health == 98, "Baby missile damage was not halved exactly once.");
        Console.WriteLine("PASS Nitrogolems: normal/ghost ranged attacks, player hit/armor/Baby/invulnerability/thrust, owner/species exclusion, homing/sound, morph and cleanup");
    }
}
