using OctagonFighter.Core.Time;

namespace OctagonFighter.Domain.StateMachine
{
    /// <summary>
    /// Describes a completed fighter state transition.
    /// </summary>
    public readonly struct StateTransition
    {
        /// <summary>
        /// Gets the state that was exited.
        /// </summary>
        public FighterStateId From { get; }

        /// <summary>
        /// Gets the state that was entered.
        /// </summary>
        public FighterStateId To { get; }

        /// <summary>
        /// Gets the tick in which the transition occurred.
        /// </summary>
        public TickId Tick { get; }

        internal StateTransition(FighterStateId from, FighterStateId to, TickId tick)
        {
            From = from;
            To = to;
            Tick = tick;
        }
    }
}
