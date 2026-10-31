using System;
using OctagonFighter.Domain.Health;
using UnityEngine;

namespace OctagonFighter.Infrastructure.Health
{
    /// <summary>
    /// Unity adapter that exposes a <see cref="HealthModel"/> to the scene.
    /// </summary>
    public sealed class HealthComponent : MonoBehaviour, IDamageable, IHealthSource
    {
        private HealthModel _model;

        /// <summary>
        /// Gets a value indicating whether the component has been initialized.
        /// </summary>
        public bool IsInitialized => _model != null;

        /// <summary>
        /// Gets the owned domain model after initialization.
        /// </summary>
        public HealthModel Model
        {
            get
            {
                EnsureInitialized();
                return _model;
            }
        }

        /// <inheritdoc />
        public float CurrentHealth
        {
            get
            {
                EnsureInitialized();
                return _model.CurrentHealth;
            }
        }

        /// <inheritdoc />
        public float MaxHealth
        {
            get
            {
                EnsureInitialized();
                return _model.MaxHealth;
            }
        }

        /// <inheritdoc />
        public float HeadTrauma
        {
            get
            {
                EnsureInitialized();
                return _model.HeadTrauma;
            }
        }

        /// <inheritdoc />
        public float BodyTrauma
        {
            get
            {
                EnsureInitialized();
                return _model.BodyTrauma;
            }
        }

        /// <inheritdoc />
        public bool IsAlive
        {
            get
            {
                EnsureInitialized();
                return _model.IsAlive;
            }
        }

        /// <inheritdoc />
        public bool IsKnockedOut
        {
            get
            {
                EnsureInitialized();
                return _model.IsKnockedOut;
            }
        }

        /// <inheritdoc />
        public event Action<HealthChangedEvent> Changed;

        /// <inheritdoc />
        public event Action<HealthDepletedEvent> Depleted;

        /// <inheritdoc />
        public event Action<FighterKnockedOutEvent> KnockedOut;

        /// <summary>
        /// Occurs when an impact hit actually removes health, carrying the resolved damage so
        /// listeners know the attacker, the region, and the tick.
        /// </summary>
        /// <remarks>
        /// Zero-damage or already-defeated applications do not raise this event, which is what keeps
        /// a missed strike from ever starting a hit reaction.
        /// </remarks>
        public event Action<DamageEvent> Damaged;

        /// <summary>
        /// Creates the domain model from validated profile data.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the component is already initialized.</exception>
        public void Initialize(HealthProfile profile)
        {
            if (_model != null)
            {
                throw new InvalidOperationException("Health component is already initialized.");
            }

            _model = new HealthModel(profile);
            _model.Changed += OnModelChanged;
            _model.Depleted += OnModelDepleted;
            _model.KnockedOut += OnModelKnockedOut;
        }

        /// <inheritdoc />
        public DamageResult ApplyDamage(in DamageEvent damage)
        {
            EnsureInitialized();
            DamageResult result = _model.ApplyDamage(damage);

            if (result.AppliedAmount > 0f && damage.Type == DamageType.Impact)
            {
                Damaged?.Invoke(damage);
            }

            return result;
        }

        private void OnDestroy()
        {
            if (_model == null)
            {
                return;
            }

            _model.Changed -= OnModelChanged;
            _model.Depleted -= OnModelDepleted;
            _model.KnockedOut -= OnModelKnockedOut;
            _model = null;
        }

        private void OnModelChanged(HealthChangedEvent changedEvent)
        {
            Changed?.Invoke(changedEvent);
        }

        private void OnModelDepleted(HealthDepletedEvent depletedEvent)
        {
            Depleted?.Invoke(depletedEvent);
        }

        private void OnModelKnockedOut(FighterKnockedOutEvent knockedOutEvent)
        {
            KnockedOut?.Invoke(knockedOutEvent);
        }

        private void EnsureInitialized()
        {
            if (_model == null)
            {
                throw new InvalidOperationException("Health component has not been initialized.");
            }
        }
    }
}
