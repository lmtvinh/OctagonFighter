using System;
using OctagonFighter.Domain.Stamina;
using UnityEngine;

namespace OctagonFighter.Data.Fighters
{
    /// <summary>
    /// Designer-facing authoring values for <see cref="StaminaTierModifiers"/>.
    /// </summary>
    [Serializable]
    public struct StaminaTierModifiersAuthoring
    {
        [Min(0.01f)]
        [SerializeField] private float _speedMultiplier;

        [Min(0.01f)]
        [SerializeField] private float _damageMultiplier;

        [Min(0.01f)]
        [SerializeField] private float _defenseMultiplier;

        [Min(0.01f)]
        [SerializeField] private float _regenMultiplier;

        /// <summary>
        /// Initializes a new instance of the <see cref="StaminaTierModifiersAuthoring"/> struct.
        /// </summary>
        public StaminaTierModifiersAuthoring(
            float speedMultiplier,
            float damageMultiplier,
            float defenseMultiplier,
            float regenMultiplier)
        {
            _speedMultiplier = speedMultiplier;
            _damageMultiplier = damageMultiplier;
            _defenseMultiplier = defenseMultiplier;
            _regenMultiplier = regenMultiplier;
        }

        /// <summary>
        /// Creates default modifiers that leave all values unchanged.
        /// </summary>
        public static StaminaTierModifiersAuthoring CreateNeutral()
        {
            return new StaminaTierModifiersAuthoring(1f, 1f, 1f, 1f);
        }

        /// <summary>
        /// Converts the authoring values into immutable domain data.
        /// </summary>
        public StaminaTierModifiers ToDomain()
        {
            return new StaminaTierModifiers(
                _speedMultiplier,
                _damageMultiplier,
                _defenseMultiplier,
                _regenMultiplier);
        }

        /// <summary>
        /// Validates the authoring values.
        /// </summary>
        public bool TryValidate(string fieldName, out string error)
        {
            if (!IsValidMultiplier(_speedMultiplier))
            {
                error = $"{fieldName} speed multiplier must be finite and positive.";
                return false;
            }

            if (!IsValidMultiplier(_damageMultiplier))
            {
                error = $"{fieldName} damage multiplier must be finite and positive.";
                return false;
            }

            if (!IsValidMultiplier(_defenseMultiplier))
            {
                error = $"{fieldName} defense multiplier must be finite and positive.";
                return false;
            }

            if (!IsValidMultiplier(_regenMultiplier))
            {
                error = $"{fieldName} regeneration multiplier must be finite and positive.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static bool IsValidMultiplier(float value)
        {
            return float.IsFinite(value) && value > 0f;
        }
    }
}
