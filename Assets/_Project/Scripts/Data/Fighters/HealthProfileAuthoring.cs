using System;
using OctagonFighter.Domain.Health;
using UnityEngine;

namespace OctagonFighter.Data.Fighters
{
    /// <summary>
    /// Designer-facing authoring values for <see cref="HealthProfile"/>.
    /// </summary>
    [Serializable]
    public struct HealthProfileAuthoring
    {
        [Min(0f)]
        [SerializeField] private float _maxHealth;

        [Min(0f)]
        [SerializeField] private float _headTraumaLimit;

        [Min(0f)]
        [SerializeField] private float _bodyTraumaLimit;

        [Min(0f)]
        [SerializeField] private float _knockoutThreshold;

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthProfileAuthoring"/> struct.
        /// </summary>
        public HealthProfileAuthoring(
            float maxHealth,
            float headTraumaLimit,
            float bodyTraumaLimit,
            float knockoutThreshold)
        {
            _maxHealth = maxHealth;
            _headTraumaLimit = headTraumaLimit;
            _bodyTraumaLimit = bodyTraumaLimit;
            _knockoutThreshold = knockoutThreshold;
        }

        /// <summary>
        /// Creates the default authoring values for a playable prototype fighter.
        /// </summary>
        public static HealthProfileAuthoring CreateDefault()
        {
            return new HealthProfileAuthoring(100f, 100f, 100f, 100f);
        }

        /// <summary>
        /// Converts the authoring values into immutable domain data.
        /// </summary>
        public HealthProfile ToDomain()
        {
            return new HealthProfile(
                _maxHealth,
                _headTraumaLimit,
                _bodyTraumaLimit,
                _knockoutThreshold);
        }

        /// <summary>
        /// Validates the authoring values.
        /// </summary>
        public bool TryValidate(out string error)
        {
            if (!float.IsFinite(_maxHealth) || _maxHealth <= 0f)
            {
                error = "Max health must be finite and positive.";
                return false;
            }

            if (!float.IsFinite(_headTraumaLimit) || _headTraumaLimit <= 0f)
            {
                error = "Head trauma limit must be finite and positive.";
                return false;
            }

            if (!float.IsFinite(_bodyTraumaLimit) || _bodyTraumaLimit <= 0f)
            {
                error = "Body trauma limit must be finite and positive.";
                return false;
            }

            if (!float.IsFinite(_knockoutThreshold) ||
                _knockoutThreshold <= 0f ||
                _knockoutThreshold > _headTraumaLimit)
            {
                error = "Knockout threshold must be finite, positive, and no greater than the head trauma limit.";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}
