using System;
using OctagonFighter.Domain.Stamina;
using UnityEngine;

namespace OctagonFighter.Infrastructure.Stamina
{
    /// <summary>
    /// Unity adapter that exposes a <see cref="StaminaModel"/> to the scene.
    /// </summary>
    public sealed class StaminaComponent : MonoBehaviour, IStaminaSource
    {
        private StaminaModel _model;

        /// <summary>
        /// Gets a value indicating whether the component has been initialized.
        /// </summary>
        public bool IsInitialized => _model != null;

        /// <summary>
        /// Gets the owned domain model after initialization.
        /// </summary>
        public StaminaModel Model
        {
            get
            {
                EnsureInitialized();
                return _model;
            }
        }

        /// <inheritdoc />
        public float CurrentStamina
        {
            get
            {
                EnsureInitialized();
                return _model.CurrentStamina;
            }
        }

        /// <inheritdoc />
        public float MaxStamina
        {
            get
            {
                EnsureInitialized();
                return _model.MaxStamina;
            }
        }

        /// <inheritdoc />
        public StaminaTier Tier
        {
            get
            {
                EnsureInitialized();
                return _model.Tier;
            }
        }

        /// <inheritdoc />
        public event Action<StaminaChangedEvent> Changed;

        /// <inheritdoc />
        public event Action<StaminaTierChangedEvent> TierChanged;

        /// <inheritdoc />
        public event Action<StaminaDepletedEvent> Depleted;

        /// <summary>
        /// Creates the domain model from validated profile data.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the component is already initialized.</exception>
        public void Initialize(StaminaProfile profile)
        {
            if (_model != null)
            {
                throw new InvalidOperationException("Stamina component is already initialized.");
            }

            _model = new StaminaModel(profile);
            _model.Changed += OnModelChanged;
            _model.TierChanged += OnModelTierChanged;
            _model.Depleted += OnModelDepleted;
        }

        /// <inheritdoc />
        public bool CanAfford(in StaminaCost cost)
        {
            EnsureInitialized();
            return _model.CanAfford(cost);
        }

        /// <inheritdoc />
        public bool TrySpend(in StaminaCost cost)
        {
            EnsureInitialized();
            return _model.TrySpend(cost);
        }

        /// <summary>
        /// Advances regeneration by one simulation tick.
        /// </summary>
        public void Tick(float deltaSeconds)
        {
            EnsureInitialized();
            _model.Tick(deltaSeconds);
        }

        private void OnDestroy()
        {
            if (_model == null)
            {
                return;
            }

            _model.Changed -= OnModelChanged;
            _model.TierChanged -= OnModelTierChanged;
            _model.Depleted -= OnModelDepleted;
            _model = null;
        }

        private void OnModelChanged(StaminaChangedEvent changedEvent)
        {
            Changed?.Invoke(changedEvent);
        }

        private void OnModelTierChanged(StaminaTierChangedEvent tierChangedEvent)
        {
            TierChanged?.Invoke(tierChangedEvent);
        }

        private void OnModelDepleted(StaminaDepletedEvent depletedEvent)
        {
            Depleted?.Invoke(depletedEvent);
        }

        private void EnsureInitialized()
        {
            if (_model == null)
            {
                throw new InvalidOperationException("Stamina component has not been initialized.");
            }
        }
    }
}
