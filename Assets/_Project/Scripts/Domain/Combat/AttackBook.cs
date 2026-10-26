using System;
using OctagonFighter.Domain.Health;

namespace OctagonFighter.Domain.Combat
{
    /// <summary>
    /// Immutable set of strikes available to one fighter.
    /// </summary>
    public readonly struct AttackBook
    {
        /// <summary>
        /// Gets the light attack definition.
        /// </summary>
        public AttackDefinition Light { get; }

        /// <summary>
        /// Gets the heavy attack definition.
        /// </summary>
        public AttackDefinition Heavy { get; }

        /// <summary>
        /// Gets how long a pressed attack stays buffered while the fighter is busy.
        /// </summary>
        public float InputBufferSeconds { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttackBook"/> struct.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the buffer duration is invalid.</exception>
        /// <exception cref="ArgumentException">Thrown when a required attack is missing.</exception>
        public AttackBook(AttackDefinition light, AttackDefinition heavy, float inputBufferSeconds)
        {
            if (!light.IsDefined)
            {
                throw new ArgumentException("A light attack must be defined.", nameof(light));
            }

            if (!heavy.IsDefined)
            {
                throw new ArgumentException("A heavy attack must be defined.", nameof(heavy));
            }

            if (!float.IsFinite(inputBufferSeconds) || inputBufferSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(inputBufferSeconds),
                    "Input buffer duration must be finite and non-negative.");
            }

            Light = light;
            Heavy = heavy;
            InputBufferSeconds = inputBufferSeconds;
        }

        /// <summary>
        /// Returns the definition for a slot.
        /// </summary>
        public bool TryGet(AttackSlot slot, out AttackDefinition definition)
        {
            switch (slot)
            {
                case AttackSlot.Light:
                    definition = Light;
                    return definition.IsDefined;

                case AttackSlot.Heavy:
                    definition = Heavy;
                    return definition.IsDefined;

                default:
                    definition = default;
                    return false;
            }
        }

        /// <summary>
        /// Creates the default MVP strike set.
        /// </summary>
        public static AttackBook CreateDefault()
        {
            var light = new AttackDefinition(
                "Jab",
                "Jab",
                0.12f,
                0.08f,
                0.20f,
                0.35f,
                6f,
                8f,
                1.05f,
                0.32f,
                HitRegion.Head);

            var heavy = new AttackDefinition(
                "Cross",
                "Cross",
                0.22f,
                0.10f,
                0.33f,
                0.75f,
                12f,
                15f,
                1.25f,
                0.40f,
                HitRegion.Head);

            return new AttackBook(light, heavy, 0.25f);
        }
    }
}
