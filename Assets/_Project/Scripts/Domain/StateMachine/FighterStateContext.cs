using System;
using OctagonFighter.Core.Time;
using OctagonFighter.Domain.Fighters;
using OctagonFighter.Domain.Health;
using OctagonFighter.Domain.Stamina;

namespace OctagonFighter.Domain.StateMachine
{
    /// <summary>
    /// Read-only context supplied to fighter states for one simulation tick.
    /// </summary>
    public readonly struct FighterStateContext
    {
        /// <summary>
        /// Gets the immutable fighter data.
        /// </summary>
        public FighterData Data { get; }

        /// <summary>
        /// Gets the health source.
        /// </summary>
        public IHealthSource Health { get; }

        /// <summary>
        /// Gets the stamina source.
        /// </summary>
        public IStaminaSource Stamina { get; }

        /// <summary>
        /// Gets the combat clock.
        /// </summary>
        public ICombatClock Clock { get; }

        /// <summary>
        /// Gets the latest fighter snapshot.
        /// </summary>
        public FighterSnapshot Snapshot { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FighterStateContext"/> struct.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when a required dependency is null.</exception>
        public FighterStateContext(
            FighterData data,
            IHealthSource health,
            IStaminaSource stamina,
            ICombatClock clock,
            FighterSnapshot snapshot)
        {
            Data = data;
            Health = health ?? throw new ArgumentNullException(nameof(health));
            Stamina = stamina ?? throw new ArgumentNullException(nameof(stamina));
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Snapshot = snapshot;
        }
    }
}
