namespace OctagonFighter.Domain.Guard
{
    /// <summary>
    /// Describes the result of applying guard damage.
    /// </summary>
    public readonly struct GuardDamageResult
    {
        /// <summary>
        /// Gets the guard value before the hit.
        /// </summary>
        public float PreviousGuard { get; }

        /// <summary>
        /// Gets the guard value after the hit.
        /// </summary>
        public float CurrentGuard { get; }

        /// <summary>
        /// Gets the guard actually removed, clamped to the remaining guard.
        /// </summary>
        public float AppliedGuardDamage { get; }

        /// <summary>
        /// Gets the dynamic multiplier applied to the strike's authored guard damage.
        /// </summary>
        public float Multiplier { get; }

        /// <summary>
        /// Gets a value indicating whether this hit depleted the guard.
        /// </summary>
        public bool BecameBroken { get; }

        /// <summary>
        /// Gets a value indicating whether the guard is broken after the hit.
        /// </summary>
        public bool IsBroken { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GuardDamageResult"/> struct.
        /// </summary>
        public GuardDamageResult(
            float previousGuard,
            float currentGuard,
            float appliedGuardDamage,
            float multiplier,
            bool becameBroken,
            bool isBroken)
        {
            PreviousGuard = previousGuard;
            CurrentGuard = currentGuard;
            AppliedGuardDamage = appliedGuardDamage;
            Multiplier = multiplier;
            BecameBroken = becameBroken;
            IsBroken = isBroken;
        }
    }
}
