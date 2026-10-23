using System;

namespace OctagonFighter.Domain.Fighters
{
    /// <summary>
    /// Core attributes that drive health, stamina, and movement behavior.
    /// </summary>
    public readonly struct FighterAttributes
    {
        /// <summary>
        /// Gets physical strength from 1 to 100.
        /// </summary>
        public int Strength { get; }

        /// <summary>
        /// Gets movement speed from 1 to 100.
        /// </summary>
        public int Speed { get; }

        /// <summary>
        /// Gets cardiovascular capacity from 1 to 100.
        /// </summary>
        public int Cardio { get; }

        /// <summary>
        /// Gets damage resistance from 1 to 100.
        /// </summary>
        public int Durability { get; }

        /// <summary>
        /// Gets flexibility from 1 to 100.
        /// </summary>
        public int Flexibility { get; }

        /// <summary>
        /// Gets composure under pressure from 1 to 100.
        /// </summary>
        public int Composure { get; }

        /// <summary>
        /// Gets combat decision-making from 1 to 100.
        /// </summary>
        public int FightIq { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FighterAttributes"/> struct.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an attribute is outside 1-100.</exception>
        public FighterAttributes(
            int strength,
            int speed,
            int cardio,
            int durability,
            int flexibility,
            int composure,
            int fightIq)
        {
            ValidateAttribute(strength, nameof(strength));
            ValidateAttribute(speed, nameof(speed));
            ValidateAttribute(cardio, nameof(cardio));
            ValidateAttribute(durability, nameof(durability));
            ValidateAttribute(flexibility, nameof(flexibility));
            ValidateAttribute(composure, nameof(composure));
            ValidateAttribute(fightIq, nameof(fightIq));

            Strength = strength;
            Speed = speed;
            Cardio = cardio;
            Durability = durability;
            Flexibility = flexibility;
            Composure = composure;
            FightIq = fightIq;
        }

        private static void ValidateAttribute(int value, string parameterName)
        {
            if (value < 1 || value > 100)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Fighter attributes must be between 1 and 100.");
            }
        }
    }
}
