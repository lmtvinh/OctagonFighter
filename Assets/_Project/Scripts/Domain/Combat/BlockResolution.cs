namespace OctagonFighter.Domain.Combat
{
    using OctagonFighter.Domain.Guard;
    using OctagonFighter.Domain.Health;

    /// <summary>
    /// Describes the resolved outcome of a strike that reached a guarding fighter.
    /// </summary>
    public readonly struct BlockResolution
    {
        /// <summary>
        /// Gets a value indicating whether the strike was actually blocked.
        /// </summary>
        public bool WasBlocked { get; }

        /// <summary>
        /// Gets the health damage that passed through the guard.
        /// </summary>
        public DamageResult HealthDamage { get; }

        /// <summary>
        /// Gets the guard damage applied by the strike.
        /// </summary>
        public GuardDamageResult GuardDamage { get; }

        /// <summary>
        /// Gets the controlled distance the defender is pushed back.
        /// </summary>
        public float PushbackDistance { get; }

        /// <summary>
        /// Gets the stamina spent by the defender.
        /// </summary>
        public float StaminaSpent { get; }

        /// <summary>
        /// Gets a value indicating whether this strike depleted the guard.
        /// </summary>
        public bool BrokeGuard => GuardDamage.BecameBroken;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlockResolution"/> struct.
        /// </summary>
        public BlockResolution(
            bool wasBlocked,
            in DamageResult healthDamage,
            in GuardDamageResult guardDamage,
            float pushbackDistance,
            float staminaSpent)
        {
            WasBlocked = wasBlocked;
            HealthDamage = healthDamage;
            GuardDamage = guardDamage;
            PushbackDistance = pushbackDistance;
            StaminaSpent = staminaSpent;
        }

        /// <summary>
        /// Gets a resolution for damage that was not blocked.
        /// </summary>
        public static BlockResolution NotBlocked => default;
    }
}
