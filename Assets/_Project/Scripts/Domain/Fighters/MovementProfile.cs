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
        /// Gets the lateral movement speed in meters per second.
        /// </summary>
        public float LateralSpeed { get; }

        /// <summary>
        /// Gets the fraction of the movement speeds used while walking.
        /// </summary>
        public float WalkSpeedMultiplier { get; }

        /// <summary>
        /// Gets the maximum character rotation speed in degrees per second.
        /// </summary>
        public float RotationDegreesPerSecond { get; }

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
        /// Gets the configured planar speed in the supplied direction, before any additional
        /// scaling. Lateral and forward/backward speeds are independent, so a diagonal input
        /// produces the same speed the motor will apply for that direction.
        /// </summary>
        /// <param name="lateralDirection">Normalized lateral (right-positive) component.</param>
        /// <param name="forwardDirection">Normalized forward (opponent-positive) component.</param>
        /// <param name="speedScale">Optional multiplier, normally the walk-speed multiplier.</param>
        public float GetPlanarSpeed(
            float lateralDirection,
            float forwardDirection,
            float speedScale = 1f)
        {
            float forwardSpeed = forwardDirection >= 0f ? ForwardSpeed : BackwardSpeed;
            float lateral = lateralDirection * LateralSpeed;
            float forward = forwardDirection * forwardSpeed;
            return MathF.Sqrt((lateral * lateral) + (forward * forward)) * speedScale;
        }

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
            float recoveryDurationSeconds,
            float walkSpeedMultiplier = 0.55f,
            float rotationDegreesPerSecond = 720f)
        {
            ValidateSpeed(forwardSpeed, nameof(forwardSpeed));
            ValidateSpeed(backwardSpeed, nameof(backwardSpeed));
            ValidateSpeed(lateralSpeed, nameof(lateralSpeed));
            ValidateWalkSpeedMultiplier(walkSpeedMultiplier, nameof(walkSpeedMultiplier));
            ValidateRotationSpeed(rotationDegreesPerSecond, nameof(rotationDegreesPerSecond));
            ValidateDuration(spawnDurationSeconds, nameof(spawnDurationSeconds));
            ValidateDuration(dodgeDurationSeconds, nameof(dodgeDurationSeconds));
            ValidateDuration(hitReactionDurationSeconds, nameof(hitReactionDurationSeconds));
            ValidateDuration(knockdownDurationSeconds, nameof(knockdownDurationSeconds));
            ValidateDuration(recoveryDurationSeconds, nameof(recoveryDurationSeconds));

            ForwardSpeed = forwardSpeed;
            BackwardSpeed = backwardSpeed;
            LateralSpeed = lateralSpeed;
            WalkSpeedMultiplier = walkSpeedMultiplier;
            RotationDegreesPerSecond = rotationDegreesPerSecond;
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

        private static void ValidateRotationSpeed(float value, string parameterName)
        {
            if (!float.IsFinite(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    "Rotation speed must be finite and positive.");
            }
        }

        private static void ValidateWalkSpeedMultiplier(float value, string parameterName)
        {
            if (!float.IsFinite(value) || value <= 0f || value > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    "The walk speed multiplier must be finite and within (0, 1].");
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
