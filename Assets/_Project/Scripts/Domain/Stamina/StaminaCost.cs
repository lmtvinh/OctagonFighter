using System;

namespace OctagonFighter.Domain.Stamina
{
    /// <summary>
    /// Describes the stamina cost of a committed action.
    /// </summary>
    public readonly struct StaminaCost
    {
        /// <summary>
        /// Gets the stamina amount consumed by the action.
        /// </summary>
        public float Amount { get; }

        /// <summary>
        /// Gets the additional regeneration delay requested by the action.
        /// </summary>
        public float RecoveryDelaySeconds { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StaminaCost"/> struct.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when a value is invalid.</exception>
        public StaminaCost(float amount, float recoveryDelaySeconds = 0f)
        {
            if (!float.IsFinite(amount) || amount < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Stamina cost must be finite and non-negative.");
            }

            if (!float.IsFinite(recoveryDelaySeconds) || recoveryDelaySeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(recoveryDelaySeconds),
                    "Recovery delay must be finite and non-negative.");
            }

            Amount = amount;
            RecoveryDelaySeconds = recoveryDelaySeconds;
        }
    }
}
