using System;

namespace OctagonFighter.Domain.Stamina
{
    /// <summary>
    /// Multipliers applied when a fighter reaches a fatigue tier.
    /// </summary>
    public readonly struct StaminaTierModifiers
    {
        /// <summary>
        /// Gets the default unmodified values.
        /// </summary>
        public static StaminaTierModifiers Default => new StaminaTierModifiers(1f, 1f, 1f, 1f);

        /// <summary>
        /// Gets the movement speed multiplier.
        /// </summary>
        public float SpeedMultiplier { get; }

        /// <summary>
        /// Gets the offensive output multiplier.
        /// </summary>
        public float DamageMultiplier { get; }

        /// <summary>
        /// Gets the defensive effectiveness multiplier.
        /// </summary>
        public float DefenseMultiplier { get; }

        /// <summary>
        /// Gets the stamina regeneration multiplier.
        /// </summary>
        public float RegenMultiplier { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StaminaTierModifiers"/> struct.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when a multiplier is not finite and positive.</exception>
        public StaminaTierModifiers(
            float speedMultiplier,
            float damageMultiplier,
            float defenseMultiplier,
            float regenMultiplier)
        {
            ValidateMultiplier(speedMultiplier, nameof(speedMultiplier));
            ValidateMultiplier(damageMultiplier, nameof(damageMultiplier));
            ValidateMultiplier(defenseMultiplier, nameof(defenseMultiplier));
            ValidateMultiplier(regenMultiplier, nameof(regenMultiplier));

            SpeedMultiplier = speedMultiplier;
            DamageMultiplier = damageMultiplier;
            DefenseMultiplier = defenseMultiplier;
            RegenMultiplier = regenMultiplier;
        }

        private static void ValidateMultiplier(float value, string parameterName)
        {
            if (!float.IsFinite(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Multipliers must be finite and positive.");
            }
        }
    }
}
