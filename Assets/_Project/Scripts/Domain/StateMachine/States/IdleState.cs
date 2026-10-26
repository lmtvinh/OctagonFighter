using OctagonFighter.Domain.Combat;
using OctagonFighter.Domain.Fighters;

namespace OctagonFighter.Domain.StateMachine.States
{
    /// <summary>
    /// Default standing state.
    /// </summary>
    public sealed class IdleState : FighterStateBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IdleState"/> class.
        /// </summary>
        public IdleState()
            : base(
                FighterStateId.Idle,
                MovementAuthority.None,
                FighterIntentMask.Move | FighterIntentMask.Block | FighterIntentMask.Dodge | FighterIntentMask.Attack,
                10)
        {
        }

        /// <inheritdoc />
        public override StateTickResult Tick(
            in FighterStateContext context,
            in FighterIntent intent,
            float deltaSeconds)
        {
            if (AttackSelection.TrySelect(intent, context, out FighterStateId attackState))
            {
                return StateTickResult.TransitionTo(attackState);
            }

            if (intent.Command == FighterCommand.Dodge && intent.IsCommandPressed)
            {
                return StateTickResult.TransitionTo(FighterStateId.Dodging);
            }

            if (intent.Command == FighterCommand.Block && intent.IsCommandHeld)
            {
                return StateTickResult.TransitionTo(FighterStateId.Blocking);
            }

            return intent.IsMovementRequested
                ? StateTickResult.TransitionTo(GetLocomotionState(intent))
                : StateTickResult.Stay();
        }

        /// <inheritdoc />
        protected override bool IsTransitionAllowed(FighterStateId targetState)
        {
            return IsLocomotionState(targetState) ||
                   IsAttackState(targetState) ||
                   targetState == FighterStateId.Blocking ||
                   targetState == FighterStateId.Dodging ||
                   IsInterruptState(targetState);
        }
    }
}
