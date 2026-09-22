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
        private bool observedUse, sentUse, pendingUse, pendingCenter, pendingLand, pendingTestAttack;
        public int TickCount { get; private set; }
        public void Advance(double elapsedSeconds, HereticCommand sampled, Action<HereticCommand> tick)
        {
            if (!double.IsFinite(elapsedSeconds) || elapsedSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            if (tick == null) throw new ArgumentNullException(nameof(tick));
            pendingUse |= sampled.Use && !observedUse;
            observedUse = sampled.Use;
            pendingCenter |= sampled.CenterLook;
            pendingLand |= sampled.Land;
            pendingTestAttack |= sampled.TestAttack;
            // Bound recovery after a paused/stalled editor frame. Retain the fractional tick.
            remainder += Math.Min(elapsedSeconds, 0.25) * 35;
            while (remainder >= 1 - 1e-9)
            {
                var command = sampled;
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
                tick(command);
                sentUse = command.Use;
                pendingCenter = pendingLand = pendingTestAttack = false;
                remainder = Math.Max(0, remainder - 1);
                TickCount++;
            }
        }
    }
}
