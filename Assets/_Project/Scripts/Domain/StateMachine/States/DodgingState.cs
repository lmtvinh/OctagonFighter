using OctagonFighter.Domain.Fighters;

namespace OctagonFighter.Domain.StateMachine.States
{
    /// <summary>
    /// Timed evasive state.
    /// </summary>
    public sealed class DodgingState : FighterStateBase
    {
        private float _elapsedSeconds;

        /// <summary>
        /// Initializes a new instance of the <see cref="DodgingState"/> class.
        /// </summary>
        public DodgingState()
            : base(FighterStateId.Dodging, MovementAuthority.Limited, FighterIntentMask.None, 30)
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
            return _elapsedSeconds >= context.Data.Movement.DodgeDurationSeconds
                ? StateTickResult.TransitionTo(FighterStateId.Idle)
                : StateTickResult.Stay();
        }

        /// <inheritdoc />
        protected override bool IsTransitionAllowed(FighterStateId targetState)
        {
            return targetState == FighterStateId.Idle ||
                   IsLocomotionState(targetState) ||
                   IsInterruptState(targetState);
        }
    }
}
