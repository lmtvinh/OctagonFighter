using OctagonFighter.Domain.Fighters;

namespace OctagonFighter.Domain.StateMachine.States
{
    /// <summary>
    /// Timed reaction to received damage.
    /// </summary>
    public sealed class HitReactionState : FighterStateBase
    {
        private float _elapsedSeconds;

        /// <summary>
        /// Initializes a new instance of the <see cref="HitReactionState"/> class.
        /// </summary>
        public HitReactionState()
            : base(FighterStateId.HitReaction, MovementAuthority.None, FighterIntentMask.None, 70)
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
            return _elapsedSeconds >= context.Data.Movement.HitReactionDurationSeconds
                ? StateTickResult.TransitionTo(FighterStateId.Idle)
                : StateTickResult.Stay();
        }

        /// <inheritdoc />
        protected override bool IsTransitionAllowed(FighterStateId targetState)
        {
            return targetState == FighterStateId.Idle ||
                   IsLocomotionState(targetState) ||
                   targetState == FighterStateId.KnockedDown ||
                   targetState == FighterStateId.Defeated;
        }
    }
}
