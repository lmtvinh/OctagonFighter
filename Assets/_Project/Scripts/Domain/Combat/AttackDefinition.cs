using System;
using OctagonFighter.Domain.Health;

namespace OctagonFighter.Domain.Combat
{
    /// <summary>
    /// Immutable timing, cost, and hit data for one strike.
    /// </summary>
    public readonly struct AttackDefinition
    {
        /// <summary>
        /// Gets the designer-facing name.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the animator state name used to present the strike.
        /// </summary>
        public string AnimationState { get; }

        /// <summary>
        /// Gets the wind-up duration in seconds.
        /// </summary>
        public float StartupSeconds { get; }

        /// <summary>
        /// Gets the duration of the active hit window in seconds.
        /// </summary>
        public float ActiveSeconds { get; }

        /// <summary>
        /// Gets the recovery duration in seconds.
        /// </summary>
        public float RecoverySeconds { get; }

        /// <summary>
        /// Gets the cooldown duration in seconds.
        /// </summary>
        public float CooldownSeconds { get; }

        /// <summary>
        /// Gets the stamina consumed when the strike starts.
        /// </summary>
        public float StaminaCost { get; }

        /// <summary>
        /// Gets the resolved damage applied on a successful hit.
        /// </summary>
        public float Damage { get; }

        /// <summary>
        /// Gets the forward reach of the hit query in meters.
        /// </summary>
        public float Range { get; }

        /// <summary>
        /// Gets the sweep radius of the hit query in meters.
        /// </summary>
        public float Radius { get; }

        /// <summary>
        /// Gets the body region this strike targets.
        /// </summary>
        public HitRegion Region { get; }

        /// <summary>
        /// Gets the total attack duration in seconds.
        /// </summary>
        public float DurationSeconds => StartupSeconds + ActiveSeconds + RecoverySeconds;

        /// <summary>
        /// Gets a value indicating whether this definition was configured.
        /// </summary>
        public bool IsDefined => DurationSeconds > 0f && Damage > 0f;

        /// <summary>
        /// Initializes a new instance of the <see cref="AttackDefinition"/> struct.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric value is invalid.</exception>
        /// <exception cref="ArgumentException">Thrown when a required name is empty.</exception>
        public AttackDefinition(
            string displayName,
            string animationState,
            float startupSeconds,
            float activeSeconds,
            float recoverySeconds,
            float cooldownSeconds,
            float staminaCost,
            float damage,
            float range,
            float radius,
            HitRegion region)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Attack display name must be specified.", nameof(displayName));
            }

            if (string.IsNullOrWhiteSpace(animationState))
            {
                throw new ArgumentException("Attack animation state must be specified.", nameof(animationState));
            }

            ValidateNonNegative(startupSeconds, nameof(startupSeconds));
            ValidatePositive(activeSeconds, nameof(activeSeconds));
            ValidateNonNegative(recoverySeconds, nameof(recoverySeconds));
            ValidateNonNegative(cooldownSeconds, nameof(cooldownSeconds));
            ValidateNonNegative(staminaCost, nameof(staminaCost));
            ValidatePositive(damage, nameof(damage));
            ValidatePositive(range, nameof(range));
            ValidatePositive(radius, nameof(radius));

            if (!Enum.IsDefined(typeof(HitRegion), region))
            {
                throw new ArgumentException("Hit region is not defined.", nameof(region));
            }

            DisplayName = displayName.Trim();
            AnimationState = animationState.Trim();
            StartupSeconds = startupSeconds;
            ActiveSeconds = activeSeconds;
            RecoverySeconds = recoverySeconds;
            CooldownSeconds = cooldownSeconds;
            StaminaCost = staminaCost;
            Damage = damage;
            Range = range;
            Radius = radius;
            Region = region;
        }

        /// <summary>
        /// Returns the timeline phase for an elapsed time since the strike started.
        /// </summary>
        public AttackPhase GetPhase(float elapsedSeconds)
        {
            if (elapsedSeconds < StartupSeconds)
            {
                return AttackPhase.Startup;
            }

            if (elapsedSeconds < StartupSeconds + ActiveSeconds)
            {
                return AttackPhase.Active;
            }

            return elapsedSeconds < DurationSeconds ? AttackPhase.Recovery : AttackPhase.Complete;
        }

        private static void ValidateNonNegative(float value, string parameterName)
        {
            if (!float.IsFinite(value) || value < 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Value must be finite and non-negative.");
            }
        }

        private static void ValidatePositive(float value, string parameterName)
        {
            if (!float.IsFinite(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Value must be finite and positive.");
            }
        }
    }
}
