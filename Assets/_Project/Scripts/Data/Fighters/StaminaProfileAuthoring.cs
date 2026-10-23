using System;
using OctagonFighter.Domain.Stamina;
using UnityEngine;

namespace OctagonFighter.Data.Fighters
{
    /// <summary>
    /// Designer-facing authoring values for <see cref="StaminaProfile"/>.
    /// </summary>
    [Serializable]
    public struct StaminaProfileAuthoring
    {
        [Min(0f)]
        [SerializeField] private float _maxStamina;

        [Min(0f)]
        [SerializeField] private float _regenPerSecond;

        [Min(0f)]
        [SerializeField] private float _regenDelaySeconds;

        [SerializeField] private StaminaTierThresholdsAuthoring _tiers;

        [SerializeField] private StaminaTierModifiersAuthoring _tiredModifiers;

        [SerializeField] private StaminaTierModifiersAuthoring _gassedModifiers;

        /// <summary>
        /// Initializes a new instance of the <see cref="StaminaProfileAuthoring"/> struct.
        /// </summary>
        public StaminaProfileAuthoring(
            float maxStamina,
            float regenPerSecond,
            float regenDelaySeconds,
            StaminaTierThresholdsAuthoring tiers,
            StaminaTierModifiersAuthoring tiredModifiers,
            StaminaTierModifiersAuthoring gassedModifiers)
        {
            _maxStamina = maxStamina;
            _regenPerSecond = regenPerSecond;
            _regenDelaySeconds = regenDelaySeconds;
            _tiers = tiers;
            _tiredModifiers = tiredModifiers;
            _gassedModifiers = gassedModifiers;
        }

        /// <summary>
        /// Creates the default authoring values for a playable prototype fighter.
        /// </summary>
        public static StaminaProfileAuthoring CreateDefault()
        {
            return new StaminaProfileAuthoring(
                100f,
                8f,
                2f,
                StaminaTierThresholdsAuthoring.CreateDefault(),
                new StaminaTierModifiersAuthoring(0.85f, 0.85f, 0.9f, 0.75f),
                new StaminaTierModifiersAuthoring(0.65f, 0.7f, 0.75f, 0.5f));
        }

        /// <summary>
        /// Converts the authoring values into immutable domain data.
        /// </summary>
        public StaminaProfile ToDomain()
        {
            return new StaminaProfile(
                _maxStamina,
                _regenPerSecond,
                _regenDelaySeconds,
                _tiers.ToDomain(),
                _tiredModifiers.ToDomain(),
                _gassedModifiers.ToDomain());
        }

        /// <summary>
        /// Validates the authoring values.
        /// </summary>
        public bool TryValidate(out string error)
        {
            if (!float.IsFinite(_maxStamina) || _maxStamina <= 0f)
            {
                error = "Max stamina must be finite and positive.";
                return false;
            }

            if (!float.IsFinite(_regenPerSecond) || _regenPerSecond < 0f)
            {
                error = "Stamina regeneration must be finite and non-negative.";
                return false;
            }

            if (!float.IsFinite(_regenDelaySeconds) || _regenDelaySeconds < 0f)
            {
                error = "Regeneration delay must be finite and non-negative.";
                return false;
            }

            if (!_tiers.TryValidate(out error))
            {
                return false;
            }

            if (!_tiredModifiers.TryValidate("Tired", out error))
            {
                return false;
            }

            if (!_gassedModifiers.TryValidate("Gassed", out error))
            {
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}
