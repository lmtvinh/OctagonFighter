using OctagonFighter.Domain.Fighters;

namespace OctagonFighter.Domain.StateMachine.States
{
    /// <summary>
    /// Intent-driven movement state.
    /// </summary>
    public sealed class LocomotionState : FighterStateBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LocomotionState"/> class.
        /// </summary>
        public LocomotionState()
            : base(
                FighterStateId.Locomotion,
                MovementAuthority.Full,
                FighterIntentMask.Move | FighterIntentMask.Block | FighterIntentMask.Dodge,
                10)
        {
        }

        /// <inheritdoc />
        public override StateTickResult Tick(
            in FighterStateContext context,
            in FighterIntent intent,
            float deltaSeconds)
        {
            if (intent.Command == FighterCommand.Dodge && intent.IsCommandPressed)
            {
                return StateTickResult.TransitionTo(FighterStateId.Dodging);
            }

            if (intent.Command == FighterCommand.Block && intent.IsCommandHeld)
            {
                return StateTickResult.TransitionTo(FighterStateId.Blocking);
            }

            return intent.IsMovementRequested
                ? StateTickResult.Stay()
                : StateTickResult.TransitionTo(FighterStateId.Idle);
        }

        /// <inheritdoc />
        protected override bool IsTransitionAllowed(FighterStateId targetState)
        {
            return targetState == FighterStateId.Idle ||
                   targetState == FighterStateId.Blocking ||
                   targetState == FighterStateId.Dodging ||
                   IsInterruptState(targetState);
        }
    }
}
