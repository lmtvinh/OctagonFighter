using System;
using OctagonFighter.Application.Time;
using OctagonFighter.Core.Ids;
using OctagonFighter.Core.Time;
using OctagonFighter.Domain.Combat;
using OctagonFighter.Domain.Fighters;
using OctagonFighter.Domain.Health;
using OctagonFighter.Domain.Stamina;
using OctagonFighter.Domain.StateMachine;
using OctagonFighter.Domain.StateMachine.States;

namespace OctagonFighter.Application.Fighters
{
    /// <summary>
    /// Pure runtime orchestration for one fighter's health, stamina, intents, and state.
    /// </summary>
    public sealed class FighterRuntime : IDisposable
    {
        private readonly FighterData _data;
        private readonly HealthModel _health;
        private readonly StaminaModel _stamina;
        private readonly FighterStateMachine _stateMachine;
        private readonly IFighterIntentSource _intentSource;
        private readonly ICombatClock _clock;
        private readonly AttackTracker _attacks;
        private FighterSnapshot _snapshot;
        private bool _pendingDefeat;
        private bool _defeatAnnounced;
        private bool _disposed;

        /// <summary>
        /// Gets the immutable fighter data.
        /// </summary>
        public FighterData Data => _data;

        /// <summary>
        /// Gets the health model.
        /// </summary>
        public HealthModel Health => _health;

        /// <summary>
        /// Gets the stamina model.
        /// </summary>
        public StaminaModel Stamina => _stamina;

        /// <summary>
        /// Gets the state machine.
        /// </summary>
        public FighterStateMachine StateMachine => _stateMachine;

        /// <summary>
        /// Gets the attack cooldown, input buffer, and hit-request tracker.
        /// </summary>
        public AttackTracker Attacks => _attacks;

        /// <summary>
        /// Gets the latest immutable snapshot.
        /// </summary>
        public FighterSnapshot Snapshot => _snapshot;

        /// <summary>
        /// Gets the intent consumed by the most recent tick.
        /// </summary>
        public FighterIntent CurrentIntent { get; private set; }

        /// <summary>
        /// Gets the movement authority of the current state.
        /// </summary>
        public MovementAuthority CurrentMovementAuthority => _stateMachine.CurrentMovementAuthority;

        /// <summary>
        /// Gets the facing authority of the current state.
        /// </summary>
        public FacingAuthority CurrentFacingAuthority => _stateMachine.CurrentFacingAuthority;

        /// <summary>
        /// Gets the reaction that is currently being presented, or a default value when the fighter
        /// is not reacting.
        /// </summary>
        public HitReactionInfo ActiveHitReaction { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the fighter is in a hit reaction.
        /// </summary>
        public bool IsReactingToHit => _stateMachine.CurrentStateId == FighterStateId.HitReaction;

        /// <summary>
        /// Occurs after a successful state transition.
        /// </summary>
        public event Action<FighterStateChangedEvent> StateChanged;

        /// <summary>
        /// Occurs once when the fighter becomes defeated.
        /// </summary>
        public event Action<FighterDefeatedEvent> Defeated;

        /// <summary>
        /// Occurs when an active strike frame requests hit resolution.
        /// </summary>
        public event Action<AttackHitRequest> HitRequested;

        /// <summary>
        /// Occurs once for every confirmed hit that starts (or refreshes) a hit reaction.
        /// </summary>
        public event Action<HitReactionInfo> HitReactionStarted;

        /// <summary>
        /// Initializes a new instance of the <see cref="FighterRuntime"/> class.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when a dependency is null.</exception>
        public FighterRuntime(
            FighterData data,
            HealthModel health,
            StaminaModel stamina,
            FighterStateMachine stateMachine,
            IFighterIntentSource intentSource,
            ICombatClock clock)
        {
            _data = data;
            _health = health ?? throw new ArgumentNullException(nameof(health));
            _stamina = stamina ?? throw new ArgumentNullException(nameof(stamina));
            _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
            _intentSource = intentSource ?? throw new ArgumentNullException(nameof(intentSource));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _attacks = new AttackTracker(data.Attacks);

            _health.KnockedOut += OnHealthKnockedOut;

            FighterSnapshot initialSnapshot = CreateSnapshot(
                FighterStateId.Spawning,
                _clock.CurrentTick);
            var initialContext = CreateContext(initialSnapshot);
            _stateMachine.Initialize(FighterStateId.Spawning, initialContext, FighterIntent.None);
            _snapshot = CreateSnapshot(_stateMachine.CurrentStateId, _clock.CurrentTick);
        }

        /// <summary>
        /// Advances the fighter by one fixed tick.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the runtime has been disposed.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="deltaSeconds"/> is invalid.</exception>
        public void Tick(float deltaSeconds)
        {
            ThrowIfDisposed();

            if (!float.IsFinite(deltaSeconds) || deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Delta time must be finite and non-negative.");
            }

            TickId tick = _clock.CurrentTick;
            if (_pendingDefeat)
            {
                ForceDefeat(tick);
            }

            if (!_intentSource.TryGetIntent(tick, out FighterIntent intent))
            {
                intent = FighterIntent.None;
            }

            CurrentIntent = intent;

            if (_stateMachine.CurrentStateId != FighterStateId.Defeated)
            {
                _stamina.Tick(deltaSeconds);
                _attacks.Tick(deltaSeconds);
                BufferAttackIntent(intent);

                FighterSnapshot beforeTick = CreateSnapshot(_stateMachine.CurrentStateId, tick);
                FighterStateContext context = CreateContext(beforeTick);
                StateTransition? transition = _stateMachine.Tick(context, intent, deltaSeconds);

                if (transition.HasValue)
                {
                    RaiseStateChanged(transition.Value);
                }

                if (_attacks.TryTakeHitRequest(out AttackHitRequest hitRequest))
                {
                    HitRequested?.Invoke(hitRequest);
                }

                if (_health.IsKnockedOut)
                {
                    ForceDefeat(tick);
                }
            }

            _snapshot = CreateSnapshot(_stateMachine.CurrentStateId, tick);

            // The reaction payload lives exactly as long as the reaction state, so observers never
            // read a stale region after control has been handed back.
            if (_stateMachine.CurrentStateId != FighterStateId.HitReaction)
            {
                ActiveHitReaction = default;
            }

            AnnounceDefeat(tick);
        }

        /// <summary>
        /// Applies resolved damage through the health model.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the runtime has been disposed.</exception>
        public DamageResult ApplyDamage(in DamageEvent damage)
        {
            ThrowIfDisposed();

            DamageResult result = _health.ApplyDamage(damage);

            if (_health.IsKnockedOut)
            {
                // Defeat is decided before any reaction, so a killing blow enters the terminal
                // Defeat state instead of a normal reaction.
                ForceDefeat(damage.Tick);
            }
            else if (result.AppliedAmount > 0f && damage.Type == DamageType.Impact)
            {
                // Damage applied through the runtime follows the same rule as damage applied through
                // the Unity health adapter: only a hit that actually removed health reacts.
                ReactToHit(damage.Region, damage.Tick);
            }

            return result;
        }

        /// <summary>
        /// Starts or refreshes the hit reaction for a confirmed hit.
        /// </summary>
        /// <remarks>
        /// This is the single authoritative interrupt path for reactions. It is called only after a
        /// hit has been confirmed and applied, so a missed strike can never reach it. A hit that
        /// lands during an existing reaction refreshes that reaction instead of stacking a second
        /// one, and a defeated fighter ignores reactions because defeat is terminal.
        /// </remarks>
        /// <param name="region">Body region that received the hit.</param>
        /// <param name="tick">Tick in which the hit was resolved.</param>
        /// <returns><see langword="true"/> when a reaction was started or refreshed.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the runtime has been disposed.</exception>
        public bool ReactToHit(HitRegion region, TickId tick)
        {
            ThrowIfDisposed();

            if (_stateMachine.CurrentStateId == FighterStateId.Defeated)
            {
                return false;
            }

            // A killing blow must never start a normal reaction, even temporarily. The Unity health
            // adapter raises KnockedOut and then Damaged before the next Tick, so this guard also
            // covers that path: damage is applied, defeat is decided, and only a surviving fighter
            // reacts.
            if (_health.IsKnockedOut || _pendingDefeat)
            {
                ForceDefeat(tick);
                return false;
            }

            if (!_stateMachine.TryGetState(FighterStateId.HitReaction, out HitReactionState reaction))
            {
                return false;
            }

            reaction.Prepare(region);

            FighterSnapshot snapshot = CreateSnapshot(_stateMachine.CurrentStateId, _clock.CurrentTick);
            FighterStateContext context = CreateContext(snapshot);
            var info = new HitReactionInfo(region, tick);

            if (_stateMachine.CurrentStateId == FighterStateId.HitReaction)
            {
                if (!_stateMachine.RestartCurrentState(context, FighterIntent.None))
                {
                    return false;
                }
            }
            else
            {
                FighterStateId previousState = _stateMachine.CurrentStateId;
                if (!_stateMachine.ForceTransition(FighterStateId.HitReaction, context, FighterIntent.None))
                {
                    return false;
                }

                RaiseStateChanged(new StateTransition(previousState, FighterStateId.HitReaction, tick));
            }

            ActiveHitReaction = info;
            HitReactionStarted?.Invoke(info);
            return true;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _health.KnockedOut -= OnHealthKnockedOut;
            StateChanged = null;
            Defeated = null;
            HitRequested = null;
            HitReactionStarted = null;
            _disposed = true;
        }

        private void OnHealthKnockedOut(FighterKnockedOutEvent knockedOutEvent)
        {
            _pendingDefeat = true;
        }

        private void ForceDefeat(TickId tick)
        {
            _pendingDefeat = false;

            if (_stateMachine.CurrentStateId == FighterStateId.Defeated)
            {
                return;
            }

            FighterSnapshot snapshot = CreateSnapshot(_stateMachine.CurrentStateId, tick);
            FighterStateContext context = CreateContext(snapshot);
            FighterStateId previousState = _stateMachine.CurrentStateId;

            if (!_stateMachine.ForceTransition(FighterStateId.Defeated, context, FighterIntent.None))
            {
                return;
            }

            RaiseStateChanged(new StateTransition(previousState, FighterStateId.Defeated, tick));

            // Defeat ends any running reaction immediately; do not leave a stale payload for
            // observers between this transition and the next Tick.
            ActiveHitReaction = default;
        }

        private void AnnounceDefeat(TickId tick)
        {
            if (_defeatAnnounced || _stateMachine.CurrentStateId != FighterStateId.Defeated)
            {
                return;
            }

            _defeatAnnounced = true;
            Defeated?.Invoke(new FighterDefeatedEvent(_data.Id, tick));
        }

        private void BufferAttackIntent(in FighterIntent intent)
        {
            if (!intent.IsCommandPressed)
            {
                return;
            }

            switch (intent.Command)
            {
                case FighterCommand.LightPunch:
                    _attacks.BufferAttack(AttackSlot.Light);
                    break;

                case FighterCommand.HeavyPunch:
                    _attacks.BufferAttack(AttackSlot.Heavy);
                    break;
            }
        }

        private void RaiseStateChanged(StateTransition transition)
        {
            StateChanged?.Invoke(new FighterStateChangedEvent(
                _data.Id,
                transition.From,
                transition.To,
                transition.Tick));
        }

        private FighterSnapshot CreateSnapshot(FighterStateId state, TickId tick)
        {
            return new FighterSnapshot(
                _data.Id,
                state,
                _health.CurrentHealth,
                _health.MaxHealth,
                _stamina.CurrentStamina,
                _stamina.MaxStamina,
                _stamina.Tier,
                _health.IsAlive,
                _health.IsKnockedOut,
                tick);
        }

        private FighterStateContext CreateContext(FighterSnapshot snapshot)
        {
            return new FighterStateContext(_data, _health, _stamina, _clock, _attacks, snapshot);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(FighterRuntime));
            }
        }
    }
}
