using System;

namespace OctagonFighter.Domain.Guard
{
    /// <summary>
    /// Immutable guard configuration for a fighter.
    /// </summary>
    public readonly struct GuardProfile
    {
        /// <summary>
        /// Gets the maximum guard value.
        /// </summary>
        public float MaxGuard { get; }

        /// <summary>
        /// Gets the fraction of a blocked strike's damage that still reaches health.
        /// </summary>
        public float BlockedHealthDamageMultiplier { get; }

        /// <summary>
        /// Gets the delay before guard recovery starts after a blocked hit or a guard release.
        /// </summary>
        public float RecoveryDelaySeconds { get; }

        /// <summary>
        /// Gets the guard recovered per second once the delay has elapsed.
        /// </summary>
        public float RecoveryPerSecond { get; }

        /// <summary>
        /// Gets the block-impact reaction duration in seconds.
        /// </summary>
        public float BlockImpactSeconds { get; }

        /// <summary>
        /// Gets the guard-break stun duration in seconds.
        /// </summary>
        public float GuardBreakSeconds { get; }

        /// <summary>
        /// Gets the controlled distance, in meters, that a blocked hit pushes the defender.
        /// </summary>
        public float PushbackDistanceMeters { get; }

        /// <summary>
        /// Gets the extra guard-damage fraction applied at full impact strength.
        /// </summary>
        public float ImpactGuardDamageBonus { get; }

        /// <summary>
        /// Gets the extra guard-damage fraction applied when guard is fully depleted.
        /// </summary>
        public float LowGuardDamageBonus { get; }

        /// <summary>
        /// Gets the guard fraction at or below which low-guard damage amplification applies.
        /// </summary>
        public float LowGuardThresholdFraction { get; }

        /// <summary>
        /// Gets the minimum dynamic guard-damage multiplier.
        /// </summary>
        public float MinimumGuardDamageMultiplier { get; }

        /// <summary>
        /// Gets the maximum dynamic guard-damage multiplier.
        /// </summary>
        public float MaximumGuardDamageMultiplier { get; }

        /// <summary>
        /// Gets the guard damage applied per point of direct damage that bypasses authored strike data.
        /// </summary>
        public float DirectGuardDamagePerDamagePoint { get; }

        /// <summary>
        /// Gets the stamina spent when a direct damage application is blocked.
        /// </summary>
        public float DirectBlockStaminaCost { get; }

        /// <summary>
        /// Gets the fraction of maximum guard restored when a guard-break stun ends.
        /// </summary>
        public float GuardBreakRecoveryFraction { get; }

        /// <summary>
        /// Gets a value indicating whether this profile was configured.
        /// </summary>
        public bool IsDefined => MaxGuard > 0f;

        /// <summary>
        /// Initializes a new instance of the <see cref="GuardProfile"/> struct.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when a value is invalid.</exception>
        public GuardProfile(
            float maxGuard,
            float blockedHealthDamageMultiplier,
            float recoveryDelaySeconds,
            float recoveryPerSecond,
            float blockImpactSeconds,
            float guardBreakSeconds,
            float pushbackDistanceMeters,
            float impactGuardDamageBonus,
            float lowGuardDamageBonus,
            float minimumGuardDamageMultiplier,
            float maximumGuardDamageMultiplier,
            float directGuardDamagePerDamagePoint,
            float directBlockStaminaCost,
            float guardBreakRecoveryFraction,
            float lowGuardThresholdFraction = 0.25f)
        {
            if (!float.IsFinite(maxGuard) || maxGuard <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maxGuard), "Max guard must be finite and positive.");
            }

            if (!float.IsFinite(blockedHealthDamageMultiplier) ||
                blockedHealthDamageMultiplier < 0f ||
                blockedHealthDamageMultiplier > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(blockedHealthDamageMultiplier),
                    "Blocked health damage multiplier must be within [0, 1].");
            }

            ValidateNonNegative(recoveryDelaySeconds, nameof(recoveryDelaySeconds));
            ValidateNonNegative(recoveryPerSecond, nameof(recoveryPerSecond));
            ValidatePositive(blockImpactSeconds, nameof(blockImpactSeconds));
            ValidatePositive(guardBreakSeconds, nameof(guardBreakSeconds));
            ValidateNonNegative(pushbackDistanceMeters, nameof(pushbackDistanceMeters));
            ValidateNonNegative(impactGuardDamageBonus, nameof(impactGuardDamageBonus));
            ValidateNonNegative(lowGuardDamageBonus, nameof(lowGuardDamageBonus));

            if (!float.IsFinite(lowGuardThresholdFraction) ||
                lowGuardThresholdFraction < 0f ||
                lowGuardThresholdFraction > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lowGuardThresholdFraction),
                    "Low-guard threshold must be within [0, 1].");
            }

            if (!float.IsFinite(minimumGuardDamageMultiplier) || minimumGuardDamageMultiplier < 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumGuardDamageMultiplier),
                    "Minimum guard-damage multiplier must be finite and at least one.");
            }

            if (!float.IsFinite(maximumGuardDamageMultiplier) ||
                maximumGuardDamageMultiplier < minimumGuardDamageMultiplier)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumGuardDamageMultiplier),
                    "Maximum guard-damage multiplier must be finite and no less than the minimum.");
            }

            ValidateNonNegative(directGuardDamagePerDamagePoint, nameof(directGuardDamagePerDamagePoint));
            ValidateNonNegative(directBlockStaminaCost, nameof(directBlockStaminaCost));

            if (!float.IsFinite(guardBreakRecoveryFraction) ||
                guardBreakRecoveryFraction < 0f ||
                guardBreakRecoveryFraction > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(guardBreakRecoveryFraction),
                    "Guard-break recovery fraction must be within [0, 1].");
            }

            MaxGuard = maxGuard;
            BlockedHealthDamageMultiplier = blockedHealthDamageMultiplier;
            RecoveryDelaySeconds = recoveryDelaySeconds;
            RecoveryPerSecond = recoveryPerSecond;
            BlockImpactSeconds = blockImpactSeconds;
            GuardBreakSeconds = guardBreakSeconds;
            PushbackDistanceMeters = pushbackDistanceMeters;
            ImpactGuardDamageBonus = impactGuardDamageBonus;
            LowGuardDamageBonus = lowGuardDamageBonus;
            LowGuardThresholdFraction = lowGuardThresholdFraction;
            MinimumGuardDamageMultiplier = minimumGuardDamageMultiplier;
            MaximumGuardDamageMultiplier = maximumGuardDamageMultiplier;
            DirectGuardDamagePerDamagePoint = directGuardDamagePerDamagePoint;
            DirectBlockStaminaCost = directBlockStaminaCost;
            GuardBreakRecoveryFraction = guardBreakRecoveryFraction;
        }

        /// <summary>
        /// Creates the default balanced guard configuration.
        /// </summary>
        public static GuardProfile CreateDefault()
        {
            return new GuardProfile(
                maxGuard: 100f,
                blockedHealthDamageMultiplier: 0.25f,
                recoveryDelaySeconds: 1.5f,
                recoveryPerSecond: 15f,
                blockImpactSeconds: 0.18f,
                guardBreakSeconds: 0.9f,
                pushbackDistanceMeters: 0.28f,
                impactGuardDamageBonus: 0.75f,
                lowGuardDamageBonus: 0.5f,
                minimumGuardDamageMultiplier: 1f,
                maximumGuardDamageMultiplier: 2.25f,
                directGuardDamagePerDamagePoint: 1f,
                directBlockStaminaCost: 4f,
                guardBreakRecoveryFraction: 0.35f,
                lowGuardThresholdFraction: 0.25f);
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
