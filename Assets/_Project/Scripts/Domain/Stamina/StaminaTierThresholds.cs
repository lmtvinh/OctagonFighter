using System;

namespace OctagonFighter.Domain.Stamina
{
    /// <summary>
    /// Normalized stamina thresholds used to derive a fatigue tier.
    /// </summary>
    public readonly struct StaminaTierThresholds
    {
        /// <summary>
        /// Gets the minimum normalized stamina for the fresh tier.
        /// </summary>
        public float FreshThreshold { get; }

        /// <summary>
        /// Gets the minimum normalized stamina for the working tier.
        /// </summary>
        public float WorkingThreshold { get; }

        /// <summary>
        /// Gets the minimum normalized stamina for the tired tier.
        /// </summary>
        public float TiredThreshold { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StaminaTierThresholds"/> struct.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when thresholds are not strictly descending within (0, 1).</exception>
        public StaminaTierThresholds(float freshThreshold, float workingThreshold, float tiredThreshold)
        {
            if (!IsValidThreshold(freshThreshold) ||
                !IsValidThreshold(workingThreshold) ||
                !IsValidThreshold(tiredThreshold) ||
                freshThreshold <= workingThreshold ||
                workingThreshold <= tiredThreshold)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(freshThreshold),
                    "Stamina thresholds must be finite, within (0, 1), and strictly descending.");
            }

            FreshThreshold = freshThreshold;
            WorkingThreshold = workingThreshold;
            TiredThreshold = tiredThreshold;
        }

        /// <summary>
        /// Derives a fatigue tier from a normalized stamina value.
        /// </summary>
        public StaminaTier GetTier(float normalizedStamina)
        {
            if (normalizedStamina >= FreshThreshold)
            {
                return StaminaTier.Fresh;
            }

            if (normalizedStamina >= WorkingThreshold)
            {
                return StaminaTier.Working;
            }

            if (normalizedStamina >= TiredThreshold)
            {
                return StaminaTier.Tired;
            }

            return StaminaTier.Gassed;
        }

        private static bool IsValidThreshold(float value)
        {
            return float.IsFinite(value) && value > 0f && value < 1f;
        }
    }
}
