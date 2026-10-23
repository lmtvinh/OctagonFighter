using System;

namespace OctagonFighter.Domain.Stamina
{
    /// <summary>
    /// Immutable stamina configuration for a fighter.
    /// </summary>
    public readonly struct StaminaProfile
    {
        /// <summary>
        /// Gets the maximum stamina value.
        /// </summary>
        public float MaxStamina { get; }

        /// <summary>
        /// Gets the stamina regenerated per second after the delay expires.
        /// </summary>
        public float RegenPerSecond { get; }

        /// <summary>
        /// Gets the base delay before regeneration resumes.
        /// </summary>
        public float RegenDelaySeconds { get; }

        /// <summary>
        /// Gets the fatigue tier thresholds.
        /// </summary>
        public StaminaTierThresholds Tiers { get; }

        /// <summary>
        /// Gets the modifiers applied while tired.
        /// </summary>
        public StaminaTierModifiers TiredModifiers { get; }

        /// <summary>
        /// Gets the modifiers applied while gassed.
        /// </summary>
        public StaminaTierModifiers GassedModifiers { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StaminaProfile"/> struct.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when a value is invalid.</exception>
        public StaminaProfile(
            float maxStamina,
            float regenPerSecond,
            float regenDelaySeconds,
            StaminaTierThresholds tiers,
            StaminaTierModifiers tiredModifiers,
            StaminaTierModifiers gassedModifiers)
        {
            if (!float.IsFinite(maxStamina) || maxStamina <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maxStamina), "Max stamina must be finite and positive.");
            }

            if (!float.IsFinite(regenPerSecond) || regenPerSecond < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(regenPerSecond), "Stamina regeneration must be finite and non-negative.");
            }

            if (!float.IsFinite(regenDelaySeconds) || regenDelaySeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(regenDelaySeconds), "Regeneration delay must be finite and non-negative.");
            }

            MaxStamina = maxStamina;
            RegenPerSecond = regenPerSecond;
            RegenDelaySeconds = regenDelaySeconds;
            Tiers = tiers;
            TiredModifiers = tiredModifiers;
            GassedModifiers = gassedModifiers;
        }
    }
}
