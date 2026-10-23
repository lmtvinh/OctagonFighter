using System;

namespace OctagonFighter.Domain.Health
{
    /// <summary>
    /// Immutable health configuration for a fighter.
    /// </summary>
    public readonly struct HealthProfile
    {
        /// <summary>
        /// Gets the maximum health value.
        /// </summary>
        public float MaxHealth { get; }

        /// <summary>
        /// Gets the maximum accumulated head trauma.
        /// </summary>
        public float HeadTraumaLimit { get; }

        /// <summary>
        /// Gets the maximum accumulated body trauma.
        /// </summary>
        public float BodyTraumaLimit { get; }

        /// <summary>
        /// Gets the head trauma value that causes a knockout.
        /// </summary>
        public float KnockoutThreshold { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthProfile"/> struct.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when a value is invalid.</exception>
        public HealthProfile(
            float maxHealth,
            float headTraumaLimit,
            float bodyTraumaLimit,
            float knockoutThreshold)
        {
            if (!float.IsFinite(maxHealth) || maxHealth <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHealth), "Max health must be finite and positive.");
            }

            if (!float.IsFinite(headTraumaLimit) || headTraumaLimit <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(headTraumaLimit), "Head trauma limit must be finite and positive.");
            }

            if (!float.IsFinite(bodyTraumaLimit) || bodyTraumaLimit <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(bodyTraumaLimit), "Body trauma limit must be finite and positive.");
            }

            if (!float.IsFinite(knockoutThreshold) ||
                knockoutThreshold <= 0f ||
                knockoutThreshold > headTraumaLimit)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(knockoutThreshold),
                    "Knockout threshold must be finite, positive, and no greater than the head trauma limit.");
            }

            MaxHealth = maxHealth;
            HeadTraumaLimit = headTraumaLimit;
            BodyTraumaLimit = bodyTraumaLimit;
            KnockoutThreshold = knockoutThreshold;
        }
    }
}
