using System;
using OctagonFighter.Domain.Stamina;
using UnityEngine;

namespace OctagonFighter.Data.Fighters
{
    /// <summary>
    /// Designer-facing authoring values for <see cref="StaminaTierThresholds"/>.
    /// </summary>
    [Serializable]
    public struct StaminaTierThresholdsAuthoring
    {
        [Range(0f, 1f)]
        [SerializeField] private float _freshThreshold;

        [Range(0f, 1f)]
        [SerializeField] private float _workingThreshold;

        [Range(0f, 1f)]
        [SerializeField] private float _tiredThreshold;

        /// <summary>
        /// Initializes a new instance of the <see cref="StaminaTierThresholdsAuthoring"/> struct.
        /// </summary>
        public StaminaTierThresholdsAuthoring(
            float freshThreshold,
            float workingThreshold,
            float tiredThreshold)
        {
            _freshThreshold = freshThreshold;
            _workingThreshold = workingThreshold;
            _tiredThreshold = tiredThreshold;
        }

        /// <summary>
        /// Creates the default authoring values for a playable prototype fighter.
        /// </summary>
        public static StaminaTierThresholdsAuthoring CreateDefault()
        {
            return new StaminaTierThresholdsAuthoring(0.75f, 0.5f, 0.2f);
        }

        /// <summary>
        /// Converts the authoring values into immutable domain data.
        /// </summary>
        public StaminaTierThresholds ToDomain()
        {
            return new StaminaTierThresholds(_freshThreshold, _workingThreshold, _tiredThreshold);
        }

        /// <summary>
        /// Validates the authoring values.
        /// </summary>
        public bool TryValidate(out string error)
        {
            if (!IsValidThreshold(_freshThreshold) ||
                !IsValidThreshold(_workingThreshold) ||
                !IsValidThreshold(_tiredThreshold) ||
                _freshThreshold <= _workingThreshold ||
                _workingThreshold <= _tiredThreshold)
            {
                error = "Stamina thresholds must be within (0, 1) and strictly descending.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static bool IsValidThreshold(float value)
        {
            return float.IsFinite(value) && value > 0f && value < 1f;
        }
    }
}
