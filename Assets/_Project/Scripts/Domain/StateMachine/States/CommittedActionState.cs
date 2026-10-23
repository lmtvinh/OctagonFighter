using OctagonFighter.Domain.Fighters;

namespace OctagonFighter.Domain.StateMachine.States
{
    /// <summary>
    /// Generic locked state reserved for future combat actions.
    /// </summary>
    public sealed class CommittedActionState : FighterStateBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommittedActionState"/> class.
        /// </summary>
        public CommittedActionState()
            : base(FighterStateId.CommittedAction, MovementAuthority.None, FighterIntentMask.None, 50)
        {
        }

        /// <inheritdoc />
        public override StateTickResult Tick(
            in FighterStateContext context,
            in FighterIntent intent,
            float deltaSeconds)
        {
            return StateTickResult.Stay();
        }

        /// <inheritdoc />
        protected override bool IsTransitionAllowed(FighterStateId targetState)
        {
            return targetState == FighterStateId.Idle ||
                   targetState == FighterStateId.Locomotion ||
                   IsInterruptState(targetState);
        }
    }
}
