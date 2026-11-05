using OctagonFighter.Domain.Fighters;

namespace OctagonFighter.Domain.StateMachine.States
{
    /// <summary>
    /// Stun after a blocked strike depletes the guard.
    /// </summary>
    /// <remarks>
    /// The guard is no longer active while this state runs, so follow-up strikes resolve against
    /// health instead of being blocked. The runtime restores the authored post-break guard fraction
    /// when the state ends, which keeps the resource transition out of the presentation layer.
    /// </remarks>
    public sealed class GuardBreakState : FighterStateBase
    {
        private float _elapsedSeconds;

        /// <summary>
        /// Initializes a new instance of the <see cref="GuardBreakState"/> class.
        /// </summary>
        public GuardBreakState()
            : base(
                FighterStateId.GuardBreak,
                MovementAuthority.None,
                FighterIntentMask.None,
                80,
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
            return _elapsedSeconds >= context.Data.Guard.GuardBreakSeconds
                ? StateTickResult.TransitionTo(FighterStateId.Idle)
                : StateTickResult.Stay();
        }

        /// <inheritdoc />
        protected override bool IsTransitionAllowed(FighterStateId targetState)
        {
            return targetState == FighterStateId.Idle ||
                   IsLocomotionState(targetState) ||
                   targetState == FighterStateId.HitReaction ||
                   targetState == FighterStateId.Defeated;
        }
    }
}
