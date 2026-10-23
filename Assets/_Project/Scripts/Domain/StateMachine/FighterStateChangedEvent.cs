using OctagonFighter.Core.Ids;
using OctagonFighter.Core.Time;

namespace OctagonFighter.Domain.StateMachine
{
    /// <summary>
    /// Reports that a fighter changed state.
    /// </summary>
    public readonly struct FighterStateChangedEvent
    {
        /// <summary>
        /// Gets the fighter identifier.
        /// </summary>
        public FighterId FighterId { get; }

        /// <summary>
        /// Gets the previous state.
        /// </summary>
        public FighterStateId PreviousState { get; }

        /// <summary>
        /// Gets the new state.
        /// </summary>
        public FighterStateId CurrentState { get; }

        /// <summary>
        /// Gets the tick in which the transition occurred.
        /// </summary>
        public TickId Tick { get; }

        public FighterStateChangedEvent(
            FighterId fighterId,
            FighterStateId previousState,
            FighterStateId currentState,
            TickId tick)
        {
            FighterId = fighterId;
            PreviousState = previousState;
            CurrentState = currentState;
            Tick = tick;
        }
    }
}
