using System;
using OctagonFighter.Domain.Fighters;
using UnityEngine;

namespace OctagonFighter.Data.Fighters
{
    /// <summary>
    /// Designer-facing authoring values for <see cref="MovementProfile"/>.
    /// </summary>
    [Serializable]
    public struct MovementProfileAuthoring
    {
        [Min(0f)]
        [SerializeField] private float _forwardSpeed;

        [Min(0f)]
        [SerializeField] private float _backwardSpeed;

        [Min(0f)]
        [SerializeField] private float _lateralSpeed;

        [Range(0.01f, 1f)]
        [SerializeField] private float _walkSpeedMultiplier;

        [Min(0f)]
        [SerializeField] private float _rotationDegreesPerSecond;

        [Min(0f)]
        [SerializeField] private float _spawnDurationSeconds;

        [Min(0f)]
        [SerializeField] private float _dodgeDurationSeconds;

        [Min(0f)]
        [SerializeField] private float _hitReactionDurationSeconds;

        [Min(0f)]
        [SerializeField] private float _knockdownDurationSeconds;

        [Min(0f)]
        [SerializeField] private float _recoveryDurationSeconds;

        /// <summary>
        /// Initializes a new instance of the <see cref="MovementProfileAuthoring"/> struct.
        /// </summary>
        public MovementProfileAuthoring(
            float forwardSpeed,
            float backwardSpeed,
            float lateralSpeed,
            float spawnDurationSeconds,
            float dodgeDurationSeconds,
            float hitReactionDurationSeconds,
            float knockdownDurationSeconds,
            float recoveryDurationSeconds,
            float walkSpeedMultiplier = 0.55f,
            float rotationDegreesPerSecond = 720f)
        {
            _forwardSpeed = forwardSpeed;
            _backwardSpeed = backwardSpeed;
            _lateralSpeed = lateralSpeed;
            _walkSpeedMultiplier = walkSpeedMultiplier;
            _rotationDegreesPerSecond = rotationDegreesPerSecond;
            _spawnDurationSeconds = spawnDurationSeconds;
            _dodgeDurationSeconds = dodgeDurationSeconds;
            _hitReactionDurationSeconds = hitReactionDurationSeconds;
            _knockdownDurationSeconds = knockdownDurationSeconds;
            _recoveryDurationSeconds = recoveryDurationSeconds;
        }

        /// <summary>
        /// Creates the default authoring values for a playable prototype fighter.
        /// </summary>
        public static MovementProfileAuthoring CreateDefault()
        {
            return new MovementProfileAuthoring(
                2.6f,
                2.1f,
                2.3f,
                0.25f,
                0.4f,
                0.35f,
                1.5f,
                0.8f,
                0.55f,
                720f);
        }

        /// <summary>
        /// Converts the authoring values into immutable domain data.
        /// </summary>
        public MovementProfile ToDomain()
        {
            return new MovementProfile(
                _forwardSpeed,
                _backwardSpeed,
                _lateralSpeed,
                _spawnDurationSeconds,
                _dodgeDurationSeconds,
                _hitReactionDurationSeconds,
                _knockdownDurationSeconds,
                _recoveryDurationSeconds,
                _walkSpeedMultiplier,
                _rotationDegreesPerSecond);
        }

        /// <summary>
        /// Validates the authoring values.
        /// </summary>
        public bool TryValidate(out string error)
        {
            if (!IsValidSpeed(_forwardSpeed))
            {
                error = "Forward speed must be finite and non-negative.";
                return false;
            }

            if (!IsValidSpeed(_backwardSpeed))
            {
                error = "Backward speed must be finite and non-negative.";
                return false;
            }

            if (!IsValidSpeed(_lateralSpeed))
            {
                error = "Lateral speed must be finite and non-negative.";
                return false;
            }

            if (!float.IsFinite(_walkSpeedMultiplier) ||
                _walkSpeedMultiplier <= 0f ||
                _walkSpeedMultiplier > 1f)
            {
                error = "Walk speed multiplier must be finite and within (0, 1].";
                return false;
            }

            if (!float.IsFinite(_rotationDegreesPerSecond) || _rotationDegreesPerSecond <= 0f)
            {
                error = "Rotation speed must be finite and positive.";
                return false;
            }

            if (!IsValidDuration(_spawnDurationSeconds))
            {
                error = "Spawn duration must be finite and non-negative.";
                return false;
            }

            if (!IsValidDuration(_dodgeDurationSeconds))
            {
                error = "Dodge duration must be finite and non-negative.";
                return false;
            }

            if (!IsValidDuration(_hitReactionDurationSeconds))
            {
                error = "Hit reaction duration must be finite and non-negative.";
                return false;
            }

            if (!IsValidDuration(_knockdownDurationSeconds))
            {
                error = "Knockdown duration must be finite and non-negative.";
                return false;
            }

            if (!IsValidDuration(_recoveryDurationSeconds))
            {
                error = "Recovery duration must be finite and non-negative.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static bool IsValidSpeed(float value)
        {
            return float.IsFinite(value) && value >= 0f;
        }

        private static bool IsValidDuration(float value)
        {
            return float.IsFinite(value) && value >= 0f;
        }
    }
}
