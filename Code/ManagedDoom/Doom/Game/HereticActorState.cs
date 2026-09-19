//
// Copyright(C) 1993-1996 Id Software, Inc.
// Copyright(C) 1993-2008 Raven Software
// Copyright(C) 2005-2014 Simon Howard
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//

// Adapted 2026-09-18: isolated actor state scheduling from p_mobj.c.
// Chocolate Doom 895f581c5d91497bdda0516612da803fe5843e28.
using System;
namespace ManagedDoom
{
    /// <summary>Action implementation owned by the Heretic actor layer, never DoomInfo.</summary>
    public interface IHereticActorActions
    {
        bool Supports(HereticAction action);
        void Execute(HereticAction action, HereticActorState actor);
    }

    /// <summary>Heretic actor timing only; weapon overlays have different transition rules.</summary>
    public sealed class HereticActorState
    {
        private readonly IHereticActorActions actions;
        private int transitionDepth;
        public HereticStateId State { get; private set; }
        public int Tics { get; private set; }
        public bool Removed { get; private set; }
        public HereticStateDefinition Definition => HereticDefinitions.States[(int)State];

        public HereticActorState(HereticStateId spawnState, IHereticActorActions actions = null)
        {
            this.actions = actions;
            // P_SpawnMobj assigns the initial frame without running its action.
            SetState(spawnState, false);
        }

        public void SetState(HereticStateId state, bool runAction = true)
        {
            if ((uint)state >= (uint)HereticDefinitions.States.Count)
                throw new ArgumentOutOfRangeException(nameof(state));
            if (Removed) throw new InvalidOperationException("Cannot reanimate a removed Heretic actor.");
            var definition = HereticDefinitions.States[(int)state];
            if (state != HereticStateId.S_NULL && runAction && definition.Action != HereticAction.None &&
                (actions == null || !actions.Supports(definition.Action)))
                throw new NotSupportedException("Heretic actor action is not implemented: " + definition.Action);
            if (++transitionDepth > 256)
            {
                transitionDepth--;
                throw new InvalidOperationException("Recursive Heretic actor state actions.");
            }
            try
            {
                State = state;
                Tics = definition.Tics;
                if (state == HereticStateId.S_NULL) { Removed = true; return; }
                if (runAction && definition.Action != HereticAction.None) actions.Execute(definition.Action, this);
            }
            finally { transitionDepth--; }
        }

        public void Tick()
        {
            if (Removed || Tics == -1) return;
            Tics--;
            // Unlike Doom's SetState, Heretic drains zero-duration successors here.
            var transitions = 0;
            while (Tics == 0 && !Removed)
            {
                if (++transitions > HereticDefinitions.States.Count)
                    throw new InvalidOperationException("Zero-duration Heretic actor state cycle.");
                SetState(Definition.Next);
            }
        }
    }
}
