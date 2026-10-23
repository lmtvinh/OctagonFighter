namespace OctagonFighter.Domain.StateMachine
{
    /// <summary>
    /// Movement authority granted by a fighter state.
    /// </summary>
    public enum MovementAuthority : byte
    {
        /// <summary>
        /// No movement is allowed.
        /// </summary>
        None = 0,

        /// <summary>
        /// Movement is allowed at a reduced rate.
        /// </summary>
        Limited = 1,

        /// <summary>
        /// Full movement is allowed.
        /// </summary>
        Full = 2
    }
}
