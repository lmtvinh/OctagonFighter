using System;
using System.Text;
using OctagonFighter.Core.Ids;
using OctagonFighter.Domain.Fighters;
using UnityEngine;

namespace OctagonFighter.Data.Fighters
{
    /// <summary>
    /// Authored, immutable-at-runtime definition of a fighter.
    /// </summary>
    [CreateAssetMenu(
        menuName = "OctagonFighter/Fighters/Fighter Definition",
        fileName = "FD_Fighter")]
    public sealed class FighterDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _id = "fighter_01";
        [SerializeField] private string _displayName = "New Fighter";

        [Header("Attributes")]
        [SerializeField] private FighterAttributesAuthoring _attributes =
            FighterAttributesAuthoring.CreateDefault();

        [Header("Body")]
        [SerializeField] private BodyProfileAuthoring _body = BodyProfileAuthoring.CreateDefault();

        [Header("Health")]
        [SerializeField] private HealthProfileAuthoring _health = HealthProfileAuthoring.CreateDefault();

        [Header("Stamina")]
        [SerializeField] private StaminaProfileAuthoring _stamina = StaminaProfileAuthoring.CreateDefault();

        [Header("Movement")]
        [SerializeField] private MovementProfileAuthoring _movement = MovementProfileAuthoring.CreateDefault();

        [Header("Attacks")]
        [SerializeField] private AttackBookAuthoring _attacks = AttackBookAuthoring.CreateDefault();

        /// <summary>
        /// Gets the stable authored fighter identifier.
        /// </summary>
        public string Id => _id;

        /// <summary>
        /// Gets the authored display name.
        /// </summary>
        public string DisplayName => _displayName;

        /// <summary>
        /// Creates an immutable domain snapshot from this definition.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the definition is invalid.</exception>
        public FighterData CreateRuntimeData()
        {
            if (!TryValidate(out string error))
            {
                throw new InvalidOperationException(
                    $"Fighter definition '{name}' is invalid: {error}");
            }

            try
            {
                return new FighterData(
                    new FighterId(_id),
                    _displayName,
                    _attributes.ToDomain(),
                    _body.ToDomain(),
                    _health.ToDomain(),
                    _stamina.ToDomain(),
                    _movement.ToDomain(),
                    _attacks.ToDomain());
            }
            catch (Exception exception) when (
                exception is ArgumentException ||
                exception is ArgumentOutOfRangeException)
            {
                throw new InvalidOperationException(
                    $"Fighter definition '{name}' could not be converted to runtime data: {exception.Message}",
                    exception);
            }
        }

        /// <summary>
        /// Validates every authored profile.
        /// </summary>
        public bool TryValidate(out string error)
        {
            var errors = new StringBuilder();

            if (string.IsNullOrWhiteSpace(_id))
            {
                errors.Append("Fighter ID must not be empty. ");
            }

            if (string.IsNullOrWhiteSpace(_displayName))
            {
                errors.Append("Display name must not be empty. ");
            }

            if (!_attributes.TryValidate(out string attributeError))
            {
                errors.Append(attributeError).Append(' ');
            }

            if (!_body.TryValidate(out string bodyError))
            {
                errors.Append(bodyError).Append(' ');
            }

            if (!_health.TryValidate(out string healthError))
            {
                errors.Append(healthError).Append(' ');
            }

            if (!_stamina.TryValidate(out string staminaError))
            {
                errors.Append(staminaError).Append(' ');
            }

            if (!_movement.TryValidate(out string movementError))
            {
                errors.Append(movementError).Append(' ');
            }

            if (!_attacks.TryValidate(out string attackError))
            {
                errors.Append(attackError).Append(' ');
            }

            error = errors.ToString().Trim();
            return error.Length == 0;
        }

        private void OnValidate()
        {
            if (!TryValidate(out string error))
            {
                Debug.LogError($"Fighter definition '{name}' is invalid: {error}", this);
            }
        }
    }
}
