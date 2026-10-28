using OctagonFighter.Domain.Fighters;

namespace OctagonFighter.Domain.StateMachine
{
    /// <summary>
    /// Base implementation for fighter states.
    /// </summary>
    public abstract class FighterStateBase : IFighterState
    {
        /// <inheritdoc />
        public FighterStateId Id { get; }

        /// <inheritdoc />
        public MovementAuthority MovementAuthority { get; }

        /// <inheritdoc />
        public FacingAuthority FacingAuthority { get; }

        /// <inheritdoc />
        public FighterIntentMask AcceptedIntents { get; }

        /// <inheritdoc />
        public int TransitionPriority { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FighterStateBase"/> class.
        /// </summary>
        protected FighterStateBase(
            FighterStateId id,
            MovementAuthority movementAuthority,
            FighterIntentMask acceptedIntents,
            int transitionPriority,
            FacingAuthority facingAuthority = FacingAuthority.Free)
        {
            Id = id;
            MovementAuthority = movementAuthority;
            AcceptedIntents = acceptedIntents;
            TransitionPriority = transitionPriority;
            FacingAuthority = facingAuthority;
        }

        /// <inheritdoc />
        public virtual void Enter(in FighterStateContext context, in FighterIntent intent)
        {
        }

        /// <inheritdoc />
        public abstract StateTickResult Tick(in FighterStateContext context, in FighterIntent intent, float deltaSeconds);

        /// <inheritdoc />
        public virtual void Exit(in FighterStateContext context)
        {
        }

        /// <inheritdoc />
        public bool CanTransitionTo(FighterStateId targetState)
        {
            return targetState != Id && IsTransitionAllowed(targetState);
        }

        /// <summary>
        /// Determines whether this state allows a transition to a target state.
        /// </summary>
        protected abstract bool IsTransitionAllowed(FighterStateId targetState);

        /// <summary>
        /// Determines whether a target is one of the foundation interruption states.
        /// </summary>
        protected static bool IsInterruptState(FighterStateId targetState)
        {
            return targetState == FighterStateId.HitReaction ||
                   targetState == FighterStateId.KnockedDown ||
                   targetState == FighterStateId.Defeated;
        }

        /// <summary>
        /// Selects the locomotion state that matches the requested gait.
        /// </summary>
        protected static FighterStateId GetLocomotionState(in FighterIntent intent)
        {
            return intent.Gait == MovementGait.Run
                ? FighterStateId.Run
                : FighterStateId.Walk;
        }

        /// <summary>
        /// Determines whether a target is one of the locomotion states.
        /// </summary>
        protected static bool IsLocomotionState(FighterStateId targetState)
        {
            return targetState == FighterStateId.Walk ||
                   targetState == FighterStateId.Run;
        }

        /// <summary>
        /// Determines whether a target is one of the striking states.
        /// </summary>
        protected static bool IsAttackState(FighterStateId targetState)
        {
            return targetState == FighterStateId.LightPunch ||
                   targetState == FighterStateId.HeavyPunch;
        }
    }
}
