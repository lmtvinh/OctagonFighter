using System;
using OctagonFighter.Domain.Combat;
using UnityEngine;

namespace OctagonFighter.Data.Fighters
{
    /// <summary>
    /// Designer-facing authoring values for an <see cref="AttackBook"/>.
    /// </summary>
    [Serializable]
    public struct AttackBookAuthoring
    {
        [SerializeField] private AttackDefinitionAuthoring _light;
        [SerializeField] private AttackDefinitionAuthoring _heavy;

        [Min(0f)]
        [SerializeField] private float _inputBufferSeconds;

        /// <summary>
        /// Initializes a new instance of the <see cref="AttackBookAuthoring"/> struct.
        /// </summary>
        public AttackBookAuthoring(
            AttackDefinitionAuthoring light,
            AttackDefinitionAuthoring heavy,
            float inputBufferSeconds)
        {
            _light = light;
            _heavy = heavy;
            _inputBufferSeconds = inputBufferSeconds;
        }

        /// <summary>
        /// Creates the default MVP strike set.
        /// </summary>
        public static AttackBookAuthoring CreateDefault()
        {
            return new AttackBookAuthoring(
                AttackDefinitionAuthoring.CreateLightDefault(),
                AttackDefinitionAuthoring.CreateHeavyDefault(),
                0.25f);
        }

        /// <summary>
        /// Converts the authoring values into immutable domain data.
        /// </summary>
        public AttackBook ToDomain()
        {
            return new AttackBook(
                _light.ToDomain(),
                _heavy.ToDomain(),
                _inputBufferSeconds);
        }

        /// <summary>
        /// Validates the authoring values.
        /// </summary>
        public bool TryValidate(out string error)
        {
            if (!_light.TryValidate(out string lightError))
            {
                error = $"Light attack is invalid: {lightError}";
                return false;
            }

            if (!_heavy.TryValidate(out string heavyError))
            {
                error = $"Heavy attack is invalid: {heavyError}";
                return false;
            }

            if (!float.IsFinite(_inputBufferSeconds) || _inputBufferSeconds < 0f)
            {
                error = "Input buffer duration must be finite and non-negative.";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}
