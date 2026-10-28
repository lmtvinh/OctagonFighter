namespace OctagonFighter.Domain.Combat
{
    /// <summary>
    /// Identifies which fist performs a strike.
    /// </summary>
    public enum StrikeHand : byte
    {
        /// <summary>
        /// The lead hand of an orthodox stance.
        /// </summary>
        Left = 0,

        /// <summary>
        /// The rear hand of an orthodox stance.
        /// </summary>
        Right = 1
    }
}
