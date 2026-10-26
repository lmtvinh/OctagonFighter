using OctagonFighter.Domain.Combat;

namespace OctagonFighter.Domain.StateMachine.States
{
    /// <summary>
    /// Fast light strike (jab).
    /// </summary>
    public sealed class LightPunchState : AttackStateBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LightPunchState"/> class.
        /// </summary>
        public LightPunchState()
            : base(FighterStateId.LightPunch)
        {
        }

        /// <inheritdoc />
        protected override AttackSlot Slot => AttackSlot.Light;
    }
}
