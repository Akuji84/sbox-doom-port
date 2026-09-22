// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticDamageChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    // Test action spy only: intentionally does not pretend to implement enemy AI/audio/drops.
    sealed class Actions : IHereticActorActions
    {
        public readonly List<HereticAction> Calls = new();
        public HereticAction? Missing;
        public Action<HereticAction> OnAction;
        public bool Supports(HereticAction action) => action != Missing;
        public void Execute(HereticAction action, HereticActorState state) { Calls.Add(action); OnAction?.Invoke(action); }
    }
    public static void Verify(string root)
    {
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        var session = new HereticWorldSession(content);
        var world = session.World;
        var random = world.Random;
        var spy = new Actions();
        var beast = new HereticCombatant(world, HereticActorType.MT_BEAST, spy);
        Check(beast.Body.Info == null && beast.Body.State == null && spy.Calls.Count == 0, "Combatant invoked Doom/spawn actions.");
        beast.Body.X = Fixed.FromInt(64);
        var source = new Mobj(world);
        random.Clear();
        var result = beast.ApplyOrdinaryDamage(20, source, source);
        Check(result == HereticDamageResult.Pain && beast.Body.Health == 200 && beast.Animation.State == HereticStateId.S_BEAST_PAIN1, "Nonlethal damage/pain differs from reference.");
        Check(beast.Body.MomX > Fixed.Zero && Fixed.Abs(beast.Body.MomY) < Fixed.One / 100, "Thrust points toward inflictor.");
        Check(beast.Body.ReactionTime == 0 && beast.Body.Target == source && beast.Body.Threshold == 100, "Damage did not wake/retarget monster.");
        var target = new Mobj(world); beast.Body.Target = target;
        beast.ApplyOrdinaryDamage(1, source, source, HereticDamageThrust.None);
        Check(beast.Body.Target == target, "Damage replaced an established target threshold.");
        var suppressed = new HereticCombatant(world, HereticActorType.MT_BEAST, new Actions());
        suppressed.ApplyOrdinaryDamage(1, source, source, HereticDamageThrust.None, sourceIsBoss: true);
        Check(suppressed.Body.MomX == Fixed.Zero && suppressed.Body.MomZ == Fixed.Zero && suppressed.Body.Target == null, "Suppressed thrust/boss-source retarget ignored.");
        var staff = new HereticCombatant(world, HereticActorType.MT_BEAST, new Actions()); staff.Body.X = Fixed.FromInt(64);
        staff.ApplyOrdinaryDamage(1, source, source, HereticDamageThrust.PoweredStaff);
        Check(Fixed.Abs(staff.Body.MomX - Fixed.FromInt(10)) < Fixed.One / 100 && staff.Body.MomZ == Fixed.FromInt(5), "Powered staff impulse differs.");
        foreach (var (damage, expected) in new[] { (220, HereticStateId.S_BEAST_DIE1), (330, HereticStateId.S_BEAST_DIE1), (331, HereticStateId.S_BEAST_XDIE1) })
        {
            spy = new Actions(); var actor = new HereticCombatant(world, HereticActorType.MT_BEAST, spy);
            var height = actor.Body.Height;
            spy.OnAction = action => { if (action == HereticAction.A_NoBlocking) actor.Body.Flags &= ~MobjFlags.Solid; };
            random.Clear();
            Check(actor.ApplyOrdinaryDamage(damage, source: source) == HereticDamageResult.Killed && actor.Animation.State == expected, "Normal/extreme death threshold differs.");
            Check(actor.Body.Health == 220 - damage && actor.Body.Height == height / 4 && actor.Killer == source, "Death lost overkill, corpse height or kill attribution.");
            Check((actor.Body.Flags & MobjFlags.Shootable) == 0 && (actor.Body.Flags & MobjFlags.Corpse) != 0 && (actor.Flags2 & HereticActorFlags2.MF2_PASSMOBJ) == 0, "Corpse flags differ.");
            var index = random.Index;
            Check(actor.ApplyOrdinaryDamage(100) == HereticDamageResult.Ignored && random.Index == index && actor.Body.Height == height / 4, "Repeat kill changed actor/randomness.");
            for (var i = 0; i < 100; i++) actor.TickState();
            Check(spy.Calls.Contains(HereticAction.A_Scream) && spy.Calls.Count(a => a == HereticAction.A_NoBlocking) == 1 && (actor.Body.Flags & MobjFlags.Solid) == 0, "Death action dispatch/corpse completion failed.");
        }
        var knight = new HereticCombatant(world, HereticActorType.MT_KNIGHT, new Actions());
        knight.ApplyOrdinaryDamage(int.MaxValue);
        Check(knight.Animation.State == HereticStateId.S_KNIGHT_DIE1 && knight.Body.Health < 0, "Actor without extreme state failed ordinary death fallback.");
        var indexBefore = random.Index;
        try { _ = new HereticCombatant(world, HereticActorType.MT_BEAST, new Actions { Missing = HereticAction.A_NoBlocking }); throw new Exception("Missing later death action accepted."); }
        catch (NotSupportedException) { }
        Check(random.Index == indexBefore, "Rejected actor consumed randomness.");
        try { _ = new HereticCombatant(world, HereticActorType.MT_MINOTAUR, new Actions()); throw new Exception("Unimplemented boss damage path accepted."); }
        catch (ArgumentException) { }
        var unchanged = beast.Body.Health;
        try { beast.ApplyOrdinaryDamage(-1); throw new Exception("Negative damage accepted."); }
        catch (ArgumentOutOfRangeException) { }
        Check(beast.Body.Health == unchanged, "Rejected damage changed health.");
        var another = new HereticWorldSession(content);
        try { beast.ApplyOrdinaryDamage(1, another.Body); throw new Exception("Foreign-world damage accepted."); }
        catch (ArgumentException) { }
        Check(!HereticMapSpawns.Supports(HereticActorType.MT_BEAST), "Damage foundation accidentally enabled unfinished map enemies.");
        Console.WriteLine("PASS Heretic ordinary actor damage: health, pain, thrust, target thresholds, corpse flags, normal/extreme death, action dispatch and unsupported-path guards");
    }
}
