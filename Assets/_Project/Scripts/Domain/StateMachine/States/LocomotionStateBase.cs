using OctagonFighter.Domain.Combat;
using OctagonFighter.Domain.Fighters;

namespace OctagonFighter.Domain.StateMachine.States
{
    /// <summary>
    /// Shared intent-driven movement behavior for walk and run states.
    /// </summary>
    public abstract class LocomotionStateBase : FighterStateBase
    {
        /// <summary>
        /// Gets the gait represented by this state.
        /// </summary>
        protected abstract MovementGait Gait { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocomotionStateBase"/> class.
        /// </summary>
        protected LocomotionStateBase(FighterStateId locomotionState)
            : base(
                locomotionState,
                MovementAuthority.Full,
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

            if (!intent.IsMovementRequested)
            {
                return StateTickResult.TransitionTo(FighterStateId.Idle);
            }

            return intent.Gait == Gait
                ? StateTickResult.Stay()
                : StateTickResult.TransitionTo(GetLocomotionState(intent));
        }

        /// <inheritdoc />
        protected override bool IsTransitionAllowed(FighterStateId targetState)
        {
            return targetState == FighterStateId.Idle ||
                   IsLocomotionState(targetState) ||
                   IsAttackState(targetState) ||
                   targetState == FighterStateId.Blocking ||
                   targetState == FighterStateId.Dodging ||
                   IsInterruptState(targetState);
        }
    }
}
