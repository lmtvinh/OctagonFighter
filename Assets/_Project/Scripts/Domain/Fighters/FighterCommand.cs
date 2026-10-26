namespace OctagonFighter.Domain.Fighters
{
    /// <summary>
    /// Non-attack commands that can be expressed through a fighter intent.
    /// </summary>
    public enum FighterCommand : byte
    {
        /// <summary>
        /// No command.
        /// </summary>
        None = 0,

        /// <summary>
        /// Enter or hold a defensive block.
        /// </summary>
        Block = 1,

        /// <summary>
        /// Request an evasive dodge.
        /// </summary>
        Dodge = 2,

        /// <summary>
        /// Cancel the current committed action when allowed.
        /// </summary>
        Cancel = 3,

        /// <summary>
        /// Interact with the environment.
        /// </summary>
        Interact = 4,

        /// <summary>
        /// Attempt to stand up from a downed state.
        /// </summary>
        StandUp = 5,

        /// <summary>
        /// Request the fast light strike (jab).
        /// </summary>
        LightPunch = 6,

        /// <summary>
        /// Request the slow heavy strike (cross).
        /// </summary>
        HeavyPunch = 7
    }
}
