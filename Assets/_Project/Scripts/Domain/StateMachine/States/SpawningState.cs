using OctagonFighter.Domain.Fighters;

namespace OctagonFighter.Domain.StateMachine.States
{
    /// <summary>
    /// Initial state that locks the fighter for the configured spawn duration.
    /// </summary>
    public sealed class SpawningState : FighterStateBase
    {
        private float _elapsedSeconds;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpawningState"/> class.
        /// </summary>
        public SpawningState()
            : base(FighterStateId.Spawning, MovementAuthority.None, FighterIntentMask.None, 100)
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
            return _elapsedSeconds >= context.Data.Movement.SpawnDurationSeconds
                ? StateTickResult.TransitionTo(FighterStateId.Idle)
                : StateTickResult.Stay();
        }

        /// <inheritdoc />
        protected override bool IsTransitionAllowed(FighterStateId targetState)
        {
            return targetState == FighterStateId.Idle || targetState == FighterStateId.Defeated;
        }
    }
}
