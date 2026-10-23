namespace OctagonFighter.Domain.Stamina
{
    /// <summary>
    /// Reports a change to the current stamina value.
    /// </summary>
    public readonly struct StaminaChangedEvent
    {
        /// <summary>
        /// Gets the stamina value before the change.
        /// </summary>
        public float PreviousStamina { get; }

        /// <summary>
        /// Gets the stamina value after the change.
        /// </summary>
        public float CurrentStamina { get; }

        /// <summary>
        /// Gets the maximum stamina value.
        /// </summary>
        public float MaxStamina { get; }

        /// <summary>
        /// Gets the remaining regeneration delay.
        /// </summary>
        public float RecoveryDelayRemaining { get; }

        internal StaminaChangedEvent(
            float previousStamina,
            float currentStamina,
            float maxStamina,
            float recoveryDelayRemaining)
        {
            PreviousStamina = previousStamina;
            CurrentStamina = currentStamina;
            MaxStamina = maxStamina;
            RecoveryDelayRemaining = recoveryDelayRemaining;
        }
    }
}
