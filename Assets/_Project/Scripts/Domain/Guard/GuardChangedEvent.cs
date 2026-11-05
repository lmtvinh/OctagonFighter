namespace OctagonFighter.Domain.Guard
{
    /// <summary>
    /// Reports a change to the current guard value.
    /// </summary>
    public readonly struct GuardChangedEvent
    {
        /// <summary>
        /// Gets the guard value before the change.
        /// </summary>
        public float PreviousGuard { get; }

        /// <summary>
        /// Gets the guard value after the change.
        /// </summary>
        public float CurrentGuard { get; }

        /// <summary>
        /// Gets the maximum guard value.
        /// </summary>
        public float MaxGuard { get; }

        /// <summary>
        /// Gets the remaining recovery delay.
        /// </summary>
        public float RecoveryDelayRemaining { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GuardChangedEvent"/> struct.
        /// </summary>
        public GuardChangedEvent(
            float previousGuard,
            float currentGuard,
            float maxGuard,
            float recoveryDelayRemaining)
        {
            PreviousGuard = previousGuard;
            CurrentGuard = currentGuard;
            MaxGuard = maxGuard;
            RecoveryDelayRemaining = recoveryDelayRemaining;
        }
    }
}
