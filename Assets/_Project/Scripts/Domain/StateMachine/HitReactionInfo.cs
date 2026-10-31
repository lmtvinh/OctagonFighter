using System;
using OctagonFighter.Core.Time;
using OctagonFighter.Domain.Health;

namespace OctagonFighter.Domain.StateMachine
{
    /// <summary>
    /// Describes the confirmed hit that started a fighter's current reaction.
    /// </summary>
    /// <remarks>
    /// A reaction is only ever created from damage that actually applied, so an instance of this
    /// struct is proof that a strike connected. The region is what lets the presentation layer pick
    /// a head or body reaction without re-deriving it from animation state.
    /// </remarks>
    public readonly struct HitReactionInfo
    {
        /// <summary>
        /// Gets the body region that received the confirmed hit.
        /// </summary>
        public HitRegion Region { get; }

        /// <summary>
        /// Gets the tick in which the hit was resolved.
        /// </summary>
        public TickId Tick { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HitReactionInfo"/> struct.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the region is undefined.</exception>
        public HitReactionInfo(HitRegion region, TickId tick)
        {
            if (!Enum.IsDefined(typeof(HitRegion), region))
            {
                throw new ArgumentException("Hit region is not defined.", nameof(region));
            }

            Region = region;
            Tick = tick;
        }
    }
}
