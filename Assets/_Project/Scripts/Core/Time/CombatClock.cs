using System;

namespace OctagonFighter.Core.Time
{
    /// <summary>
    /// Deterministic tick counter advanced by the combat tick driver.
    /// </summary>
    public sealed class CombatClock : ICombatClock
    {
        /// <inheritdoc />
        public TickId CurrentTick { get; private set; } = TickId.Zero;

        /// <summary>
        /// Advances the clock by one tick.
        /// </summary>
        public TickId Advance()
        {
            CurrentTick = CurrentTick.Next();
            return CurrentTick;
        }

        /// <summary>
        /// Advances the clock by a number of ticks.
        /// </summary>
        /// <param name="tickCount">Number of ticks to advance.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="tickCount"/> is negative.</exception>
        public TickId Advance(int tickCount)
        {
            if (tickCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tickCount));
            }

            for (int index = 0; index < tickCount; index++)
            {
                CurrentTick = CurrentTick.Next();
            }

            return CurrentTick;
        }
    }
}
