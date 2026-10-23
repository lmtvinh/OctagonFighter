namespace OctagonFighter.Domain.Stamina
{
    /// <summary>
    /// Reports a transition between fatigue tiers.
    /// </summary>
    public readonly struct StaminaTierChangedEvent
    {
        /// <summary>
        /// Gets the previous fatigue tier.
        /// </summary>
        public StaminaTier PreviousTier { get; }

        /// <summary>
        /// Gets the current fatigue tier.
        /// </summary>
        public StaminaTier CurrentTier { get; }

        /// <summary>
        /// Gets the current stamina value.
        /// </summary>
        public float CurrentStamina { get; }

        internal StaminaTierChangedEvent(StaminaTier previousTier, StaminaTier currentTier, float currentStamina)
        {
            PreviousTier = previousTier;
            CurrentTier = currentTier;
            CurrentStamina = currentStamina;
        }
    }
}
