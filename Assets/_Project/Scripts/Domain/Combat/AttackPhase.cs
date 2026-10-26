namespace OctagonFighter.Domain.Combat
{
    /// <summary>
    /// Timeline phase of an in-progress attack.
    /// </summary>
    public enum AttackPhase : byte
    {
        /// <summary>
        /// Wind-up before the hit window opens.
        /// </summary>
        Startup = 0,

        /// <summary>
        /// Window in which the attack may connect.
        /// </summary>
        Active = 1,

        /// <summary>
        /// Post-hit recovery.
        /// </summary>
        Recovery = 2,

        /// <summary>
        /// The attack duration has elapsed.
        /// </summary>
        Complete = 3
    }
}
