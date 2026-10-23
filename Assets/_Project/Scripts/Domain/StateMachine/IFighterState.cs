using OctagonFighter.Domain.Fighters;

namespace OctagonFighter.Domain.StateMachine
{
    /// <summary>
    /// Defines one state in the fighter state machine.
    /// </summary>
    public interface IFighterState
    {
        /// <summary>
        /// Gets the state identifier.
        /// </summary>
        FighterStateId Id { get; }

        /// <summary>
        /// Gets the movement authority granted by this state.
        /// </summary>
        MovementAuthority MovementAuthority { get; }

        /// <summary>
        /// Gets the intent channels accepted by this state.
        /// </summary>
        FighterIntentMask AcceptedIntents { get; }

        /// <summary>
        /// Gets the priority used by higher-level interrupt decisions.
        /// </summary>
        int TransitionPriority { get; }

        /// <summary>
        /// Enters the state.
        /// </summary>
        void Enter(in FighterStateContext context, in FighterIntent intent);

        /// <summary>
        /// Advances the state by one simulation tick.
        /// </summary>
        StateTickResult Tick(in FighterStateContext context, in FighterIntent intent, float deltaSeconds);

        /// <summary>
        /// Exits the state.
        /// </summary>
        void Exit(in FighterStateContext context);

        /// <summary>
        /// Determines whether this state can transition to a target state.
        /// </summary>
        bool CanTransitionTo(FighterStateId targetState);
    }
}
