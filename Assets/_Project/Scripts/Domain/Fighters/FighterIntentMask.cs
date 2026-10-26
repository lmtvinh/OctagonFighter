using System;

namespace OctagonFighter.Domain.Fighters
{
    /// <summary>
    /// Identifies the intent channels accepted by a fighter state.
    /// </summary>
    [Flags]
    public enum FighterIntentMask : byte
    {
        /// <summary>
        /// No intents are accepted.
        /// </summary>
        None = 0,

        /// <summary>
        /// Movement input is accepted.
        /// </summary>
        Move = 1 << 0,

        /// <summary>
        /// Block command is accepted.
        /// </summary>
        Block = 1 << 1,

        /// <summary>
        /// Dodge command is accepted.
        /// </summary>
        Dodge = 1 << 2,

        /// <summary>
        /// Cancel command is accepted.
        /// </summary>
        Cancel = 1 << 3,

        /// <summary>
        /// Interact command is accepted.
        /// </summary>
        Interact = 1 << 4,

        /// <summary>
        /// Stand-up command is accepted.
        /// </summary>
        StandUp = 1 << 5,

        /// <summary>
        /// Light and heavy punch commands are accepted.
        /// </summary>
        Attack = 1 << 6
    }
}
