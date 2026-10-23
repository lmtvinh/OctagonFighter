namespace OctagonFighter.Domain.Health
{
    /// <summary>
    /// Body region receiving damage.
    /// </summary>
    public enum HitRegion : byte
    {
        /// <summary>
        /// Head region.
        /// </summary>
        Head = 0,

        /// <summary>
        /// Body region.
        /// </summary>
        Body = 1,

        /// <summary>
        /// Limb region.
        /// </summary>
        Limb = 2
    }
}
