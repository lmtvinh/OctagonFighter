using OctagonFighter.Domain.Fighters;

namespace OctagonFighter.Domain.StateMachine.States
{
    /// <summary>
    /// Downed state entered after a knockdown.
    /// </summary>
    public sealed class KnockedDownState : FighterStateBase
    {
        private float _elapsedSeconds;

        /// <summary>
        /// Initializes a new instance of the <see cref="KnockedDownState"/> class.
        /// </summary>
        public KnockedDownState()
            : base(FighterStateId.KnockedDown, MovementAuthority.None, FighterIntentMask.None, 80)
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
            return _elapsedSeconds >= context.Data.Movement.KnockdownDurationSeconds
                ? StateTickResult.TransitionTo(FighterStateId.Recovering)
                : StateTickResult.Stay();
        }

        /// <inheritdoc />
        protected override bool IsTransitionAllowed(FighterStateId targetState)
        {
            return targetState == FighterStateId.Recovering || targetState == FighterStateId.Defeated;
        }
    }
}
