using OctagonFighter.Core.Ids;
using OctagonFighter.Core.Time;
using OctagonFighter.Domain.Stamina;
using OctagonFighter.Domain.StateMachine;

namespace OctagonFighter.Domain.Fighters
{
    /// <summary>
    /// Immutable read-only view of a fighter's simulation state.
    /// </summary>
    public readonly struct FighterSnapshot
    {
        /// <summary>
        /// Gets the fighter identifier.
        /// </summary>
        public FighterId Id { get; }

        /// <summary>
        /// Gets the current state-machine state.
        /// </summary>
        public FighterStateId State { get; }

        /// <summary>
        /// Gets the current health.
        /// </summary>
        public float CurrentHealth { get; }

        /// <summary>
        /// Gets the maximum health.
        /// </summary>
        public float MaxHealth { get; }

        /// <summary>
        /// Gets the current stamina.
        /// </summary>
        public float CurrentStamina { get; }

        /// <summary>
        /// Gets the maximum stamina.
        /// </summary>
        public float MaxStamina { get; }

        /// <summary>
        /// Gets the current fatigue tier.
        /// </summary>
        public StaminaTier StaminaTier { get; }

        /// <summary>
        /// Gets a value indicating whether the fighter is alive.
        /// </summary>
        public bool IsAlive { get; }

        /// <summary>
        /// Gets a value indicating whether the fighter is knocked out.
        /// </summary>
        public bool IsKnockedOut { get; }

        /// <summary>
        /// Gets the tick represented by this snapshot.
        /// </summary>
        public TickId Tick { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FighterSnapshot"/> struct.
        /// </summary>
        public FighterSnapshot(
            FighterId id,
            FighterStateId state,
            float currentHealth,
            float maxHealth,
            float currentStamina,
            float maxStamina,
            StaminaTier staminaTier,
            bool isAlive,
            bool isKnockedOut,
            TickId tick)
        {
            Id = id;
            State = state;
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
            CurrentStamina = currentStamina;
            MaxStamina = maxStamina;
            StaminaTier = staminaTier;
            IsAlive = isAlive;
            IsKnockedOut = isKnockedOut;
            Tick = tick;
        }
    }
}
