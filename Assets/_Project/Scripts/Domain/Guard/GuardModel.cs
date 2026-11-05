using System;

namespace OctagonFighter.Domain.Guard
{
    /// <summary>
    /// Owns the guard resource, dynamic guard damage, and delayed recovery for one fighter.
    /// </summary>
    public sealed class GuardModel : IGuardSource
    {
        private const float ComparisonTolerance = 0.000001f;

        private readonly GuardProfile _profile;
        private float _currentGuard;
        private float _recoveryDelayRemaining;
        private bool _isBroken;

        /// <summary>
        /// Initializes a new instance of the <see cref="GuardModel"/> class.
        /// </summary>
        /// <param name="profile">Validated guard configuration.</param>
        public GuardModel(GuardProfile profile)
        {
            _profile = profile;
            _currentGuard = profile.MaxGuard;
        }

        /// <inheritdoc />
        public float CurrentGuard => _currentGuard;

        /// <inheritdoc />
        public float MaxGuard => _profile.MaxGuard;

        /// <inheritdoc />
        public bool IsBroken => _isBroken;

        /// <summary>
        /// Gets the remaining delay before recovery resumes.
        /// </summary>
        public float RecoveryDelayRemaining => _recoveryDelayRemaining;

        /// <inheritdoc />
        public event Action<GuardChangedEvent> Changed;

        /// <inheritdoc />
        public event Action<GuardBrokenEvent> Broken;

        /// <summary>
        /// Applies guard damage from a blocked strike.
        /// </summary>
        /// <param name="guardDamage">The strike's authored guard damage.</param>
        /// <param name="impactStrength">Normalized impact strength within [0, 1].</param>
        /// <returns>The resolved guard damage.</returns>
        public GuardDamageResult ApplyGuardDamage(float guardDamage, float impactStrength)
        {
            if (_isBroken)
            {
                return new GuardDamageResult(
                    _currentGuard,
                    _currentGuard,
                    0f,
                    _profile.MinimumGuardDamageMultiplier,
                    false,
                    true);
            }

            float previousGuard = _currentGuard;
            float multiplier = CalculateMultiplier(previousGuard, impactStrength);
            float requestedDamage = MathF.Max(0f, guardDamage) * multiplier;
            float appliedDamage = MathF.Min(requestedDamage, previousGuard);
            _currentGuard = MathF.Max(0f, previousGuard - appliedDamage);
            _recoveryDelayRemaining = _profile.RecoveryDelaySeconds;

            bool becameBroken = appliedDamage > ComparisonTolerance && _currentGuard <= ComparisonTolerance;
            if (becameBroken)
            {
                _currentGuard = 0f;
                _isBroken = true;
            }

            if (MathF.Abs(previousGuard - _currentGuard) > ComparisonTolerance)
            {
                RaiseChanged(previousGuard);
            }

            if (becameBroken)
            {
                Broken?.Invoke(new GuardBrokenEvent(appliedDamage, multiplier));
            }

            return new GuardDamageResult(
                previousGuard,
                _currentGuard,
                appliedDamage,
                multiplier,
                becameBroken,
                _isBroken);
        }

        /// <summary>
        /// Advances guard recovery by one simulation step.
        /// </summary>
        /// <param name="deltaSeconds">Elapsed simulation time.</param>
        /// <param name="recoveryPaused">
        /// Whether recovery is currently paused because the fighter is blocking or in block impact.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="deltaSeconds"/> is invalid.</exception>
        public void Tick(float deltaSeconds, bool recoveryPaused)
        {
            if (!float.IsFinite(deltaSeconds) || deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Delta time must be finite and non-negative.");
            }

            if (deltaSeconds <= 0f || _isBroken)
            {
                return;
            }

            if (recoveryPaused)
            {
                // Guard must not regenerate behind a held block, and repeatedly toggling the
                // guard must not be a faster recovery path: every paused tick restarts the delay.
                _recoveryDelayRemaining = _profile.RecoveryDelaySeconds;
                return;
            }

            float previousGuard = _currentGuard;
            float remainingTime = deltaSeconds;

            if (_recoveryDelayRemaining > 0f)
            {
                float delayConsumed = MathF.Min(_recoveryDelayRemaining, remainingTime);
                _recoveryDelayRemaining -= delayConsumed;
                remainingTime -= delayConsumed;
            }

            if (remainingTime > 0f && _recoveryDelayRemaining <= 0f && _currentGuard < _profile.MaxGuard)
            {
                float recoveryAmount = _profile.RecoveryPerSecond * remainingTime;
                _currentGuard = MathF.Min(_profile.MaxGuard, _currentGuard + recoveryAmount);
            }

            if (MathF.Abs(previousGuard - _currentGuard) > ComparisonTolerance)
            {
                RaiseChanged(previousGuard);
            }
        }

        /// <summary>
        /// Restores the post-break guard fraction when the guard-break stun ends.
        /// </summary>
        public void CompleteGuardBreak()
        {
            if (!_isBroken)
            {
                return;
            }

            float previousGuard = _currentGuard;
            _isBroken = false;
            _currentGuard = _profile.MaxGuard * _profile.GuardBreakRecoveryFraction;
            _recoveryDelayRemaining = _profile.RecoveryDelaySeconds;
            RaiseChanged(previousGuard);
        }

        private float CalculateMultiplier(float currentGuard, float impactStrength)
        {
            float normalizedGuard = currentGuard / _profile.MaxGuard;
            float lowGuardFactor = normalizedGuard <= _profile.LowGuardThresholdFraction
                ? 1f - normalizedGuard
                : 0f;
            float clampedImpact = MathF.Max(0f, MathF.Min(1f, impactStrength));
            float multiplier =
                1f +
                (_profile.LowGuardDamageBonus * lowGuardFactor) +
                (_profile.ImpactGuardDamageBonus * clampedImpact);

            return MathF.Max(
                _profile.MinimumGuardDamageMultiplier,
                MathF.Min(_profile.MaximumGuardDamageMultiplier, multiplier));
        }

        private void RaiseChanged(float previousGuard)
        {
            Changed?.Invoke(new GuardChangedEvent(
                previousGuard,
                _currentGuard,
                _profile.MaxGuard,
                _recoveryDelayRemaining));
        }
    }
}
