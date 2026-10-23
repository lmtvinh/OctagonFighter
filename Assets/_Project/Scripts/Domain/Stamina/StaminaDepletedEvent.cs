namespace OctagonFighter.Domain.Stamina
{
    /// <summary>
    /// Reports that a fighter reached zero stamina.
    /// </summary>
    public readonly struct StaminaDepletedEvent
    {
        /// <summary>
        /// Gets the action that depleted stamina.
        /// </summary>
        public StaminaCost Cost { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StaminaDepletedEvent"/> struct.
        /// </summary>
        public StaminaDepletedEvent(StaminaCost cost)
        {
            Cost = cost;
        }
    }
}
