// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using ManagedDoom;
static class HereticActorChecks
{
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    sealed class Actions : IHereticActorActions
    {
        public readonly List<HereticAction> Calls = new();
        public Action<HereticActorState> OnAction;
        public bool Supports(HereticAction action) => action == HereticAction.A_Look;
        public void Execute(HereticAction action, HereticActorState actor) { Calls.Add(action); OnAction?.Invoke(actor); }
    }
    public static void Verify(string root)
    {
        Check(HereticDefinitions.States.Count == 1208 && HereticDefinitions.Actors.Count == 161, "Incomplete Heretic definitions.");
        Check(HereticDefinitions.States.Count == (int)HereticStateId.NUMSTATES && HereticDefinitions.Actors.Count == (int)HereticActorType.NUMMOBJTYPES, "Heretic enum/table mismatch.");
        using var content = GameContent.CreateHereticPreview(Path.Combine(root, "Assets/doom/blasphem.wad"));
        for (var i = 0; i < (int)HereticSpriteId.NUMSPRITES; i++)
            Check(((HereticSpriteId)i).ToString() == "SPR_" + HereticAssets.SpriteNames[i], "Heretic sprite order changed.");
        foreach (var state in HereticDefinitions.States)
        {
            Check((uint)state.Next < HereticDefinitions.States.Count && state.Tics >= -1, "Bad Heretic state edge/timing.");
            var sprite = content.Sprites[(Sprite)state.Sprite];
            Check((state.Frame & 0x7fff) < sprite.Frames.Length, "Missing Blasphemer frame for " + state.Sprite);
            Check(Enum.IsDefined(state.Action), "Unknown Heretic action.");
        }
        var numbers = new HashSet<int>();
        foreach (var actor in HereticDefinitions.Actors)
        {
            Check(actor.Radius >= Fixed.Zero && actor.Height >= Fixed.Zero, "Invalid actor dimensions.");
            if (actor.MapNumber >= 0) Check(numbers.Add(actor.MapNumber), "Ambiguous Heretic map thing number.");
            foreach (var state in new[] { actor.SpawnState, actor.SeeState, actor.PainState, actor.MeleeState, actor.MissileState, actor.CrashState, actor.DeathState, actor.ExtremeDeathState })
                Check((uint)state < HereticDefinitions.States.Count, "Bad actor state reference.");
            foreach (var sound in new[] { actor.SeeSound, actor.AttackSound, actor.PainSound, actor.DeathSound, actor.ActiveSound })
                Check((uint)sound < (uint)HereticSoundId.NUMSFX, "Bad actor sound reference.");
        }
        // Check distinctive upstream values, not just a self-consistent generated graph.
        var minotaur = HereticDefinitions.Actors[(int)HereticActorType.MT_MINOTAUR];
        Check(minotaur.MapNumber == 9 && minotaur.SpawnHealth == 3000 && (minotaur.Flags2 & HereticActorFlags2.MF2_BOSS) != 0, "Minotaur definition changed.");
        var imp = HereticDefinitions.Actors[(int)HereticActorType.MT_IMP];
        Check(imp.MapNumber == 66 && imp.SpawnHealth == 40 && imp.SpawnState == HereticStateId.S_IMP_LOOK1, "Imp definition changed.");
        CheckWeapons();
        try { ((IList<HereticStateDefinition>)HereticDefinitions.States)[1] = default; throw new Exception("Mutable Heretic definitions."); }
        catch (NotSupportedException) { }
        CheckStateTiming();
        CheckKeyIntegration(content);
        Console.WriteLine("PASS Heretic actor foundation: 1208 states, 161 actors, 18 weapon variants, Blasphemer frame coverage, action isolation, state timing/removal and animated map keys");
    }
    static void CheckWeapons()
    {
        var expectedNormal = new[] { 0, 1, 1, 1, 1, 1, 1, 0, 0 };
        var expectedPowered = new[] { 0, 1, 1, 5, 5, 1, 5, 0, 0 };
        foreach (var (table, expected) in new[] { (HereticDefinitions.Weapons1, expectedNormal), (HereticDefinitions.Weapons2, expectedPowered) })
        {
            Check(table.Count == 9, "Missing Heretic weapon variant.");
            for (var i = 0; i < table.Count; i++)
            {
                var weapon = table[i];
                Check(weapon.AmmoPerShot == expected[i], "Wrong weapon ammo consumption.");
                foreach (var state in new[] { weapon.Up, weapon.Down, weapon.Ready, weapon.Attack, weapon.HoldAttack, weapon.Flash })
                    Check((uint)state < HereticDefinitions.States.Count, "Bad weapon state reference.");
            }
        }
        var wand = HereticDefinitions.Weapons1[(int)HereticWeapon.wp_goldwand];
        var powered = HereticDefinitions.Weapons2[(int)HereticWeapon.wp_goldwand];
        Check(wand.Ammo == HereticAmmo.am_goldwand && wand.Attack == HereticStateId.S_GOLDWANDATK1_1 && powered.Attack == HereticStateId.S_GOLDWANDATK2_1, "Normal/powered wand mixed.");
        var blaster = HereticDefinitions.Weapons1[(int)HereticWeapon.wp_blaster];
        Check(blaster.Attack != blaster.HoldAttack, "Blaster refire state lost.");
    }
    static void CheckStateTiming()
    {
        foreach (var (start, period) in new[] { (HereticStateId.S_AKYY1, 30), (HereticStateId.S_BKYY1, 30), (HereticStateId.S_CKYY1, 27) })
        {
            var state = new HereticActorState(start);
            Check((state.Definition.Frame & 0x8000) != 0, "Key fullbright flag missing.");
            state.Tick(); state.Tick(); Check(state.State == start, "Key animation advanced early.");
            state.Tick(); Check(state.State != start, "Key animation did not advance.");
            for (var i = 3; i < period; i++) state.Tick();
            Check(state.State == start && state.Tics == 3, "Key animation did not wrap at reference cadence.");
        }
        var blood = new HereticActorState(HereticStateId.S_BLOOD1);
        for (var i = 0; i < 23; i++) blood.Tick();
        Check(!blood.Removed, "Effect removed early."); blood.Tick(); Check(blood.Removed, "Effect not removed at S_NULL.");
        blood.Tick(); Check(blood.Removed, "Removed effect resumed.");
        var permanent = new HereticActorState(HereticStateId.S_ITEM_SHLD1);
        for (var i = 0; i < 100; i++) permanent.Tick();
        Check(permanent.Tics == -1 && !permanent.Removed, "Permanent state expired.");
        var actions = new Actions(); var actor = new HereticActorState(HereticStateId.S_IMP_LOOK1, actions);
        Check(actions.Calls.Count == 0, "Spawn executed action.");
        for (var i = 0; i < 10; i++) actor.Tick();
        Check(actor.State == HereticStateId.S_IMP_LOOK2 && actions.Calls.SequenceEqual(new[] { HereticAction.A_Look }), "Action timing differs from Heretic.");
        actions.OnAction = a => a.SetState(HereticStateId.S_BLOOD1);
        actor.SetState(HereticStateId.S_IMP_LOOK1);
        Check(actor.State == HereticStateId.S_BLOOD1 && actor.Tics == 8, "Action-driven state redirection overwritten.");
        actions.OnAction = a => a.SetState(HereticStateId.S_NULL);
        actor.SetState(HereticStateId.S_IMP_LOOK1);
        Check(actor.Removed, "Action removal ignored.");
        var unsupported = new HereticActorState(HereticStateId.S_AKYY1);
        try { unsupported.SetState(HereticStateId.S_IMP_LOOK1); throw new Exception("Missing action silently skipped."); }
        catch (NotSupportedException) { }
        Check(unsupported.State == HereticStateId.S_AKYY1 && unsupported.Tics == 3, "Rejected action changed actor.");
        try { unsupported.SetState((HereticStateId)(-1)); throw new Exception("Invalid state accepted."); }
        catch (ArgumentOutOfRangeException) { }
        // Explicit no-function state changes are used by the reference for several effects.
        unsupported.SetState(HereticStateId.S_IMP_LOOK1, false);
        Check(unsupported.State == HereticStateId.S_IMP_LOOK1, "No-function state change rejected.");
    }
    static void CheckKeyIntegration(GameContent content)
    {
        var found = new HashSet<Sprite>();
        for (var map = 1; map <= 4; map++)
        {
            var session = new HereticWorldSession(content, 1, map);
            var keys = new List<Mobj>();
            foreach (var sector in session.World.Map.Sectors)
            for (var thing = sector.ThingList; thing != null; thing = thing.SectorNext)
                if (thing.Sprite == (Sprite)HereticSpriteId.SPR_AKYY || thing.Sprite == (Sprite)HereticSpriteId.SPR_BKYY || thing.Sprite == (Sprite)HereticSpriteId.SPR_CKYY) keys.Add(thing);
            for (var i = 0; i < 3; i++) session.Tick(default);
            foreach (var key in keys)
            {
                found.Add(key.Sprite);
                Check(key.Frame == 0x8001, "Map key not using Heretic animation state.");
                Check(key.State == null && key.Info == null, "Key borrowed Doom actor definitions.");
            }
            new HereticMapPreview(content, session).Render(new byte[320 * 200 * 4], 3);
        }
        Check(found.Count == 3, "Did not cover every key color in map integration.");
    }
}
