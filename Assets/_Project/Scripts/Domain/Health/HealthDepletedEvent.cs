namespace OctagonFighter.Domain.Health
{
    /// <summary>
    /// Reports that a fighter's health reached zero.
    /// </summary>
    public readonly struct HealthDepletedEvent
    {
        /// <summary>
        /// Gets the damage event that depleted health.
        /// </summary>
        public DamageEvent LethalDamage { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthDepletedEvent"/> struct.
        /// </summary>
        public HealthDepletedEvent(DamageEvent lethalDamage)
        {
            LethalDamage = lethalDamage;
        }
    }
}
