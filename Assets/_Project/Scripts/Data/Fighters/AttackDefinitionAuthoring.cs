using System;
using OctagonFighter.Domain.Combat;
using OctagonFighter.Domain.Health;
using UnityEngine;

namespace OctagonFighter.Data.Fighters
{
    /// <summary>
    /// Designer-facing authoring values for one <see cref="AttackDefinition"/>.
    /// </summary>
    [Serializable]
    public struct AttackDefinitionAuthoring
    {
        [SerializeField] private string _displayName;

        [SerializeField] private string _animationState;

        [Min(0f)]
        [SerializeField] private float _startupSeconds;

        [Min(0f)]
        [SerializeField] private float _activeSeconds;

        [Min(0f)]
        [SerializeField] private float _recoverySeconds;

        [Min(0f)]
        [SerializeField] private float _cooldownSeconds;

        [Min(0f)]
        [SerializeField] private float _staminaCost;

        [Min(0f)]
        [SerializeField] private float _damage;

        [Min(0f)]
        [SerializeField] private float _range;

        [Min(0f)]
        [SerializeField] private float _radius;

        [SerializeField] private HitRegion _region;

        [SerializeField] private StrikeHand _hand;

        /// <summary>
        /// Initializes a new instance of the <see cref="AttackDefinitionAuthoring"/> struct.
        /// </summary>
        public AttackDefinitionAuthoring(
            string displayName,
            string animationState,
            float startupSeconds,
            float activeSeconds,
            float recoverySeconds,
            float cooldownSeconds,
            float staminaCost,
            float damage,
            float range,
            float radius,
            HitRegion region,
            StrikeHand hand)
        {
            _displayName = displayName;
            _animationState = animationState;
            _startupSeconds = startupSeconds;
            _activeSeconds = activeSeconds;
            _recoverySeconds = recoverySeconds;
            _cooldownSeconds = cooldownSeconds;
            _staminaCost = staminaCost;
            _damage = damage;
            _range = range;
            _radius = radius;
            _region = region;
            _hand = hand;
        }

        /// <summary>
        /// Creates the default light strike (jab).
        /// </summary>
        public static AttackDefinitionAuthoring CreateLightDefault()
        {
            return new AttackDefinitionAuthoring(
                "Jab",
                "Jab",
                0.26f,
                0.16f,
                0.16f,
                0.35f,
                6f,
                8f,
                0.95f,
                0.06f,
                HitRegion.Head,
                StrikeHand.Left);
        }

        /// <summary>
        /// Creates the default heavy strike (cross).
        /// </summary>
        public static AttackDefinitionAuthoring CreateHeavyDefault()
        {
            return new AttackDefinitionAuthoring(
                "Cross",
                "Cross",
                0.60f,
                0.18f,
                0.14f,
                0.75f,
                12f,
                15f,
                1.05f,
                0.07f,
                HitRegion.Head,
                StrikeHand.Right);
        }

        /// <summary>
        /// Converts the authoring values into immutable domain data.
        /// </summary>
        public AttackDefinition ToDomain()
        {
            return new AttackDefinition(
                _displayName,
                _animationState,
                _startupSeconds,
                _activeSeconds,
                _recoverySeconds,
                _cooldownSeconds,
                _staminaCost,
                _damage,
                _range,
                _radius,
                _region,
                _hand);
        }

        /// <summary>
        /// Validates the authoring values.
        /// </summary>
        public bool TryValidate(out string error)
        {
            try
            {
                ToDomain();
            }
            catch (Exception exception) when (
                exception is ArgumentException ||
                exception is ArgumentOutOfRangeException)
            {
                error = exception.Message;
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}
