using System;
using OctagonFighter.Domain.Combat;
using OctagonFighter.Domain.Fighters;
using OctagonFighter.Domain.Stamina;

namespace OctagonFighter.Domain.StateMachine.States
{
    /// <summary>
    /// Committed strike that spends stamina, starts a cooldown, and emits one hit request.
    /// </summary>
    public abstract class AttackStateBase : FighterStateBase
    {
        private float _elapsedSeconds;
        private bool _hitRequested;

        /// <summary>
        /// Gets the attack slot represented by this state.
        /// </summary>
        protected abstract AttackSlot Slot { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttackStateBase"/> class.
        /// </summary>
        protected AttackStateBase(FighterStateId stateId)
            : base(stateId, MovementAuthority.None, FighterIntentMask.None, 40)
        {
        }

        /// <inheritdoc />
        public override void Enter(in FighterStateContext context, in FighterIntent intent)
        {
            AttackDefinition attack = GetDefinition(context);

            _elapsedSeconds = 0f;
            _hitRequested = false;

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

            if (!_hitRequested && _elapsedSeconds >= attack.StartupSeconds)
            {
                _hitRequested = true;
                context.Attacks.RequestHit(new AttackHitRequest(
                    context.Data.Id,
                    Slot,
                    attack,
                    context.Attacks.NextHitId(),
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
