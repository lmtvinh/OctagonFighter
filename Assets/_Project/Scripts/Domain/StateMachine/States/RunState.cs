using OctagonFighter.Domain.Fighters;

namespace OctagonFighter.Domain.StateMachine.States
{
    /// <summary>
    /// Committed running movement state.
    /// </summary>
    public sealed class RunState : LocomotionStateBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RunState"/> class.
        /// </summary>
        public RunState()
            : base(FighterStateId.Run)
        {
        }

        /// <inheritdoc />
        protected override MovementGait Gait => MovementGait.Run;
    }
}
