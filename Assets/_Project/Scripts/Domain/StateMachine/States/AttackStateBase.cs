using System;
using OctagonFighter.Core.Ids;
using OctagonFighter.Domain.Combat;
using OctagonFighter.Domain.Fighters;
using OctagonFighter.Domain.Stamina;

namespace OctagonFighter.Domain.StateMachine.States
{
    /// <summary>
    /// Committed strike that spends stamina, starts a cooldown, and drives a timed contact window.
    /// </summary>
    /// <remarks>
    /// The state owns the strike timeline. Entry allocates one <see cref="HitId"/> for the whole
    /// attack. Every tick that falls inside the contact window re-issues a hit request with that
    /// same identifier, so the presentation layer can sweep the fist on each tick while
    /// <see cref="HitRegistry"/> keeps the strike from damaging more than one target. The state
    /// leaves for Idle exactly when the authored duration elapses, which is what makes startup,
    /// contact, and recovery distinct phases.
    /// </remarks>
    public abstract class AttackStateBase : FighterStateBase
    {
        private float _elapsedSeconds;
        private HitId _hitId;
        private AttackPhase _phase;

        /// <summary>
        /// Initializes a new instance of the <see cref="AttackStateBase"/> class.
        /// </summary>
        protected AttackStateBase(FighterStateId stateId)
            : base(
                stateId,
                MovementAuthority.None,
                FighterIntentMask.None,
                40,
                FacingAuthority.Locked)
        {
        }

        /// <summary>
        /// Gets the attack slot represented by this state.
        /// </summary>
        protected abstract AttackSlot Slot { get; }

        /// <summary>
        /// Gets the identifier shared by every hit request produced by the current strike.
        /// </summary>
        public HitId ActiveHitId => _hitId;

        /// <summary>
        /// Gets the phase the strike reached on the most recent tick.
        /// </summary>
        public AttackPhase Phase => _phase;

        /// <inheritdoc />
        public override void Enter(in FighterStateContext context, in FighterIntent intent)
        {
            AttackDefinition attack = GetDefinition(context);

            _elapsedSeconds = 0f;
            _phase = AttackPhase.Startup;
            _hitId = context.Attacks.NextHitId();

            context.Attacks.ClearBufferedAttack();
            context.Attacks.StartAttack(Slot, attack);
            context.Stamina.TrySpend(new StaminaCost(attack.StaminaCost));
        }

        /// <inheritdoc />
        public override StateTickResult Tick(
            in FighterStateContext context,
            in FighterIntent intent,
            float deltaSeconds)
        {
            AttackDefinition attack = GetDefinition(context);
            _elapsedSeconds += deltaSeconds;
            _phase = attack.GetPhase(_elapsedSeconds);

            if (_phase == AttackPhase.Active)
            {
                context.Attacks.RequestHit(new AttackHitRequest(
                    context.Data.Id,
                    Slot,
                    attack,
                    _hitId,
                    context.Clock.CurrentTick));
            }

            return _elapsedSeconds >= attack.DurationSeconds
                ? StateTickResult.TransitionTo(FighterStateId.Idle)
                : StateTickResult.Stay();
        }

        /// <inheritdoc />
        protected override bool IsTransitionAllowed(FighterStateId targetState)
        {
            return targetState == FighterStateId.Idle ||
                   IsInterruptState(targetState);
        }

        private AttackDefinition GetDefinition(in FighterStateContext context)
        {
            if (!context.Data.Attacks.TryGet(Slot, out AttackDefinition attack))
            {
                throw new InvalidOperationException(
                    $"Attack slot '{Slot}' is not defined for fighter '{context.Data.Id}'.");
            }

            return attack;
        }
    }
}
