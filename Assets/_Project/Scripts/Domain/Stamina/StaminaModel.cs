using System;

namespace OctagonFighter.Domain.Stamina
{
    /// <summary>
    /// Owns stamina, regeneration delay, and fatigue tier state for one fighter.
    /// </summary>
    public sealed class StaminaModel : IStaminaSource
    {
        private readonly StaminaProfile _profile;
        private float _currentStamina;
        private float _recoveryDelayRemaining;
        private StaminaTier _tier;

        /// <inheritdoc />
        public float CurrentStamina => _currentStamina;

        /// <inheritdoc />
        public float MaxStamina => _profile.MaxStamina;

        /// <inheritdoc />
        public StaminaTier Tier => _tier;

        /// <summary>
        /// Gets the remaining delay before regeneration resumes.
        /// </summary>
        public float RecoveryDelayRemaining => _recoveryDelayRemaining;

        /// <summary>
        /// Gets a value indicating whether stamina is currently regenerating.
        /// </summary>
        public bool IsRegenerating => _recoveryDelayRemaining <= 0f && _currentStamina < _profile.MaxStamina;

        /// <summary>
        /// Gets the modifiers for the current fatigue tier.
        /// </summary>
        public StaminaTierModifiers CurrentModifiers
        {
            get
            {
                switch (_tier)
                {
                    case StaminaTier.Tired:
                        return _profile.TiredModifiers;

                    case StaminaTier.Gassed:
                        return _profile.GassedModifiers;

                    default:
                        return StaminaTierModifiers.Default;
                }
            }
        }

        /// <inheritdoc />
        public event Action<StaminaChangedEvent> Changed;

        /// <inheritdoc />
        public event Action<StaminaTierChangedEvent> TierChanged;

        /// <inheritdoc />
        public event Action<StaminaDepletedEvent> Depleted;

        /// <summary>
        /// Initializes a new instance of the <see cref="StaminaModel"/> class.
        /// </summary>
        /// <param name="profile">Validated stamina configuration.</param>
        public StaminaModel(StaminaProfile profile)
        {
            _profile = profile;
            _currentStamina = profile.MaxStamina;
            _tier = profile.Tiers.GetTier(1f);
        }

        /// <inheritdoc />
        public bool CanAfford(in StaminaCost cost)
        {
            return _currentStamina >= cost.Amount;
        }

        /// <inheritdoc />
        public bool TrySpend(in StaminaCost cost)
        {
            if (!CanAfford(cost))
            {
                return false;
            }

            if (cost.Amount <= 0f)
            {
                return true;
            }

            float previousStamina = _currentStamina;
            _currentStamina = MathF.Max(0f, previousStamina - cost.Amount);
            _recoveryDelayRemaining = MathF.Max(_profile.RegenDelaySeconds, cost.RecoveryDelaySeconds);

            RaiseChanged(previousStamina);
            UpdateTier();

            if (previousStamina > 0f && _currentStamina <= 0f)
            {
                Depleted?.Invoke(new StaminaDepletedEvent(cost));
            }

            return true;
        }

        /// <summary>
        /// Advances regeneration for one simulation step.
        /// </summary>
        /// <param name="deltaSeconds">Elapsed simulation time.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="deltaSeconds"/> is invalid.</exception>
        public void Tick(float deltaSeconds)
        {
            if (!float.IsFinite(deltaSeconds) || deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Delta time must be finite and non-negative.");
            }

            if (deltaSeconds <= 0f)
            {
                return;
            }

            float previousStamina = _currentStamina;
            float remainingTime = deltaSeconds;

            if (_recoveryDelayRemaining > 0f)
            {
                float delayConsumed = MathF.Min(_recoveryDelayRemaining, remainingTime);
                _recoveryDelayRemaining -= delayConsumed;
                remainingTime -= delayConsumed;
            }

            if (remainingTime > 0f && _recoveryDelayRemaining <= 0f && _currentStamina < _profile.MaxStamina)
            {
                float regenAmount = _profile.RegenPerSecond * CurrentModifiers.RegenMultiplier * remainingTime;
                _currentStamina = MathF.Min(_profile.MaxStamina, _currentStamina + regenAmount);
            }

            if (!MathF.Approximately(previousStamina, _currentStamina))
            {
                RaiseChanged(previousStamina);
                UpdateTier();
            }
        }

        private void RaiseChanged(float previousStamina)
        {
            Changed?.Invoke(new StaminaChangedEvent(
                previousStamina,
                _currentStamina,
                _profile.MaxStamina,
                _recoveryDelayRemaining));
        }

        private void UpdateTier()
        {
            float normalizedStamina = _currentStamina / _profile.MaxStamina;
            StaminaTier newTier = _profile.Tiers.GetTier(normalizedStamina);
            if (newTier == _tier)
            {
                return;
            }

            StaminaTier previousTier = _tier;
            _tier = newTier;
            TierChanged?.Invoke(new StaminaTierChangedEvent(previousTier, _tier, _currentStamina));
        }
    }
}
