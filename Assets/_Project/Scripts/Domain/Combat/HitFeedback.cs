using OctagonFighter.Core.Ids;
using OctagonFighter.Core.Time;
using OctagonFighter.Domain.Health;

namespace OctagonFighter.Domain.Combat
{
    /// <summary>
    /// Reports one resolved strike for presentation and HUD feedback.
    /// </summary>
    /// <remarks>
    /// Exactly one <see cref="HitFeedback"/> is raised per resolved strike, whether the strike was
    /// blocked or landed. The region is derived from the collided body volume (collision data),
    /// never from the attack's authored name.
    /// </remarks>
    public readonly struct HitFeedback
    {
        /// <summary>
        /// Gets the attacking fighter identifier.
        /// </summary>
        public FighterId AttackerId { get; }

        /// <summary>
        /// Gets the target (defender) fighter identifier.
        /// </summary>
        public FighterId TargetId { get; }

        /// <summary>
        /// Gets the body region that received the strike.
        /// </summary>
        public HitRegion Region { get; }

        /// <summary>
        /// Gets a value indicating whether the strike was absorbed by the guard.
        /// </summary>
        public bool Blocked { get; }

        /// <summary>
        /// Gets the resolved damage amount: chip damage when blocked, applied damage when landed.
        /// </summary>
        public float DamageAmount { get; }

        /// <summary>
        /// Gets the tick in which the strike was resolved.
        /// </summary>
        public TickId Tick { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HitFeedback"/> struct.
        /// </summary>
        public HitFeedback(
            FighterId attackerId,
            FighterId targetId,
            HitRegion region,
            bool blocked,
            float damageAmount,
            TickId tick)
        {
            AttackerId = attackerId;
            TargetId = targetId;
            Region = region;
            Blocked = blocked;
            DamageAmount = damageAmount;
            Tick = tick;
        }
    }
}
