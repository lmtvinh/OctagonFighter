using OctagonFighter.Domain.Combat;

namespace OctagonFighter.Domain.StateMachine.States
{
    /// <summary>
    /// Slow heavy strike (cross).
    /// </summary>
    public sealed class HeavyPunchState : AttackStateBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HeavyPunchState"/> class.
        /// </summary>
        public HeavyPunchState()
            : base(FighterStateId.HeavyPunch)
        {
        }

        /// <inheritdoc />
        protected override AttackSlot Slot => AttackSlot.Heavy;
    }
}
