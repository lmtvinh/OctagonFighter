using OctagonFighter.Domain.Fighters;

namespace OctagonFighter.Domain.StateMachine.States
{
    /// <summary>
    /// Defensive blocking stance.
    /// </summary>
    public sealed class BlockingState : FighterStateBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BlockingState"/> class.
        /// </summary>
        public BlockingState()
            : base(
                FighterStateId.Blocking,
                MovementAuthority.Limited,
                FighterIntentMask.Move | FighterIntentMask.Block | FighterIntentMask.Dodge,
                20)
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
                return StateTickResult.Stay();
            }

            return intent.IsMovementRequested
                ? StateTickResult.TransitionTo(FighterStateId.Locomotion)
                : StateTickResult.TransitionTo(FighterStateId.Idle);
        }

        /// <inheritdoc />
        protected override bool IsTransitionAllowed(FighterStateId targetState)
        {
            return targetState == FighterStateId.Idle ||
                   targetState == FighterStateId.Locomotion ||
                   targetState == FighterStateId.Dodging ||
                   IsInterruptState(targetState);
        }
    }
}
