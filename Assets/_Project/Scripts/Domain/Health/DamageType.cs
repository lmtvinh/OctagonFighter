namespace OctagonFighter.Domain.Health
{
    /// <summary>
    /// Classification of resolved damage.
    /// </summary>
    public enum DamageType : byte
    {
        /// <summary>
        /// Direct impact damage.
        /// </summary>
        Impact = 0,

        /// <summary>
        /// Accumulated wear damage.
        /// </summary>
        Accumulated = 1,

        /// <summary>
        /// Laceration damage.
        /// </summary>
        Cut = 2,

        /// <summary>
        /// Stun or concussion damage.
        /// </summary>
        Stun = 3
    }
}
