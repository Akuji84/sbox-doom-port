// s&Doom modification: 2026-09-29, consume frame mouse turn once with fractional accumulation.
// s&Doom modification: 2026-09-29, pause without queued input or simulation catch-up.
// s&Doom modification: 2026-09-22, opt-in native Clink test encounter integration.
// Copyright (C) 2026 s&Doom contributors.
// SPDX-License-Identifier: GPL-2.0-or-later
using System;
namespace ManagedDoom
{
    /// <summary>Samples frame input once, retaining short actions until a 35 Hz command consumes them.</summary>
    public sealed class HereticFrameStepper
    {
        private double remainder;
        private double pendingTurn;
        public void ClearFrameTurn() => pendingTurn = 0;
        public void QueueFrameTurn(double turn)
        {
            if (!double.IsFinite(turn)) throw new ArgumentOutOfRangeException(nameof(turn));
            if (!Paused) pendingTurn = Math.Clamp(pendingTurn + turn, -131072, 131072);
        }
        private HereticWeapon? pendingWeapon;
        private HereticArtifact? pendingArtifact;
        private bool observedUse, sentUse, pendingUse, pendingCenter, pendingLand, pendingTestAttack;
        public int TickCount { get; private set; }
        public bool Paused { get; private set; }
        public void SetPaused(bool paused)
        {
            if (Paused == paused) return;
            Paused = paused;
            remainder = 0;
            ClearFrameTurn();
            pendingWeapon = null;
            pendingArtifact = null;
            observedUse = sentUse = pendingUse = pendingCenter = pendingLand = pendingTestAttack = false;
        }
        public void Advance(double elapsedSeconds, HereticCommand sampled, Action<HereticCommand> tick)
        {
            if (!double.IsFinite(elapsedSeconds) || elapsedSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            if (tick == null) throw new ArgumentNullException(nameof(tick));
            if (Paused) return;
            pendingUse |= sampled.Use && !observedUse;
            observedUse = sampled.Use;
            pendingCenter |= sampled.CenterLook;
            pendingLand |= sampled.Land;
            pendingTestAttack |= sampled.TestAttack;
            pendingWeapon = sampled.SelectWeapon ?? pendingWeapon;
            pendingArtifact = sampled.UseArtifact ?? pendingArtifact;
            // Bound recovery after a paused/stalled editor frame. Retain the fractional tick.
            remainder += Math.Min(elapsedSeconds, 0.25) * 35;
            while (remainder >= 1 - 1e-9)
            {
                var command = sampled;
                var frameTurn = (int)Math.Clamp(Math.Truncate(pendingTurn), short.MinValue - (int)sampled.Turn, short.MaxValue - (int)sampled.Turn);
                command.Turn = (short)(sampled.Turn + frameTurn);
                pendingTurn -= frameTurn;
                if (pendingUse && sentUse)
                    command.Use = false; // Preserve a release between two sampled presses.
                else
                {
                    command.Use |= pendingUse;
                    pendingUse = false;
                }
                command.CenterLook |= pendingCenter;
                command.Land |= pendingLand;
                command.TestAttack |= pendingTestAttack;
                command.SelectWeapon = pendingWeapon;
                command.UseArtifact = pendingArtifact;
                tick(command);
                pendingWeapon = null;
                pendingArtifact = null;
                sentUse = command.Use;
                pendingCenter = pendingLand = pendingTestAttack = false;
                remainder = Math.Max(0, remainder - 1);
                TickCount++;
            }
        }
    }
}
