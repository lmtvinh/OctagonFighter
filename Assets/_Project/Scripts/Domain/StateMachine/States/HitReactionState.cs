using OctagonFighter.Domain.Fighters;
using OctagonFighter.Domain.Health;

namespace OctagonFighter.Domain.StateMachine.States
{
    /// <summary>
    /// Timed reaction to received damage.
    /// </summary>
    /// <remarks>
    /// The state is entered only from a confirmed hit (see <see cref="HitReactionInfo"/>), so a miss
    /// can never start a reaction. While it runs it grants no movement authority and accepts no
    /// intents, which is what makes movement and attack input unable to interrupt the stun. Facing
    /// is locked for the same reason it is locked during a committed strike: a staggered fighter
    /// must not spin toward its opponent. The state ends on its authored duration, so a missing or
    /// interrupted animation can never lock the fighter.
    /// </remarks>
    public sealed class HitReactionState : FighterStateBase
    {
        private float _elapsedSeconds;
        private HitRegion _pendingRegion;

        /// <summary>
        /// Initializes a new instance of the <see cref="HitReactionState"/> class.
        /// </summary>
        public HitReactionState()
            : base(
                FighterStateId.HitReaction,
                MovementAuthority.None,
                FighterIntentMask.None,
                70,
                FacingAuthority.Locked)
        {
        }

        /// <summary>
        /// Gets the body region of the hit that started the current reaction.
        /// </summary>
        public HitRegion Region { get; private set; }

        /// <summary>
        /// Records which reaction variant the next entry should present.
        /// </summary>
        /// <remarks>
        /// The state machine has no payload channel, so the authoritative interrupt path sets the
        /// region here immediately before forcing or restarting the state. Entry consumes it, which
        /// keeps the region stable for the whole reaction even if another hit lands mid-stun.
        /// </remarks>
        public void Prepare(HitRegion region)
        {
            _pendingRegion = region;
        }

        /// <inheritdoc />
        public override void Enter(in FighterStateContext context, in FighterIntent intent)
        {
            _elapsedSeconds = 0f;
            Region = _pendingRegion;
        }

        /// <inheritdoc />
        public override StateTickResult Tick(
            in FighterStateContext context,
            in FighterIntent intent,
            float deltaSeconds)
        {
            _elapsedSeconds += deltaSeconds;
            return _elapsedSeconds >= context.Data.Movement.HitReactionDurationSeconds
                ? StateTickResult.TransitionTo(FighterStateId.Idle)
                : StateTickResult.Stay();
        }

        /// <inheritdoc />
        protected override bool IsTransitionAllowed(FighterStateId targetState)
        {
            return targetState == FighterStateId.Idle ||
                   IsLocomotionState(targetState) ||
                   targetState == FighterStateId.KnockedDown ||
                   targetState == FighterStateId.Defeated;
        }
    }
}
