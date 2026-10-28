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
        /// <remarks>
        /// Timings are measured from the imported clips at 30 fps:
        /// <c>Lead Jab.fbx</c> (1.500 s) reaches 90% of its extension between 0.375 s and 0.442 s
        /// with a peak at 0.404 s, and <c>Jab Cross.fbx</c> (2.233 s) reaches 90% of the right hand's
        /// extension between 0.613 s and 0.675 s with a peak at 0.638 s. The contact windows below
        /// bracket those intervals and the recovery covers the hand's return to guard. The clips
        /// carry a long idle tail, so the strike states end after the useful motion instead of
        /// playing the whole file.
        /// </remarks>
        public static AttackBook CreateDefault()
        {
            var light = new AttackDefinition(
                "Jab",
                "Jab",
                0.36f,
                0.11f,
                0.23f,
                0.35f,
                6f,
                8f,
                0.95f,
                0.06f,
                HitRegion.Head,
                StrikeHand.Left);

            var heavy = new AttackDefinition(
                "Cross",
                "Cross",
                0.60f,
                0.10f,
                0.20f,
                0.75f,
                12f,
                15f,
                1.05f,
                0.07f,
                HitRegion.Head,
                StrikeHand.Right);

            return new AttackBook(light, heavy, 0.25f);
        }
    }
}
