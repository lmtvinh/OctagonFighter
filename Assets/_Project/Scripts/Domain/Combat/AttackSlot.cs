namespace OctagonFighter.Domain.Combat
{
    /// <summary>
    /// Identifies an attack slot on a fighter.
    /// </summary>
    public enum AttackSlot : byte
    {
        /// <summary>
        /// No attack slot.
        /// </summary>
        None = 0,

        /// <summary>
        /// Fast light attack (jab).
        /// </summary>
        Light = 1,

        /// <summary>
        /// Slow heavy attack (cross).
        /// </summary>
        Heavy = 2
    }
}
