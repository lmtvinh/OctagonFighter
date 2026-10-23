using OctagonFighter.Core.Ids;
using OctagonFighter.Core.Time;

namespace OctagonFighter.Domain.Fighters
{
    /// <summary>
    /// Reports that a fighter became defeated.
    /// </summary>
    public readonly struct FighterDefeatedEvent
    {
        /// <summary>
        /// Gets the defeated fighter identifier.
        /// </summary>
        public FighterId FighterId { get; }

        /// <summary>
        /// Gets the tick in which defeat was resolved.
        /// </summary>
        public TickId Tick { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FighterDefeatedEvent"/> struct.
        /// </summary>
        public FighterDefeatedEvent(FighterId fighterId, TickId tick)
        {
            FighterId = fighterId;
            Tick = tick;
        }
    }
}
