using OctagonFighter.Core.Ids;
using OctagonFighter.Core.Time;

namespace OctagonFighter.Domain.Combat
{
    /// <summary>
    /// Raised when an active strike frame wants a hit query resolved.
    /// </summary>
    public readonly struct AttackHitRequest
    {
        /// <summary>
        /// Gets the attacking fighter identifier.
        /// </summary>
        public FighterId AttackerId { get; }

        /// <summary>
        /// Gets the attack slot that produced the request.
        /// </summary>
        public AttackSlot Slot { get; }

        /// <summary>
        /// Gets the strike definition that produced the request.
        /// </summary>
        public AttackDefinition Attack { get; }

        /// <summary>
        /// Gets the identifier of this hit window.
        /// </summary>
        public HitId HitId { get; }

        /// <summary>
        /// Gets the tick in which the hit window opened.
        /// </summary>
        public TickId Tick { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttackHitRequest"/> struct.
        /// </summary>
        public AttackHitRequest(
            FighterId attackerId,
            AttackSlot slot,
            in AttackDefinition attack,
            HitId hitId,
            TickId tick)
        {
            AttackerId = attackerId;
            Slot = slot;
            Attack = attack;
            HitId = hitId;
            Tick = tick;
        }
    }
}
