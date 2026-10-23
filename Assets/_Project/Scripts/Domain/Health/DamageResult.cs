namespace OctagonFighter.Domain.Health
{
    /// <summary>
    /// Describes the result of applying a <see cref="DamageEvent"/>.
    /// </summary>
    public readonly struct DamageResult
    {
        /// <summary>
        /// Gets the amount of health actually removed.
        /// </summary>
        public float AppliedAmount { get; }

        /// <summary>
        /// Gets the health before damage was applied.
        /// </summary>
        public float PreviousHealth { get; }

        /// <summary>
        /// Gets the health after damage was applied.
        /// </summary>
        public float CurrentHealth { get; }

        /// <summary>
        /// Gets the accumulated head trauma after damage was applied.
        /// </summary>
        public float HeadTrauma { get; }

        /// <summary>
        /// Gets the accumulated body trauma after damage was applied.
        /// </summary>
        public float BodyTrauma { get; }

        /// <summary>
        /// Gets a value indicating whether this damage caused a knockout.
        /// </summary>
        public bool BecameKnockedOut { get; }

        internal DamageResult(
            float appliedAmount,
            float previousHealth,
            float currentHealth,
            float headTrauma,
            float bodyTrauma,
            bool becameKnockedOut)
        {
            AppliedAmount = appliedAmount;
            PreviousHealth = previousHealth;
            CurrentHealth = currentHealth;
            HeadTrauma = headTrauma;
            BodyTrauma = bodyTrauma;
            BecameKnockedOut = becameKnockedOut;
        }
    }
}
