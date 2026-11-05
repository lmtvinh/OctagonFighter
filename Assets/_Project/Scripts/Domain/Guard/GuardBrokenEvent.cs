namespace OctagonFighter.Domain.Guard
{
    /// <summary>
    /// Reports that a fighter's guard was depleted by a blocked strike.
    /// </summary>
    public readonly struct GuardBrokenEvent
    {
        /// <summary>
        /// Gets the guard damage that depleted the guard.
        /// </summary>
        public float AppliedGuardDamage { get; }

        /// <summary>
        /// Gets the dynamic multiplier that was applied to the breaking strike.
        /// </summary>
        public float Multiplier { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GuardBrokenEvent"/> struct.
        /// </summary>
        public GuardBrokenEvent(float appliedGuardDamage, float multiplier)
        {
            AppliedGuardDamage = appliedGuardDamage;
            Multiplier = multiplier;
        }
    }
}
