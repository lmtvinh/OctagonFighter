using System;

namespace OctagonFighter.Domain.Fighters
{
    /// <summary>
    /// Immutable movement and reaction timing configuration.
    /// </summary>
    public readonly struct MovementProfile
    {
        /// <summary>
        /// Gets forward movement speed in meters per second.
        /// </summary>
        public float ForwardSpeed { get; }

        /// <summary>
        /// Gets backward movement speed in meters per second.
        /// </summary>
        public float BackwardSpeed { get; }

        /// <summary>
        /// Gets lateral movement speed in meters per second.
        /// </summary>
        public float LateralSpeed { get; }

        /// <summary>
        /// Gets the spawn lock duration in seconds.
        /// </summary>
        public float SpawnDurationSeconds { get; }

        /// <summary>
        /// Gets the dodge duration in seconds.
        /// </summary>
        public float DodgeDurationSeconds { get; }

        /// <summary>
        /// Gets the hit-reaction duration in seconds.
        /// </summary>
        public float HitReactionDurationSeconds { get; }

        /// <summary>
        /// Gets the knocked-down duration in seconds.
        /// </summary>
        public float KnockdownDurationSeconds { get; }

        /// <summary>
        /// Gets the recovery duration in seconds.
        /// </summary>
        public float RecoveryDurationSeconds { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MovementProfile"/> struct.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when a speed or duration is invalid.</exception>
        public MovementProfile(
            float forwardSpeed,
            float backwardSpeed,
            float lateralSpeed,
            float spawnDurationSeconds,
            float dodgeDurationSeconds,
            float hitReactionDurationSeconds,
            float knockdownDurationSeconds,
            float recoveryDurationSeconds)
        {
            ValidateSpeed(forwardSpeed, nameof(forwardSpeed));
            ValidateSpeed(backwardSpeed, nameof(backwardSpeed));
            ValidateSpeed(lateralSpeed, nameof(lateralSpeed));
            ValidateDuration(spawnDurationSeconds, nameof(spawnDurationSeconds));
            ValidateDuration(dodgeDurationSeconds, nameof(dodgeDurationSeconds));
            ValidateDuration(hitReactionDurationSeconds, nameof(hitReactionDurationSeconds));
            ValidateDuration(knockdownDurationSeconds, nameof(knockdownDurationSeconds));
            ValidateDuration(recoveryDurationSeconds, nameof(recoveryDurationSeconds));

            ForwardSpeed = forwardSpeed;
            BackwardSpeed = backwardSpeed;
            LateralSpeed = lateralSpeed;
            SpawnDurationSeconds = spawnDurationSeconds;
            DodgeDurationSeconds = dodgeDurationSeconds;
            HitReactionDurationSeconds = hitReactionDurationSeconds;
            KnockdownDurationSeconds = knockdownDurationSeconds;
            RecoveryDurationSeconds = recoveryDurationSeconds;
        }

        private static void ValidateSpeed(float value, string parameterName)
        {
            if (!float.IsFinite(value) || value < 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Movement speeds must be finite and non-negative.");
            }
        }

        private static void ValidateDuration(float value, string parameterName)
        {
            if (!float.IsFinite(value) || value < 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Durations must be finite and non-negative.");
            }
        }
    }
}
