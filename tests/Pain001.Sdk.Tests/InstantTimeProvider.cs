// SPDX-License-Identifier: Apache-2.0 OR MIT

namespace Pain001.Sdk.Tests;

/// <summary>
/// A clock fixed at 2026-11-14 12:00:00 UTC whose timers fire at once,
/// recording each wait asked for, so retry tests never sleep.
/// </summary>
internal sealed class InstantTimeProvider : TimeProvider
{
    /// <summary>Every finite wait a timer was created for, in order.</summary>
    internal List<TimeSpan> Delays { get; } = [];

    /// <summary>Runs when a wait is asked for, before it ends.</summary>
    internal Action? OnDelay { get; set; }

    public override DateTimeOffset GetUtcNow() => new(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        Delays.Add(dueTime);
        OnDelay?.Invoke();
        ThreadPool.QueueUserWorkItem(_ => callback(state));
        return new Fired();
    }

    private sealed class Fired : ITimer
    {
        public bool Change(TimeSpan dueTime, TimeSpan period) => true;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
