using System;

namespace ManagedDoom;

// Wall-clock progress tracking also runs while menus or the lockstep barrier
// prevent simulation ticks. Recovery transfers get a separate grace period.
public sealed class SboxManagedDoomProgressWatchdog
{
    private DateTime lastProgress = DateTime.MinValue;
    private int round, tic, recovery, chunks;
    private bool recovering;

    public void Reset() => lastProgress = DateTime.MinValue;

    public bool HasTimedOut(DateTime now, int roundId, int commandTic, bool isRecovering, int recoveryId, int receivedChunks)
    {
        if (lastProgress == DateTime.MinValue || round != roundId || tic != commandTic
            || recovering != isRecovering || (isRecovering && (recovery != recoveryId || chunks != receivedChunks)))
        {
            lastProgress = now;
            round = roundId; tic = commandTic; recovering = isRecovering;
            recovery = recoveryId; chunks = receivedChunks;
            return false;
        }
        return now - lastProgress >= TimeSpan.FromSeconds(isRecovering ? 30 : 15);
    }
}
