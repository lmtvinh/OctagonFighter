using OctagonFighter.Domain.Fighters;

namespace OctagonFighter.Domain.StateMachine.States
{
    /// <summary>
    /// Short reaction after a strike is absorbed by the guard.
    /// </summary>
    /// <remarks>
    /// The state keeps the guard active for its whole duration, so another strike that lands during
    /// the reaction is still blocked and refreshes the impact. It owns a short movement lock and a
    /// locked facing so the controlled pushback reads as a hit rather than player movement.
    /// </remarks>
    public sealed class BlockImpactState : FighterStateBase
    {
        private float _elapsedSeconds;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlockImpactState"/> class.
        /// </summary>
        public BlockImpactState()
            : base(
                FighterStateId.BlockImpact,
                MovementAuthority.None,
                FighterIntentMask.Move | FighterIntentMask.Block,
                60,
                FacingAuthority.Locked)
        {
        }

        /// <inheritdoc />
        public override void Enter(in FighterStateContext context, in FighterIntent intent)
        {
            _elapsedSeconds = 0f;
        }

        /// <inheritdoc />
        public override StateTickResult Tick(
            in FighterStateContext context,
            in FighterIntent intent,
            float deltaSeconds)
        {
            _elapsedSeconds += deltaSeconds;
            if (_elapsedSeconds < context.Data.Guard.BlockImpactSeconds)
            {
                return StateTickResult.Stay();
            }

            if (intent.Command == FighterCommand.Block && intent.IsCommandHeld)
            {
                return StateTickResult.TransitionTo(FighterStateId.Blocking);
            }

            return intent.IsMovementRequested
                ? StateTickResult.TransitionTo(GetLocomotionState(intent))
                : StateTickResult.TransitionTo(FighterStateId.Idle);
        }

        /// <inheritdoc />
        protected override bool IsTransitionAllowed(FighterStateId targetState)
        {
            return targetState == FighterStateId.Idle ||
                   targetState == FighterStateId.Blocking ||
                   IsLocomotionState(targetState) ||
                   IsInterruptState(targetState);
        }
    }
}
