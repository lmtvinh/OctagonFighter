using System;
using OctagonFighter.Domain.Fighters;
using UnityEngine;

namespace OctagonFighter.Data.Fighters
{
    /// <summary>
    /// Designer-facing authoring values for <see cref="FighterAttributes"/>.
    /// </summary>
    [Serializable]
    public struct FighterAttributesAuthoring
    {
        [Range(1, 100)]
        [SerializeField] private int _strength;

        [Range(1, 100)]
        [SerializeField] private int _speed;

        [Range(1, 100)]
        [SerializeField] private int _cardio;

        [Range(1, 100)]
        [SerializeField] private int _durability;

        [Range(1, 100)]
        [SerializeField] private int _flexibility;

        [Range(1, 100)]
        [SerializeField] private int _composure;

        [Range(1, 100)]
        [SerializeField] private int _fightIq;

        /// <summary>
        /// Initializes a new instance of the <see cref="FighterAttributesAuthoring"/> struct.
        /// </summary>
        public FighterAttributesAuthoring(
            int strength,
            int speed,
            int cardio,
            int durability,
            int flexibility,
            int composure,
            int fightIq)
        {
            _strength = strength;
            _speed = speed;
            _cardio = cardio;
            _durability = durability;
            _flexibility = flexibility;
            _composure = composure;
            _fightIq = fightIq;
        }

        /// <summary>
        /// Creates the default authoring values for a playable prototype fighter.
        /// </summary>
        public static FighterAttributesAuthoring CreateDefault()
        {
            return new FighterAttributesAuthoring(65, 65, 65, 65, 60, 60, 60);
        }

        /// <summary>
        /// Converts the authoring values into immutable domain data.
        /// </summary>
        public FighterAttributes ToDomain()
        {
            return new FighterAttributes(
                _strength,
                _speed,
                _cardio,
                _durability,
                _flexibility,
                _composure,
                _fightIq);
        }

        /// <summary>
        /// Validates the authoring values.
        /// </summary>
        public bool TryValidate(out string error)
        {
            if (!IsValidAttribute(_strength))
            {
                error = "Strength must be between 1 and 100.";
                return false;
            }

            if (!IsValidAttribute(_speed))
            {
                error = "Speed must be between 1 and 100.";
                return false;
            }

            if (!IsValidAttribute(_cardio))
            {
                error = "Cardio must be between 1 and 100.";
                return false;
            }

            if (!IsValidAttribute(_durability))
            {
                error = "Durability must be between 1 and 100.";
                return false;
            }

            if (!IsValidAttribute(_flexibility))
            {
                error = "Flexibility must be between 1 and 100.";
                return false;
            }

            if (!IsValidAttribute(_composure))
            {
                error = "Composure must be between 1 and 100.";
                return false;
            }

            if (!IsValidAttribute(_fightIq))
            {
                error = "Fight IQ must be between 1 and 100.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static bool IsValidAttribute(int value)
        {
            return value >= 1 && value <= 100;
        }
    }
}
