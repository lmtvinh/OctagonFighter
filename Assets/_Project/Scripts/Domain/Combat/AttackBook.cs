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
        /// reaches 0.523 m forward at 0.317 s. The shipped Cross presentation is the right-hand
        /// portion of <c>Jab Cross.fbx</c> / <c>BoxerCross.anim</c>, trimmed to 0.50-1.25 s so the
        /// heavy state no longer plays the combo's lead jab. Inside that window the right hand's
        /// 90% extension is 0.113-0.175 s (0.613-0.675 s on the untrimmed source) and the deepest
        /// sample is ~0.23 s. Startup 0.10 / active 0.18 brackets that plateau; recovery returns
        /// the hand toward guard before the unused tail.
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
                StrikeHand.Left,
                guardDamage: 10f,
                impactStrength: 0.25f,
                blockStaminaCost: 3f);

            var heavy = new AttackDefinition(
                "Cross",
                "Cross",
                0.10f,
                0.18f,
                0.32f,
                0.75f,
                12f,
                15f,
                1.05f,
                0.07f,
                HitRegion.Body,
                StrikeHand.Right,
                guardDamage: 24f,
                impactStrength: 0.8f,
                blockStaminaCost: 7f);

            return new AttackBook(light, heavy, 0.25f);
        }
    }
}
