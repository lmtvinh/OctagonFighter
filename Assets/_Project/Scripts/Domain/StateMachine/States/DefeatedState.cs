using OctagonFighter.Domain.Fighters;

namespace OctagonFighter.Domain.StateMachine.States
{
    /// <summary>
    /// Terminal defeated state.
    /// </summary>
    public sealed class DefeatedState : FighterStateBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefeatedState"/> class.
        /// </summary>
        public DefeatedState()
            : base(FighterStateId.Defeated, MovementAuthority.None, FighterIntentMask.None, 100)
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
            return false;
        }
    }
}
