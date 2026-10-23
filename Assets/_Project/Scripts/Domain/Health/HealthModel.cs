using System;

namespace OctagonFighter.Domain.Health
{
    /// <summary>
    /// Owns health, regional trauma, and knockout state for one fighter.
    /// </summary>
    public sealed class HealthModel : IHealthSource, IDamageable
    {
        private readonly HealthProfile _profile;
        private float _currentHealth;
        private float _headTrauma;
        private float _bodyTrauma;
        private bool _isKnockedOut;

        /// <inheritdoc />
        public float CurrentHealth => _currentHealth;

        /// <inheritdoc />
        public float MaxHealth => _profile.MaxHealth;

        /// <inheritdoc />
        public float HeadTrauma => _headTrauma;

        /// <inheritdoc />
        public float BodyTrauma => _bodyTrauma;

        /// <inheritdoc />
        public bool IsAlive => _currentHealth > 0f && !_isKnockedOut;

        /// <inheritdoc />
        public bool IsKnockedOut => _isKnockedOut;

        /// <inheritdoc />
        public event Action<HealthChangedEvent> Changed;

        /// <inheritdoc />
        public event Action<HealthDepletedEvent> Depleted;

        /// <inheritdoc />
        public event Action<FighterKnockedOutEvent> KnockedOut;

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthModel"/> class.
        /// </summary>
        /// <param name="profile">Validated health configuration.</param>
        public HealthModel(HealthProfile profile)
        {
            _profile = profile;
            _currentHealth = profile.MaxHealth;
        }

        /// <inheritdoc />
        public DamageResult ApplyDamage(in DamageEvent damage)
        {
            if (_isKnockedOut || damage.Amount <= 0f)
            {
                return CreateResult(0f, _currentHealth, false);
            }

            float previousHealth = _currentHealth;
            float previousHeadTrauma = _headTrauma;
            float previousBodyTrauma = _bodyTrauma;

            float appliedAmount = MathF.Min(damage.Amount, previousHealth);
            _currentHealth = MathF.Max(0f, previousHealth - damage.Amount);

            switch (damage.Region)
            {
                case HitRegion.Head:
                    _headTrauma = MathF.Min(_profile.HeadTraumaLimit, _headTrauma + damage.Amount);
                    break;

                case HitRegion.Body:
                    _bodyTrauma = MathF.Min(_profile.BodyTraumaLimit, _bodyTrauma + damage.Amount);
                    break;
            }

            bool wasKnockedOut = _isKnockedOut;
            _isKnockedOut = _currentHealth <= 0f || _headTrauma >= _profile.KnockoutThreshold;
            bool becameKnockedOut = !wasKnockedOut && _isKnockedOut;

            var changedEvent = new HealthChangedEvent(
                previousHealth,
                _currentHealth,
                previousHeadTrauma,
                _headTrauma,
                previousBodyTrauma,
                _bodyTrauma);
            Changed?.Invoke(changedEvent);

            if (previousHealth > 0f && _currentHealth <= 0f)
            {
                Depleted?.Invoke(new HealthDepletedEvent(damage));
            }

            if (becameKnockedOut)
            {
                KnockedOut?.Invoke(new FighterKnockedOutEvent(damage));
            }

            return CreateResult(appliedAmount, previousHealth, becameKnockedOut);
        }

        private DamageResult CreateResult(float appliedAmount, float previousHealth, bool becameKnockedOut)
        {
            return new DamageResult(
                appliedAmount,
                previousHealth,
                _currentHealth,
                _headTrauma,
                _bodyTrauma,
                becameKnockedOut);
        }
    }
}
