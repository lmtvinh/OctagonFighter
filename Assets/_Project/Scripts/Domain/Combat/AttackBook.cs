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
        /// Timings are measured from the imported clips at their authored frame rate:
        /// <c>Boxing_mixamo.fbx</c> frames 50-64 (24 fps, 0.583 s) is the lead jab, whose left hand
        /// reaches 0.523 m forward at 0.317 s; <c>Jab Cross.fbx</c> (2.233 s, 30 fps) reaches its
        /// right-hand peak at 0.638 s. Both windows bracket the measured extension plateau rather
        /// than only its onset: Play Mode sampling showed the presented fist still extending until
        /// ~0.73 s on the cross, so the old 0.60-0.70 s window closed before the hand's deepest
        /// sample. The recovery covers the hand's return to guard, and the clips' long idle tails
        /// are cut off by ending the state after the useful motion.
        /// </remarks>
        public static AttackBook CreateDefault()
        {
            var light = new AttackDefinition(
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

            var heavy = new AttackDefinition(
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
                HitRegion.Body,
                StrikeHand.Right);

            return new AttackBook(light, heavy, 0.25f);
        }
    }
}
