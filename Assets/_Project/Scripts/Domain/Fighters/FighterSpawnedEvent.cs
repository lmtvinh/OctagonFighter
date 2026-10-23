using OctagonFighter.Core.Ids;
using OctagonFighter.Core.Time;

namespace OctagonFighter.Domain.Fighters
{
    /// <summary>
    /// Reports that a fighter entered the simulation.
    /// </summary>
    public readonly struct FighterSpawnedEvent
    {
        /// <summary>
        /// Gets the spawned fighter identifier.
        /// </summary>
        public FighterId FighterId { get; }

        /// <summary>
        /// Gets the tick in which the fighter spawned.
        /// </summary>
        public TickId Tick { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FighterSpawnedEvent"/> struct.
        /// </summary>
        public FighterSpawnedEvent(FighterId fighterId, TickId tick)
        {
            FighterId = fighterId;
            Tick = tick;
        }
    }
}
