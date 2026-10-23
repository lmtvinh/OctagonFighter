using System;
using OctagonFighter.Domain.Fighters;
using UnityEngine;

namespace OctagonFighter.Data.Fighters
{
    /// <summary>
    /// Designer-facing authoring values for <see cref="BodyProfile"/>.
    /// </summary>
    [Serializable]
    public struct BodyProfileAuthoring
    {
        [Min(0f)]
        [SerializeField] private float _heightMeters;

        [Min(0f)]
        [SerializeField] private float _reachMeters;

        [SerializeField] private string _weightClass;

        [SerializeField] private FighterStance _stance;

        /// <summary>
        /// Initializes a new instance of the <see cref="BodyProfileAuthoring"/> struct.
        /// </summary>
        public BodyProfileAuthoring(
            float heightMeters,
            float reachMeters,
            string weightClass,
            FighterStance stance)
        {
            _heightMeters = heightMeters;
            _reachMeters = reachMeters;
            _weightClass = weightClass;
            _stance = stance;
        }

        /// <summary>
        /// Creates the default authoring values for a playable prototype fighter.
        /// </summary>
        public static BodyProfileAuthoring CreateDefault()
        {
            return new BodyProfileAuthoring(1.8f, 1.85f, "Welterweight", FighterStance.Orthodox);
        }

        /// <summary>
        /// Converts the authoring values into immutable domain data.
        /// </summary>
        public BodyProfile ToDomain()
        {
            return new BodyProfile(_heightMeters, _reachMeters, _weightClass, _stance);
        }

        /// <summary>
        /// Validates the authoring values.
        /// </summary>
        public bool TryValidate(out string error)
        {
            if (!float.IsFinite(_heightMeters) || _heightMeters <= 0f)
            {
                error = "Height must be finite and positive.";
                return false;
            }

            if (!float.IsFinite(_reachMeters) || _reachMeters <= 0f)
            {
                error = "Reach must be finite and positive.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(_weightClass))
            {
                error = "Weight class must be specified.";
                return false;
            }

            if (!Enum.IsDefined(typeof(FighterStance), _stance))
            {
                error = "Stance is not defined.";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}
