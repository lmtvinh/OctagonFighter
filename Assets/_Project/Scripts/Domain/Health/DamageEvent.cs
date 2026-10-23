using System;
using OctagonFighter.Core.Ids;
using OctagonFighter.Core.Time;

namespace OctagonFighter.Domain.Health
{
    /// <summary>
    /// Describes resolved damage that should be applied to a target.
    /// </summary>
    public readonly struct DamageEvent
    {
        /// <summary>
        /// Gets the attacker identifier, or an empty identifier for environmental damage.
        /// </summary>
        public FighterId AttackerId { get; }

        /// <summary>
        /// Gets the target fighter identifier.
        /// </summary>
        public FighterId TargetId { get; }

        /// <summary>
        /// Gets the resolved damage amount.
        /// </summary>
        public float Amount { get; }

        /// <summary>
        /// Gets the damage classification.
        /// </summary>
        public DamageType Type { get; }

        /// <summary>
        /// Gets the damaged body region.
        /// </summary>
        public HitRegion Region { get; }

        /// <summary>
        /// Gets the tick in which the damage was resolved.
        /// </summary>
        public TickId Tick { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DamageEvent"/> struct.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the damage amount is invalid.</exception>
        /// <exception cref="ArgumentException">Thrown when the target identifier is empty.</exception>
        public DamageEvent(
            FighterId attackerId,
            FighterId targetId,
            float amount,
            DamageType type,
            HitRegion region,
            TickId tick)
        {
            if (!float.IsFinite(amount) || amount < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Damage amount must be finite and non-negative.");
            }

            if (targetId.IsEmpty)
            {
                throw new ArgumentException("Damage target must be specified.", nameof(targetId));
            }

            AttackerId = attackerId;
            TargetId = targetId;
            Amount = amount;
            Type = type;
            Region = region;
            Tick = tick;
        }
    }
}
