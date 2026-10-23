namespace OctagonFighter.Domain.Health
{
    /// <summary>
    /// Reports a change to health or accumulated regional trauma.
    /// </summary>
    public readonly struct HealthChangedEvent
    {
        /// <summary>
        /// Gets the health value before the change.
        /// </summary>
        public float PreviousHealth { get; }

        /// <summary>
        /// Gets the health value after the change.
        /// </summary>
        public float CurrentHealth { get; }

        /// <summary>
        /// Gets the head trauma before the change.
        /// </summary>
        public float PreviousHeadTrauma { get; }

        /// <summary>
        /// Gets the head trauma after the change.
        /// </summary>
        public float CurrentHeadTrauma { get; }

        /// <summary>
        /// Gets the body trauma before the change.
        /// </summary>
        public float PreviousBodyTrauma { get; }

        /// <summary>
        /// Gets the body trauma after the change.
        /// </summary>
        public float CurrentBodyTrauma { get; }

        internal HealthChangedEvent(
            float previousHealth,
            float currentHealth,
            float previousHeadTrauma,
            float currentHeadTrauma,
            float previousBodyTrauma,
            float currentBodyTrauma)
        {
            PreviousHealth = previousHealth;
            CurrentHealth = currentHealth;
            PreviousHeadTrauma = previousHeadTrauma;
            CurrentHeadTrauma = currentHeadTrauma;
            PreviousBodyTrauma = previousBodyTrauma;
            CurrentBodyTrauma = currentBodyTrauma;
        }
    }
}
