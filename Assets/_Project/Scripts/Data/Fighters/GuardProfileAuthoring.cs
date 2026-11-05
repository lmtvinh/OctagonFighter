using System;
using OctagonFighter.Domain.Guard;
using UnityEngine;

namespace OctagonFighter.Data.Fighters
{
    /// <summary>
    /// Designer-facing authoring values for <see cref="GuardProfile"/>.
    /// </summary>
    [Serializable]
    public struct GuardProfileAuthoring
    {
        [Min(0f)]
        [SerializeField] private float _maxGuard;

        [Range(0f, 1f)]
        [SerializeField] private float _blockedHealthDamageMultiplier;

        [Min(0f)]
        [SerializeField] private float _recoveryDelaySeconds;

        [Min(0f)]
        [SerializeField] private float _recoveryPerSecond;

        [Min(0f)]
        [SerializeField] private float _blockImpactSeconds;

        [Min(0f)]
        [SerializeField] private float _guardBreakSeconds;

        [Min(0f)]
        [SerializeField] private float _pushbackDistanceMeters;

        [Min(0f)]
        [SerializeField] private float _impactGuardDamageBonus;

        [Min(0f)]
        [SerializeField] private float _lowGuardDamageBonus;

        [Range(0f, 1f)]
        [SerializeField] private float _lowGuardThresholdFraction;

        [Min(0f)]
        [SerializeField] private float _minimumGuardDamageMultiplier;

        [Min(0f)]
        [SerializeField] private float _maximumGuardDamageMultiplier;

        [Min(0f)]
        [SerializeField] private float _directGuardDamagePerDamagePoint;

        [Min(0f)]
        [SerializeField] private float _directBlockStaminaCost;

        [Range(0f, 1f)]
        [SerializeField] private float _guardBreakRecoveryFraction;

        /// <summary>
        /// Initializes a new instance of the <see cref="GuardProfileAuthoring"/> struct.
        /// </summary>
        public GuardProfileAuthoring(
            float maxGuard,
            float blockedHealthDamageMultiplier,
            float recoveryDelaySeconds,
            float recoveryPerSecond,
            float blockImpactSeconds,
            float guardBreakSeconds,
            float pushbackDistanceMeters,
            float impactGuardDamageBonus,
            float lowGuardDamageBonus,
            float lowGuardThresholdFraction,
            float minimumGuardDamageMultiplier,
            float maximumGuardDamageMultiplier,
            float directGuardDamagePerDamagePoint,
            float directBlockStaminaCost,
            float guardBreakRecoveryFraction)
        {
            _maxGuard = maxGuard;
            _blockedHealthDamageMultiplier = blockedHealthDamageMultiplier;
            _recoveryDelaySeconds = recoveryDelaySeconds;
            _recoveryPerSecond = recoveryPerSecond;
            _blockImpactSeconds = blockImpactSeconds;
            _guardBreakSeconds = guardBreakSeconds;
            _pushbackDistanceMeters = pushbackDistanceMeters;
            _impactGuardDamageBonus = impactGuardDamageBonus;
            _lowGuardDamageBonus = lowGuardDamageBonus;
            _lowGuardThresholdFraction = lowGuardThresholdFraction;
            _minimumGuardDamageMultiplier = minimumGuardDamageMultiplier;
            _maximumGuardDamageMultiplier = maximumGuardDamageMultiplier;
            _directGuardDamagePerDamagePoint = directGuardDamagePerDamagePoint;
            _directBlockStaminaCost = directBlockStaminaCost;
            _guardBreakRecoveryFraction = guardBreakRecoveryFraction;
        }

        /// <summary>
        /// Creates the default balanced guard configuration.
        /// </summary>
        public static GuardProfileAuthoring CreateDefault()
        {
            return new GuardProfileAuthoring(
                maxGuard: 100f,
                blockedHealthDamageMultiplier: 0.25f,
                recoveryDelaySeconds: 1.5f,
                recoveryPerSecond: 15f,
                blockImpactSeconds: 0.18f,
                guardBreakSeconds: 0.9f,
                pushbackDistanceMeters: 0.28f,
                impactGuardDamageBonus: 0.75f,
                lowGuardDamageBonus: 0.5f,
                lowGuardThresholdFraction: 0.25f,
                minimumGuardDamageMultiplier: 1f,
                maximumGuardDamageMultiplier: 2.25f,
                directGuardDamagePerDamagePoint: 1f,
                directBlockStaminaCost: 4f,
                guardBreakRecoveryFraction: 0.35f);
        }

        /// <summary>
        /// Converts the authoring values into immutable domain data.
        /// </summary>
        public GuardProfile ToDomain()
        {
            return new GuardProfile(
                _maxGuard,
                _blockedHealthDamageMultiplier,
                _recoveryDelaySeconds,
                _recoveryPerSecond,
                _blockImpactSeconds,
                _guardBreakSeconds,
                _pushbackDistanceMeters,
                _impactGuardDamageBonus,
                _lowGuardDamageBonus,
                _minimumGuardDamageMultiplier,
                _maximumGuardDamageMultiplier,
                _directGuardDamagePerDamagePoint,
                _directBlockStaminaCost,
                _guardBreakRecoveryFraction,
                _lowGuardThresholdFraction);
        }

        /// <summary>
        /// Validates the authoring values.
        /// </summary>
        public bool TryValidate(out string error)
        {
            if (!float.IsFinite(_maxGuard) || _maxGuard <= 0f)
            {
                error = "Max guard must be finite and positive.";
                return false;
            }

            try
            {
                ToDomain();
            }
            catch (Exception exception) when (
                exception is ArgumentException ||
                exception is ArgumentOutOfRangeException)
            {
                error = exception.Message;
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}
