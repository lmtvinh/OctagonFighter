namespace OctagonFighter.Domain.Fighters
{
    /// <summary>
    /// Fighting stance used by a fighter.
    /// </summary>
    public enum FighterStance : byte
    {
        /// <summary>
        /// Left foot forward with the right hand as the power hand.
        /// </summary>
        Orthodox = 0,

        /// <summary>
        /// Right foot forward with the left hand as the power hand.
        /// </summary>
        Southpaw = 1,

        /// <summary>
        /// Fighter alternates stance.
        /// </summary>
        Switch = 2
    }
}
