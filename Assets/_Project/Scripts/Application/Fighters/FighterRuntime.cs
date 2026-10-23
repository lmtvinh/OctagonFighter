using System;
using OctagonFighter.Application.Time;
using OctagonFighter.Core.Ids;
using OctagonFighter.Core.Time;
using OctagonFighter.Domain.Fighters;
using OctagonFighter.Domain.Health;
using OctagonFighter.Domain.Stamina;
using OctagonFighter.Domain.StateMachine;

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
        /// Occurs after a successful state transition.
        /// </summary>
        public event Action<FighterStateChangedEvent> StateChanged;

        /// <summary>
        /// Occurs once when the fighter becomes defeated.
        /// </summary>
        public event Action<FighterDefeatedEvent> Defeated;

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

                FighterSnapshot beforeTick = CreateSnapshot(_stateMachine.CurrentStateId, tick);
                FighterStateContext context = CreateContext(beforeTick);
                StateTransition? transition = _stateMachine.Tick(context, intent, deltaSeconds);

                if (transition.HasValue)
                {
                    RaiseStateChanged(transition.Value);
                }

                if (_health.IsKnockedOut)
                {
                    ForceDefeat(tick);
                }
            }

            _snapshot = CreateSnapshot(_stateMachine.CurrentStateId, tick);
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
                _pendingDefeat = true;
            }

            return result;
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
            return new FighterStateContext(_data, _health, _stamina, _clock, snapshot);
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
