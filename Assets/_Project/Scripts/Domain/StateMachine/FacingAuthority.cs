namespace OctagonFighter.Domain.StateMachine
{
    /// <summary>
    /// Controls whether the movement adapter may turn the fighter toward an opponent during a
    /// state.
    /// </summary>
    public enum FacingAuthority : byte
    {
        /// <summary>
        /// The adapter may rotate the body toward the nearest opponent, which keeps locomotion and
        /// guard states aimed at the target.
        /// </summary>
        Free = 0,

        /// <summary>
        /// The body keeps the orientation it had when the state was entered. Used by committed
        /// strikes so a missed punch stays missed instead of homing onto the opponent.
        /// </summary>
        Locked = 1
    }
}
