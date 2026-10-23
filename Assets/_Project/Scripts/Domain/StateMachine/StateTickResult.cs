using System;

namespace OctagonFighter.Domain.StateMachine
{
    /// <summary>
    /// Describes whether a state wants to request a transition after a tick.
    /// </summary>
    public readonly struct StateTickResult
    {
        /// <summary>
        /// Gets a value indicating whether a transition was requested.
        /// </summary>
        public bool HasTransition { get; }

        /// <summary>
        /// Gets the requested target state.
        /// </summary>
        public FighterStateId TargetState { get; }

        private StateTickResult(bool hasTransition, FighterStateId targetState)
        {
            HasTransition = hasTransition;
            TargetState = targetState;
        }

        /// <summary>
        /// Returns a result that keeps the current state.
        /// </summary>
        public static StateTickResult Stay()
        {
            return new StateTickResult(false, default);
        }

        /// <summary>
        /// Returns a result that requests a transition.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the target state is undefined.</exception>
        public static StateTickResult TransitionTo(FighterStateId targetState)
        {
            if (!Enum.IsDefined(typeof(FighterStateId), targetState))
            {
                throw new ArgumentException("Target state is not defined.", nameof(targetState));
            }

            return new StateTickResult(true, targetState);
        }
    }
}
