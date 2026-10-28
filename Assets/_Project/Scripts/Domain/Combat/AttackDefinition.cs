using System;
using OctagonFighter.Core.Math;
using OctagonFighter.Domain.Health;

namespace OctagonFighter.Domain.Combat
{
    /// <summary>
    /// Immutable timing, cost, and hit data for one strike.
    /// </summary>
    public readonly struct AttackDefinition
    {
        /// <summary>
        /// Gets the designer-facing name.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the animator state name used to present the strike.
        /// </summary>
        public string AnimationState { get; }

        /// <summary>
        /// Gets the wind-up duration in seconds. The contact window opens when this elapses.
        /// </summary>
        public float StartupSeconds { get; }

        /// <summary>
        /// Gets the length of the contact window in seconds.
        /// </summary>
        /// <remarks>
        /// The window is a sustained interval, not a single frame. It opens exactly at
        /// <see cref="StartupSeconds"/> and closes at <see cref="ContactEndSeconds"/>. Hit queries
        /// are re-issued on every tick inside it so a target that enters the fist's path late can
        /// still be struck, while the registry stops the same strike from damaging twice.
        /// </remarks>
        public float ActiveSeconds { get; }

        /// <summary>
        /// Gets the recovery duration in seconds.
        /// </summary>
        public float RecoverySeconds { get; }

        /// <summary>
        /// Gets the cooldown duration in seconds.
        /// </summary>
        public float CooldownSeconds { get; }

        /// <summary>
        /// Gets the stamina consumed when the strike starts.
        /// </summary>
        public float StaminaCost { get; }

        /// <summary>
        /// Gets the resolved damage applied on a successful hit.
        /// </summary>
        public float Damage { get; }

        /// <summary>
        /// Gets the forward reach of the hit query in meters.
        /// </summary>
        /// <remarks>
        /// The strike is resolved from the striking hand, so this value is the maximum distance the
        /// fist may travel from the attacker's root before a query is considered plausible. It acts
        /// as a sanity envelope around the sampled hand, not as a chest-centered reach.
        /// </remarks>
        public float Range { get; }

        /// <summary>
        /// Gets the collision radius of the striking fist in meters.
        /// </summary>
        /// <remarks>
        /// Keep this small: it is the tolerance around the hand's swept path, not a substitute for
        /// arm length.
        /// </remarks>
        public float Radius { get; }

        /// <summary>
        /// Gets the body region this strike targets.
        /// </summary>
        public HitRegion Region { get; }

        /// <summary>
        /// Gets the fist that performs this strike.
        /// </summary>
        public StrikeHand Hand { get; }

        /// <summary>
        /// Gets the instant, in seconds since the strike started, at which the contact window opens.
        /// </summary>
        public float ContactStartSeconds => StartupSeconds;

        /// <summary>
        /// Gets the instant, in seconds since the strike started, at which the contact window closes.
        /// </summary>
        public float ContactEndSeconds => StartupSeconds + ActiveSeconds;

        /// <summary>
        /// Gets the total attack duration in seconds.
        /// </summary>
        public float DurationSeconds => StartupSeconds + ActiveSeconds + RecoverySeconds;

        /// <summary>
        /// Gets a value indicating whether this definition was configured.
        /// </summary>
        public bool IsDefined => DurationSeconds > 0f && Damage > 0f;

        /// <summary>
        /// Initializes a new instance of the <see cref="AttackDefinition"/> struct.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric value is invalid.</exception>
        /// <exception cref="ArgumentException">Thrown when a required name is empty.</exception>
        public AttackDefinition(
            string displayName,
            string animationState,
            float startupSeconds,
            float activeSeconds,
            float recoverySeconds,
            float cooldownSeconds,
            float staminaCost,
            float damage,
            float range,
            float radius,
            HitRegion region,
            StrikeHand hand)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Attack display name must be specified.", nameof(displayName));
            }

            if (string.IsNullOrWhiteSpace(animationState))
            {
                throw new ArgumentException("Attack animation state must be specified.", nameof(animationState));
            }

            ValidateNonNegative(startupSeconds, nameof(startupSeconds));
            ValidatePositive(activeSeconds, nameof(activeSeconds));
            ValidateNonNegative(recoverySeconds, nameof(recoverySeconds));
            ValidateNonNegative(cooldownSeconds, nameof(cooldownSeconds));
            ValidateNonNegative(staminaCost, nameof(staminaCost));
            ValidatePositive(damage, nameof(damage));
            ValidatePositive(range, nameof(range));
            ValidatePositive(radius, nameof(radius));

            if (!Enum.IsDefined(typeof(HitRegion), region))
            {
                throw new ArgumentException("Hit region is not defined.", nameof(region));
            }

            if (!Enum.IsDefined(typeof(StrikeHand), hand))
            {
                throw new ArgumentException("Strike hand is not defined.", nameof(hand));
            }

            DisplayName = displayName.Trim();
            AnimationState = animationState.Trim();
            StartupSeconds = startupSeconds;
            ActiveSeconds = activeSeconds;
            RecoverySeconds = recoverySeconds;
            CooldownSeconds = cooldownSeconds;
            StaminaCost = staminaCost;
            Damage = damage;
            Range = range;
            Radius = radius;
            Region = region;
            Hand = hand;
        }

        /// <summary>
        /// Returns the timeline phase for an elapsed time since the strike started.
        /// </summary>
        public AttackPhase GetPhase(float elapsedSeconds)
        {
            if (elapsedSeconds < StartupSeconds)
            {
                return AttackPhase.Startup;
            }

            if (elapsedSeconds < StartupSeconds + ActiveSeconds)
            {
                return AttackPhase.Active;
            }

            return elapsedSeconds < DurationSeconds ? AttackPhase.Recovery : AttackPhase.Complete;
        }

        /// <summary>
        /// Determines whether a sampled fist position is still plausible for this strike.
        /// </summary>
        /// <remarks>
        /// The striking hand can never travel further from the attacker's body axis than the arm
        /// allows, so a hand sample beyond <see cref="Range"/> on the plane means the rig or the
        /// sweep is invalid and the query must be discarded instead of reaching across the arena.
        /// Height is ignored because the fist is always about shoulder high while the fighter root
        /// sits on the floor.
        /// </remarks>
        public bool IsHandInReach(Float3 attackerRoot, Float3 handPosition)
        {
            float dx = handPosition.X - attackerRoot.X;
            float dz = handPosition.Z - attackerRoot.Z;
            return (dx * dx) + (dz * dz) <= Range * Range;
        }

        private static void ValidateNonNegative(float value, string parameterName)
        {
            if (!float.IsFinite(value) || value < 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Value must be finite and non-negative.");
            }
        }

        private static void ValidatePositive(float value, string parameterName)
        {
            if (!float.IsFinite(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Value must be finite and positive.");
            }
        }
    }
}
