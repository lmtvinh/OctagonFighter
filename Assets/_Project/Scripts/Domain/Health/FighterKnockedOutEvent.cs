namespace OctagonFighter.Domain.Health
{
    /// <summary>
    /// Reports that a fighter entered the knocked-out state.
    /// </summary>
    public readonly struct FighterKnockedOutEvent
    {
        /// <summary>
        /// Gets the damage event that caused the knockout.
        /// </summary>
        public DamageEvent Cause { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FighterKnockedOutEvent"/> struct.
        /// </summary>
        public FighterKnockedOutEvent(DamageEvent cause)
        {
            Cause = cause;
        }
    }
}
