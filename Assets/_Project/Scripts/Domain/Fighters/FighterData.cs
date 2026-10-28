using System;
using OctagonFighter.Core.Ids;
using OctagonFighter.Domain.Combat;
using OctagonFighter.Domain.Health;
using OctagonFighter.Domain.Stamina;

namespace OctagonFighter.Domain.Fighters
{
    /// <summary>
    /// Immutable runtime fighter data derived from an authored definition.
    /// </summary>
    public readonly struct FighterData
    {
        /// <summary>
        /// Gets the stable fighter identifier.
        /// </summary>
        public FighterId Id { get; }

        /// <summary>
        /// Gets the display name.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the fighter attributes.
        /// </summary>
        public FighterAttributes Attributes { get; }

        /// <summary>
        /// Gets the physical body profile.
        /// </summary>
        public BodyProfile Body { get; }

        /// <summary>
        /// Gets the health configuration.
        /// </summary>
        public HealthProfile Health { get; }

        /// <summary>
        /// Gets the stamina configuration.
        /// </summary>
        public StaminaProfile Stamina { get; }

        /// <summary>
        /// Gets the movement and reaction timing configuration.
        /// </summary>
        public MovementProfile Movement { get; }

        /// <summary>
        /// Gets the strikes available to this fighter.
        /// </summary>
        public AttackBook Attacks { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FighterData"/> struct.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the display name or attack book is invalid.</exception>
        public FighterData(
            FighterId id,
            string displayName,
            FighterAttributes attributes,
            BodyProfile body,
            HealthProfile health,
            StaminaProfile stamina,
            MovementProfile movement,
            AttackBook attacks)
        {
            if (id.IsEmpty)
            {
                throw new ArgumentException("Fighter ID must be specified.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Display name must be specified.", nameof(displayName));
            }

            if (!attacks.Light.IsDefined || !attacks.Heavy.IsDefined)
            {
                throw new ArgumentException("A light and heavy attack must be defined.", nameof(attacks));
            }

            Id = id;
            DisplayName = displayName.Trim();
            Attributes = attributes;
            Body = body;
            Health = health;
            Stamina = stamina;
            Movement = movement;
            Attacks = attacks;
        }

        /// <summary>
        /// Returns a copy of this data with a different fighter identifier.
        /// </summary>
        /// <remarks>
        /// Used when several scene instances share one authored definition but must have
        /// unique runtime identities for targeting and damage routing.
        /// </remarks>
        public FighterData WithId(FighterId id)
        {
            return new FighterData(
                id,
                DisplayName,
                Attributes,
                Body,
                Health,
                Stamina,
                Movement,
                Attacks);
        }
    }
}
