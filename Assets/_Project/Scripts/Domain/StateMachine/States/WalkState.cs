using OctagonFighter.Domain.Fighters;

namespace OctagonFighter.Domain.StateMachine.States
{
    /// <summary>
    /// Controlled walking movement state.
    /// </summary>
    public sealed class WalkState : LocomotionStateBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WalkState"/> class.
        /// </summary>
        public WalkState()
            : base(FighterStateId.Walk)
        {
        }

        /// <inheritdoc />
        protected override MovementGait Gait => MovementGait.Walk;
    }
}
